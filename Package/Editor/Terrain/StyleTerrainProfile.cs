using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleTerrainProfile
    {
        const int Samples = 256;
        const int SlopeBins = 90;
        const int HeightBins = 64;

        static readonly Color s_Back = new Color(0.12f, 0.12f, 0.12f, 1f);
        static readonly Color s_Fill = new Color(0.75f, 0.75f, 0.75f, 1f);

        static readonly Dictionary<HBStyleTerrainData, StyleTerrainProfile> s_Cache =
            new Dictionary<HBStyleTerrainData, StyleTerrainProfile>();

        internal float low;
        internal float high = 1f;
        internal Texture2D histogram;

        int m_Version = -1;

        internal static StyleTerrainProfile Get(HBStyleTerrainData data)
        {
            if (data == null || data.terrainData == null)
                return null;

            if (!s_Cache.TryGetValue(data, out var profile))
            {
                profile = new StyleTerrainProfile();
                s_Cache.Add(data, profile);
            }

            if (profile.m_Version != data.version || profile.histogram == null)
                profile.Build(data.terrainData, data.version);

            return profile;
        }

        void Build(TerrainData terrain, int version)
        {
            var slopes = new float[Samples * Samples];
            var heights = new float[slopes.Length];
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            for (var z = 0; z < Samples; z++)
            {
                for (var x = 0; x < Samples; x++)
                {
                    var u = (x + 0.5f) / Samples;
                    var v = (z + 0.5f) / Samples;
                    var index = z * Samples + x;

                    slopes[index] = terrain.GetSteepness(u, v);
                    heights[index] = terrain.GetInterpolatedHeight(u, v);
                    lowest = Mathf.Min(lowest, heights[index]);
                    highest = Mathf.Max(highest, heights[index]);
                }
            }

            var pad = Mathf.Max((highest - lowest) * 0.05f, 1f);

            low = Mathf.Floor(lowest - pad);
            high = Mathf.Ceil(highest + pad);

            var counts = new int[SlopeBins * HeightBins];
            var most = 1;

            for (var index = 0; index < slopes.Length; index++)
            {
                var column = Mathf.Clamp((int)(slopes[index] / 90f * SlopeBins), 0, SlopeBins - 1);
                var line = Mathf.Clamp((int)((heights[index] - low) / (high - low) * HeightBins), 0, HeightBins - 1);
                var bin = line * SlopeBins + column;

                counts[bin]++;
                most = Mathf.Max(most, counts[bin]);
            }

            if (histogram == null)
            {
                histogram = new Texture2D(SlopeBins, HeightBins, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var pixels = new Color[counts.Length];
            var scale = 1f / Mathf.Log(1f + most);

            for (var index = 0; index < counts.Length; index++)
            {
                var amount = counts[index] > 0 ? 0.25f + 0.75f * Mathf.Log(1f + counts[index]) * scale : 0f;
                pixels[index] = Color.Lerp(s_Back, s_Fill, amount);
            }

            histogram.SetPixels(pixels);
            histogram.Apply();
            m_Version = version;
        }
    }
}
