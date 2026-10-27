using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleGrass
    {
        internal const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/Grass.shader";
        internal const string ComputePath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrass.compute";

        const float CellTarget = 8f;
        const int LeastSide = 8;
        const int MostSide = 64;
        const int GroupSize = 64;
        const int MostJobs = 64;
        const float FarShare = 0.35f;
        const float Dither = 3f;
        const float FadeShare = 0.1f;
        const float TallestGrass = 3f;
        const double PruneSeconds = 10.0;
        const int NearVertices = 12;
        const int FarVertices = 6;
        const int NearCountOffset = 4;
        const int FarCountOffset = 20;

        static readonly int s_GridId = Shader.PropertyToID("_HB_GrassGrid");
        static readonly int s_ReachId = Shader.PropertyToID("_HB_GrassReach");
        static readonly int s_CameraId = Shader.PropertyToID("_HB_GrassCamera");
        static readonly int s_ModeId = Shader.PropertyToID("_HB_GrassMode");
        static readonly int s_PlanesId = Shader.PropertyToID("_HB_GrassPlanes");
        static readonly int s_BladesId = Shader.PropertyToID("_HB_GrassBlades");
        static readonly int s_BladesOutId = Shader.PropertyToID("_HB_GrassBladesOut");
        static readonly int s_SlotsId = Shader.PropertyToID("_HB_GrassSlots");
        static readonly int s_JobsId = Shader.PropertyToID("_HB_GrassJobs");
        static readonly int s_VisibleId = Shader.PropertyToID("_HB_GrassVisible");
        static readonly int s_NearId = Shader.PropertyToID("_HB_GrassNear");
        static readonly int s_FarId = Shader.PropertyToID("_HB_GrassFar");
        static readonly int s_ListId = Shader.PropertyToID("_HB_GrassList");
        static readonly int s_TuftsId = Shader.PropertyToID("_HB_GrassTufts");
        static readonly int s_LayerGrassId = Shader.PropertyToID("_HB_LayerGrass");
        static readonly int s_LayerGrassTipId = Shader.PropertyToID("_HB_LayerGrassTip");
        static readonly int s_HeightmapId = Shader.PropertyToID("_HB_TerrainHeightmap");
        static readonly int s_HolesId = Shader.PropertyToID("_HB_TerrainHoles");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_TerrainSize");
        static readonly int s_OriginId = Shader.PropertyToID("_HB_TerrainOrigin");
        static readonly int s_ControlId = Shader.PropertyToID("_HB_TerrainControl");
        static readonly int s_AlbedoId = Shader.PropertyToID("_HB_LayerAlbedo");
        static readonly int s_MaskId = Shader.PropertyToID("_HB_LayerMask");
        static readonly int s_LayersId = Shader.PropertyToID("_HB_TerrainLayers");
        static readonly int s_LayerSTId = Shader.PropertyToID("_HB_LayerST");
        static readonly int s_LayerLookId = Shader.PropertyToID("_HB_LayerLook");
        static readonly int s_GrassMapId = Shader.PropertyToID("_HB_GrassMap");
        static readonly int s_StyleId = Shader.PropertyToID("_HB_GrassStyle");
        static readonly int s_TerrainStyleId = Shader.PropertyToID("_HB_TerrainStyle");
        static readonly int s_TerrainVariationId = Shader.PropertyToID("_HB_TerrainVariation");

        static readonly Vector4 s_NearMode = Vector4.zero;
        static readonly Vector4 s_FarMode = new Vector4(1f, 0f, 0f, 0f);

        sealed class View
        {
            internal int side;
            internal int ring;
            internal float spacing;
            internal int perSlot;
            internal int stamp;
            internal double lastUsed;
            internal GraphicsBuffer blades;
            internal GraphicsBuffer slots;
            internal GraphicsBuffer jobs;
            internal GraphicsBuffer visible;
            internal GraphicsBuffer near;
            internal GraphicsBuffer far;
            internal GraphicsBuffer args;
            internal Vector2Int[] cells;
            internal int[] stamps;
            internal bool[] placed;
            internal int[] table;
            internal Vector2Int[] order;
            internal MaterialPropertyBlock nearBlock;
            internal MaterialPropertyBlock farBlock;

            internal void Release()
            {
                blades?.Release();
                slots?.Release();
                jobs?.Release();
                visible?.Release();
                near?.Release();
                far?.Release();
                args?.Release();
            }
        }

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();
        readonly List<Camera> m_Stale = new List<Camera>();
        readonly Plane[] m_Planes = new Plane[6];
        readonly Vector4[] m_PlaneData = new Vector4[6];
        readonly int[] m_Jobs = new int[MostJobs * 4];

        int[] m_Visible = new int[0];
        CommandBuffer m_Commands;
        Material m_Material;
        Shader m_Shader;
        ComputeShader m_Compute;
        int m_PlaceKernel = -1;
        int m_CullKernel = -1;

        internal void Draw(ScriptableRenderContext context, Camera camera, HBStyleTerrain terrain)
        {
            var data = terrain.data;
            var tufts = data.grassArray;

            if (tufts == null || !Grows(terrain) || !Ready(terrain.grassShader, terrain.grassCompute))
                return;

            var level = StyleTerrains.grassQuality.current;
            var side = Mathf.Clamp(Mathf.RoundToInt(CellTarget / level.spacing), LeastSide, MostSide);
            var cell = side * level.spacing;
            var reach = Mathf.CeilToInt(level.distance / cell);
            var view = Acquire(camera, side, reach * 2 + 1, level.spacing);
            var stamp = Stamp(terrain, level.spacing);
            var origin = terrain.transform.position;
            var size = data.size;
            var position = camera.transform.position;
            var centre = new Vector2Int(Mathf.FloorToInt(position.x / cell), Mathf.FloorToInt(position.z / cell));
            var near = Mathf.Min(level.nearDistance, level.distance);

            GeometryUtility.CalculateFrustumPlanes(camera, m_Planes);

            for (var i = 0; i < 6; i++)
                m_PlaneData[i] = new Vector4(m_Planes[i].normal.x, m_Planes[i].normal.y, m_Planes[i].normal.z,
                                             m_Planes[i].distance);

            if (m_Visible.Length < view.ring * view.ring)
                m_Visible = new int[view.ring * view.ring];

            var jobs = 0;
            var visible = 0;

            foreach (var offset in view.order)
            {
                var spot = centre + offset;
                var low = new Vector2(spot.x * cell, spot.y * cell);

                if (low.x > origin.x + size.x || low.y > origin.z + size.z || low.x + cell < origin.x ||
                    low.y + cell < origin.z)
                    continue;

                var slot = Wrap(spot.x, view.ring) + Wrap(spot.y, view.ring) * view.ring;
                var stale = !view.placed[slot] || view.cells[slot] != spot || view.stamps[slot] != stamp;

                if (stale && jobs < MostJobs)
                {
                    view.cells[slot] = spot;
                    view.stamps[slot] = stamp;
                    view.placed[slot] = true;
                    view.table[slot * 4] = spot.x;
                    view.table[slot * 4 + 1] = spot.y;
                    m_Jobs[jobs * 4] = slot;
                    m_Jobs[jobs * 4 + 1] = spot.x;
                    m_Jobs[jobs * 4 + 2] = spot.y;
                    jobs++;
                }

                if (!view.placed[slot] || view.cells[slot] != spot)
                    continue;

                var nearest = new Vector2(Mathf.Clamp(position.x, low.x, low.x + cell),
                                          Mathf.Clamp(position.z, low.y, low.y + cell));

                if ((nearest - new Vector2(position.x, position.z)).sqrMagnitude > level.distance * level.distance)
                    continue;

                if (!GeometryUtility.TestPlanesAABB(m_Planes, Bounds(data, origin, low, cell)))
                    continue;

                m_Visible[visible++] = slot;
            }

            var cmd = m_Commands ??= new CommandBuffer { name = "HB Style Grass" };

            cmd.Clear();

            if (jobs > 0)
                Place(cmd, view, terrain, jobs);

            cmd.SetBufferCounterValue(view.near, 0u);
            cmd.SetBufferCounterValue(view.far, 0u);

            var grid = new Vector4(level.spacing, view.side, view.perSlot, cell);
            var reachData = new Vector4(level.distance, near, Dither, FarShare);
            var cameraData = new Vector4(position.x, position.y, position.z, level.distance * FadeShare);

            if (visible > 0)
            {
                cmd.SetBufferData(view.visible, m_Visible, 0, 0, visible);
                cmd.SetComputeVectorParam(m_Compute, s_GridId, grid);
                cmd.SetComputeVectorParam(m_Compute, s_ReachId, reachData);
                cmd.SetComputeVectorParam(m_Compute, s_CameraId, cameraData);
                cmd.SetComputeVectorArrayParam(m_Compute, s_PlanesId, m_PlaneData);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_BladesId, view.blades);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_SlotsId, view.slots);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_VisibleId, view.visible);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_NearId, view.near);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_FarId, view.far);
                cmd.DispatchCompute(m_Compute, m_CullKernel, Groups(view.perSlot), visible, 1);
            }

            cmd.CopyCounterValue(view.near, view.args, NearCountOffset);
            cmd.CopyCounterValue(view.far, view.args, FarCountOffset);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (visible == 0)
                return;

            Bind(view.nearBlock, view, view.near, s_NearMode, grid, reachData, cameraData, terrain, tufts);
            Bind(view.farBlock, view, view.far, s_FarMode, grid, reachData, cameraData, terrain, tufts);

            var bounds = new Bounds(new Vector3(position.x, origin.y + size.y * 0.5f, position.z),
                                    new Vector3(level.distance * 2f, size.y + TallestGrass * 2f, level.distance * 2f));
            var parameters = new RenderParams(m_Material)
            {
                camera = camera,
                layer = terrain.gameObject.layer,
                worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                matProps = view.nearBlock
            };

            Graphics.RenderPrimitivesIndirect(parameters, MeshTopology.Triangles, view.args, 1, 0);
            parameters.matProps = view.farBlock;
            Graphics.RenderPrimitivesIndirect(parameters, MeshTopology.Triangles, view.args, 1, 1);
        }

        static bool Grows(HBStyleTerrain terrain)
        {
            var grass = terrain.layerGrass;

            for (var index = 0; index < terrain.boundLayers; index++)
            {
                if (grass[index].x > 0f && grass[index].w >= 0f)
                    return true;
            }

            return false;
        }

        bool Ready(Shader shader, ComputeShader compute)
        {
            if (shader == null || compute == null)
                return false;

            if (m_Material == null || m_Shader != shader)
            {
                CoreUtils.Destroy(m_Material);
                m_Material = CoreUtils.CreateEngineMaterial(shader);
                m_Shader = shader;
            }

            if (m_Compute != compute)
            {
                m_Compute = compute;
                m_PlaceKernel = compute.HasKernel("Place") ? compute.FindKernel("Place") : -1;
                m_CullKernel = compute.HasKernel("Cull") ? compute.FindKernel("Cull") : -1;
            }

            return m_PlaceKernel >= 0 && m_CullKernel >= 0 && SystemInfo.supportsComputeShaders;
        }

        static int Stamp(HBStyleTerrain terrain, float spacing)
        {
            var data = terrain.data;

            unchecked
            {
                var hash = 17;

                hash = hash * 31 + data.version;
                hash = hash * 31 + data.grassVersion;
                hash = hash * 31 + data.grassStamp;
                hash = hash * 31 + data.handVersion;
                hash = hash * 31 + StyleTerrains.previewVersion;
                hash = hash * 31 + (terrain.boundControl != null ? terrain.boundControl.GetHashCode() : 0);
                hash = hash * 31 + (data.grassArray != null ? data.grassArray.GetHashCode() : 0);
                hash = hash * 31 + terrain.boundLayers;
                hash = hash * 31 + spacing.GetHashCode();
                hash = hash * 31 + terrain.transform.position.GetHashCode();
                hash = hash * 31 + data.look.style.GetHashCode();
                hash = hash * 31 + data.look.variation.GetHashCode();

                for (var index = 0; index < terrain.boundLayers; index++)
                {
                    hash = hash * 31 + terrain.layerGrass[index].GetHashCode();
                    hash = hash * 31 + terrain.layerLook[index].GetHashCode();
                    hash = hash * 31 + terrain.layerST[index].GetHashCode();
                }

                return hash;
            }
        }

        void Place(CommandBuffer cmd, View view, HBStyleTerrain terrain, int jobs)
        {
            var data = terrain.data;
            var origin = terrain.transform.position;
            var size = data.size;
            var holes = data.terrainData.holesTexture;
            var control = terrain.boundControl;
            var layers = control != null ? terrain.boundLayers : 0;

            cmd.SetBufferData(view.slots, view.table);
            cmd.SetBufferData(view.jobs, m_Jobs, 0, 0, jobs * 4);
            cmd.SetComputeVectorParam(m_Compute, s_GridId, new Vector4(view.spacing, view.side, view.perSlot,
                                                                       view.side * view.spacing));
            cmd.SetComputeTextureParam(m_Compute, m_PlaceKernel, s_HeightmapId, data.terrainData.heightmapTexture);
            cmd.SetComputeTextureParam(m_Compute, m_PlaceKernel, s_HolesId,
                                       holes != null ? (Texture)holes : Texture2D.whiteTexture);
            cmd.SetComputeTextureParam(m_Compute, m_PlaceKernel, s_ControlId,
                                       control != null ? control : terrain.emptyArray);
            cmd.SetComputeTextureParam(m_Compute, m_PlaceKernel, s_AlbedoId,
                                       data.albedoArray != null ? (Texture)data.albedoArray : terrain.emptyArray);
            cmd.SetComputeTextureParam(m_Compute, m_PlaceKernel, s_MaskId,
                                       data.maskArray != null ? (Texture)data.maskArray : terrain.emptyArray);
            cmd.SetComputeTextureParam(m_Compute, m_PlaceKernel, s_GrassMapId, terrain.grassMap);
            cmd.SetComputeVectorParam(m_Compute, s_SizeId, new Vector4(size.x, size.y, size.z, data.resolution));
            cmd.SetComputeVectorParam(m_Compute, s_OriginId, new Vector4(origin.x, origin.y, origin.z, 0f));
            cmd.SetComputeVectorParam(m_Compute, s_LayersId, new Vector4(layers, (layers + 3) / 4,
                                                                         layers > 0 ? 1f : 0f, -1f));
            cmd.SetComputeVectorParam(m_Compute, s_TerrainStyleId, data.look.style);
            cmd.SetComputeVectorParam(m_Compute, s_TerrainVariationId, data.look.variation);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerGrassId, terrain.layerGrass);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerSTId, terrain.layerST);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerLookId, terrain.layerLook);
            cmd.SetComputeBufferParam(m_Compute, m_PlaceKernel, s_JobsId, view.jobs);
            cmd.SetComputeBufferParam(m_Compute, m_PlaceKernel, s_BladesOutId, view.blades);
            cmd.DispatchCompute(m_Compute, m_PlaceKernel, Groups(view.perSlot), jobs, 1);
        }

        static void Bind(MaterialPropertyBlock block, View view, GraphicsBuffer list, Vector4 mode, Vector4 grid,
                         Vector4 reach, Vector4 camera, HBStyleTerrain terrain, Texture tufts)
        {
            block.SetBuffer(s_BladesId, view.blades);
            block.SetBuffer(s_SlotsId, view.slots);
            block.SetBuffer(s_ListId, list);
            block.SetVector(s_ModeId, mode);
            block.SetVector(s_GridId, grid);
            block.SetVector(s_ReachId, reach);
            block.SetVector(s_CameraId, camera);
            block.SetTexture(s_TuftsId, tufts);
            block.SetVector(s_StyleId, terrain.grassStyle);
            block.SetVectorArray(s_LayerGrassId, terrain.layerGrass);
            block.SetVectorArray(s_LayerGrassTipId, terrain.layerGrassTip);
        }

        static Bounds Bounds(HBStyleTerrainData data, Vector3 origin, Vector2 low, float cell)
        {
            var size = data.size;
            var patches = data.patches;
            var patchSize = size.x / patches;
            var x0 = Mathf.Clamp(Mathf.FloorToInt((low.x - origin.x) / patchSize), 0, patches - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt((low.y - origin.z) / patchSize), 0, patches - 1);
            var x1 = Mathf.Clamp(Mathf.FloorToInt((low.x + cell - origin.x) / patchSize), 0, patches - 1);
            var z1 = Mathf.Clamp(Mathf.FloorToInt((low.y + cell - origin.z) / patchSize), 0, patches - 1);
            var bottom = float.MaxValue;
            var top = float.MinValue;

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    bottom = Mathf.Min(bottom, data.patchMin[z * patches + x]);
                    top = Mathf.Max(top, data.patchMax[z * patches + x]);
                }
            }

            var centre = new Vector3(low.x + cell * 0.5f, origin.y + (bottom + top + TallestGrass) * 0.5f,
                                     low.y + cell * 0.5f);

            return new Bounds(centre, new Vector3(cell, top - bottom + TallestGrass, cell));
        }

        View Acquire(Camera camera, int side, int ring, float spacing)
        {
            var now = Time.realtimeSinceStartupAsDouble;

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

            if (m_Views.TryGetValue(camera, out var view) && view.side == side && view.ring == ring &&
                Mathf.Approximately(view.spacing, spacing))
            {
                view.lastUsed = now;
                return view;
            }

            view?.Release();

            var slots = ring * ring;
            var perSlot = side * side;
            var blades = slots * perSlot;

            view = new View
            {
                side = side,
                ring = ring,
                spacing = spacing,
                perSlot = perSlot,
                lastUsed = now,
                blades = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blades, sizeof(uint) * 3),
                slots = new GraphicsBuffer(GraphicsBuffer.Target.Structured, slots, sizeof(int) * 4),
                jobs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MostJobs, sizeof(int) * 4),
                visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, slots, sizeof(uint)),
                near = new GraphicsBuffer(GraphicsBuffer.Target.Append, blades, sizeof(uint)),
                far = new GraphicsBuffer(GraphicsBuffer.Target.Append, blades, sizeof(uint)),
                args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 2,
                                          GraphicsBuffer.IndirectDrawArgs.size),
                cells = new Vector2Int[slots],
                stamps = new int[slots],
                placed = new bool[slots],
                table = new int[slots * 4],
                order = Order(ring / 2),
                nearBlock = new MaterialPropertyBlock(),
                farBlock = new MaterialPropertyBlock()
            };

            view.args.SetData(new[]
            {
                new GraphicsBuffer.IndirectDrawArgs { vertexCountPerInstance = NearVertices },
                new GraphicsBuffer.IndirectDrawArgs { vertexCountPerInstance = FarVertices }
            });

            m_Views[camera] = view;
            return view;
        }

        static Vector2Int[] Order(int reach)
        {
            var order = new List<Vector2Int>();

            for (var z = -reach; z <= reach; z++)
            {
                for (var x = -reach; x <= reach; x++)
                    order.Add(new Vector2Int(x, z));
            }

            order.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
            return order.ToArray();
        }

        static int Wrap(int value, int ring)
        {
            var wrapped = value % ring;

            return wrapped < 0 ? wrapped + ring : wrapped;
        }

        static int Groups(int count)
        {
            return (count + GroupSize - 1) / GroupSize;
        }

        internal void Release()
        {
            foreach (var view in m_Views.Values)
                view.Release();

            m_Views.Clear();
        }

        internal void Dispose()
        {
            Release();
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            m_Shader = null;
            m_Compute = null;
            m_Commands?.Release();
            m_Commands = null;
        }
    }
}
