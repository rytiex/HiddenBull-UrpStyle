using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TerrainCollider))]
    [AddComponentMenu("HiddenBull/HB Style/Terrain/HB Style Terrain")]
    public sealed class HBStyleTerrain : MonoBehaviour
    {
        internal const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/Terrain.shader";

        const float ShadowMargin = 64f;
        const float FallbackShadowDistance = 150f;
        const int BatchSize = 1023;

        static readonly int s_HeightmapId = Shader.PropertyToID("_HB_TerrainHeightmap");
        static readonly int s_HolesId = Shader.PropertyToID("_HB_TerrainHoles");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_TerrainSize");
        static readonly int s_OriginId = Shader.PropertyToID("_HB_TerrainOrigin");
        static readonly int s_CameraId = Shader.PropertyToID("_HB_TerrainCamera");
        static readonly int s_RangesId = Shader.PropertyToID("_HB_TerrainRanges");
        static readonly int s_ControlId = Shader.PropertyToID("_HB_TerrainControl");
        static readonly int s_AlbedoId = Shader.PropertyToID("_HB_LayerAlbedo");
        static readonly int s_NormalId = Shader.PropertyToID("_HB_LayerNormal");
        static readonly int s_MaskId = Shader.PropertyToID("_HB_LayerMask");
        static readonly int s_LayersId = Shader.PropertyToID("_HB_TerrainLayers");
        static readonly int s_LayerSTId = Shader.PropertyToID("_HB_LayerST");
        static readonly int s_LayerLookId = Shader.PropertyToID("_HB_LayerLook");
        static readonly int s_LayerSurfaceId = Shader.PropertyToID("_HB_LayerSurface");
        static readonly int s_BrushId = Shader.PropertyToID("_HB_TerrainBrush");
        static readonly int s_StyleId = Shader.PropertyToID("_HB_TerrainStyle");
        static readonly int s_VariationId = Shader.PropertyToID("_HB_TerrainVariation");

        static readonly List<HBStyleTerrain> s_Active = new List<HBStyleTerrain>();

        static Mesh s_Patch;
        static Mesh s_Quarter;
        static Texture2DArray s_EmptyArray;

        [SerializeField]
        [Tooltip("Stores the terrain: heights, holes, layer maps and settings. Press New for a flat terrain, then " +
                 "edit everything here; Settings can import a Unity terrain.")]
        HBStyleTerrainData m_Data;

        [SerializeField]
        [HideInInspector]
        Shader m_Shader;

        readonly StyleTerrainTree m_Tree = new StyleTerrainTree();
        readonly List<StyleTerrainNode> m_Nodes = new List<StyleTerrainNode>();
        readonly List<Matrix4x4> m_Full = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_Quarters = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_ShadowFull = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_ShadowQuarters = new List<Matrix4x4>();
        readonly Dictionary<Camera, MaterialPropertyBlock> m_Blocks = new Dictionary<Camera, MaterialPropertyBlock>();
        readonly Plane[] m_Planes = new Plane[6];
        readonly float[] m_LocalPlanes = new float[24];
        readonly Vector4[] m_Ranges = new Vector4[StyleTerrainTree.MaxLevels];
        readonly Vector4[] m_LayerST = new Vector4[HBStyleTerrainData.MaxLayers];
        readonly Vector4[] m_LayerLook = new Vector4[HBStyleTerrainData.MaxLayers];
        readonly Vector4[] m_LayerSurface = new Vector4[HBStyleTerrainData.MaxLayers];
        readonly float[] m_Weights = new float[HBStyleTerrainData.MaxLayers];
        readonly HBStyleTerrainLayer[] m_OneLayer = new HBStyleTerrainLayer[1];
        readonly float[] m_OneWeight = new float[1];
        readonly Stopwatch m_Watch = new Stopwatch();

        Material m_Material;
        HBStyleTerrainData m_BuiltData;
        int m_BuiltVersion = -1;

        internal static IReadOnlyList<HBStyleTerrain> active => s_Active;

        public HBStyleTerrainData data
        {
            get => m_Data;
            set
            {
                m_Data = value;
                Bind();
            }
        }

        public bool TryGetHeight(Vector3 position, out float height)
        {
            height = 0f;

            if (!TryGetUV(position, out var u, out var v))
                return false;

            height = transform.position.y + m_Data.terrainData.GetInterpolatedHeight(u, v);
            return true;
        }

        public bool TryGetNormal(Vector3 position, out Vector3 normal)
        {
            normal = Vector3.up;

            if (!TryGetUV(position, out var u, out var v))
                return false;

            normal = m_Data.terrainData.GetInterpolatedNormal(u, v);
            return true;
        }

        public HBStyleTerrainLayer GetLayerAt(Vector3 position)
        {
            return GetLayersAt(position, m_OneLayer, m_OneWeight) > 0 ? m_OneLayer[0] : null;
        }

        public int GetLayersAt(Vector3 position, HBStyleTerrainLayer[] layers, float[] weights)
        {
            if (layers == null || weights == null)
                return 0;

            var wanted = Mathf.Min(3, Mathf.Min(layers.Length, weights.Length));

            for (var i = 0; i < wanted; i++)
            {
                layers[i] = null;
                weights[i] = 0f;
            }

            if (wanted == 0 || !TryGetUV(position, out var u, out var v) || !m_Data.painted ||
                !m_Data.control.isReadable)
                return 0;

            var control = m_Data.control;
            var resolution = control.width;
            var count = Mathf.Min(m_Data.controlLayers, HBStyleTerrainData.MaxLayers);
            var x = u * resolution - 0.5f;
            var z = v * resolution - 0.5f;
            var left = Mathf.FloorToInt(x);
            var bottom = Mathf.FloorToInt(z);
            var fx = x - left;
            var fz = z - bottom;
            var x0 = Mathf.Clamp(left, 0, resolution - 1);
            var x1 = Mathf.Clamp(left + 1, 0, resolution - 1);
            var z0 = Mathf.Clamp(bottom, 0, resolution - 1) * resolution;
            var z1 = Mathf.Clamp(bottom + 1, 0, resolution - 1) * resolution;

            for (var slice = 0; slice * 4 < count; slice++)
            {
                var pixels = control.GetPixelData<Color32>(0, slice);
                var a = pixels[z0 + x0];
                var b = pixels[z0 + x1];
                var c = pixels[z1 + x0];
                var d = pixels[z1 + x1];

                for (var channel = 0; channel < 4 && slice * 4 + channel < count; channel++)
                {
                    var bottomRow = Mathf.Lerp(a[channel], b[channel], fx);
                    var topRow = Mathf.Lerp(c[channel], d[channel], fx);

                    m_Weights[slice * 4 + channel] = Mathf.Lerp(bottomRow, topRow, fz) / 255f;
                }
            }

            var found = 0;
            var total = 0f;

            for (; found < wanted; found++)
            {
                var best = -1;

                for (var index = 0; index < count; index++)
                {
                    if (m_Weights[index] > 0f && (best < 0 || m_Weights[index] > m_Weights[best]))
                        best = index;
                }

                if (best < 0)
                    break;

                layers[found] = m_Data.Layer(best);
                weights[found] = m_Weights[best];
                total += m_Weights[best];
                m_Weights[best] = 0f;
            }

            for (var i = 0; i < found; i++)
                weights[i] /= total;

            return found;
        }

        bool TryGetUV(Vector3 position, out float u, out float v)
        {
            u = 0f;
            v = 0f;

            if (m_Data == null || m_Data.terrainData == null)
                return false;

            var size = m_Data.size;
            var local = position - transform.position;

            u = local.x / size.x;
            v = local.z / size.z;

            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }

        void OnEnable()
        {
            s_Active.Add(this);
            RenderPipelineManager.beginCameraRendering += Draw;
            Bind();
        }

        void OnDisable()
        {
            s_Active.Remove(this);
            RenderPipelineManager.beginCameraRendering -= Draw;
        }

        void OnDestroy()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }

#if UNITY_EDITOR
        void Reset()
        {
            m_Shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        }

        void OnValidate()
        {
            if (m_Shader == null)
                m_Shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    Bind();
            };
        }
#endif

        void Bind()
        {
            if (TryGetComponent<TerrainCollider>(out var collider))
                collider.terrainData = m_Data != null ? m_Data.terrainData : null;

            m_BuiltVersion = -1;
        }

        bool Ready()
        {
            if (m_Data == null || !m_Data.valid)
                return false;

            if (m_BuiltData != m_Data || m_BuiltVersion != m_Data.version)
            {
                var size = m_Data.size;
                var leaf = size.x / (m_Data.resolution - 1) * StyleTerrainTree.PatchQuads;

                if (!m_Tree.Build(m_Data.patchMin, m_Data.patchMax, m_Data.patches, leaf))
                    return false;

                m_BuiltData = m_Data;
                m_BuiltVersion = m_Data.version;
            }

#if UNITY_EDITOR
            if (m_Shader == null)
                m_Shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
#endif

            if (m_Material == null && m_Shader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
                m_Material.enableInstancing = true;

                if (!m_Material.enableInstancing)
                {
                    UnityEngine.Debug.LogError($"HB Style Terrain: '{m_Shader.name}' does not support GPU instancing " +
                                               $"here (shader supported: {m_Shader.isSupported}); the terrain is " +
                                               "not drawn.", this);
                }
            }

            return m_Material != null && m_Material.enableInstancing;
        }

        void Draw(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
                return;

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additional) &&
                additional.renderType == CameraRenderType.Overlay)
                return;

            if (!Ready())
                return;

            var origin = transform.position;
            var local = camera.transform.position - origin;
            var size = m_Data.size;

            m_Tree.SetRanges(StyleTerrains.quality.current.detailRange);

            for (var level = 0; level < StyleTerrainTree.MaxLevels; level++)
            {
                var end = m_Tree.Range(level);
                m_Ranges[level] = new Vector4(StyleTerrainTree.MorphStart(end), end, 0f, 0f);
            }

            GeometryUtility.CalculateFrustumPlanes(camera, m_Planes);

            for (var i = 0; i < 6; i++)
            {
                var normal = m_Planes[i].normal;

                m_LocalPlanes[i * 4] = normal.x;
                m_LocalPlanes[i * 4 + 1] = normal.y;
                m_LocalPlanes[i * 4 + 2] = normal.z;
                m_LocalPlanes[i * 4 + 3] = m_Planes[i].distance + Vector3.Dot(normal, origin);
            }

            m_Watch.Restart();
            m_Tree.Select(local.x, local.y, local.z, m_LocalPlanes, camera.farClipPlane, m_Nodes);
            Fill(origin, m_Full, m_Quarters);

            var block = Block(camera, origin, size);
            var bounds = new Bounds(origin + size * 0.5f, size + Vector3.up * 2f);

            Render(camera, block, bounds, ShadowCastingMode.Off, m_Full, m_Quarters);

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var shadowDistance = pipeline != null ? pipeline.shadowDistance : FallbackShadowDistance;

            m_Tree.Select(local.x, local.y, local.z, null, shadowDistance + ShadowMargin, m_Nodes);
            Fill(origin, m_ShadowFull, m_ShadowQuarters);
            m_Watch.Stop();

            Render(camera, block, bounds, ShadowCastingMode.ShadowsOnly, m_ShadowFull, m_ShadowQuarters);

            if (camera.cameraType == CameraType.Game)
            {
                StyleDebug.terrainPatches = m_Full.Count + m_Quarters.Count;
                StyleDebug.terrainSelectMs = m_Watch.Elapsed.TotalMilliseconds;
            }
        }

        void Fill(Vector3 origin, List<Matrix4x4> full, List<Matrix4x4> quarters)
        {
            full.Clear();
            quarters.Clear();

            foreach (var node in m_Nodes)
            {
                var matrix = Matrix4x4.identity;
                matrix.m00 = node.size;
                matrix.m11 = node.level + 1f;
                matrix.m22 = node.size;
                matrix.m03 = origin.x + node.x;
                matrix.m13 = origin.y;
                matrix.m23 = origin.z + node.z;

                if (node.size < m_Tree.leafSize * (1 << node.level) * 0.75f)
                    quarters.Add(matrix);
                else
                    full.Add(matrix);
            }
        }

        MaterialPropertyBlock Block(Camera camera, Vector3 origin, Vector3 size)
        {
            if (!m_Blocks.TryGetValue(camera, out var block))
            {
                PruneBlocks();
                block = new MaterialPropertyBlock();
                m_Blocks.Add(camera, block);
            }

            var terrain = m_Data.terrainData;
            var holes = terrain.holesTexture;

            block.SetTexture(s_HeightmapId, terrain.heightmapTexture);
            block.SetTexture(s_HolesId, holes != null ? holes : Texture2D.whiteTexture);
            block.SetVector(s_SizeId, new Vector4(size.x, size.y, size.z, m_Data.resolution));
            block.SetVector(s_OriginId, new Vector4(origin.x, origin.y, origin.z, m_Tree.leafSize));
            block.SetVector(s_CameraId, (Vector4)camera.transform.position + new Vector4(0f, 0f, 0f,
                                                                                          StyleTerrainTree.PatchQuads));
            block.SetVectorArray(s_RangesId, m_Ranges);
            block.SetVector(s_BrushId, m_Data.look.brush);
            block.SetVector(s_StyleId, m_Data.look.style);
            block.SetVector(s_VariationId, m_Data.look.variation);
            BindLayers(block, camera);

            return block;
        }

        void BindLayers(MaterialPropertyBlock block, Camera camera)
        {
            Texture control = m_Data.control;
            var layers = m_Data.controlLayers;
            HBStyleTerrainLayer[] preview = null;

            if (StyleTerrains.TryGetPreview(m_Data, out var state))
            {
                control = state.control;
                preview = state.layers;
                layers = preview.Length;
            }

            var albedo = m_Data.albedoArray;
            var painted = control != null && layers > 0 && albedo != null && m_Data.normalArray != null &&
                          m_Data.maskArray != null && albedo.depth >= layers;

            if (!painted)
                layers = 0;

            block.SetTexture(s_ControlId, painted ? control : EmptyArray());
            block.SetTexture(s_AlbedoId, painted ? albedo : EmptyArray());
            block.SetTexture(s_NormalId, painted ? m_Data.normalArray : EmptyArray());
            block.SetTexture(s_MaskId, painted ? m_Data.maskArray : EmptyArray());
            block.SetVector(s_LayersId, new Vector4(layers, (layers + 3) / 4, painted ? 1f : 0f,
                                                    Highlight(camera, layers)));

            for (var index = 0; index < HBStyleTerrainData.MaxLayers; index++)
            {
                var layer = index >= layers ? null : preview != null ? preview[index] : m_Data.Layer(index);

                if (layer == null)
                {
                    m_LayerST[index] = new Vector4(0.25f, 0.25f, 0f, 0f);
                    m_LayerLook[index] = new Vector4(1f, 1f, 1f, 1f);
                    m_LayerSurface[index] = new Vector4(0.2f, 0f, 1f, 1f);
                    continue;
                }

                var size = layer.size;
                var tint = layer.tint.linear;

                m_LayerST[index] = new Vector4(1f / size.x, 1f / size.y, layer.offset.x / size.x,
                                               layer.offset.y / size.y);
                m_LayerLook[index] = new Vector4(tint.r, tint.g, tint.b, layer.normalStrength);
                m_LayerSurface[index] = new Vector4(layer.smoothness, layer.metallic, layer.wetness, layer.snowCover);
            }

            block.SetVectorArray(s_LayerSTId, m_LayerST);
            block.SetVectorArray(s_LayerLookId, m_LayerLook);
            block.SetVectorArray(s_LayerSurfaceId, m_LayerSurface);
        }

        float Highlight(Camera camera, int layers)
        {
            if (camera.cameraType != CameraType.SceneView || StyleTerrains.highlightTerrain != this)
                return -1f;

            if (StyleTerrains.layerMap)
                return -2f;

            var row = StyleTerrains.highlightRow;

            return row >= 0 && row < layers ? row : -1f;
        }

        static Texture2DArray EmptyArray()
        {
            if (s_EmptyArray == null)
            {
                s_EmptyArray = new Texture2DArray(1, 1, 1, TextureFormat.RGBA32, false, true)
                {
                    name = "HB Style Terrain Empty Array",
                    hideFlags = HideFlags.HideAndDontSave
                };

                s_EmptyArray.SetPixels32(new[] { new Color32(0, 0, 0, 0) }, 0);
                s_EmptyArray.Apply(false, true);
            }

            return s_EmptyArray;
        }

        void PruneBlocks()
        {
            var stale = new List<Camera>();

            foreach (var pair in m_Blocks)
            {
                if (pair.Key == null)
                    stale.Add(pair.Key);
            }

            foreach (var camera in stale)
                m_Blocks.Remove(camera);
        }

        void Render(Camera camera, MaterialPropertyBlock block, Bounds bounds, ShadowCastingMode shadows,
                    List<Matrix4x4> full, List<Matrix4x4> quarters)
        {
            var parameters = new RenderParams(m_Material)
            {
                camera = camera,
                layer = gameObject.layer,
                worldBounds = bounds,
                matProps = block,
                receiveShadows = true,
                shadowCastingMode = shadows,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.BlendProbes
            };

            Submit(parameters, Patch(false), full);
            Submit(parameters, Patch(true), quarters);
        }

        static void Submit(RenderParams parameters, Mesh mesh, List<Matrix4x4> instances)
        {
            for (var start = 0; start < instances.Count; start += BatchSize)
            {
                var count = Mathf.Min(BatchSize, instances.Count - start);

                Graphics.RenderMeshInstanced(parameters, mesh, 0, instances, count, start);
            }
        }

        static Mesh Patch(bool quarter)
        {
            ref var mesh = ref quarter ? ref s_Quarter : ref s_Patch;

            if (mesh != null)
                return mesh;

            var quads = quarter ? StyleTerrainTree.PatchQuads / 2 : StyleTerrainTree.PatchQuads;
            var side = quads + 1;
            var vertices = new Vector3[side * side];
            var normals = new Vector3[side * side];
            var indices = new int[quads * quads * 6];

            for (var z = 0; z < side; z++)
            {
                for (var x = 0; x < side; x++)
                {
                    vertices[z * side + x] = new Vector3((float)x / quads, 0f, (float)z / quads);
                    normals[z * side + x] = Vector3.up;
                }
            }

            var index = 0;

            for (var z = 0; z < quads; z++)
            {
                for (var x = 0; x < quads; x++)
                {
                    var a = z * side + x;

                    indices[index++] = a;
                    indices[index++] = a + side;
                    indices[index++] = a + 1;
                    indices[index++] = a + 1;
                    indices[index++] = a + side;
                    indices[index++] = a + side + 1;
                }
            }

            mesh = new Mesh
            {
                name = quarter ? "HB Style Terrain Quarter Patch" : "HB Style Terrain Patch",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                normals = normals,
                triangles = indices,
                bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f)
            };

            return mesh;
        }
    }
}
