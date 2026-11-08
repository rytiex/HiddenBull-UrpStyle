using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleLiquidCalm
    {
        internal const float Slope = 0.25f;
        internal const float Tile = 12f;

        const int LeastSize = 64;
        const int MostSize = 1024;
        const int Group = 8;
        const int Polish = 2;
        const float HeightScale = 65535f / 32766f;

        static readonly int s_HeightmapId = Shader.PropertyToID("_HB_CalmHeightmap");
        static readonly int s_LevelId = Shader.PropertyToID("_HB_CalmLevel");
        static readonly int s_HolesId = Shader.PropertyToID("_HB_CalmHoles");
        static readonly int s_FlowId = Shader.PropertyToID("_HB_CalmFlow");
        static readonly int s_InId = Shader.PropertyToID("_HB_CalmIn");
        static readonly int s_OutId = Shader.PropertyToID("_HB_CalmOut");
        static readonly int s_TilesOutId = Shader.PropertyToID("_HB_CalmTilesOut");
        static readonly int s_GridId = Shader.PropertyToID("_HB_CalmGrid");
        static readonly int s_TerrainId = Shader.PropertyToID("_HB_CalmTerrain");
        static readonly int s_TileId = Shader.PropertyToID("_HB_CalmTile");
        static readonly int s_CalmId = Shader.PropertyToID("_HB_LiquidCalm");
        static readonly int s_TilesId = Shader.PropertyToID("_HB_LiquidTiles");
        static readonly int s_CalmGridId = Shader.PropertyToID("_HB_LiquidCalmGrid");

        ComputeShader m_Compute;
        int m_Seed = -1;
        int m_Spread = -1;
        int m_Finish = -1;
        int m_Tiles = -1;
        RenderTexture m_Front;
        RenderTexture m_Back;
        RenderTexture m_TileMap;
        CommandBuffer m_Commands;
        HBStyleTerrainData m_BakedData;
        int m_BakedVersion = -1;
        int m_BakedWater = -1;
        int m_BakedStamp = -1;
        float m_BakedSea = float.NaN;
        int m_BakedHoles = -1;
        Vector3 m_BakedSize;
        bool m_Baked;
        int m_Stamp;
        float[] m_CalmRead;
        int m_CalmSize;
        float[] m_TileRead;
        int m_TileSize;

        internal bool readable => m_CalmRead != null && m_TileRead != null && m_CalmSize > 0 && m_TileSize > 0;

        internal int tiles => m_TileSize;

        internal Vector4 Calm(Vector2 uv)
        {
            var size = m_CalmSize;
            var x = Mathf.Clamp01(uv.x) * size - 0.5f;
            var y = Mathf.Clamp01(uv.y) * size - 0.5f;
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var fx = x - x0;
            var fy = y - y0;
            var x1 = Mathf.Clamp(x0 + 1, 0, size - 1);
            var y1 = Mathf.Clamp(y0 + 1, 0, size - 1);

            x0 = Mathf.Clamp(x0, 0, size - 1);
            y0 = Mathf.Clamp(y0, 0, size - 1);

            var low = Vector4.Lerp(CalmTexel(x0, y0), CalmTexel(x1, y0), fx);
            var high = Vector4.Lerp(CalmTexel(x0, y1), CalmTexel(x1, y1), fx);

            return Vector4.Lerp(low, high, fy);
        }

        internal Vector2 TileHeading(int x, int y)
        {
            var index = (Mathf.Clamp(y, 0, m_TileSize - 1) * m_TileSize + Mathf.Clamp(x, 0, m_TileSize - 1)) * 2;

            return new Vector2(m_TileRead[index], m_TileRead[index + 1]);
        }

        Vector4 CalmTexel(int x, int y)
        {
            var index = (y * m_CalmSize + x) * 4;

            return new Vector4(m_CalmRead[index], m_CalmRead[index + 1], m_CalmRead[index + 2],
                               m_CalmRead[index + 3]);
        }

        void Read(AsyncGPUReadbackRequest request, int stamp, bool calm)
        {
            if (stamp != m_Stamp || request.hasError || !request.done)
                return;

            var halves = request.GetData<ushort>();
            var size = request.width;
            var read = new float[halves.Length];

            for (var index = 0; index < halves.Length; index++)
                read[index] = Mathf.HalfToFloat(halves[index]);

            if (calm)
            {
                m_CalmRead = read;
                m_CalmSize = size;
            }
            else
            {
                m_TileRead = read;
                m_TileSize = size;
            }
        }

        internal void Bind(ScriptableRenderContext context, ComputeShader compute, HBStyleTerrainData data,
                           MaterialPropertyBlock block)
        {
            var ready = Ready(compute, data);

            if (ready && (!m_Baked || m_BakedData != data || m_BakedVersion != data.version ||
                          m_BakedWater != data.waterVersion || m_BakedStamp != data.waterStamp ||
                          m_BakedSea != data.seaLevel || m_BakedSize != data.size || m_BakedHoles != Holes(data)))
            {
                Bake(context, data);
                m_Baked = true;
                m_BakedData = data;
                m_BakedVersion = data.version;
                m_BakedWater = data.waterVersion;
                m_BakedStamp = data.waterStamp;
                m_BakedSea = data.seaLevel;
                m_BakedSize = data.size;
                m_BakedHoles = Holes(data);
            }

            var bound = ready && m_Baked;
            var tiles = bound ? m_TileMap.width : 1;

            block.SetTexture(s_CalmId, bound ? m_Front : Texture2D.blackTexture);
            block.SetTexture(s_TilesId, bound ? m_TileMap : Texture2D.blackTexture);
            block.SetVector(s_CalmGridId, new Vector4(Slope, Tile, tiles, bound ? 1f : 0f));
        }

        internal void Dispose()
        {
            Release(ref m_Front);
            Release(ref m_Back);
            Release(ref m_TileMap);
            m_Commands?.Release();
            m_Commands = null;
            m_Compute = null;
            m_Baked = false;
            m_BakedData = null;
            m_Stamp++;
            m_CalmRead = null;
            m_TileRead = null;
            m_CalmSize = 0;
            m_TileSize = 0;
        }

        bool Ready(ComputeShader compute, HBStyleTerrainData data)
        {
            if (compute == null || !SystemInfo.supportsComputeShaders || data.terrainData == null ||
                data.terrainData.heightmapTexture == null || data.waterLevel == null || data.waterFlow == null)
                return false;

            if (compute != m_Compute)
            {
                m_Compute = compute;
                m_Seed = Kernel(compute, "CalmSeed");
                m_Spread = Kernel(compute, "CalmSpread");
                m_Finish = Kernel(compute, "CalmFinish");
                m_Tiles = Kernel(compute, "CalmTiles");
                m_Baked = false;
            }

            if (m_Seed < 0 || m_Spread < 0 || m_Finish < 0 || m_Tiles < 0)
                return false;

            var size = Mathf.Clamp(data.resolution - 1, LeastSize, MostSize);
            var tiles = Mathf.Max(Mathf.CeilToInt(data.size.x / Tile), 1);

            var depth = GraphicsFormat.R16G16B16A16_SFloat;

            if (Fit(ref m_Front, size, depth, FilterMode.Bilinear, "HB Style Liquid Calm") |
                Fit(ref m_Back, size, depth, FilterMode.Bilinear, "HB Style Liquid Calm") |
                Fit(ref m_TileMap, tiles, GraphicsFormat.R16G16_SFloat, FilterMode.Point, "HB Style Liquid Tiles"))
                m_Baked = false;

            return true;
        }

        static Texture HolesTexture(HBStyleTerrainData data)
        {
            var holes = data.terrainData.holesTexture;

            return holes != null ? holes : Texture2D.whiteTexture;
        }

        static int Holes(HBStyleTerrainData data)
        {
            var holes = data.terrainData != null ? data.terrainData.holesTexture : null;

            return holes != null ? (int)holes.updateCount : 0;
        }

        void Bake(ScriptableRenderContext context, HBStyleTerrainData data)
        {
            var size = m_Front.width;
            var groups = (size + Group - 1) / Group;
            var cell = data.size.x / size;
            var cmd = m_Commands ??= new CommandBuffer { name = "HB Style Liquid Calm" };

            cmd.Clear();
            cmd.SetComputeVectorParam(m_Compute, s_TerrainId,
                                      new Vector4(HeightScale * data.size.y, data.seaLevel,
                                                  Mathf.Max(data.size.y, 0.01f), data.resolution));
            cmd.SetComputeVectorParam(m_Compute, s_GridId, new Vector4(size, cell, Slope, 0f));
            cmd.SetComputeTextureParam(m_Compute, m_Seed, s_HeightmapId, data.terrainData.heightmapTexture);
            cmd.SetComputeTextureParam(m_Compute, m_Seed, s_LevelId, data.waterLevel);
            cmd.SetComputeTextureParam(m_Compute, m_Seed, s_HolesId, HolesTexture(data));
            cmd.SetComputeTextureParam(m_Compute, m_Seed, s_OutId, m_Front);
            cmd.DispatchCompute(m_Compute, m_Seed, groups, groups, 1);

            var step = Mathf.NextPowerOfTwo(size) / 2;
            var polish = Polish;

            while (step >= 1)
            {
                Spread(cmd, size, cell, step, groups);

                if (step == 1 && polish-- > 0)
                    step = 4;

                step /= 2;
            }

            cmd.SetComputeTextureParam(m_Compute, m_Finish, s_InId, m_Front);
            cmd.SetComputeTextureParam(m_Compute, m_Finish, s_OutId, m_Back);
            cmd.DispatchCompute(m_Compute, m_Finish, groups, groups, 1);
            (m_Front, m_Back) = (m_Back, m_Front);

            var tiles = m_TileMap.width;
            var tileGroups = (tiles + Group - 1) / Group;

            cmd.SetComputeVectorParam(m_Compute, s_TileId, new Vector4(tiles, Tile, data.size.x, data.size.z));
            cmd.SetComputeTextureParam(m_Compute, m_Tiles, s_InId, m_Front);
            cmd.SetComputeTextureParam(m_Compute, m_Tiles, s_FlowId, data.waterFlow);
            cmd.SetComputeTextureParam(m_Compute, m_Tiles, s_TilesOutId, m_TileMap);
            cmd.DispatchCompute(m_Compute, m_Tiles, tileGroups, tileGroups, 1);

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                var stamp = ++m_Stamp;

                cmd.RequestAsyncReadback(m_Front, 0, request => Read(request, stamp, true));
                cmd.RequestAsyncReadback(m_TileMap, 0, request => Read(request, stamp, false));
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        void Spread(CommandBuffer cmd, int size, float cell, int step, int groups)
        {
            cmd.SetComputeVectorParam(m_Compute, s_GridId, new Vector4(size, cell, Slope, step));
            cmd.SetComputeTextureParam(m_Compute, m_Spread, s_InId, m_Front);
            cmd.SetComputeTextureParam(m_Compute, m_Spread, s_OutId, m_Back);
            cmd.DispatchCompute(m_Compute, m_Spread, groups, groups, 1);
            (m_Front, m_Back) = (m_Back, m_Front);
        }

        static int Kernel(ComputeShader compute, string name)
        {
            return compute.HasKernel(name) ? compute.FindKernel(name) : -1;
        }

        static bool Fit(ref RenderTexture texture, int size, GraphicsFormat format, FilterMode filter, string name)
        {
            if (texture != null && texture.IsCreated() && texture.width == size)
                return false;

            Release(ref texture);
            texture = new RenderTexture(size, size, 0, format)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.Create();
            return true;
        }

        static void Release(ref RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                CoreUtils.Destroy(texture);
            }

            texture = null;
        }
    }
}
