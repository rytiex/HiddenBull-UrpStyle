using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public static class HBStyleKeyLight
    {
        public const float Transition = 0.12f;

        const float OvercastStart = 0.55f;
        const float OvercastFull = 0.95f;

        public static float Overcast(HBStyleClouds clouds)
        {
            if (clouds == null)
                return 0f;

            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(OvercastStart, OvercastFull, clouds.coverage.value));
        }

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

        public static float Share(HBStyleSky sky, float sunElevation)
        {
            if (sunElevation >= 0f)
                return 1f;

            return sky != null ? sky.moonLight.value : 1f;
        }

        public static Color Resolve(HBStyleSky sky, float sunElevation, out float intensity)
        {
            var gradient = sky != null && sky.skyLight.value != null
                ? sky.skyLight.value
                : HBStyleSky.DefaultSkyLight();

            var reach = sky != null ? sky.skyLightIntensity.value : 1f;

            intensity = reach * Handoff(sunElevation) * Share(sky, sunElevation);

            return EvaluateOverElevation(gradient, sunElevation);
        }
    }
}
