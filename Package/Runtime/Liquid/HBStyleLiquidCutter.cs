using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleLiquidCutShape
    {
        Box,
        Sphere,
        Mesh
    }

    public enum HBStyleLiquidCutMode
    {
        CutOut,
        KeepInside
    }

    [Flags]
    public enum HBStyleLiquidCutTargets
    {
        Terrain = 1,
        Pool = 2
    }

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Liquid/HB Style Liquid Cutter")]
    public sealed class HBStyleLiquidCutter : MonoBehaviour
    {
        const int SphereSegments = 32;
        const int SphereRings = 16;

        internal const float TerrainKind = 1f;
        internal const float PoolKind = 2f;

        static readonly List<HBStyleLiquidCutter> s_Enabled = new List<HBStyleLiquidCutter>();

        static Mesh s_Box;
        static Mesh s_Sphere;

        [SerializeField]
        [Tooltip("The shape of the volume. Box and Sphere are one metre across and follow this object's " +
                 "position, rotation and scale, so scale the object to size them. Mesh uses any closed mesh, " +
                 "the inside of a boat hull for instance.")]
        HBStyleLiquidCutShape m_Shape = HBStyleLiquidCutShape.Box;

        [SerializeField]
        [Tooltip("The mesh used when Shape is Mesh. It must be closed, with no open edges, and have normals. " +
                 "Keep it simple, a rough hull is enough.")]
        Mesh m_Mesh;

        [SerializeField]
        [Tooltip("Cut Out removes the liquid inside the volume. Keep Inside does the opposite and draws the " +
                 "liquid only inside it, which turns an endless surface into a pool, a canal or a tank.")]
        HBStyleLiquidCutMode m_Mode = HBStyleLiquidCutMode.CutOut;

        [SerializeField]
        [Tooltip("Which liquids this cutter works on: the liquid of HB Style Terrains, Pools, or both. Either way " +
                 "it only changes a liquid its volume actually touches.")]
        HBStyleLiquidCutTargets m_Affects = HBStyleLiquidCutTargets.Terrain | HBStyleLiquidCutTargets.Pool;

        Mesh m_Solid;
        Vector3[] m_Points;
        int[] m_Corners;

        public HBStyleLiquidCutShape shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        public Mesh mesh
        {
            get => m_Mesh;
            set => m_Mesh = value;
        }

        public HBStyleLiquidCutMode mode
        {
            get => m_Mode;
            set => m_Mode = value;
        }

        public HBStyleLiquidCutTargets affects
        {
            get => m_Affects;
            set => m_Affects = value;
        }

        internal static List<HBStyleLiquidCutter> enabledCutters => s_Enabled;

        internal Mesh volume
        {
            get
            {
                switch (m_Shape)
                {
                    case HBStyleLiquidCutShape.Sphere:
                        return s_Sphere != null ? s_Sphere : s_Sphere = BuildSphere();
                    case HBStyleLiquidCutShape.Mesh:
                        return m_Mesh;
                    default:
                        return s_Box != null ? s_Box : s_Box = BuildBox();
                }
            }
        }

        internal static bool Keeps(Component liquid, Bounds bounds)
        {
            foreach (var cutter in s_Enabled)
            {
                if (cutter != null && cutter.m_Mode == HBStyleLiquidCutMode.KeepInside && cutter.Affects(liquid) &&
                    cutter.Spans(out var volume) && volume.Intersects(bounds))
                    return true;
            }

            return false;
        }

        internal bool Affects(Component liquid)
        {
            return Affects(liquid is HBStyleLiquidPool
                ? HBStyleLiquidCutTargets.Pool
                : HBStyleLiquidCutTargets.Terrain);
        }

        internal bool Affects(HBStyleLiquidCutTargets kind)
        {
            return (m_Affects & kind) != 0;
        }

        internal bool Reaches(Component liquid, Bounds bounds)
        {
            return Affects(liquid) && Spans(out var volume) && volume.Intersects(bounds);
        }

        internal bool Spans(out Bounds bounds)
        {
            var shape = volume;

            bounds = default;

            if (shape == null)
                return false;

            var matrix = transform.localToWorldMatrix;
            var extents = shape.bounds.extents;
            var reach = new Vector3(Span(matrix.GetRow(0), extents), Span(matrix.GetRow(1), extents),
                                    Span(matrix.GetRow(2), extents));

            bounds = new Bounds(matrix.MultiplyPoint3x4(shape.bounds.center), reach * 2f);

            return true;
        }

        static float Span(Vector4 row, Vector3 extents)
        {
            return Mathf.Abs(row.x) * extents.x + Mathf.Abs(row.y) * extents.y + Mathf.Abs(row.z) * extents.z;
        }

        internal bool Contains(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);

            if (m_Shape == HBStyleLiquidCutShape.Sphere)
                return local.sqrMagnitude <= 0.25f;

            if (m_Shape == HBStyleLiquidCutShape.Box)
                return Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f && Mathf.Abs(local.z) <= 0.5f;

            if (m_Mesh == null || !m_Mesh.bounds.Contains(local))
                return false;

            if (!m_Mesh.isReadable)
                return true;

            if (m_Solid != m_Mesh)
            {
                m_Solid = m_Mesh;
                m_Points = m_Mesh.vertices;
                m_Corners = m_Mesh.triangles;
            }

            var above = 0;

            for (var index = 0; index + 2 < m_Corners.Length; index += 3)
            {
                var first = m_Points[m_Corners[index]];
                var second = m_Points[m_Corners[index + 1]] - first;
                var third = m_Points[m_Corners[index + 2]] - first;
                var area = second.x * third.z - second.z * third.x;

                if (Mathf.Abs(area) < 1e-12f)
                    continue;

                var offset = new Vector2(local.x - first.x, local.z - first.z);
                var along = (offset.x * third.z - offset.y * third.x) / area;
                var across = (second.x * offset.y - second.z * offset.x) / area;

                if (along < 0f || across < 0f || along + across >= 1f)
                    continue;

                if (first.y + second.y * along + third.y * across > local.y)
                    above++;
            }

            return (above & 1) == 1;
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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = m_Mode == HBStyleLiquidCutMode.KeepInside
                ? new Color(0.3f, 0.95f, 0.6f, 0.8f)
                : new Color(0.3f, 0.7f, 0.95f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (m_Shape == HBStyleLiquidCutShape.Sphere)
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
            else if (m_Shape == HBStyleLiquidCutShape.Box)
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            else if (m_Mesh != null && m_Mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
                Gizmos.DrawWireMesh(m_Mesh);
        }

        static Mesh BuildBox()
        {
            var vertices = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var triangles = new List<int>(36);

            for (var axis = 0; axis < 3; axis++)
            {
                for (var sign = -1; sign <= 1; sign += 2)
                {
                    var normal = Vector3.zero;
                    var right = Vector3.zero;
                    var up = Vector3.zero;

                    normal[axis] = sign;
                    right[(axis + 1) % 3] = 1f;
                    up[(axis + 2) % 3] = 1f;

                    var first = vertices.Count;

                    vertices.Add((normal - right - up) * 0.5f);
                    vertices.Add((normal + right - up) * 0.5f);
                    vertices.Add((normal + right + up) * 0.5f);
                    vertices.Add((normal - right + up) * 0.5f);

                    for (var corner = 0; corner < 4; corner++)
                        normals.Add(normal);

                    triangles.Add(first);
                    triangles.Add(first + 1);
                    triangles.Add(first + 2);
                    triangles.Add(first);
                    triangles.Add(first + 2);
                    triangles.Add(first + 3);
                }
            }

            return Build("HB Style Liquid Cut Box", vertices, normals, triangles);
        }

        static Mesh BuildSphere()
        {
            var vertices = new List<Vector3>((SphereSegments + 1) * (SphereRings + 1));
            var normals = new List<Vector3>(vertices.Capacity);
            var triangles = new List<int>(SphereSegments * SphereRings * 6);

            for (var ring = 0; ring <= SphereRings; ring++)
            {
                var polar = Mathf.PI * ring / SphereRings;

                for (var segment = 0; segment <= SphereSegments; segment++)
                {
                    var around = 2f * Mathf.PI * segment / SphereSegments;
                    var normal = new Vector3(Mathf.Sin(polar) * Mathf.Cos(around), Mathf.Cos(polar),
                                             Mathf.Sin(polar) * Mathf.Sin(around));

                    vertices.Add(normal * 0.5f);
                    normals.Add(normal);
                }
            }

            for (var ring = 0; ring < SphereRings; ring++)
            {
                for (var segment = 0; segment < SphereSegments; segment++)
                {
                    var corner = ring * (SphereSegments + 1) + segment;
                    var below = corner + SphereSegments + 1;

                    triangles.Add(corner);
                    triangles.Add(corner + 1);
                    triangles.Add(below);
                    triangles.Add(corner + 1);
                    triangles.Add(below + 1);
                    triangles.Add(below);
                }
            }

            return Build("HB Style Liquid Cut Sphere", vertices, normals, triangles);
        }

        static Mesh Build(string name, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
        {
            var built = new Mesh
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave
            };

            built.SetVertices(vertices);
            built.SetNormals(normals);
            built.SetTriangles(triangles, 0);

            return built;
        }
    }
}
