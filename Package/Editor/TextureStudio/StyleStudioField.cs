using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleStudioField
    {
        const int Resolution = 129;
        const int ControlSize = 16;
        const float Side = 24f;
        const float Rise = 2.5f;
        const int GroundSize = 1024;
        const int HeadWidth = 1024;
        const int HeadHeight = 512;
        const float GrassDensity = 1f;
        const float GrassHeight = 0.5f;
        const int AlbedoPass = 0;
        const int NormalPass = 1;
        const int MaskPass = 2;
        const string SliceShader = "Packages/com.hiddenbull.urpstyle/Editor/Terrain/StyleTerrainLayerSlice.shader";

        static readonly Vector3 s_Origin = new Vector3(5000f, 0f, 5000f);
        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_AlbedoId = Shader.PropertyToID("_Albedo");
        static readonly int s_FillId = Shader.PropertyToID("_Fill");
        static readonly int s_HasSourceId = Shader.PropertyToID("_HasSource");
        static readonly int s_HasAlbedoId = Shader.PropertyToID("_HasAlbedo");

        GameObject m_Object;
        HBStyleTerrainData m_Data;
        TerrainData m_Heights;
        Texture2DArray m_Control;
        Texture2DArray m_Albedo;
        Texture2DArray m_Normal;
        Texture2DArray m_Mask;
        Material m_Slice;
        Texture2DArray m_Heads;
        Texture2D m_CardKey;
        Texture2D m_FlowerKey;
        int m_Variants = 1;

        internal HBStyleTerrainLayer layer { get; private set; }

        internal bool built => m_Object != null;

        internal Color groundColour { get; private set; } = new Color(0.5f, 0.5f, 0.5f, 1f).linear;

        internal Vector3 centre => s_Origin + new Vector3(Side * 0.5f, Height(0.5f, 0.5f) * Rise, Side * 0.5f);

        internal void Build(PreviewRenderUtility utility)
        {
            if (m_Object != null)
                return;

            m_Heights = new TerrainData { name = "HB Studio Field", hideFlags = HideFlags.HideAndDontSave };
            m_Heights.heightmapResolution = Resolution;
            m_Heights.size = new Vector3(Side, Rise, Side);
            m_Heights.SetHeights(0, 0, Hills());
            m_Heights.SetHoles(0, 0, Solid());

            m_Control = new Texture2DArray(ControlSize, ControlSize, 1, TextureFormat.RGBA32, false, true)
            {
                name = "HB Studio Field Control",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var full = new Color32[ControlSize * ControlSize];

            for (var i = 0; i < full.Length; i++)
                full[i] = new Color32(255, 0, 0, 0);

            m_Control.SetPixels32(full, 0, 0);
            m_Control.Apply(false, false);

            m_Data = ScriptableObject.CreateInstance<HBStyleTerrainData>();
            m_Data.hideFlags = HideFlags.HideAndDontSave;
            m_Data.Assign(m_Heights);
            m_Albedo = NewArray("Albedo");
            m_Normal = NewArray("Normal");
            m_Mask = NewArray("Mask");
            m_Data.SetTextures(m_Albedo, m_Normal, m_Mask, string.Empty);
            MakeHeads();
            SetGround(null);

            m_Object = new GameObject("HB Studio Field")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = FirstLayer(utility.camera.cullingMask)
            };

            m_Object.SetActive(false);
            m_Object.transform.position = s_Origin;
            m_Object.AddComponent<TerrainCollider>();

            var terrain = m_Object.AddComponent<HBStyleTerrain>();

            terrain.onlyCamera = utility.camera;
            terrain.data = m_Data;
            utility.AddSingleGO(m_Object);
            m_Object.SetActive(true);
        }

        internal void SetGround(HBStyleTerrainLayer source)
        {
            if (m_Data == null)
                return;

            var old = layer;

            layer = ScriptableObject.CreateInstance<HBStyleTerrainLayer>();
            layer.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            if (source != null)
            {
                layer.tint = source.tint;
                layer.size = source.size;
                layer.offset = source.offset;
                layer.normalStrength = source.normalStrength;
                layer.smoothness = source.smoothness;
                layer.metallic = source.metallic;
            }

            layer.grassDensity = GrassDensity;
            layer.grassHeight = old != null ? old.grassHeight : GrassHeight;
            layer.grassCardTexture = m_CardKey;
            layer.grassCardShare = old != null ? old.grassCardShare : 1f;
            layer.flowerShare = old != null ? old.flowerShare : 1f;
            layer.flowerTexture = m_FlowerKey;
            layer.flowerVariants = m_Variants;

            m_Data.SetControl(m_Control, new[] { layer });
            Destroy(ref old);

            if (m_Slice == null)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(SliceShader);

                if (shader == null)
                    return;

                m_Slice = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            var albedo = source != null ? source.albedo : null;

            m_Slice.SetTexture(s_AlbedoId, albedo);
            m_Slice.SetFloat(s_HasAlbedoId, albedo != null ? 1f : 0f);
            m_Slice.SetColor(s_FillId, new Color(0.5f, 0.5f, 0.5f, 1f).linear);

            Slice(AlbedoPass, albedo, m_Albedo);
            Slice(NormalPass, source != null ? source.normal : null, m_Normal);
            Slice(MaskPass, source != null ? source.mask : null, m_Mask);
        }

        static Color Average(RenderTexture source)
        {
            var single = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGBHalf,
                                                    RenderTextureReadWrite.Linear);
            var pixel = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            var active = RenderTexture.active;

            try
            {
                Graphics.CopyTexture(source, 0, source.mipmapCount - 1, single, 0, 0);
                RenderTexture.active = single;
                pixel.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0, false);
                pixel.Apply(false);
                return pixel.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(single);
                Object.DestroyImmediate(pixel);
            }
        }

        void MakeHeads()
        {
            m_Heads = new Texture2DArray(HeadWidth, HeadHeight, 2, TextureFormat.RGBAHalf, true, true)
            {
                name = "HB Studio Field Heads",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                wrapMode = TextureWrapMode.Clamp
            };

            m_Heads.Apply(false, true);
            m_CardKey = new Texture2D(1, 1) { name = "HB Studio Card", hideFlags = HideFlags.HideAndDontSave };
            m_FlowerKey = new Texture2D(1, 1) { name = "HB Studio Flower", hideFlags = HideFlags.HideAndDontSave };
            m_Data.SetFlowerTextures(m_Heads, new[] { m_CardKey, m_FlowerKey });
        }

        internal void SetCard(RenderTexture card)
        {
            SetHead(0, card);
        }

        internal void SetFlower(RenderTexture flower, int variants)
        {
            m_Variants = Mathf.Clamp(variants, 1, 8);
            SetHead(1, flower);

            if (layer != null)
                layer.flowerVariants = m_Variants;
        }

        void SetHead(int slice, RenderTexture source)
        {
            if (m_Heads == null || source == null)
                return;

            var stretched = new RenderTexture(HeadWidth, HeadHeight, 0, RenderTextureFormat.ARGBHalf,
                                              RenderTextureReadWrite.Linear)
            {
                useMipMap = true,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave
            };

            var active = RenderTexture.active;

            stretched.Create();
            Graphics.Blit(source, stretched);
            RenderTexture.active = active;
            stretched.GenerateMips();

            for (var mip = 0; mip < stretched.mipmapCount && mip < m_Heads.mipmapCount; mip++)
                Graphics.CopyTexture(stretched, 0, mip, m_Heads, slice, mip);

            stretched.Release();
            Object.DestroyImmediate(stretched);
        }

        void Slice(int pass, Texture source, Texture2DArray target)
        {
            var temporary = new RenderTexture(GroundSize, GroundSize, 0, RenderTextureFormat.ARGBHalf,
                                              RenderTextureReadWrite.Linear)
            {
                useMipMap = true,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave
            };

            temporary.Create();
            m_Slice.SetTexture(s_SourceId, source);
            m_Slice.SetFloat(s_HasSourceId, source != null ? 1f : 0f);

            var commands = new CommandBuffer { name = "HB Studio Field Ground" };

            commands.SetRenderTarget(temporary);
            commands.DrawProcedural(Matrix4x4.identity, m_Slice, pass, MeshTopology.Triangles, 3);
            commands.GenerateMips(temporary);
            Graphics.ExecuteCommandBuffer(commands);
            commands.Release();

            for (var mip = 0; mip < temporary.mipmapCount && mip < target.mipmapCount; mip++)
                Graphics.CopyTexture(temporary, 0, mip, target, 0, mip);

            if (pass == AlbedoPass)
                groundColour = Average(temporary);

            temporary.Release();
            Object.DestroyImmediate(temporary);
        }

        static int FirstLayer(int mask)
        {
            for (var layer = 0; layer < 32; layer++)
            {
                if ((mask & (1 << layer)) != 0)
                    return layer;
            }

            return 0;
        }

        static bool[,] Solid()
        {
            var cells = Resolution - 1;
            var solid = new bool[cells, cells];

            for (var z = 0; z < cells; z++)
            {
                for (var x = 0; x < cells; x++)
                    solid[z, x] = true;
            }

            return solid;
        }

        static float[,] Hills()
        {
            var heights = new float[Resolution, Resolution];

            for (var z = 0; z < Resolution; z++)
            {
                for (var x = 0; x < Resolution; x++)
                    heights[z, x] = Height((float)x / (Resolution - 1), (float)z / (Resolution - 1));
            }

            return heights;
        }

        static float Height(float u, float v)
        {
            var broad = Mathf.PerlinNoise(u * 2.2f + 3.1f, v * 2.2f + 7.4f);
            var small = Mathf.PerlinNoise(u * 5.3f + 11.2f, v * 5.3f + 2.6f);

            return Mathf.Clamp01(broad * 0.75f + small * 0.25f);
        }

        static Texture2DArray NewArray(string name)
        {
            var array = new Texture2DArray(GroundSize, GroundSize, 1, TextureFormat.RGBAHalf, true, true)
            {
                name = "HB Studio Field " + name,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
                wrapMode = TextureWrapMode.Repeat
            };

            array.Apply(false, true);
            return array;
        }

        void DestroyArrays()
        {
            Destroy(ref m_Albedo);
            Destroy(ref m_Normal);
            Destroy(ref m_Mask);
        }

        static void Destroy<T>(ref T target) where T : Object
        {
            if (target != null)
                Object.DestroyImmediate(target);

            target = null;
        }

        internal void Release()
        {
            Destroy(ref m_Object);
            DestroyArrays();
            Destroy(ref m_Control);
            Destroy(ref m_Data);
            Destroy(ref m_Heights);
            Destroy(ref m_Slice);
            Destroy(ref m_Heads);
            Destroy(ref m_CardKey);
            Destroy(ref m_FlowerKey);

            var owned = layer;

            Destroy(ref owned);
            layer = null;
        }
    }
}
