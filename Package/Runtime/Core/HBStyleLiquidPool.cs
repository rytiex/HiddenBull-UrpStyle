using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Liquid/HB Style Liquid Pool")]
    public sealed class HBStyleLiquidPool : MonoBehaviour
    {
        internal const int Most = 8;

        const float Cell = 1f;
        const int CellsLeast = 4;
        const int CellsMost = 128;
        const float Kind = 2f;
        const float BoundsRise = 32f;
        const float BoundsMargin = 8f;
        const float CutMargin = 4f;
        const int Channels = 4;
        const string SurfaceName = "Style Liquid Pool Surface";
        const HideFlags SurfaceFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        static readonly int s_GridId = Shader.PropertyToID("_WaterGrid");
        static readonly int s_BoxId = Shader.PropertyToID("_WaterBox");
        static readonly int s_LevelMapId = Shader.PropertyToID("_WaterLevelMap");
        static readonly int s_WeightMapId = Shader.PropertyToID("_WaterWeightMap");

        static readonly Dictionary<Vector2Int, Mesh> s_Grids = new Dictionary<Vector2Int, Mesh>();
        static readonly HashSet<GameObject> s_Owned = new HashSet<GameObject>();
        static readonly List<HBStyleLiquidPool> s_Pools = new List<HBStyleLiquidPool>();

        [SerializeField]
        [Tooltip("A material using the HB URP Style/Liquid shader. The liquid fills the box drawn around this " +
                 "object up to its top face: scale the object to size it, the height of the box is how deep the " +
                 "liquid is. Only rotation around Y is used. A Pool stands on its own and never blends with a " +
                 "Liquid Terrain or another Pool.")]
        Material m_Material;

        GameObject m_Surface;
        MeshRenderer m_Renderer;
        MeshFilter m_Filter;
        MaterialPropertyBlock m_Properties;
        Vector4 m_Box;
        int m_Channel = -1;

        public static bool any => s_Pools.Count > 0;

        public Material material
        {
            get => m_Material;
            set
            {
                m_Material = value;
                Apply();
            }
        }

        internal static List<HBStyleLiquidPool> pools => s_Pools;

        internal int channel => m_Channel;

        internal float level => transform.position.y + Mathf.Abs(transform.lossyScale.y) * 0.5f;

        internal float bottom => transform.position.y - Mathf.Abs(transform.lossyScale.y) * 0.5f;

        internal Vector4 box
        {
            get
            {
                var scale = transform.lossyScale;
                var forward = heading;

                return new Vector4(Mathf.Max(Mathf.Abs(scale.x) * 0.5f, 1e-3f),
                                   Mathf.Max(Mathf.Abs(scale.z) * 0.5f, 1e-3f), forward.x, forward.z);
            }
        }

        internal Vector4 cells
        {
            get
            {
                var area = box;

                return new Vector4(Mathf.Max(area.x * 2f / Count(area.x * 2f), area.y * 2f / Count(area.y * 2f)), 0f,
                                   m_Channel, Kind);
            }
        }

        Vector3 heading
        {
            get
            {
                var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

                return forward.sqrMagnitude < 1e-6f ? Vector3.forward : forward.normalized;
            }
        }

        internal bool Near(Bounds bounds)
        {
            var own = box;
            var wide = new Vector2(own.x, own.y).magnitude * 2f + CutMargin;
            var deep = level - bottom + CutMargin * 2f;

            return bounds.Intersects(new Bounds(transform.position, new Vector3(wide, deep, wide)));
        }

        internal void Draw(RasterCommandBuffer cmd, Material drawn, int pass)
        {
            if (m_Filter != null && m_Filter.sharedMesh != null && m_Properties != null)
                cmd.DrawMesh(m_Filter.sharedMesh, m_Surface.transform.localToWorldMatrix, drawn, 0, pass, m_Properties);
        }

        internal static void AssignChannels(int next)
        {
            foreach (var pool in s_Pools)
            {
                if (pool == null)
                    continue;

                var wanted = -1;

                foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
                {
                    if (cutter == null || !cutter.Reaches(pool))
                        continue;

                    wanted = next++ % Channels;
                    break;
                }

                if (wanted == pool.m_Channel)
                    continue;

                pool.m_Channel = wanted;
                pool.Apply();
            }
        }

        static int Count(float side)
        {
            return Mathf.Clamp(Mathf.CeilToInt(side / Cell), CellsLeast, CellsMost);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void Sweep()
        {
            UnityEditor.EditorApplication.delayCall += static () =>
            {
                foreach (var filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
                {
                    var surface = filter.gameObject;

                    if (surface.name == SurfaceName && (surface.hideFlags & HideFlags.HideInHierarchy) != 0
                        && !s_Owned.Contains(surface))
                        DestroyImmediate(surface);
                }
            };
        }
#endif

        void OnEnable()
        {
            m_Surface = new GameObject(SurfaceName)
            {
                hideFlags = SurfaceFlags
            };

            s_Owned.Add(m_Surface);
            s_Pools.Remove(this);
            m_Channel = -1;
            s_Pools.Add(this);

            m_Filter = m_Surface.AddComponent<MeshFilter>();
            m_Renderer = m_Surface.AddComponent<MeshRenderer>();
            m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
            m_Renderer.lightProbeUsage = LightProbeUsage.Off;
            m_Renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

            Apply();
        }

        void OnDisable()
        {
            if (m_Surface != null)
                s_Owned.Remove(m_Surface);

            s_Pools.Remove(this);
            CoreUtils.Destroy(m_Surface);

            m_Surface = null;
            m_Renderer = null;
            m_Filter = null;
        }

        void OnValidate()
        {
            Apply();
        }

        void LateUpdate()
        {
            if (m_Surface == null)
                return;

            var position = transform.position;
            var area = box;

            position.y = level;

            m_Surface.transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading, Vector3.up));
            m_Surface.transform.localScale = new Vector3(area.x * 2f, 1f, area.y * 2f);
            m_Surface.layer = gameObject.layer;

            if (m_Renderer != null && m_Properties != null && area != m_Box)
                Apply();
        }

        void OnDrawGizmosSelected()
        {
            var scale = transform.lossyScale;

            Gizmos.color = new Color(0.3f, 0.7f, 0.95f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(heading, Vector3.up),
                                          new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        void Apply()
        {
            if (m_Renderer == null)
                return;

            m_Box = box;
            m_Properties ??= new MaterialPropertyBlock();
            m_Properties.SetVector(s_GridId, cells);
            m_Properties.SetVector(s_BoxId, m_Box);
            m_Properties.SetTexture(s_LevelMapId, Texture2D.blackTexture);
            m_Properties.SetTexture(s_WeightMapId, Texture2D.blackTexture);

            m_Filter.sharedMesh = Grid(Count(m_Box.x * 2f), Count(m_Box.y * 2f));
            m_Renderer.sharedMaterial = m_Material;
            m_Renderer.forceRenderingOff = m_Material == null;
            m_Renderer.localBounds = new Bounds(Vector3.zero,
                                                new Vector3(1f + BoundsMargin / m_Box.x, BoundsRise,
                                                            1f + BoundsMargin / m_Box.y));
            m_Renderer.SetPropertyBlock(m_Properties);

            LateUpdate();
        }

        static Mesh Grid(int columns, int rows)
        {
            var key = new Vector2Int(columns, rows);

            if (s_Grids.TryGetValue(key, out var grid) && grid != null)
                return grid;

            var side = columns + 1;
            var vertices = new List<Vector3>(side * (rows + 1));
            var levels = new List<Vector2>(side * (rows + 1));
            var triangles = new List<int>(columns * rows * 6);

            for (var z = 0; z <= rows; z++)
            {
                for (var x = 0; x <= columns; x++)
                {
                    vertices.Add(new Vector3((float)x / columns - 0.5f, 0f, (float)z / rows - 0.5f));
                    levels.Add(Vector2.zero);
                }
            }

            for (var z = 0; z < rows; z++)
            {
                for (var x = 0; x < columns; x++)
                {
                    var corner = z * side + x;

                    triangles.Add(corner);
                    triangles.Add(corner + side);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + side);
                    triangles.Add(corner + side + 1);
                }
            }

            grid = new Mesh
            {
                name = "Style Liquid Pool",
                hideFlags = HideFlags.HideAndDontSave,
                indexFormat = IndexFormat.UInt32
            };

            grid.SetVertices(vertices);
            grid.SetUVs(0, levels);
            grid.SetTriangles(triangles, 0);
            grid.bounds = new Bounds(Vector3.zero, new Vector3(1f, BoundsRise, 1f));

            s_Grids[key] = grid;

            return grid;
        }
    }
}
