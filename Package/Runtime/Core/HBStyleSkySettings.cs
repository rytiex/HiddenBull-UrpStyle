using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleSkySettings
    {
        [SerializeField]
        [Tooltip("The sky by day, read from straight down on the left to straight up on the right; the middle " +
                 "of the bar is the horizon. Fog and distant surfaces fade toward this same colour.")]
        Gradient m_DaySky = DefaultDaySky();

        [SerializeField]
        [Tooltip("The sky while the sun is near the horizon, read the same way as Day Sky.")]
        Gradient m_DuskSky = DefaultDuskSky();

        [SerializeField]
        [Tooltip("The sky after dark, read the same way as Day Sky.")]
        Gradient m_NightSky = DefaultNightSky();

        [SerializeField]
        [Tooltip("Overall brightness of the sky (multiplier, 0 to 4).")]
        [Range(0f, 4f)]
        float m_SkyIntensity = 1f;

        [SerializeField]
        [Tooltip("How far the brush atlas breaks up the sky so it reads as painted (0 to 1). It fades out " +
                 "toward the horizon to keep that line clean.")]
        [Range(0f, 1f)]
        float m_SkyBrush = 0.225f;

        [SerializeField]
        [Tooltip("How far the strokes repaint the sky (0 to 3). Above 1 a stroke pulls colour from further up " +
                 "and down the sky, so neighbouring strokes carry clearly different colours.")]
        [Range(0f, 3f)]
        float m_SkyPaint = 2.15f;

        [SerializeField]
        [Tooltip("Size of the sky strokes, as how many times the atlas wraps around the sky (0.5 to 12). Low " +
                 "values give broad sweeps.")]
        [Range(0.5f, 12f)]
        float m_SkyBrushScale = 0.5f;

        [SerializeField]
        [Tooltip("How soft the sky strokes are (0 to 1). Higher reads blurred copies of the atlas, so the " +
                 "marks keep their shape but lose the bristle tooth.")]
        [Range(0f, 1f)]
        float m_SkyBrushSmoothness = 0.125f;

        [SerializeField]
        [Tooltip("How tightly night arrives across the sky (0 to 1). At 0 the whole dome dims at once; higher " +
                 "values narrow a front that sweeps from the moon's side toward the sunset.")]
        [Range(0f, 1f)]
        float m_SunsetFocus = 1f;

        [SerializeField]
        [Tooltip("The light the sky casts by day, read along the surface normal: left is a surface facing " +
                 "down, right is a surface facing up. Keep it calmer and less saturated than the sky itself.")]
        Gradient m_DayAmbient = DefaultDayAmbient();

        [SerializeField]
        [Tooltip("Ambient light while the sun is near the horizon, read the same way as Day Ambient.")]
        Gradient m_DuskAmbient = DefaultDuskAmbient();

        [SerializeField]
        [Tooltip("Ambient light after dark, read the same way as Day Ambient.")]
        Gradient m_NightAmbient = DefaultNightAmbient();

        [SerializeField]
        [Tooltip("Overall strength of the ambient light (multiplier, 0 to 4).")]
        [Range(0f, 4f)]
        float m_AmbientIntensity = 1f;

        [SerializeField]
        [Tooltip("How far the ambient leans toward the main light instead of straight up (0 to 1). At 0 it " +
                 "is read purely by height; higher values give form a lit and a shaded side even where the " +
                 "direct light has saturated. It does nothing at noon and fades out as the sun reaches the " +
                 "horizon.")]
        [Range(0f, 1f)]
        float m_AmbientLightBias = 0.85f;

        [SerializeField]
        [Tooltip("Colour of the directional light, read by the sun's height: far left is midnight with the " +
                 "moon overhead, the middle is the horizon, far right is noon. The light fades out and back " +
                 "in across the sun-to-moon swap on its own, so the middle can hold any colour.")]
        Gradient m_SkyLight = DefaultSkyLight();

        [SerializeField]
        [Tooltip("Overall strength of the directional light (Light intensity, 0 to 4).")]
        [Range(0f, 4f)]
        float m_SkyLightIntensity = 1f;

        [SerializeField]
        [Tooltip("Strength of the moonlight relative to the night end of Sky Light (0 to 2).")]
        [Range(0f, 2f)]
        float m_MoonLight = 1f;

        [SerializeField]
        [Tooltip("Colour of the sun's disc, read by its own height: far right is noon, the middle is the " +
                 "horizon. Everything left of the middle is below the horizon and never drawn.")]
        Gradient m_SunDiscColor = DefaultSunDiscColor();

        [SerializeField]
        [Tooltip("Colour of the moon's disc, read by the moon's own height: far right is the moon at its " +
                 "highest, the middle is the horizon.")]
        Gradient m_MoonDiscColor = DefaultMoonDiscColor();

        [SerializeField]
        [Tooltip("Sun elevation, in degrees above the horizon, above which it is fully day. Dusk lies between " +
                 "this and Night Elevation.")]
        [Range(1f, 40f)]
        float m_DuskElevation = 30f;

        [SerializeField]
        [Tooltip("Sun elevation, in degrees, below which it is fully night. Negative means below the horizon.")]
        [Range(-30f, -1f)]
        float m_NightElevation = -8.6f;

        public Gradient daySky => m_DaySky;
        public Gradient duskSky => m_DuskSky;
        public Gradient nightSky => m_NightSky;
        public float skyIntensity => m_SkyIntensity;
        public float skyBrush => m_SkyBrush;
        public float skyPaint => m_SkyPaint;
        public float skyBrushScale => m_SkyBrushScale;
        public float skyBrushSmoothness => m_SkyBrushSmoothness;
        public float sunsetFocus => m_SunsetFocus;
        public Gradient dayAmbient => m_DayAmbient;
        public Gradient duskAmbient => m_DuskAmbient;
        public Gradient nightAmbient => m_NightAmbient;
        public float ambientIntensity => m_AmbientIntensity;
        public float ambientLightBias => m_AmbientLightBias;
        public Gradient skyLight => m_SkyLight;
        public float skyLightIntensity => m_SkyLightIntensity;
        public float moonLight => m_MoonLight;
        public Gradient sunDiscColor => m_SunDiscColor;
        public Gradient moonDiscColor => m_MoonDiscColor;
        public float duskElevation => m_DuskElevation;
        public float nightElevation => m_NightElevation;

        public static Gradient DefaultDaySky() => Make(
            Key(0.4433962f, 0.4433962f, 0.4433962f, 0f),
            Key(0.8584906f, 0.8584906f, 0.8584906f, 0.45f),
            Key(0.95f, 0.96f, 0.98f, 0.50f),
            Key(0.55f, 0.72f, 0.93f, 0.70f),
            Key(0.28f, 0.52f, 0.88f, 1f));

        public static Gradient DefaultDuskSky() => Make(
            Key(0.10f, 0.07f, 0.09f, 0f),
            Key(0.22f, 0.13f, 0.16f, 0.44f),
            Key(1.00f, 0.44f, 0.19f, 0.50f),
            Key(0.85f, 0.35f, 0.30f, 0.68f),
            Key(0.3356989f, 0.2784314f, 0.4509804f, 0.85f),
            Key(0.22f, 0.20f, 0.42f, 1f));

        public static Gradient DefaultNightSky() => Make(
            Key(0.0200000f, 0.0250000f, 0.0500000f, 0f),
            Key(0.0400000f, 0.0500000f, 0.0900000f, 0.46f),
            Key(0.1000000f, 0.1200000f, 0.2000000f, 0.52f),
            Key(0.0020470f, 0.0054112f, 0.0188679f, 1f));

        public static Gradient DefaultDayAmbient() => Make(
            Key(0.2735849f, 0.2735849f, 0.2735849f, 0f),
            Key(0.5606978f, 0.5606978f, 0.6792453f, 0.1147f),
            Key(0.7436943f, 0.7451354f, 0.8431187f, 0.2294f),
            Key(0.8155037f, 0.8505028f, 0.9245283f, 0.50f),
            Key(1.0000000f, 0.9519084f, 0.8915094f, 1f));

        public static Gradient DefaultDuskAmbient() => Make(
            Key(0.2735849f, 0.1185056f, 0.0916251f, 0f),
            Key(0.5000000f, 0.3004838f, 0.2004717f, 0.1029f),
            Key(0.7547170f, 0.4336543f, 0.3666786f, 0.50f),
            Key(0.8962264f, 0.7226003f, 0.5115255f, 0.7735f),
            Key(0.9703549f, 0.8426444f, 0.6886137f, 0.8823f),
            Key(1.0000000f, 1.0000000f, 1.0000000f, 1f));

        public static Gradient DefaultNightAmbient() => Make(
            Key(0.0200000f, 0.0250000f, 0.0500000f, 0f),
            Key(0.0400000f, 0.0500000f, 0.0900000f, 0.46f),
            Key(0.1000000f, 0.1200000f, 0.2000000f, 0.52f),
            Key(0.2276166f, 0.2276166f, 0.3113208f, 1f));

        public static Gradient DefaultSkyLight() => Make(
            Key(0.2321000f, 0.2527000f, 0.3133000f, 0f),
            Key(0.2321000f, 0.2527000f, 0.3133000f, 0.48f),
            Key(0.4433962f, 0.2087761f, 0.1024831f, 0.50f),
            Key(1.0000000f, 0.6414817f, 0.1650943f, 0.60f),
            Key(1.0000000f, 0.7120591f, 0.4481132f, 0.65f),
            Key(1.0000000f, 1.0000000f, 1.0000000f, 0.7412f),
            Key(1.0000000f, 1.0000000f, 1.0000000f, 1f));

        public static Gradient DefaultSunDiscColor() => Make(
            Key(0.0000f, 0.0000000f, 0.0000f, 0.49f),
            Key(1.0000f, 0.7593924f, 0.0000f, 0.50f),
            Key(1.0000f, 0.9535000f, 0.9190f, 0.65f),
            Key(1.0000f, 0.9535000f, 0.9190f, 1f));

        public static Gradient DefaultMoonDiscColor() => Make(
            Key(1.0000f, 0.8500000f, 0.7000000f, 0f),
            Key(1.0000f, 0.9138691f, 0.8254717f, 0.52f),
            Key(0.9063f, 0.9309000f, 1.0000000f, 0.65f),
            Key(0.9063f, 0.9309000f, 1.0000000f, 1f));

        static readonly GradientAlphaKey[] s_Opaque =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        static GradientColorKey Key(float r, float g, float b, float position)
        {
            return new GradientColorKey(new Color(r, g, b), position);
        }

        static Gradient Make(params GradientColorKey[] keys)
        {
            var gradient = new Gradient();
            gradient.SetKeys(keys, s_Opaque);
            return gradient;
        }
    }
}
