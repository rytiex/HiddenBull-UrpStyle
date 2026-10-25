using System;

namespace HiddenBull.UrpStyle
{
    public static class HBStyleEnvironment
    {
        public static event Action<HBStyleEnvironmentProfile> profileChanged
        {
            add => StyleEnvironment.profileChanged += value;
            remove => StyleEnvironment.profileChanged -= value;
        }

        public static event Action weatherChanged
        {
            add => StyleEnvironment.weatherChanged += value;
            remove => StyleEnvironment.weatherChanged -= value;
        }

        public static HBStyleEnvironmentController controller => StyleEnvironment.controller;

        public static HBStyleEnvironmentProfile profile => StyleEnvironment.targetProfile;

        public static float profileBlend => StyleEnvironment.profileBlend;

        public static float timeOfDay
        {
            get => StyleEnvironment.timeOfDay;
            set => StyleEnvironment.timeOfDay = value;
        }

        public static float rain => StyleEnvironment.weather.x;

        public static float snow => StyleEnvironment.weather.y;

        public static float wind => StyleEnvironment.weather.z;

        public static float wetness => StyleWeatherState.wetness;

        public static float snowCover => StyleWeatherState.snow;

        public static void SetProfile(HBStyleEnvironmentProfile profile) => StyleEnvironment.BlendTo(profile, 0f);

        public static void BlendTo(HBStyleEnvironmentProfile profile, float seconds) =>
            StyleEnvironment.BlendTo(profile, seconds);

        public static void SetWeather(float rain, float snow, float wind, float seconds) =>
            StyleEnvironment.SetWeather(rain, snow, wind, seconds);
    }
}
