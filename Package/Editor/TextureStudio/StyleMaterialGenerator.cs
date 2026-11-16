using System.Collections.Generic;
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
        const int ThumbnailSize = 64;

        static class Ids
        {
            internal static readonly int Height = Shader.PropertyToID("_Height");
            internal static readonly int Cells = Shader.PropertyToID("_Cells");
            internal static readonly int Frame = Shader.PropertyToID("_Frame");
            internal static readonly int FrameIn = Shader.PropertyToID("_FrameIn");
            internal static readonly int Pattern = Shader.PropertyToID("_Pattern");
            internal static readonly int Scatter = Shader.PropertyToID("_Scatter");
            internal static readonly int Ground = Shader.PropertyToID("_Ground");
            internal static readonly int GroundMore = Shader.PropertyToID("_GroundMore");
            internal static readonly int Detail = Shader.PropertyToID("_Detail");
            internal static readonly int NormalPreview = Shader.PropertyToID("_NormalPreview");
            internal static readonly int NormalExport = Shader.PropertyToID("_NormalExport");
            internal static readonly int Albedo = Shader.PropertyToID("_Albedo");
            internal static readonly int Mask = Shader.PropertyToID("_Mask");
            internal static readonly int Display = Shader.PropertyToID("_Display");
            internal static readonly int Encoded = Shader.PropertyToID("_Encoded");
            internal static readonly int HeightIn = Shader.PropertyToID("_HeightIn");
            internal static readonly int Lift = Shader.PropertyToID("_Lift");
            internal static readonly int LiftIn = Shader.PropertyToID("_LiftIn");
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
            internal static readonly int Surface = Shader.PropertyToID("_Surface");
            internal static readonly int Light = Shader.PropertyToID("_Light");
            internal static readonly int ShowMode = Shader.PropertyToID("_ShowMode");
            internal static readonly int Blend = Shader.PropertyToID("_Blend");
            internal static readonly int BlendMore = Shader.PropertyToID("_BlendMore");
            internal static readonly int BelowAlbedo = Shader.PropertyToID("_BelowAlbedo");
            internal static readonly int BelowMask = Shader.PropertyToID("_BelowMask");
            internal static readonly int BelowHeight = Shader.PropertyToID("_BelowHeight");
            internal static readonly int StackHeight = Shader.PropertyToID("_StackHeight");
            internal static readonly int ModuleShape = Shader.PropertyToID("_ModuleShape");
            internal static readonly int ModuleColour = Shader.PropertyToID("_ModuleColour");
            internal static readonly int ModuleMore = Shader.PropertyToID("_ModuleMore");
            internal static readonly int ModuleSurface = Shader.PropertyToID("_ModuleSurface");
            internal static readonly int ModuleWhere = Shader.PropertyToID("_ModuleWhere");
            internal static readonly int ModuleMask = Shader.PropertyToID("_ModuleMask");
            internal static readonly int MaskA = Shader.PropertyToID("_MaskA");
            internal static readonly int MaskB = Shader.PropertyToID("_MaskB");

        }

        ComputeShader m_Compute;
        int m_Size;

        RenderTexture m_Height;
        RenderTexture m_ModuleHeight;
        RenderTexture m_Lift;

        readonly Vector4[] m_MaskA = new Vector4[StyleMaterialModule.MaxMasks];
        readonly Vector4[] m_MaskB = new Vector4[StyleMaterialModule.MaxMasks];
        RenderTexture m_Cells;
        RenderTexture m_Frame;
        RenderTexture m_Detail;
        RenderTexture m_NormalExport;
        RenderTexture m_Encoded;
        RenderTexture m_LayerAlbedo;
        RenderTexture m_LayerMask;
        RenderTexture m_BelowAlbedo;
        RenderTexture m_BelowMask;
        RenderTexture m_BelowHeight;
        RenderTexture m_StackHeight;

        readonly List<RenderTexture> m_Thumbnails = new List<RenderTexture>();

        internal RenderTexture albedo { get; private set; }
        internal RenderTexture normal { get; private set; }
        internal RenderTexture mask { get; private set; }
        internal RenderTexture display { get; private set; }

        internal bool ready => albedo != null;

        internal void Generate(StyleMaterialStack stack)
        {
            if (m_Compute == null)
                m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

            if (m_Compute == null)
                return;

            Allocate(stack.resolution);

            var compute = m_Compute;
            var size = (float)m_Size;
            var first = true;

            compute.SetVector(Ids.Size, new Vector4(size, 1f / size, 0f, 0f));

            for (var i = 0; i < stack.layers.Count; i++)
            {
                var layer = stack.layers[i];

                if (!layer.visible)
                    continue;

                Bind(layer);
                RunLayer();
                RunModules(layer);
                FinishLayer();
                Composite(layer, first);
                var active = RenderTexture.active;
                Graphics.Blit(m_LayerAlbedo, ThumbnailTarget(i));
                RenderTexture.active = active;
                first = false;
            }

            if (first)
            {
                Clear(albedo);
                Clear(mask);
                Clear(m_StackHeight);
            }

            Clear(m_Lift);
            Derive(m_StackHeight);

            albedo.GenerateMips();
            normal.GenerateMips();
            mask.GenerateMips();
        }

        internal Texture Thumbnail(int index)
        {
            return index >= 0 && index < m_Thumbnails.Count ? m_Thumbnails[index] : null;
        }

        void Bind(StyleMaterialSettings settings)
        {
            var compute = m_Compute;
            var pattern = settings.pattern;
            var spread = pattern == StyleMaterialPattern.Rows ? settings.lengthVariation : settings.jitter;

            compute.SetVector(Ids.Layout, new Vector4((int)pattern, settings.units, spread, settings.perRow));
            compute.SetVector(Ids.Pattern, new Vector4(settings.randomShift ? 1f : 0f, settings.brickLength, 0f,
                                                       settings.naturalGround ? 1f : 0f));
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
            compute.SetVector(Ids.Scatter, new Vector4(settings.density, settings.pebbleSize, settings.elongation,
                pattern == StyleMaterialPattern.Cells ? settings.fineCracks : 0f));
            compute.SetVector(Ids.Ground, new Vector4(settings.groundRelief, settings.lumps, settings.ripples,
                                                      settings.rippleCount));
            compute.SetVector(Ids.GroundMore, new Vector4(settings.rippleWarp, 0f, 0f, 0f));
            compute.SetVector(Ids.Surface, new Vector4(settings.depth, settings.smoothness, settings.gapSmoothness,
                                                       settings.metallic));
            compute.SetVector(Ids.Light, new Vector4(settings.edgeHighlight, settings.cavityShade,
                                                     settings.occlusion, settings.lightWidth));

        }

        void RunModule(StyleMaterialModule module)
        {
            var compute = m_Compute;
            var kernel = compute.FindKernel("Module");

            Graphics.CopyTexture(m_Height, m_ModuleHeight);

            compute.SetVector(Ids.ModuleShape, new Vector4((int)module.shape, module.amount, module.scale,
                                                           module.seed));
            compute.SetVector(Ids.ModuleColour, module.colour.linear);
            compute.SetVector(Ids.ModuleMore, new Vector4(module.size, module.variation, (int)module.height,
                                                          module.depth));
            compute.SetVector(Ids.ModuleSurface, new Vector4(module.smoothness, module.metallic, module.extra, 0f));
            var count = Mathf.Min(module.masks.Count, StyleMaterialModule.MaxMasks);

            for (var i = 0; i < StyleMaterialModule.MaxMasks; i++)
            {
                var mask = i < count ? module.masks[i] : null;

                m_MaskA[i] = mask == null
                    ? Vector4.zero
                    : new Vector4((int)mask.kind, (int)mask.blend, mask.threshold, mask.softness);
                m_MaskB[i] = mask == null
                    ? Vector4.zero
                    : new Vector4(mask.invert ? 1f : 0f, mask.scale, mask.warp, 0f);
            }

            compute.SetVector(Ids.ModuleWhere, new Vector4((int)module.area, 0f, 0f, 0f));
            compute.SetVector(Ids.ModuleMask, new Vector4(count, 0f, 0f, 0f));
            compute.SetVectorArray(Ids.MaskA, m_MaskA);
            compute.SetVectorArray(Ids.MaskB, m_MaskB);
            compute.SetTexture(kernel, Ids.CellsIn, m_Cells);
            compute.SetTexture(kernel, Ids.DetailIn, m_Detail);
            compute.SetTexture(kernel, Ids.FrameIn, m_Frame);
            compute.SetTexture(kernel, Ids.HeightIn, m_ModuleHeight);
            compute.SetTexture(kernel, Ids.Height, m_Height);
            compute.SetTexture(kernel, Ids.Lift, m_Lift);
            compute.SetTexture(kernel, Ids.Albedo, m_LayerAlbedo);
            compute.SetTexture(kernel, Ids.Mask, m_LayerMask);
            Dispatch(kernel);
        }

        void RunLayer()
        {
            var compute = m_Compute;

            Clear(m_Lift);

            var cells = compute.FindKernel("Cells");
            compute.SetTexture(cells, Ids.Cells, m_Cells);
            compute.SetTexture(cells, Ids.Frame, m_Frame);
            Dispatch(cells);

            var shape = compute.FindKernel("Shape");
            compute.SetTexture(shape, Ids.CellsIn, m_Cells);
            compute.SetTexture(shape, Ids.FrameIn, m_Frame);
            compute.SetTexture(shape, Ids.Height, m_Height);
            Dispatch(shape);
            Derive(m_Height);

            var compose = compute.FindKernel("Compose");
            compute.SetTexture(compose, Ids.CellsIn, m_Cells);
            compute.SetTexture(compose, Ids.FrameIn, m_Frame);
            compute.SetTexture(compose, Ids.HeightIn, m_Height);
            compute.SetTexture(compose, Ids.DetailIn, m_Detail);
            compute.SetTexture(compose, Ids.Albedo, m_LayerAlbedo);
            compute.SetTexture(compose, Ids.Mask, m_LayerMask);
            Dispatch(compose);
        }

        void RunModules(StyleMaterialSettings layer)
        {
            var reshaped = false;

            foreach (var module in layer.modules)
            {
                if (!module.on)
                    continue;

                if (reshaped && module.masks.Exists(ReadsDetail))
                {
                    Derive(m_Height);
                    reshaped = false;
                }

                RunModule(module);
                reshaped |= module.height != StyleModuleHeight.None;
            }
        }

        static bool ReadsDetail(StyleModuleMask mask)
        {
            return mask.kind == StyleMaskKind.Edges || mask.kind == StyleMaskKind.Cavities;
        }

        void Derive(RenderTexture height)
        {
            var compute = m_Compute;
            var derive = compute.FindKernel("Derive");

            compute.SetTexture(derive, Ids.HeightIn, height);
            compute.SetTexture(derive, Ids.LiftIn, m_Lift);
            compute.SetTexture(derive, Ids.Detail, m_Detail);
            compute.SetTexture(derive, Ids.NormalPreview, normal);
            compute.SetTexture(derive, Ids.NormalExport, m_NormalExport);
            Dispatch(derive);
        }

        void FinishLayer()
        {
            var compute = m_Compute;

            Derive(m_Height);

            var light = compute.FindKernel("Light");
            compute.SetTexture(light, Ids.DetailIn, m_Detail);
            compute.SetTexture(light, Ids.HeightIn, m_Height);
            compute.SetTexture(light, Ids.Albedo, m_LayerAlbedo);
            compute.SetTexture(light, Ids.Mask, m_LayerMask);
            Dispatch(light);
        }

        void Composite(StyleMaterialSettings layer, bool first)
        {
            var compute = m_Compute;
            var composite = compute.FindKernel("Composite");

            compute.SetVector(Ids.Blend, new Vector4(first ? 1f : layer.coverage, first ? 1f : 0f, (int)layer.blend,
                                                     layer.contrast));
            compute.SetVector(Ids.BlendMore, new Vector4(layer.offset, 0f, 0f, 0f));
            compute.SetTexture(composite, Ids.AlbedoIn, m_LayerAlbedo);
            compute.SetTexture(composite, Ids.MaskIn, m_LayerMask);
            compute.SetTexture(composite, Ids.HeightIn, m_Height);
            compute.SetTexture(composite, Ids.BelowAlbedo, m_BelowAlbedo);
            compute.SetTexture(composite, Ids.BelowMask, m_BelowMask);
            compute.SetTexture(composite, Ids.BelowHeight, m_BelowHeight);
            compute.SetTexture(composite, Ids.Albedo, albedo);
            compute.SetTexture(composite, Ids.Mask, mask);
            compute.SetTexture(composite, Ids.StackHeight, m_StackHeight);
            Dispatch(composite);

            Graphics.CopyTexture(albedo, 0, 0, m_BelowAlbedo, 0, 0);
            Graphics.CopyTexture(mask, 0, 0, m_BelowMask, 0, 0);
            Graphics.CopyTexture(m_StackHeight, m_BelowHeight);
        }

        RenderTexture ThumbnailTarget(int index)
        {
            while (m_Thumbnails.Count <= index)
            {
                var thumbnail = new RenderTexture(ThumbnailSize, ThumbnailSize, 0, RenderTextureFormat.ARGB32,
                                                  RenderTextureReadWrite.sRGB)
                {
                    name = "HB Texture Studio Thumbnail",
                    hideFlags = HideFlags.HideAndDontSave
                };

                thumbnail.Create();
                m_Thumbnails.Add(thumbnail);
            }

            return m_Thumbnails[index];
        }

        static void Clear(RenderTexture texture)
        {
            var active = RenderTexture.active;

            RenderTexture.active = texture;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = active;
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
            m_Compute.SetTexture(show, Ids.HeightIn, m_StackHeight);
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
            m_ModuleHeight = Create(RenderTextureFormat.RFloat, false, "Module Height");
            m_Lift = Create(RenderTextureFormat.RFloat, false, "Lift");
            m_Cells = Create(RenderTextureFormat.ARGBFloat, false, "Cells");
            m_Frame = Create(RenderTextureFormat.ARGBFloat, false, "Frame");
            m_Detail = Create(RenderTextureFormat.ARGBHalf, false, "Detail");
            m_NormalExport = Create(RenderTextureFormat.ARGBHalf, false, "Normal Export");
            m_Encoded = Create(RenderTextureFormat.ARGBHalf, false, "Encoded");
            m_LayerAlbedo = Create(RenderTextureFormat.ARGBHalf, false, "Layer Albedo");
            m_LayerMask = Create(RenderTextureFormat.ARGBHalf, false, "Layer Mask");
            m_BelowAlbedo = Create(RenderTextureFormat.ARGBHalf, false, "Below Albedo");
            m_BelowMask = Create(RenderTextureFormat.ARGBHalf, false, "Below Mask");
            m_BelowHeight = Create(RenderTextureFormat.RFloat, false, "Below Height");
            m_StackHeight = Create(RenderTextureFormat.RFloat, false, "Stack Height");
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
            Destroy(ref m_ModuleHeight);
            Destroy(ref m_Lift);
            Destroy(ref m_Cells);
            Destroy(ref m_Frame);
            Destroy(ref m_Detail);
            Destroy(ref m_NormalExport);
            Destroy(ref m_Encoded);
            Destroy(ref m_LayerAlbedo);
            Destroy(ref m_LayerMask);
            Destroy(ref m_BelowAlbedo);
            Destroy(ref m_BelowMask);
            Destroy(ref m_BelowHeight);
            Destroy(ref m_StackHeight);

            for (var i = 0; i < m_Thumbnails.Count; i++)
            {
                var thumbnail = m_Thumbnails[i];
                Destroy(ref thumbnail);
            }

            m_Thumbnails.Clear();

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
