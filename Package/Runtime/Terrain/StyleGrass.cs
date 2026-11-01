using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        const float ThinStart = 200f;
        const float BandShare = 0.3f;
        const int MostRings = 8;
        const float TallestGrass = 3f;
        const double PruneSeconds = 10.0;
        const int NearLeaves = 8;
        const int MidLeaves = 5;
        const int FarLeaves = 3;
        const int LeafVertices = 7;
        const int CountOffset = 4;
        const int FlowerVertices = 12;
        const float MovingWeather = 2f;
        const int MostFlowers = 262144;
        const int DrawnSize = sizeof(uint) * 3;
        const int BoxSize = sizeof(float) * 6 + sizeof(uint);
        const float CloseShare = 0.3f;
        const float SpareTufts = 1.25f;
        const string FlowerKeyword = "_HB_GRASS_FLOWERS";

        static readonly ushort[] s_NearLeaf = { 0, 1, 2, 2, 1, 3, 2, 3, 4, 4, 3, 5, 4, 5, 6 };
        static readonly ushort[] s_FarLeaf = { 0, 1, 2, 2, 1, 3, 2, 3, 6 };

        static readonly int s_GridId = Shader.PropertyToID("_HB_GrassGrid");
        static readonly int s_ReachId = Shader.PropertyToID("_HB_GrassReach");
        static readonly int s_CameraId = Shader.PropertyToID("_HB_GrassCamera");
        static readonly int s_RootsId = Shader.PropertyToID("_HB_GrassRoots");
        static readonly int s_PlanesId = Shader.PropertyToID("_HB_GrassPlanes");
        static readonly int s_BladesId = Shader.PropertyToID("_HB_GrassBlades");
        static readonly int s_BladesOutId = Shader.PropertyToID("_HB_GrassBladesOut");
        static readonly int s_SlotsId = Shader.PropertyToID("_HB_GrassSlots");
        static readonly int s_JobsId = Shader.PropertyToID("_HB_GrassJobs");
        static readonly int s_VisibleId = Shader.PropertyToID("_HB_GrassVisible");
        static readonly int s_BoxesId = Shader.PropertyToID("_HB_GrassBoxes");
        static readonly int s_BoxCountId = Shader.PropertyToID("_HB_GrassBoxCount");
        static readonly int s_SeenId = Shader.PropertyToID("_HB_GrassSeen");
        static readonly int s_NearId = Shader.PropertyToID("_HB_GrassNear");
        static readonly int s_MidId = Shader.PropertyToID("_HB_GrassMid");
        static readonly int s_FarId = Shader.PropertyToID("_HB_GrassFar");
        static readonly int s_FlowersId = Shader.PropertyToID("_HB_GrassFlowers");
        static readonly int s_AirId = Shader.PropertyToID("_HB_GrassAir");
        static readonly int s_ClockId = Shader.PropertyToID("_HB_GrassClock");
        static readonly int s_TrailId = Shader.PropertyToID("_HB_GrassTrail");
        static readonly int s_TimeId = Shader.PropertyToID("_Time");
        static readonly int s_FlowerHeadsId = Shader.PropertyToID("_HB_FlowerHeads");
        static readonly int s_LayerFlowerId = Shader.PropertyToID("_HB_LayerFlower");
        static readonly int s_ListId = Shader.PropertyToID("_HB_GrassList");
        static readonly int s_LayerGrassId = Shader.PropertyToID("_HB_LayerGrass");
        static readonly int s_LayerGrassEdgeId = Shader.PropertyToID("_HB_LayerGrassEdge");
        static readonly int s_LayerVaryId = Shader.PropertyToID("_HB_LayerVary");
        static readonly int s_LayerVaryShapeId = Shader.PropertyToID("_HB_LayerVaryShape");
        static readonly int s_LayerVarySizeId = Shader.PropertyToID("_HB_LayerVarySize");
        static readonly int s_LayerGrassTipId = Shader.PropertyToID("_HB_LayerGrassTip");
        static readonly int s_LayerSurfaceId = Shader.PropertyToID("_HB_LayerSurface");
        static readonly int s_LayerWetGlossId = Shader.PropertyToID("_HB_LayerWetGloss");
        static readonly int s_ReceiveWeatherId = Shader.PropertyToID("_ReceiveWeather");
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

        [StructLayout(LayoutKind.Sequential)]
        struct BoxData
        {
            public Vector3 centre;
            public Vector3 extent;
            public uint slot;
        }

        sealed class View
        {
            internal int side;
            internal int ring;
            internal int rings;
            internal float spacing;
            internal float distance;
            internal int perSlot;
            internal int stamp;
            internal double lastUsed;
            internal GraphicsBuffer blades;
            internal GraphicsBuffer slots;
            internal GraphicsBuffer jobs;
            internal GraphicsBuffer boxes;
            internal GraphicsBuffer visible;
            internal GraphicsBuffer near;
            internal GraphicsBuffer mid;
            internal GraphicsBuffer far;
            internal GraphicsBuffer flowers;
            internal GraphicsBuffer dispatch;
            internal GraphicsBuffer args;
            internal GraphicsBuffer nearArgs;
            internal GraphicsBuffer midArgs;
            internal GraphicsBuffer farArgs;
            internal Vector2Int[] cells;
            internal int[] stamps;
            internal int[] painted;
            internal bool[] placed;
            internal int[] table;
            internal Vector2Int[] order;
            internal MaterialPropertyBlock nearBlock;
            internal MaterialPropertyBlock midBlock;
            internal MaterialPropertyBlock farBlock;
            internal MaterialPropertyBlock flowerBlock;

            internal void Release()
            {
                blades?.Release();
                slots?.Release();
                jobs?.Release();
                boxes?.Release();
                visible?.Release();
                near?.Release();
                mid?.Release();
                far?.Release();
                flowers?.Release();
                dispatch?.Release();
                args?.Release();
                nearArgs?.Release();
                midArgs?.Release();
                farArgs?.Release();
            }
        }

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();
        readonly List<Camera> m_Stale = new List<Camera>();
        readonly Plane[] m_Planes = new Plane[6];
        readonly Vector4[] m_PlaneData = new Vector4[6];
        readonly int[] m_Jobs = new int[MostJobs * 4];

        BoxData[] m_Boxes = new BoxData[0];
        CommandBuffer m_Commands;
        Material m_Material;
        Material m_FlowerMaterial;
        GraphicsBuffer m_NearIndices;
        GraphicsBuffer m_MidIndices;
        GraphicsBuffer m_FarIndices;
        Shader m_Shader;
        ComputeShader m_Compute;
        int m_PlaceKernel = -1;
        int m_CullKernel = -1;
        int m_OccludeKernel = -1;

        internal void Draw(ScriptableRenderContext context, Camera camera, HBStyleTerrain terrain, Texture trail)
        {
            if (!StyleTerrains.grassQuality.enabled)
            {
                Release();
                return;
            }

            if (StyleDebug.hidesGrass)
                return;

            var data = terrain.data;

            if (!Grows(terrain) || !Ready(terrain.grassShader, terrain.grassCompute))
                return;

            var level = StyleTerrains.grassQuality.current;
            var side = Mathf.Clamp(Mathf.RoundToInt(CellTarget / level.spacing), LeastSide, MostSide);
            var cell = side * level.spacing;
            var firstRing = level.spacing * ThinStart * 2f;
            var reach = Mathf.CeilToInt(Mathf.Min(firstRing * (1f + BandShare * 0.5f), level.distance) / cell);
            var rings = Mathf.Clamp(Mathf.CeilToInt(Mathf.Log(Mathf.Max(level.distance / firstRing, 1f), 2f)) + 1, 1,
                                    MostRings);
            var view = Acquire(camera, side, reach * 2 + 1, rings, level.spacing, firstRing, level.distance);
            var stamp = Stamp(terrain, level.spacing);
            var origin = terrain.transform.position;
            var size = data.size;
            var viewer = StyleDebug.Viewer(camera);
            var position = viewer.transform.position;

            GeometryUtility.CalculateFrustumPlanes(viewer, m_Planes);

            for (var i = 0; i < 6; i++)
                m_PlaneData[i] = new Vector4(m_Planes[i].normal.x, m_Planes[i].normal.y, m_Planes[i].normal.z,
                                             m_Planes[i].distance);

            var slotCount = view.ring * view.ring * rings;

            if (m_Boxes.Length < slotCount)
                m_Boxes = new BoxData[slotCount];

            var jobs = 0;
            var visible = 0;

            for (var ringIndex = 0; ringIndex < rings; ringIndex++)
            {
                var scale = 1 << ringIndex;
                var width = cell * scale;
                var outer = ringIndex + 1 >= rings ? level.distance : firstRing * scale * (1f + BandShare * 0.5f);
                var inner = ringIndex > 0 ? firstRing * (scale >> 1) * (1f - BandShare * 0.5f) : -1f;
                var centre = new Vector2Int(Mathf.FloorToInt(position.x / width),
                                            Mathf.FloorToInt(position.z / width));

                foreach (var offset in view.order)
                {
                    var spot = centre + offset;
                    var low = new Vector2(spot.x * width, spot.y * width);

                    if (low.x > origin.x + size.x || low.y > origin.z + size.z || low.x + width < origin.x ||
                        low.y + width < origin.z)
                        continue;

                    var slot = ringIndex * view.ring * view.ring + Wrap(spot.x, view.ring) +
                               Wrap(spot.y, view.ring) * view.ring;
                    var area = new Vector4(low.x - origin.x, low.y - origin.z, low.x + width - origin.x,
                                           low.y + width - origin.z);
                    var stale = !view.placed[slot] || view.cells[slot] != spot || view.stamps[slot] != stamp ||
                                data.GrassTouched(view.painted[slot], area);

                    if (!stale)
                        view.painted[slot] = data.grassPaint;

                    if (stale && jobs < MostJobs)
                    {
                        view.painted[slot] = data.grassPaint;
                        view.cells[slot] = spot;
                        view.stamps[slot] = stamp;
                        view.placed[slot] = true;
                        view.table[slot * 4] = spot.x;
                        view.table[slot * 4 + 1] = spot.y;
                        view.table[slot * 4 + 2] = ringIndex;
                        m_Jobs[jobs * 4] = slot;
                        m_Jobs[jobs * 4 + 1] = spot.x;
                        m_Jobs[jobs * 4 + 2] = spot.y;
                        m_Jobs[jobs * 4 + 3] = ringIndex;
                        jobs++;
                    }

                    if (!view.placed[slot] || view.cells[slot] != spot)
                        continue;

                    var box = Bounds(data, origin, low, width);
                    var far = Vector3.Max(position - box.min, box.max - position);

                    if (box.SqrDistance(position) > outer * outer || far.magnitude < inner)
                        continue;

                    if (!GeometryUtility.TestPlanesAABB(m_Planes, box))
                        continue;

                    m_Boxes[visible++] = new BoxData
                    {
                        centre = box.center, extent = box.extents, slot = (uint)slot
                    };
                }
            }

            var cmd = m_Commands ??= new CommandBuffer { name = "HB Style Grass" };

            cmd.Clear();

            if (jobs > 0)
                Place(cmd, view, terrain, jobs);

            cmd.SetBufferCounterValue(view.near, 0u);
            cmd.SetBufferCounterValue(view.mid, 0u);
            cmd.SetBufferCounterValue(view.far, 0u);
            cmd.SetBufferCounterValue(view.flowers, 0u);

            var grid = new Vector4(level.spacing, view.side, view.perSlot, PixelSlope(camera));
            var reachData = new Vector4(level.distance, firstRing, BandShare, rings);
            var cameraData = new Vector4(position.x, position.y, position.z, 0f);

            if (visible > 0)
            {
                cmd.SetBufferData(view.boxes, m_Boxes, 0, 0, visible);
                cmd.SetBufferCounterValue(view.visible, 0u);
                StyleDepthPyramidPass.Bind(StyleTerrains.pyramid, cmd, m_Compute, m_OccludeKernel, camera, viewer,
                                           position);
                cmd.SetComputeIntParam(m_Compute, s_BoxCountId, visible);
                cmd.SetComputeBufferParam(m_Compute, m_OccludeKernel, s_BoxesId, view.boxes);
                cmd.SetComputeBufferParam(m_Compute, m_OccludeKernel, s_SeenId, view.visible);
                cmd.DispatchCompute(m_Compute, m_OccludeKernel, Groups(visible), 1, 1);
                cmd.CopyCounterValue(view.visible, view.dispatch, CountOffset);
                cmd.SetComputeVectorParam(m_Compute, s_GridId, grid);
                cmd.SetComputeVectorParam(m_Compute, s_ReachId, reachData);
                cmd.SetComputeVectorParam(m_Compute, s_CameraId, cameraData);
                cmd.SetComputeVectorParam(m_Compute, s_RootsId, Roots(terrain));
                cmd.SetComputeVectorArrayParam(m_Compute, s_PlanesId, m_PlaneData);
                cmd.SetComputeVectorParam(m_Compute, s_ClockId, new Vector4(Shader.GetGlobalVector(s_TimeId).y, 0f));
                cmd.SetComputeVectorParam(m_Compute, s_AirId, Air());
                cmd.SetComputeVectorArrayParam(m_Compute, s_LayerSurfaceId, terrain.layerSurface);
                Shelter(cmd, camera);
                cmd.SetComputeTextureParam(m_Compute, m_CullKernel, s_TrailId, trail);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_BladesId, view.blades);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_SlotsId, view.slots);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_VisibleId, view.visible);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_NearId, view.near);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_MidId, view.mid);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_FarId, view.far);
                cmd.SetComputeBufferParam(m_Compute, m_CullKernel, s_FlowersId, view.flowers);
                cmd.DispatchCompute(m_Compute, m_CullKernel, view.dispatch, 0u);
            }

            cmd.CopyCounterValue(view.near, view.nearArgs, CountOffset);
            cmd.CopyCounterValue(view.mid, view.midArgs, CountOffset);
            cmd.CopyCounterValue(view.far, view.farArgs, CountOffset);
            cmd.CopyCounterValue(view.flowers, view.args, CountOffset);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (visible == 0)
                return;

            var style = terrain.grassStyle;

            Bind(view.nearBlock, view, view.near, grid, reachData, cameraData, style, terrain);
            Bind(view.midBlock, view, view.mid, grid, reachData, cameraData, style, terrain);
            Bind(view.farBlock, view, view.far, grid, reachData, cameraData, style, terrain);

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

            Graphics.RenderPrimitivesIndexedIndirect(parameters, MeshTopology.Triangles, m_NearIndices, view.nearArgs);
            parameters.matProps = view.midBlock;
            Graphics.RenderPrimitivesIndexedIndirect(parameters, MeshTopology.Triangles, m_MidIndices, view.midArgs);
            parameters.matProps = view.farBlock;
            Graphics.RenderPrimitivesIndexedIndirect(parameters, MeshTopology.Triangles, m_FarIndices, view.farArgs);

            if (data.flowerArray == null)
                return;

            Bind(view.flowerBlock, view, view.flowers, grid, reachData, cameraData, style, terrain);
            view.flowerBlock.SetTexture(s_FlowerHeadsId, data.flowerArray);
            view.flowerBlock.SetVectorArray(s_LayerFlowerId, terrain.layerFlower);

            parameters.material = m_FlowerMaterial;
            parameters.matProps = view.flowerBlock;
            Graphics.RenderPrimitivesIndirect(parameters, MeshTopology.Triangles, view.args);
        }

        static bool Grows(HBStyleTerrain terrain)
        {
            var grass = terrain.layerGrass;

            for (var index = 0; index < terrain.boundLayers; index++)
            {
                if (grass[index].x > 0f)
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
                CoreUtils.Destroy(m_FlowerMaterial);
                m_Material = CoreUtils.CreateEngineMaterial(shader);
                m_FlowerMaterial = CoreUtils.CreateEngineMaterial(shader);
                m_FlowerMaterial.EnableKeyword(FlowerKeyword);
                m_Shader = shader;
            }

            m_NearIndices ??= LeafIndices(s_NearLeaf, NearLeaves);
            m_MidIndices ??= LeafIndices(s_FarLeaf, MidLeaves);
            m_FarIndices ??= LeafIndices(s_FarLeaf, FarLeaves);

            m_Compute = compute;
            m_PlaceKernel = compute.HasKernel("Place") ? compute.FindKernel("Place") : -1;
            m_CullKernel = compute.HasKernel("Cull") ? compute.FindKernel("Cull") : -1;
            m_OccludeKernel = compute.HasKernel("Occlude") ? compute.FindKernel("Occlude") : -1;

            return m_PlaceKernel >= 0 && m_CullKernel >= 0 && m_OccludeKernel >= 0 &&
                   SystemInfo.supportsComputeShaders;
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
                hash = hash * 31 + terrain.boundLayers;
                hash = hash * 31 + spacing.GetHashCode();
                hash = hash * 31 + terrain.transform.position.GetHashCode();
                hash = hash * 31 + data.look.style.GetHashCode();
                hash = hash * 31 + data.look.variation.GetHashCode();

                for (var index = 0; index < terrain.boundLayers; index++)
                {
                    hash = hash * 31 + terrain.layerGrass[index].GetHashCode();
                    hash = hash * 31 + terrain.layerGrassEdge[index].GetHashCode();
                    hash = hash * 31 + terrain.layerVary[index].GetHashCode();
                    hash = hash * 31 + terrain.layerVaryShape[index].GetHashCode();
                    hash = hash * 31 + terrain.layerVarySize[index].GetHashCode();
                    hash = hash * 31 + terrain.layerFlower[index].GetHashCode();
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
            cmd.SetComputeVectorParam(m_Compute, s_RootsId, Roots(terrain));
            cmd.SetComputeVectorParam(m_Compute, s_LayersId, new Vector4(layers, (layers + 3) / 4,
                                                                         layers > 0 ? 1f : 0f, -1f));
            cmd.SetComputeVectorParam(m_Compute, s_TerrainStyleId, data.look.style);
            cmd.SetComputeVectorParam(m_Compute, s_TerrainVariationId, data.look.variation);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerGrassId, terrain.layerGrass);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerGrassEdgeId, terrain.layerGrassEdge);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerVaryId, terrain.layerVary);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerVaryShapeId, terrain.layerVaryShape);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerVarySizeId, terrain.layerVarySize);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerFlowerId, terrain.layerFlower);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerSTId, terrain.layerST);
            cmd.SetComputeVectorArrayParam(m_Compute, s_LayerLookId, terrain.layerLook);
            cmd.SetComputeBufferParam(m_Compute, m_PlaceKernel, s_JobsId, view.jobs);
            cmd.SetComputeBufferParam(m_Compute, m_PlaceKernel, s_BladesOutId, view.blades);
            cmd.DispatchCompute(m_Compute, m_PlaceKernel, Groups(view.perSlot), jobs, 1);
        }

        void Shelter(CommandBuffer cmd, Camera camera)
        {
            if (StyleTerrains.shelter != null && StyleTerrains.shelter.TryGetMap(camera, out var map) &&
                map.depth != null)
            {
                Texture cover = map.cover != null ? map.cover.rt : Texture2D.whiteTexture;

                cmd.SetComputeTextureParam(m_Compute, m_CullKernel, StyleShelterPass.depthId, map.depth);
                cmd.SetComputeTextureParam(m_Compute, m_CullKernel, StyleShelterPass.coverId, cover);
                cmd.SetComputeMatrixParam(m_Compute, StyleShelterPass.viewProjectionId, map.viewProjection);
                cmd.SetComputeVectorParam(m_Compute, StyleShelterPass.heightId, map.height);
                cmd.SetComputeVectorParam(m_Compute, StyleShelterPass.stateId, StyleShelterPass.State(false));
                return;
            }

            cmd.SetComputeTextureParam(m_Compute, m_CullKernel, StyleShelterPass.depthId, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(m_Compute, m_CullKernel, StyleShelterPass.coverId, Texture2D.whiteTexture);
            cmd.SetComputeVectorParam(m_Compute, StyleShelterPass.stateId, Vector4.zero);
        }

        static Vector4 Air()
        {
            return new Vector4(StyleEnvironment.weather.z, StyleEnvironment.windHeading * Mathf.Deg2Rad,
                               StyleWeatherState.wetness, StyleWeatherState.snow);
        }

        static Vector4 Roots(HBStyleTerrain terrain)
        {
            return new Vector4(terrain.transform.position.y, Mathf.Max(terrain.data.size.y, 1e-3f), 0f, 0f);
        }

        static void Bind(MaterialPropertyBlock block, View view, GraphicsBuffer list, Vector4 grid, Vector4 reach,
                         Vector4 camera, Vector4 style, HBStyleTerrain terrain)
        {
            block.SetBuffer(s_BladesId, view.blades);
            block.SetBuffer(s_SlotsId, view.slots);
            block.SetBuffer(s_ListId, list);
            block.SetVector(s_GridId, grid);
            block.SetVector(s_ReachId, reach);
            block.SetVector(s_CameraId, camera);
            block.SetVector(s_RootsId, Roots(terrain));
            block.SetVector(s_StyleId, style);
            block.SetFloat(s_ReceiveWeatherId, MovingWeather);
            block.SetVectorArray(s_LayerGrassTipId, terrain.layerGrassTip);
            block.SetVectorArray(s_LayerGrassEdgeId, terrain.layerGrassEdge);
            block.SetFloatArray(s_LayerWetGlossId, terrain.layerWetGloss);
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

        View Acquire(Camera camera, int side, int ring, int rings, float spacing, float firstRing, float distance)
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
                view.rings == rings && Mathf.Approximately(view.spacing, spacing) &&
                Mathf.Approximately(view.distance, distance))
            {
                view.lastUsed = now;
                return view;
            }

            view?.Release();

            var slots = ring * ring * rings;
            var perSlot = side * side;
            var blades = slots * perSlot;
            var nearBlades = ring * ring * perSlot;
            var close = firstRing * CloseShare;
            var full = firstRing * 0.5f;
            var joined = firstRing * (1f + BandShare * 0.5f);
            var spare = perSlot * 4;
            var closeTufts = Mathf.Min(Tufts(0f, close, full, spacing) + spare, nearBlades);
            var midTufts = Mathf.Min(Tufts(0f, joined, full, spacing) + spare, nearBlades);
            var farTufts = Mathf.Min(Tufts(0f, distance, full, spacing) + spare, blades);

            view = new View
            {
                side = side,
                ring = ring,
                rings = rings,
                spacing = spacing,
                distance = distance,
                perSlot = perSlot,
                lastUsed = now,
                blades = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blades, sizeof(uint) * 3),
                slots = new GraphicsBuffer(GraphicsBuffer.Target.Structured, slots, sizeof(int) * 4),
                jobs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MostJobs, sizeof(int) * 4),
                boxes = new GraphicsBuffer(GraphicsBuffer.Target.Structured, slots, BoxSize),
                visible = new GraphicsBuffer(GraphicsBuffer.Target.Append, slots, sizeof(uint)),
                near = new GraphicsBuffer(GraphicsBuffer.Target.Append, closeTufts, DrawnSize),
                mid = new GraphicsBuffer(GraphicsBuffer.Target.Append, midTufts, DrawnSize),
                far = new GraphicsBuffer(GraphicsBuffer.Target.Append, farTufts, DrawnSize),
                flowers = new GraphicsBuffer(GraphicsBuffer.Target.Append, Mathf.Min(blades, MostFlowers), DrawnSize),
                dispatch = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 3, sizeof(uint)),
                args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
                                          GraphicsBuffer.IndirectDrawArgs.size),
                nearArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
                                              GraphicsBuffer.IndirectDrawIndexedArgs.size),
                midArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
                                             GraphicsBuffer.IndirectDrawIndexedArgs.size),
                farArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1,
                                             GraphicsBuffer.IndirectDrawIndexedArgs.size),
                cells = new Vector2Int[slots],
                stamps = new int[slots],
                painted = new int[slots],
                placed = new bool[slots],
                table = new int[slots * 4],
                order = Order(ring / 2),
                nearBlock = new MaterialPropertyBlock(),
                midBlock = new MaterialPropertyBlock(),
                farBlock = new MaterialPropertyBlock(),
                flowerBlock = new MaterialPropertyBlock()
            };

            view.dispatch.SetData(new[] { (uint)Groups(perSlot), 0u, 1u });
            view.args.SetData(new[]
            {
                new GraphicsBuffer.IndirectDrawArgs { vertexCountPerInstance = FlowerVertices }
            });
            view.nearArgs.SetData(new[] { LeafArgs(s_NearLeaf, NearLeaves) });
            view.midArgs.SetData(new[] { LeafArgs(s_FarLeaf, MidLeaves) });
            view.farArgs.SetData(new[] { LeafArgs(s_FarLeaf, FarLeaves) });

            m_Views[camera] = view;
            return view;
        }

        static int Tufts(float inner, float outer, float full, float spacing)
        {
            var flat = Mathf.Min(outer, full);
            var area = inner < full ? Mathf.PI * (flat * flat - inner * inner) : 0f;

            if (outer > full)
                area += 2f * Mathf.PI * full * full * Mathf.Log(outer / Mathf.Max(inner, full));

            return Mathf.CeilToInt(Mathf.Max(area, 0f) / (spacing * spacing) * SpareTufts);
        }

        static float PixelSlope(Camera camera)
        {
            return camera.orthographic
                ? 0f
                : 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(camera.pixelHeight, 1);
        }

        static GraphicsBuffer.IndirectDrawIndexedArgs LeafArgs(ushort[] leaf, int leaves)
        {
            return new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = (uint)(leaves * leaf.Length) };
        }

        static GraphicsBuffer LeafIndices(ushort[] pattern, int leaves)
        {
            var indices = new ushort[leaves * pattern.Length];

            for (var index = 0; index < indices.Length; index++)
            {
                var leaf = index / pattern.Length;

                indices[index] = (ushort)(leaf * LeafVertices + pattern[index % pattern.Length]);
            }

            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Length, sizeof(ushort));

            buffer.SetData(indices);
            return buffer;
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
            m_NearIndices?.Release();
            m_NearIndices = null;
            m_MidIndices?.Release();
            m_MidIndices = null;
            m_FarIndices?.Release();
            m_FarIndices = null;
        }

        internal void Dispose()
        {
            Release();
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            CoreUtils.Destroy(m_FlowerMaterial);
            m_FlowerMaterial = null;
            m_Shader = null;
            m_Compute = null;
            m_Commands?.Release();
            m_Commands = null;
        }
    }
}
