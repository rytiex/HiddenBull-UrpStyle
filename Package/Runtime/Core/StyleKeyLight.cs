using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public static class StyleKeyLight
    {
        public const float Transition = 0.12f;

        static readonly bool s_LinearColorSpace =
            QualitySettings.activeColorSpace == ColorSpace.Linear;

        public static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float Handoff(float elevation)
        {
            return Smooth01(Mathf.Abs(elevation) / Transition);
        }

        public static Color EvaluateOverElevation(Gradient gradient, float sunElevation)
        {
            var authored = gradient.Evaluate(sunElevation * 0.5f + 0.5f);

            return s_LinearColorSpace ? authored.linear : authored;
        }

        public static Color Resolve(StyleSky sky, float sunElevation, out float intensity)
        {
            var gradient = sky != null && sky.skyLight.value != null
                ? sky.skyLight.value
                : StyleSky.DefaultSkyLight();

            var reach = sky != null ? sky.skyLightIntensity.value : 1f;

            intensity = reach * Handoff(sunElevation);

            return EvaluateOverElevation(gradient, sunElevation);
        }
    }
}
