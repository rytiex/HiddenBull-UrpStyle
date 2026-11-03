using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HB Style/Weather/HB Style Weather Body")]
    public sealed class HBStyleWeatherBody : MonoBehaviour
    {
        const uint Tracked = 0x80000000u;
        const double ClockWrap = 65536.0;
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
        double m_RainClock;
        double m_SnowClock;
        int m_Slot = -1;
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
            m_Slot = StyleWeatherBodies.Register(this);
        }

        void OnDisable()
        {
            StyleWeatherBodies.Unregister(this, m_Slot);
            m_Slot = -1;
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

            var falling = StyleEnvironment.weather;

            if (falling.x > 0f)
                m_RainClock += Time.deltaTime * m_Exposure;

            if (falling.y > 0f)
                m_SnowClock += Time.deltaTime * m_Exposure;

            var step = Time.deltaTime * settings.speed;

            m_Wetness = Toward(m_Wetness, wetTarget, step / settings.uncoverTime, step / settings.dryTime);
            m_Snow = Toward(m_Snow, snowTarget, step / settings.uncoverTime, step / settings.meltTime);

            StyleWeatherBodies.Store(m_Slot, new Vector4(m_Wetness, m_Snow, (float)(m_RainClock % ClockWrap),
                                                           (float)(m_SnowClock % ClockWrap)));
            Write(m_Slot >= 0 ? Tracked | (uint)m_Slot : 0u);
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
