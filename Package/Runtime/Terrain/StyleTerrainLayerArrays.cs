using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    enum StyleTerrainLayerMap
    {
        Albedo,
        Normal,
        Mask
    }

    sealed class StyleTerrainLayerArrays
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/StyleTerrainLayerSlice.shader";

        const int Maps = 3;
        const int Placeholder = 4;
        const int Aniso = 8;

        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_AlbedoId = Shader.PropertyToID("_Albedo");
        static readonly int s_FillId = Shader.PropertyToID("_Fill");
        static readonly int s_HasSourceId = Shader.PropertyToID("_HasSource");
        static readonly int s_HasAlbedoId = Shader.PropertyToID("_HasAlbedo");

        static readonly string[] s_Names = { "HB Terrain Albedo", "HB Terrain Normal", "HB Terrain Mask" };

        readonly Texture[] m_Seen = new Texture[HBStyleTerrainData.MaxLayers * Maps];
        readonly Vector4[] m_Sources = new Vector4[HBStyleTerrainData.MaxLayers];
        readonly Texture[] m_Arrays = new Texture[Maps];
        readonly bool[] m_Slow = new bool[Maps];

#if UNITY_EDITOR
        readonly Hash128[] m_SeenHash = new Hash128[HBStyleTerrainData.MaxLayers * Maps];
#endif

        int m_SeenCount = -1;
        int m_SeenResolution;
        int m_SeenLimit;
        bool m_SeenSlice;
        Material m_Material;

        internal Texture albedo => m_Arrays[(int)StyleTerrainLayerMap.Albedo];

        internal Texture normal => m_Arrays[(int)StyleTerrainLayerMap.Normal];

        internal Texture mask => m_Arrays[(int)StyleTerrainLayerMap.Mask];

        internal int depth { get; private set; }

        internal Vector4[] sources => m_Sources;

        internal bool Slow(StyleTerrainLayerMap map) => m_Slow[(int)map];

        internal void Refresh(HBStyleTerrainLayer[] layers, int count, int resolution, Shader slice)
        {
            if (!Changed(layers, count, resolution, slice != null))
                return;

            Release();
            Remember(layers, count, resolution, slice != null);

            if (count <= 0)
                return;

            for (var index = 0; index < m_Sources.Length; index++)
                m_Sources[index] = Vector4.zero;

            for (var map = 0; map < Maps; map++)
                m_Arrays[map] = Assemble(layers, count, resolution, (StyleTerrainLayerMap)map, slice);

            depth = count;
        }

        internal void Release()
        {
            for (var map = 0; map < Maps; map++)
            {
                CoreUtils.Destroy(m_Arrays[map]);
                m_Arrays[map] = null;
                m_Slow[map] = false;
            }

            CoreUtils.Destroy(m_Material);
            m_Material = null;
            depth = 0;
            m_SeenCount = -1;
        }

        internal static Texture2D Source(HBStyleTerrainLayer layer, StyleTerrainLayerMap map)
        {
            if (layer == null)
                return null;

            switch (map)
            {
                case StyleTerrainLayerMap.Albedo:
                    return layer.albedo;
                case StyleTerrainLayerMap.Normal:
                    return layer.normal;
                default:
                    return layer.mask;
            }
        }

        internal static int Mips(int resolution)
        {
            return Mathf.FloorToInt(Mathf.Log(Mathf.Max(resolution, 1), 2f)) + 1;
        }

        internal static string Problem(Texture2D source, int resolution, StyleTerrainLayerMap map)
        {
            if (source.width != source.height || !Mathf.IsPowerOfTwo(source.width))
                return "is not square with a power-of-two size";

            if (source.width < resolution)
                return $"is smaller than the {resolution} texture resolution";

            if (source.mipmapCount < Offset(source, resolution) + Mips(resolution))
                return "has no mipmaps";

            if (map != StyleTerrainLayerMap.Albedo && GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat))
                return "has sRGB on";

            if (source.streamingMipmaps)
                return "uses mipmap streaming";

            return null;
        }

        static int Offset(Texture2D source, int resolution)
        {
            return Mathf.RoundToInt(Mathf.Log(Mathf.Max(source.width / Mathf.Max(resolution, 1), 1), 2f));
        }

        Texture Assemble(HBStyleTerrainLayer[] layers, int count, int resolution, StyleTerrainLayerMap map,
                         Shader slice)
        {
            var format = GraphicsFormat.None;
            var limit = QualitySettings.globalTextureMipmapLimit;
            var fits = (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0 && limit <= 0;
            var any = false;

            for (var index = 0; index < count; index++)
            {
                var source = Source(layers[index], map);

                if (source == null)
                    continue;

                any = true;

                if (Problem(source, resolution, map) != null)
                    fits = false;
                else if (format == GraphicsFormat.None)
                    format = source.graphicsFormat;
                else if (format != source.graphicsFormat)
                    fits = false;
            }

            if (!any)
                return Empty(count, map);

            if (fits)
                return Copy(layers, count, resolution, map, format);

            if (slice == null)
                return Empty(count, map);

            m_Slow[(int)map] = true;
            return Render(layers, count, Mathf.Max(resolution >> Mathf.Max(limit, 0), 1), map, slice);
        }

        Texture Copy(HBStyleTerrainLayer[] layers, int count, int resolution, StyleTerrainLayerMap map,
                     GraphicsFormat format)
        {
            var mips = Mips(resolution);
            var array = new Texture2DArray(resolution, resolution, count, format, TextureCreationFlags.MipChain, mips)
            {
                name = s_Names[(int)map],
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = Aniso,
                hideFlags = HideFlags.HideAndDontSave
            };

            array.Apply(false, true);

            for (var index = 0; index < count; index++)
            {
                var source = Source(layers[index], map);

                if (source == null)
                    continue;

                var offset = Offset(source, resolution);

                for (var mip = 0; mip < mips; mip++)
                    Graphics.CopyTexture(source, 0, offset + mip, array, index, mip);

                m_Sources[index][(int)map] = 1f;
            }

            return array;
        }

        Texture Render(HBStyleTerrainLayer[] layers, int count, int resolution, StyleTerrainLayerMap map,
                       Shader slice)
        {
            var colour = map == StyleTerrainLayerMap.Albedo ? GraphicsFormat.R8G8B8A8_SRGB
                                                            : GraphicsFormat.R8G8B8A8_UNorm;
            var descriptor = new RenderTextureDescriptor(resolution, resolution, colour, 0, Mips(resolution))
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = count,
                useMipMap = true,
                autoGenerateMips = false
            };

            var target = new RenderTexture(descriptor)
            {
                name = s_Names[(int)map],
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = Aniso,
                hideFlags = HideFlags.HideAndDontSave
            };

            target.Create();

            if (m_Material == null)
                m_Material = new Material(slice) { hideFlags = HideFlags.HideAndDontSave };

            var commands = new CommandBuffer { name = "HB Style Terrain Layer Arrays" };
            var fill = new Color(0.5f, 0.5f, 0.5f, 1f).linear;

            for (var index = 0; index < count; index++)
            {
                var layer = layers[index];
                var source = Source(layer, map);
                var albedo = layer != null ? layer.albedo : null;
                var block = new MaterialPropertyBlock();

                if (source != null)
                    block.SetTexture(s_SourceId, source);

                if (albedo != null)
                    block.SetTexture(s_AlbedoId, albedo);

                block.SetFloat(s_HasSourceId, source != null ? 1f : 0f);
                block.SetFloat(s_HasAlbedoId, albedo != null ? 1f : 0f);
                block.SetColor(s_FillId, fill);

                commands.SetRenderTarget(target, 0, CubemapFace.Unknown, index);
                commands.DrawProcedural(Matrix4x4.identity, m_Material, (int)map, MeshTopology.Triangles, 3, 1,
                                        block);

                m_Sources[index][(int)map] = 1f;
            }

            Graphics.ExecuteCommandBuffer(commands);
            commands.Release();
            target.GenerateMips();
            return target;
        }

        static Texture Empty(int count, StyleTerrainLayerMap map)
        {
            var array = new Texture2DArray(Placeholder, Placeholder, count, TextureFormat.RGBA32, false,
                                           map != StyleTerrainLayerMap.Albedo)
            {
                name = s_Names[(int)map],
                hideFlags = HideFlags.HideAndDontSave
            };

            array.Apply(false, true);
            return array;
        }

        bool Changed(HBStyleTerrainLayer[] layers, int count, int resolution, bool slice)
        {
            if (count != m_SeenCount || resolution != m_SeenResolution || slice != m_SeenSlice ||
                QualitySettings.globalTextureMipmapLimit != m_SeenLimit)
                return true;

            foreach (var array in m_Arrays)
            {
                if (array == null || array is RenderTexture target && !target.IsCreated())
                    return true;
            }

            for (var index = 0; index < count; index++)
            {
                for (var map = 0; map < Maps; map++)
                {
                    var source = Source(layers[index], (StyleTerrainLayerMap)map);
                    var slot = index * Maps + map;

                    if (source != m_Seen[slot])
                        return true;

#if UNITY_EDITOR
                    if (source != null && source.imageContentsHash != m_SeenHash[slot])
                        return true;
#endif
                }
            }

            return false;
        }

        void Remember(HBStyleTerrainLayer[] layers, int count, int resolution, bool slice)
        {
            m_SeenCount = count;
            m_SeenResolution = resolution;
            m_SeenSlice = slice;
            m_SeenLimit = QualitySettings.globalTextureMipmapLimit;

            for (var index = 0; index < count; index++)
            {
                for (var map = 0; map < Maps; map++)
                {
                    var source = Source(layers[index], (StyleTerrainLayerMap)map);
                    var slot = index * Maps + map;

                    m_Seen[slot] = source;
#if UNITY_EDITOR
                    m_SeenHash[slot] = source != null ? source.imageContentsHash : default;
#endif
                }
            }
        }
    }
}
