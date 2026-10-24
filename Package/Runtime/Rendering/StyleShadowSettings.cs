using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    static class StyleShadowSettings
    {
        static UniversalRenderPipelineAsset s_Asset;
        static HBStyleQualityLevel s_Level;
        static HBStyleShadowLevel s_Applied;

#if UNITY_EDITOR
        static bool s_EditorPending;
#endif

        internal static void Apply(UniversalRenderPipelineAsset asset, HBStyleShadowQuality quality)
        {
            if (asset == null || quality == null)
                return;

            var current = quality.current;

            if (asset == s_Asset && quality.level == s_Level && Same(current, s_Applied))
                return;

            s_Asset = asset;
            s_Level = quality.level;
            s_Applied = current;

            if (quality.level == HBStyleQualityLevel.Off)
            {
                asset.shadowDistance = 0f;
                return;
            }

            asset.shadowDistance = current.distance;
            asset.shadowCascadeCount = Mathf.Clamp(current.cascades, 1, 4);
            asset.mainLightShadowmapResolution = (int)current.resolution;
            asset.shadowDepthBias = 1f;
            asset.shadowNormalBias = 1f;
            asset.cascadeBorder = Border(quality.level);

            if (current.cascades == 2)
                asset.cascade2Split = 0.2f;
            else if (current.cascades == 4)
                asset.cascade4Split = Splits(quality.level);

#if UNITY_EDITOR
            WriteEditorOnly(asset, quality.level);
#endif
        }

        internal static Vector2 AtlasTile(int cascades)
        {
            return cascades switch
            {
                <= 1 => new Vector2(1f, 1f),
                2 => new Vector2(0.5f, 1f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        static bool Same(HBStyleShadowLevel a, HBStyleShadowLevel b)
        {
            return a.distance == b.distance && a.cascades == b.cascades && a.resolution == b.resolution;
        }

        static float Border(HBStyleQualityLevel level)
        {
            return level switch
            {
                HBStyleQualityLevel.High => 0.3f,
                HBStyleQualityLevel.Ultra => 0.35f,
                _ => 0.25f
            };
        }

        static Vector3 Splits(HBStyleQualityLevel level)
        {
            return level == HBStyleQualityLevel.Ultra
                ? new Vector3(0.04f, 0.11f, 0.28f)
                : new Vector3(0.05f, 0.14f, 0.34f);
        }

#if UNITY_EDITOR
        static void WriteEditorOnly(UniversalRenderPipelineAsset asset, HBStyleQualityLevel level)
        {
            if (s_EditorPending)
                return;

            s_EditorPending = true;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                s_EditorPending = false;

                if (asset == null)
                    return;

                var target = new UnityEditor.SerializedObject(asset);
                var changed = Set(target, "m_MainLightShadowsSupported", true);
                changed |= Set(target, "m_SoftShadowsSupported", level != HBStyleQualityLevel.Low);
                changed |= Set(target, "m_SoftShadowQuality", level switch
                {
                    HBStyleQualityLevel.High => 2,
                    HBStyleQualityLevel.Ultra => 3,
                    _ => 1
                });

                if (changed)
                    target.ApplyModifiedProperties();
            };
        }

        static bool Set(UnityEditor.SerializedObject target, string path, bool value)
        {
            var property = target.FindProperty(path);

            if (property == null || property.boolValue == value)
                return false;

            property.boolValue = value;
            return true;
        }

        static bool Set(UnityEditor.SerializedObject target, string path, int value)
        {
            var property = target.FindProperty(path);

            if (property == null || property.intValue == value)
                return false;

            property.intValue = value;
            return true;
        }
#endif
    }
}
