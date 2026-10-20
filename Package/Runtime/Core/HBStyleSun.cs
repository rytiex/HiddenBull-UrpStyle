using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Environment/HB Style Sun")]
    public sealed class HBStyleSun : MonoBehaviour
    {
        static readonly List<HBStyleSun> s_Enabled = new List<HBStyleSun>();

        [SerializeField]
        [Range(0f, 24f)]
        [Tooltip("Hour of the day. The sun rises at 6 and sets at 18; between 18 and 6 the light " +
                 "is turned to come from the moon instead, and the sky, fog and clouds follow this " +
                 "hour rather than the light's angle. Set it to the hour you want before baking.")]
        float m_TimeOfDay = 14f;

        [SerializeField]
        [Range(0f, 360f)]
        [Tooltip("Which way the sun faces, in degrees around the vertical — the light's Y rotation. " +
                 "The sun rises on one side of this line, passes overhead at noon and sets on the other.")]
        float m_Heading;

        Light m_Light;

        public static HBStyleSun active => s_Enabled.Count > 0 ? s_Enabled[s_Enabled.Count - 1] : null;

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

        public void Apply()
        {
            if (m_Light == null)
                m_Light = GetComponent<Light>();

            var sun = sunDirection;
            var angle = SunAngle(m_TimeOfDay);

            var euler = new Vector3(Mathf.Repeat(night ? angle + 180f : angle, 360f), m_Heading, 0f);

            if (transform.rotation != Quaternion.Euler(euler))
            {
                if (transform.parent == null)
                    transform.localEulerAngles = euler;
                else
                    transform.rotation = Quaternion.Euler(euler);
            }

            var stack = VolumeManager.instance.stack;
            var colour = HBStyleKeyLight.Resolve(stack?.GetComponent<HBStyleSky>(), sun.y, out var intensity);

            intensity *= 1f - HBStyleKeyLight.Overcast(stack?.GetComponent<HBStyleClouds>());

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                colour = colour.gamma;

            if (m_Light.color != colour)
                m_Light.color = colour;

            if (!Mathf.Approximately(m_Light.intensity, intensity))
                m_Light.intensity = intensity;
        }

        void OnEnable()
        {
            s_Enabled.Remove(this);
            s_Enabled.Add(this);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
            UnityEditor.EditorApplication.update += EditorTick;
#endif

            Apply();
        }

        void OnDisable()
        {
            s_Enabled.Remove(this);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
        }

#if UNITY_EDITOR
        void EditorTick()
        {
            if (!Application.isPlaying)
                Apply();
        }
#endif

        void OnValidate()
        {
            m_TimeOfDay = Mathf.Clamp(m_TimeOfDay, 0f, 24f);
            m_Heading = Mathf.Clamp(m_Heading, 0f, 360f);
        }

        void Update()
        {
            if (Application.isPlaying)
                Apply();
        }
    }
}
