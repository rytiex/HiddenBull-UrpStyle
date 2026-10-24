using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    struct StyleMixResult
    {
        internal StyleFrameGlobals globals;
        internal Texture skyLut;
        internal Texture brushAtlas;
        internal Texture cloudAtlas;
    }

    sealed class StyleEnvironmentMixer : IDisposable
    {
        readonly Dictionary<HBStyleEnvironmentProfile, StyleSkyLut> m_Luts =
            new Dictionary<HBStyleEnvironmentProfile, StyleSkyLut>();

        readonly List<HBStyleEnvironmentProfile> m_Stale = new List<HBStyleEnvironmentProfile>();

        StyleSkyLut Lut(HBStyleEnvironmentProfile profile)
        {
            if (!m_Luts.TryGetValue(profile, out var lut))
            {
                lut = new StyleSkyLut();
                m_Luts.Add(profile, lut);
            }

            lut.Bake(profile);

            return lut;
        }

        internal void Prune()
        {
            foreach (var pair in m_Luts)
            {
                if (pair.Key == null)
                    m_Stale.Add(pair.Key);
            }

            for (var i = 0; i < m_Stale.Count; i++)
            {
                m_Luts[m_Stale[i]].Dispose();
                m_Luts.Remove(m_Stale[i]);
            }

            m_Stale.Clear();
        }

        internal StyleMixResult Resolve(StyleProfileMix mix, StyleSkyLut mixLut, HBStyleQualityProfile quality,
                                        int cascades)
        {
            var result = new StyleMixResult();
            var target = mix.target;
            var targetLut = Lut(target);

            if (mix.single || mixLut == null)
            {
                result.globals = StyleFrameGlobals.Resolve(target, targetLut, quality, cascades);
                result.skyLut = targetLut.texture;
                result.brushAtlas = target.brush.atlas;
                result.cloudAtlas = target.clouds.atlas;
                return result;
            }

            var start = mix.blend < 1f ? mix.source : target;
            var startLut = Lut(start);
            var key = Key(mix, start);
            var rebuild = key != mixLut.mixKey;

            if (rebuild)
            {
                mixLut.CopyFrom(startLut);
                mixLut.mixKey = key;
            }

            result.globals = StyleFrameGlobals.Resolve(start, startLut, quality, cascades);
            result.brushAtlas = start.brush.atlas;
            result.cloudAtlas = start.clouds.atlas;

            if (mix.blend < 1f)
                Apply(ref result, mixLut, rebuild, target, mix.blend, HBStyleProfileSections.All, quality, cascades);

            for (var i = 0; i < mix.zones.Count; i++)
            {
                var zone = mix.zones[i];
                Apply(ref result, mixLut, rebuild, zone.profile, zone.weight, zone.sections, quality, cascades);
            }

            result.skyLut = mixLut.texture;

            return result;
        }

        void Apply(ref StyleMixResult result, StyleSkyLut mixLut, bool rebuild, HBStyleEnvironmentProfile profile,
                   float weight, HBStyleProfileSections sections, HBStyleQualityProfile quality, int cascades)
        {
            var lut = Lut(profile);
            var other = StyleFrameGlobals.Resolve(profile, lut, quality, cascades);

            StyleFrameGlobals.Mix(ref result.globals, other, weight, sections);

            if ((sections & HBStyleProfileSections.Brush) != 0 && weight >= 0.5f)
            {
                result.brushAtlas = profile.brush.atlas;
                result.globals._HB_Brush.w = result.brushAtlas != null ? 1f : 0f;
            }

            if ((sections & HBStyleProfileSections.Clouds) != 0)
            {
                var next = profile.clouds.atlas;

                if (result.cloudAtlas == null)
                {
                    result.cloudAtlas = next;
                }
                else if (next != null && next != result.cloudAtlas)
                {
                    result.globals._HB_CloudParams.x *= Mathf.Abs(1f - 2f * weight);

                    if (weight >= 0.5f)
                        result.cloudAtlas = next;
                }
            }

            if (rebuild)
            {
                var sky = (sections & HBStyleProfileSections.Sky) != 0 ? weight : 0f;
                var fog = (sections & HBStyleProfileSections.Atmosphere) != 0 ? weight : 0f;

                mixLut.BlendToward(lut, sky, fog);
            }
        }

        int Key(StyleProfileMix mix, HBStyleEnvironmentProfile start)
        {
            var hash = new HashCode();

            hash.Add(start);
            hash.Add(Lut(start).version);
            hash.Add(mix.target);
            hash.Add(Lut(mix.target).version);
            hash.Add(Mathf.RoundToInt(mix.blend * 4096f));

            for (var i = 0; i < mix.zones.Count; i++)
            {
                var zone = mix.zones[i];

                hash.Add(zone.profile);
                hash.Add(Lut(zone.profile).version);
                hash.Add(Mathf.RoundToInt(zone.weight * 4096f));
                hash.Add((int)zone.sections);
            }

            return hash.ToHashCode();
        }

        public void Dispose()
        {
            foreach (var pair in m_Luts)
                pair.Value.Dispose();

            m_Luts.Clear();
        }
    }
}
