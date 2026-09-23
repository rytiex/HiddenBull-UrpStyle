using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    public enum StyleShadowQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    [Serializable]
    public class ShadowQualitySettings
    {
        [SerializeField]
        [Tooltip("Writes a set of shadow settings into the URP asset in one go. URP ships with a " +
                 "single cascade, soft shadows off and no distance fade, which is what gives the " +
                 "hard blocky edge and the hard cut where shadows stop. These presets spend the " +
                 "same texels where they are seen instead: several cascades packed toward the " +
                 "camera, soft filtering on, and a fade at the far end so shadows dissolve rather " +
                 "than end.\n\n" +
                 "This is the value to expose in a quality menu. Everything else follows from it.")]
        StyleShadowQuality m_Quality = StyleShadowQuality.High;

        [SerializeField]
        [Tooltip("How far shadows reach, in metres. Its range follows the preset, because the two " +
                 "are the same trade in different clothes: a fixed count of texels stretched over " +
                 "more ground is a coarser shadow. Asking Low for two hundred metres would just " +
                 "undo the preset, so the slider does not offer it.")]
        float m_Distance = 80f;

        [SerializeField]
        [Tooltip("The widest a shadow edge is allowed to spread, in metres. 0 hands filtering back " +
                 "to URP, which gives every edge the same width wherever it falls.")]
        [Range(0f, 0.225f)]
        float m_Softness = 0.225f;

        [SerializeField]
        [Tooltip("How far a caster has to stand off a surface, in metres, before its shadow reaches " +
                 "full Softness. This is what makes a shadow read as touching the ground: right at " +
                 "the contact point there is no gap, so the edge stays sharp, and it opens up as the " +
                 "object lifts away.\n\n" +
                 "Small values spread the edge almost immediately. 0 turns it off and gives one width " +
                 "everywhere, which is what a plain filter does and why shadows float.")]
        [Range(0f, 4.5f)]
        float m_Contact = 4.5f;

        [SerializeField]
        [Tooltip("How far the brush drags the shadow edge, in metres. It does not blur the edge, it " +
                 "reshapes it: the line stops following the texel grid and starts following a " +
                 "stroke, which is what stops a shadow reading as a rendered edge.\n\n" +
                 "Metres rather than texels, so a stroke is the same size wherever it lands. A near " +
                 "cascade texel is a couple of millimetres across, so counting in texels would make " +
                 "the effect vanish exactly where it is looked at most.\n\n" +
                 "Needs a brush atlas on the Brush block.")]
        [Range(0f, 1f)]
        float m_Brush = 0.1f;

        [SerializeField]
        [Tooltip("World size of one stroke, in metres, measured on the plane facing the light. Small " +
                 "values give a fine tooth along the edge, large ones a broad sweep.")]
        [Min(0.05f)]
        float m_BrushSize = 0.5f;

        [SerializeField]
        [HideInInspector]
        int m_Applied = -1;

        public static Vector2 DistanceRange(StyleShadowQuality quality)
        {
            return quality switch
            {
                StyleShadowQuality.Low => new Vector2(10f, 25f),
                StyleShadowQuality.Medium => new Vector2(15f, 60f),
                StyleShadowQuality.High => new Vector2(20f, 120f),
                _ => new Vector2(30f, 200f)
            };
        }

        public static float DefaultDistance(StyleShadowQuality quality)
        {
            return quality switch
            {
                StyleShadowQuality.Low => 20f,
                StyleShadowQuality.Medium => 45f,
                StyleShadowQuality.High => 80f,
                _ => 150f
            };
        }

        public StyleShadowQuality quality
        {
            get => m_Quality;
            set
            {
                m_Quality = value;
                m_Distance = DefaultDistance(value);
            }
        }

        public float distance
        {
            get => m_Distance;
            set
            {
                var range = DistanceRange(m_Quality);
                m_Distance = Mathf.Clamp(value, range.x, range.y);
            }
        }

        public float brush
        {
            get => m_Brush;
            set => m_Brush = Mathf.Clamp(value, 0f, 1f);
        }

        public float softness
        {
            get => m_Softness;
            set => m_Softness = Mathf.Clamp(value, 0f, 0.225f);
        }

        public float contact
        {
            get => m_Contact;
            set => m_Contact = Mathf.Clamp(value, 0f, 4.5f);
        }

        public static int Cascades(StyleShadowQuality quality)
        {
            return quality switch
            {
                StyleShadowQuality.Low => 1,
                StyleShadowQuality.Medium => 2,
                _ => 4
            };
        }

        public static Vector2 AtlasTile(StyleShadowQuality quality)
        {
            return Cascades(quality) switch
            {
                1 => new Vector2(1f, 1f),
                2 => new Vector2(0.5f, 1f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        public static int Taps(StyleShadowQuality quality)
        {
            return quality switch
            {
                StyleShadowQuality.Low => 4,
                StyleShadowQuality.Medium => 8,
                StyleShadowQuality.High => 12,
                _ => 16
            };
        }

        public Vector4 PackFilter()
        {
            return new Vector4(m_Softness, m_Contact, Taps(m_Quality), 0f);
        }

        public Vector4 PackBrush(bool atlasBound)
        {
            var tile = AtlasTile(m_Quality);

            return new Vector4(
                atlasBound ? m_Brush : 0f,
                1f / Mathf.Max(0.05f, m_BrushSize),
                tile.x,
                tile.y);
        }

        public void Apply()
        {
#if UNITY_EDITOR
            var limits = DistanceRange(m_Quality);
            m_Distance = Mathf.Clamp(m_Distance, limits.x, limits.y);

            var signature = (int)m_Quality * 397 ^ Mathf.RoundToInt(m_Distance * 16f);

            if (signature == m_Applied)
                return;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset)
                return;

            Write(asset);

            m_Applied = signature;
#endif
        }

#if UNITY_EDITOR
        void Write(UniversalRenderPipelineAsset asset)
        {
            var target = new UnityEditor.SerializedObject(asset);

            Set(target, "m_MainLightShadowsSupported", true);
            Set(target, "m_ShadowDistance", m_Distance);
            Set(target, "m_ShadowDepthBias", 1f);
            Set(target, "m_ShadowNormalBias", 1f);
            Set(target, "m_ShadowCascadeCount", Cascades(m_Quality));

            switch (m_Quality)
            {
                case StyleShadowQuality.Low:
                    Set(target, "m_MainLightShadowmapResolution", 1024);
                    Set(target, "m_CascadeBorder", 0.25f);
                    Set(target, "m_SoftShadowsSupported", false);
                    Set(target, "m_SoftShadowQuality", 1);
                    break;

                case StyleShadowQuality.Medium:
                    Set(target, "m_MainLightShadowmapResolution", 2048);
                    Set(target, "m_Cascade2Split", 0.2f);
                    Set(target, "m_CascadeBorder", 0.25f);
                    Set(target, "m_SoftShadowsSupported", true);
                    Set(target, "m_SoftShadowQuality", 1);
                    break;

                case StyleShadowQuality.High:
                    Set(target, "m_MainLightShadowmapResolution", 2048);
                    Set(target, "m_Cascade4Split", new Vector3(0.05f, 0.14f, 0.34f));
                    Set(target, "m_CascadeBorder", 0.3f);
                    Set(target, "m_SoftShadowsSupported", true);
                    Set(target, "m_SoftShadowQuality", 2);
                    break;

                case StyleShadowQuality.Ultra:
                    Set(target, "m_MainLightShadowmapResolution", 4096);
                    Set(target, "m_Cascade4Split", new Vector3(0.04f, 0.11f, 0.28f));
                    Set(target, "m_CascadeBorder", 0.35f);
                    Set(target, "m_SoftShadowsSupported", true);
                    Set(target, "m_SoftShadowQuality", 3);
                    break;
            }

            target.ApplyModifiedProperties();
        }

        static void Set(UnityEditor.SerializedObject target, string path, bool value)
        {
            var property = target.FindProperty(path);

            if (property != null)
                property.boolValue = value;
        }

        static void Set(UnityEditor.SerializedObject target, string path, int value)
        {
            var property = target.FindProperty(path);

            if (property != null)
                property.intValue = value;
        }

        static void Set(UnityEditor.SerializedObject target, string path, float value)
        {
            var property = target.FindProperty(path);

            if (property != null)
                property.floatValue = value;
        }

        static void Set(UnityEditor.SerializedObject target, string path, Vector3 value)
        {
            var property = target.FindProperty(path);

            if (property != null)
                property.vector3Value = value;
        }
#endif
    }
}
