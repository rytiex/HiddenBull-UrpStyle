using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleLiquidQuality
    {
        Low,
        Medium,
        High
    }

    public enum HBStyleLiquidSize
    {
        [InspectorName("256 m")] Size256 = 256,
        [InspectorName("512 m")] Size512 = 512,
        [InspectorName("1024 m")] Size1024 = 1024,
        [InspectorName("2048 m")] Size2048 = 2048,
        [InspectorName("4096 m")] Size4096 = 4096,
        [InspectorName("8192 m")] Size8192 = 8192
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Liquid/HB Style Liquid Terrain")]
    public sealed class HBStyleLiquidTerrain : MonoBehaviour
    {
        public static readonly float[] Distances = { 0.5f, 1f, 2f, 4f };

        const int MaxLevels = 11;
        const int Cells = 64;
        const float Cell = 1f;
        const float Extent = 100000f;
        const int Channels = 4;
        const string SurfaceName = "Style Liquid Surface";
        const HideFlags SurfaceFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        const float CutMargin = 4f;
        const string StreamsKeyword = "_HB_LIQUID_STREAMS";
        const int TileCount = HBStyleLiquidTerrainData.Tiles;
        const int TileCells = 64;
        const float TileRise = 16f;
        const int FarSteps = 4;
        const float FarSpan = 16f;
        const float Slack = 0.02f;
        const int PatchCells = 16;
        const int PatchSide = Cells / PatchCells;
        const int PatchSlots = PatchSide * PatchSide;
        const float PatchMargin = 0.25f;
        const float PatchSway = 8f;
        const float Rise = 16f;
        const float Coarsest = 6f;
        const float Snap = 2f;
        const float FitLeast = -1f;
        const float FitMost = 4f;
        const float FitSlack = 1e-4f;

        static readonly int s_GridId = Shader.PropertyToID("_WaterGrid");
        static readonly int s_AreaId = Shader.PropertyToID("_WaterArea");
        static readonly int s_LevelMapId = Shader.PropertyToID("_WaterLevelMap");
        static readonly int s_WeightMapId = Shader.PropertyToID("_WaterWeightMap");
        static readonly int s_StreamsId = Shader.PropertyToID("_WaterStreams");
        static readonly int s_BlocksId = Shader.PropertyToID("_WaterBlockMap");
        static readonly int s_StencilId = Shader.PropertyToID("_WaterStencil");
        static readonly int s_SeaAreaId = Shader.PropertyToID("_HB_LiquidArea");
        static readonly int s_SeaLevelMapId = Shader.PropertyToID("_HB_LiquidLevelMap");

        static readonly Dictionary<GameObject, HBStyleLiquidTerrain> s_Owned =
            new Dictionary<GameObject, HBStyleLiquidTerrain>();
        static readonly List<HBStyleLiquidTerrain> s_Terrains = new List<HBStyleLiquidTerrain>();

        static float s_Coarse = 1f;
        static readonly List<Vector3> s_Points = new List<Vector3>();
        static readonly List<Vector2> s_Zeros = new List<Vector2>();
        static readonly List<int> s_Corners = new List<int>();
        static readonly Dictionary<int, int[]> s_Lattices = new Dictionary<int, int[]>();
        static Mesh s_Patch;
        static float[] s_Heights;
        static float[] s_Errors;
        static ulong s_Sheets;
        static int s_Cells;
        static int s_Far;
        static float s_Slack;
        static int[] s_Found;

        [SerializeField]
        [Tooltip("Up to four materials using the HB URP Style/Liquid shader. The first is the liquid itself: it " +
                 "fills everything that is not painted and carries on to the horizon. The others are painted " +
                 "over it. Nothing is drawn until the first one is assigned.")]
        List<Material> m_Layers = new List<Material>();

        [SerializeField]
        [Tooltip("How far the liquid is drawn from the camera, in kilometres. Shorter draws fewer vertices; " +
                 "let fog or the shape of the land hide where it ends.")]
        float m_Distance = 4f;

        [SerializeField]
        [Tooltip("The size of the square area that can be sculpted and painted, in metres, centred on this " +
                 "object. Outside it the liquid carries on to the horizon at this object's height. The " +
                 "object snaps to a matching grid so sculpted liquid lines up with the open liquid around it.")]
        HBStyleLiquidSize m_Extent;

        [SerializeField]
        [HideInInspector]
        Vector2 m_Size = new Vector2(512f, 512f);

        [SerializeField]
        [Tooltip("The asset that stores what is sculpted and painted on this liquid. Without one the liquid " +
                 "is flat everywhere.")]
        HBStyleLiquidTerrainData m_Data;

        GameObject m_Surface;
        MeshRenderer m_Renderer;
        MeshFilter m_Filter;
        MaterialPropertyBlock m_Properties;
        int m_Channel = -1;
        Vector4 m_Area;
        Texture m_LevelMap;
        Texture m_WeightMap;
        int m_Version = -1;
        MaterialPropertyBlock m_TileProperties;
        readonly List<MeshRenderer> m_Tiles = new List<MeshRenderer>();
        readonly List<MeshFilter> m_Sheets = new List<MeshFilter>();
        Vector3 m_PlacedAt;
        Vector2 m_PlacedSize;
        int m_PlacedLayer;
        int m_Stamp;
        readonly List<Mesh> m_Meshes = new List<Mesh>();
        readonly List<ulong> m_Patterns = new List<ulong>();
        readonly List<int> m_Stamps = new List<int>();
        readonly List<MeshRenderer> m_Patches = new List<MeshRenderer>();
        readonly List<Vector4> m_Spans = new List<Vector4>();
        Material m_Sea;
        Matrix4x4[] m_Seen;
        int m_Rings;
        bool m_Stale;

        public static bool any => s_Terrains.Count > 0;

        public Material material => m_Layers.Count > 0 ? m_Layers[0] : null;

        public List<Material> layers => m_Layers;

        public float distance
        {
            get => m_Distance;
            set
            {
                m_Distance = Mathf.Clamp(value, Distances[0], Distances[Distances.Length - 1]);
                Apply();
            }
        }

        public Vector2 size
        {
            get => m_Size;
            set
            {
                m_Extent = Picked(Mathf.Max(value.x, value.y), true);
                m_Size = Vector2.one * (int)m_Extent;
                Apply();
            }
        }

        static HBStyleLiquidSize Picked(float size, bool cover)
        {
            var step = Mathf.Log(Mathf.Max(size, 1f) / HBStyleLiquidTerrainData.Size, 2f);

            step = Mathf.Clamp(cover ? Mathf.Ceil(step - FitSlack) : Mathf.Round(step), FitLeast, FitMost);

            return (HBStyleLiquidSize)Mathf.RoundToInt(HBStyleLiquidTerrainData.Size * Mathf.Pow(2f, step));
        }

        void Settle()
        {
            if ((int)m_Extent == 0)
                m_Extent = Picked(Mathf.Max(m_Size.x, m_Size.y), false);

            m_Size = Vector2.one * (int)m_Extent;
        }

        void Align()
        {
            Settle();

            var block = m_Size / HBStyleLiquidTerrainData.Blocks;
            var position = transform.position;
            var snapped = new Vector3(Mathf.Round(position.x / block.x) * block.x, position.y,
                                      Mathf.Round(position.z / block.y) * block.y);

            if (snapped != position)
                transform.position = snapped;
        }

        public HBStyleLiquidTerrainData data
        {
            get => m_Data;
            set
            {
                m_Data = value;
                Apply();
            }
        }

        internal static List<HBStyleLiquidTerrain> terrains => s_Terrains;

        internal static HBStyleLiquidTerrain sea
        {
            get
            {
                foreach (var terrain in s_Terrains)
                {
                    if (terrain != null)
                        return terrain;
                }

                return null;
            }
        }

        internal static Texture seaLevelMap => sea != null ? sea.levelMap : Texture2D.blackTexture;

        internal static Vector4 seaArea => sea != null ? sea.area : Vector4.zero;

        internal static int seaChannel => sea != null ? sea.m_Channel : -1;

        internal int channel => m_Channel;

        internal float reach => m_Distance * 1000f;

        internal float level => transform.position.y;

        internal Vector4 cells => new Vector4(Cell * s_Coarse, reach, m_Channel, 0f);

        Vector4 tiles => new Vector4(Mathf.Max(m_Size.x, m_Size.y) / HBStyleLiquidTerrainData.Size * s_Coarse, reach,
                                     m_Channel, 1f);

        internal void Draw(RasterCommandBuffer cmd, Material drawn, int pass, Plane[] planes)
        {
            if (m_Properties == null)
                return;

            if (m_Seen == null || m_Seen.Length < m_Patches.Count)
                m_Seen = new Matrix4x4[m_Patches.Count];

            var seen = 0;

            foreach (var patch in m_Patches)
            {
                if (patch != null && GeometryUtility.TestPlanesAABB(planes, patch.bounds))
                    m_Seen[seen++] = patch.transform.localToWorldMatrix;
            }

            if (seen > 0 && m_Sea != null)
                cmd.DrawMeshInstanced(Patch(), 0, m_Sea, pass, m_Seen, seen);

            for (var index = 0; index < m_Tiles.Count; index++)
            {
                var tile = m_Tiles[index];

                if (tile != null && tile.enabled && GeometryUtility.TestPlanesAABB(planes, tile.bounds))
                    cmd.DrawMesh(m_Meshes[index * FarSteps], tile.transform.localToWorldMatrix, drawn, 0, pass,
                                 m_TileProperties);
            }
        }

        void Fill(MaterialPropertyBlock block, Vector4 grid)
        {
            block.SetVector(s_GridId, grid);
            block.SetVector(s_AreaId, m_Area);
            block.SetTexture(s_LevelMapId, m_LevelMap);
            block.SetTexture(s_WeightMapId, m_WeightMap);
            block.SetFloat(s_StreamsId, streams);
            block.SetTexture(s_BlocksId, m_Data != null ? m_Data.blockMap : Texture2D.blackTexture);
            StyleLiquidLayers.Write(m_Layers, block, null);
        }

        int Stamp()
        {
            var stamp = cells.GetHashCode() * 31 + tiles.GetHashCode();

            stamp = stamp * 31 + streams.GetHashCode();

            foreach (var layer in m_Layers)
                stamp = stamp * 31 + (layer != null ? layer.ComputeCRC() : 0);

            return stamp == 0 ? 1 : stamp;
        }

        void Commit()
        {
            m_Renderer.SetPropertyBlock(m_Properties);

            foreach (var tile in m_Tiles)
            {
                if (tile != null)
                    tile.SetPropertyBlock(m_TileProperties);
            }
        }

        void Lay()
        {
            var wanted = m_Data != null ? TileCount * TileCount : 0;

            while (m_Tiles.Count > wanted)
            {
                var last = m_Tiles[m_Tiles.Count - 1];

                m_Tiles.RemoveAt(m_Tiles.Count - 1);
                m_Sheets.RemoveAt(m_Sheets.Count - 1);

                for (var step = 0; step < FarSteps; step++)
                {
                    CoreUtils.Destroy(m_Meshes[m_Meshes.Count - 1]);
                    m_Meshes.RemoveAt(m_Meshes.Count - 1);
                }

                m_Patterns.RemoveAt(m_Patterns.Count - 1);
                m_Stamps.RemoveAt(m_Stamps.Count - 1);

                if (last == null)
                    continue;

                s_Owned.Remove(last.gameObject);
                CoreUtils.Destroy(last.gameObject);
            }

            while (m_Tiles.Count < wanted)
            {
                var tile = new GameObject(SurfaceName)
                {
                    hideFlags = SurfaceFlags
                };

                s_Owned[tile] = this;
                m_Sheets.Add(tile.AddComponent<MeshFilter>());

                var drawn = tile.AddComponent<MeshRenderer>();

                drawn.shadowCastingMode = ShadowCastingMode.Off;
                drawn.lightProbeUsage = LightProbeUsage.Off;
                drawn.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

                m_Tiles.Add(drawn);

                for (var step = 0; step < FarSteps; step++)
                    m_Meshes.Add(new Mesh { name = "Style Liquid Tile", hideFlags = HideFlags.HideAndDontSave });

                m_Patterns.Add(0ul);
                m_Stamps.Add(0);
            }

            for (var index = 0; index < m_Tiles.Count; index++)
            {
                var tile = m_Tiles[index];

                if (tile == null)
                    continue;

                m_Sheets[index].sharedMesh = m_Meshes[index * FarSteps];
                tile.sharedMaterial = material;
                tile.forceRenderingOff = material == null;
            }

            m_Version = -1;

            m_Stale = true;
        }

        void Place()
        {
            if (m_Tiles.Count == 0 || m_Data == null)
                return;

            var centre = transform.position;

            if (m_Version == m_Data.version && centre == m_PlacedAt && m_Size == m_PlacedSize
                && gameObject.layer == m_PlacedLayer)
                return;

            var span = new Vector2(m_Size.x / TileCount, m_Size.y / TileCount);
            var cells = Mathf.RoundToInt(TileCells / s_Coarse);

            m_Version = m_Data.version;
            m_PlacedAt = centre;
            m_PlacedSize = m_Size;
            m_PlacedLayer = gameObject.layer;

            for (var index = 0; index < m_Tiles.Count; index++)
            {
                var tile = m_Tiles[index];

                if (tile == null)
                    continue;

                var sheets = m_Data.Sheets(index);
                var stamp = sheets != 0ul ? Survey(m_Data, index, cells) : 0;

                tile.enabled = sheets != 0ul;

                if (m_Stale || sheets != m_Patterns[index] || stamp != m_Stamps[index])
                {
                    m_Patterns[index] = sheets;
                    m_Stamps[index] = stamp;

                    if (sheets != 0ul)
                    {
                        Weigh(sheets, cells);

                        for (var step = 0; step < FarSteps; step++)
                            Build(m_Meshes[index * FarSteps + step], sheets, cells, 1 << step);
                    }
                }

                var corner = new Vector3(centre.x - m_Size.x * 0.5f + index % TileCount * span.x, level,
                                         centre.z - m_Size.y * 0.5f + index / TileCount * span.y);

                tile.transform.SetPositionAndRotation(corner, Quaternion.identity);
                tile.transform.localScale = new Vector3(span.x, 1f, span.y);
                tile.gameObject.layer = gameObject.layer;

                var range = m_Data.Range(index);

                tile.localBounds = new Bounds(
                    new Vector3(0.5f, (range.x + range.y) * 0.5f, 0.5f),
                    new Vector3(1f + TileRise / span.x, range.y - range.x + TileRise * 2f, 1f + TileRise / span.y));
            }

            m_Stale = false;
        }

        static void Arrange(ScriptableRenderContext context, Camera camera)
        {
            var eye = camera.transform.position;

            foreach (var terrain in s_Terrains)
            {
                if (terrain != null)
                {
                    terrain.Thin(new Vector2(eye.x, eye.z));
                    terrain.Frame(eye);
                }
            }
        }

        void Spread(int rings)
        {
            if (rings != m_Rings || m_Patches.Count == 0)
            {
                foreach (var old in m_Patches)
                {
                    if (old == null)
                        continue;

                    s_Owned.Remove(old.gameObject);
                    CoreUtils.Destroy(old.gameObject);
                }

                m_Patches.Clear();
                m_Spans.Clear();
                m_Rings = rings;

                for (var slot = 0; slot < rings * PatchSlots; slot++)
                {
                    var ring = slot / PatchSlots;
                    var x = slot % PatchSide;
                    var z = slot % PatchSlots / PatchSide;

                    if (ring > 0 && x > 0 && z > 0 && x < PatchSide - 1 && z < PatchSide - 1)
                        continue;

                    var patch = new GameObject(SurfaceName)
                    {
                        hideFlags = SurfaceFlags
                    };

                    var scale = 1 << ring;
                    var half = Cells / 2;
                    var span = new Vector4((x * PatchCells - half) * scale, (z * PatchCells - half) * scale,
                                           ((x + 1) * PatchCells - half) * scale,
                                           ((z + 1) * PatchCells - half) * scale);

                    s_Owned[patch] = this;
                    patch.transform.SetParent(m_Surface.transform, false);
                    patch.transform.localPosition = new Vector3(span.x, 0f, span.y);
                    patch.transform.localScale = new Vector3(scale, 1f, scale);
                    patch.AddComponent<MeshFilter>().sharedMesh = Patch();

                    var drawn = patch.AddComponent<MeshRenderer>();

                    drawn.shadowCastingMode = ShadowCastingMode.Off;
                    drawn.lightProbeUsage = LightProbeUsage.Off;
                    drawn.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

                    m_Patches.Add(drawn);
                    m_Spans.Add(span);
                }
            }

            foreach (var patch in m_Patches)
            {
                if (patch == null)
                    continue;

                patch.gameObject.layer = gameObject.layer;
                patch.sharedMaterial = m_Sea;
                patch.forceRenderingOff = m_Sea == null;
            }
        }

        bool Washed()
        {
            return m_Sea != null
                && (!m_Sea.HasTexture(s_LevelMapId) || m_Sea.GetTexture(s_LevelMapId) != m_LevelMap
                    || !m_Sea.HasVector(s_AreaId) || m_Sea.GetVector(s_AreaId) != m_Area
                    || !m_Sea.enableInstancing || !m_Sea.HasFloat(s_StencilId) || m_Sea.GetFloat(s_StencilId) != 0f
                    || StyleLiquidLayers.Lost(m_Layers, m_Sea));
        }

        void Tint()
        {
            var source = material;

            if (source == null)
            {
                CoreUtils.Destroy(m_Sea);
                m_Sea = null;

                foreach (var patch in m_Patches)
                {
                    if (patch != null)
                        patch.forceRenderingOff = true;
                }

                return;
            }

            if (m_Sea == null || m_Sea.shader != source.shader)
            {
                CoreUtils.Destroy(m_Sea);
                m_Sea = new Material(source)
                {
                    name = "Style Liquid Sea",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            m_Sea.CopyPropertiesFromMaterial(source);
            m_Sea.renderQueue = source.renderQueue;
            m_Sea.enableInstancing = true;
            m_Sea.SetVector(s_GridId, cells);
            m_Sea.SetVector(s_AreaId, m_Area);
            m_Sea.SetTexture(s_LevelMapId, m_LevelMap);
            m_Sea.SetTexture(s_WeightMapId, m_WeightMap);
            m_Sea.SetFloat(s_StreamsId, streams);
            m_Sea.SetTexture(s_BlocksId, m_Data != null ? m_Data.blockMap : Texture2D.blackTexture);
            m_Sea.SetFloat(s_StencilId, 0f);
            StyleLiquidLayers.Write(m_Layers, null, m_Sea);

            foreach (var patch in m_Patches)
            {
                if (patch == null || patch.sharedMaterial == m_Sea)
                    continue;

                patch.sharedMaterial = m_Sea;
                patch.forceRenderingOff = false;
            }
        }

        static Mesh Patch()
        {
            if (s_Patch != null)
                return s_Patch;

            var side = PatchCells + 1;
            var vertices = new List<Vector3>(side * side);
            var levels = new List<Vector2>(side * side);
            var triangles = new List<int>(PatchCells * PatchCells * 6);

            for (var z = 0; z <= PatchCells; z++)
            {
                for (var x = 0; x <= PatchCells; x++)
                {
                    vertices.Add(new Vector3(x, 0f, z));
                    levels.Add(Vector2.zero);
                }
            }

            for (var z = 0; z < PatchCells; z++)
            {
                for (var x = 0; x < PatchCells; x++)
                {
                    var corner = z * side + x;

                    triangles.Add(corner);
                    triangles.Add(corner + side);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + side);
                    triangles.Add(corner + side + 1);
                }
            }

            s_Patch = new Mesh
            {
                name = "Style Liquid Patch",
                hideFlags = HideFlags.HideAndDontSave
            };

            s_Patch.SetVertices(vertices);
            s_Patch.SetUVs(0, levels);
            s_Patch.SetTriangles(triangles, 0);
            s_Patch.bounds = new Bounds(Vector3.zero, new Vector3(Extent, Extent, Extent));

            return s_Patch;
        }

        void Frame(Vector3 eye)
        {
            if (m_Patches.Count == 0)
                return;

            var first = sea;
            var seat = level + (first != null ? first.LevelAt(new Vector2(eye.x, eye.z)) - first.level : 0f);
            var coarse = Mathf.Clamp(Mathf.Log(Mathf.Max(Mathf.Abs(eye.y - seat), 1e-3f) / Rise, 2f) + 1f, 0f,
                                     Coarsest);
            var cell = Cell * s_Coarse * (1 << Mathf.FloorToInt(coarse));
            var snap = cell * Snap;
            var origin = new Vector2(Mathf.Floor(eye.x / snap) * snap, Mathf.Floor(eye.z / snap) * snap);

            for (var index = 0; index < m_Patches.Count; index++)
            {
                var patch = m_Patches[index];

                if (patch == null)
                    continue;

                var span = m_Spans[index];
                var wide = (span.z - span.x) * cell;
                var size = wide * (1f + PatchMargin * 2f) + PatchSway * 2f;

                var bounds = new Bounds(
                    new Vector3(origin.x + (span.x + span.z) * 0.5f * cell, level,
                                origin.y + (span.y + span.w) * 0.5f * cell),
                    new Vector3(size, TileRise * 2f, size));

                // Every write marks the renderer changed, which Unity then re-processes on the next cull.
                if (patch.bounds != bounds)
                    patch.bounds = bounds;
            }
        }

        void Thin(Vector2 eye)
        {
            if (m_Data == null || m_Tiles.Count == 0)
                return;

            var centre = transform.position;
            var span = new Vector2(m_Size.x / TileCount, m_Size.y / TileCount);
            var reach = Mathf.Max(span.x, span.y) * s_Coarse / TileCells * FarSpan;

            for (var index = 0; index < m_Tiles.Count; index++)
            {
                var tile = m_Tiles[index];

                if (tile == null || !tile.enabled)
                    continue;

                var low = new Vector2(centre.x - m_Size.x * 0.5f + index % TileCount * span.x,
                                      centre.z - m_Size.y * 0.5f + index / TileCount * span.y);
                var away = Mathf.Max(Mathf.Max(low.x - eye.x, eye.x - low.x - span.x),
                                     Mathf.Max(low.y - eye.y, eye.y - low.y - span.y));
                var step = 0;

                while (step + 1 < FarSteps && away >= reach * (2 << step))
                    step++;

                var wanted = m_Meshes[index * FarSteps + step];

                if (m_Sheets[index].sharedMesh != wanted)
                    m_Sheets[index].sharedMesh = wanted;
            }
        }

        static int[] Lattice(int cells)
        {
            if (s_Lattices.TryGetValue(cells, out var coords))
                return coords;

            var count = cells * cells * 2 - 2;

            coords = new int[count * 4];

            for (var index = 0; index < count; index++)
            {
                var id = index + 2;
                var ax = 0;
                var ay = 0;
                var bx = 0;
                var by = 0;
                var cx = 0;
                var cy = 0;

                if ((id & 1) != 0)
                {
                    bx = cells;
                    by = cells;
                    cx = cells;
                }
                else
                {
                    ax = cells;
                    ay = cells;
                    cy = cells;
                }

                while ((id >>= 1) > 1)
                {
                    var mx = (ax + bx) >> 1;
                    var my = (ay + by) >> 1;

                    if ((id & 1) != 0)
                    {
                        bx = ax;
                        by = ay;
                        ax = cx;
                        ay = cy;
                    }
                    else
                    {
                        ax = bx;
                        ay = by;
                        bx = cx;
                        by = cy;
                    }

                    cx = mx;
                    cy = my;
                }

                coords[index * 4] = ax;
                coords[index * 4 + 1] = ay;
                coords[index * 4 + 2] = bx;
                coords[index * 4 + 3] = by;
            }

            s_Lattices[cells] = coords;

            return coords;
        }

        static int Survey(HBStyleLiquidTerrainData data, int tile, int cells)
        {
            var side = cells + 1;
            var span = HBStyleLiquidTerrainData.Size / TileCount;
            var stride = span / cells;
            var left = tile % TileCount * span;
            var bottom = tile / TileCount * span;
            var stamp = 17;

            if (s_Heights == null || s_Heights.Length != side * side)
            {
                s_Heights = new float[side * side];
                s_Errors = new float[side * side];
                s_Found = new int[side * side];
            }

            for (var z = 0; z <= cells; z++)
            {
                for (var x = 0; x <= cells; x++)
                {
                    var height = data.Sample((left + x * stride) / (float)HBStyleLiquidTerrainData.Size,
                                             (bottom + z * stride) / (float)HBStyleLiquidTerrainData.Size);

                    s_Heights[z * side + x] = height;
                    stamp = stamp * 31 + height.GetHashCode();
                }
            }

            return stamp;
        }

        static void Weigh(ulong sheets, int cells)
        {
            var side = cells + 1;
            var across = HBStyleLiquidTerrainData.Blocks / TileCount;
            var per = cells / across;
            var coords = Lattice(cells);
            var count = coords.Length / 4;
            var parents = count - cells * cells;

            System.Array.Clear(s_Errors, 0, s_Errors.Length);

            for (var block = 0; block < across * across; block++)
            {
                if ((sheets & (1ul << block)) == 0ul)
                    continue;

                var column = block % across;
                var row = block / across;

                for (var step = 0; step <= per; step++)
                {
                    if (column == 0 || (sheets & (1ul << (block - 1))) == 0ul)
                        s_Errors[(row * per + step) * side + column * per] = float.MaxValue;

                    if (column == across - 1 || (sheets & (1ul << (block + 1))) == 0ul)
                        s_Errors[(row * per + step) * side + (column + 1) * per] = float.MaxValue;

                    if (row == 0 || (sheets & (1ul << (block - across))) == 0ul)
                        s_Errors[row * per * side + column * per + step] = float.MaxValue;

                    if (row == across - 1 || (sheets & (1ul << (block + across))) == 0ul)
                        s_Errors[(row + 1) * per * side + column * per + step] = float.MaxValue;
                }
            }

            for (var index = count - 1; index >= 0; index--)
            {
                var ax = coords[index * 4];
                var ay = coords[index * 4 + 1];
                var bx = coords[index * 4 + 2];
                var by = coords[index * 4 + 3];
                var mx = (ax + bx) >> 1;
                var my = (ay + by) >> 1;
                var cx = mx + my - ay;
                var cy = my + ax - mx;
                var middle = my * side + mx;
                var error = Mathf.Abs((s_Heights[ay * side + ax] + s_Heights[by * side + bx]) * 0.5f
                                      - s_Heights[middle]);

                if (index < parents)
                {
                    error = Mathf.Max(error, Mathf.Max(s_Errors[((ay + cy) >> 1) * side + ((ax + cx) >> 1)],
                                                       s_Errors[((by + cy) >> 1) * side + ((bx + cx) >> 1)]));
                }

                s_Errors[middle] = Mathf.Max(s_Errors[middle], error);
            }
        }

        static void Build(Mesh built, ulong sheets, int cells, int far)
        {
            s_Points.Clear();
            s_Zeros.Clear();
            s_Corners.Clear();

            for (var slot = 0; slot < s_Found.Length; slot++)
                s_Found[slot] = -1;

            s_Sheets = sheets;
            s_Cells = cells;
            s_Far = Mathf.Min(far, cells / (HBStyleLiquidTerrainData.Blocks / TileCount));
            s_Slack = Slack * s_Far;

            Split(0, 0, cells, cells, cells, 0);
            Split(cells, cells, 0, 0, 0, cells);

            built.Clear();
            built.SetVertices(s_Points);
            built.SetUVs(0, s_Zeros);
            built.SetTriangles(s_Corners, 0);
            built.bounds = new Bounds(new Vector3(0.5f, 0f, 0.5f), new Vector3(1f, TileRise * 2f, 1f));
        }

        static void Split(int ax, int ay, int bx, int by, int cx, int cy)
        {
            var side = s_Cells + 1;
            var mx = (ax + bx) >> 1;
            var my = (ay + by) >> 1;

            if (Mathf.Abs(ax - cx) + Mathf.Abs(ay - cy) > 1
                && (s_Errors[my * side + mx] > s_Slack || Mathf.Max(Mathf.Abs(ax - bx), Mathf.Abs(ay - by)) > s_Far))
            {
                Split(cx, cy, ax, ay, mx, my);
                Split(bx, by, cx, cy, mx, my);
                return;
            }

            var across = HBStyleLiquidTerrainData.Blocks / TileCount;
            var per = s_Cells / across;
            var block = (ay + by + cy) / 3 / per * across + (ax + bx + cx) / 3 / per;

            if ((s_Sheets & (1ul << block)) == 0ul)
                return;

            s_Corners.Add(Corner(ax, ay));
            s_Corners.Add(Corner(bx, by));
            s_Corners.Add(Corner(cx, cy));
        }

        static int Corner(int x, int y)
        {
            var side = s_Cells + 1;
            var at = s_Found[y * side + x];

            if (at >= 0)
                return at;

            at = s_Points.Count;
            s_Found[y * side + x] = at;
            s_Points.Add(new Vector3((float)x / s_Cells, 0f, (float)y / s_Cells));
            s_Zeros.Add(Vector2.zero);

            return at;
        }

        internal Vector4 area
        {
            get
            {
                if (m_Data == null)
                    return Vector4.zero;

                var position = transform.position;

                return new Vector4(position.x, position.z, 1f / Mathf.Max(m_Size.x, 1f), 1f / Mathf.Max(m_Size.y, 1f));
            }
        }

        internal Texture levelMap => m_Data != null ? m_Data.levelMap : Texture2D.blackTexture;

        internal Texture weightMap => m_Data != null ? m_Data.weightMap : Texture2D.blackTexture;

        float streams => m_Data != null && m_Data.flowing ? 1f : 0f;

        internal float LevelAt(Vector2 position)
        {
            if (m_Data == null)
                return level;

            var own = area;

            return level + m_Data.Sample((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f);
        }

        internal Vector3 PaintedAt(Vector2 position)
        {
            if (m_Data == null)
                return Vector3.zero;

            var own = area;

            return m_Data.Painted((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f);
        }

        internal Vector2 StreamAt(Vector2 position)
        {
            if (m_Data == null)
                return Vector2.zero;

            var own = area;

            return m_Data.Stream((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f);
        }

        internal float StillAt(Vector2 position)
        {
            if (m_Data == null)
                return 0f;

            var own = area;

            return m_Data.Still((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f);
        }

        internal bool Masked(Vector2 position)
        {
            if (m_Data == null)
                return false;

            var own = area;

            return m_Data.Masked((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f);
        }

        internal float Inside(Vector2 position)
        {
            var own = area;

            return Mathf.Min(0.5f / own.z - Mathf.Abs(position.x - own.x),
                             0.5f / own.w - Mathf.Abs(position.y - own.y));
        }

        internal bool GroundAt(Vector2 position, out float bed, out float calm)
        {
            bed = 0f;
            calm = 0f;

            if (m_Data == null)
                return false;

            var own = area;

            return m_Data.Ground((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f, out bed,
                                 out calm);
        }

        internal bool ShoreAt(Vector2 position, out Vector3 field)
        {
            field = Vector3.zero;

            if (m_Data == null)
                return false;

            var own = area;

            return m_Data.Shore((position.x - own.x) * own.z + 0.5f, (position.y - own.y) * own.w + 0.5f, out field);
        }

        internal bool Near(Bounds bounds)
        {
            var top = level + (m_Data != null ? m_Data.highest : 0f);
            var bottom = level + (m_Data != null ? m_Data.lowest : 0f);

            return bounds.min.y <= top + CutMargin && bounds.max.y >= bottom - CutMargin;
        }

        internal static void SetQuality(HBStyleLiquidQuality quality)
        {
            var coarse = quality == HBStyleLiquidQuality.Low ? 2f : 1f;

            if (Mathf.Approximately(coarse, s_Coarse))
                return;

            s_Coarse = coarse;

            foreach (var terrain in s_Terrains)
            {
                if (terrain != null)
                    terrain.Apply();
            }
        }

        internal static int AssignChannels()
        {
            var next = 0;

            foreach (var terrain in s_Terrains)
            {
                if (terrain == null)
                    continue;

                var wanted = -1;

                foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
                {
                    if (cutter == null || !cutter.Reaches(terrain))
                        continue;

                    wanted = next++ % Channels;
                    break;
                }

                if (wanted == terrain.m_Channel)
                    continue;

                terrain.m_Channel = wanted;
                terrain.Apply();
            }

            return next;
        }

        internal static void Refresh()
        {
            var streaming = false;

            foreach (var terrain in s_Terrains)
            {
                if (terrain == null || terrain.m_Renderer == null || terrain.m_Properties == null)
                    continue;

                var stamp = terrain.Stamp();

                if (stamp != terrain.m_Stamp || terrain.Washed())
                {
                    terrain.m_Stamp = stamp;
                    terrain.Fill(terrain.m_Properties, terrain.cells);
                    terrain.Fill(terrain.m_TileProperties, terrain.tiles);
                    terrain.Tint();
                    terrain.Commit();
                }

                if (terrain.m_Data != null && terrain.m_Data.version != terrain.m_Version)
                    terrain.Place();

                streaming |= terrain.streams > 0.5f;
            }

            if (streaming)
                Shader.EnableKeyword(StreamsKeyword);
            else
                Shader.DisableKeyword(StreamsKeyword);
        }

        internal static bool TryGetSeaLevel(out float level)
        {
            var first = sea;

            level = first != null ? first.level : 0f;

            return first != null;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void Sweep()
        {
            UnityEditor.EditorApplication.delayCall += static () =>
            {
                foreach (var filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
                {
                    var surface = filter.gameObject;

                    if (surface.name == SurfaceName && (surface.hideFlags & HideFlags.HideInHierarchy) != 0
                        && !s_Owned.ContainsKey(surface))
                        DestroyImmediate(surface);
                }
            };
        }
#endif

        void OnEnable()
        {
            m_Surface = new GameObject(SurfaceName)
            {
                hideFlags = SurfaceFlags
            };

            s_Owned[m_Surface] = this;
            s_Terrains.Remove(this);
            m_Channel = -1;
            s_Terrains.Add(this);

            RenderPipelineManager.beginCameraRendering -= Arrange;
            RenderPipelineManager.beginCameraRendering += Arrange;

            m_Filter = m_Surface.AddComponent<MeshFilter>();
            m_Renderer = m_Surface.AddComponent<MeshRenderer>();
            m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
            m_Renderer.lightProbeUsage = LightProbeUsage.Off;
            m_Renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

            Apply();
        }

        void OnDisable()
        {
            if (m_Surface != null)
                s_Owned.Remove(m_Surface);

            s_Terrains.Remove(this);

            if (s_Terrains.Count == 0)
                RenderPipelineManager.beginCameraRendering -= Arrange;

            Publish();

            foreach (var patch in m_Patches)
            {
                if (patch != null)
                    s_Owned.Remove(patch.gameObject);
            }

            m_Patches.Clear();
            m_Spans.Clear();
            CoreUtils.Destroy(m_Sea);
            m_Sea = null;
            CoreUtils.Destroy(m_Surface);

            foreach (var tile in m_Tiles)
            {
                if (tile == null)
                    continue;

                s_Owned.Remove(tile.gameObject);
                CoreUtils.Destroy(tile.gameObject);
            }

            m_Tiles.Clear();
            m_Sheets.Clear();

            foreach (var mesh in m_Meshes)
                CoreUtils.Destroy(mesh);

            m_Meshes.Clear();
            m_Patterns.Clear();
            m_Stamps.Clear();

            m_Surface = null;
            m_Renderer = null;
            m_Filter = null;
        }

        void OnValidate()
        {
            while (m_Layers.Count > StyleLiquidLayers.Most)
                m_Layers.RemoveAt(m_Layers.Count - 1);

            Settle();
            Apply();
        }

        void LateUpdate()
        {
            if (m_Surface == null)
                return;

            Align();
            m_Surface.transform.SetPositionAndRotation(new Vector3(0f, level, 0f), Quaternion.identity);
            m_Surface.layer = gameObject.layer;
            Place();

            if (m_Renderer != null && m_Properties != null
                && (area != m_Area || levelMap != m_LevelMap || weightMap != m_WeightMap))
                Apply();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 0.95f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(m_Size.x, 0f, m_Size.y));
        }

        void Apply()
        {
            if (m_Renderer == null)
                return;

            m_Properties ??= new MaterialPropertyBlock();
            m_TileProperties ??= new MaterialPropertyBlock();

            m_Area = area;
            m_LevelMap = levelMap;
            m_WeightMap = weightMap;
            Fill(m_Properties, cells);
            Fill(m_TileProperties, tiles);
            Tint();
            m_Stamp = 0;

            m_Renderer.sharedMaterial = material;
            m_Renderer.forceRenderingOff = material == null;
            m_Renderer.enabled = false;
            Spread(LevelsFor(reach));
            Lay();
            Commit();

            Publish();

            LateUpdate();
        }

        static void Publish()
        {
            Shader.SetGlobalVector(s_SeaAreaId, seaArea);
            Shader.SetGlobalTexture(s_SeaLevelMapId, seaLevelMap);
        }

        static int LevelsFor(float metres)
        {
            var rings = Mathf.CeilToInt(Mathf.Log(Mathf.Max(metres, 1f) / (Cell * Cells * 0.5f), 2f)) + 1;

            return Mathf.Clamp(rings, 1, MaxLevels);
        }
    }
}
