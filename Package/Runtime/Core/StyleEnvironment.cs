using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleEnvironment
    {
        static readonly List<HBStyleEnvironmentController> s_Controllers = new List<HBStyleEnvironmentController>();

        static HBStyleEnvironmentProfile s_Default;
        static HBStyleEnvironmentProfile s_Override;
        static HBStyleEnvironmentProfile s_BlendFrom;
        static double s_BlendStart;
        static float s_BlendSeconds;

        static float s_TimeOfDay = 12f;

        static Vector3 s_Weather = new Vector3(0f, 0f, 0.2f);
        static Vector3 s_WeatherFrom;
        static Vector3 s_WeatherTo;
        static double s_WeatherStart;
        static float s_WeatherSeconds;
        static bool s_WeatherMoving;
        static Vector3 s_WeatherShown;

        internal static event Action<HBStyleEnvironmentProfile> profileChanged;
        internal static event Action weatherChanged;

        internal static HBStyleEnvironmentController controller =>
            s_Controllers.Count > 0 ? s_Controllers[s_Controllers.Count - 1] : null;

        internal static double clock => Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;

        internal static HBStyleEnvironmentProfile defaultProfile
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<HBStyleEnvironmentProfile>();
                    s_Default.name = "HB Style Default Environment";
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Default;
            }
        }

        internal static HBStyleEnvironmentProfile authoredProfile
        {
            get
            {
                var active = controller;

                return active != null && active.profile != null ? active.profile : defaultProfile;
            }
        }

        internal static HBStyleEnvironmentProfile targetProfile => s_Override != null ? s_Override : authoredProfile;

        internal static HBStyleEnvironmentProfile sourceProfile =>
            s_BlendFrom != null && profileBlend < 1f ? s_BlendFrom : targetProfile;

        internal static float profileBlend => Progress(s_BlendStart, s_BlendSeconds);

        internal static Vector3 weather
        {
            get
            {
                if (s_WeatherMoving)
                    return Vector3.Lerp(s_WeatherFrom, s_WeatherTo, Progress(s_WeatherStart, s_WeatherSeconds));

                var active = controller;

                return active != null ? active.weather : s_Weather;
            }
        }

        internal static float windHeading
        {
            get
            {
                var active = controller;

                return active != null ? active.windHeading : 0f;
            }
        }

        internal static float timeOfDay
        {
            get
            {
                var active = controller;

                return active != null ? active.timeOfDay : s_TimeOfDay;
            }
            set
            {
                var active = controller;

                if (active != null)
                    active.timeOfDay = value;
                else
                    s_TimeOfDay = Mathf.Repeat(value, 24f);
            }
        }

        internal static void Register(HBStyleEnvironmentController controller)
        {
            s_Controllers.Remove(controller);
            s_Controllers.Add(controller);
        }

        internal static void Unregister(HBStyleEnvironmentController controller)
        {
            s_Controllers.Remove(controller);
        }

        internal static void BlendTo(HBStyleEnvironmentProfile profile, float seconds)
        {
            s_BlendFrom = targetProfile;
            s_Override = profile;
            s_BlendStart = clock;
            s_BlendSeconds = Mathf.Max(seconds, 0f);

            profileChanged?.Invoke(targetProfile);
        }

        internal static void SetWeather(float rain, float snow, float wind, float seconds)
        {
            s_WeatherFrom = weather;
            s_WeatherTo = new Vector3(Mathf.Clamp01(rain), Mathf.Clamp01(snow), Mathf.Clamp01(wind));
            s_WeatherStart = clock;
            s_WeatherSeconds = Mathf.Max(seconds, 0f);
            s_WeatherMoving = true;

            AdvanceWeather();

            weatherChanged?.Invoke();
        }

        internal static void AdvanceWeather()
        {
            if (!s_WeatherMoving)
                return;

            var progress = Progress(s_WeatherStart, s_WeatherSeconds);
            var value = Vector3.Lerp(s_WeatherFrom, s_WeatherTo, progress);
            var active = controller;

            s_WeatherShown = value;

            if (active != null)
                active.ShowWeather(value);
            else
                s_Weather = value;

            if (progress >= 1f)
                s_WeatherMoving = false;
        }

        internal static void StopWeather()
        {
            s_WeatherMoving = false;
        }

        internal static void WeatherEdited(Vector3 value)
        {
            if (s_WeatherMoving && value != s_WeatherShown)
                s_WeatherMoving = false;
        }

        static float Progress(double start, float seconds)
        {
            if (seconds <= 0f)
                return 1f;

            return Mathf.Clamp01((float)((clock - start) / seconds));
        }

        static void ResetRuntime()
        {
            s_Override = null;
            s_BlendFrom = null;
            s_BlendStart = 0.0;
            s_BlendSeconds = 0f;
            s_TimeOfDay = 12f;
            s_Weather = new Vector3(0f, 0f, 0.2f);
            s_WeatherFrom = Vector3.zero;
            s_WeatherTo = Vector3.zero;
            s_WeatherStart = 0.0;
            s_WeatherSeconds = 0f;
            s_WeatherMoving = false;
            profileChanged = null;
            weatherChanged = null;
            StyleWeatherState.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_Controllers.Clear();
            ResetRuntime();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                ResetRuntime();
        }
#endif
    }
}
