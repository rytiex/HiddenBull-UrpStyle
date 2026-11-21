using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleWeatherMaskTransition
    {
        Instant,
        Gradual
    }

    public enum HBStyleWeatherMaskShape
    {
        Sphere,
        Box
    }

    public enum HBStyleWeatherMaskAffects
    {
        Both,
        Static,
        Moving
    }

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Weather/HB Style Weather Mask")]
    public sealed class HBStyleWeatherMask : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Sphere is a dome around this transform. Box is a flat roof the size of Size that follows this " +
                 "object's position, turn and scale, for a car or a canopy.")]
        HBStyleWeatherMaskShape m_Shape = HBStyleWeatherMaskShape.Sphere;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Radius in metres of the dome that keeps rain, snow and wetness off everything under it, as an " +
                 "invisible roof would. The dome is a sphere around this transform; it counts within 80 m of the " +
                 "camera, nearest 16 masks.")]
        float m_Radius = 3f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Box only: width, height and length in metres before scale, along this object's X, Y and Z axes. " +
                 "Everything under the top of the box stays dry.")]
        Vector3 m_Size = new Vector3(2f, 1.5f, 4.5f);

        [SerializeField]
        [Tooltip("Also stops falling rain and snow at the dome. Turn it off to keep them falling while the " +
                 "surfaces under the dome stay dry, onto a camp fire for instance.")]
        bool m_BlockPrecipitation = true;

        [SerializeField]
        [Tooltip("Which surfaces the mask keeps dry: Static surfaces such as buildings and ground, Moving surfaces " +
                 "such as vehicles and characters, or both. Falling rain and snow follow Block Precipitation.")]
        HBStyleWeatherMaskAffects m_Affects = HBStyleWeatherMaskAffects.Both;

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

        public HBStyleWeatherMaskShape shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        public Vector3 size
        {
            get => m_Size;
            set => m_Size = Vector3.Max(value, Vector3.one * 0.1f);
        }

        internal Vector3 scaledSize
        {
            get
            {
                var scale = transform.lossyScale;

                return new Vector3(m_Size.x * Mathf.Abs(scale.x), m_Size.y * Mathf.Abs(scale.y),
                                   m_Size.z * Mathf.Abs(scale.z));
            }
        }

        public bool blockPrecipitation
        {
            get => m_BlockPrecipitation;
            set => m_BlockPrecipitation = value;
        }

        public HBStyleWeatherMaskAffects affects
        {
            get => m_Affects;
            set => m_Affects = value;
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
            var box = m_Shape == HBStyleWeatherMaskShape.Box;

            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.8f);

            if (box)
            {
                Gizmos.matrix = Matrix4x4.TRS(position, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, scaledSize);
            }
            else
            {
                Gizmos.DrawWireSphere(position, m_Radius);
            }

            if (open > 0f && open < 1f)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);

                if (box)
                    Gizmos.DrawWireCube(Vector3.zero, scaledSize * open);
                else
                    Gizmos.DrawWireSphere(position, m_Radius * open);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
