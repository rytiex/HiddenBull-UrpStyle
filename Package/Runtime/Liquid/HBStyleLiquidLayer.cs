using UnityEngine;

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
        float m_Tiling = 0.125f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How strongly the small ripples bend the surface.")]
        float m_NormalStrength = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How sharp the reflections and the sun's highlight are.")]
        float m_Smoothness = 0.65f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Blurs what is seen through the surface, on top of the blur murky liquid already adds with " +
                 "depth.")]
        float m_RefractionBlur = 0.145f;

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
        [Tooltip("How much the liquid shines in its own colour, day and night, like acid or lava. Raise the " +
                 "gradient's intensity above 1 for a glow strong enough to bloom.")]
        float m_Glow;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("The net of sunlight the ripples throw onto whatever lies under the liquid. 0 turns it off.")]
        float m_Caustics = 0.05f;

        [SerializeField]
        [Range(0.25f, 4f)]
        [Tooltip("Size of the cells in the net of light. 1 is about a metre across.")]
        float m_CausticScale = 0.65f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the light that shows through the tops of the waves, strongest toward the sun.")]
        Color m_CrestColor = new Color(0.7075472f, 0.7075472f, 0.7075472f, 1f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How gradually the crest colour fades down the wave: 0 a sharp cap, 1 most of the wave.")]
        float m_CrestSoftness = 0.45f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the waterline, shore foam and white caps.")]
        Color m_FoamColor = new Color(0.76495194f, 0.8158059f, 0.8490566f, 1f);

        [SerializeField]
        [Range(0f, 3f)]
        [Tooltip("Depth of liquid, in metres, over which a thin line of foam hugs the shore and anything " +
                 "standing in it.")]
        float m_WaterlineDepth = 0.1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strong all the foam is: waterline, shore foam and white caps. 0 turns it off.")]
        float m_FoamAmount = 0.3f;

        [SerializeField]
        [Range(0f, 30f)]
        [Tooltip("How far from the coast, in metres, a loose band of foam reaches. 0 turns it off.")]
        float m_ShoreFoam = 2.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How many bubbles swell and burst across the surface, gathering along the shore.")]
        float m_Bubbles;

        [SerializeField]
        [Range(0f, 8f)]
        [Tooltip("How quickly bubbles swell and burst and how fast the glow under a crust throbs.")]
        float m_Pulse = 1f;

        [SerializeField]
        [Range(0.1f, 8f)]
        [Tooltip("Size of the foam cells, the sun glints, and the bubble and crust patches.")]
        float m_PatternScale = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of the surface a solid, drifting crust covers, as on lava. 0 turns it off.")]
        float m_Crust;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the crust.")]
        Color m_CrustColor = Color.black;

        [SerializeField]
        [Tooltip("Its brightness decides where the crust forms. Leave it empty for crust from the pattern alone.")]
        Texture2D m_CrustMap;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How softly light wraps round foam and crust.")]
        float m_DiffuseWrap = 0.125f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("Width of each step between the tones of foam and crust.")]
        float m_DiffuseSoftness = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Beams of sunlight seen from under the surface. 0 turns them off.")]
        float m_LightShafts = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Small bubbles drifting up past the camera while it is under the surface.")]
        float m_UnderBubbles = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the view sways while the camera is under the surface.")]
        float m_Distortion = 0.3f;

        [System.NonSerialized]
        int m_Version;

        public bool windWaves => m_WindWaves;

        public float waves => m_Waves;

        public float waveHeight => m_WaveHeight;

        public float waveSteepness => m_WaveSteepness;

        public float waveLength => m_WaveLength;

        public float tiling => Mathf.Max(m_Tiling, 0.01f);

        public float normalStrength => m_NormalStrength;

        public float smoothness => m_Smoothness;

        public float refractionBlur => m_RefractionBlur;

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

        public Texture2D crustMap => m_CrustMap;

        public float diffuseWrap => m_DiffuseWrap;

        public float diffuseSoftness => m_DiffuseSoftness;

        public float lightShafts => m_LightShafts;

        public float underBubbles => m_UnderBubbles;

        public float distortion => m_Distortion;

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

            m_Tiling = lava ? 0.25f : acid ? 0.45f : 0.125f;
            m_Waves = 0.2f;
            m_WindWaves = !lava;
            m_WaveHeight = lava ? 1.65f : 1f;
            m_WaveSteepness = lava ? 2f : 1f;
            m_WaveLength = lava ? 0.45f : 1f;
            m_NormalStrength = water ? 0.25f : 2f;
            m_Smoothness = lava ? 0.575f : 0.65f;
            m_RefractionBlur = water ? 0.145f : 0f;
            m_DiffuseWrap = lava ? 0.175f : 0.125f;
            m_DiffuseSoftness = lava ? 2f : 0.25f;
            m_Caustics = lava ? 0f : acid ? 0.085f : 0.05f;
            m_Glow = water ? 0f : 1f;
            m_CausticScale = water ? 0.65f : 0.25f;
            m_CrestSoftness = water ? 0.45f : 0f;
            m_WaterlineDepth = lava ? 3f : acid ? 0.45f : 0.1f;
            m_FoamAmount = lava ? 1f : 0.3f;
            m_ShoreFoam = lava ? 0f : 2.5f;
            m_Bubbles = lava ? 0.0225f : acid ? 0.125f : 0f;
            m_Pulse = lava ? 0.125f : acid ? 1.5f : 1f;
            m_PatternScale = acid ? 3f : 1f;
            m_Crust = lava ? 0.865f : 0f;
            m_LightShafts = lava ? 1f : acid ? 0.4f : 0.5f;
            m_UnderBubbles = lava ? 0f : acid ? 0.65f : 0.25f;
            m_Distortion = lava ? 1f : 0.3f;
            m_RainRipples = true;

            m_CrestColor = lava ? new Color(0.44339618f, 0.44339618f, 0.44339618f, 1f)
                         : acid ? new Color(0.7546641f, 1f, 0.3820755f, 1f)
                         : new Color(0.7075472f, 0.7075472f, 0.7075472f, 1f);
            m_FoamColor = lava ? new Color(1f, 0.69112843f, 0f, 1f)
                        : acid ? new Color(0.69319016f, 1f, 0.06132078f, 1f)
                        : new Color(0.76495194f, 0.8158059f, 0.8490566f, 1f);
            m_CrustColor = lava ? new Color(0.16981131f, 0.07429245f, 0f, 1f) : Color.black;
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
                        new GradientColorKey(new Color(0.1509434f, 0.024598183f, 0f), 1f)
                    }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                    break;

                case Preset.Acid:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.1648759f, 0.7176471f, 0f), 0f),
                        new GradientColorKey(new Color(0.055667076f, 0.34117648f, 0f), 0.30000764f),
                        new GradientColorKey(new Color(0.2601031f, 0.4245283f, 0f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7538165f, 0.6500038f),
                        new GradientAlphaKey(1f, 1f)
                    });
                    break;

                default:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0f, 0.7176471f, 0.6846459f), 0f),
                        new GradientColorKey(new Color(0f, 0.21511805f, 0.33962262f), 0.30000764f),
                        new GradientColorKey(new Color(0f, 0.0031320888f, 0.028301895f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.1254902f, 0.28824294f),
                        new GradientAlphaKey(0.2509804f, 0.57941556f), new GradientAlphaKey(0.7538165f, 0.8f),
                        new GradientAlphaKey(1f, 1f)
                    });
                    break;
            }

            return gradient;
        }
    }
}
