using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleBakeQuality
    {
        Potato,
        Low,
        Default,
        Ultra
    }

    static class StyleBakeDefaults
    {
        public const int CustomPacking = 1;

        const int XAtlasPacking = 1;
        const int Padding = 4;

        readonly struct Tier
        {
            public readonly int direct;
            public readonly int indirect;
            public readonly int environment;
            public readonly float probes;
            public readonly int bounces;
            public readonly float resolution;
            public readonly int maxSize;
            public readonly LightmapCompression compression;

            public Tier(int direct, int indirect, int environment, float probes, int bounces, float resolution,
                        int maxSize, LightmapCompression compression)
            {
                this.direct = direct;
                this.indirect = indirect;
                this.environment = environment;
                this.probes = probes;
                this.bounces = bounces;
                this.resolution = resolution;
                this.maxSize = maxSize;
                this.compression = compression;
            }
        }

        static readonly Tier[] s_Tiers =
        {
            new Tier(4, 16, 8, 2f, 1, 15f, 512, LightmapCompression.LowQuality),
            new Tier(8, 32, 8, 3f, 1, 30f, 1024, LightmapCompression.NormalQuality),
            new Tier(16, 64, 8, 6f, 2, 60f, 2048, LightmapCompression.HighQuality),
            new Tier(32, 128, 16, 12f, 4, 120f, 4096, LightmapCompression.HighQuality)
        };

        public static void Apply(LightingSettings settings) => Apply(settings, StyleBakeQuality.Default);

        public static void Apply(LightingSettings settings, StyleBakeQuality quality)
        {
            var tier = s_Tiers[(int)quality];

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.Shadowmask;

            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.environmentImportanceSampling = false;
            settings.directSampleCount = tier.direct;
            settings.indirectSampleCount = tier.indirect;
            settings.environmentSampleCount = tier.environment;
            settings.lightProbeSampleCountMultiplier = tier.probes;
            settings.maxBounces = tier.bounces;
            settings.filteringMode = LightingSettings.FilterMode.Auto;

            settings.lightmapResolution = tier.resolution;
            settings.lightmapMaxSize = tier.maxSize;
            settings.lightmapCompression = tier.compression;
            settings.lightmapPadding = Padding;
            settings.ao = false;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            settings.albedoBoost = 1f;
            settings.indirectScale = 1f;

            var serialized = new SerializedObject(settings);

            SetInt(serialized, "m_LightmapPackingMode", CustomPacking);
            SetInt(serialized, "m_LightmapPackingMethod", XAtlasPacking);
            SetBool(serialized, "m_XAtlasBlockAlign", true);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(SerializedObject serialized, string name, int value)
        {
            var property = serialized.FindProperty(name);

            if (property != null)
                property.intValue = value;
        }

        static void SetBool(SerializedObject serialized, string name, bool value)
        {
            var property = serialized.FindProperty(name);

            if (property != null)
                property.boolValue = value;
        }

        static bool BlockAligned(LightingSettings settings)
        {
            var property = new SerializedObject(settings).FindProperty("m_XAtlasBlockAlign");

            return property == null || property.boolValue;
        }

        public static bool Matches(LightingSettings settings, StyleBakeQuality quality)
        {
            var tier = s_Tiers[(int)quality];

            return settings.directSampleCount == tier.direct &&
                   settings.indirectSampleCount == tier.indirect &&
                   settings.environmentSampleCount == tier.environment &&
                   Mathf.Approximately(settings.lightProbeSampleCountMultiplier, tier.probes) &&
                   settings.maxBounces == tier.bounces &&
                   Mathf.Approximately(settings.lightmapResolution, tier.resolution) &&
                   settings.lightmapMaxSize == tier.maxSize &&
                   settings.lightmapCompression == tier.compression &&
                   settings.lightmapPadding == Padding &&
                   BlockAligned(settings);
        }
    }
}
