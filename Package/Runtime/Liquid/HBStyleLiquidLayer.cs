using UnityEngine;
using UnityEngine.Serialization;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Liquid Layer", order = 0,
                     menuName = "HiddenBull/URP Style/Liquid/Liquid Layer")]
    public sealed class HBStyleLiquidLayer : ScriptableObject
    {
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
                 "still; painted flow still moves them. With a Mask Map the bumps move with Motion instead, " +
                 "together with the rocks.")]
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
        Gradient m_DepthGradient = DepthGradient();

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
        [Tooltip("How quickly bubbles swell and burst.")]
        float m_Pulse = 0f;

        [SerializeField]
        [Range(0.1f, 8f)]
        [Tooltip("Size of the foam cells, the sun glints and the bubbles.")]
        float m_PatternScale = 0.5f;

        [SerializeField]
        [Tooltip("Mask map saved inside the layer by the Texture Studio. Red picks the colour along Depth Gradient, " +
                 "green marks floating rocks, blue marks lines and foam. Without it the liquid is coloured by " +
                 "depth alone.")]
        Texture2D m_MaskMap;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the Tone of the Mask Map picks the colour along Depth Gradient instead of depth. 1 " +
                 "paints the liquid fully from the mask, as on lava. 0 colours it by depth alone.")]
        float m_ToneAmount;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the lines and foam of the Mask Map show in Line Color.")]
        float m_Lines;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the lines and foam of the Mask Map. It is lit like foam.")]
        Color m_LineColor = new Color(0.92f, 0.98f, 1f);

        [SerializeField]
        [ColorUsage(false)]
        [FormerlySerializedAs("m_CrustColor")]
        [Tooltip("Colour of the floating rocks of the Mask Map. Rocks are lit like stone.")]
        Color m_RockColor = new Color(0.26f, 0.17f, 0.18f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How brightly the liquid glows in a thin ring around each rock, and how much that glow lights " +
                 "the rock's edge. Needs Glow.")]
        float m_RockEdge = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How glossy the rocks are. Low gives dull, matte rock next to the " +
                 "shiny liquid.")]
        float m_RockSmoothness = 0.2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How fast the Mask Map drifts across the surface. Rocks drift with it.")]
        float m_FlowSpeed = 0.2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the colours and lines of the Mask Map curl and sway as they drift. Rocks keep their " +
                 "shape.")]
        float m_FlowSwirl = 0.3f;

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

        public Gradient depthGradient => m_DepthGradient ??= DepthGradient();

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

        public Texture2D maskMap => m_MaskMap;

        public float toneAmount => m_ToneAmount;

        public float lines => m_Lines;

        public Color lineColor => m_LineColor;

        public Color rockColor => m_RockColor;

        public float rockEdge => m_RockEdge;

        public float rockSmoothness => m_RockSmoothness;

        public float flowSpeed => m_FlowSpeed;

        public float flowSwirl => m_FlowSwirl;

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

        void OnValidate()
        {
            m_Version++;
        }

        internal void Touch()
        {
            m_Version++;
        }

        static Gradient DepthGradient()
        {
            var gradient = new Gradient();

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

            return gradient;
        }
    }
}
