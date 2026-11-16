using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleGrassClearShape
    {
        Box,
        Sphere
    }

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Terrain/HB Style Grass Clear")]
    public sealed class HBStyleGrassClear : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Box is a block the size of Size that follows this object's position, turn and scale, for a " +
                 "foundation, a floor or a road piece. Sphere is a ball around this transform, for a camp fire " +
                 "or a tent.")]
        HBStyleGrassClearShape m_Shape = HBStyleGrassClearShape.Box;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Box only: width, height and length in metres before scale, along this object's X, Y and Z axes. " +
                 "Grass and flowers growing inside the box, or under it close enough to poke into it, are " +
                 "removed.")]
        Vector3 m_Size = new Vector3(4f, 1f, 4f);

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Sphere only: radius in metres of the ball around this transform that grass and flowers are " +
                 "removed from.")]
        float m_Radius = 2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Width in metres of the band outside the shape where the grass grows back: shorter and sparser " +
                 "next to the shape, full beyond the band. 0 cuts the grass off sharply at the edge.")]
        float m_Edge = 0.4f;

        public HBStyleGrassClearShape shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        public Vector3 size
        {
            get => m_Size;
            set => m_Size = Vector3.Max(value, Vector3.one * 0.1f);
        }

        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, 0.1f);
        }

        public float edge
        {
            get => m_Edge;
            set => m_Edge = Mathf.Max(value, 0f);
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

        void OnEnable()
        {
            StyleGrassClears.Register(this);
        }

        void OnDisable()
        {
            StyleGrassClears.Release(this);
        }

        void OnDrawGizmosSelected()
        {
            var position = transform.position;

            Gizmos.color = new Color(0.4f, 0.9f, 0.3f, 0.8f);

            if (m_Shape == HBStyleGrassClearShape.Box)
            {
                var right = transform.right;
                var heading = new Vector2(right.x, right.z);
                var yaw = heading.sqrMagnitude > 1e-6f ? Mathf.Atan2(-heading.y, heading.x) * Mathf.Rad2Deg : 0f;

                Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, scaledSize);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else
            {
                Gizmos.DrawWireSphere(position, m_Radius);
            }
        }
    }
}
