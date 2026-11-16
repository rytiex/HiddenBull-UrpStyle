using System;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleCloudKind
    {
        Cumulus,
        Stratus,
        Cirrus,
        Altocumulus
    }

    enum StyleCloudMap
    {
        Shaded,
        Relief,
        Height,
        Density
    }

    [Serializable]
    sealed class StyleCloudSettings
    {
        public string name = "Painted Clouds";
        public int resolution = 512;
        public StyleCloudKind kind;

        public int puffScale = 2;
        public float puffiness = 1f;
        public int shapeScale = 1;
        public int octaves = 2;
        public float stretch;

        public float erosion = 0.325f;
        public int erosionScale = 8;
        public float contrast = 4f;

        public float warp = 0.325f;
        public int warpScale = 1;

        public float reliefStrength = 1f;
        public float reliefSmoothing = 1f;

        public Texture2D brush;
        public float brushAmount = 0.225f;
        public float brushScale = 0.65f;
        public float paint = 2.25f;

        public int seed = 1;

        public float previewCoverage = 0.5f;
        public float previewSoftness = 0.25f;

        internal static string LabelOf(StyleCloudKind kind)
        {
            return kind.ToString();
        }

        internal void Apply(StyleCloudKind picked)
        {
            kind = picked;
            ResetShape();
            ResetBreakup();
            ResetRelief();
            ResetBrush();
        }

        internal void ResetShape()
        {
            switch (kind)
            {
                case StyleCloudKind.Stratus:
                    SetShape(6, 0.15f, 2, 4, 0.35f);
                    break;
                case StyleCloudKind.Cirrus:
                    SetShape(10, 0.1f, 2, 5, 0.85f);
                    break;
                case StyleCloudKind.Altocumulus:
                    SetShape(9, 0.9f, 2, 3, 0f);
                    break;
                default:
                    SetShape(2, 1f, 1, 2, 0f);
                    break;
            }
        }

        internal void ResetBreakup()
        {
            seed = 1;

            switch (kind)
            {
                case StyleCloudKind.Stratus:
                    SetBreakup(0.15f, 12, 1.5f, 0.5f, 2);
                    break;
                case StyleCloudKind.Cirrus:
                    SetBreakup(0.6f, 24, 2.5f, 0.8f, 3);
                    break;
                case StyleCloudKind.Altocumulus:
                    SetBreakup(0.4f, 20, 3f, 0.25f, 2);
                    break;
                default:
                    SetBreakup(0.325f, 8, 4f, 0.325f, 1);
                    break;
            }
        }

        internal void ResetRelief()
        {
            switch (kind)
            {
                case StyleCloudKind.Stratus:
                    SetRelief(0.35f, 1f);
                    break;
                case StyleCloudKind.Cirrus:
                    SetRelief(0.2f, 0.4f);
                    break;
                case StyleCloudKind.Altocumulus:
                    SetRelief(0.8f, 0.5f);
                    break;
                default:
                    SetRelief(1f, 1f);
                    break;
            }
        }

        internal void ResetBrush()
        {
            switch (kind)
            {
                case StyleCloudKind.Stratus:
                    SetBrush(0.15f, 0.5f, 1.25f);
                    break;
                case StyleCloudKind.Cirrus:
                    SetBrush(0.3f, 1f, 1.5f);
                    break;
                case StyleCloudKind.Altocumulus:
                    SetBrush(0.2f, 1.25f, 1.75f);
                    break;
                default:
                    SetBrush(0.225f, 0.65f, 2.25f);
                    break;
            }
        }

        internal void ResetPreview()
        {
            previewCoverage = 0.5f;
            previewSoftness = 0.25f;
        }

        void SetShape(int puffs, float puffy, int shapes, int detail, float streak)
        {
            puffScale = puffs;
            puffiness = puffy;
            shapeScale = shapes;
            octaves = detail;
            stretch = streak;
        }

        void SetBreakup(float eat, int eatScale, float sharpness, float bend, int bendScale)
        {
            erosion = eat;
            erosionScale = eatScale;
            contrast = sharpness;
            warp = bend;
            warpScale = bendScale;
        }

        void SetRelief(float strength, float smoothing)
        {
            reliefStrength = strength;
            reliefSmoothing = smoothing;
        }

        void SetBrush(float amount, float scale, float reach)
        {
            brushAmount = amount;
            brushScale = scale;
            paint = reach;
        }
    }

    sealed class StyleCloudGenerator
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleCloudStudio.compute";
        const float SpineRange = 0.0625f;
        const int GroupSide = 16;

        static readonly int s_DensityId = Shader.PropertyToID("_Density");
        static readonly int s_BlurredId = Shader.PropertyToID("_Blurred");
        static readonly int s_HeightId = Shader.PropertyToID("_Height");
        static readonly int s_PackedId = Shader.PropertyToID("_Packed");
        static readonly int s_AtlasId = Shader.PropertyToID("_Atlas");
        static readonly int s_DisplayId = Shader.PropertyToID("_Display");
        static readonly int s_BrushId = Shader.PropertyToID("_Brush");
        static readonly int s_BlurSourceId = Shader.PropertyToID("_BlurSource");
        static readonly int s_PackedInId = Shader.PropertyToID("_PackedIn");
        static readonly int s_AtlasInId = Shader.PropertyToID("_AtlasIn");
        static readonly int s_PartialsId = Shader.PropertyToID("_Partials");
        static readonly int s_StatsId = Shader.PropertyToID("_Stats");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_ShapeId = Shader.PropertyToID("_Shape");
        static readonly int s_ErodeId = Shader.PropertyToID("_Erode");
        static readonly int s_WarpId = Shader.PropertyToID("_Warp");
        static readonly int s_ReliefId = Shader.PropertyToID("_Relief");
        static readonly int s_ShowId = Shader.PropertyToID("_Show");
        static readonly int s_SeedId = Shader.PropertyToID("_Seed");
        static readonly int s_SlotId = Shader.PropertyToID("_Slot");

        readonly Vector4[] m_Readback = new Vector4[2];

        ComputeShader m_Compute;
        ComputeBuffer m_Partials;
        ComputeBuffer m_Stats;
        RenderTexture m_Density;
        RenderTexture m_Blurred;
        RenderTexture m_Height;
        RenderTexture m_Packed;
        int m_DensityKernel;
        int m_Gather;
        int m_Shape;
        int m_BlurX;
        int m_BlurY;
        int m_Pack;
        int m_Paint;
        int m_Show;
        Vector3 m_Measured = new Vector3(-1f, -1f, -1f);

        internal RenderTexture atlas { get; private set; }

        internal RenderTexture display { get; private set; }

        internal bool ready => atlas != null;

        internal float visible { get; private set; }

        internal bool Generate(StyleCloudSettings settings)
        {
            if (!Prepare())
                return false;

            var resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(settings.resolution), 128, 2048);
            var groups = resolution / GroupSide;

            if (atlas == null || atlas.width != resolution)
            {
                Release();
                Prepare();
                m_Density = NewTarget(resolution, RenderTextureFormat.RFloat, false, "Density");
                m_Blurred = NewTarget(resolution, RenderTextureFormat.RFloat, false, "Blurred");
                m_Height = NewTarget(resolution, RenderTextureFormat.RFloat, false, "Height");
                m_Packed = NewTarget(resolution, RenderTextureFormat.ARGBFloat, false, "Packed");
                atlas = NewTarget(resolution, RenderTextureFormat.ARGBFloat, true, "Atlas");
                display = NewTarget(resolution, RenderTextureFormat.ARGBHalf, false, "Display");
                m_Partials = new ComputeBuffer(groups * groups, 16);
            }

            var brush = settings.brush;
            var hasBrush = brush != null;
            var brushSize = hasBrush ? Mathf.Max(Mathf.Min(brush.width, brush.height), 1) : 1;
            var radius = Mathf.RoundToInt(Mathf.Lerp(1f, resolution * 0.025f, settings.reliefSmoothing));
            var reach = SpineRange * settings.paint / Mathf.Max(settings.brushScale, 1e-3f);
            var stretch = 1 + Mathf.RoundToInt(settings.stretch * 7f);

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, brushSize, groups));
            m_Compute.SetVector(s_ShapeId, new Vector4(settings.puffScale, settings.puffiness, settings.shapeScale,
                                                       settings.octaves));
            m_Compute.SetVector(s_ErodeId, new Vector4(settings.erosion, settings.erosionScale, stretch,
                                                       settings.contrast));
            m_Compute.SetVector(s_WarpId, new Vector4(settings.warp, settings.warpScale, settings.brushAmount,
                                                      settings.brushScale));
            m_Compute.SetVector(s_ReliefId, new Vector4(resolution * 0.03f * settings.reliefStrength, radius, reach,
                                                        hasBrush ? 1f : 0f));
            m_Compute.SetInt(s_SeedId, settings.seed);

            Texture brushTexture = hasBrush ? brush : Texture2D.grayTexture;

            m_Compute.SetTexture(m_DensityKernel, s_BrushId, brushTexture);
            m_Compute.SetTexture(m_DensityKernel, s_DensityId, m_Density);
            m_Compute.SetBuffer(m_DensityKernel, s_PartialsId, m_Partials);
            m_Compute.Dispatch(m_DensityKernel, groups, groups, 1);
            Gather(0);

            m_Compute.SetTexture(m_Shape, s_DensityId, m_Density);
            m_Compute.SetBuffer(m_Shape, s_StatsId, m_Stats);
            Dispatch(m_Shape, resolution);

            m_Compute.SetTexture(m_BlurX, s_BlurSourceId, m_Density);
            m_Compute.SetTexture(m_BlurX, s_BlurredId, m_Blurred);
            Dispatch(m_BlurX, resolution);
            m_Compute.SetTexture(m_BlurY, s_BlurSourceId, m_Blurred);
            m_Compute.SetTexture(m_BlurY, s_HeightId, m_Height);
            Dispatch(m_BlurY, resolution);

            m_Compute.SetTexture(m_Pack, s_HeightId, m_Height);
            m_Compute.SetTexture(m_Pack, s_DensityId, m_Density);
            m_Compute.SetTexture(m_Pack, s_PackedId, m_Packed);
            Dispatch(m_Pack, resolution);

            m_Compute.SetTexture(m_Paint, s_BrushId, brushTexture);
            m_Compute.SetTexture(m_Paint, s_PackedInId, m_Packed);
            m_Compute.SetTexture(m_Paint, s_AtlasId, atlas);
            Dispatch(m_Paint, resolution);
            atlas.GenerateMips();

            m_Measured = new Vector3(-1f, -1f, -1f);
            return true;
        }

        internal void Show(StyleCloudMap map, float coverage, float softness)
        {
            if (!ready)
                return;

            var resolution = atlas.width;
            var groups = resolution / GroupSide;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, 1f, groups));
            m_Compute.SetVector(s_ShowId, new Vector4((int)map, coverage, softness, 0f));
            m_Compute.SetTexture(m_Show, s_AtlasInId, atlas);
            m_Compute.SetTexture(m_Show, s_DisplayId, display);
            m_Compute.SetBuffer(m_Show, s_PartialsId, m_Partials);
            m_Compute.Dispatch(m_Show, groups, groups, 1);

            var key = new Vector3((int)map, coverage, softness);

            if (key == m_Measured)
                return;

            Gather(1);
            m_Stats.GetData(m_Readback);
            visible = m_Readback[1].x / (resolution * (float)resolution);
            m_Measured = key;
        }

        void Gather(int slot)
        {
            m_Compute.SetInt(s_SlotId, slot);
            m_Compute.SetBuffer(m_Gather, s_PartialsId, m_Partials);
            m_Compute.SetBuffer(m_Gather, s_StatsId, m_Stats);
            m_Compute.Dispatch(m_Gather, 1, 1, 1);
        }

        bool Prepare()
        {
            if (m_Compute == null)
            {
                m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

                if (m_Compute == null || !SystemInfo.supportsComputeShaders)
                    return false;

                m_DensityKernel = m_Compute.FindKernel("Density");
                m_Gather = m_Compute.FindKernel("Gather");
                m_Shape = m_Compute.FindKernel("Shape");
                m_BlurX = m_Compute.FindKernel("BlurX");
                m_BlurY = m_Compute.FindKernel("BlurY");
                m_Pack = m_Compute.FindKernel("Pack");
                m_Paint = m_Compute.FindKernel("Paint");
                m_Show = m_Compute.FindKernel("Show");
            }

            m_Stats ??= new ComputeBuffer(2, 16);
            return true;
        }

        void Dispatch(int kernel, int size)
        {
            var groups = (size + 7) / 8;

            m_Compute.Dispatch(kernel, groups, groups, 1);
        }

        static RenderTexture NewTarget(int size, RenderTextureFormat format, bool mips, string name)
        {
            var target = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "HB Texture Studio Cloud " + name,
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

        static void Destroy(ref RenderTexture target)
        {
            if (target == null)
                return;

            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            target = null;
        }

        internal void Release()
        {
            var shown = atlas;
            var screen = display;

            Destroy(ref shown);
            Destroy(ref screen);
            atlas = null;
            display = null;
            Destroy(ref m_Density);
            Destroy(ref m_Blurred);
            Destroy(ref m_Height);
            Destroy(ref m_Packed);
            m_Partials?.Release();
            m_Stats?.Release();
            m_Partials = null;
            m_Stats = null;
        }
    }
}
