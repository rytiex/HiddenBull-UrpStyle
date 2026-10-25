using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HB Style/Weather/HB Style Weather Body")]
    public sealed class HBStyleWeatherBody : MonoBehaviour
    {
        const uint Tracked = 0x80000000u;
        const float Levels = 127f;
        const int SnowShift = 7;
        const int ClockShift = 14;
        const uint ClockMask = 0x1FFFFu;
        const double ClockRate = 32.0;
        const float CoverReach = 0.7f;

        [SerializeField]
        [Tooltip("Where the body checks for cover, in metres from this transform. Place it at the top of the " +
                 "object; the body counts as covered when a roof is at least 0.7 m above this point.")]
        Vector3 m_ProbeOffset;

        [SerializeField]
        [Tooltip("Turn the offset with this transform. Leave off for an FPS camera or hands so looking up and " +
                 "down does not move the point; turn on for vehicles and props that tip over.")]
        bool m_FollowRotation;

        readonly List<Renderer> m_Renderers = new List<Renderer>();

        float m_Wetness;
        float m_Snow;
        float m_Exposure = 1f;
        double m_Clock;
        uint m_Written;

        public float wetness => m_Wetness;

        public float snow => m_Snow;

        public float exposure => m_Exposure;

        internal int rendererCount => m_Renderers.Count;

        public Vector3 probe =>
            transform.position + (m_FollowRotation ? transform.rotation * m_ProbeOffset : m_ProbeOffset);

        void OnEnable()
        {
            Collect();
            StyleWeatherBodies.Register(this);
        }

        void OnDisable()
        {
            StyleWeatherBodies.Unregister(this);
            Write(0u);
        }

        void OnTransformChildrenChanged()
        {
            Write(0u);
            Collect();
        }

        void Collect()
        {
            m_Renderers.Clear();
            GetComponentsInChildren(true, m_Renderers);
            m_Written = 0u;
        }

        void Update()
        {
            StyleWeatherBodies.Tick();

            var settings = StyleEnvironment.targetProfile.weather;

            m_Exposure = StyleWeatherBodies.Exposure(probe);

            var wetTarget = StyleWeatherState.wetness * m_Exposure;
            var snowTarget = StyleWeatherState.snow * m_Exposure;

            if (StyleWeatherState.falling)
                m_Clock += Time.deltaTime * m_Exposure;

            var step = Time.deltaTime * settings.speed;

            m_Wetness = Toward(m_Wetness, wetTarget, step / settings.uncoverTime, step / settings.dryTime);
            m_Snow = Toward(m_Snow, snowTarget, step / settings.uncoverTime, step / settings.meltTime);

            var wet = (uint)Mathf.RoundToInt(m_Wetness * Levels);
            var flakes = (uint)Mathf.RoundToInt(m_Snow * Levels);
            var clock = (uint)((long)(m_Clock * ClockRate) & ClockMask);

            Write(Tracked | wet | (flakes << SnowShift) | (clock << ClockShift));
        }

        void OnDrawGizmosSelected()
        {
            var point = probe;

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 1f);
            Gizmos.DrawWireSphere(point, 0.05f);
            Gizmos.DrawLine(point, point + Vector3.up * CoverReach);
        }

        void Write(uint value)
        {
            if (value == m_Written)
                return;

            m_Written = value;

            foreach (var renderer in m_Renderers)
            {
                if (renderer == null)
                    continue;

                switch (renderer)
                {
                    case MeshRenderer mesh:
                        mesh.SetShaderUserValue(value);
                        break;
                    case SkinnedMeshRenderer skinned:
                        skinned.SetShaderUserValue(value);
                        break;
                }
            }
        }

        static float Toward(float value, float target, float rise, float fall)
        {
            return value < target ? Mathf.Min(value + rise, target) : Mathf.Max(value - fall, target);
        }
    }
}
