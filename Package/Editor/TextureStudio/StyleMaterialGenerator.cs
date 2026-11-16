using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleMaterialMap
    {
        Albedo,
        Normal,
        Mask,
        Height,
        Occlusion,
        Edges
    }

    sealed class StyleMaterialGenerator
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleMaterialStudio.compute";

        static class Ids
        {
            internal static readonly int Height = Shader.PropertyToID("_Height");
            internal static readonly int Colour = Shader.PropertyToID("_Colour");
            internal static readonly int Detail = Shader.PropertyToID("_Detail");
            internal static readonly int NormalPreview = Shader.PropertyToID("_NormalPreview");
            internal static readonly int NormalExport = Shader.PropertyToID("_NormalExport");
            internal static readonly int Albedo = Shader.PropertyToID("_Albedo");
            internal static readonly int Mask = Shader.PropertyToID("_Mask");
            internal static readonly int Display = Shader.PropertyToID("_Display");
            internal static readonly int Encoded = Shader.PropertyToID("_Encoded");
            internal static readonly int HeightIn = Shader.PropertyToID("_HeightIn");
            internal static readonly int ColourIn = Shader.PropertyToID("_ColourIn");
            internal static readonly int DetailIn = Shader.PropertyToID("_DetailIn");
            internal static readonly int NormalIn = Shader.PropertyToID("_NormalIn");
            internal static readonly int AlbedoIn = Shader.PropertyToID("_AlbedoIn");
            internal static readonly int MaskIn = Shader.PropertyToID("_MaskIn");
            internal static readonly int Size = Shader.PropertyToID("_Size");
            internal static readonly int Pattern = Shader.PropertyToID("_Pattern");
            internal static readonly int PatternMore = Shader.PropertyToID("_PatternMore");
            internal static readonly int Tint = Shader.PropertyToID("_Tint");
            internal static readonly int GapTint = Shader.PropertyToID("_GapTint");
            internal static readonly int Surface = Shader.PropertyToID("_Surface");
            internal static readonly int Light = Shader.PropertyToID("_Light");
            internal static readonly int ShowMode = Shader.PropertyToID("_ShowMode");
        }

        ComputeShader m_Compute;
        int m_Size;

        RenderTexture m_Height;
        RenderTexture m_Colour;
        RenderTexture m_Detail;
        RenderTexture m_NormalExport;
        RenderTexture m_Encoded;

        internal RenderTexture albedo { get; private set; }
        internal RenderTexture normal { get; private set; }
        internal RenderTexture mask { get; private set; }
        internal RenderTexture display { get; private set; }

        internal bool ready => albedo != null;

        internal void Generate(StyleMaterialSettings settings)
        {
            if (m_Compute == null)
                m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

            if (m_Compute == null)
                return;

            Allocate(settings.resolution);

            var compute = m_Compute;
            var size = (float)m_Size;

            compute.SetVector(Ids.Size, new Vector4(size, 1f / size, 0f, 0f));
            compute.SetVector(Ids.Pattern, new Vector4(settings.tiles, settings.gap, settings.bevel,
                                                       settings.heightVariation));
            compute.SetVector(Ids.PatternMore, new Vector4(settings.seed, settings.variation, 0f, 0f));
            compute.SetVector(Ids.Tint, settings.colour.linear);
            compute.SetVector(Ids.GapTint, settings.gapColour.linear);
            compute.SetVector(Ids.Surface, new Vector4(settings.depth, settings.smoothness, settings.gapSmoothness,
                                                       settings.metallic));
            compute.SetVector(Ids.Light, new Vector4(settings.edgeHighlight, settings.cavityShade,
                                                     settings.occlusion, settings.lightWidth));

            var tiles = compute.FindKernel("Tiles");
            compute.SetTexture(tiles, Ids.Height, m_Height);
            compute.SetTexture(tiles, Ids.Colour, m_Colour);
            Dispatch(tiles);

            var derive = compute.FindKernel("Derive");
            compute.SetTexture(derive, Ids.HeightIn, m_Height);
            compute.SetTexture(derive, Ids.Detail, m_Detail);
            compute.SetTexture(derive, Ids.NormalPreview, normal);
            compute.SetTexture(derive, Ids.NormalExport, m_NormalExport);
            Dispatch(derive);

            var compose = compute.FindKernel("Compose");
            compute.SetTexture(compose, Ids.HeightIn, m_Height);
            compute.SetTexture(compose, Ids.ColourIn, m_Colour);
            compute.SetTexture(compose, Ids.DetailIn, m_Detail);
            compute.SetTexture(compose, Ids.Albedo, albedo);
            compute.SetTexture(compose, Ids.Mask, mask);
            Dispatch(compose);

            albedo.GenerateMips();
            normal.GenerateMips();
            mask.GenerateMips();
        }

        internal void Show(StyleMaterialMap map)
        {
            if (!ready)
                return;

            var show = m_Compute.FindKernel("Show");
            m_Compute.SetInt(Ids.ShowMode, (int)map);
            m_Compute.SetTexture(show, Ids.AlbedoIn, albedo);
            m_Compute.SetTexture(show, Ids.NormalIn, m_NormalExport);
            m_Compute.SetTexture(show, Ids.MaskIn, mask);
            m_Compute.SetTexture(show, Ids.HeightIn, m_Height);
            m_Compute.SetTexture(show, Ids.DetailIn, m_Detail);
            m_Compute.SetTexture(show, Ids.Display, display);
            Dispatch(show);
        }

        internal RenderTexture Export(StyleMaterialMap map)
        {
            if (!ready)
                return null;

            if (map == StyleMaterialMap.Normal)
                return m_NormalExport;

            if (map == StyleMaterialMap.Mask)
                return mask;

            var encode = m_Compute.FindKernel("Encode");
            m_Compute.SetTexture(encode, Ids.AlbedoIn, albedo);
            m_Compute.SetTexture(encode, Ids.Encoded, m_Encoded);
            Dispatch(encode);
            return m_Encoded;
        }

        void Dispatch(int kernel)
        {
            var groups = (m_Size + 7) / 8;

            m_Compute.Dispatch(kernel, groups, groups, 1);
        }

        void Allocate(int size)
        {
            size = Mathf.Clamp(Mathf.ClosestPowerOfTwo(size), 64, 4096);

            if (size == m_Size && ready)
                return;

            Release();
            m_Size = size;

            m_Height = Create(RenderTextureFormat.RFloat, false, "Height");
            m_Colour = Create(RenderTextureFormat.ARGBHalf, false, "Colour");
            m_Detail = Create(RenderTextureFormat.ARGBHalf, false, "Detail");
            m_NormalExport = Create(RenderTextureFormat.ARGBHalf, false, "Normal Export");
            m_Encoded = Create(RenderTextureFormat.ARGBHalf, false, "Encoded");
            normal = Create(RenderTextureFormat.ARGBHalf, true, "Normal");
            albedo = Create(RenderTextureFormat.ARGBHalf, true, "Albedo");
            mask = Create(RenderTextureFormat.ARGBHalf, true, "Mask");
            display = Create(RenderTextureFormat.ARGBHalf, false, "Display");
        }

        RenderTexture Create(RenderTextureFormat format, bool mips, string name)
        {
            var texture = new RenderTexture(m_Size, m_Size, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "HB Texture Studio " + name,
                enableRandomWrite = true,
                useMipMap = mips,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear,
                anisoLevel = mips ? 8 : 0,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.Create();
            return texture;
        }

        internal void Release()
        {
            Destroy(ref m_Height);
            Destroy(ref m_Colour);
            Destroy(ref m_Detail);
            Destroy(ref m_NormalExport);
            Destroy(ref m_Encoded);

            var texture = albedo;
            Destroy(ref texture);
            albedo = null;
            texture = normal;
            Destroy(ref texture);
            normal = null;
            texture = mask;
            Destroy(ref texture);
            mask = null;
            texture = display;
            Destroy(ref texture);
            display = null;

            m_Size = 0;
        }

        static void Destroy(ref RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            Object.DestroyImmediate(texture);
            texture = null;
        }
    }
}
