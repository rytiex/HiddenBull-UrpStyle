using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("HiddenBull/HB Style/Environment/HB Style Sun")]
    public sealed class HBStyleSun : MonoBehaviour
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
        [Tooltip("Environment profile this scene uses: sky, celestial, clouds, atmosphere and weather " +
                 "defaults. Empty uses the package defaults.")]
        HBStyleEnvironmentProfile m_Profile;

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

        public static float SunAngle(float timeOfDay) => (timeOfDay - 6f) * 15f;

        public Vector3 sunDirection =>
            -(Quaternion.Euler(SunAngle(m_TimeOfDay), m_Heading, 0f) * Vector3.forward);

        public bool night => m_TimeOfDay < 6f || m_TimeOfDay > 18f;

        public void Match(Vector3 towardSun)
        {
            var travel = -towardSun.normalized;

            var pitch = Mathf.Asin(Mathf.Clamp(-travel.y, -1f, 1f)) * Mathf.Rad2Deg;

            m_TimeOfDay = Mathf.Repeat(6f + pitch / 15f, 24f);
            m_Heading = Mathf.Repeat(Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg, 360f);

            Apply();
        }

        void Apply()
        {
            var angle = SunAngle(m_TimeOfDay);
            var rotation = Quaternion.Euler(Mathf.Repeat(night ? angle + 180f : angle, 360f), m_Heading, 0f);

            if (transform.rotation != rotation)
                transform.rotation = rotation;
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

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        void Update()
        {
            Apply();
        }
    }
}
