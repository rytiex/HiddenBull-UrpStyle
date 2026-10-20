using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleWeatherState
    {
        const float WetTime = 30f;
        const float DryTime = 15f;
        const float SettleTime = 15f;
        const float MeltTime = 30f;
        const float RainToSoak = 3f;
        const float MeltWetness = 0.7f;
        const float Settled = 1e-3f;

        static int s_Frame = -1;

        public static float wetness { get; private set; }
        public static float snow { get; private set; }
        public static float rain { get; private set; }
        public static float swell { get; private set; }

        public static bool surfaceActive => wetness > Settled || snow > Settled;

        public static void Advance(HBStyleWeather weather)
        {
            var rainNow = weather != null ? weather.rain.value : 0f;
            var snowNow = weather != null ? weather.snow.value : 0f;
            var speed = weather != null ? weather.speed.value : 1f;

            rain = rainNow;

            var swellNow = weather != null ? weather.wind.value : 0f;

            var melting = snow > snowNow + Settled;
            var wetTarget = Mathf.Max(Mathf.Clamp01(rainNow * RainToSoak), melting ? MeltWetness : 0f);

            var instant = weather != null && weather.instant.value;

            if (!Application.isPlaying || instant)
            {
                wetness = Mathf.Clamp01(rainNow * RainToSoak);
                snow = snowNow;
                swell = swellNow;
                return;
            }

            if (Time.frameCount == s_Frame)
                return;

            s_Frame = Time.frameCount;

            var step = Time.deltaTime * speed;
            var soak = Mathf.Max(rainNow, melting ? 1f : 0f);

            wetness = Toward(wetness, wetTarget,
                             step * soak / (weather != null ? weather.wetTime.value : WetTime),
                             step / (weather != null ? weather.dryTime.value : DryTime));

            snow = Toward(snow, snowNow,
                          step / (weather != null ? weather.settleTime.value : SettleTime),
                          step / (weather != null ? weather.meltTime.value : MeltTime));

            swell = Toward(swell, swellNow,
                           step / (weather != null ? weather.coverTime.value : 1f),
                           step / (weather != null ? weather.uncoverTime.value : 1f));
        }

        static float Toward(float value, float target, float rise, float fall)
        {
            return value < target ? Mathf.Min(value + rise, target) : Mathf.Max(value - fall, target);
        }
    }
}
