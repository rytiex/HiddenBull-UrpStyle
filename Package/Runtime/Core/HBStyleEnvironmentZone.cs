using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Environment/HB Style Environment Zone")]
    public sealed class HBStyleEnvironmentZone : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Environment profile this zone blends toward while the camera is inside it.")]
        HBStyleEnvironmentProfile m_Profile;

        [SerializeField]
        [Tooltip("Which sections of the profile the zone takes over. The rest keeps following the scene's " +
                 "profile, so a cave can change only its Atmosphere and Sky ambient.")]
        HBStyleProfileSections m_Sections = HBStyleProfileSections.All;

        [SerializeField]
        [Tooltip("Size of the box, in metres, in this object's rotation. The object's scale multiplies it.")]
        Vector3 m_Size = new Vector3(10f, 10f, 10f);

        [SerializeField]
        [Tooltip("Distance outside the box, in metres, over which the zone fades in as the camera approaches.")]
        [Min(0f)]
        float m_BlendDistance = 5f;

        [SerializeField]
        [Tooltip("Zones with a higher priority are applied on top of lower ones where they overlap.")]
        int m_Priority;

        public HBStyleEnvironmentProfile profile
        {
            get => m_Profile;
            set => m_Profile = value;
        }

        public HBStyleProfileSections sections
        {
            get => m_Sections;
            set => m_Sections = value;
        }

        public Vector3 size
        {
            get => m_Size;
            set => m_Size = value;
        }

        public float blendDistance
        {
            get => m_BlendDistance;
            set => m_BlendDistance = Mathf.Max(0f, value);
        }

        public int priority
        {
            get => m_Priority;
            set
            {
                m_Priority = value;
                StyleEnvironmentZones.Sort();
            }
        }

        public float Weight(Vector3 position)
        {
            var local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            var half = Vector3.Scale(m_Size, transform.lossyScale) * 0.5f;

            var outside = new Vector3(Mathf.Max(Mathf.Abs(local.x) - Mathf.Abs(half.x), 0f),
                                      Mathf.Max(Mathf.Abs(local.y) - Mathf.Abs(half.y), 0f),
                                      Mathf.Max(Mathf.Abs(local.z) - Mathf.Abs(half.z), 0f));
            var distance = outside.magnitude;

            if (distance <= 0f)
                return 1f;

            return m_BlendDistance > 0f ? 1f - Mathf.Clamp01(distance / m_BlendDistance) : 0f;
        }

        void OnEnable()
        {
            StyleEnvironmentZones.Register(this);
        }

        void OnDisable()
        {
            StyleEnvironmentZones.Unregister(this);
        }

        void OnValidate()
        {
            m_BlendDistance = Mathf.Max(0f, m_BlendDistance);
            StyleEnvironmentZones.Sort();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, m_Size);

            if (m_BlendDistance <= 0f)
                return;

            var scale = transform.lossyScale;
            var grow = new Vector3(2f * m_BlendDistance / Mathf.Max(Mathf.Abs(scale.x), 1e-4f),
                                   2f * m_BlendDistance / Mathf.Max(Mathf.Abs(scale.y), 1e-4f),
                                   2f * m_BlendDistance / Mathf.Max(Mathf.Abs(scale.z), 1e-4f));

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(Vector3.zero, m_Size + grow);
        }
    }
}
