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
        Plates
    }

    enum StyleLiquidMap
    {
        Normal,
        Height
    }

    [Serializable]
    sealed class StyleRippleLayer
    {
        public string label = "Waves";
        public bool visible = true;
        public StyleRippleKind kind;
        public float strength = 1f;
        public int scale = 6;
        public int seed = 1;

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

        internal void ResetShape()
        {
            strength = 1f;
            seed = 1;

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
                    strength = 0.15f;
                    break;
                case StyleRippleKind.Plates:
                    scale = 5;
                    gap = 0.5f;
                    gapVariation = 0.6f;
                    facets = 0.7f;
                    grain = 0.3f;
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
        public StyleLiquidRecipe recipe;
        public List<StyleRippleLayer> layers = Layers(StyleLiquidRecipe.Water);

        internal static List<StyleRippleLayer> Layers(StyleLiquidRecipe recipe)
        {
            switch (recipe)
            {
                case StyleLiquidRecipe.Acid:
                    return new List<StyleRippleLayer> { StyleRippleLayer.Create(StyleRippleKind.Domes) };
                case StyleLiquidRecipe.Lava:
                    return new List<StyleRippleLayer> { StyleRippleLayer.Create(StyleRippleKind.Plates) };
                default:
                {
                    var detail = StyleRippleLayer.Create(StyleRippleKind.Noise);

                    detail.label = "Fine Ripples";
                    return new List<StyleRippleLayer> { StyleRippleLayer.Create(StyleRippleKind.Waves), detail };
                }
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
    }

    sealed class StyleLiquidGenerator
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleLiquidStudio.compute";
        const int ThumbnailSize = 64;
        const int GroupSide = 16;
        const float Slope = 0.15f;

        static readonly int s_LayerId = Shader.PropertyToID("_Layer");
        static readonly int s_TotalId = Shader.PropertyToID("_Total");
        static readonly int s_NormalId = Shader.PropertyToID("_Normal");
        static readonly int s_DisplayId = Shader.PropertyToID("_Display");
        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_LayerInId = Shader.PropertyToID("_LayerIn");
        static readonly int s_TotalInId = Shader.PropertyToID("_TotalIn");
        static readonly int s_NormalInId = Shader.PropertyToID("_NormalIn");
        static readonly int s_PartialsId = Shader.PropertyToID("_Partials");
        static readonly int s_StatsId = Shader.PropertyToID("_Stats");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_KindId = Shader.PropertyToID("_Kind");
        static readonly int s_ShapeId = Shader.PropertyToID("_Shape");
        static readonly int s_DetailId = Shader.PropertyToID("_Detail");
        static readonly int s_ShowId = Shader.PropertyToID("_Show");
        static readonly int s_WavesId = Shader.PropertyToID("_Waves");
        static readonly int s_SeedId = Shader.PropertyToID("_Seed");
        static readonly int s_SlotId = Shader.PropertyToID("_Slot");

        sealed class Targets
        {
            internal RenderTexture layer;
            internal RenderTexture total;
            internal RenderTexture normal;
            internal ComputeBuffer stats;
        }

        readonly Vector4[] m_Waves = new Vector4[StyleRippleLayer.MostWaves];
        readonly List<RenderTexture> m_Thumbnails = new List<RenderTexture>();
        readonly List<StyleRippleLayer> m_Single = new List<StyleRippleLayer> { null };

        ComputeShader m_Compute;
        ComputeBuffer m_Partials;
        Targets m_Main;
        Targets m_Small;
        Texture2D m_Preview;
        int m_Clear;
        int m_Height;
        int m_Reduce;
        int m_Gather;
        int m_Accumulate;
        int m_NormalKernel;
        int m_Show;

        internal RenderTexture normal => m_Main?.normal;

        internal RenderTexture display { get; private set; }

        internal Texture2D previewNormal => m_Preview;

        internal bool ready => m_Main != null && m_Preview != null;

        internal Texture Thumbnail(int index)
        {
            return index >= 0 && index < m_Thumbnails.Count ? m_Thumbnails[index] : null;
        }

        internal bool Generate(StyleRippleStack stack)
        {
            if (!Prepare())
                return false;

            var resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(stack.resolution), 128, 2048);

            if (m_Main == null || m_Main.total.width != resolution)
            {
                Free(m_Main);
                Destroy(display);
                Destroy(m_Preview);
                m_Partials?.Release();
                m_Main = NewTargets(resolution, true);
                display = NewTarget(resolution, RenderTextureFormat.ARGBHalf, false, "Display");
                m_Partials = new ComputeBuffer((resolution / GroupSide) * (resolution / GroupSide), 16);
                m_Preview = new Texture2D(resolution, resolution, GraphicsFormat.R8G8B8A8_UNorm,
                                          m_Main.normal.mipmapCount, TextureCreationFlags.MipChain)
                {
                    name = "HB Texture Studio Liquid Normal",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 4
                };
            }

            m_Small ??= NewTargets(ThumbnailSize, false);
            Bake(stack.layers, m_Main, false);
            m_Main.normal.GenerateMips();
            Graphics.CopyTexture(m_Main.normal, m_Preview);
            m_Preview.IncrementUpdateCount();

            while (m_Thumbnails.Count < stack.layers.Count)
                m_Thumbnails.Add(NewTarget(ThumbnailSize, RenderTextureFormat.ARGB32, false, "Layer"));

            for (var i = 0; i < stack.layers.Count; i++)
            {
                m_Single[0] = stack.layers[i];
                Bake(m_Single, m_Small, true);
                Graphics.CopyTexture(m_Small.normal, m_Thumbnails[i]);
            }

            m_Single[0] = null;
            return true;
        }

        internal void Show(StyleLiquidMap map)
        {
            if (!ready)
                return;

            var resolution = m_Main.total.width;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, resolution / GroupSide));
            m_Compute.SetVector(s_ShowId, new Vector4(map == StyleLiquidMap.Height ? 1f : 0f, 0f, 0f, 0f));
            m_Compute.SetTexture(m_Show, s_TotalInId, m_Main.total);
            m_Compute.SetTexture(m_Show, s_NormalInId, m_Main.normal);
            m_Compute.SetTexture(m_Show, s_DisplayId, display);
            m_Compute.SetBuffer(m_Show, s_StatsId, m_Main.stats);
            Dispatch(m_Show, resolution);
        }

        void Bake(List<StyleRippleLayer> layers, Targets targets, bool showHidden)
        {
            var resolution = targets.total.width;
            var groups = resolution / GroupSide;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 0f, groups));
            m_Compute.SetTexture(m_Clear, s_TotalId, targets.total);
            Dispatch(m_Clear, resolution);

            foreach (var layer in layers)
            {
                if (layer == null || !layer.visible && !showHidden || layer.strength <= 0f)
                    continue;

                var scale = Mathf.Max(layer.scale, 1);

                m_Compute.SetVector(s_KindId, new Vector4((int)layer.kind, scale, layer.strength * Slope / scale, 0f));
                m_Compute.SetVector(s_ShapeId, Shape(layer));
                m_Compute.SetVector(s_DetailId, new Vector4(layer.octaves, 0f, 0f, 0f));
                m_Compute.SetInt(s_SeedId, layer.seed);

                if (layer.kind == StyleRippleKind.Waves)
                    m_Compute.SetVectorArray(s_WavesId, Waves(layer));

                m_Compute.SetTexture(m_Height, s_LayerId, targets.layer);
                Dispatch(m_Height, resolution);
                Range(targets.layer, targets.stats, 0, groups);
                m_Compute.SetTexture(m_Accumulate, s_LayerInId, targets.layer);
                m_Compute.SetTexture(m_Accumulate, s_TotalId, targets.total);
                m_Compute.SetBuffer(m_Accumulate, s_StatsId, targets.stats);
                Dispatch(m_Accumulate, resolution);
            }

            Range(targets.total, targets.stats, 1, groups);
            m_Compute.SetTexture(m_NormalKernel, s_TotalInId, targets.total);
            m_Compute.SetTexture(m_NormalKernel, s_NormalId, targets.normal);
            Dispatch(m_NormalKernel, resolution);
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
                case StyleRippleKind.Waves:
                    return new Vector4(Mathf.Clamp(layer.waveCount, 1, StyleRippleLayer.MostWaves), layer.sharpness,
                                       0f, 0f);
                default:
                    return Vector4.zero;
            }
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
                total = NewTarget(size, RenderTextureFormat.RFloat, false, "Height"),
                normal = NewTarget(size, RenderTextureFormat.ARGB32, mips, "Normal"),
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

        static void Free(Targets targets)
        {
            if (targets == null)
                return;

            Destroy(targets.layer);
            Destroy(targets.total);
            Destroy(targets.normal);
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
            display = null;
            m_Preview = null;

            foreach (var thumbnail in m_Thumbnails)
                Destroy(thumbnail);

            m_Thumbnails.Clear();
            m_Partials?.Release();
            m_Partials = null;
        }
    }
}
