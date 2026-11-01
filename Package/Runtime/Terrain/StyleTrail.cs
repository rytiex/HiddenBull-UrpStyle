using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTrail
    {
        const int MapSize = 256;
        const int Tiles = MapSize / 8;
        const int TileCount = Tiles * Tiles;
        const float TileSize = 2f;
        const int MostEntries = 16384;
        const int StampSize = 3;
        const float Reach = 30f;
        const float MostStep = 0.1f;
        const double Linger = 90.0;
        const double PruneSeconds = 10.0;

        static readonly int s_CameraId = Shader.PropertyToID("_HB_GrassCamera");
        static readonly int s_ClockId = Shader.PropertyToID("_HB_GrassClock");
        static readonly int s_FromId = Shader.PropertyToID("_HB_GrassTrailFrom");
        static readonly int s_TrailId = Shader.PropertyToID("_HB_GrassTrail");
        static readonly int s_TrailOutId = Shader.PropertyToID("_HB_GrassTrailOut");
        static readonly int s_StampsId = Shader.PropertyToID("_HB_TrailStamps");
        static readonly int s_TilesId = Shader.PropertyToID("_HB_TrailTiles");
        static readonly int s_ListId = Shader.PropertyToID("_HB_TrailList");
        static readonly int s_HeightmapId = Shader.PropertyToID("_HB_TerrainHeightmap");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_TerrainSize");
        static readonly int s_OriginId = Shader.PropertyToID("_HB_TerrainOrigin");

        sealed class View
        {
            internal RenderTexture map;
            internal RenderTexture next;
            internal Vector4 from;
            internal double stamp;
            internal double until;
            internal double lastUsed;
            internal bool ready;

            internal void Release()
            {
                Free(map);
                Free(next);
            }
        }

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();
        readonly List<Camera> m_Stale = new List<Camera>();
        readonly Vector4[] m_Stamps = new Vector4[HBStyleBender.Limit * StampSize];
        readonly int[] m_Tiles = new int[TileCount];
        readonly int[] m_Cursors = new int[TileCount];
        readonly int[] m_List = new int[MostEntries];

        GraphicsBuffer m_StampBuffer;
        GraphicsBuffer m_TileBuffer;
        GraphicsBuffer m_ListBuffer;
        CommandBuffer m_Commands;
        ComputeShader m_Compute;
        int m_Kernel = -1;

        static void Free(RenderTexture map)
        {
            if (map == null)
                return;

            map.Release();
            CoreUtils.Destroy(map);
        }

        static RenderTexture NewMap()
        {
            var map = new RenderTexture(MapSize, MapSize, 0, RenderTextureFormat.ARGBHalf,
                                        RenderTextureReadWrite.Linear)
            {
                name = "HB Style Trail",
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            map.Create();
            return map;
        }

        internal Texture Map(Camera camera)
        {
            return m_Views.TryGetValue(camera, out var view) ? view.map : Texture2D.blackTexture;
        }

        internal void Run(ScriptableRenderContext context, Camera camera, HBStyleTerrain terrain)
        {
            if (!Ready(terrain.grassCompute))
                return;

            var now = Time.realtimeSinceStartupAsDouble;
            var position = camera.transform.position;
            var benders = HBStyleBender.Collect(position, Reach, m_Stamps);

            Prune(camera, now);

            if (!m_Views.TryGetValue(camera, out var view))
            {
                if (benders == 0)
                    return;

                view = new View { map = NewMap(), next = NewMap(), stamp = now };
                m_Views[camera] = view;
            }

            view.lastUsed = now;

            if (benders > 0)
                view.until = now + Linger;

            if (now > view.until)
            {
                view.Release();
                m_Views.Remove(camera);
                return;
            }

            var data = terrain.data;
            var origin = terrain.transform.position;
            var size = data.size;
            var step = Mathf.Min((float)(now - view.stamp), MostStep);
            var cmd = m_Commands ??= new CommandBuffer { name = "HB Style Trail" };
            var entries = Bin(benders, position);

            view.stamp = now;
            cmd.Clear();
            cmd.SetComputeVectorParam(m_Compute, s_ClockId, new Vector4(0f, step, view.ready ? 0f : 1f, 0f));
            cmd.SetComputeVectorParam(m_Compute, s_CameraId, new Vector4(position.x, position.y, position.z, 0f));
            cmd.SetComputeVectorParam(m_Compute, s_FromId, view.from);
            cmd.SetComputeVectorParam(m_Compute, s_SizeId, new Vector4(size.x, size.y, size.z, data.resolution));
            cmd.SetComputeVectorParam(m_Compute, s_OriginId, new Vector4(origin.x, origin.y, origin.z, 0f));
            cmd.SetBufferData(m_TileBuffer, m_Tiles);

            if (entries > 0)
            {
                cmd.SetBufferData(m_StampBuffer, m_Stamps, 0, 0, benders * StampSize);
                cmd.SetBufferData(m_ListBuffer, m_List, 0, 0, entries);
            }

            cmd.SetComputeBufferParam(m_Compute, m_Kernel, s_StampsId, m_StampBuffer);
            cmd.SetComputeBufferParam(m_Compute, m_Kernel, s_TilesId, m_TileBuffer);
            cmd.SetComputeBufferParam(m_Compute, m_Kernel, s_ListId, m_ListBuffer);
            cmd.SetComputeTextureParam(m_Compute, m_Kernel, s_HeightmapId, data.terrainData.heightmapTexture);
            cmd.SetComputeTextureParam(m_Compute, m_Kernel, s_TrailId, view.map);
            cmd.SetComputeTextureParam(m_Compute, m_Kernel, s_TrailOutId, view.next);
            cmd.DispatchCompute(m_Compute, m_Kernel, Tiles, Tiles, 1);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            (view.map, view.next) = (view.next, view.map);
            view.from = new Vector4(position.x, position.z, 0f, 0f);
            view.ready = true;
        }

        int Bin(int benders, Vector3 position)
        {
            var camera = new Vector2Int(Mathf.FloorToInt(position.x / TileSize),
                                        Mathf.FloorToInt(position.z / TileSize));
            var used = 0;

            Array.Clear(m_Cursors, 0, TileCount);

            for (var bender = 0; bender < benders; bender++)
                Visit(bender, camera, false);

            for (var tile = 0; tile < TileCount; tile++)
            {
                var count = Mathf.Min(m_Cursors[tile], MostEntries - used);

                m_Tiles[tile] = (used << 16) | count;
                m_Cursors[tile] = used;
                used += count;
            }

            for (var bender = 0; bender < benders; bender++)
                Visit(bender, camera, true);

            return used;
        }

        void Visit(int bender, Vector2Int camera, bool fill)
        {
            var ball = m_Stamps[bender * StampSize];
            var form = m_Stamps[bender * StampSize + 2];
            var extent = ball.w + new Vector2(form.x, form.y).magnitude;
            var x0 = Mathf.Max(Mathf.FloorToInt((ball.x - extent) / TileSize), camera.x - Tiles / 2);
            var x1 = Mathf.Min(Mathf.FloorToInt((ball.x + extent) / TileSize), camera.x + Tiles / 2);
            var z0 = Mathf.Max(Mathf.FloorToInt((ball.z - extent) / TileSize), camera.y - Tiles / 2);
            var z1 = Mathf.Min(Mathf.FloorToInt((ball.z + extent) / TileSize), camera.y + Tiles / 2);

            x1 = Mathf.Min(x1, x0 + Tiles - 1);
            z1 = Mathf.Min(z1, z0 + Tiles - 1);

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var tile = (z & (Tiles - 1)) * Tiles + (x & (Tiles - 1));

                    if (!fill)
                    {
                        m_Cursors[tile]++;
                        continue;
                    }

                    var cursor = m_Cursors[tile];

                    if (cursor < (m_Tiles[tile] >> 16) + (m_Tiles[tile] & 65535))
                    {
                        m_List[cursor] = bender;
                        m_Cursors[tile] = cursor + 1;
                    }
                }
            }
        }

        bool Ready(ComputeShader compute)
        {
            if (compute == null || !SystemInfo.supportsComputeShaders)
                return false;

            if (m_Compute != compute)
            {
                m_Compute = compute;
                m_Kernel = compute.HasKernel("Trail") ? compute.FindKernel("Trail") : -1;
            }

            if (m_Kernel < 0)
                return false;

            m_StampBuffer ??= new GraphicsBuffer(GraphicsBuffer.Target.Structured, m_Stamps.Length, sizeof(float) * 4);
            m_TileBuffer ??= new GraphicsBuffer(GraphicsBuffer.Target.Structured, TileCount, sizeof(uint));
            m_ListBuffer ??= new GraphicsBuffer(GraphicsBuffer.Target.Structured, MostEntries, sizeof(uint));
            return true;
        }

        void Prune(Camera camera, double now)
        {
            m_Stale.Clear();

            foreach (var pair in m_Views)
            {
                if (pair.Key == null || (pair.Key != camera && now - pair.Value.lastUsed > PruneSeconds))
                    m_Stale.Add(pair.Key);
            }

            foreach (var stale in m_Stale)
            {
                m_Views[stale].Release();
                m_Views.Remove(stale);
            }
        }

        internal void Release()
        {
            foreach (var view in m_Views.Values)
                view.Release();

            m_Views.Clear();
            m_StampBuffer?.Release();
            m_StampBuffer = null;
            m_TileBuffer?.Release();
            m_TileBuffer = null;
            m_ListBuffer?.Release();
            m_ListBuffer = null;
        }

        internal void Dispose()
        {
            Release();
            m_Compute = null;
            m_Commands?.Release();
            m_Commands = null;
        }
    }
}
