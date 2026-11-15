using UnityEngine;
using UnityEngine.Serialization;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Liquid Layer", order = 0,
                     menuName = "HiddenBull/URP Style/Liquid/Liquid Layer")]
    public sealed class HBStyleLiquidLayer : ScriptableObject
    {
        public enum Preset
        {
            Water,
            Acid,
            Lava
        }

        [SerializeField]
        [Tooltip("Takes the height of the waves from the wind instead of Waves: flat at no wind, tallest in a " +
                 "storm. While on, white caps follow the wind too.")]
        bool m_WindWaves = true;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How tall the waves are while Wind Affects Waves is off. High values break white caps.")]
        float m_Waves = 0.2f;

        [SerializeField]
        [Range(0f, 3f)]
        [Tooltip("Scales how tall the waves stand without changing the wind or Waves. 1 is the natural height.")]
        float m_WaveHeight = 1f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How sharp the crests are: 0 soft rolling swell, 1 the natural shape, higher pinches them into " +
                 "peaks.")]
        float m_WaveSteepness = 1f;

        [SerializeField]
        [Range(0.25f, 4f)]
        [Tooltip("How far apart the waves are: high for long ocean swell, low for short choppy water.")]
        float m_WaveLength = 1f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("How many times the surface patterns repeat per metre.")]
        float m_Tiling = 1f;

        [SerializeField]
        [Tooltip("Normal map of the small ripples. It repeats by Tiling, drifts with the wind and follows the " +
                 "flow painted on the terrain liquid. Leave it empty for a surface shaped by the waves alone.")]
        Texture2D m_NormalMap;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How strongly the small ripples of the Normal Map bend the surface.")]
        float m_NormalStrength = 0.125f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How fast the small ripples, the crust and the brush strokes drift with the wind. 0 holds them " +
                 "still; painted flow still moves them.")]
        float m_RippleDrift = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How sharp the reflections and the sun's highlight are.")]
        float m_Smoothness = 0.8f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the ripples shift what is seen through the surface, and the sky seen from below it. " +
                 "0 turns it off.")]
        [FormerlySerializedAs("m_Refraction")]
        float m_SurfaceDistortion = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Blurs what is seen through the surface, on top of the blur murky liquid already adds with " +
                 "depth.")]
        float m_RefractionBlur = 0.05f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the brush strokes break up the light and the reflections on the surface. The " +
                 "strokes drift with the current and the wind. 0 turns them off.")]
        float m_Brush = 0.2f;

        [SerializeField]
        [Range(0.1f, 4f)]
        [Tooltip("How large the brush strokes are on the surface. 1 matches the stroke size of the Brush profile.")]
        float m_BrushSize = 2f;

        [SerializeField]
        [Tooltip("Rings spread across the surface wherever rain falls on it, never under a roof.")]
        bool m_RainRipples = true;

        [SerializeField]
        [GradientUsage(true)]
        [Tooltip("The liquid by how much of it you look through, from the shore on the left to deep on the " +
                 "right. Colour is the liquid's colour there; alpha is how much it hides the bottom. The middle " +
                 "is about three metres. Up to six colour and six alpha keys are used.")]
        Gradient m_DepthGradient = DepthGradient(Preset.Water);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the liquid shines in its own colour, day and night, like acid or lava. It also lights " +
                 "what lies in it, so it brightens without hiding the bottom. Raise the gradient's intensity " +
                 "above 1 for a glow strong enough to bloom.")]
        float m_Glow;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("The net of sunlight the ripples throw onto whatever lies under the liquid. 0 turns it off.")]
        float m_Caustics = 0.125f;

        [SerializeField]
        [Range(0.25f, 4f)]
        [Tooltip("Size of the cells in the net of light. 1 is about a metre across.")]
        float m_CausticScale = 0.25f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the light that shows through the tops of the waves, strongest toward the sun.")]
        Color m_CrestColor = new Color(0.5566038f, 0.5566038f, 0.5566038f, 1f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How gradually the crest colour fades down the wave: 0 a sharp cap, 1 most of the wave.")]
        float m_CrestSoftness = 0.225f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the waterline, shore foam and white caps.")]
        Color m_FoamColor = new Color(0.8396226f, 0.8396226f, 0.8396226f, 1f);

        [SerializeField]
        [Range(0f, 3f)]
        [Tooltip("Depth of liquid, in metres, over which a thin line of foam hugs the shore and anything " +
                 "standing in it.")]
        float m_WaterlineDepth = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of the surface the foam covers: waterline, shore foam and white caps. 0 turns it off.")]
        float m_FoamAmount = 0.5f;

        [SerializeField]
        [Range(0f, 30f)]
        [Tooltip("How far from the coast, in metres, a loose band of foam reaches. 0 turns it off.")]
        float m_ShoreFoam = 0.75f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How many bubbles swell and burst across the surface, gathering along the shore.")]
        float m_Bubbles;

        [SerializeField]
        [Range(0f, 8f)]
        [Tooltip("How quickly bubbles swell and burst and how fast the glow under a crust throbs.")]
        float m_Pulse = 0f;

        [SerializeField]
        [Range(0.1f, 8f)]
        [Tooltip("Size of the foam cells, the sun glints, the bubbles and the crust patches.")]
        float m_PatternScale = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of each plate in the normal map cools into crust, as on lava, while its cracks keep " +
                 "glowing. Foam and glow stay under the crust. 0 turns it off.")]
        float m_Crust;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("What the crust does to the liquid's colour and glow. Dark cools it to rock; white leaves it as " +
                 "it is.")]
        Color m_CrustColor = Color.black;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How softly light wraps round the foam.")]
        float m_DiffuseWrap = 0.125f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("Width of each step between the tones of the foam.")]
        float m_DiffuseSoftness = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Beams of sunlight seen from under the surface. 0 turns them off.")]
        float m_LightShafts = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Small bubbles drifting up past the camera while it is under the surface.")]
        float m_UnderBubbles = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the view sways while the camera is under the surface.")]
        float m_Distortion = 0.3f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How clearly the sky shows through the bright window above the camera while it is under the " +
                 "surface. 1 is fully clear, 0.5 half, 0 closes the window.")]
        float m_SnellWindow = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How blurry things look while the camera is under the surface. Near things, the hands included, " +
                 "soften a little and far things blur fully. 0 keeps everything sharp.")]
        float m_UnderBlur = 0.25f;

        [SerializeField]
        [Range(0.25f, 4f)]
        [Tooltip("How far you see while the camera is under the surface. 1 follows the Depth Gradient, 2 lets " +
                 "you see twice as far, 0.5 half as far.")]
        float m_UnderVisibility = 0.65f;

        [SerializeField]
        [Min(0.5f)]
        [Tooltip("Depth in metres at which the light under the surface has faded to about a third. Lower values " +
                 "make deep water darker sooner.")]
        float m_UnderLightDepth = 6f;

        [System.NonSerialized]
        int m_Version;

        public bool windWaves => m_WindWaves;

        public float waves => m_Waves;

        public float waveHeight => m_WaveHeight;

        public float waveSteepness => m_WaveSteepness;

        public float waveLength => m_WaveLength;

        public float tiling => Mathf.Max(m_Tiling, 0.01f);

        public Texture2D normalMap => m_NormalMap;

        public float normalStrength => m_NormalStrength;

        public float rippleDrift => m_RippleDrift;

        public float smoothness => m_Smoothness;

        public float surfaceDistortion => m_SurfaceDistortion;

        public float refractionBlur => m_RefractionBlur;

        public float brush => m_Brush;

        public float brushSize => m_BrushSize;

        public bool rainRipples => m_RainRipples;

        public Gradient depthGradient => m_DepthGradient ??= DepthGradient(Preset.Water);

        public float glow => m_Glow;

        public float caustics => m_Caustics;

        public float causticScale => m_CausticScale;

        public Color crestColor => m_CrestColor;

        public float crestSoftness => m_CrestSoftness;

        public Color foamColor => m_FoamColor;

        public float waterlineDepth => m_WaterlineDepth;

        public float foamAmount => m_FoamAmount;

        public float shoreFoam => m_ShoreFoam;

        public float bubbles => m_Bubbles;

        public float pulse => m_Pulse;

        public float patternScale => m_PatternScale;

        public float crust => m_Crust;

        public Color crustColor => m_CrustColor;

        public float diffuseWrap => m_DiffuseWrap;

        public float diffuseSoftness => m_DiffuseSoftness;

        public float lightShafts => m_LightShafts;

        public float underBubbles => m_UnderBubbles;

        public float distortion => m_Distortion;

        public float snellWindow => m_SnellWindow;

        public float underBlur => m_UnderBlur;

        public float underVisibility => m_UnderVisibility;

        public float underLightDepth => Mathf.Max(m_UnderLightDepth, 0.5f);

        internal int version => m_Version;

        public Color shallowColor
        {
            get
            {
                var colour = depthGradient.Evaluate(0f);

                return new Color(colour.r, colour.g, colour.b, 1f);
            }
        }

        public void Apply(Preset preset)
        {
            var water = preset == Preset.Water;
            var acid = preset == Preset.Acid;
            var lava = preset == Preset.Lava;

            m_Tiling = lava ? 0.435f : acid ? 0.145f : 1f;
            m_Waves = 0.2f;
            m_WindWaves = water;
            m_WaveHeight = lava ? 1.65f : 1f;
            m_WaveSteepness = lava ? 2f : 1f;
            m_WaveLength = lava ? 0.45f : 1f;
            m_NormalStrength = lava ? 2f : acid ? 1f : 0.125f;
            m_RippleDrift = lava ? 0f : 1f;
            m_Smoothness = lava ? 0.25f : acid ? 0.65f : 0.8f;
            m_SurfaceDistortion = lava ? 0f : 1f;
            m_RefractionBlur = lava ? 0f : acid ? 0.125f : 0.05f;
            m_Brush = water ? 0.2f : 0.5f;
            m_BrushSize = water ? 2f : 1f;
            m_DiffuseWrap = lava ? 0.246f : 0.125f;
            m_DiffuseSoftness = lava ? 2f : 0.25f;
            m_Caustics = lava ? 0f : acid ? 0.085f : 0.125f;
            m_Glow = water ? 0f : 1f;
            m_CausticScale = 0.25f;
            m_CrestSoftness = water ? 0.225f : 0f;
            m_WaterlineDepth = water ? 1f : 3f;
            m_FoamAmount = water ? 0.5f : 1f;
            m_ShoreFoam = lava ? 0f : acid ? 2.5f : 0.75f;
            m_Bubbles = lava ? 0.0225f : acid ? 0.125f : 0f;
            m_Pulse = lava ? 0.125f : acid ? 1.5f : 0f;
            m_PatternScale = water ? 0.5f : 3f;
            m_Crust = lava ? 0.222f : 0f;
            m_LightShafts = acid ? 0.4f : 1f;
            m_UnderBubbles = lava ? 0f : acid ? 1f : 0.25f;
            m_Distortion = lava ? 1f : 0.3f;
            m_SnellWindow = lava ? 0f : acid ? 0.525f : 1f;
            m_UnderBlur = water ? 0.25f : 0.5f;
            m_UnderVisibility = lava ? 0.75f : acid ? 2f : 0.65f;
            m_UnderLightDepth = lava ? 2f : acid ? 18f : 6f;
            m_RainRipples = true;

            m_CrestColor = lava ? new Color(0.44339618f, 0.44339618f, 0.44339618f, 1f)
                         : acid ? new Color(0.7546641f, 1f, 0.3820755f, 1f)
                         : new Color(0.5566038f, 0.5566038f, 0.5566038f, 1f);
            m_FoamColor = lava ? new Color(1f, 0.69112843f, 0f, 1f)
                        : acid ? Color.white
                        : new Color(0.8396226f, 0.8396226f, 0.8396226f, 1f);
            m_CrustColor = Color.black;
            m_DepthGradient = DepthGradient(preset);
            m_Version++;
        }

        void OnValidate()
        {
            m_Version++;
        }

        static Gradient DepthGradient(Preset preset)
        {
            var gradient = new Gradient();

            switch (preset)
            {
                case Preset.Lava:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(4f, 0.565445f, 0f), 0f),
                        new GradientColorKey(new Color(3.5584905f, 0.5034067f, 0f), 0.114702f),
                        new GradientColorKey(new Color(0.2830189f, 0.04468719f, 0f), 1f)
                    }, new[] { new GradientAlphaKey(0.47058824f, 0f), new GradientAlphaKey(1f, 1f) });
                    break;

                case Preset.Acid:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.69479233f, 0.7176471f, 0f), 0f),
                        new GradientColorKey(new Color(0.12174808f, 0.34117648f, 0f), 0.682353f),
                        new GradientColorKey(new Color(0.2601031f, 0.4245283f, 0f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0.75f, 0.50588f),
                        new GradientAlphaKey(0.7764706f, 0.85f)
                    });
                    break;

                default:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.21586053f, 0.26225072f, 0.33245158f), 0f),
                        new GradientColorKey(new Color(0.038204376f, 0.06662595f, 0.09084173f), 0.30000764f),
                        new GradientColorKey(new Color(0.013702081f, 0.045186214f, 0.05448028f), 0.66764f),
                        new GradientColorKey(new Color(0.010329823f, 0.04091521f, 0.048171833f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.3137255f, 0.25f),
                        new GradientAlphaKey(0.7f, 0.5f),
                        new GradientAlphaKey(0.92f, 0.75f), new GradientAlphaKey(1f, 1f)
                    });
                    break;
            }

            return gradient;
        }
    }
}
