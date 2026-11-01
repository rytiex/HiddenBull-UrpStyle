using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleBenderShape
    {
        Ball,
        Capsule,
        Box
    }

    public enum HBStyleBenderAxis
    {
        X,
        Y,
        Z
    }

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Terrain/HB Style Bender")]
    public sealed class HBStyleBender : MonoBehaviour
    {
        internal const int Limit = 256;

        const float TrailFade = 3f;
        const float LeastTrail = 0.05f;

        static readonly List<HBStyleBender> s_Enabled = new List<HBStyleBender>();
        static readonly float[] s_Distances = new float[Limit];

        [SerializeField]
        [Tooltip("The footprint pressed into the grass and the snow. It follows this object's position. The 256 " +
                 "benders nearest the camera count, up to 30 m away.")]
        HBStyleBenderShape m_Shape = HBStyleBenderShape.Ball;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Radius in metres. It must reach the grass to bend it.")]
        float m_Radius = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Length in metres of the straight part, along Axis.")]
        float m_Length = 2f;

        [SerializeField]
        [Tooltip("The object's local axis the capsule lies along. With Freeze Rotation it is the world axis.")]
        HBStyleBenderAxis m_Axis = HBStyleBenderAxis.Z;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Width, height and length in metres. Length runs along this object's forward axis.")]
        Vector3 m_Size = new Vector3(2f, 1f, 2f);

        [SerializeField]
        [Tooltip("Ignores this object's rotation, so the shape keeps its world direction while the object turns.")]
        bool m_FreezeRotation;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How hard the grass is pressed at the centre. 1 leans it away, darkens it and shortens it. " +
                 "0 leaves it standing.")]
        float m_Strength = 1f;

        [SerializeField]
        [Range(0.05f, 30f)]
        [Tooltip("Seconds the pressed grass takes to stand back up behind this bender.")]
        float m_Trail = 3f;

        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, 0.05f);
        }

        public float strength
        {
            get => m_Strength;
            set => m_Strength = Mathf.Clamp01(value);
        }

        public HBStyleBenderShape shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        public float length
        {
            get => m_Length;
            set => m_Length = Mathf.Max(value, 0f);
        }

        public Vector3 size
        {
            get => m_Size;
            set => m_Size = Vector3.Max(value, Vector3.one * 0.05f);
        }

        public HBStyleBenderAxis axis
        {
            get => m_Axis;
            set => m_Axis = value;
        }

        public bool freezeRotation
        {
            get => m_FreezeRotation;
            set => m_FreezeRotation = value;
        }

        public float trail
        {
            get => m_Trail;
            set => m_Trail = Mathf.Clamp(value, LeastTrail, 30f);
        }

        void OnEnable()
        {
            s_Enabled.Remove(this);
            s_Enabled.Add(this);
        }

        void OnDisable()
        {
            s_Enabled.Remove(this);
        }

        float Across => m_Shape == HBStyleBenderShape.Box ? m_Size.x * 0.5f : m_Radius;

        float Extent
        {
            get
            {
                switch (m_Shape)
                {
                    case HBStyleBenderShape.Box:
                        return new Vector2(m_Size.x, m_Size.z).magnitude * 0.5f;

                    case HBStyleBenderShape.Capsule:
                        return m_Radius + m_Length * 0.5f;

                    default:
                        return m_Radius;
                }
            }
        }

        Quaternion Turn
        {
            get
            {
                if (m_FreezeRotation)
                    return Quaternion.identity;

                if (m_Shape == HBStyleBenderShape.Capsule)
                    return transform.rotation;

                var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

                return forward.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            }
        }

        Vector3 Direction
        {
            get
            {
                switch (m_Axis)
                {
                    case HBStyleBenderAxis.X:
                        return Vector3.right;

                    case HBStyleBenderAxis.Y:
                        return Vector3.up;

                    default:
                        return Vector3.forward;
                }
            }
        }

        Vector4 Form()
        {
            if (m_Shape == HBStyleBenderShape.Ball)
                return new Vector4(0f, 0f, 0f, m_Radius);

            if (m_Shape == HBStyleBenderShape.Box)
            {
                var along = Turn * Vector3.forward * (m_Size.z * 0.5f);

                return new Vector4(along.x, along.z, 1f, m_Size.y * 0.5f);
            }

            var half = Turn * Direction * (m_Length * 0.5f);

            return new Vector4(half.x, half.z, 0f, Mathf.Abs(half.y) + m_Radius);
        }

        void OnDrawGizmosSelected()
        {
            var direction = Direction;
            var end = direction * (m_Length * 0.5f);
            var cross = Vector3.Cross(direction, Vector3.one).normalized * m_Radius;

            Gizmos.color = new Color(0.4f, 0.9f, 0.3f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Turn, Vector3.one);

            switch (m_Shape)
            {
                case HBStyleBenderShape.Box:
                    Gizmos.DrawWireCube(Vector3.zero, m_Size);
                    break;

                case HBStyleBenderShape.Capsule:
                    Gizmos.DrawWireSphere(end, m_Radius);
                    Gizmos.DrawWireSphere(-end, m_Radius);
                    Side(cross, end);
                    Side(-cross, end);
                    Side(Vector3.Cross(direction, cross), end);
                    Side(-Vector3.Cross(direction, cross), end);
                    break;

                default:
                    Gizmos.DrawWireSphere(Vector3.zero, m_Radius);
                    break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        static void Side(Vector3 offset, Vector3 end)
        {
            Gizmos.DrawLine(offset - end, offset + end);
        }

        internal static int Collect(Vector3 camera, float reach, Vector4[] stamps)
        {
            var count = 0;

            foreach (var bender in s_Enabled)
            {
                if (bender.m_Strength <= 0f)
                    continue;

                var position = bender.transform.position;
                var away = (position - camera).sqrMagnitude;
                var limit = reach + bender.Extent;

                if (away > limit * limit)
                    continue;

                var slot = count;

                if (count == Limit)
                {
                    slot = 0;

                    for (var other = 1; other < Limit; other++)
                    {
                        if (s_Distances[other] > s_Distances[slot])
                            slot = other;
                    }

                    if (s_Distances[slot] <= away)
                        continue;
                }
                else
                {
                    count++;
                }

                stamps[slot * 3] = new Vector4(position.x, position.y, position.z, bender.Across);
                stamps[slot * 3 + 1] = new Vector4(bender.m_Strength,
                                                   TrailFade / Mathf.Max(bender.m_Trail, LeastTrail));
                stamps[slot * 3 + 2] = bender.Form();
                s_Distances[slot] = away;
            }

            return count;
        }
    }
}
