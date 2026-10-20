using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleWavesPass : ScriptableRenderPass, IDisposable
    {
        public const string ComputePath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/StyleWaves.compute";

        public const int Bands = 3;
        public const int Count = 12;
        internal const float Chop = 0.9f;

        const int Resolution = 256;
        const float Steepness = 0.1f;
        const float Spread = 1.1f;
        const float Gravity = 9.81f;
        const float Shortest = 2f;
        const float Longest = 6.5f;
        const string CutPass = "CutMask";
        const string CutDepthPass = "CutDepth";
        const string CutDryPass = "CutDry";
        const string CutFloorPass = "CutFloor";
        const string CutRoofPass = "CutRoof";
        const float IncludeWeight = 8f;
        const float TightWeight = 0.125f;
        const int DryResolution = 512;
        const float DryExtent = 96f;
        const float DryFarExtent = 1024f;
        const float DryRise = 1000f;
        const float DryNear = 1f;

        static readonly float[] s_Patch = { 96f, 24f, 6f };

        static readonly int s_DisplacementOutId = Shader.PropertyToID("_HB_WaveDisplacementOut");
        static readonly int s_DerivativeOutId = Shader.PropertyToID("_HB_WaveDerivativeOut");
        static readonly int s_DisplacementId = Shader.PropertyToID("_HB_WaveDisplacement");
        static readonly int s_DerivativeId = Shader.PropertyToID("_HB_WaveDerivative");
        static readonly int s_SetId = Shader.PropertyToID("_HB_WaveSet");
        static readonly int s_PatchId = Shader.PropertyToID("_HB_WavePatch");
        static readonly int s_BakeId = Shader.PropertyToID("_HB_WaveBake");
        static readonly int s_FrameId = Shader.PropertyToID("_HB_WaveFrame");
        static readonly int s_ScaleId = Shader.PropertyToID("_HB_WaveScale");
        static readonly int s_TexelId = Shader.PropertyToID("_HB_WaveTexel");
        static readonly int s_CutMaskId = Shader.PropertyToID("_HB_LiquidCutMask");
        static readonly int s_DryMapId = Shader.PropertyToID("_HB_LiquidDryMap");
        static readonly int s_DryViewProjId = Shader.PropertyToID("_HB_LiquidDryViewProj");
        static readonly int s_DryFarMapId = Shader.PropertyToID("_HB_LiquidDryFarMap");
        static readonly int s_DryFloorId = Shader.PropertyToID("_HB_LiquidDryFloor");
        static readonly int s_DryFarFloorId = Shader.PropertyToID("_HB_LiquidDryFarFloor");
        static readonly int s_DryRoofId = Shader.PropertyToID("_HB_LiquidDryRoof");
        static readonly int s_DryFarViewProjId = Shader.PropertyToID("_HB_LiquidDryFarViewProj");
        static readonly int s_CutStateId = Shader.PropertyToID("_HB_LiquidCutState");
        static readonly int s_CutIncludeId = Shader.PropertyToID("_HB_LiquidCutInclude");
        static readonly int s_EconomyId = Shader.PropertyToID("_HB_LiquidEconomy");
        static readonly int s_SeatId = Shader.PropertyToID("_HB_LiquidSeat");
        static readonly int s_PoolsId = Shader.PropertyToID("_HB_LiquidPools");
        static readonly int s_PoolBoxesId = Shader.PropertyToID("_HB_LiquidPoolBoxes");
        static readonly int s_PoolSidesId = Shader.PropertyToID("_HB_LiquidPoolSides");
        static readonly int s_PoolCountId = Shader.PropertyToID("_HB_LiquidPoolCount");
        static readonly int s_CutWriteId = Shader.PropertyToID("_HB_CutWrite");
        static readonly int s_CutEyeId = Shader.PropertyToID("_HB_CutEye");
        static readonly int s_CutForwardId = Shader.PropertyToID("_HB_CutForward");
        static readonly int s_GridId = Shader.PropertyToID("_WaterGrid");
        static readonly int s_AreaId = Shader.PropertyToID("_WaterArea");
        static readonly int s_LevelMapId = Shader.PropertyToID("_WaterLevelMap");

        static MaterialPropertyBlock s_Block;

        const int CausticCount = 16;
        const int CausticResolution = 256;
        const float CausticPatch = 8f;
        const float CausticStrength = 0.16f;
        const float CausticPace = 0.5f;
        const float CausticShortest = 3f;
        const float CausticLongest = 10f;
        const int FoamResolution = 256;

        static readonly int s_CausticsOutId = Shader.PropertyToID("_HB_CausticsOut");
        static readonly int s_CausticsId = Shader.PropertyToID("_HB_Caustics");
        static readonly int s_CausticSetId = Shader.PropertyToID("_HB_CausticSet");
        static readonly int s_CausticBakeId = Shader.PropertyToID("_HB_CausticBake");
        static readonly int s_CausticParamsId = Shader.PropertyToID("_HB_CausticParams");
        static readonly int s_FoamOutId = Shader.PropertyToID("_HB_FoamOut");
        static readonly int s_FoamId = Shader.PropertyToID("_HB_FoamPattern");
        static readonly int s_FoamBakeId = Shader.PropertyToID("_HB_FoamBake");

        static readonly Vector4[] s_Caustics = new Vector4[CausticCount];
        static readonly double[] s_CausticSpeeds = new double[CausticCount];

        static readonly Vector4[] s_Waves = new Vector4[Bands * Count];
        static readonly double[] s_Speeds = new double[Bands * Count];
        static readonly double[] s_Starts = new double[Bands * Count];

        ComputeShader m_Compute;
        int m_Kernel;
        bool m_Ready;
        RTHandle m_Displacement;
        RTHandle m_Derivative;
        RTHandle m_Caustics;
        int m_CausticsKernel = -1;
        RTHandle m_Foam;
        int m_FoamKernel = -1;
        bool m_FoamBaked;
        bool m_Floors;
        float m_Economy;

        readonly List<CutDraw> m_Draws = new List<CutDraw>();
        RTHandle m_Dry;
        RTHandle m_DryFar;
        RTHandle m_DryFloor;
        RTHandle m_DryFarFloor;
        RTHandle m_DryRoof;
        StyleSkyOcclusion m_Shore;

        static StyleWavesPass()
        {
            var taken = new HashSet<Vector2Int>();

            for (var band = 0; band < Bands; band++)
            {
                taken.Clear();

                for (var index = 0; index < Count; index++)
                {
                    var slot = band * Count + index;
                    var reach = Mathf.Lerp(Shortest, Longest, index / (Count - 1f));
                    var angle = (Hash(slot, 17) - 0.5f) * 2f * Spread;

                    var cell = new Vector2Int(Mathf.RoundToInt(reach * Mathf.Cos(angle)),
                                              Mathf.RoundToInt(reach * Mathf.Sin(angle)));

                    if (cell == Vector2Int.zero)
                        cell.x = 1;

                    while (!taken.Add(cell))
                        cell.y++;

                    var number = new Vector2(cell.x, cell.y) * (2f * Mathf.PI / s_Patch[band]);
                    var length = number.magnitude;

                    s_Waves[slot] = new Vector4(number.x, number.y, Steepness / length, 0f);
                    s_Speeds[slot] = Math.Sqrt(Gravity * length);
                    s_Starts[slot] = Hash(slot, 53) * 2.0 * Math.PI;
                }
            }

            taken.Clear();

            for (var index = 0; index < CausticCount; index++)
            {
                var reach = Mathf.Lerp(CausticShortest, CausticLongest, Hash(index, 71));
                var angle = Hash(index, 29) * 2f * Mathf.PI;

                var cell = new Vector2Int(Mathf.RoundToInt(reach * Mathf.Cos(angle)),
                                          Mathf.RoundToInt(reach * Mathf.Sin(angle)));

                if (cell == Vector2Int.zero)
                    cell.x = 1;

                while (!taken.Add(cell))
                    cell.y++;

                var number = new Vector2(cell.x, cell.y) * (2f * Mathf.PI / CausticPatch);

                s_Caustics[index] = new Vector4(number.x, number.y, CausticStrength, Hash(index, 97) * 2f * Mathf.PI);
                s_CausticSpeeds[index] = Math.Sqrt(Gravity * number.magnitude) * CausticPace;
            }
        }

        internal static float Patch(int band)
        {
            return s_Patch[band];
        }

        internal static Vector4 Wave(int slot)
        {
            return s_Waves[slot];
        }

        internal static float Phase(int slot, double time)
        {
            return (float)(s_Starts[slot] - s_Speeds[slot] * time % (2.0 * Math.PI));
        }

        static float Hash(int index, int salt)
        {
            var value = (uint)(index * 73856093) ^ (uint)(salt * 19349663);

            value ^= value >> 15;
            value *= 0x2C1B3C6Du;
            value ^= value >> 12;
            value *= 0x297A2D39u;
            value ^= value >> 15;

            return value / 4294967296f;
        }

        public StyleWavesPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
            profilingSampler = new ProfilingSampler("HiddenBull Style Waves");
        }

        public bool isReady => m_Ready;

        public void Setup(ComputeShader compute, StyleSkyOcclusion shore, HBStyleLiquidQuality quality)
        {
            m_Economy = 2f - (float)quality;
            HBStyleLiquidTerrain.SetQuality(quality);

            m_Shore = shore;

            if (compute == m_Compute && m_Ready && m_CausticsKernel < 0 && compute.HasKernel("Caustics"))
                m_CausticsKernel = compute.FindKernel("Caustics");

            if (compute == m_Compute && m_Ready && m_FoamKernel < 0 && compute.HasKernel("Foam"))
                m_FoamKernel = compute.FindKernel("Foam");

            if (compute == m_Compute && m_Ready)
                return;

            m_Compute = compute;
            m_Ready = compute != null && compute.HasKernel("Bake");

            if (m_Ready)
            {
                m_Kernel = compute.FindKernel("Bake");
                m_CausticsKernel = compute.HasKernel("Caustics") ? compute.FindKernel("Caustics") : -1;
                m_FoamKernel = compute.HasKernel("Foam") ? compute.FindKernel("Foam") : -1;
            }
        }

        class PassData
        {
            public ComputeShader compute;
            public int kernel;
            public TextureHandle displacement;
            public TextureHandle derivative;
            public Vector4 frame;
            public readonly Vector4[] waves = new Vector4[Bands * Count];
            public int causticsKernel;
            public TextureHandle caustics;
            public int foamKernel;
            public TextureHandle foam;
            public bool foamBound;
            public readonly Vector4[] causticSet = new Vector4[CausticCount];
        }

        static RTHandle Allocate(string name)
        {
            return RTHandles.Alloc(Resolution, Resolution,
                slices: Bands,
                dimension: TextureDimension.Tex2DArray,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Trilinear,
                wrapMode: TextureWrapMode.Repeat,
                enableRandomWrite: true,
                useMipMap: true,
                autoGenerateMips: false,
                name: name);
        }

        struct CutDraw
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Matrix4x4 dryMatrix;
            public Material material;
            public int pass;
            public Vector4 grid;
            public Vector4 area;
            public Texture levelMap;
            public HBStyleLiquidTerrain terrain;
            public HBStyleLiquidPool pool;
            public bool ground;
            public bool lead;
            public bool seen;
            public bool surface;
            public int depthPass;
            public int dryPass;
            public int floorPass;
            public int roofPass;
            public Vector4 write;
            public bool dry;
        }

        class CutData
        {
            public readonly List<CutDraw> draws = new List<CutDraw>();
            public bool dry;
            public bool ground;
            public bool roof;
            public Matrix4x4 view;
            public Matrix4x4 projection;
            public Matrix4x4 cameraView;
            public Matrix4x4 cameraProjection;
            public Vector4 eye;
            public Vector4 forward;
            public Plane[] planes;
        }

        class OffData
        {
        }

        static void RecordOff(RenderGraph renderGraph)
        {
            using var off = renderGraph.AddUnsafePass<OffData>("HiddenBull Style Liquid Cut Off", out _);

            off.AllowPassCulling(false);
            off.AllowGlobalStateModification(true);
            off.SetRenderFunc(static (OffData data, UnsafeGraphContext context) =>
            {
                context.cmd.SetGlobalVector(s_CutStateId, Vector4.zero);
                context.cmd.SetGlobalFloat(s_SeatId, 0f);
                context.cmd.SetGlobalFloat(s_PoolCountId, 0f);
            });
        }

        readonly Plane[] m_Planes = new Plane[6];

        bool Collect(out bool dries, out Vector4 include)
        {
            m_Draws.Clear();
            dries = false;
            include = Vector4.zero;
            m_Floors = false;

            HBStyleLiquidPool.AssignChannels(HBStyleLiquidTerrain.AssignChannels());

            if (HBStyleLiquidCutter.enabledCutters.Count == 0)
                return false;

            foreach (var terrain in HBStyleLiquidTerrain.terrains)
            {
                if (terrain != null)
                    Gather(terrain.material, terrain, null, ref dries, ref include);
            }

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool != null)
                    Gather(pool.material, null, pool, ref dries, ref include);
            }

            return m_Draws.Count > 0;
        }

        void Gather(Material material, HBStyleLiquidTerrain terrain, HBStyleLiquidPool pool, ref bool dries,
                    ref Vector4 include)
        {
            var pass = material != null ? material.FindPass(CutPass) : -1;
            var depthPass = material != null ? material.FindPass(CutDepthPass) : -1;
            var dryPass = material != null ? material.FindPass(CutDryPass) : -1;
            var floorPass = material != null ? material.FindPass(CutFloorPass) : -1;
            var roofPass = material != null ? material.FindPass(CutRoofPass) : -1;

            if (pass < 0 || depthPass < 0 || dryPass < 0 || floorPass < 0 || roofPass < 0)
                return;

            var channel = pool != null ? pool.channel : terrain.channel;
            var lead = true;
            var first = true;

            foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
            {
                var reached = cutter != null && (pool != null ? cutter.Reaches(pool) : cutter.Reaches(terrain));
                var mesh = reached ? cutter.volume : null;

                if (channel < 0 || mesh == null || !mesh.HasVertexAttribute(VertexAttribute.Normal))
                    continue;

                var inside = cutter.mode == HBStyleLiquidCutMode.KeepInside;
                var seen = cutter.Spans(out var bounds) && GeometryUtility.TestPlanesAABB(m_Planes, bounds);
                var write = Vector4.zero;

                write[channel] = (inside ? IncludeWeight : 1f) + (cutter.tight ? TightWeight : 0f);

                if (inside)
                    include[channel] = 1f;

                dries |= cutter.dries;
                m_Floors |= cutter.dries && !cutter.driesGround;

                m_Draws.Add(new CutDraw
                {
                    mesh = mesh,
                    matrix = cutter.transform.localToWorldMatrix,
                    dryMatrix = cutter.dryMatrix,
                    material = material,
                    pass = pass,
                    grid = pool != null ? pool.cells : terrain.cells,
                    area = pool != null ? Vector4.zero : terrain.area,
                    levelMap = pool != null ? Texture2D.blackTexture : terrain.levelMap,
                    terrain = terrain,
                    pool = pool,
                    write = write,
                    dry = cutter.dries,
                    ground = cutter.driesGround,
                    lead = lead,
                    surface = seen && first,
                    seen = seen,
                    depthPass = depthPass,
                    dryPass = dryPass,
                    floorPass = floorPass,
                    roofPass = roofPass
                });

                lead = false;
                first &= !seen;
            }
        }

        static void Draw(RasterCommandBuffer cmd, CutData data)
        {
            s_Block ??= new MaterialPropertyBlock();

            RenderingUtils.SetViewAndProjectionMatrices(cmd, data.view, data.projection, false);
            cmd.SetGlobalVector(s_CutEyeId, data.eye);
            cmd.SetGlobalVector(s_CutForwardId, data.forward);

            foreach (var draw in data.draws)
            {
                if (!data.dry && !draw.seen)
                    continue;

                s_Block.SetVector(s_GridId, draw.grid);
                s_Block.SetVector(s_AreaId, draw.area);
                s_Block.SetTexture(s_LevelMapId, draw.levelMap);

                if (draw.surface && !data.dry)
                {
                    if (draw.terrain != null)
                        draw.terrain.Draw(cmd, draw.material, draw.depthPass, data.planes);
                    else if (draw.pool != null)
                        draw.pool.Draw(cmd, draw.material, draw.depthPass);
                }

                if (data.dry && (!draw.dry || (data.ground && draw.ground)))
                    continue;

                s_Block.SetVector(s_CutWriteId, draw.write);

                var pass = !data.dry ? draw.pass
                         : data.roof ? draw.roofPass
                         : data.ground ? draw.floorPass
                         : draw.dryPass;

                for (var part = 0; part < draw.mesh.subMeshCount; part++)
                    cmd.DrawMesh(draw.mesh, data.dry ? draw.dryMatrix : draw.matrix, draw.material, part, pass,
                                 s_Block);
            }

            RenderingUtils.SetViewAndProjectionMatrices(cmd, data.cameraView, data.cameraProjection, true);
        }

        static TextureHandle CutDepth(RenderGraph renderGraph, int width, int height)
        {
            return renderGraph.CreateTexture(new TextureDesc(width, height)
            {
                format = GraphicsFormat.D32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                name = "_HB_LiquidCutDepth"
            });
        }

        void RecordCuts(RenderGraph renderGraph, ContextContainer frameData, out Vector4 state, out Vector4 include,
                        out Matrix4x4 dryViewProjection, out Matrix4x4 dryFarViewProjection)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;

            state = new Vector4(0f, 0f, 0f, HBStyleLiquidTerrain.seaChannel);
            dryViewProjection = Matrix4x4.identity;
            dryFarViewProjection = Matrix4x4.identity;

            GeometryUtility.CalculateFrustumPlanes(camera, m_Planes);

            if (!Collect(out var dries, out include))
                return;

            state.x = 1f;
            state.z = dries ? 1f : 0f;

            var cameraView = cameraData.GetViewMatrix();
            var cameraProjection = GL.GetGPUProjectionMatrix(
                cameraData.GetProjectionMatrix(), !frameData.Get<UniversalResourceData>().isActiveTargetBackBuffer);
            var eye = camera.transform.position;
            var target = cameraData.cameraTargetDescriptor;

            var mask = renderGraph.CreateTexture(new TextureDesc(target.width, target.height)
            {
                format = GraphicsFormat.R16G16B16A16_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                clearColor = Color.clear,
                name = "_HB_LiquidCutMask"
            });

            for (var first = 0; first < m_Draws.Count;)
            {
                var last = first + 1;

                while (last < m_Draws.Count && !m_Draws[last].lead)
                    last++;

                using var builder = renderGraph.AddRasterRenderPass<CutData>("HiddenBull Style Liquid Cut",
                                                                           out var passData, profilingSampler);

                passData.draws.Clear();
                passData.planes = m_Planes;

                for (var index = first; index < last; index++)
                    passData.draws.Add(m_Draws[index]);

                first = last;

                passData.dry = false;
                passData.view = cameraView;
                passData.projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true);
                passData.cameraView = cameraView;
                passData.cameraProjection = cameraProjection;
                passData.eye = new Vector4(eye.x, eye.y, eye.z, camera.orthographic ? 0f : 1f);
                passData.forward = camera.transform.forward;

                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(CutDepth(renderGraph, target.width, target.height),
                                                 AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(mask, s_CutMaskId);
                builder.SetRenderFunc(static (CutData data, RasterGraphContext context) => Draw(context.cmd, data));
            }

            if (!dries)
                return;

            dryViewProjection = RecordDry(renderGraph, ref m_Dry, "_HB_LiquidDryMap", s_DryMapId, false, false,
                                          DryExtent, eye, cameraView, cameraProjection);
            dryFarViewProjection = RecordDry(renderGraph, ref m_DryFar, "_HB_LiquidDryFarMap", s_DryFarMapId, false,
                                             false, DryFarExtent, eye, cameraView, cameraProjection);
            RecordDry(renderGraph, ref m_DryRoof, "_HB_LiquidDryRoof", s_DryRoofId, false, true, DryExtent, eye,
                      cameraView, cameraProjection);

            if (!m_Floors)
                return;

            RecordDry(renderGraph, ref m_DryFloor, "_HB_LiquidDryFloor", s_DryFloorId, true, false, DryExtent, eye,
                      cameraView, cameraProjection);
            RecordDry(renderGraph, ref m_DryFarFloor, "_HB_LiquidDryFarFloor", s_DryFarFloorId, true, false,
                      DryFarExtent, eye, cameraView, cameraProjection);
        }

        Matrix4x4 RecordDry(RenderGraph renderGraph, ref RTHandle handle, string name, int id, bool ground,
                            bool roof, float extent, Vector3 eye, Matrix4x4 cameraView,
                            Matrix4x4 cameraProjection)
        {
            handle ??= RTHandles.Alloc(DryResolution, DryResolution,
                colorFormat: ground || roof ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Point,
                wrapMode: TextureWrapMode.Clamp,
                name: name);

            var texel = extent / DryResolution;
            var centre = new Vector3(Mathf.Round(eye.x / texel) * texel, eye.y + DryRise,
                                     Mathf.Round(eye.z / texel) * texel);
            var half = extent * 0.5f;
            var dryView = Matrix4x4.Scale(new Vector3(1f, 1f, -1f))
                        * Matrix4x4.TRS(centre, Quaternion.LookRotation(Vector3.down, Vector3.forward), Vector3.one)
                            .inverse;
            var dryProjection = GL.GetGPUProjectionMatrix(
                Matrix4x4.Ortho(-half, half, -half, half, DryNear, DryRise * 2f), true);
            var dry = renderGraph.ImportTexture(handle);

            using var builder = renderGraph.AddRasterRenderPass<CutData>("HiddenBull Style Liquid Dry",
                                                                        out var passData, profilingSampler);

            passData.draws.Clear();
            passData.planes = m_Planes;
            passData.draws.AddRange(m_Draws);
            passData.dry = true;
            passData.ground = ground;
            passData.roof = roof;
            passData.view = dryView;
            passData.projection = dryProjection;
            passData.cameraView = cameraView;
            passData.cameraProjection = cameraProjection;
            passData.eye = Vector4.zero;
            passData.forward = Vector3.down;

            builder.SetRenderAttachment(dry, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(dry, id);
            builder.SetRenderFunc(static (CutData data, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(false, true, Color.clear);
                Draw(context.cmd, data);
            });

            return dryProjection * dryView;
        }

        class StateData
        {
            public Vector4 state;
            public Vector4 include;
            public Matrix4x4 dryViewProjection;
            public Matrix4x4 dryFarViewProjection;
            public bool floors;
            public float economy;
            public float seat;
            public readonly Vector4[] pools = new Vector4[HBStyleLiquidPool.Most];
            public readonly Vector4[] poolBoxes = new Vector4[HBStyleLiquidPool.Most];
            public readonly Vector4[] poolSides = new Vector4[HBStyleLiquidPool.Most];
            public int poolCount;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_Ready || !HBStyleLiquid.any
                || frameData.Get<UniversalCameraData>().cameraType == CameraType.Preview)
            {
                RecordOff(renderGraph);
                return;
            }

            HBStyleLiquidTerrain.Refresh();
            RecordBake(renderGraph);

            m_Shore?.RecordWater(renderGraph, frameData.Get<UniversalCameraData>().camera);
            RecordCuts(renderGraph, frameData, out var state, out var include, out var dryViewProjection,
                       out var dryFarViewProjection);

            using var builder = renderGraph.AddUnsafePass<StateData>("HiddenBull Style Liquid Cut State",
                                                                     out var passData);

            passData.state = state;
            passData.include = include;
            passData.dryViewProjection = dryViewProjection;
            passData.dryFarViewProjection = dryFarViewProjection;
            passData.floors = m_Floors;
            passData.economy = m_Economy;

            var sea = HBStyleLiquidTerrain.sea;
            var eye = frameData.Get<UniversalCameraData>().camera.transform.position;

            passData.seat = sea != null ? sea.LevelAt(new Vector2(eye.x, eye.z)) - sea.level : 0f;
            passData.poolCount = 0;

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool == null || passData.poolCount >= HBStyleLiquidPool.Most)
                    continue;

                var centre = pool.transform.position;

                passData.pools[passData.poolCount] = new Vector4(centre.x, centre.z, pool.level, pool.bottom);
                passData.poolBoxes[passData.poolCount] = pool.box;
                passData.poolSides[passData.poolCount] = new Vector4(pool.channel, 0f, 0f, 0f);
                passData.poolCount++;
            }

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (StateData data, UnsafeGraphContext context) =>
            {
                context.cmd.SetGlobalVector(s_CutStateId, data.state);
                context.cmd.SetGlobalVector(s_CutIncludeId, data.include);
                context.cmd.SetGlobalMatrix(s_DryViewProjId, data.dryViewProjection);
                context.cmd.SetGlobalMatrix(s_DryFarViewProjId, data.dryFarViewProjection);
                context.cmd.SetGlobalFloat(s_EconomyId, data.economy);
                context.cmd.SetGlobalFloat(s_SeatId, data.seat);
                context.cmd.SetGlobalVectorArray(s_PoolsId, data.pools);
                context.cmd.SetGlobalVectorArray(s_PoolBoxesId, data.poolBoxes);
                context.cmd.SetGlobalVectorArray(s_PoolSidesId, data.poolSides);
                context.cmd.SetGlobalFloat(s_PoolCountId, data.poolCount);

                if (data.floors)
                    return;

                context.cmd.SetGlobalTexture(s_DryFloorId, Texture2D.blackTexture);
                context.cmd.SetGlobalTexture(s_DryFarFloorId, Texture2D.blackTexture);
            });
        }

        void RecordBake(RenderGraph renderGraph)
        {
            m_Displacement ??= Allocate("_HB_WaveDisplacement");
            m_Derivative ??= Allocate("_HB_WaveDerivative");

            var heading = HBStyleLiquid.windHeading * Mathf.Deg2Rad;
            var time = HBStyleLiquid.time;

            using var builder = renderGraph.AddUnsafePass<PassData>("HiddenBull Style Waves", out var passData,
                                                                    profilingSampler);

            passData.compute = m_Compute;
            passData.kernel = m_Kernel;
            passData.displacement = renderGraph.ImportTexture(m_Displacement);
            passData.derivative = renderGraph.ImportTexture(m_Derivative);
            passData.frame = new Vector4(Mathf.Sin(heading), Mathf.Cos(heading), 1f, HBStyleLiquid.wind);

            for (var slot = 0; slot < s_Waves.Length; slot++)
            {
                var wave = s_Waves[slot];

                wave.w = Phase(slot, time);
                passData.waves[slot] = wave;
            }

            m_Caustics ??= RTHandles.Alloc(CausticResolution, CausticResolution,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Trilinear,
                wrapMode: TextureWrapMode.Repeat,
                enableRandomWrite: true,
                useMipMap: true,
                autoGenerateMips: false,
                name: "_HB_Caustics");

            passData.causticsKernel = m_CausticsKernel;
            passData.caustics = renderGraph.ImportTexture(m_Caustics);

            for (var index = 0; index < CausticCount; index++)
            {
                var wave = s_Caustics[index];

                wave.w -= (float)(s_CausticSpeeds[index] * time % (2.0 * Math.PI));
                passData.causticSet[index] = wave;
            }

            builder.UseTexture(passData.caustics, AccessFlags.Write);
            builder.SetGlobalTextureAfterPass(passData.caustics, s_CausticsId);

            if (m_Foam == null)
            {
                m_Foam = RTHandles.Alloc(FoamResolution, FoamResolution,
                    colorFormat: GraphicsFormat.R16_SFloat,
                    filterMode: FilterMode.Trilinear,
                    wrapMode: TextureWrapMode.Repeat,
                    enableRandomWrite: true,
                    useMipMap: true,
                    autoGenerateMips: false,
                    name: "_HB_FoamPattern");
                m_FoamBaked = false;
            }

            passData.foamKernel = m_FoamBaked ? -1 : m_FoamKernel;
            passData.foam = renderGraph.ImportTexture(m_Foam);
            passData.foamBound = m_FoamBaked || m_FoamKernel >= 0;
            m_FoamBaked |= m_FoamKernel >= 0;

            builder.UseTexture(passData.foam, AccessFlags.Write);
            builder.SetGlobalTextureAfterPass(passData.foam, s_FoamId);
            builder.UseTexture(passData.displacement, AccessFlags.Write);
            builder.UseTexture(passData.derivative, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(passData.displacement, s_DisplacementId);
            builder.SetGlobalTextureAfterPass(passData.derivative, s_DerivativeId);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                cmd.SetComputeTextureParam(data.compute, data.kernel, s_DisplacementOutId, data.displacement);
                cmd.SetComputeTextureParam(data.compute, data.kernel, s_DerivativeOutId, data.derivative);
                cmd.SetComputeVectorArrayParam(data.compute, s_SetId, data.waves);
                cmd.SetComputeVectorParam(data.compute, s_PatchId,
                                          new Vector4(s_Patch[0], s_Patch[1], s_Patch[2], 0f));
                cmd.SetComputeVectorParam(data.compute, s_BakeId, new Vector4(Resolution, Chop, 0f, 0f));

                var groups = (Resolution + 7) / 8;

                cmd.DispatchCompute(data.compute, data.kernel, groups, groups, Bands);
                cmd.GenerateMips(data.displacement);
                cmd.GenerateMips(data.derivative);

                if (data.causticsKernel >= 0)
                {
                    var tiles = (CausticResolution + 7) / 8;

                    cmd.SetComputeTextureParam(data.compute, data.causticsKernel, s_CausticsOutId, data.caustics);
                    cmd.SetComputeVectorArrayParam(data.compute, s_CausticSetId, data.causticSet);
                    cmd.SetComputeVectorParam(data.compute, s_CausticBakeId,
                                              new Vector4(CausticResolution, CausticPatch, 0f, 0f));
                    cmd.DispatchCompute(data.compute, data.causticsKernel, tiles, tiles, 1);
                    cmd.GenerateMips(data.caustics);
                }

                if (data.foamKernel >= 0)
                {
                    var tiles = (FoamResolution + 7) / 8;

                    cmd.SetComputeTextureParam(data.compute, data.foamKernel, s_FoamOutId, data.foam);
                    cmd.SetComputeVectorParam(data.compute, s_FoamBakeId, new Vector4(FoamResolution, 0f, 0f, 0f));
                    cmd.DispatchCompute(data.compute, data.foamKernel, tiles, tiles, 1);
                    cmd.GenerateMips(data.foam);
                }

                cmd.SetGlobalVector(s_CausticParamsId,
                                    new Vector4(1f / CausticPatch, data.causticsKernel >= 0 ? 1f : 0f,
                                                data.foamBound ? 1f : 0f, 0f));

                cmd.SetGlobalVector(s_FrameId, data.frame);
                cmd.SetGlobalVector(s_ScaleId, new Vector4(1f / s_Patch[0], 1f / s_Patch[1], 1f / s_Patch[2], 0f));
                cmd.SetGlobalVector(s_TexelId, new Vector4(s_Patch[0] / Resolution, s_Patch[1] / Resolution,
                                                           s_Patch[2] / Resolution, 1f));
            });
        }

        public void Dispose()
        {
            m_Displacement?.Release();
            m_Displacement = null;

            m_Derivative?.Release();
            m_Derivative = null;

            m_Foam?.Release();
            m_Foam = null;
            m_FoamBaked = false;

            m_Caustics?.Release();
            m_Caustics = null;

            m_Dry?.Release();
            m_Dry = null;

            m_DryFar?.Release();
            m_DryFar = null;

            m_DryFloor?.Release();
            m_DryFloor = null;

            m_DryFarFloor?.Release();
            m_DryFarFloor = null;

            m_DryRoof?.Release();
            m_DryRoof = null;
        }
    }
}
