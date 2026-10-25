using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StylePrecipitation
    {
        internal const float Box = 30f;
        internal const int MaxRainDrops = 40000;
        internal const int MaxSnowFlakes = 30000;
        internal const int MaxWindRibbons = 12;
        internal const int WindVertices = 20 * 6;
        internal const int FarVertices = 2 * 64 * 6;
        internal const float FarRainShare = 0.6f;
        internal const float FarSnowShare = 0.5f;

        const float RainFall = 9f;
        const float RainTiltDegrees = 30f;
        const float SnowFall = 1f;
        const float SnowDrift = 2f;
        const double FarWrap = 60.0;
        const float WindStart = 0.3f;
        const float WindSpeed = 12f;

        static readonly float s_RainTilt = Mathf.Tan(RainTiltDegrees * Mathf.Deg2Rad);

        internal static Vector3 rainOffset { get; private set; }

        internal static Vector3 snowOffset { get; private set; }

        internal static Vector3 rainVelocity => Velocity(RainFall, RainFall * s_RainTilt);

        internal static Vector3 snowVelocity => Velocity(SnowFall, SnowDrift);

        internal static float rainScroll => (float)(StyleWeatherState.fallClock * RainFall % FarWrap);

        internal static float snowScroll => (float)(StyleWeatherState.fallClock * SnowFall % FarWrap);

        internal static float windShare => Mathf.Clamp01((StyleEnvironment.weather.z - WindStart) / (1f - WindStart));

        internal static Vector4 windMotion
        {
            get
            {
                var heading = StyleEnvironment.windHeading * Mathf.Deg2Rad;

                return new Vector4(Mathf.Sin(heading), 0f, Mathf.Cos(heading), 0f);
            }
        }

        internal static float windSpeed => StyleEnvironment.weather.z * WindSpeed;

        internal static void Advance(float step)
        {
            rainOffset = Wrap(rainOffset + rainVelocity * step);
            snowOffset = Wrap(snowOffset + snowVelocity * step);
        }

        internal static void Reset()
        {
            rainOffset = Vector3.zero;
            snowOffset = Vector3.zero;
        }

        static Vector3 Velocity(float fall, float drift)
        {
            var heading = StyleEnvironment.windHeading * Mathf.Deg2Rad;
            var wind = StyleEnvironment.weather.z * drift;

            return new Vector3(Mathf.Sin(heading) * wind, -fall, Mathf.Cos(heading) * wind);
        }

        static Vector3 Wrap(Vector3 offset)
        {
            return new Vector3(Mathf.Repeat(offset.x, Box), Mathf.Repeat(offset.y, Box), Mathf.Repeat(offset.z, Box));
        }
    }
}
