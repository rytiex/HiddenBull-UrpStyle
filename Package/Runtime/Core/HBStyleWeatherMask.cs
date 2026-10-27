using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleWeatherMaskTransition
    {
        Instant,
        Gradual
    }

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Weather/HB Style Weather Mask")]
    public sealed class HBStyleWeatherMask : MonoBehaviour
    {
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Radius in metres of the dome that keeps rain, snow and wetness off everything under it, as an " +
                 "invisible roof would. The dome is a sphere around this transform; it counts within 80 m of the " +
                 "camera, nearest 16 masks.")]
        float m_Radius = 3f;

        [SerializeField]
        [Tooltip("Also stops falling rain and snow at the dome. Turn it off to keep them falling while the " +
                 "surfaces under the dome stay dry, onto a camp fire for instance.")]
        bool m_BlockPrecipitation = true;

        [SerializeField]
        [Tooltip("Instant: the area is dry the moment the mask turns on and covered the moment it turns off, " +
                 "like under a roof. Gradual: in Play mode the dome grows from its centre over Clear Time and " +
                 "shrinks over Cover Time, also when the object is deleted. Outside Play mode it is instant.")]
        HBStyleWeatherMaskTransition m_Transition = HBStyleWeatherMaskTransition.Instant;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Gradual only: seconds for the dome to grow from its centre to the full radius once the mask " +
                 "turns on.")]
        float m_ClearTime = 15f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Gradual only: seconds for the dome to shrink back to nothing once the mask turns off or the " +
                 "object is deleted.")]
        float m_CoverTime = 30f;

        internal float open;

        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, 0.1f);
        }

        public bool blockPrecipitation
        {
            get => m_BlockPrecipitation;
            set => m_BlockPrecipitation = value;
        }

        public HBStyleWeatherMaskTransition transition
        {
            get => m_Transition;
            set => m_Transition = value;
        }

        public float clearTime
        {
            get => m_ClearTime;
            set => m_ClearTime = Mathf.Max(value, 0f);
        }

        public float coverTime
        {
            get => m_CoverTime;
            set => m_CoverTime = Mathf.Max(value, 0f);
        }

        public float grown => open;

        void OnEnable()
        {
            StyleWeatherMasks.Register(this);
        }

        void OnDisable()
        {
            StyleWeatherMasks.Release(this);
        }

        void OnDestroy()
        {
            StyleWeatherMasks.Detach(this);
        }

        void OnDrawGizmosSelected()
        {
            var position = transform.position;

            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(position, m_Radius);

            if (open > 0f && open < 1f)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
                Gizmos.DrawWireSphere(position, m_Radius * open);
            }
        }
    }
}
