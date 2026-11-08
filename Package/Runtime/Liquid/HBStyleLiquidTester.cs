using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleLiquidTesterMode
    {
        Simple,
        Grid
    }

    public enum HBStyleLiquidTesterState
    {
        NoLiquid,
        Above,
        AtSurface,
        Submerged
    }

    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Liquid/HB Style Liquid Tester")]
    public sealed class HBStyleLiquidTester : MonoBehaviour
    {
        const float Marker = 0.25f;
        const float NormalLength = 1f;
        const float PlateSize = 1f;
        const float CellMark = 0.05f;

        static readonly Color s_None = new Color(0.55f, 0.55f, 0.55f, 1f);
        static readonly Color s_Above = new Color(1f, 0.8f, 0.2f, 1f);
        static readonly Color s_Surface = new Color(0.3f, 1f, 0.6f, 1f);
        static readonly Color s_Under = new Color(0.2f, 0.55f, 1f, 1f);
        static readonly Color s_Normal = new Color(1f, 0.35f, 0.85f, 1f);

        [SerializeField]
        [Tooltip("Simple tests this object's position: under, at or above the liquid, with the surface point, " +
                 "normal and depth. Grid samples a square around this object and draws the liquid surface as a mesh.")]
        HBStyleLiquidTesterMode m_Mode = HBStyleLiquidTesterMode.Simple;

        [SerializeField]
        [Tooltip("Includes the waves in the query. Off reads the still liquid level.")]
        bool m_Waves = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("How close to the surface, in metres, still counts as at the surface.")]
        float m_SurfaceBand = 0.1f;

        [SerializeField]
        [Range(1f, 200f)]
        [Tooltip("Grid mode: the width of the sampled square, in metres.")]
        float m_GridSize = 10f;

        [SerializeField]
        [Range(2, 64)]
        [Tooltip("Grid mode: the number of samples along each side.")]
        int m_GridSamples = 16;

        [SerializeField]
        [Tooltip("Draws the gizmo even when this object is not selected.")]
        bool m_AlwaysShow = true;

        Vector3[] m_Points;
        bool[] m_Found;

        public HBStyleLiquidTesterMode mode
        {
            get => m_Mode;
            set => m_Mode = value;
        }

        public HBStyleLiquidTesterState state { get; private set; }

        public float surfaceHeight { get; private set; }

        public Vector3 surfaceNormal { get; private set; } = Vector3.up;

        public float offset { get; private set; }

        public float depth { get; private set; }

        public Component liquid { get; private set; }

        public void Sample()
        {
            var position = transform.position;

            liquid = null;
            surfaceHeight = 0f;
            surfaceNormal = Vector3.up;
            offset = 0f;
            depth = 0f;

            if (!HBStyleLiquid.TryGetSurface(position, out var height, out var normal, out var found, m_Waves))
            {
                state = HBStyleLiquidTesterState.NoLiquid;
                return;
            }

            liquid = found;
            surfaceHeight = height;
            surfaceNormal = normal;
            offset = position.y - height;
            depth = HBStyleLiquid.TryGetDepth(position, out var water, m_Waves) ? water : 0f;

            if (Mathf.Abs(offset) <= m_SurfaceBand)
                state = HBStyleLiquidTesterState.AtSurface;
            else
                state = offset < 0f ? HBStyleLiquidTesterState.Submerged : HBStyleLiquidTesterState.Above;
        }

        void Update()
        {
            Sample();
        }

        void OnDrawGizmos()
        {
            if (m_AlwaysShow)
                Draw();
        }

        void OnDrawGizmosSelected()
        {
            if (!m_AlwaysShow)
                Draw();
        }

        void Draw()
        {
            if (m_Mode == HBStyleLiquidTesterMode.Grid)
                DrawGrid();
            else
                DrawSimple();
        }

        void DrawSimple()
        {
            Sample();

            var position = transform.position;
            var colour = Tint(state);

            Gizmos.color = colour;
            Gizmos.DrawSphere(position, Marker * 0.5f);
            Gizmos.DrawWireSphere(position, Marker);

            if (state != HBStyleLiquidTesterState.NoLiquid)
            {
                var surface = new Vector3(position.x, surfaceHeight, position.z);

                Gizmos.DrawLine(position, surface);
                Gizmos.matrix = Matrix4x4.TRS(surface, Quaternion.FromToRotation(Vector3.up, surfaceNormal),
                                              Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(PlateSize, 0f, PlateSize));
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = s_Normal;
                Gizmos.DrawLine(surface, surface + surfaceNormal * NormalLength);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = colour;
            UnityEditor.Handles.Label(position + Vector3.up * (Marker * 2f), Describe());
#endif
        }

        void DrawGrid()
        {
            var samples = Mathf.Clamp(m_GridSamples, 2, 64);
            var count = samples * samples;

            if (m_Points == null || m_Points.Length != count)
            {
                m_Points = new Vector3[count];
                m_Found = new bool[count];
            }

            var centre = transform.position;
            var size = Mathf.Max(m_GridSize, 0.01f);
            var step = size / (samples - 1);
            var start = new Vector3(centre.x - size * 0.5f, centre.y, centre.z - size * 0.5f);

            for (var z = 0; z < samples; z++)
            {
                for (var x = 0; x < samples; x++)
                {
                    var index = z * samples + x;
                    var point = new Vector3(start.x + x * step, centre.y, start.z + z * step);

                    m_Found[index] = HBStyleLiquid.TryGetHeight(point, out var height, m_Waves);
                    m_Points[index] = m_Found[index] ? new Vector3(point.x, height, point.z) : point;
                }
            }

            for (var z = 0; z < samples; z++)
            {
                for (var x = 0; x < samples; x++)
                {
                    var index = z * samples + x;
                    var point = m_Points[index];

                    if (!m_Found[index])
                    {
                        Gizmos.color = s_None;
                        Gizmos.DrawLine(point - Vector3.right * CellMark, point + Vector3.right * CellMark);
                        Gizmos.DrawLine(point - Vector3.forward * CellMark, point + Vector3.forward * CellMark);
                        continue;
                    }

                    Gizmos.color = Tint(point.y > centre.y ? HBStyleLiquidTesterState.Submerged
                                                           : HBStyleLiquidTesterState.Above);

                    if (x + 1 < samples && m_Found[index + 1])
                        Gizmos.DrawLine(point, m_Points[index + 1]);

                    if (z + 1 < samples && m_Found[index + samples])
                        Gizmos.DrawLine(point, m_Points[index + samples]);
                }
            }

            Gizmos.color = s_None;
            Gizmos.DrawWireCube(new Vector3(centre.x, centre.y, centre.z), new Vector3(size, 0f, size));
        }

        string Describe()
        {
            switch (state)
            {
                case HBStyleLiquidTesterState.NoLiquid:
                    return "No liquid";
                case HBStyleLiquidTesterState.Submerged:
                    return $"Submerged {-offset:0.00} m\nWater depth {Depth()}\n{Name()}";
                case HBStyleLiquidTesterState.AtSurface:
                    return $"At surface ({offset:+0.00;-0.00} m)\nWater depth {Depth()}\n{Name()}";
                default:
                    return $"Above {offset:0.00} m\nWater depth {Depth()}\n{Name()}";
            }
        }

        string Depth()
        {
            return float.IsInfinity(depth) ? "open" : $"{depth:0.00} m";
        }

        string Name()
        {
            return liquid != null ? liquid.name : string.Empty;
        }

        static Color Tint(HBStyleLiquidTesterState state)
        {
            switch (state)
            {
                case HBStyleLiquidTesterState.Above:
                    return s_Above;
                case HBStyleLiquidTesterState.AtSurface:
                    return s_Surface;
                case HBStyleLiquidTesterState.Submerged:
                    return s_Under;
                default:
                    return s_None;
            }
        }
    }
}
