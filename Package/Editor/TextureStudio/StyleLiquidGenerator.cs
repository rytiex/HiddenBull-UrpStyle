using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleLiquidRecipe
    {
        Water,
        Acid,
        Lava
    }

    enum StyleRippleKind
    {
        Waves,
        Domes,
        Flow,
        Cells,
        Noise,
        Plates,
        Blobs,
        Swirl,
        Rocks
    }

    enum StyleLiquidMap
    {
        Normal,
        Height,
        Tone,
        Solid,
        Lines,
        Colour
    }

    enum StyleLiquidChannel
    {
        Tone,
        Solid,
        Lines
    }

    enum StyleLiquidBlend
    {
        Add,
        Multiply,
        Subtract,
        Max
    }

    [Serializable]
    sealed class StyleLiquidTarget
    {
        public bool on;
        public StyleLiquidBlend blend;
        public float threshold = 0.5f;
        public float feather = 1f;
        public float opacity = 1f;
        public bool invert;

        internal void Reset()
        {
            blend = StyleLiquidBlend.Add;
            threshold = 0.5f;
            feather = 1f;
            opacity = 1f;
            invert = false;
        }
    }

    [Serializable]
    sealed class StyleRippleLayer
    {
        public string label = "Waves";
        public bool visible = true;
        public StyleRippleKind kind;
        public int scale = 6;
        public int seed = 1;

        public bool height = true;
        public float strength = 1f;
        public StyleLiquidTarget tone = new StyleLiquidTarget();
        public StyleLiquidTarget solid = new StyleLiquidTarget();
        public StyleLiquidTarget lines = new StyleLiquidTarget();

        public int waveCount = 7;
        public float direction;
        public float spread = 1f;
        public float sharpness = 2f;
        public float lengthVariation = 1f;

        public float domeSize = 0.45f;
        public float smallDomes = 0.5f;
        public float swell = 0.6f;

        public float turbulence = 1.5f;
        public float streaks = 0.25f;
        public float cooling = 0.35f;

        public float edgeWidth = 0.15f;
        public float edgeSharpness = 1f;

        public int octaves = 3;

        public float gap = 0.5f;
        public float gapVariation = 0.6f;
        public float facets = 0.7f;
        public float grain = 0.3f;
        public float blobSize = 0.55f;
        public float sizeVariation = 0.3f;
        public float corners = 0.7f;
        public float warp = 0.7f;
        public float softness = 0.4f;
        public float rings = 3f;
        public int twist = 1;
        public float reach = 0.9f;
        public float density = 0.35f;

        public float roundness;
        public bool invert;

        public float bend;
        public int bendScale = 4;
        public float blur;
        public float black;
        public float white = 1f;
        public float gamma = 1f;

        internal const int MostWaves = 16;

        internal static StyleRippleLayer Create(StyleRippleKind kind)
        {
            var layer = new StyleRippleLayer { kind = kind, label = LabelOf(kind) };

            layer.ResetShape();
            return layer;
        }

        internal static string LabelOf(StyleRippleKind kind)
        {
            return kind.ToString();
        }

        internal StyleLiquidTarget Target(StyleLiquidChannel channel)
        {
            switch (channel)
            {
                case StyleLiquidChannel.Solid:
                    return solid ??= new StyleLiquidTarget();
                case StyleLiquidChannel.Lines:
                    return lines ??= new StyleLiquidTarget();
                default:
                    return tone ??= new StyleLiquidTarget();
            }
        }

        internal bool masks => Target(StyleLiquidChannel.Tone).on || Target(StyleLiquidChannel.Solid).on ||
                               Target(StyleLiquidChannel.Lines).on;

        internal bool adjusted => bend > 0f || blur > 0f || black > 0f || white < 1f || !Mathf.Approximately(gamma, 1f);

        internal void ResetEffects()
        {
            bend = 0f;
            bendScale = 4;
            blur = 0f;
            black = 0f;
            white = 1f;
            gamma = 1f;
        }

        internal void ResetShape()
        {
            seed = 1;
            invert = false;

            switch (kind)
            {
                case StyleRippleKind.Domes:
                    scale = 5;
                    domeSize = 0.45f;
                    smallDomes = 0.5f;
                    swell = 0.6f;
                    break;
                case StyleRippleKind.Flow:
                    scale = 4;
                    turbulence = 1.5f;
                    streaks = 0.25f;
                    cooling = 0.35f;
                    break;
                case StyleRippleKind.Cells:
                    scale = 8;
                    edgeWidth = 0.15f;
                    edgeSharpness = 1f;
                    break;
                case StyleRippleKind.Noise:
                    scale = 12;
                    octaves = 3;
                    break;
                case StyleRippleKind.Plates:
                    scale = 5;
                    gap = 0.5f;
                    gapVariation = 0.6f;
                    facets = 0.7f;
                    grain = 0.3f;
                    roundness = 0f;
                    break;
                case StyleRippleKind.Blobs:
                    scale = 5;
                    blobSize = 0.55f;
                    sizeVariation = 0.3f;
                    gap = 0.5f;
                    gapVariation = 0.8f;
                    corners = 0.7f;
                    warp = 0.7f;
                    softness = 0.4f;
                    roundness = 0.5f;
                    break;
                case StyleRippleKind.Swirl:
                    scale = 4;
                    rings = 3f;
                    twist = 1;
                    reach = 0.9f;
                    warp = 0.6f;
                    break;
                case StyleRippleKind.Rocks:
                    scale = 8;
                    blobSize = 0.35f;
                    sizeVariation = 0.6f;
                    gap = 0.3f;
                    gapVariation = 0.5f;
                    corners = 0.15f;
                    warp = 0.4f;
                    softness = 0.15f;
                    roundness = 0.6f;
                    density = 0.35f;
                    break;
                default:
                    scale = 6;
                    waveCount = 7;
                    direction = 0f;
                    spread = 1f;
                    sharpness = 2f;
                    lengthVariation = 1f;
                    break;
            }
        }

        internal StyleRippleLayer Clone()
        {
            return JsonUtility.FromJson<StyleRippleLayer>(JsonUtility.ToJson(this));
        }
    }

    [Serializable]
    sealed class StyleRippleStack
    {
        public string name = "Clear Water";
        public int resolution = 512;
        public List<StyleRippleLayer> layers = Layers(StyleLiquidRecipe.Water);

        internal static List<StyleRippleLayer> Layers(StyleLiquidRecipe recipe)
        {
            switch (recipe)
            {
                case StyleLiquidRecipe.Acid:
                    return AcidLayers();
                case StyleLiquidRecipe.Lava:
                    return LavaLayers();
                default:
                    return WaterLayers();
            }
        }

        internal static string NameOf(StyleLiquidRecipe recipe)
        {
            switch (recipe)
            {
                case StyleLiquidRecipe.Acid:
                    return "Green Acid";
                case StyleLiquidRecipe.Lava:
                    return "Molten Lava";
                default:
                    return "Clear Water";
            }
        }

        static List<StyleRippleLayer> WaterLayers()
        {
            var depth = Layer(StyleRippleKind.Noise, "Depth");
            var cells = Layer(StyleRippleKind.Blobs, "Cells");
            var waves = Layer(StyleRippleKind.Waves, "Waves");

            depth.scale = 4;
            depth.height = false;
            depth.tone.on = true;
            cells.lines.on = true;
            cells.lines.invert = true;
            cells.lines.feather = 0.3f;
            waves.strength = 0.6f;
            return new List<StyleRippleLayer> { depth, cells, waves };
        }

        static List<StyleRippleLayer> AcidLayers()
        {
            var swirl = Layer(StyleRippleKind.Swirl, "Swirl");
            var veins = Layer(StyleRippleKind.Blobs, "Veins");
            var domes = Layer(StyleRippleKind.Domes, "Domes");

            swirl.scale = 3;
            swirl.rings = 2f;
            swirl.warp = 0.8f;
            swirl.height = false;
            swirl.tone.on = true;
            veins.scale = 7;
            veins.blobSize = 1f;
            veins.sizeVariation = 0f;
            veins.gap = 0.18f;
            veins.gapVariation = 0.6f;
            veins.corners = 0.5f;
            veins.warp = 0.5f;
            veins.softness = 0.2f;
            veins.roundness = 0.3f;
            veins.lines.on = true;
            veins.lines.invert = true;
            veins.lines.feather = 0.3f;
            domes.strength = 0.5f;
            return new List<StyleRippleLayer> { swirl, veins, domes };
        }

        static List<StyleRippleLayer> LavaLayers()
        {
            var swirl = Layer(StyleRippleKind.Swirl, "Swirl");
            var mottle = Layer(StyleRippleKind.Noise, "Mottle");
            var rocks = Layer(StyleRippleKind.Rocks, "Rocks");

            swirl.tone.on = true;
            swirl.strength = 0.35f;
            mottle.scale = 8;
            mottle.height = false;
            mottle.tone.on = true;
            mottle.tone.blend = StyleLiquidBlend.Multiply;
            mottle.tone.opacity = 0.3f;
            rocks.solid.on = true;
            rocks.solid.threshold = 0.25f;
            rocks.solid.feather = 0.15f;
            return new List<StyleRippleLayer> { swirl, mottle, rocks };
        }

        static StyleRippleLayer Layer(StyleRippleKind kind, string label)
        {
            var layer = StyleRippleLayer.Create(kind);

            layer.label = label;
            return layer;
        }
    }

    sealed class StyleLiquidGenerator
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleLiquidStudio.compute";
        const int ThumbnailSize = 64;
        const int GroupSide = 16;
        const int GradientSize = 64;
        const float Slope = 0.15f;
        const float BlurReach = 0.02f;

        static readonly int s_LayerId = Shader.PropertyToID("_Layer");
        static readonly int s_TotalId = Shader.PropertyToID("_Total");
        static readonly int s_NormalId = Shader.PropertyToID("_Normal");
        static readonly int s_DisplayId = Shader.PropertyToID("_Display");
        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_LayerInId = Shader.PropertyToID("_LayerIn");
        static readonly int s_TotalInId = Shader.PropertyToID("_TotalIn");
        static readonly int s_NormalInId = Shader.PropertyToID("_NormalIn");
        static readonly int s_MaskId = Shader.PropertyToID("_Mask");
        static readonly int s_MaskInId = Shader.PropertyToID("_MaskIn");
        static readonly int s_GradientId = Shader.PropertyToID("_Gradient");
        static readonly int s_PaintId = Shader.PropertyToID("_Paint");
        static readonly int s_PaintLinesId = Shader.PropertyToID("_PaintLines");
        static readonly int s_PartialsId = Shader.PropertyToID("_Partials");
        static readonly int s_StatsId = Shader.PropertyToID("_Stats");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_KindId = Shader.PropertyToID("_Kind");
        static readonly int s_ShapeId = Shader.PropertyToID("_Shape");
        static readonly int s_DetailId = Shader.PropertyToID("_Detail");
        static readonly int s_BlobId = Shader.PropertyToID("_Blob");
        static readonly int s_BlendId = Shader.PropertyToID("_Blend");
        static readonly int s_ChannelId = Shader.PropertyToID("_Channel");
        static readonly int s_ShowId = Shader.PropertyToID("_Show");
        static readonly int s_WavesId = Shader.PropertyToID("_Waves");
        static readonly int s_SeedId = Shader.PropertyToID("_Seed");
        static readonly int s_SlotId = Shader.PropertyToID("_Slot");
        static readonly int s_EffectId = Shader.PropertyToID("_Effect");
        static readonly int s_LevelsId = Shader.PropertyToID("_Levels");

        static readonly StyleLiquidChannel[] s_Channels =
        {
            StyleLiquidChannel.Tone, StyleLiquidChannel.Solid, StyleLiquidChannel.Lines
        };

        sealed class Targets
        {
            internal RenderTexture layer;
            internal RenderTexture spare;
            internal RenderTexture total;
            internal RenderTexture normal;
            internal RenderTexture mask;
            internal ComputeBuffer stats;
        }

        readonly Vector4[] m_Waves = new Vector4[StyleRippleLayer.MostWaves];
        readonly List<RenderTexture> m_Thumbnails = new List<RenderTexture>();
        readonly Color[] m_GradientPixels = new Color[GradientSize];

        ComputeShader m_Compute;
        ComputeBuffer m_Partials;
        Targets m_Main;
        Targets m_Small;
        Texture2D m_Preview;
        Texture2D m_PreviewMask;
        Texture2D m_Gradient;
        Vector4 m_Paint = new Vector4(0.26f, 0.17f, 0.18f, 0f);
        Vector4 m_PaintLines;
        int m_Clear;
        int m_Height;
        int m_Reduce;
        int m_Gather;
        int m_Accumulate;
        int m_NormalKernel;
        int m_Show;
        int m_ClearMask;
        int m_MaskLayer;
        int m_MaskHeight;
        int m_Warp;
        int m_Blur;
        int m_Levels;

        internal RenderTexture normal => m_Main?.normal;

        internal RenderTexture mask => m_Main?.mask;

        internal RenderTexture display { get; private set; }

        internal Texture2D previewNormal => m_Preview;

        internal Texture2D previewMask => m_PreviewMask;

        internal bool ready => m_Main != null && m_Preview != null;

        internal Texture Thumbnail(int index)
        {
            return index >= 0 && index < m_Thumbnails.Count ? m_Thumbnails[index] : null;
        }

        internal bool Generate(StyleRippleStack stack, bool thumbnails = true)
        {
            if (!Prepare())
                return false;

            var resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(stack.resolution), 128, 2048);

            if (m_Main == null || m_Main.total.width != resolution)
            {
                Free(m_Main);
                Destroy(display);
                Destroy(m_Preview);
                Destroy(m_PreviewMask);
                m_Partials?.Release();
                m_Main = NewTargets(resolution, true);
                display = NewTarget(resolution, RenderTextureFormat.ARGBHalf, false, "Display");
                m_Partials = new ComputeBuffer((resolution / GroupSide) * (resolution / GroupSide), 16);
                m_Preview = NewPreview(m_Main.normal, GraphicsFormat.R8G8B8A8_UNorm, "Normal");
                m_PreviewMask = NewPreview(m_Main.mask, GraphicsFormat.R16G16B16A16_SFloat, "Mask");
            }

            Bake(stack.layers, m_Main, false);
            m_Main.normal.GenerateMips();
            m_Main.mask.GenerateMips();
            Graphics.CopyTexture(m_Main.normal, m_Preview);
            Graphics.CopyTexture(m_Main.mask, m_PreviewMask);
            m_Preview.IncrementUpdateCount();
            m_PreviewMask.IncrementUpdateCount();

            if (!thumbnails)
                return true;

            m_Small ??= NewTargets(ThumbnailSize, false);

            while (m_Thumbnails.Count < stack.layers.Count)
                m_Thumbnails.Add(NewTarget(ThumbnailSize, RenderTextureFormat.ARGB32, false, "Layer"));

            for (var i = 0; i < stack.layers.Count; i++)
            {
                ClearMask(m_Small);
                DrawLayer(stack.layers[i], m_Small);
                BlendMask(m_Small, StyleLiquidChannel.Tone, StyleLiquidBlend.Add, 0.5f, 1f, 1f, false);
                ShowInto(m_Small, m_Thumbnails[i], StyleLiquidMap.Tone);
            }

            return true;
        }

        internal void Paint(HBStyleLiquidLayer look)
        {
            if (look == null)
                return;

            if (m_Gradient == null)
            {
                m_Gradient = new Texture2D(GradientSize, 1, TextureFormat.RGBAHalf, false, true)
                {
                    name = "HB Texture Studio Liquid Gradient",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            for (var x = 0; x < GradientSize; x++)
                m_GradientPixels[x] = look.depthGradient.Evaluate((float)x / (GradientSize - 1));

            m_Gradient.SetPixels(m_GradientPixels);
            m_Gradient.Apply(false, false);

            var rock = look.rockColor;
            var line = look.lineColor;

            m_Paint = new Vector4(rock.r, rock.g, rock.b, look.toneAmount);
            m_PaintLines = new Vector4(line.r, line.g, line.b, look.lines);
        }

        internal void Show(StyleLiquidMap map)
        {
            if (ready)
                ShowInto(m_Main, display, map);
        }

        void ShowInto(Targets targets, RenderTexture output, StyleLiquidMap map)
        {
            var resolution = targets.total.width;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, resolution / GroupSide));
            m_Compute.SetVector(s_ShowId, new Vector4((int)map, 0f, 0f, 0f));
            m_Compute.SetVector(s_PaintId, m_Paint);
            m_Compute.SetVector(s_PaintLinesId, m_PaintLines);
            m_Compute.SetTexture(m_Show, s_GradientId, m_Gradient != null ? m_Gradient : Texture2D.grayTexture);
            m_Compute.SetTexture(m_Show, s_TotalInId, targets.total);
            m_Compute.SetTexture(m_Show, s_NormalInId, targets.normal);
            m_Compute.SetTexture(m_Show, s_MaskInId, targets.mask);
            m_Compute.SetTexture(m_Show, s_DisplayId, output);
            m_Compute.SetBuffer(m_Show, s_StatsId, targets.stats);
            Dispatch(m_Show, resolution);
        }

        void Bake(List<StyleRippleLayer> layers, Targets targets, bool solo)
        {
            var resolution = targets.total.width;
            var groups = resolution / GroupSide;

            ClearMask(targets);
            m_Compute.SetTexture(m_Clear, s_TotalId, targets.total);
            Dispatch(m_Clear, resolution);

            foreach (var layer in layers)
            {
                if (layer == null || !layer.visible && !solo || !layer.height && !layer.masks)
                    continue;

                DrawLayer(layer, targets);

                if (layer.height && layer.strength > 0f)
                {
                    m_Compute.SetTexture(m_Accumulate, s_LayerInId, targets.layer);
                    m_Compute.SetTexture(m_Accumulate, s_TotalId, targets.total);
                    m_Compute.SetBuffer(m_Accumulate, s_StatsId, targets.stats);
                    Dispatch(m_Accumulate, resolution);
                }

                foreach (var channel in s_Channels)
                {
                    var target = layer.Target(channel);

                    if (target.on && target.opacity > 0f)
                        BlendMask(targets, channel, target.blend, target.threshold, target.feather, target.opacity,
                                  target.invert);
                }
            }

            Range(targets.total, targets.stats, 1, groups);
            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, groups));
            m_Compute.SetTexture(m_NormalKernel, s_TotalInId, targets.total);
            m_Compute.SetTexture(m_NormalKernel, s_NormalId, targets.normal);
            Dispatch(m_NormalKernel, resolution);
            m_Compute.SetTexture(m_MaskHeight, s_TotalInId, targets.total);
            m_Compute.SetTexture(m_MaskHeight, s_MaskId, targets.mask);
            m_Compute.SetBuffer(m_MaskHeight, s_StatsId, targets.stats);
            Dispatch(m_MaskHeight, resolution);
        }

        void ClearMask(Targets targets)
        {
            var resolution = targets.total.width;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, resolution / GroupSide));
            m_Compute.SetTexture(m_ClearMask, s_MaskId, targets.mask);
            Dispatch(m_ClearMask, resolution);
        }

        void BlendMask(Targets targets, StyleLiquidChannel channel, StyleLiquidBlend blend, float threshold,
                       float feather, float opacity, bool invert)
        {
            var resolution = targets.total.width;
            var pick = new Vector4(channel == StyleLiquidChannel.Tone ? 1f : 0f,
                                   channel == StyleLiquidChannel.Solid ? 1f : 0f,
                                   channel == StyleLiquidChannel.Lines ? 1f : 0f, invert ? 1f : 0f);

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, resolution / GroupSide));
            m_Compute.SetVector(s_BlendId, new Vector4((int)blend, threshold, feather, Mathf.Clamp01(opacity)));
            m_Compute.SetVector(s_ChannelId, pick);
            m_Compute.SetTexture(m_MaskLayer, s_LayerInId, targets.layer);
            m_Compute.SetTexture(m_MaskLayer, s_MaskId, targets.mask);
            m_Compute.SetBuffer(m_MaskLayer, s_StatsId, targets.stats);
            Dispatch(m_MaskLayer, resolution);
        }

        void DrawLayer(StyleRippleLayer layer, Targets targets)
        {
            var resolution = targets.total.width;
            var scale = Mathf.Max(layer.scale, 1);
            var invert = layer.invert ? 1f : 0f;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, resolution / GroupSide));
            m_Compute.SetVector(s_KindId, new Vector4((int)layer.kind, scale, layer.strength * Slope / scale, 0f));
            m_Compute.SetVector(s_ShapeId, Shape(layer));
            m_Compute.SetVector(s_DetailId, new Vector4(layer.octaves, layer.roundness, invert, layer.density));
            m_Compute.SetVector(s_BlobId, Blob(layer));
            m_Compute.SetInt(s_SeedId, layer.seed);

            if (layer.kind == StyleRippleKind.Waves)
                m_Compute.SetVectorArray(s_WavesId, Waves(layer));

            m_Compute.SetTexture(m_Height, s_LayerId, targets.layer);
            Dispatch(m_Height, resolution);
            Range(targets.layer, targets.stats, 0, resolution / GroupSide);

            if (layer.adjusted)
                Adjust(layer, targets);
        }

        void Adjust(StyleRippleLayer layer, Targets targets)
        {
            var resolution = targets.total.width;
            var groups = resolution / GroupSide;
            var radius = Mathf.Clamp01(layer.blur) * resolution * BlurReach;

            if (layer.bend > 0f)
            {
                m_Compute.SetInt(s_SeedId, layer.seed);
                m_Compute.SetVector(s_EffectId, new Vector4(0f, 0f, layer.bend, Mathf.Max(layer.bendScale, 1)));
                Pass(m_Warp, targets.layer, targets.spare, resolution);
                Graphics.CopyTexture(targets.spare, targets.layer);
            }

            if (radius >= 0.5f)
            {
                m_Compute.SetVector(s_EffectId, new Vector4(radius, 0f, 0f, 0f));
                Pass(m_Blur, targets.layer, targets.spare, resolution);
                m_Compute.SetVector(s_EffectId, new Vector4(radius, 1f, 0f, 0f));
                Pass(m_Blur, targets.spare, targets.layer, resolution);
            }

            Range(targets.layer, targets.stats, 0, groups);

            if (layer.black <= 0f && layer.white >= 1f && Mathf.Approximately(layer.gamma, 1f))
                return;

            m_Compute.SetVector(s_LevelsId, new Vector4(layer.black, Mathf.Max(layer.white, layer.black + 0.001f),
                                                        layer.gamma, 0f));
            m_Compute.SetTexture(m_Levels, s_LayerId, targets.layer);
            m_Compute.SetBuffer(m_Levels, s_StatsId, targets.stats);
            Dispatch(m_Levels, resolution);
        }

        void Pass(int kernel, RenderTexture source, RenderTexture output, int resolution)
        {
            m_Compute.SetTexture(kernel, s_LayerInId, source);
            m_Compute.SetTexture(kernel, s_LayerId, output);
            Dispatch(kernel, resolution);
        }


        void Range(RenderTexture source, ComputeBuffer stats, int slot, int groups)
        {
            m_Compute.SetTexture(m_Reduce, s_SourceId, source);
            m_Compute.SetBuffer(m_Reduce, s_PartialsId, m_Partials);
            m_Compute.Dispatch(m_Reduce, groups, groups, 1);
            m_Compute.SetInt(s_SlotId, slot);
            m_Compute.SetBuffer(m_Gather, s_PartialsId, m_Partials);
            m_Compute.SetBuffer(m_Gather, s_StatsId, stats);
            m_Compute.Dispatch(m_Gather, 1, 1, 1);
        }

        static Vector4 Shape(StyleRippleLayer layer)
        {
            switch (layer.kind)
            {
                case StyleRippleKind.Domes:
                    return new Vector4(layer.domeSize, layer.smallDomes, layer.swell, 0f);
                case StyleRippleKind.Flow:
                    return new Vector4(layer.turbulence, layer.streaks, layer.cooling, 0f);
                case StyleRippleKind.Cells:
                    return new Vector4(layer.edgeWidth, layer.edgeSharpness, 0f, 0f);
                case StyleRippleKind.Plates:
                    return new Vector4(Mathf.Lerp(0.016f, 0.048f, layer.gap), layer.gapVariation, layer.facets,
                                       layer.grain);
                case StyleRippleKind.Blobs:
                case StyleRippleKind.Rocks:
                    return new Vector4(Mathf.Lerp(0.3f, 1.2f, layer.blobSize), layer.sizeVariation,
                                       Mathf.Lerp(0.01f, 0.15f, layer.gap), layer.gapVariation);
                case StyleRippleKind.Swirl:
                    return new Vector4(layer.rings, layer.twist, layer.reach, 0f);
                case StyleRippleKind.Waves:
                    return new Vector4(Mathf.Clamp(layer.waveCount, 1, StyleRippleLayer.MostWaves), layer.sharpness,
                                       0f, 0f);
                default:
                    return Vector4.zero;
            }
        }

        static Vector4 Blob(StyleRippleLayer layer)
        {
            return new Vector4(0.02f + 0.4f * layer.corners, layer.warp, Mathf.Lerp(0.01f, 0.08f, layer.softness), 0f);
        }

        Vector4[] Waves(StyleRippleLayer layer)
        {
            var count = Mathf.Clamp(layer.waveCount, 1, StyleRippleLayer.MostWaves);
            var shortest = 1f - 0.4f * layer.lengthVariation;
            var longest = 1f + 0.6f * layer.lengthVariation;

            for (var i = 0; i < m_Waves.Length; i++)
            {
                if (i >= count)
                {
                    m_Waves[i] = Vector4.zero;
                    continue;
                }

                var angle = (layer.direction + (Hash(i, 0, layer.seed) * 2f - 1f) * layer.spread * 180f) *
                            Mathf.Deg2Rad;
                var magnitude = layer.scale * Mathf.Lerp(shortest, longest, Hash(i, 1, layer.seed));
                var kx = Mathf.Round(Mathf.Cos(angle) * magnitude);
                var ky = Mathf.Round(Mathf.Sin(angle) * magnitude);

                if (kx == 0f && ky == 0f)
                    kx = 1f;

                m_Waves[i] = new Vector4(kx, ky, Hash(i, 2, layer.seed) * Mathf.PI * 2f,
                                         1f / Mathf.Sqrt(kx * kx + ky * ky));
            }

            return m_Waves;
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = x * 374761393 + y * 668265263 + seed * 1274126177;

                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        bool Prepare()
        {
            if (m_Compute != null)
                return true;

            m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

            if (m_Compute == null || !SystemInfo.supportsComputeShaders)
            {
                m_Compute = null;
                return false;
            }

            m_Clear = m_Compute.FindKernel("Clear");
            m_Height = m_Compute.FindKernel("Height");
            m_Reduce = m_Compute.FindKernel("Reduce");
            m_Gather = m_Compute.FindKernel("Gather");
            m_Accumulate = m_Compute.FindKernel("Accumulate");
            m_NormalKernel = m_Compute.FindKernel("Normal");
            m_Show = m_Compute.FindKernel("Show");
            m_ClearMask = m_Compute.FindKernel("ClearMask");
            m_MaskLayer = m_Compute.FindKernel("MaskLayer");
            m_MaskHeight = m_Compute.FindKernel("MaskHeight");
            m_Warp = m_Compute.FindKernel("Warp");
            m_Blur = m_Compute.FindKernel("Blur");
            m_Levels = m_Compute.FindKernel("Levels");
            return true;
        }

        void Dispatch(int kernel, int size)
        {
            var groups = (size + 7) / 8;

            m_Compute.Dispatch(kernel, groups, groups, 1);
        }

        static Targets NewTargets(int size, bool mips)
        {
            return new Targets
            {
                layer = NewTarget(size, RenderTextureFormat.RFloat, false, "Layer Height"),
                spare = NewTarget(size, RenderTextureFormat.RFloat, false, "Layer Spare"),
                total = NewTarget(size, RenderTextureFormat.RFloat, false, "Height"),
                normal = NewTarget(size, RenderTextureFormat.ARGB32, mips, "Normal"),
                mask = NewTarget(size, RenderTextureFormat.ARGBHalf, mips, "Mask"),
                stats = new ComputeBuffer(2, 16)
            };
        }

        static RenderTexture NewTarget(int size, RenderTextureFormat format, bool mips, string name)
        {
            var target = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "HB Texture Studio Liquid " + name,
                enableRandomWrite = true,
                useMipMap = mips,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            target.Create();
            return target;
        }

        static Texture2D NewPreview(RenderTexture source, GraphicsFormat format, string name)
        {
            return new Texture2D(source.width, source.height, format, source.mipmapCount,
                                 TextureCreationFlags.MipChain)
            {
                name = "HB Texture Studio Liquid " + name,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
        }

        internal static Texture2D Capture(RenderTexture source, string name)
        {
            var width = source.width;
            var height = source.height;
            var plain = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                                                   RenderTextureReadWrite.Linear);
            var active = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };

            try
            {
                Graphics.Blit(source, plain);
                RenderTexture.active = plain;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(true);
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(plain);
            }

            EditorUtility.CompressTexture(texture, TextureFormat.BC7, TextureCompressionQuality.Normal);
            return texture;
        }

        static void Free(Targets targets)
        {
            if (targets == null)
                return;

            Destroy(targets.layer);
            Destroy(targets.spare);
            Destroy(targets.total);
            Destroy(targets.normal);
            Destroy(targets.mask);
            targets.stats?.Release();
        }

        static void Destroy(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (target is RenderTexture texture)
                texture.Release();

            UnityEngine.Object.DestroyImmediate(target);
        }

        internal void Release()
        {
            Free(m_Main);
            Free(m_Small);
            m_Main = null;
            m_Small = null;
            Destroy(display);
            Destroy(m_Preview);
            Destroy(m_PreviewMask);
            Destroy(m_Gradient);
            display = null;
            m_Gradient = null;
            m_Preview = null;
            m_PreviewMask = null;

            foreach (var thumbnail in m_Thumbnails)
                Destroy(thumbnail);

            m_Thumbnails.Clear();
            m_Partials?.Release();
            m_Partials = null;
        }
    }
}
