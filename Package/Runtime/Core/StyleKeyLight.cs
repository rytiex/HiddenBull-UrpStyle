using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleKeyLight
    {
        internal const float Transition = 0.12f;

        const float OvercastStart = 0.55f;
        const float OvercastFull = 0.95f;

        static Gradient s_DefaultSkyLight;
        static Gradient s_DefaultSunDisc;
        static Gradient s_DefaultMoonDisc;

        internal static Gradient SkyLight(HBStyleSkySettings sky) =>
            sky.skyLight ?? (s_DefaultSkyLight ??= HBStyleSkySettings.DefaultSkyLight());

        internal static Gradient SunDisc(HBStyleSkySettings sky) =>
            sky.sunDiscColor ?? (s_DefaultSunDisc ??= HBStyleSkySettings.DefaultSunDiscColor());

        internal static Gradient MoonDisc(HBStyleSkySettings sky) =>
            sky.moonDiscColor ?? (s_DefaultMoonDisc ??= HBStyleSkySettings.DefaultMoonDiscColor());

        internal static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);

            return t * t * (3f - 2f * t);
        }

        internal static float SunElevation(float timeOfDay)
        {
            return Mathf.Sin(HBStyleSun.SunAngle(timeOfDay) * Mathf.Deg2Rad);
        }

        internal static float Handoff(float elevation)
        {
            return Smooth01(Mathf.Abs(elevation) / Transition);
        }

        internal static float Share(HBStyleSkySettings sky, float elevation)
        {
            return elevation >= 0f ? 1f : sky.moonLight;
        }

        internal static float Overcast(HBStyleCloudSettings clouds)
        {
            if (clouds.atlas == null)
                return 0f;

            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(OvercastStart, OvercastFull, clouds.coverage));
        }

        internal static float DayBlend(HBStyleSkySettings sky, float elevation)
        {
            var night = Mathf.Sin(sky.nightElevation * Mathf.Deg2Rad);
            var dusk = Mathf.Max(Mathf.Sin(sky.duskElevation * Mathf.Deg2Rad), night + 0.01f);

            return 1f - Mathf.Clamp01(Mathf.InverseLerp(night, dusk, elevation));
        }

        internal static Color OverElevation(Gradient gradient, float elevation)
        {
            return gradient.Evaluate(elevation * 0.5f + 0.5f);
        }

        internal static Color Linear(Color authored)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? authored.linear : authored;
        }

        internal static void Resolve(StyleProfileMix mix, float elevation, out Color colour, out float intensity)
        {
            var start = mix.blend < 1f ? mix.source : mix.target;

            Resolve(start, elevation, out colour, out intensity);

            if (mix.blend < 1f)
                Toward(mix.target, mix.blend, elevation, ref colour, ref intensity);

            for (var i = 0; i < mix.zones.Count; i++)
            {
                var zone = mix.zones[i];

                if ((zone.sections & (HBStyleProfileSections.Sky | HBStyleProfileSections.Clouds)) != 0)
                    Toward(zone.profile, zone.weight, elevation, ref colour, ref intensity);
            }
        }

        static void Toward(HBStyleEnvironmentProfile profile, float weight, float elevation, ref Color colour,
                           ref float intensity)
        {
            Resolve(profile, elevation, out var otherColour, out var otherIntensity);

            colour = Color.LerpUnclamped(colour, otherColour, weight);
            intensity = Mathf.LerpUnclamped(intensity, otherIntensity, weight);
        }

        internal static void Resolve(HBStyleEnvironmentProfile profile, float elevation, out Color colour,
                                     out float intensity)
        {
            var sky = profile.sky;

            colour = OverElevation(SkyLight(sky), elevation);
            intensity = sky.skyLightIntensity * Handoff(elevation) * Share(sky, elevation)
                      * (1f - Overcast(profile.clouds));
        }
    }
}
