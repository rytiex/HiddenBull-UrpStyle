using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleRenderSettings : IEquatable<HBStyleRenderSettings>
    {
        public HBStyleShadowQuality shadowQuality;

        public float shadowDistance;

        public HBStyleGrassQuality grass;

        public HBStylePrecipitationQuality precipitation;

        public HBStyleLiquidQuality liquid;

        static readonly List<HBStyleFeature> s_Features = new List<HBStyleFeature>();

        public static HBStyleRenderSettings Default => new HBStyleRenderSettings
        {
            shadowQuality = HBStyleShadowQuality.High,
            shadowDistance = HBStyleShadowQualitySettings.DefaultDistance(HBStyleShadowQuality.High),
            grass = HBStyleGrassQuality.High,
            precipitation = HBStylePrecipitationQuality.Ultra,
            liquid = HBStyleLiquidQuality.High
        };

        public static bool TryGet(out HBStyleRenderSettings settings)
        {
            var features = Features();

            if (features.Count == 0)
            {
                settings = Default;

                return false;
            }

            settings = Read(features[0]);

            return true;
        }

        public static HBStyleRenderSettings Get()
        {
            TryGet(out var settings);

            return settings;
        }

        public static bool Set(HBStyleRenderSettings settings)
        {
            var changed = false;

            foreach (var feature in Features())
            {
                var current = Read(feature);

                if (current.Equals(settings))
                    continue;

#if UNITY_EDITOR
                Remember(feature, current);
#endif
                Write(feature, current, settings);
                changed = true;
            }

            return changed;
        }

        public bool Equals(HBStyleRenderSettings other)
        {
            return shadowQuality == other.shadowQuality &&
                   Mathf.Approximately(shadowDistance, other.shadowDistance) &&
                   grass == other.grass &&
                   precipitation == other.precipitation &&
                   liquid == other.liquid;
        }

        public override bool Equals(object obj)
        {
            return obj is HBStyleRenderSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(shadowQuality, Mathf.RoundToInt(shadowDistance * 16f), grass, precipitation,
                                    liquid);
        }

        static List<HBStyleFeature> Features()
        {
            s_Features.Clear();

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset)
                return s_Features;

            foreach (var data in asset.rendererDataList)
            {
                if (data != null && data.TryGetRendererFeature(out HBStyleFeature feature) &&
                    !s_Features.Contains(feature))
                    s_Features.Add(feature);
            }

            return s_Features;
        }

        static HBStyleRenderSettings Read(HBStyleFeature feature)
        {
            return new HBStyleRenderSettings
            {
                shadowQuality = feature.shadows.quality,
                shadowDistance = feature.shadows.distance,
                grass = feature.grass.quality,
                precipitation = feature.precipitation.quality,
                liquid = feature.liquidQuality
            };
        }

        static void Write(HBStyleFeature feature, HBStyleRenderSettings current, HBStyleRenderSettings target)
        {
            if (target.shadowQuality != current.shadowQuality)
                feature.shadows.quality = target.shadowQuality;

            if (!Mathf.Approximately(target.shadowDistance, current.shadowDistance))
                feature.shadows.distance = target.shadowDistance;

            feature.grass.quality = target.grass;
            feature.precipitation.quality = target.precipitation;
            feature.liquidQuality = target.liquid;
        }

#if UNITY_EDITOR
        static readonly Dictionary<HBStyleFeature, HBStyleRenderSettings> s_Authored =
            new Dictionary<HBStyleFeature, HBStyleRenderSettings>();

        static void Remember(HBStyleFeature feature, HBStyleRenderSettings current)
        {
            if (!Application.isPlaying || s_Authored.ContainsKey(feature))
                return;

            if (s_Authored.Count == 0)
                UnityEditor.EditorApplication.playModeStateChanged += Restore;

            s_Authored.Add(feature, current);
        }

        static void Restore(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                return;

            UnityEditor.EditorApplication.playModeStateChanged -= Restore;

            foreach (var pair in s_Authored)
            {
                if (pair.Key != null)
                    Write(pair.Key, Read(pair.Key), pair.Value);
            }

            s_Authored.Clear();
        }
#endif
    }
}
