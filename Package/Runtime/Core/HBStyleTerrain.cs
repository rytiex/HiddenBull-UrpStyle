using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleTerrainLayerResolution
    {
        [InspectorName("256")] Size256 = 256,
        [InspectorName("512")] Size512 = 512,
        [InspectorName("1024")] Size1024 = 1024,
        [InspectorName("2048")] Size2048 = 2048
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Terrain))]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Terrain/HB Style Terrain")]
    public sealed class HBStyleTerrain : MonoBehaviour
    {
        public const string ShaderName = "HB URP Style/Terrain";
        public const int Limit = HBStyleTerrainProfile.MaxLayers;
        public const float BasemapDistance = 20000f;

        static readonly List<HBStyleTerrain> s_Enabled = new List<HBStyleTerrain>();

        static readonly int s_ControlId = Shader.PropertyToID("_HB_TerrainControl");
        static readonly int s_AlbedoId = Shader.PropertyToID("_HB_LayerAlbedo");
        static readonly int s_NormalId = Shader.PropertyToID("_HB_LayerNormal");
        static readonly int s_MaskId = Shader.PropertyToID("_HB_LayerMask");
        static readonly int s_LayerCountId = Shader.PropertyToID("_HB_TerrainLayerCount");
        static readonly int s_ControlCountId = Shader.PropertyToID("_HB_TerrainControlCount");
        static readonly int s_ControlTexelId = Shader.PropertyToID("_HB_TerrainControlTexel");
        static readonly int s_TerrainRectId = Shader.PropertyToID("_HB_TerrainRect");
        static readonly int s_TilingId = Shader.PropertyToID("_HB_LayerST");
        static readonly int s_LookId = Shader.PropertyToID("_HB_LayerLook");
        static readonly int s_WeatherId = Shader.PropertyToID("_HB_LayerWeather");
        static readonly int s_VaryId = Shader.PropertyToID("_HB_LayerVary");
        static readonly int s_VaryShapeId = Shader.PropertyToID("_HB_LayerVaryShape");
        static readonly int s_VaryExtraId = Shader.PropertyToID("_HB_LayerVaryExtra");
        static readonly int s_MaskScaleId = Shader.PropertyToID("_HB_LayerMaskScale");
        static readonly int s_MaskOffsetId = Shader.PropertyToID("_HB_LayerMaskOffset");

        [SerializeField]
        [Tooltip("The layers of this terrain and the rules that place them. Edit it in Tools > HiddenBull > " +
                 "URP Style > Terrain Layers.")]
        HBStyleTerrainProfile m_Profile;

        [SerializeField]
        [Tooltip("Bakes the layers again whenever the terrain is sculpted or the profile changes. Hand painting " +
                 "is kept.")]
        bool m_AutoBake = true;

        [SerializeField]
        [Tooltip("Size every layer's textures are resampled to for the shader. Memory grows with the square of " +
                 "it: 8 layers take about 32 MB at 1024.")]
        HBStyleTerrainLayerResolution m_LayerResolution = HBStyleTerrainLayerResolution.Size1024;

        [SerializeField]
        [HideInInspector]
        HBStyleTerrainLayerArrays m_Arrays;

        readonly Vector4[] m_Tiling = new Vector4[Limit];
        readonly Vector4[] m_Look = new Vector4[Limit];
        readonly Vector4[] m_Weather = new Vector4[Limit];
        readonly Vector4[] m_Vary = new Vector4[Limit];
        readonly Vector4[] m_VaryShape = new Vector4[Limit];
        readonly Vector4[] m_VaryExtra = new Vector4[Limit];
        readonly Vector4[] m_MaskScale = new Vector4[Limit];
        readonly Vector4[] m_MaskOffset = new Vector4[Limit];
        readonly Vector4[] m_Grass = new Vector4[Limit];
        readonly Vector4[] m_GrassTip = new Vector4[Limit];
        readonly Vector4[] m_Flowers = new Vector4[Limit];

        Terrain m_Terrain;
        Texture2DArray m_Control;
        Texture m_Preview;
        MaterialPropertyBlock m_Block;
        int m_Count;

        public HBStyleTerrainProfile profile
        {
            get => m_Profile;
            set
            {
                m_Profile = value;
                Bind();
            }
        }

        public bool autoBake
        {
            get => m_AutoBake;
            set => m_AutoBake = value;
        }

        public int layerResolution => (int)m_LayerResolution;

        public HBStyleTerrainLayerArrays arrays
        {
            get => m_Arrays;
            set
            {
                m_Arrays = value;
                Bind();
            }
        }

        internal Texture2DArray control => m_Control;

        internal static IReadOnlyList<HBStyleTerrain> active => s_Enabled;

        internal int layerCount => m_Count;
        internal Texture layerControl => m_Preview != null ? m_Preview : m_Control;
        internal Vector4[] tiling => m_Tiling;
        internal Vector4[] look => m_Look;
        internal Vector4[] weather => m_Weather;
        internal Vector4[] vary => m_Vary;
        internal Vector4[] varyShape => m_VaryShape;
        internal Vector4[] varyExtra => m_VaryExtra;
        internal Vector4[] grass => m_Grass;
        internal Vector4[] grassTip => m_GrassTip;
        internal Vector4[] flowers => m_Flowers;

        internal bool ready => m_Terrain != null && layerControl != null && m_Arrays != null && m_Arrays.valid &&
                               m_Count > 0;

        internal TerrainLayer[] syncedLayers { get; set; }

        TerrainData data => m_Terrain != null ? m_Terrain.terrainData : null;

        void OnEnable()
        {
            m_Terrain = GetComponent<Terrain>();
            s_Enabled.Remove(this);
            s_Enabled.Add(this);
#if UNITY_EDITOR
            HBStyleTerrainProfile.edited += OnProfileEdited;
#endif
            Refresh();
            RefreshTrees();
        }

        void OnDisable()
        {
            s_Enabled.Remove(this);
            StyleBillboardSystem.ClearTerrain(m_Terrain);
#if UNITY_EDITOR
            HBStyleTerrainProfile.edited -= OnProfileEdited;
#endif
            if (m_Terrain != null)
                m_Terrain.SetSplatMaterialPropertyBlock(null);

            Release();
        }

        void OnTerrainChanged(TerrainChangedFlags flags)
        {
            const TerrainChangedFlags trees = TerrainChangedFlags.TreeInstances | TerrainChangedFlags.Heightmap |
                                              TerrainChangedFlags.DelayedHeightmapUpdate;

            if ((flags & trees) != 0 && isActiveAndEnabled)
                RefreshTrees();
        }

        public void RefreshTrees()
        {
#if UNITY_EDITOR
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene))
                return;
#endif
            StyleBillboardSystem.SetTerrain(m_Terrain);
        }

        void OnValidate()
        {
            if (isActiveAndEnabled && m_Terrain != null)
                Bind();
        }

#if UNITY_EDITOR
        void OnProfileEdited(HBStyleTerrainProfile edited)
        {
            if (edited == m_Profile)
                Bind();
        }
#endif

        public void Refresh()
        {
            RefreshControl();
            Bind();
        }

        public void RefreshControl()
        {
            var terrainData = data;

            if (terrainData == null)
                return;

            var maps = terrainData.alphamapTextures;

            if (maps.Length == 0)
            {
                Release();
                return;
            }

            var resolution = maps[0].width;

            if (m_Control == null || m_Control.width != resolution || m_Control.depth != maps.Length ||
                m_Control.format != maps[0].format)
            {
                Release();

                m_Control = new Texture2DArray(resolution, resolution, maps.Length, maps[0].format, false, true)
                {
                    name = "HB Terrain Control",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var copy = (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0;

            for (var slice = 0; slice < maps.Length; slice++)
            {
                if (copy)
                    Graphics.CopyTexture(maps[slice], 0, 0, m_Control, slice, 0);
                else
                    m_Control.SetPixels32(maps[slice].GetPixels32(), slice);
            }

            if (!copy)
                m_Control.Apply(false);
        }

        internal void SetPreview(Texture control)
        {
            m_Preview = control;
            Bind();
        }

        public void Bind()
        {
            if (m_Terrain == null)
                return;

            var terrainData = data;
            var control = m_Preview != null ? m_Preview : m_Control;

            if (terrainData == null || control == null || m_Arrays == null || !m_Arrays.valid)
            {
                m_Count = 0;
                m_Terrain.SetSplatMaterialPropertyBlock(null);
                return;
            }

            var layers = m_Arrays.layers;
            var count = Mathf.Min(Mathf.Min(layers.Length, Limit), m_Arrays.albedo.depth);
            var origin = m_Terrain.GetPosition();

            for (var index = 0; index < count; index++)
                Fill(index, layers[index], origin);

            m_Count = count;

            var material = m_Terrain.materialTemplate;

            if (material != null && material.shader != null && material.shader.name == ShaderName &&
                m_Terrain.basemapDistance < BasemapDistance)
            {
                m_Terrain.basemapDistance = BasemapDistance;
            }

            var slices = control is RenderTexture target ? target.volumeDepth : ((Texture2DArray)control).depth;

            m_Block ??= new MaterialPropertyBlock();
            m_Block.Clear();
            m_Block.SetTexture(s_ControlId, control);
            m_Block.SetTexture(s_AlbedoId, m_Arrays.albedo);
            m_Block.SetTexture(s_NormalId, m_Arrays.normal);
            m_Block.SetTexture(s_MaskId, m_Arrays.mask);
            m_Block.SetFloat(s_LayerCountId, count);
            m_Block.SetFloat(s_ControlCountId, Mathf.Min(slices, (count + 3) / 4));
            m_Block.SetVector(s_ControlTexelId, new Vector4(1f / control.width, 1f / control.height,
                                                            control.width, control.height));
            m_Block.SetVector(s_TerrainRectId, new Vector4(origin.x, origin.z, 1f / terrainData.size.x,
                                                           1f / terrainData.size.z));
            m_Block.SetVectorArray(s_TilingId, m_Tiling);
            m_Block.SetVectorArray(s_LookId, m_Look);
            m_Block.SetVectorArray(s_WeatherId, m_Weather);
            m_Block.SetVectorArray(s_VaryId, m_Vary);
            m_Block.SetVectorArray(s_VaryShapeId, m_VaryShape);
            m_Block.SetVectorArray(s_VaryExtraId, m_VaryExtra);
            m_Block.SetVectorArray(s_MaskScaleId, m_MaskScale);
            m_Block.SetVectorArray(s_MaskOffsetId, m_MaskOffset);

            m_Terrain.SetSplatMaterialPropertyBlock(m_Block);
        }

        void Fill(int index, TerrainLayer layer, Vector3 origin)
        {
            var row = Find(layer);
            var tile = layer != null ? layer.tileSize : Vector2.one;
            var offset = layer != null ? layer.tileOffset : Vector2.zero;
            var remap = layer != null ? layer.diffuseRemapMax : Vector4.one;
            var tint = row != null ? row.tint.linear : Color.white;

            tile = new Vector2(Mathf.Max(tile.x, 0.01f), Mathf.Max(tile.y, 0.01f));

            m_Tiling[index] = new Vector4(1f / tile.x, 1f / tile.y, (offset.x - origin.x) / tile.x,
                                          (offset.y - origin.z) / tile.y);
            m_Look[index] = new Vector4(tint.r * remap.x, tint.g * remap.y, tint.b * remap.z,
                                        layer != null ? layer.normalScale : 1f);
            FillMask(index, layer);
            m_Weather[index] = new Vector4(row != null ? row.wetness : 0.5f, row != null ? row.snowCover : 1f, 0f, 0f);
            m_Vary[index] = row != null ? (Vector4)row.variation.linear : Vector4.zero;
            m_VaryShape[index] = row != null ? row.variationShape : Vector4.zero;
            m_VaryExtra[index] = row != null
                ? new Vector4(row.variationSize, row.variationCover, 0f, 0f)
                : new Vector4(30f, 0f, 0f, 0f);
            m_Grass[index] = row != null
                ? new Vector4(row.grass, row.grassHeight, row.grassWidth, row.ownTip ? 1f : 0f)
                : Vector4.zero;
            m_GrassTip[index] = row != null ? (Vector4)row.tipColor.linear : Vector4.zero;
            m_Flowers[index] = row != null ? new Vector4(row.flowers, row.flowerSize, 0f, 0f) : Vector4.zero;
        }

        void FillMask(int index, TerrainLayer layer)
        {
            if (layer == null)
            {
                m_MaskScale[index] = new Vector4(0f, 0f, 1f, 0f);
                m_MaskOffset[index] = new Vector4(0f, 1f, 0f, 0.5f);
                return;
            }

            var low = layer.maskMapRemapMin;
            var high = layer.maskMapRemapMax;

            if (layer.maskMapTexture != null)
            {
                m_MaskScale[index] = high - low;
                m_MaskOffset[index] = low;
                return;
            }

            var diffuse = layer.diffuseTexture;
            var alpha = layer.smoothnessSource == TerrainLayerSmoothnessSource.DiffuseAlphaChannel && diffuse != null &&
                        GraphicsFormatUtility.HasAlphaChannel(diffuse.graphicsFormat);

            m_MaskScale[index] = new Vector4(0f, 0f, 1f, alpha ? 1f : 0f);
            m_MaskOffset[index] = new Vector4(layer.metallic, high.y, 0f, alpha ? 0f : layer.smoothness);
        }

        HBStyleTerrainProfileLayer Find(TerrainLayer layer)
        {
            if (m_Profile == null || layer == null)
                return null;

            foreach (var row in m_Profile.layers)
            {
                if (row != null && row.layer == layer)
                    return row;
            }

            return null;
        }

        void Release()
        {
            if (m_Control == null)
                return;

            if (Application.isPlaying)
                Destroy(m_Control);
            else
                DestroyImmediate(m_Control);

            m_Control = null;
        }
    }
}
