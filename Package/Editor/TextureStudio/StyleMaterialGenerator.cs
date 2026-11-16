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
        const float GroundRelief = 0.05f;

        static class Ids
        {
            internal static readonly int Height = Shader.PropertyToID("_Height");
            internal static readonly int Cells = Shader.PropertyToID("_Cells");
            internal static readonly int Frame = Shader.PropertyToID("_Frame");
            internal static readonly int FrameIn = Shader.PropertyToID("_FrameIn");
            internal static readonly int Pattern = Shader.PropertyToID("_Pattern");
            internal static readonly int GrainColour = Shader.PropertyToID("_GrainColour");
            internal static readonly int Wood = Shader.PropertyToID("_Wood");
            internal static readonly int Scatter = Shader.PropertyToID("_Scatter");
            internal static readonly int Ground = Shader.PropertyToID("_Ground");
            internal static readonly int GroundMore = Shader.PropertyToID("_GroundMore");
            internal static readonly int Specks = Shader.PropertyToID("_Specks");
            internal static readonly int SpecksMore = Shader.PropertyToID("_SpecksMore");
            internal static readonly int Detail = Shader.PropertyToID("_Detail");
            internal static readonly int NormalPreview = Shader.PropertyToID("_NormalPreview");
            internal static readonly int NormalExport = Shader.PropertyToID("_NormalExport");
            internal static readonly int Albedo = Shader.PropertyToID("_Albedo");
            internal static readonly int Mask = Shader.PropertyToID("_Mask");
            internal static readonly int Display = Shader.PropertyToID("_Display");
            internal static readonly int Encoded = Shader.PropertyToID("_Encoded");
            internal static readonly int HeightIn = Shader.PropertyToID("_HeightIn");
            internal static readonly int CellsIn = Shader.PropertyToID("_CellsIn");
            internal static readonly int DetailIn = Shader.PropertyToID("_DetailIn");
            internal static readonly int NormalIn = Shader.PropertyToID("_NormalIn");
            internal static readonly int AlbedoIn = Shader.PropertyToID("_AlbedoIn");
            internal static readonly int MaskIn = Shader.PropertyToID("_MaskIn");
            internal static readonly int Size = Shader.PropertyToID("_Size");
            internal static readonly int Layout = Shader.PropertyToID("_Layout");
            internal static readonly int Joint = Shader.PropertyToID("_Joint");
            internal static readonly int ShapeId = Shader.PropertyToID("_Shape");
            internal static readonly int ShapeMore = Shader.PropertyToID("_ShapeMore");
            internal static readonly int StoneColour = Shader.PropertyToID("_StoneColour");
            internal static readonly int GapColour = Shader.PropertyToID("_GapColour");
            internal static readonly int VariationColour = Shader.PropertyToID("_VariationColour");
            internal static readonly int VariationMore = Shader.PropertyToID("_VariationMore");
            internal static readonly int WearColour = Shader.PropertyToID("_WearColour");
            internal static readonly int DirtColour = Shader.PropertyToID("_DirtColour");
            internal static readonly int DirtMore = Shader.PropertyToID("_DirtMore");
            internal static readonly int MossColour = Shader.PropertyToID("_MossColour");
            internal static readonly int MossMore = Shader.PropertyToID("_MossMore");
            internal static readonly int Surface = Shader.PropertyToID("_Surface");
            internal static readonly int Light = Shader.PropertyToID("_Light");
            internal static readonly int ShowMode = Shader.PropertyToID("_ShowMode");
        }

        ComputeShader m_Compute;
        int m_Size;

        RenderTexture m_Height;
        RenderTexture m_Cells;
        RenderTexture m_Frame;
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
            var pattern = settings.pattern;
            var wood = settings.family == StyleMaterialFamily.Wood;
            var woodOn = wood && settings.woodOn;
            var spread = pattern == StyleMaterialPattern.Courses ? settings.lengthVariation : settings.jitter;
            var organic = settings.family == StyleMaterialFamily.Ground || settings.family == StyleMaterialFamily.Ice;

            compute.SetVector(Ids.Size, new Vector4(size, 1f / size, 0f, 0f));
            compute.SetVector(Ids.Layout, new Vector4((int)pattern, settings.units, spread, settings.perRow));
            compute.SetVector(Ids.Pattern, new Vector4(settings.randomShift ? 1f : 0f, settings.brickLength,
                                                       wood ? 1f : 0f, organic ? 1f : 0f));
            compute.SetVector(Ids.GrainColour, Colour(settings.grainColour, woodOn ? settings.woodRelief : 0f));
            compute.SetVector(Ids.Wood, woodOn
                ? new Vector4(settings.rings, settings.ringDensity, settings.fibres, settings.knots)
                : Vector4.zero);
            compute.SetVector(Ids.Joint, new Vector4(settings.gap, settings.roundness, settings.chipping,
                                                     settings.rowOffset));
            compute.SetVector(Ids.ShapeId, new Vector4(settings.bevel, settings.dome, settings.facets,
                                                       settings.heightVariation));
            compute.SetVector(Ids.ShapeMore, new Vector4(settings.tilt, settings.roughness, settings.seed, 0f));
            compute.SetVector(Ids.StoneColour, Colour(settings.stoneColour, settings.grain));
            compute.SetVector(Ids.GapColour, Colour(settings.gapColour, 0f));
            compute.SetVector(Ids.VariationColour, Colour(settings.variationColour, settings.variation));
            compute.SetVector(Ids.VariationMore, new Vector4(settings.variationShare, settings.variationOn ? 1f : 0f,
                                                             settings.heightTint, 0f));

            var ground = settings.family == StyleMaterialFamily.Ground;

            compute.SetVector(Ids.Scatter, new Vector4(settings.density, settings.pebbleSize, settings.elongation,
                pattern == StyleMaterialPattern.Voronoi ? settings.fineCracks : 0f));
            compute.SetVector(Ids.Ground, ground
                ? new Vector4(settings.groundRelief, settings.lumps, settings.ripples, settings.rippleCount)
                : new Vector4(GroundRelief, settings.units * 4, 0f, 0f));
            compute.SetVector(Ids.GroundMore, new Vector4(settings.rippleWarp, 0f, 0f, 0f));
            compute.SetVector(Ids.Specks, Colour(settings.specksColour, settings.specksOn ? settings.specks : 0f));
            compute.SetVector(Ids.SpecksMore, new Vector4(settings.specksScale, settings.specksShine,
                                                          settings.specksShine > 0f ? 1f : 0f, 0f));
            compute.SetVector(Ids.WearColour, Colour(settings.wearColour, settings.wearOn ? settings.wear : 0f));
            compute.SetVector(Ids.DirtColour, Colour(settings.dirtColour, settings.dirtOn ? settings.dirt : 0f));
            compute.SetVector(Ids.DirtMore, new Vector4(settings.dirtSpread, 0f, 0f, 0f));
            compute.SetVector(Ids.MossColour, Colour(settings.mossColour, settings.mossOn ? settings.moss : 0f));
            compute.SetVector(Ids.MossMore, new Vector4(settings.mossSpread, settings.mossScale, 0f, 0f));
            compute.SetVector(Ids.Surface, new Vector4(settings.depth, settings.smoothness, settings.gapSmoothness,
                                                       settings.metallic));
            compute.SetVector(Ids.Light, new Vector4(settings.edgeHighlight, settings.cavityShade,
                                                     settings.occlusion, settings.lightWidth));

            var cells = compute.FindKernel("Cells");
            compute.SetTexture(cells, Ids.Cells, m_Cells);
            compute.SetTexture(cells, Ids.Frame, m_Frame);
            Dispatch(cells);

            var shape = compute.FindKernel("Shape");
            compute.SetTexture(shape, Ids.CellsIn, m_Cells);
            compute.SetTexture(shape, Ids.FrameIn, m_Frame);
            compute.SetTexture(shape, Ids.Height, m_Height);
            Dispatch(shape);

            var derive = compute.FindKernel("Derive");
            compute.SetTexture(derive, Ids.HeightIn, m_Height);
            compute.SetTexture(derive, Ids.Detail, m_Detail);
            compute.SetTexture(derive, Ids.NormalPreview, normal);
            compute.SetTexture(derive, Ids.NormalExport, m_NormalExport);
            Dispatch(derive);

            var compose = compute.FindKernel("Compose");
            compute.SetTexture(compose, Ids.CellsIn, m_Cells);
            compute.SetTexture(compose, Ids.FrameIn, m_Frame);
            compute.SetTexture(compose, Ids.HeightIn, m_Height);
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

        static Vector4 Colour(Color colour, float extra)
        {
            var linear = colour.linear;

            return new Vector4(linear.r, linear.g, linear.b, extra);
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
            m_Cells = Create(RenderTextureFormat.ARGBFloat, false, "Cells");
            m_Frame = Create(RenderTextureFormat.ARGBFloat, false, "Frame");
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
            Destroy(ref m_Cells);
            Destroy(ref m_Frame);
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
