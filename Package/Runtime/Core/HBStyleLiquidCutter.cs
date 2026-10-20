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

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Liquid/HB Style Liquid Cutter")]
    public sealed class HBStyleLiquidCutter : MonoBehaviour
    {
        const int SphereSegments = 32;
        const int SphereRings = 16;
        const float TightSize = 6f;

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
        [Tooltip("Limits this cutter to one Liquid Terrain in the scene. Leave empty to affect every Liquid " +
                 "Terrain and every Pool the volume touches.")]
        HBStyleLiquidTerrain m_Target;

        [SerializeField]
        [Tooltip("Treats the cut out space as dry land: the ground in it is no longer soaked by the liquid, " +
                 "rain and snow reach it, and a roof over it keeps it dry. Turn it off to leave the ground " +
                 "wet, as in a hole that has just been drained.")]
        bool m_KeepDry = true;

        [SerializeField]
        [Tooltip("Cuts the liquid without drying what lies below the volume, so the bed under a boat stays " +
                 "wet while the boat itself stays dry. Only used while Keep Dry is on.")]
        bool m_KeepGroundWet;

        [SerializeField]
        [Tooltip("Grows the dried area beyond the volume without cutting any more liquid, in metres on each " +
                 "side along this object's own X and Z. Use it to keep a deck or a rim around the hole dry. " +
                 "Negative values shrink it. Only used while Keep Dry is on.")]
        Vector2 m_DryOffset;

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

        public HBStyleLiquidTerrain target
        {
            get => m_Target;
            set => m_Target = value;
        }

        public bool keepDry
        {
            get => m_KeepDry;
            set => m_KeepDry = value;
        }

        public bool keepGroundWet
        {
            get => m_KeepGroundWet;
            set => m_KeepGroundWet = value;
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

        public Vector2 dryOffset
        {
            get => m_DryOffset;
            set => m_DryOffset = value;
        }

        internal Matrix4x4 dryMatrix
        {
            get
            {
                var own = transform.localToWorldMatrix;
                var mesh = volume;

                if (mesh == null || m_DryOffset == Vector2.zero)
                    return own;

                var bounds = mesh.bounds;
                var scale = transform.lossyScale;
                var width = Mathf.Max(Mathf.Abs(bounds.size.x * scale.x), 1e-3f);
                var depth = Mathf.Max(Mathf.Abs(bounds.size.z * scale.z), 1e-3f);
                var grow = new Vector3(Mathf.Max((width + m_DryOffset.x * 2f) / width, 0f), 1f,
                                       Mathf.Max((depth + m_DryOffset.y * 2f) / depth, 0f));

                return own * Matrix4x4.Translate(bounds.center) * Matrix4x4.Scale(grow)
                           * Matrix4x4.Translate(-bounds.center);
            }
        }

        internal bool dries => m_Mode == HBStyleLiquidCutMode.KeepInside || m_KeepDry;

        internal bool driesGround => m_Mode == HBStyleLiquidCutMode.KeepInside || !m_KeepGroundWet;

        internal bool tight
        {
            get
            {
                var mesh = volume;

                if (mesh == null)
                    return false;

                var size = Vector3.Scale(mesh.bounds.size, transform.lossyScale);

                return Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.z)) < TightSize;
            }
        }

        internal bool Reaches(HBStyleLiquidTerrain terrain)
        {
            return Affects(terrain) && Spans(out var bounds)
                && (m_Mode == HBStyleLiquidCutMode.KeepInside || terrain.Near(bounds));
        }

        internal bool Reaches(HBStyleLiquidPool pool)
        {
            return Affects(pool) && Spans(out var bounds) && pool.Near(bounds);
        }

        internal bool Spans(out Bounds bounds)
        {
            var mesh = volume;

            bounds = default;

            if (mesh == null)
                return false;

            var matrix = transform.localToWorldMatrix;
            var extents = mesh.bounds.extents;
            var reach = new Vector3(Span(matrix.GetRow(0), extents), Span(matrix.GetRow(1), extents),
                                    Span(matrix.GetRow(2), extents));

            bounds = new Bounds(matrix.MultiplyPoint3x4(mesh.bounds.center), reach * 2f);

            return true;
        }

        static float Span(Vector4 row, Vector3 extents)
        {
            return Mathf.Abs(row.x) * extents.x + Mathf.Abs(row.y) * extents.y + Mathf.Abs(row.z) * extents.z;
        }

        internal bool Affects(HBStyleLiquidPool pool)
        {
            return m_Target == null;
        }

        internal bool Affects(HBStyleLiquidTerrain terrain)
        {
            return m_Target == null || m_Target == terrain;
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

            return Build("Style Liquid Cut Box", vertices, normals, triangles);
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

            return Build("Style Liquid Cut Sphere", vertices, normals, triangles);
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
