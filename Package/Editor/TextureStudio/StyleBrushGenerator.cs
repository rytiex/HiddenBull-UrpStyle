using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleBrushStyle
    {
        Oil,
        Gouache,
        DryBrush,
        Hatching,
        Watercolour
    }

    enum StyleBrushMap
    {
        Direction,
        Coverage
    }

    [Serializable]
    sealed class StyleBrushLayer
    {
        public string label = "Oil";
        public bool visible = true;
        public StyleBrushStyle style;
        public float strength = 1f;

        public int strokeCount = 800;
        public Vector2 lengthRange = new Vector2(0.1f, 0.25f);
        public Vector2 widthRange = new Vector2(0.006f, 0.0265f);
        public float angle = 35f;
        public float angleJitter = 18f;
        public float curvature = 0.5f;

        public float taper = 0.565f;
        public float edgeSoftness = 0.25f;
        public float edgeBreakup = 0.35f;
        public float edgeBreakupScale = 20f;

        public float bristleAmount = 0.435f;
        public float bristleBreakup = 0.5f;
        public float bristleDensity = 40f;

        public float opacityVariation = 0.55f;
        public float toneVariation = 0.75f;
        public float pigmentAmount = 1f;
        public int pigmentScale = 32;

        public int seed = 1;

        internal const int MostStrokes = 4000;

        internal static StyleBrushLayer Create(StyleBrushStyle style)
        {
            var layer = new StyleBrushLayer();

            layer.Apply(style);
            layer.label = LabelOf(style);
            return layer;
        }

        internal static string LabelOf(StyleBrushStyle style)
        {
            switch (style)
            {
                case StyleBrushStyle.DryBrush:
                    return "Dry Brush";
                default:
                    return style.ToString();
            }
        }

        internal void Apply(StyleBrushStyle picked)
        {
            style = picked;
            ResetStrokes();
            ResetEdge();
            ResetBristles();
            ResetPaint();
        }

        internal void ResetStrokes()
        {
            switch (style)
            {
                case StyleBrushStyle.Gouache:
                    SetStrokes(420, 0.12f, 0.3f, 0.02f, 0.05f, 35f, 25f, 0.35f);
                    break;
                case StyleBrushStyle.DryBrush:
                    SetStrokes(650, 0.12f, 0.3f, 0.015f, 0.04f, 35f, 14f, 0.4f);
                    break;
                case StyleBrushStyle.Hatching:
                    SetStrokes(1400, 0.06f, 0.14f, 0.003f, 0.008f, 45f, 4f, 0.05f);
                    break;
                case StyleBrushStyle.Watercolour:
                    SetStrokes(180, 0.2f, 0.45f, 0.04f, 0.1f, 35f, 40f, 0.6f);
                    break;
                default:
                    SetStrokes(800, 0.1f, 0.25f, 0.006f, 0.0265f, 35f, 18f, 0.5f);
                    break;
            }
        }

        internal void ResetEdge()
        {
            switch (style)
            {
                case StyleBrushStyle.Gouache:
                    SetEdge(0.35f, 0.12f, 0.2f, 8f);
                    break;
                case StyleBrushStyle.DryBrush:
                    SetEdge(0.7f, 0.05f, 0.7f, 20f);
                    break;
                case StyleBrushStyle.Hatching:
                    SetEdge(0.3f, 0.1f, 0.1f, 6f);
                    break;
                case StyleBrushStyle.Watercolour:
                    SetEdge(0.5f, 0.75f, 0.55f, 5f);
                    break;
                default:
                    SetEdge(0.565f, 0.25f, 0.35f, 20f);
                    break;
            }
        }

        internal void ResetBristles()
        {
            switch (style)
            {
                case StyleBrushStyle.Gouache:
                    SetBristles(0.15f, 0.2f, 24f);
                    break;
                case StyleBrushStyle.DryBrush:
                    SetBristles(0.85f, 0.9f, 32f);
                    break;
                case StyleBrushStyle.Hatching:
                    SetBristles(0f, 0f, 10f);
                    break;
                case StyleBrushStyle.Watercolour:
                    SetBristles(0.05f, 0.1f, 8f);
                    break;
                default:
                    SetBristles(0.435f, 0.5f, 40f);
                    break;
            }
        }

        internal void ResetPaint()
        {
            strength = 1f;
            seed = 1;

            switch (style)
            {
                case StyleBrushStyle.Gouache:
                    SetPaint(0.2f, 0.55f, 0.45f, 12);
                    break;
                case StyleBrushStyle.DryBrush:
                    SetPaint(0.7f, 0.8f, 0.6f, 24);
                    break;
                case StyleBrushStyle.Hatching:
                    SetPaint(0.3f, 0.6f, 0.2f, 8);
                    break;
                case StyleBrushStyle.Watercolour:
                    SetPaint(0.6f, 0.5f, 1f, 6);
                    break;
                default:
                    SetPaint(0.55f, 0.75f, 1f, 32);
                    break;
            }
        }

        void SetStrokes(int count, float shortest, float longest, float thinnest, float thickest, float direction,
                        float jitter, float bend)
        {
            strokeCount = count;
            lengthRange = new Vector2(shortest, longest);
            widthRange = new Vector2(thinnest, thickest);
            angle = direction;
            angleJitter = jitter;
            curvature = bend;
        }

        void SetEdge(float ends, float softness, float breakup, float breakupScale)
        {
            taper = ends;
            edgeSoftness = softness;
            edgeBreakup = breakup;
            edgeBreakupScale = breakupScale;
        }

        void SetBristles(float amount, float breakup, float density)
        {
            bristleAmount = amount;
            bristleBreakup = breakup;
            bristleDensity = density;
        }

        void SetPaint(float opacity, float tone, float pigment, int pigmentCells)
        {
            opacityVariation = opacity;
            toneVariation = tone;
            pigmentAmount = pigment;
            pigmentScale = pigmentCells;
        }

        internal StyleBrushLayer Clone()
        {
            return JsonUtility.FromJson<StyleBrushLayer>(JsonUtility.ToJson(this));
        }
    }

    [Serializable]
    sealed class StyleBrushStack
    {
        public string name = "Painted Brush";
        public int resolution = 512;
        public float contrast = 1.65f;
        public float canvasAmount = 0.25f;
        public int canvasScale = 225;
        public int canvasSeed = 1;
        public Color previewColour = new Color(0.72f, 0.6f, 0.47f);
        public List<StyleBrushLayer> layers = new List<StyleBrushLayer> { StyleBrushLayer.Create(StyleBrushStyle.Oil) };

        internal void ResetFinish()
        {
            contrast = 1.65f;
            canvasAmount = 0.25f;
            canvasScale = 225;
            canvasSeed = 1;
        }
    }

    sealed class StyleBrushGenerator
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleBrushStudio.compute";
        const float Ground = 0.5f;
        const float SpineRange = 0.0625f;
        const int ThumbnailSize = 64;
        const int GroupSide = 16;
        const int StrokeBatch = 256;

        [StructLayout(LayoutKind.Sequential)]
        struct Stroke
        {
            public Vector2 start;
            public Vector2 control;
            public Vector2 end;
            public float width;
            public float alpha;
            public float tone;
            public int seed;
            public Vector4 box;
        }

        static readonly int s_FieldId = Shader.PropertyToID("_Field");
        static readonly int s_AtlasId = Shader.PropertyToID("_Atlas");
        static readonly int s_DisplayId = Shader.PropertyToID("_Display");
        static readonly int s_PackedId = Shader.PropertyToID("_Packed");
        static readonly int s_StrokesId = Shader.PropertyToID("_Strokes");
        static readonly int s_PartialsId = Shader.PropertyToID("_Partials");
        static readonly int s_StatsId = Shader.PropertyToID("_Stats");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_EdgeId = Shader.PropertyToID("_Edge");
        static readonly int s_BristleId = Shader.PropertyToID("_Bristle");
        static readonly int s_PigmentId = Shader.PropertyToID("_Pigment");
        static readonly int s_FinishId = Shader.PropertyToID("_Finish");
        static readonly int s_ShowId = Shader.PropertyToID("_Show");
        static readonly int s_RangeId = Shader.PropertyToID("_Range");

        readonly List<Stroke> m_Strokes = new List<Stroke>();
        readonly List<RenderTexture> m_Thumbnails = new List<RenderTexture>();

        ComputeShader m_Compute;
        ComputeBuffer m_StrokeBuffer;
        ComputeBuffer m_Partials;
        ComputeBuffer m_Stats;
        RenderTexture m_Field;
        RenderTexture m_SmallField;
        RenderTexture m_SmallAtlas;
        int m_Clear;
        int m_StrokeKernel;
        int m_Grain;
        int m_Finish;
        int m_Encode;
        int m_Show;

        internal RenderTexture atlas { get; private set; }

        internal RenderTexture display { get; private set; }

        internal bool ready => atlas != null;

        internal Texture Thumbnail(int index)
        {
            return index >= 0 && index < m_Thumbnails.Count ? m_Thumbnails[index] : null;
        }

        internal bool Generate(StyleBrushStack stack)
        {
            if (!Prepare())
                return false;

            var resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(stack.resolution), 128, 2048);

            Ensure(ref m_Field, resolution, false, "HB Texture Studio Brush Field");
            Ensure(ref m_SmallField, ThumbnailSize, false, "HB Texture Studio Brush Small Field");
            Ensure(ref m_SmallAtlas, ThumbnailSize, false, "HB Texture Studio Brush Small Atlas");

            var fresh = atlas == null || atlas.width != resolution;

            if (fresh)
            {
                Destroy(atlas);
                Destroy(display);
                atlas = NewTarget(resolution, true, "HB Texture Studio Brush Atlas");
                display = NewTarget(resolution, false, "HB Texture Studio Brush Display");
            }

            Bake(stack, stack.layers, resolution, m_Field, atlas, false);
            atlas.GenerateMips();

            while (m_Thumbnails.Count < stack.layers.Count)
                m_Thumbnails.Add(NewTarget(ThumbnailSize, false, "HB Texture Studio Brush Layer"));

            var single = new List<StyleBrushLayer>(1) { null };

            for (var i = 0; i < stack.layers.Count; i++)
            {
                single[0] = stack.layers[i];
                Bake(stack, single, ThumbnailSize, m_SmallField, m_SmallAtlas, true);
                Show(m_SmallAtlas, m_Thumbnails[i], StyleBrushMap.Coverage, ThumbnailSize);
            }

            return true;
        }

        internal void Show(StyleBrushMap map)
        {
            if (ready)
                Show(atlas, display, map, atlas.width);
        }

        void Show(RenderTexture packed, RenderTexture target, StyleBrushMap map, int size)
        {
            m_Compute.SetVector(s_SizeId, new Vector4(size, 0f, 0f, 0f));
            m_Compute.SetVector(s_ShowId, new Vector4(map == StyleBrushMap.Direction ? 1f : 0f, 0f, 0f, 0f));
            m_Compute.SetTexture(m_Show, s_PackedId, packed);
            m_Compute.SetTexture(m_Show, s_DisplayId, target);
            Dispatch(m_Show, size, 8);
        }

        void Bake(StyleBrushStack stack, List<StyleBrushLayer> layers, int resolution, RenderTexture field,
                  RenderTexture target, bool showHidden)
        {
            var groups = resolution / GroupSide;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 0f, 0f, groups));
            m_Compute.SetTexture(m_Clear, s_FieldId, field);
            Dispatch(m_Clear, resolution, 8);

            foreach (var layer in layers)
            {
                if (layer == null || (!layer.visible && !showHidden))
                    continue;

                var segments = BuildStrokes(layer, resolution);

                if (m_Strokes.Count == 0)
                    continue;

                if (m_StrokeBuffer == null || m_StrokeBuffer.count < m_Strokes.Count)
                {
                    m_StrokeBuffer?.Release();
                    m_StrokeBuffer = new ComputeBuffer(Mathf.NextPowerOfTwo(m_Strokes.Count),
                                                       Marshal.SizeOf<Stroke>());
                }

                m_StrokeBuffer.SetData(m_Strokes);
                m_Compute.SetVector(s_SizeId, new Vector4(resolution, 0f, m_Strokes.Count, groups));
                m_Compute.SetVector(s_EdgeId, new Vector4(layer.taper, layer.edgeSoftness, layer.edgeBreakup,
                                                          layer.edgeBreakupScale));
                m_Compute.SetVector(s_BristleId, new Vector4(layer.bristleAmount, layer.bristleBreakup,
                                                             layer.bristleDensity, segments));
                m_Compute.SetVector(s_PigmentId, new Vector4(layer.pigmentAmount, Mathf.Max(layer.pigmentScale, 1),
                                                             SafeSeed(layer.seed, 7), layer.strength));
                m_Compute.SetBuffer(m_StrokeKernel, s_StrokesId, m_StrokeBuffer);
                m_Compute.SetTexture(m_StrokeKernel, s_FieldId, field);

                for (var first = 0; first < m_Strokes.Count; first += StrokeBatch)
                {
                    var last = Mathf.Min(first + StrokeBatch, m_Strokes.Count);

                    m_Compute.SetVector(s_RangeId, new Vector4(first, last, 0f, 0f));
                    Dispatch(m_StrokeKernel, resolution, 8);
                }
            }

            var partials = groups * groups;

            if (m_Partials == null || m_Partials.count < partials)
            {
                m_Partials?.Release();
                m_Partials = new ComputeBuffer(partials, 16);
            }

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 0.5f / (SpineRange * resolution), 0f, groups));
            m_Compute.SetVector(s_FinishId, new Vector4(stack.contrast, stack.canvasAmount,
                                                        Mathf.Max(stack.canvasScale, 1),
                                                        SafeSeed(stack.canvasSeed, 13)));
            m_Compute.SetTexture(m_Grain, s_FieldId, field);
            m_Compute.SetBuffer(m_Grain, s_PartialsId, m_Partials);
            m_Compute.Dispatch(m_Grain, groups, groups, 1);
            m_Compute.SetBuffer(m_Finish, s_PartialsId, m_Partials);
            m_Compute.SetBuffer(m_Finish, s_StatsId, m_Stats);
            m_Compute.Dispatch(m_Finish, 1, 1, 1);
            m_Compute.SetTexture(m_Encode, s_FieldId, field);
            m_Compute.SetBuffer(m_Encode, s_StatsId, m_Stats);
            m_Compute.SetTexture(m_Encode, s_AtlasId, target);
            Dispatch(m_Encode, resolution, 8);
        }

        int BuildStrokes(StyleBrushLayer layer, int resolution)
        {
            m_Strokes.Clear();

            var random = new System.Random(layer.seed);
            var count = Mathf.Clamp(layer.strokeCount, 1, StyleBrushLayer.MostStrokes);
            var minLength = Mathf.Min(layer.lengthRange.x, layer.lengthRange.y);
            var maxLength = Mathf.Max(layer.lengthRange.x, layer.lengthRange.y);
            var minWidth = Mathf.Min(layer.widthRange.x, layer.widthRange.y);
            var maxWidth = Mathf.Max(layer.widthRange.x, layer.widthRange.y);
            var segments = 1 + Mathf.RoundToInt(layer.curvature * 7f);
            var reach = 1f + layer.edgeBreakup * 0.5f;

            for (var s = 0; s < count; s++)
            {
                var centre = new Vector2(Next(random), Next(random)) * resolution;
                var radians = (layer.angle + (Next(random) * 2f - 1f) * layer.angleJitter) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var halfLength = Mathf.Lerp(minLength, maxLength, Next(random)) * resolution * 0.5f;
                var perpendicular = new Vector2(-direction.y, direction.x);
                var bend = (Next(random) * 2f - 1f) * layer.curvature * halfLength;

                var stroke = new Stroke
                {
                    start = centre - direction * halfLength,
                    control = centre + perpendicular * bend,
                    end = centre + direction * halfLength,
                    width = Mathf.Max(1f, Mathf.Lerp(minWidth, maxWidth, Next(random)) * resolution),
                    alpha = Mathf.Lerp(1f - layer.opacityVariation, 1f, Next(random)),
                    tone = Ground + (Next(random) * 2f - 1f) * layer.toneVariation * Ground,
                    seed = random.Next()
                };

                var lowest = Vector2.Min(Vector2.Min(stroke.start, stroke.control), stroke.end);
                var highest = Vector2.Max(Vector2.Max(stroke.start, stroke.control), stroke.end);
                var pad = stroke.width * reach + 1f;

                lowest -= Vector2.one * pad;
                highest += Vector2.one * pad;

                var fromX = Mathf.FloorToInt(lowest.x / resolution);
                var toX = Mathf.FloorToInt(highest.x / resolution);
                var fromY = Mathf.FloorToInt(lowest.y / resolution);
                var toY = Mathf.FloorToInt(highest.y / resolution);

                for (var tileY = fromY; tileY <= toY; tileY++)
                {
                    for (var tileX = fromX; tileX <= toX; tileX++)
                    {
                        var shift = new Vector2(tileX, tileY) * -resolution;
                        var copy = stroke;

                        copy.start += shift;
                        copy.control += shift;
                        copy.end += shift;
                        copy.box = new Vector4(lowest.x + shift.x, lowest.y + shift.y, highest.x + shift.x,
                                               highest.y + shift.y);
                        m_Strokes.Add(copy);
                    }
                }
            }

            return segments;
        }

        static float SafeSeed(int seed, int salt)
        {
            return ((seed & 0xFFFF) * 131 + salt) & 0xFFFFFF;
        }

        static float Next(System.Random random)
        {
            return (float)random.NextDouble();
        }

        bool Prepare()
        {
            if (m_Compute == null)
            {
                m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

                if (m_Compute == null || !SystemInfo.supportsComputeShaders)
                    return false;

                m_Clear = m_Compute.FindKernel("Clear");
                m_StrokeKernel = m_Compute.FindKernel("Strokes");
                m_Grain = m_Compute.FindKernel("Grain");
                m_Finish = m_Compute.FindKernel("Finish");
                m_Encode = m_Compute.FindKernel("Encode");
                m_Show = m_Compute.FindKernel("Show");
            }

            m_Stats ??= new ComputeBuffer(1, 16);
            return true;
        }

        void Dispatch(int kernel, int size, int threads)
        {
            var groups = (size + threads - 1) / threads;

            m_Compute.Dispatch(kernel, groups, groups, 1);
        }

        static void Ensure(ref RenderTexture target, int size, bool mips, string name)
        {
            if (target != null && target.width == size)
                return;

            Destroy(target);
            target = NewTarget(size, mips, name);
        }

        static RenderTexture NewTarget(int size, bool mips, string name)
        {
            var target = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                name = name,
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

        static void Destroy(RenderTexture target)
        {
            if (target == null)
                return;

            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        internal void Release()
        {
            Destroy(atlas);
            Destroy(display);
            Destroy(m_Field);
            Destroy(m_SmallField);
            Destroy(m_SmallAtlas);
            atlas = null;
            display = null;
            m_Field = null;
            m_SmallField = null;
            m_SmallAtlas = null;

            foreach (var thumbnail in m_Thumbnails)
                Destroy(thumbnail);

            m_Thumbnails.Clear();
            m_StrokeBuffer?.Release();
            m_Partials?.Release();
            m_Stats?.Release();
            m_StrokeBuffer = null;
            m_Partials = null;
            m_Stats = null;
        }
    }
}
