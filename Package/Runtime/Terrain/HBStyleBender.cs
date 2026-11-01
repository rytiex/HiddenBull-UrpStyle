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
        internal const int StampSize = 5;

        const float TrailFade = 3f;
        const float LeastTrail = 0.05f;
        const float LeastRadius = 0.001f;
        const float NoEdge = 1e-8f;

        static readonly List<HBStyleBender> s_Enabled = new List<HBStyleBender>();
        static readonly float[] s_Distances = new float[Limit];

        [SerializeField]
        [Tooltip("The footprint pressed into the grass and the snow. It follows this object's position, rotation " +
                 "and scale the way a collider does. The 256 benders nearest the camera count, up to 30 m away.")]
        HBStyleBenderShape m_Shape = HBStyleBenderShape.Ball;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Radius in metres before scale. It must reach the grass to bend it.")]
        float m_Radius = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Length in metres of the straight part before scale, along Axis.")]
        float m_Length = 2f;

        [SerializeField]
        [Tooltip("The object's local axis the capsule lies along.")]
        HBStyleBenderAxis m_Axis = HBStyleBenderAxis.Z;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Width, height and length in metres before scale, along this object's X, Y and Z axes.")]
        Vector3 m_Size = new Vector3(2f, 1f, 2f);

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

        Vector3 Scale
        {
            get
            {
                var scale = transform.lossyScale;

                return new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }
        }

        float ScaledRadius
        {
            get
            {
                var scale = Scale;

                if (m_Shape == HBStyleBenderShape.Capsule)
                    scale[(int)m_Axis] = 0f;

                return Mathf.Max(m_Radius * Mathf.Max(scale.x, scale.y, scale.z), LeastRadius);
            }
        }

        float ScaledLength => m_Length * Scale[(int)m_Axis];

        Vector3 ScaledSize => Vector3.Scale(m_Size, Scale);

        float Extent
        {
            get
            {
                switch (m_Shape)
                {
                    case HBStyleBenderShape.Box:
                        return ScaledSize.magnitude * 0.5f;

                    case HBStyleBenderShape.Capsule:
                        return ScaledRadius + ScaledLength * 0.5f;

                    default:
                        return ScaledRadius;
                }
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

        void Stamp(Vector4[] stamps, int first, Vector3 position, float extent)
        {
            var rate = TrailFade / Mathf.Max(m_Trail, LeastTrail);

            if (m_Shape == HBStyleBenderShape.Box)
            {
                StampBox(stamps, first, position, rate, extent);
                return;
            }

            var radius = ScaledRadius;
            var half = m_Shape == HBStyleBenderShape.Capsule
                ? transform.rotation * Direction * (ScaledLength * 0.5f)
                : Vector3.zero;

            stamps[first] = new Vector4(position.x, position.y, position.z, radius);
            stamps[first + 1] = new Vector4(m_Strength, rate, Mathf.Abs(half.y) + radius, 0f);
            stamps[first + 2] = new Vector4(half.x, half.z, 0f, 0f);
            stamps[first + 3] = new Vector4(0f, 0f, 0f, extent);
        }

        void StampBox(Vector4[] stamps, int first, Vector3 position, float rate, float extent)
        {
            var half = ScaledSize * 0.5f;
            var rotation = transform.rotation;
            var right = rotation * Vector3.right * half.x;
            var up = rotation * Vector3.up * half.y;
            var forward = rotation * Vector3.forward * half.z;
            var spine = new Vector2(forward.x, forward.z);
            var side = new Vector2(right.x, right.z);
            var last = new Vector2(up.x, up.z);

            if (side.sqrMagnitude > spine.sqrMagnitude)
                (spine, side) = (side, spine);

            if (last.sqrMagnitude > spine.sqrMagnitude)
                (spine, last) = (last, spine);

            var main = Edge(spine, spine, side, last);
            var next = Edge(side, spine, side, last);
            var end = Edge(last, spine, side, last);
            var drop = Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y);

            stamps[first] = new Vector4(position.x, position.y, position.z, main.z);
            stamps[first + 1] = new Vector4(m_Strength, rate, drop, 1f);
            stamps[first + 2] = new Vector4(spine.x, spine.y, main.x, main.y);
            stamps[first + 3] = new Vector4(next.x, next.y, next.z, extent);
            stamps[first + 4] = new Vector4(end.x, end.y, end.z, 0f);
        }

        static Vector3 Edge(Vector2 edge, Vector2 spine, Vector2 side, Vector2 last)
        {
            var line = edge.sqrMagnitude > NoEdge ? edge : spine;
            var normal = new Vector2(-line.y, line.x).normalized;
            var reach = Mathf.Abs(Vector2.Dot(spine, normal)) + Mathf.Abs(Vector2.Dot(side, normal)) +
                        Mathf.Abs(Vector2.Dot(last, normal));

            return new Vector3(normal.x, normal.y, reach);
        }

        void OnDrawGizmosSelected()
        {
            var direction = Direction;
            var radius = ScaledRadius;
            var end = direction * (ScaledLength * 0.5f);
            var cross = Vector3.Cross(direction, Vector3.one).normalized * radius;

            Gizmos.color = new Color(0.4f, 0.9f, 0.3f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            switch (m_Shape)
            {
                case HBStyleBenderShape.Box:
                    Gizmos.DrawWireCube(Vector3.zero, ScaledSize);
                    break;

                case HBStyleBenderShape.Capsule:
                    Gizmos.DrawWireSphere(end, radius);
                    Gizmos.DrawWireSphere(-end, radius);
                    Side(cross, end);
                    Side(-cross, end);
                    Side(Vector3.Cross(direction, cross), end);
                    Side(-Vector3.Cross(direction, cross), end);
                    break;

                default:
                    Gizmos.DrawWireSphere(Vector3.zero, radius);
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
                var extent = bender.Extent;
                var limit = reach + extent;

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

                bender.Stamp(stamps, slot * StampSize, position, extent);
                s_Distances[slot] = away;
            }

            return count;
        }
    }
}
