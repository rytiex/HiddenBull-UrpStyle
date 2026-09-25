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

    public enum StyleShadowDebug
    {
        Off,
        Attenuation,
        UnfilteredShadow,
        BlockerCount,
        CascadeIndex,
        FilterValues,
        SearchRadius
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
        [Tooltip("Replaces the lit colour with one term of the shadow calculation, so a shadow that " +
                 "looks wrong can be traced to the step that made it wrong instead of guessed at.\n\n" +
                 "Filter Values is the one to reach for first: it draws the three numbers this block " +
                 "sends to the shader. Black there means the settings never arrived, which looks " +
                 "exactly like a broken shadow but is not one.")]
        StyleShadowDebug m_Debug = StyleShadowDebug.Off;

        [NonSerialized]
        int m_Applied = -1;

        [NonSerialized]
        bool m_Pending;

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

                Apply();
            }
        }

        public float distance
        {
            get => m_Distance;
            set
            {
                var range = DistanceRange(m_Quality);
                m_Distance = Mathf.Clamp(value, range.x, range.y);

                Apply();
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

        public StyleShadowDebug debug
        {
            get => m_Debug;
            set => m_Debug = value;
        }

        public Vector4 PackFilter()
        {
            return new Vector4(m_Softness, m_Contact, Taps(m_Quality), 0f);
        }

        public Vector4 PackDebug()
        {
            return new Vector4((int)m_Debug, 0f, 0f, 0f);
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

        public void ApplyDeferred()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (m_Pending)
                    return;

                m_Pending = true;

                UnityEditor.EditorApplication.delayCall += () =>
                {
                    m_Pending = false;
                    Apply();
                };

                return;
            }
#endif

            Apply();
        }

        public void Apply()
        {
            var limits = DistanceRange(m_Quality);
            m_Distance = Mathf.Clamp(m_Distance, limits.x, limits.y);

            var signature = (int)m_Quality * 397 ^ Mathf.RoundToInt(m_Distance * 16f);

            if (signature == m_Applied)
                return;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset)
                return;

            var cascades = Cascades(m_Quality);
            var resolution = Resolution(m_Quality);
            var border = Border(m_Quality);

            if (!Mathf.Approximately(asset.shadowDistance, m_Distance))
                asset.shadowDistance = m_Distance;

            if (asset.shadowCascadeCount != cascades)
                asset.shadowCascadeCount = cascades;

            if (asset.mainLightShadowmapResolution != resolution)
                asset.mainLightShadowmapResolution = resolution;

            if (!Mathf.Approximately(asset.shadowDepthBias, 1f))
                asset.shadowDepthBias = 1f;

            if (!Mathf.Approximately(asset.shadowNormalBias, 1f))
                asset.shadowNormalBias = 1f;

            if (!Mathf.Approximately(asset.cascadeBorder, border))
                asset.cascadeBorder = border;

            if (m_Quality == StyleShadowQuality.Medium)
            {
                if (!Mathf.Approximately(asset.cascade2Split, 0.2f))
                    asset.cascade2Split = 0.2f;
            }
            else if (cascades == 4)
            {
                var splits = Splits(m_Quality);

                if (asset.cascade4Split != splits)
                    asset.cascade4Split = splits;
            }

#if UNITY_EDITOR
            Write(asset);
#endif

            m_Applied = signature;
        }

        public static int Resolution(StyleShadowQuality quality)
        {
            return quality switch
            {
                StyleShadowQuality.Low => 1024,
                StyleShadowQuality.Ultra => 4096,
                _ => 2048
            };
        }

        public static float Border(StyleShadowQuality quality)
        {
            return quality switch
            {
                StyleShadowQuality.Low => 0.25f,
                StyleShadowQuality.Medium => 0.25f,
                StyleShadowQuality.High => 0.3f,
                _ => 0.35f
            };
        }

        public static Vector3 Splits(StyleShadowQuality quality)
        {
            return quality == StyleShadowQuality.Ultra
                ? new Vector3(0.04f, 0.11f, 0.28f)
                : new Vector3(0.05f, 0.14f, 0.34f);
        }

#if UNITY_EDITOR
        void Write(UniversalRenderPipelineAsset asset)
        {
            var target = new UnityEditor.SerializedObject(asset);

            var changed = Set(target, "m_MainLightShadowsSupported", true);

            changed |= Set(target, "m_SoftShadowsSupported", m_Quality != StyleShadowQuality.Low);

            changed |= Set(target, "m_SoftShadowQuality", m_Quality switch
            {
                StyleShadowQuality.High => 2,
                StyleShadowQuality.Ultra => 3,
                _ => 1
            });

            if (changed)
                target.ApplyModifiedProperties();
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
