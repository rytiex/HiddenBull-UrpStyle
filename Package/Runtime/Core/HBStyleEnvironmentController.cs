using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("HiddenBull/HB Style/Environment/HB Style Environment Controller")]
    public sealed class HBStyleEnvironmentController : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 24f)]
        [Tooltip("Hour of the day, 0 to 24. The sun rises at 6 and sets at 18; between 18 and 6 the light " +
                 "is turned to come from the moon instead. Set it to the hour you want before baking.")]
        float m_TimeOfDay = 14f;

        [SerializeField]
        [Range(0f, 360f)]
        [Tooltip("Which way the sun faces, in degrees around the vertical (the light's Y rotation). " +
                 "The sun rises on one side of this line, passes overhead at noon and sets on the other.")]
        float m_Heading;

        [SerializeField]
        [Tooltip("Environment profile this scene uses: brush, sky, celestial, clouds, atmosphere and weather " +
                 "style. Empty uses the package defaults.")]
        HBStyleEnvironmentProfile m_Profile;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How hard it rains. Drops fall around the camera and stop at anything with something above " +
                 "it, so it does not rain indoors. Open surfaces get wet over the profile's Wet Time. 0 turns " +
                 "rain off at no cost.")]
        float m_Rain;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How hard it snows. Flakes drift down around the camera and, like rain, stop under anything " +
                 "with something above it. Open surfaces whiten over the profile's Settle Time. Rain and snow " +
                 "are set on their own, so sleet is both at once. 0 turns snow off at no cost.")]
        float m_Snow;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How hard the wind blows. It slants the rain and pushes the snow sideways; snow leans much " +
                 "further than rain at the same wind.")]
        float m_Wind = 0.2f;

        [SerializeField]
        [Range(0f, 360f)]
        [Tooltip("The compass direction the wind blows toward, in degrees: 0 is world +Z, 90 is world +X.")]
        float m_WindHeading;

        static readonly StyleProfileMix s_Mix = new StyleProfileMix();

        Light m_Light;

        public float timeOfDay
        {
            get => m_TimeOfDay;
            set
            {
                m_TimeOfDay = Mathf.Repeat(value, 24f);
                Apply();
            }
        }

        public float heading
        {
            get => m_Heading;
            set
            {
                m_Heading = Mathf.Repeat(value, 360f);
                Apply();
            }
        }

        public HBStyleEnvironmentProfile profile
        {
            get => m_Profile;
            set => m_Profile = value;
        }

        public float rain
        {
            get => m_Rain;
            set => SetWeatherValue(ref m_Rain, value);
        }

        public float snow
        {
            get => m_Snow;
            set => SetWeatherValue(ref m_Snow, value);
        }

        public float wind
        {
            get => m_Wind;
            set => SetWeatherValue(ref m_Wind, value);
        }

        public float windHeading
        {
            get => m_WindHeading;
            set => m_WindHeading = Mathf.Repeat(value, 360f);
        }

        public static float SunAngle(float timeOfDay) => (timeOfDay - 6f) * 15f;

        public Vector3 sunDirection =>
            -(Quaternion.Euler(SunAngle(m_TimeOfDay), m_Heading, 0f) * Vector3.forward);

        public bool night => m_TimeOfDay < 6f || m_TimeOfDay > 18f;

        internal Vector3 weather => new Vector3(m_Rain, m_Snow, m_Wind);

        public void Match(Vector3 towardSun)
        {
            var travel = -towardSun.normalized;

            var pitch = Mathf.Asin(Mathf.Clamp(-travel.y, -1f, 1f)) * Mathf.Rad2Deg;

            m_TimeOfDay = Mathf.Repeat(6f + pitch / 15f, 24f);
            m_Heading = Mathf.Repeat(Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg, 360f);

            Apply();
        }

        internal void ShowWeather(Vector3 value)
        {
            m_Rain = value.x;
            m_Snow = value.y;
            m_Wind = value.z;
        }

        static void SetWeatherValue(ref float field, float value)
        {
            StyleEnvironment.StopWeather();
            field = Mathf.Clamp01(value);
        }

        void Apply()
        {
            var angle = SunAngle(m_TimeOfDay);
            var rotation = Quaternion.Euler(Mathf.Repeat(night ? angle + 180f : angle, 360f), m_Heading, 0f);

            if (transform.rotation != rotation)
                transform.rotation = rotation;

            if (m_Light == null)
                m_Light = GetComponent<Light>();

            var elevation = StyleKeyLight.SunElevation(m_TimeOfDay);
            Color colour;
            float intensity;

            if (StyleEnvironment.controller == this)
            {
                var view = Camera.main;

                if (view == null)
                    view = StyleCameraStates.lastGameCamera;

                s_Mix.Evaluate(view != null ? view.transform.position : (Vector3?)null);
                StyleKeyLight.Resolve(s_Mix, elevation, out colour, out intensity);
            }
            else
            {
                var profile = m_Profile != null ? m_Profile : StyleEnvironment.defaultProfile;
                StyleKeyLight.Resolve(profile, elevation, out colour, out intensity);
            }

            if (m_Light.color != colour)
                m_Light.color = colour;

            if (!Mathf.Approximately(m_Light.intensity, intensity))
                m_Light.intensity = intensity;
        }

        void Reset()
        {
            Match(-transform.forward);
        }

        void OnEnable()
        {
            StyleEnvironment.Register(this);
            Apply();
        }

        void OnDisable()
        {
            StyleEnvironment.Unregister(this);
        }

        void OnValidate()
        {
            m_TimeOfDay = Mathf.Clamp(m_TimeOfDay, 0f, 24f);
            m_Heading = Mathf.Clamp(m_Heading, 0f, 360f);
            m_Rain = Mathf.Clamp01(m_Rain);
            m_Snow = Mathf.Clamp01(m_Snow);
            m_Wind = Mathf.Clamp01(m_Wind);
            m_WindHeading = Mathf.Clamp(m_WindHeading, 0f, 360f);

            if (StyleEnvironment.controller == this)
                StyleEnvironment.WeatherEdited(weather);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        void Update()
        {
            if (StyleEnvironment.controller == this)
                StyleEnvironment.AdvanceWeather();

            Apply();
        }
    }
}
