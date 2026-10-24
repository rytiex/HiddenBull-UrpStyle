using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleEnvironment
    {
        static readonly List<HBStyleSun> s_Suns = new List<HBStyleSun>();

        static HBStyleEnvironmentProfile s_Default;
        static HBStyleEnvironmentProfile s_Override;
        static HBStyleEnvironmentProfile s_BlendFrom;
        static double s_BlendStart;
        static float s_BlendSeconds;

        static float s_TimeOfDay = 12f;

        static Vector3 s_WeatherFrom;
        static Vector3 s_WeatherTo;
        static double s_WeatherStart;
        static float s_WeatherSeconds;

        internal static event Action<HBStyleEnvironmentProfile> profileChanged;
        internal static event Action weatherChanged;

        internal static HBStyleSun sun => s_Suns.Count > 0 ? s_Suns[s_Suns.Count - 1] : null;

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
                var active = sun;

                return active != null && active.profile != null ? active.profile : defaultProfile;
            }
        }

        internal static HBStyleEnvironmentProfile targetProfile => s_Override != null ? s_Override : authoredProfile;

        internal static HBStyleEnvironmentProfile sourceProfile =>
            s_BlendFrom != null && profileBlend < 1f ? s_BlendFrom : targetProfile;

        internal static float profileBlend => Progress(s_BlendStart, s_BlendSeconds);

        internal static Vector3 weather =>
            Vector3.Lerp(s_WeatherFrom, s_WeatherTo, Progress(s_WeatherStart, s_WeatherSeconds));

        internal static float timeOfDay
        {
            get
            {
                var active = sun;

                return active != null ? active.timeOfDay : s_TimeOfDay;
            }
            set
            {
                var active = sun;

                if (active != null)
                    active.timeOfDay = value;
                else
                    s_TimeOfDay = Mathf.Repeat(value, 24f);
            }
        }

        internal static void Register(HBStyleSun sun)
        {
            s_Suns.Remove(sun);
            s_Suns.Add(sun);
        }

        internal static void Unregister(HBStyleSun sun)
        {
            s_Suns.Remove(sun);
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

            weatherChanged?.Invoke();
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
            s_WeatherFrom = Vector3.zero;
            s_WeatherTo = Vector3.zero;
            s_WeatherStart = 0.0;
            s_WeatherSeconds = 0f;
            profileChanged = null;
            weatherChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_Suns.Clear();
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
