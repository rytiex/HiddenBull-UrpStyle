using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTerrainLiquid
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/TerrainLiquid.shader";

        const int BatchSize = 1023;
        const int Slots = HBStyleTerrainData.LiquidSlots;
        const int GradientSize = 64;
        const int GradientKeys = 6;
        const int SeaRings = 6;
        const int SeaTiles = 3;
        const float SeaOverlap = 1f / (StyleTerrainTree.PatchQuads * 8f);
        const float Empty = 1e9f;
        const float LevelZero = 32768f;
        const float LevelSpan = 32767f;
        const int CutFrom = 32768;
        const float BoundsMargin = 1f;
        const float WaveMargin = 2f;
        const float KeyGap = 1e-4f;
        const float DetailShare = 0.25f;
        const float LeafReach = 3f;

        static readonly int s_RangesId = Shader.PropertyToID("_HB_LiquidRanges");
        static readonly int s_GradientId = Shader.PropertyToID("_HB_LiquidGradient");
        static readonly int s_OpticsId = Shader.PropertyToID("_HB_LiquidOptics");
        static readonly int s_ShoreId = Shader.PropertyToID("_HB_LiquidShore");
        static readonly int s_StyleId = Shader.PropertyToID("_HB_LiquidStyle");
        static readonly int s_FoamId = Shader.PropertyToID("_HB_LiquidFoam");
        static readonly int s_FoamShapeId = Shader.PropertyToID("_HB_LiquidFoamShape");
        static readonly int s_CrestId = Shader.PropertyToID("_HB_LiquidCrest");
        static readonly int s_WavesId = Shader.PropertyToID("_HB_LiquidWaves");

        static readonly Color s_Missing = new Color(0.2f, 0.2f, 0.2f, 1f);

        readonly StyleTerrainTree m_Tree = new StyleTerrainTree();
        readonly StyleLiquidWaves m_Waves = new StyleLiquidWaves();
        readonly StyleLiquidCalm m_Calm = new StyleLiquidCalm();
        readonly List<StyleTerrainNode> m_Nodes = new List<StyleTerrainNode>();
        readonly List<Matrix4x4> m_Full = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_Quarters = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_Sea = new List<Matrix4x4>();
        readonly Vector4[] m_Ranges = new Vector4[StyleTerrainTree.MaxLevels];
        readonly Vector4[] m_Optics = new Vector4[Slots];
        readonly Vector4[] m_Shore = new Vector4[Slots];
        readonly Vector4[] m_Style = new Vector4[Slots];
        readonly Vector4[] m_Foam = new Vector4[Slots];
        readonly Vector4[] m_FoamShape = new Vector4[Slots];
        readonly Vector4[] m_Crest = new Vector4[Slots];
        readonly Vector4[] m_WaveShape = new Vector4[Slots];
        readonly HBStyleLiquidLayer[] m_Layers = new HBStyleLiquidLayer[Slots];
        readonly int[] m_Versions = new int[Slots];
        readonly ushort[] m_Pixels = new ushort[GradientSize * Slots * 4];
        readonly Color[] m_Colours = new Color[GradientKeys];
        readonly float[] m_ColourTimes = new float[GradientKeys];
        readonly float[] m_Alphas = new float[GradientKeys];
        readonly float[] m_AlphaTimes = new float[GradientKeys];

        Material m_Material;
        Texture2D m_Gradient;
        float[] m_LeafMin;
        float[] m_LeafMax;
        float[] m_WaterMin;
        float[] m_WaterMax;
        bool[] m_Holed;
        bool m_Built;
        int m_BuiltHoles = -1;
        HBStyleTerrainData m_BuiltData;
        Texture2D m_BuiltLevel;
        int m_BuiltVersion = -1;
        int m_BuiltWater = -1;
        float m_BuiltSea = float.NaN;
        float m_BuiltRange = float.NaN;
        float m_Low;
        float m_High;

        internal void Draw(ScriptableRenderContext context, Camera camera, HBStyleTerrainData data, Shader shader,
                           ComputeShader waves, MaterialPropertyBlock block, Vector3 origin, Vector3 local,
                           float[] planes, float far, float detailRange, int layer)
        {
            if (!data.liquidOn || shader == null || !Ready(shader) || !Leaves(data))
                return;

            Bind(data, block);
            m_Waves.Bind(context, waves, block);
            m_Calm.Bind(context, waves, data, block);

            m_Tree.SetRanges(Mathf.Max(detailRange * DetailShare, m_Tree.leafSize * LeafReach));

            for (var level = 0; level < StyleTerrainTree.MaxLevels; level++)
            {
                var end = m_Tree.Range(level);
                m_Ranges[level] = new Vector4(StyleTerrainTree.MorphStart(end), end, 0f, 0f);
            }

            block.SetVectorArray(s_RangesId, m_Ranges);
            m_Tree.Select(local.x, local.y, local.z, planes, far, m_Nodes);
            Fill(origin);
            Sea(origin, local, planes, far, data.seaLevel, out var seaBounds);

            var parameters = new RenderParams(m_Material)
            {
                camera = camera,
                layer = layer,
                matProps = block,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.Off,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.BlendProbes
            };

            if (m_Full.Count + m_Quarters.Count > 0)
            {
                var low = origin.y + Mathf.Min(m_Low, data.seaLevel) - BoundsMargin;
                var high = origin.y + Mathf.Max(m_High, data.seaLevel) + BoundsMargin;
                var centre = m_Tree.span * 0.5f - m_Tree.offset;
                var middle = new Vector3(origin.x + centre, (low + high) * 0.5f, origin.z + centre);

                parameters.worldBounds = new Bounds(middle, new Vector3(m_Tree.span, high - low, m_Tree.span));
                Submit(parameters, HBStyleTerrain.Patch(false), m_Full);
                Submit(parameters, HBStyleTerrain.Patch(true), m_Quarters);
            }

            if (m_Sea.Count > 0)
            {
                parameters.worldBounds = seaBounds;
                Submit(parameters, HBStyleTerrain.Patch(false), m_Sea);
            }
        }

        internal void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_Gradient);
            m_Waves.Dispose();
            m_Calm.Dispose();
            m_Material = null;
            m_Gradient = null;
            m_Built = false;
            m_BuiltData = null;
        }

        bool Ready(Shader shader)
        {
            if (m_Material != null && m_Material.shader != shader)
            {
                CoreUtils.Destroy(m_Material);
                m_Material = null;
            }

            if (m_Material == null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(shader);
                m_Material.enableInstancing = true;
            }

            return m_Material.enableInstancing;
        }

        bool Leaves(HBStyleTerrainData data)
        {
            var level = Level(data);
            var size = data.size;
            var leaves = data.patches;
            var last = leaves - 1;

            if (leaves < 1 || data.patchMin == null || data.patchMin.Length != leaves * leaves)
                return false;

            if (!m_Built || m_BuiltData != data || m_BuiltLevel != level || m_BuiltWater != data.waterVersion ||
                m_BuiltSea != data.seaLevel || m_BuiltRange != size.y || m_LeafMin.Length != leaves * leaves)
            {
                data.TakeWaterShown(out _);
                data.TakeHolesEdited(out _);

                if (m_LeafMin == null || m_LeafMin.Length != leaves * leaves)
                {
                    m_LeafMin = new float[leaves * leaves];
                    m_LeafMax = new float[leaves * leaves];
                    m_WaterMin = new float[leaves * leaves];
                    m_WaterMax = new float[leaves * leaves];
                    m_Holed = new bool[leaves * leaves];
                }

                Measure(data, level, 0, 0, last, last);
                Holes(data, 0, 0, last, last);
                Ground(data, 0, 0, last, last);

                m_Built = Build(data);
                m_BuiltData = data;
                m_BuiltLevel = level;
                m_BuiltVersion = data.version;
                m_BuiltWater = data.waterVersion;
                m_BuiltSea = data.seaLevel;
                m_BuiltRange = size.y;
                m_BuiltHoles = HolesCount(data);
                return m_Built;
            }

            var moved = false;

            if (m_BuiltHoles != HolesCount(data))
            {
                data.TakeHolesEdited(out _);
                Holes(data, 0, 0, last, last);
                Ground(data, 0, 0, last, last);
                m_BuiltHoles = HolesCount(data);
                moved = true;
            }
            else if (data.TakeHolesEdited(out var cells))
            {
                var count = Mathf.Max(data.terrainData.holesResolution, 1);
                var x0 = Mathf.Clamp(cells.xMin * leaves / count, 0, last);
                var z0 = Mathf.Clamp(cells.yMin * leaves / count, 0, last);
                var x1 = Mathf.Clamp((cells.xMax - 1) * leaves / count, 0, last);
                var z1 = Mathf.Clamp((cells.yMax - 1) * leaves / count, 0, last);

                Holes(data, x0, z0, x1, z1);
                Ground(data, x0, z0, x1, z1);
                moved = true;
            }

            if (data.TakeWaterShown(out var area) && level != null)
            {
                var width = level.width;
                var x0 = Mathf.Clamp(Mathf.FloorToInt((area.xMin - 1f) * leaves / width), 0, last);
                var z0 = Mathf.Clamp(Mathf.FloorToInt((area.yMin - 1f) * leaves / width), 0, last);
                var x1 = Mathf.Clamp(Mathf.FloorToInt((area.xMax + 1f) * leaves / width), 0, last);
                var z1 = Mathf.Clamp(Mathf.FloorToInt((area.yMax + 1f) * leaves / width), 0, last);

                Measure(data, level, x0, z0, x1, z1);
                Ground(data, x0, z0, x1, z1);
                moved = true;
            }

            if (m_BuiltVersion != data.version)
            {
                Ground(data, 0, 0, last, last);
                m_BuiltVersion = data.version;
                moved = true;
            }

            if (moved)
                m_Built = Build(data);

            return m_Built;
        }

        static Texture2D Level(HBStyleTerrainData data)
        {
            var level = data.waterLevel;

            return data.waterFormat == HBStyleTerrainData.WaterFormat && level != null && level.isReadable &&
                   level.width == level.height
                ? level
                : null;
        }

        void Measure(HBStyleTerrainData data, Texture2D level, int x0, int z0, int x1, int z1)
        {
            var leaves = data.patches;
            var sea = data.seaLevel;
            var range = Mathf.Max(data.size.y, 0.01f);
            var width = level != null ? level.width : 0;
            var levels = level != null ? level.GetPixelData<ushort>(0) : default;

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var low = Empty;
                    var high = -Empty;

                    if (level == null)
                    {
                        low = sea;
                        high = sea;
                    }
                    else
                    {
                        Span(x, leaves, width, out var u0, out var u1);
                        Span(z, leaves, width, out var v0, out var v1);

                        for (var v = v0; v <= v1; v++)
                        {
                            for (var u = u0; u <= u1; u++)
                            {
                                var texel = (v * width + u) * 2;

                                if (levels[texel + 1] >= CutFrom)
                                    continue;

                                var height = sea + (levels[texel] - LevelZero) / LevelSpan * range;

                                low = Mathf.Min(low, height);
                                high = Mathf.Max(high, height);
                            }
                        }
                    }

                    m_WaterMin[z * leaves + x] = low;
                    m_WaterMax[z * leaves + x] = high;
                }
            }
        }

        void Ground(HBStyleTerrainData data, int x0, int z0, int x1, int z1)
        {
            var leaves = data.patches;
            var ground = data.patchMin;

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var index = z * leaves + x;
                    var dry = m_WaterMax[index] <= ground[index] && !m_Holed[index];

                    m_LeafMin[index] = dry ? Empty : m_WaterMin[index] - WaveMargin;
                    m_LeafMax[index] = dry ? -Empty : m_WaterMax[index] + WaveMargin;
                }
            }
        }

        static int HolesCount(HBStyleTerrainData data)
        {
            var holes = data.terrainData != null ? data.terrainData.holesTexture : null;

            return holes != null ? (int)holes.updateCount : 0;
        }

        void Holes(HBStyleTerrainData data, int x0, int z0, int x1, int z1)
        {
            var leaves = data.patches;
            var terrain = data.terrainData;
            var count = terrain != null ? terrain.holesResolution : 0;

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                    m_Holed[z * leaves + x] = false;
            }

            if (count < 1)
                return;

            var c0 = x0 * count / leaves;
            var r0 = z0 * count / leaves;
            var c1 = Mathf.Min((x1 + 1) * count / leaves, count) - 1;
            var r1 = Mathf.Min((z1 + 1) * count / leaves, count) - 1;

            if (c1 < c0 || r1 < r0)
                return;

            var solid = terrain.GetHoles(c0, r0, c1 - c0 + 1, r1 - r0 + 1);

            for (var r = r0; r <= r1; r++)
            {
                for (var c = c0; c <= c1; c++)
                {
                    if (solid[r - r0, c - c0])
                        continue;

                    var x = Mathf.Min(c * leaves / count, leaves - 1);
                    var z = Mathf.Min(r * leaves / count, leaves - 1);

                    m_Holed[z * leaves + x] = true;
                }
            }
        }

        static void Span(int leaf, int leaves, int width, out int first, out int last)
        {
            first = Mathf.Clamp(Mathf.FloorToInt((float)leaf * width / leaves - 0.5f), 0, width - 1);
            last = Mathf.Clamp(Mathf.FloorToInt((float)(leaf + 1) * width / leaves - 0.5f) + 1, 0, width - 1);
        }

        bool Build(HBStyleTerrainData data)
        {
            var size = data.size;
            var leaf = size.x / (data.resolution - 1) * StyleTerrainTree.PatchQuads;

            m_Low = Empty;
            m_High = -Empty;

            for (var i = 0; i < m_LeafMin.Length; i++)
            {
                if (m_LeafMin[i] > m_LeafMax[i])
                    continue;

                m_Low = Mathf.Min(m_Low, m_LeafMin[i]);
                m_High = Mathf.Max(m_High, m_LeafMax[i]);
            }

            return m_Tree.Build(m_LeafMin, m_LeafMax, data.patches, leaf, StyleTerrainTree.MaxLevels, data.seaLevel,
                               WaveMargin);
        }

        void Fill(Vector3 origin)
        {
            m_Full.Clear();
            m_Quarters.Clear();

            foreach (var node in m_Nodes)
            {
                var matrix = Matrix4x4.identity;
                matrix.m00 = node.size;
                matrix.m11 = node.level + 1f;
                matrix.m22 = node.size;
                matrix.m03 = origin.x + node.x;
                matrix.m13 = origin.y;
                matrix.m23 = origin.z + node.z;

                if (node.size < m_Tree.leafSize * (1 << node.level) * 0.75f)
                    m_Quarters.Add(matrix);
                else
                    m_Full.Add(matrix);
            }
        }

        void Sea(Vector3 origin, Vector3 local, float[] planes, float far, float sea, out Bounds bounds)
        {
            m_Sea.Clear();
            bounds = default;

            var left = -m_Tree.offset;
            var bottom = -m_Tree.offset;
            var width = m_Tree.span;
            var length = m_Tree.span;
            var any = false;

            for (var ring = 0; ring < SeaRings; ring++)
            {
                if (local.x - far >= left && local.x + far <= left + width && local.z - far >= bottom &&
                    local.z + far <= bottom + length)
                    break;

                for (var j = -1; j <= 1; j++)
                {
                    for (var i = -1; i <= 1; i++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        var x = left + i * width;
                        var z = bottom + j * length;

                        if (StyleTerrainTree.Distance(local.x, local.y, local.z, x, sea, z, x + width, sea,
                                                      z + length) > far)
                            continue;

                        if (planes != null && !StyleTerrainTree.Visible(planes, x, sea - BoundsMargin, z, x + width,
                                                                        sea + BoundsMargin, z + length))
                            continue;

                        var overlapX = width * SeaOverlap;
                        var overlapZ = length * SeaOverlap;
                        var matrix = Matrix4x4.identity;
                        matrix.m00 = width + overlapX * 2f;
                        matrix.m11 = 0f;
                        matrix.m22 = length + overlapZ * 2f;
                        matrix.m03 = origin.x + x - overlapX;
                        matrix.m13 = origin.y;
                        matrix.m23 = origin.z + z - overlapZ;
                        m_Sea.Add(matrix);

                        var tile = new Bounds(origin + new Vector3(x + width * 0.5f, sea, z + length * 0.5f),
                                              new Vector3(matrix.m00, BoundsMargin * 2f, matrix.m22));

                        if (any)
                            bounds.Encapsulate(tile);
                        else
                            bounds = tile;

                        any = true;
                    }
                }

                left -= width;
                bottom -= length;
                width *= SeaTiles;
                length *= SeaTiles;
            }
        }

        static void Submit(RenderParams parameters, Mesh mesh, List<Matrix4x4> instances)
        {
            for (var start = 0; start < instances.Count; start += BatchSize)
            {
                var count = Mathf.Min(BatchSize, instances.Count - start);

                Graphics.RenderMeshInstanced(parameters, mesh, 0, instances, count, start);
            }
        }

        void Bind(HBStyleTerrainData data, MaterialPropertyBlock block)
        {
            var changed = m_Gradient == null;

            for (var slot = 0; slot < Slots; slot++)
            {
                var liquid = slot < data.liquidCount ? data.Liquid(slot) : null;
                var version = liquid != null ? liquid.version : -1;

                if (liquid == m_Layers[slot] && version == m_Versions[slot])
                    continue;

                m_Layers[slot] = liquid;
                m_Versions[slot] = version;
                changed = true;
            }

            if (changed)
                Rebuild();

            block.SetTexture(s_GradientId, m_Gradient);
            block.SetVectorArray(s_OpticsId, m_Optics);
            block.SetVectorArray(s_ShoreId, m_Shore);
            block.SetVectorArray(s_StyleId, m_Style);
            block.SetVectorArray(s_FoamId, m_Foam);
            block.SetVectorArray(s_FoamShapeId, m_FoamShape);
            block.SetVectorArray(s_CrestId, m_Crest);
            block.SetVectorArray(s_WavesId, m_WaveShape);
        }

        void Rebuild()
        {
            if (m_Gradient == null)
            {
                m_Gradient = new Texture2D(GradientSize, Slots, TextureFormat.RGBAHalf, false, true)
                {
                    name = "HB Style Liquid Gradient",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            for (var slot = 0; slot < Slots; slot++)
            {
                var liquid = m_Layers[slot];

                if (liquid == null)
                {
                    m_Optics[slot] = new Vector4(0.65f, 0f, 0f, 0f);
                    m_Shore[slot] = s_Missing;
                    m_Style[slot] = new Vector4(0.125f, 0.25f, 1f, 1f);
                    m_Foam[slot] = Vector4.zero;
                    m_FoamShape[slot] = new Vector4(0.1f, 0f, 0.125f, 0f);
                    m_Crest[slot] = new Vector4(0.7f, 0.7f, 0.7f, 0.45f);
                    m_WaveShape[slot] = new Vector4(0f, 0f, 0f, 1f);

                    for (var x = 0; x < GradientSize; x++)
                        Write(slot, x, s_Missing, 1f);

                    continue;
                }

                Keys(liquid.depthGradient);

                m_Optics[slot] = new Vector4(liquid.smoothness, liquid.caustics, liquid.refractionBlur, liquid.glow);
                m_Shore[slot] = m_Colours[0];
                m_Style[slot] = new Vector4(liquid.diffuseWrap, liquid.diffuseSoftness, liquid.pulse,
                                            liquid.causticScale);

                var foam = liquid.foamColor.linear;

                m_Foam[slot] = new Vector4(foam.r, foam.g, foam.b, liquid.foamAmount);
                m_FoamShape[slot] = new Vector4(liquid.waterlineDepth, liquid.shoreFoam,
                                                liquid.tiling * liquid.patternScale,
                                                liquid.rainRipples ? 1f : 0f);

                var crest = liquid.crestColor.linear;

                m_Crest[slot] = new Vector4(crest.r, crest.g, crest.b, liquid.crestSoftness);
                m_WaveShape[slot] = new Vector4(liquid.windWaves ? -1f : liquid.waves, liquid.waveHeight,
                                                liquid.waveSteepness, liquid.waveLength);

                for (var x = 0; x < GradientSize; x++)
                {
                    var at = (float)x / (GradientSize - 1);

                    Write(slot, x, Colour(at), Alpha(at));
                }
            }

            m_Gradient.SetPixelData(m_Pixels, 0);
            m_Gradient.Apply(false, false);
        }

        void Keys(Gradient gradient)
        {
            var colours = gradient.colorKeys;
            var alphas = gradient.alphaKeys;
            var colourCount = Mathf.Clamp(colours.Length, 1, GradientKeys);

            var alphaCount = Mathf.Clamp(alphas.Length, 1, GradientKeys);

            for (var i = 0; i < GradientKeys; i++)
            {
                var colour = colours.Length > 0 ? colours[Mathf.Min(i, colourCount - 1)]
                                                : new GradientColorKey(s_Missing, 0f);
                var alpha = alphas.Length > 0 ? alphas[Mathf.Min(i, alphaCount - 1)] : new GradientAlphaKey(1f, 0f);
                var linear = colour.color.linear;

                m_Colours[i] = new Color(linear.r, linear.g, linear.b, 1f);
                m_ColourTimes[i] = colour.time;
                m_Alphas[i] = alpha.alpha;
                m_AlphaTimes[i] = alpha.time;
            }
        }

        Color Colour(float at)
        {
            var colour = m_Colours[0];

            for (var stop = 1; stop < GradientKeys; stop++)
            {
                var gap = Mathf.Max(m_ColourTimes[stop] - m_ColourTimes[stop - 1], KeyGap);

                colour = Color.LerpUnclamped(colour, m_Colours[stop],
                                             Mathf.Clamp01((at - m_ColourTimes[stop - 1]) / gap));
            }

            return colour;
        }

        float Alpha(float at)
        {
            var alpha = m_Alphas[0];

            for (var stop = 1; stop < GradientKeys; stop++)
            {
                var gap = Mathf.Max(m_AlphaTimes[stop] - m_AlphaTimes[stop - 1], KeyGap);

                alpha = Mathf.LerpUnclamped(alpha, m_Alphas[stop], Mathf.Clamp01((at - m_AlphaTimes[stop - 1]) / gap));
            }

            return Mathf.Clamp01(alpha);
        }

        void Write(int slot, int x, Color colour, float alpha)
        {
            var index = (slot * GradientSize + x) * 4;

            m_Pixels[index] = Mathf.FloatToHalf(colour.r);
            m_Pixels[index + 1] = Mathf.FloatToHalf(colour.g);
            m_Pixels[index + 2] = Mathf.FloatToHalf(colour.b);
            m_Pixels[index + 3] = Mathf.FloatToHalf(alpha);
        }
    }
}
