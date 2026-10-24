using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleCelestialSettings
    {
        [SerializeField]
        [Tooltip("Brightness of the sun's disc by day (0 to 20). Its colour is Sun Disc Color in the Sky " +
                 "section. 0 leaves a sky with no sun drawn in it.")]
        [Range(0f, 20f)]
        float m_SunIntensity = 0.85f;

        [SerializeField]
        [Tooltip("Brightness of the sun's disc by dusk (0 to 20).")]
        [Range(0f, 20f)]
        float m_SunDuskIntensity = 2f;

        [SerializeField]
        [Tooltip("Angular radius of the sun's disc by day, in degrees.")]
        [Range(0.1f, 30f)]
        float m_SunSize = 0.1f;

        [SerializeField]
        [Tooltip("Angular radius of the sun's disc by dusk, in degrees. A low sun reading larger than a high " +
                 "one is most of what makes a sunset feel like one.")]
        [Range(0.1f, 30f)]
        float m_SunDuskSize = 0.65f;

        [SerializeField]
        [Tooltip("Size and strength of the glow around the sun, on one curve (0 to 1). The glow outlives the " +
                 "disc below the horizon, which leaves an afterglow at sunset.")]
        [Range(0f, 1f)]
        float m_SunGlow = 0.75f;

        [SerializeField]
        [Tooltip("How far the brush atlas breaks up the disc edge (0 to 1).")]
        [Range(0f, 1f)]
        float m_SunBrush = 1f;

        [SerializeField]
        [Tooltip("Rays radiating from the sun (0 to 1). They are cut at the horizon with the disc.")]
        [Range(0f, 1f)]
        float m_SunRays = 0.35f;

        [SerializeField]
        [Tooltip("Brightness of the moon (0 to 20). It sits exactly opposite the sun, so it rises as the sun " +
                 "sets. 0 leaves a moonless sky.")]
        [Range(0f, 20f)]
        float m_MoonIntensity = 2.5f;

        [SerializeField]
        [Tooltip("Angular radius of the moon, in degrees.")]
        [Range(0.1f, 30f)]
        float m_MoonSize = 0.1f;

        [SerializeField]
        [Tooltip("Size and strength of the glow around the moon (0 to 1).")]
        [Range(0f, 1f)]
        float m_MoonGlow = 0.1f;

        [SerializeField]
        [Tooltip("How many stars there are and how brightly they burn, on one curve (0 to 1). They fade out " +
                 "on their own as the sun rises.")]
        [Range(0f, 1f)]
        float m_Stars = 0.75f;

        [SerializeField]
        [Tooltip("How much stars vary in brightness over time (0 to 1).")]
        [Range(0f, 1f)]
        float m_StarTwinkle = 0.4f;

        [SerializeField]
        [Tooltip("How far stars take a colour temperature, from cool blue-white to warm orange (0 white to 1).")]
        [Range(0f, 1f)]
        float m_StarColor = 0.6f;

        [SerializeField]
        [Tooltip("How much star sizes vary (0 all the same to 1 from half to double size).")]
        [Range(0f, 1f)]
        float m_StarSizeVariation = 0.5f;

        [SerializeField]
        [Tooltip("Brightness of the aurora (0 off to 4). It only shows at night and fades out as the sun rises.")]
        [Range(0f, 4f)]
        float m_Aurora;

        [SerializeField]
        [Tooltip("Colour of the aurora curtain from its lower edge (left) to its top (right).")]
        Gradient m_AuroraColor = DefaultAuroraColor();

        [SerializeField]
        [Tooltip("Direction the aurora hangs in, in degrees around the vertical (0 is world +Z). With more than " +
                 "one curtain they spread out to both sides of this direction.")]
        [Range(0f, 360f)]
        float m_AuroraHeading;

        [SerializeField]
        [Tooltip("Number of aurora curtains (1 to 4). Each one hangs at its own distance and folds on its own.")]
        [Range(1, 4)]
        int m_AuroraCount = 1;

        [SerializeField]
        [Tooltip("Size of the aurora (0.5 to 4): how long the curtain is and how widely it folds.")]
        [Range(0.5f, 4f)]
        float m_AuroraScale = 1f;

        [SerializeField]
        [Tooltip("How fast the aurora curtains ripple and drift (0 still to 2).")]
        [Range(0f, 2f)]
        float m_AuroraSpeed = 0.5f;

        [SerializeField]
        [Tooltip("How tall the curtains reach above their lower edge (0.2 to 2).")]
        [Range(0.2f, 2f)]
        float m_AuroraHeight = 0.8f;

        public float sunIntensity => m_SunIntensity;
        public float sunDuskIntensity => m_SunDuskIntensity;
        public float sunSize => m_SunSize;
        public float sunDuskSize => m_SunDuskSize;
        public float sunGlow => m_SunGlow;
        public float sunBrush => m_SunBrush;
        public float sunRays => m_SunRays;
        public float moonIntensity => m_MoonIntensity;
        public float moonSize => m_MoonSize;
        public float moonGlow => m_MoonGlow;
        public float stars => m_Stars;
        public float starTwinkle => m_StarTwinkle;
        public float starColor => m_StarColor;
        public float starSizeVariation => m_StarSizeVariation;
        public float aurora => m_Aurora;
        public Gradient auroraColor => m_AuroraColor;
        public float auroraHeading => m_AuroraHeading;
        public int auroraCount => m_AuroraCount;
        public float auroraScale => m_AuroraScale;
        public float auroraSpeed => m_AuroraSpeed;
        public float auroraHeight => m_AuroraHeight;

        public static Gradient DefaultAuroraColor()
        {
            var gradient = new Gradient();

            gradient.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.10f, 1.00f, 0.45f), 0f),
                new GradientColorKey(new Color(0.20f, 0.85f, 0.60f), 0.45f),
                new GradientColorKey(new Color(0.55f, 0.25f, 0.90f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }
    }
}
