using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleWeatherState
    {
        const float RainToSoak = 3f;
        const float MeltWetness = 0.7f;
        const float Settled = 1e-3f;
        const float LongestStep = 0.25f;

        static double s_LastClock = double.NegativeInfinity;

        internal static float wetness { get; private set; }
        internal static float snow { get; private set; }
        internal static float rain { get; private set; }

        internal static double fallClock { get; private set; }

        internal static bool instant { get; private set; } = true;

        internal static bool surfaceActive => wetness > Settled || snow > Settled;

        internal static bool falling => rain > 0f || StyleEnvironment.weather.y > 0f;

        internal static bool active => surfaceActive || falling;

        internal static void Advance()
        {
            var now = StyleEnvironment.clock;

            if (now == s_LastClock)
                return;

            var first = double.IsNegativeInfinity(s_LastClock);
            var step = first ? 0f : (float)(now - s_LastClock);
            s_LastClock = now;

            var weather = StyleEnvironment.weather;
            var settings = StyleEnvironment.targetProfile.weather;
            var rainNow = weather.x;
            var snowNow = weather.y;

            rain = rainNow;
            instant = !Application.isPlaying || settings.instant;

            if (rainNow > 0f || snowNow > 0f)
            {
                fallClock += step;
                StylePrecipitation.Advance(Mathf.Min(step, LongestStep));
            }

            if (instant)
            {
                wetness = Mathf.Clamp01(rainNow * RainToSoak);
                snow = snowNow;
                return;
            }

            if (step <= 0f)
                return;

            step = Mathf.Min(step, LongestStep) * settings.speed;

            var melting = snow > snowNow + Settled;
            var wetTarget = Mathf.Max(Mathf.Clamp01(rainNow * RainToSoak), melting ? MeltWetness : 0f);
            var soak = Mathf.Max(rainNow, melting ? 1f : 0f);

            wetness = Toward(wetness, wetTarget, step * soak / settings.wetTime, step / settings.dryTime);
            snow = Toward(snow, snowNow, step / settings.settleTime, step / settings.meltTime);
        }

        internal static void Reset()
        {
            s_LastClock = double.NegativeInfinity;
            wetness = 0f;
            snow = 0f;
            rain = 0f;
            fallClock = 0.0;
            instant = true;
            StylePrecipitation.Reset();
        }

        static float Toward(float value, float target, float rise, float fall)
        {
            return value < target ? Mathf.Min(value + rise, target) : Mathf.Max(value - fall, target);
        }
    }
}
