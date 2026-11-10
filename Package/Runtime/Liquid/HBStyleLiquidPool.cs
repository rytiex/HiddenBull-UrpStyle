using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HB Style/Liquid/HB Style Liquid Pool")]
    public sealed class HBStyleLiquidPool : MonoBehaviour
    {
        internal const string PoolKeyword = "HB_LIQUID_POOL";

        const float Cell = 1f;
        const int CellsLeast = 4;
        const int CellsMost = 128;
        const float Edge = 32f;
        const float EdgeShare = 0.5f;
        const float WaveMargin = 2f;

        static readonly int s_PoolBoxId = Shader.PropertyToID("_HB_PoolBox");
        static readonly int s_PoolFrameId = Shader.PropertyToID("_HB_PoolFrame");
        static readonly int s_PoolDepthId = Shader.PropertyToID("_HB_PoolDepth");
        static readonly int s_CameraId = Shader.PropertyToID("_HB_TerrainCamera");
        static readonly int s_OriginId = Shader.PropertyToID("_HB_TerrainOrigin");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_TerrainSize");
        static readonly int s_CalmGridId = Shader.PropertyToID("_HB_LiquidCalmGrid");
        static readonly int s_IdId = Shader.PropertyToID("_HB_LiquidId");
        static readonly int s_KeepId = Shader.PropertyToID("_HB_LiquidKeep");
        static readonly int s_EyeId = Shader.PropertyToID("_HB_LiquidEye");

        static readonly Dictionary<int, Mesh> s_Grids = new Dictionary<int, Mesh>();
        static readonly List<HBStyleLiquidPool> s_Pools = new List<HBStyleLiquidPool>();

        static Material s_Material;

        [SerializeField]
        [Tooltip("The liquid in the pool: water, acid or lava. The liquid fills the box drawn around this object " +
                 "up to its top face: scale the object to size it, the height of the box is how deep the liquid " +
                 "is. Only rotation around Y is used. A Pool stands on its own and never blends with terrain " +
                 "liquid or another Pool.")]
        HBStyleLiquidLayer m_Layer;

        [SerializeField]
        [HideInInspector]
        Shader m_Shader;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_WaveCompute;

        readonly StyleLiquidLook m_Look = new StyleLiquidLook();
        readonly HBStyleLiquidLayer[] m_Layers = new HBStyleLiquidLayer[StyleLiquidLook.Slots];
        readonly Dictionary<Camera, MaterialPropertyBlock> m_Blocks = new Dictionary<Camera, MaterialPropertyBlock>();

        Camera m_Drawn;

        StyleLiquidWaves m_Waves;

        public static bool any => s_Pools.Count > 0;

        public HBStyleLiquidLayer layer
        {
            get => m_Layer;
            set => m_Layer = value;
        }

        internal static List<HBStyleLiquidPool> pools => s_Pools;

        internal float level => transform.position.y + Mathf.Abs(transform.lossyScale.y) * 0.5f;

        internal float bottom => transform.position.y - Mathf.Abs(transform.lossyScale.y) * 0.5f;

        internal Vector2 half
        {
            get
            {
                var scale = transform.lossyScale;

                return new Vector2(Mathf.Max(Mathf.Abs(scale.x) * 0.5f, 1e-3f), Mathf.Max(Mathf.Abs(scale.z) * 0.5f,
                                                                                         1e-3f));
            }
        }

        internal Vector2 turn
        {
            get
            {
                var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

                if (forward.sqrMagnitude < 1e-6f)
                    return new Vector2(0f, 1f);

                forward.Normalize();

                return new Vector2(forward.x, forward.z);
            }
        }

        internal float edge
        {
            get
            {
                var size = half;

                return Mathf.Min(Edge, Mathf.Min(size.x, size.y) * EdgeShare);
            }
        }

        internal Bounds bounds
        {
            get
            {
                var size = half;
                var wide = size.magnitude * 2f + WaveMargin * 2f;

                return new Bounds(new Vector3(transform.position.x, (level + bottom) * 0.5f, transform.position.z),
                                  new Vector3(wide, level - bottom + WaveMargin * 2f, wide));
            }
        }

        internal Vector2 Local(Vector2 position)
        {
            var away = position - new Vector2(transform.position.x, transform.position.z);
            var heading = turn;

            return new Vector2(away.x * heading.y - away.y * heading.x, away.x * heading.x + away.y * heading.y);
        }

        internal float Inward(Vector2 position)
        {
            var local = Local(position);
            var size = half;

            return Mathf.Min(size.x - Mathf.Abs(local.x), size.y - Mathf.Abs(local.y));
        }

        void OnEnable()
        {
            s_Pools.Remove(this);
            s_Pools.Add(this);
            RenderPipelineManager.beginCameraRendering += Draw;
        }

        void OnDisable()
        {
            s_Pools.Remove(this);
            RenderPipelineManager.beginCameraRendering -= Draw;
        }

        void OnDestroy()
        {
            m_Look.Dispose();

            if (m_Waves != null)
                StyleLiquidWaves.Release();

            m_Waves = null;
        }

#if UNITY_EDITOR
        void Reset()
        {
            LoadShaders();
        }

        void OnValidate()
        {
            LoadShaders();
        }

        void LoadShaders()
        {
            if (m_Shader == null)
                m_Shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(StyleTerrainLiquid.ShaderPath);

            if (m_WaveCompute == null)
                m_WaveCompute = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleLiquidWaves.ComputePath);
        }
#endif

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 0.95f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(Heading(), Vector3.up),
                                          Vector3.one);

            var size = half;

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x * 2f, level - bottom, size.y * 2f));
        }

        Vector3 Heading()
        {
            var heading = turn;

            return new Vector3(heading.x, 0f, heading.y);
        }

        void Draw(ScriptableRenderContext context, Camera camera)
        {
            m_Drawn = null;

            if (m_Layer == null || camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
                return;

#if UNITY_EDITOR
            if (camera.scene.IsValid() && camera.scene != gameObject.scene &&
                UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(camera.scene))
                return;

            if (m_Shader == null || m_WaveCompute == null)
                LoadShaders();
#endif

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additional) &&
                additional.renderType == CameraRenderType.Overlay)
                return;

            var material = PoolMaterial(m_Shader);

            if (material == null)
                return;

            var block = Block(camera);

            m_Waves ??= StyleLiquidWaves.Acquire();
            m_Layers[0] = m_Layer;
            m_Look.Bind(block, m_Layers);
            m_Waves.Bind(context, m_WaveCompute, block);

            var size = half;
            var heading = turn;
            var cells = Mathf.Max(size.x * 2f / Count(size.x * 2f), size.y * 2f / Count(size.y * 2f));
            var viewer = StyleDebug.Viewer(camera).transform.position;

            block.SetVector(s_PoolBoxId, new Vector4(transform.position.x, transform.position.z, size.x, size.y));
            block.SetVector(s_PoolFrameId, new Vector4(heading.x, heading.y, level, cells));
            block.SetVector(s_PoolDepthId, new Vector4(bottom, edge, 0f, 0f));
            block.SetVector(s_CameraId, new Vector4(viewer.x, viewer.y, viewer.z, 1f));
            block.SetVector(s_OriginId, new Vector4(0f, 0f, 0f, 1f));
            block.SetVector(s_SizeId, Vector4.one);
            block.SetVector(s_CalmGridId, Vector4.zero);
            block.SetFloat(s_IdId, HBStyleLiquidCutter.PoolKind);
            block.SetFloat(s_KeepId, HBStyleLiquidCutter.Keeps(this, bounds) ? 1f : 0f);
            block.SetFloat(s_EyeId, HBStyleLiquid.EyeLevel(this, camera.transform.position));

            var parameters = new RenderParams(material)
            {
                camera = camera,
                layer = gameObject.layer,
                matProps = block,
                worldBounds = bounds,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.Off,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.BlendProbes
            };

            Graphics.RenderMesh(parameters, Grid(Count(size.x * 2f), Count(size.y * 2f)), 0, Matrix4x4.identity);
            m_Drawn = camera;
        }

        internal void DrawRoof(RasterCommandBuffer cmd, Camera camera, string pass)
        {
            if (camera != m_Drawn || s_Material == null || !m_Blocks.TryGetValue(camera, out var block))
                return;

            var index = s_Material.FindPass(pass);

            if (index < 0)
                return;

            var size = half;
            var grid = Grid(Count(size.x * 2f), Count(size.y * 2f));

            cmd.DrawMesh(grid, Matrix4x4.identity, s_Material, 0, index, block);
        }

        internal void BindLook(MaterialPropertyBlock block)
        {
            m_Layers[0] = m_Layer;
            m_Look.Bind(block, m_Layers);
        }

        MaterialPropertyBlock Block(Camera camera)
        {
            if (m_Blocks.TryGetValue(camera, out var block))
                return block;

            var stale = new List<Camera>();

            foreach (var key in m_Blocks.Keys)
            {
                if (key == null)
                    stale.Add(key);
            }

            foreach (var key in stale)
                m_Blocks.Remove(key);

            block = new MaterialPropertyBlock();
            m_Blocks.Add(camera, block);
            return block;
        }

        static int Count(float size)
        {
            return Mathf.Clamp(Mathf.CeilToInt(size / Cell), CellsLeast, CellsMost);
        }

        static Material PoolMaterial(Shader shader)
        {
            if (shader == null)
                return null;

            if (s_Material != null && s_Material.shader == shader)
                return s_Material;

            CoreUtils.Destroy(s_Material);
            s_Material = CoreUtils.CreateEngineMaterial(shader);
            s_Material.EnableKeyword(PoolKeyword);
            return s_Material;
        }

        static Mesh Grid(int across, int along)
        {
            var key = across * 1024 + along;

            if (s_Grids.TryGetValue(key, out var grid) && grid != null)
                return grid;

            var vertices = new Vector3[(across + 1) * (along + 1)];
            var triangles = new int[across * along * 6];

            for (var z = 0; z <= along; z++)
            {
                for (var x = 0; x <= across; x++)
                    vertices[z * (across + 1) + x] = new Vector3((float)x / across, 0f, (float)z / along);
            }

            var index = 0;

            for (var z = 0; z < along; z++)
            {
                for (var x = 0; x < across; x++)
                {
                    var corner = z * (across + 1) + x;

                    triangles[index++] = corner;
                    triangles[index++] = corner + across + 1;
                    triangles[index++] = corner + 1;
                    triangles[index++] = corner + 1;
                    triangles[index++] = corner + across + 1;
                    triangles[index++] = corner + across + 2;
                }
            }

            grid = new Mesh
            {
                name = "HB Style Liquid Pool Grid",
                hideFlags = HideFlags.HideAndDontSave,
                indexFormat = IndexFormat.UInt32
            };

            grid.vertices = vertices;
            grid.triangles = triangles;
            grid.bounds = new Bounds(new Vector3(0.5f, 0f, 0.5f), Vector3.one * 1e5f);
            s_Grids[key] = grid;
            return grid;
        }
    }
}
