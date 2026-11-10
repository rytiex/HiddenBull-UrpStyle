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
        const int SeaRings = 6;
        const int SeaTiles = 3;
        const float SeaOverlap = 1f / (StyleTerrainTree.PatchQuads * 8f);
        const float Empty = 1e9f;
        const float LevelZero = 32768f;
        const float LevelSpan = 32767f;
        const int CutFrom = 32768;
        const float BoundsMargin = 1f;
        const float WaveMargin = 2f;
        const float DetailShare = 0.25f;
        const float LeafReach = 3f;

        static readonly int s_RangesId = Shader.PropertyToID("_HB_LiquidRanges");

        static readonly int s_IdId = Shader.PropertyToID("_HB_LiquidId");
        static readonly int s_KeepId = Shader.PropertyToID("_HB_LiquidKeep");
        static readonly int s_EyeId = Shader.PropertyToID("_HB_LiquidEye");

        static readonly Matrix4x4[] s_Batch = new Matrix4x4[BatchSize];

        readonly StyleTerrainTree m_Tree = new StyleTerrainTree();
        readonly StyleLiquidLook m_Look = new StyleLiquidLook();
        readonly StyleLiquidCalm m_Calm = new StyleLiquidCalm();
        readonly List<StyleTerrainNode> m_Nodes = new List<StyleTerrainNode>();
        readonly List<Matrix4x4> m_Full = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_Quarters = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_Sea = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_RoofFull = new List<Matrix4x4>();
        readonly List<Matrix4x4> m_RoofQuarters = new List<Matrix4x4>();
        readonly Vector4[] m_Ranges = new Vector4[StyleTerrainTree.MaxLevels];
        readonly HBStyleLiquidLayer[] m_Layers = new HBStyleLiquidLayer[Slots];

        Material m_Material;
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
        float m_Leaf;

        StyleLiquidWaves m_Waves;
        Camera m_Camera;
        MaterialPropertyBlock m_Block;
        Vector3 m_Origin;
        Vector3 m_Local;
        float m_RoofReach = -1f;

        internal StyleLiquidCalm calm => m_Calm;

        internal void Draw(Component owner, ScriptableRenderContext context, Camera camera, HBStyleTerrainData data,
                           Shader shader,
                           ComputeShader waves, MaterialPropertyBlock block, Vector3 origin, Vector3 local,
                           float[] planes, float far, float detailRange, int layer)
        {
            m_Camera = null;
            m_RoofReach = -1f;

            if (!data.liquidOn || shader == null || !Ready(shader) || !Leaves(data))
                return;

            m_Waves ??= StyleLiquidWaves.Acquire();

            Bind(data, block);
            m_Waves.Bind(context, waves, block);
            block.SetFloat(s_IdId, HBStyleLiquidCutter.TerrainKind);
            block.SetFloat(s_KeepId, Kept(data, origin) ? 1f : 0f);
            block.SetFloat(s_EyeId, HBStyleLiquid.EyeLevel(owner, camera.transform.position));
            m_Calm.Bind(context, waves, data, block);

            m_Tree.SetRanges(Mathf.Max(detailRange * DetailShare, m_Tree.leafSize * LeafReach));

            for (var level = 0; level < StyleTerrainTree.MaxLevels; level++)
            {
                var end = m_Tree.Range(level);
                m_Ranges[level] = new Vector4(StyleTerrainTree.MorphStart(end), end, 0f, 0f);
            }

            block.SetVectorArray(s_RangesId, m_Ranges);
            m_Tree.Select(local.x, local.y, local.z, planes, far, m_Nodes);
            Fill(origin, m_Full, m_Quarters);
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

            m_Camera = camera;
            m_Block = block;
            m_Origin = origin;
            m_Local = local;
        }

        internal void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Look.Dispose();
            if (m_Waves != null)
                StyleLiquidWaves.Release();

            m_Waves = null;
            m_Calm.Dispose();
            m_Material = null;
            m_Built = false;
            m_BuiltData = null;
        }

        internal bool Touches(HBStyleTerrainData data, Vector3 origin, Bounds volume)
        {
            var leaves = data != null ? data.patches : 0;

            if (!m_Built || m_LeafMin == null || m_LeafMin.Length != leaves * leaves || m_Leaf <= 0f)
                return false;

            var min = volume.min - origin;
            var max = volume.max - origin;
            var span = leaves * m_Leaf;
            var sea = data.seaLevel;

            if (min.x < 0f || min.z < 0f || max.x > span || max.z > span)
            {
                if (min.y <= sea + WaveMargin && max.y >= sea - WaveMargin)
                    return true;
            }

            if (max.x < 0f || max.z < 0f || min.x > span || min.z > span)
                return false;

            var x0 = Mathf.Clamp(Mathf.FloorToInt(min.x / m_Leaf), 0, leaves - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt(min.z / m_Leaf), 0, leaves - 1);
            var x1 = Mathf.Clamp(Mathf.FloorToInt(max.x / m_Leaf), 0, leaves - 1);
            var z1 = Mathf.Clamp(Mathf.FloorToInt(max.z / m_Leaf), 0, leaves - 1);

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var index = z * leaves + x;

                    if (m_LeafMin[index] <= max.y && m_LeafMax[index] >= min.y)
                        return true;
                }
            }

            return false;
        }

        bool Kept(HBStyleTerrainData data, Vector3 origin)
        {
            foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
            {
                if (cutter != null && cutter.mode == HBStyleLiquidCutMode.KeepInside &&
                    cutter.Affects(HBStyleLiquidCutTargets.Terrain) && cutter.Spans(out var volume) &&
                    Touches(data, origin, volume))
                    return true;
            }

            return false;
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
            m_Leaf = leaf;

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

        void Fill(Vector3 origin, List<Matrix4x4> full, List<Matrix4x4> quarters)
        {
            full.Clear();
            quarters.Clear();

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
                    quarters.Add(matrix);
                else
                    full.Add(matrix);
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

        internal void DrawRoof(RasterCommandBuffer cmd, Camera camera, HBStyleTerrainData data, string pass,
                               float reach)
        {
            if (camera != m_Camera || m_Material == null || data == null || !data.liquidOn)
                return;

            var index = m_Material.FindPass(pass);

            if (index < 0)
                return;

            if (reach != m_RoofReach)
            {
                m_Tree.Select(m_Local.x, m_Local.y, m_Local.z, null, reach, m_Nodes);
                Fill(m_Origin, m_RoofFull, m_RoofQuarters);
                Sea(m_Origin, m_Local, null, reach, data.seaLevel, out _);
                m_RoofReach = reach;
            }

            Instanced(cmd, HBStyleTerrain.Patch(false), m_RoofFull, index);
            Instanced(cmd, HBStyleTerrain.Patch(true), m_RoofQuarters, index);
            Instanced(cmd, HBStyleTerrain.Patch(false), m_Sea, index);
        }

        void Instanced(RasterCommandBuffer cmd, Mesh mesh, List<Matrix4x4> instances, int pass)
        {
            for (var start = 0; start < instances.Count; start += BatchSize)
            {
                var count = Mathf.Min(BatchSize, instances.Count - start);

                instances.CopyTo(start, s_Batch, 0, count);
                cmd.DrawMeshInstanced(mesh, 0, m_Material, pass, s_Batch, count, m_Block);
            }
        }

        internal void Bind(HBStyleTerrainData data, MaterialPropertyBlock block)
        {
            for (var slot = 0; slot < Slots; slot++)
                m_Layers[slot] = slot < data.liquidCount ? data.Liquid(slot) : null;

            m_Look.Bind(block, m_Layers);
        }
    }
}
