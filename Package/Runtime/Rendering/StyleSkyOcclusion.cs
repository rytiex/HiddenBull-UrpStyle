using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleSkyOcclusionData : ContextItem
    {
        public TextureHandle texture;
        public Matrix4x4 viewProjection;
        public Camera camera;

        public override void Reset()
        {
            texture = TextureHandle.nullHandle;
            viewProjection = Matrix4x4.identity;
            camera = null;
        }

        public static bool TryGet(ContextContainer frameData, Camera camera, out StyleSkyOcclusionData data)
        {
            data = frameData.Contains<StyleSkyOcclusionData>() ? frameData.Get<StyleSkyOcclusionData>() : null;

            if (data != null && data.camera == camera && data.texture.IsValid())
                return true;

            data = null;
            return false;
        }
    }

    sealed class StyleSkyOcclusion : ScriptableRenderPass, IDisposable
    {
        public const string ShelterComputePath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute";

        public const int Resolution = 512;
        public const float Extent = 160f;
        public const float Above = 100f;
        public const float Depth = 250f;

        public const int FarResolution = 512;
        public const float FarExtent = 1024f;
        const int WideResolution = 512;
        const float WideExtent = 8192f;
        const float WideStep = 1024f;
        const float WideEdit = 5f;
        const int SlopeReach = 32;
        const float SlopeOpen = 1e9f;
        public const float FarAbove = 300f;
        public const float FarDepth = 1000f;

        const float Near = 0.1f;
        const float CoverTime = 2f;
        const float UncoverTime = 5f;
        const float ShelterVisible = 0.7f;
        const float InstantStep = 1e6f;
        const float ShelterMargin = 7f;
        const float StaleSeconds = 10f;
        const float PruneInterval = 1f;
        const float FarStep = 32f;
        const float FarRise = 20f;
        const float FarInterval = 2f;
        const float ShoreFar = 64f;
        const float SteadyLodSize = 8f;
        const float SteadyRise = 16f;
        const float FloorBlend = 1f;

        public struct View
        {
            public CullingResults culling;
            public Matrix4x4 view;
            public Matrix4x4 projection;
            public Vector3 origin;
            public Vector3 camera;
        }

        sealed class History : IDisposable
        {
            public RTHandle texture;
            public RTHandle fade;
            public Vector2Int low;
            public bool valid;
            public int lastUsed = -10;
            public float lastSeen;
            public RTHandle far;
            public RTHandle farFloor;
            public RTHandle seabed;
            public RTHandle seabedOld;
            public RTHandle shore;
            public RTHandle shoreOld;
            public Vector4 floor;
            public Vector4 floorOld;
            public Vector4 shoreInfo;
            public Vector4 shoreInfoOld;
            public float switched;
            public Vector3 farOrigin;
            public Vector3 farCamera;
            public float farTime;
            public bool farValid;
            public RTHandle wide;
            public RTHandle calm;
            public RTHandle calmOld;
            public Vector3 wideOrigin;
            public float wideTime;
            public bool wideValid;

            public History()
            {
                texture = RTHandles.Alloc(Resolution, Resolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Repeat,
                    enableRandomWrite: true,
                    name: "_HB_Shelter");

                fade = RTHandles.Alloc(Resolution, Resolution,
                    colorFormat: GraphicsFormat.R16_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Repeat,
                    enableRandomWrite: true,
                    name: "_HB_ShelterFade");

                far = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_ShelterFar");

                farFloor = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_ShelterFarFloor");

                seabed = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterFloor");

                seabedOld = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterFloorOld");

                shore = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterShore");

                shoreOld = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterShoreOld");

                wide = RTHandles.Alloc(WideResolution, WideResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterWide");

                calm = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterCalm");

                calmOld = RTHandles.Alloc(FarResolution, FarResolution,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "_HB_WaterCalmOld");
            }

            public void Dispose()
            {
                calm?.Release();
                calm = null;
                calmOld?.Release();
                calmOld = null;
                wide?.Release();
                wide = null;
                texture?.Release();
                texture = null;
                fade?.Release();
                fade = null;
                seabedOld?.Release();
                seabedOld = null;
                shoreOld?.Release();
                shoreOld = null;
                shore?.Release();
                shore = null;
                seabed?.Release();
                seabed = null;
                farFloor?.Release();
                farFloor = null;
                far?.Release();
                far = null;
            }
        }

        static readonly Matrix4x4 s_FlipZ = Matrix4x4.Scale(new Vector3(1f, 1f, -1f));
        static readonly Quaternion s_Down = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly ShaderTagId s_DepthOnlyTag = new ShaderTagId("DepthOnly");
        static readonly ShaderTagId s_SkyOcclusionTag = new ShaderTagId("HiddenBullSkyOcclusion");

        static readonly int s_SkyDepthId = Shader.PropertyToID("_HB_SkyDepth");
        static readonly int s_SkyLiquidId = Shader.PropertyToID("_HB_SkyLiquid");
        static readonly int s_ShelterId = Shader.PropertyToID("_HB_Shelter");
        static readonly int s_SkyViewProjId = Shader.PropertyToID("_HB_SkyViewProj");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_ShelterHeight");
        static readonly int s_WindowId = Shader.PropertyToID("_HB_ShelterWindow");
        static readonly int s_FollowId = Shader.PropertyToID("_HB_ShelterFollow");
        static readonly int s_ShelterFadeId = Shader.PropertyToID("_HB_ShelterFade");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_ShelterParams");
        static readonly int s_StateId = Shader.PropertyToID("_HB_ShelterState");
        static readonly int s_ShelterSourceId = Shader.PropertyToID("_HB_ShelterSource");
        static readonly int s_ShelterRainId = Shader.PropertyToID("_HB_ShelterRain");
        static readonly int s_ShelterBaseId = Shader.PropertyToID("_HB_ShelterBase");
        static readonly int s_ShelterFloorId = Shader.PropertyToID("_HB_ShelterFloor");
        static readonly int s_ShelterFarFloorId = Shader.PropertyToID("_HB_ShelterFarFloor");
        static readonly int s_SeabedId = Shader.PropertyToID("_HB_ShelterSeabed");
        static readonly int s_WaterFloorId = Shader.PropertyToID("_HB_WaterFloor");
        static readonly int s_WaterSeabedId = Shader.PropertyToID("_HB_WaterSeabed");
        static readonly int s_WaterShoreId = Shader.PropertyToID("_HB_WaterShore");
        static readonly int s_WaterShoreParamsId = Shader.PropertyToID("_HB_WaterShoreParams");
        static readonly int s_WaterFloorOldId = Shader.PropertyToID("_HB_WaterFloorOld");
        static readonly int s_WaterShoreOldId = Shader.PropertyToID("_HB_WaterShoreOld");
        static readonly int s_WaterFloorOldParamsId = Shader.PropertyToID("_HB_WaterFloorOldParams");
        static readonly int s_WaterShoreOldParamsId = Shader.PropertyToID("_HB_WaterShoreOldParams");
        static readonly int s_WaterBlendId = Shader.PropertyToID("_HB_WaterBlend");
        static readonly int s_WaterWideId = Shader.PropertyToID("_HB_WaterWide");
        static readonly int s_WaterWideParamsId = Shader.PropertyToID("_HB_WaterWideParams");
        static readonly int s_ShoreSeedInId = Shader.PropertyToID("_HB_ShoreSeedIn");
        static readonly int s_ShoreSeedOutId = Shader.PropertyToID("_HB_ShoreSeedOut");
        static readonly int s_ShoreOutId = Shader.PropertyToID("_HB_ShoreOut");
        static readonly int s_ShoreParamsId = Shader.PropertyToID("_HB_ShoreParams");
        static readonly int s_ShoreLevelMapId = Shader.PropertyToID("_HB_ShoreLevelMap");
        static readonly int s_ShoreAreaId = Shader.PropertyToID("_HB_ShoreArea");
        static readonly int s_ShoreOriginId = Shader.PropertyToID("_HB_ShoreOrigin");
        static readonly int s_SkyPassId = Shader.PropertyToID("_HB_SkyOcclusionPass");
        static readonly int s_GroundHeightmapId = Shader.PropertyToID("_HB_GroundHeightmap");
        static readonly int s_GroundOriginId = Shader.PropertyToID("_HB_GroundOrigin");
        static readonly int s_GroundSizeId = Shader.PropertyToID("_HB_GroundSize");
        static readonly int s_ShelterGroundId = Shader.PropertyToID("_HB_ShelterGround");
        static readonly int s_ShelterGroundParamsId = Shader.PropertyToID("_HB_ShelterGroundParams");
        static readonly int s_WaterFloorParamsId = Shader.PropertyToID("_HB_WaterFloorParams");
        static readonly int s_MasksId = Shader.PropertyToID("_HB_ShelterMasks");
        static readonly int s_MaskCountId = Shader.PropertyToID("_HB_ShelterMaskCount");
        static readonly int s_ShelterFarId = Shader.PropertyToID("_HB_ShelterFar");
        static readonly int s_FarParamsId = Shader.PropertyToID("_HB_ShelterFarParams");

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();

        readonly Dictionary<Camera, View> m_FarViews = new Dictionary<Camera, View>();
        readonly Dictionary<Camera, History> m_Histories = new Dictionary<Camera, History>();
        readonly List<Camera> m_Stale = new List<Camera>();
        readonly List<Terrain> m_Terrains = new List<Terrain>();
        readonly Dictionary<RenderTexture, RTHandle> m_Heightmaps = new Dictionary<RenderTexture, RTHandle>();
        readonly HashSet<Camera> m_Wanted = new HashSet<Camera>();
        readonly HashSet<Camera> m_Sheltered = new HashSet<Camera>();
        readonly float[] m_MaskDistances = new float[HBStyleWeatherMask.Limit];

        ComputeShader m_Compute;
        int m_Kernel;
        int m_OpenKernel;
        int m_FarKernel;
        int m_ReachKernel;
        int m_ErodeKernel;
        int m_DilateKernel;
        int m_GroundKernel;
        int m_GroundClearKernel;
        int m_SlopeKernel;
        RTHandle m_LevelHandle;
        Texture m_LevelTexture;
        int m_FarCoverKernel;
        int m_ShoreSeedKernel;
        int m_ShoreJumpKernel;
        int m_ShoreFieldKernel;
        bool m_Ready;
        RTHandle m_Rain;
        RTHandle m_Floor;
        float m_LastPrune;

        public StyleSkyOcclusion()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HiddenBull Style Sky Occlusion");

            RenderPipelineManager.beginCameraRendering += Prepare;
        }

        public void Setup(ComputeShader compute)
        {
            if (compute == m_Compute && m_Ready)
                return;

            m_Compute = compute;
            m_Ready = compute != null && compute.HasKernel("Follow") && compute.HasKernel("Open")
                   && compute.HasKernel("Far") && compute.HasKernel("Reach") && compute.HasKernel("Erode")
                   && compute.HasKernel("Dilate") && compute.HasKernel("ShoreSeed")
                   && compute.HasKernel("ShoreJump") && compute.HasKernel("ShoreField")
                   && compute.HasKernel("Ground") && compute.HasKernel("FarCover")
                   && compute.HasKernel("GroundClear") && compute.HasKernel("Slope");

            if (m_Ready)
            {
                m_Kernel = m_Compute.FindKernel("Follow");
                m_OpenKernel = m_Compute.FindKernel("Open");
                m_FarKernel = m_Compute.FindKernel("Far");
                m_ReachKernel = m_Compute.FindKernel("Reach");
                m_ErodeKernel = m_Compute.FindKernel("Erode");
                m_DilateKernel = m_Compute.FindKernel("Dilate");
                m_GroundKernel = m_Compute.FindKernel("Ground");
                m_GroundClearKernel = m_Compute.FindKernel("GroundClear");
                m_SlopeKernel = m_Compute.FindKernel("Slope");
                m_FarCoverKernel = m_Compute.FindKernel("FarCover");
                m_ShoreSeedKernel = m_Compute.FindKernel("ShoreSeed");
                m_ShoreJumpKernel = m_Compute.FindKernel("ShoreJump");
                m_ShoreFieldKernel = m_Compute.FindKernel("ShoreField");
            }
        }

        bool TryTake(Camera camera, out View view)
        {
            if (!m_Views.TryGetValue(camera, out view))
                return false;

            m_Views.Remove(camera);
            return true;
        }

        static bool Needed()
        {
            var stack = VolumeManager.instance.stack;

            if (stack == null)
                return false;

            var fog = stack.GetComponent<HBStyleFog>();
            var weather = stack.GetComponent<HBStyleWeather>();

            var fogActive = fog != null && fog.endDistance.value > fog.startDistance.value && fog.maxOpacity.value > 0f;

            return fogActive || (weather != null && weather.isVisible) || StyleWeatherState.surfaceActive;
        }

        void Prepare(ScriptableRenderContext context, Camera camera)
        {
            m_Views.Remove(camera);
            m_FarViews.Remove(camera);

            if (camera.cameraType == CameraType.Preview || !m_Wanted.Contains(camera))
                return;

            if (m_Sheltered.Contains(camera)
                && TryCull(context, camera, Extent, Resolution, Above, Depth, false, out var near))
                m_Views[camera] = near;

            if (FarDue(camera) && TryCull(context, camera, FarExtent, FarResolution, FarAbove, FarDepth, true,
                                          out var far))
                m_FarViews[camera] = far;
        }

        bool FarDue(Camera camera)
        {
            if (!m_Histories.TryGetValue(camera, out var history) || !history.farValid)
                return true;

            var moved = camera.transform.position - history.farCamera;

            return new Vector2(moved.x, moved.z).sqrMagnitude > FarStep * FarStep
                || Mathf.Abs(moved.y) > FarRise
                || Time.realtimeSinceStartup - history.farTime > FarInterval;
        }

        static bool TryCull(ScriptableRenderContext context, Camera camera, float extent, int resolution, float above,
                            float depth, bool steady, out View result)
        {
            result = default;

            if (!camera.TryGetCullingParameters(out var parameters))
                return false;

            var texel = extent / resolution;
            var position = camera.transform.position;

            var origin = new Vector3(
                Mathf.Floor(position.x / texel) * texel,
                (steady ? Mathf.Floor(position.y / SteadyRise) * SteadyRise : position.y) + above,
                Mathf.Floor(position.z / texel) * texel);

            var view = s_FlipZ * Matrix4x4.TRS(origin, s_Down, Vector3.one).inverse;
            var projection = Matrix4x4.Ortho(-extent * 0.5f, extent * 0.5f, -extent * 0.5f, extent * 0.5f, Near, depth);
            var culling = projection * view;

            var planes = GeometryUtility.CalculateFrustumPlanes(culling);

            parameters.cullingMatrix = culling;
            parameters.cullingPlaneCount = planes.Length;

            for (var i = 0; i < planes.Length; i++)
                parameters.SetCullingPlane(i, planes[i]);

            parameters.origin = origin;
            parameters.isOrthographic = true;
            parameters.cullingOptions = CullingOptions.None;
            parameters.shadowDistance = 0f;

            if (steady)
            {
                var detail = parameters.lodParameters;

                detail.isOrthographic = true;
                detail.cameraPosition = origin;
                detail.orthoSize = SteadyLodSize;
                detail.cameraPixelHeight = resolution;

                parameters.lodParameters = detail;
            }

            result = new View
            {
                culling = context.Cull(ref parameters),
                view = view,
                projection = projection,
                origin = origin,
                camera = position
            };

            return true;
        }

        History Acquire(Camera camera)
        {
            var now = Time.realtimeSinceStartup;

            if (now - m_LastPrune > PruneInterval)
            {
                m_LastPrune = now;
                Prune(now);
            }

            if (!m_Histories.TryGetValue(camera, out var history))
            {
                history = new History();
                m_Histories.Add(camera, history);
            }

            if (Time.frameCount - history.lastUsed > 1)
                history.valid = false;

            history.lastUsed = Time.frameCount;
            history.lastSeen = now;
            return history;
        }

        void Prune(float now)
        {
            m_Stale.Clear();

            foreach (var pair in m_Histories)
            {
                if (pair.Key == null || now - pair.Value.lastSeen > StaleSeconds)
                    m_Stale.Add(pair.Key);
            }

            foreach (var camera in m_Stale)
            {
                m_Histories[camera].Dispose();
                m_Histories.Remove(camera);
            }

            m_Wanted.RemoveWhere(static camera => camera == null);
            m_Sheltered.RemoveWhere(static camera => camera == null);
        }

        class SkyData
        {
            public RendererListHandle list;
            public RendererListHandle liquids;
            public Matrix4x4 skyView;
            public Matrix4x4 skyProjection;
            public Matrix4x4 cameraView;
            public Matrix4x4 cameraProjection;
        }

        class ShelterData
        {
            public ComputeShader compute;
            public int kernel;
            public TextureHandle depth;
            public TextureHandle liquid;
            public TextureHandle ground;
            public Vector4 groundParams;
            public TextureHandle shelter;
            public TextureHandle seabed;
            public TextureHandle fade;
            public Matrix4x4 skyViewProjection;
            public Vector4 height;
            public Vector4 window;
            public Vector4 follow;
            public Vector4 parameters;
            public Vector4 state;
            public int maskCount;
            public readonly Vector4[] masks = new Vector4[HBStyleWeatherMask.Limit];
        }

        class OpenData
        {
            public ComputeShader compute;
            public int kernel;
            public TextureHandle shelter;
            public TextureHandle rain;
            public float height;
            public float resolution;
            public Vector4 window;
            public TextureHandle levelMap;
            public Vector4 area;
            public Vector4 origin;
        }

        class FarData
        {
            public Vector4 parameters;
            public Vector4 floor;
            public Vector4 shore;
            public Vector4 floorOld;
            public Vector4 shoreOld;
            public float blend;
            public Vector4 wide;
        }

        class OffData
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;
            var occlusion = frameData.GetOrCreate<StyleSkyOcclusionData>();

            occlusion.Reset();

            var shown = camera.cameraType != CameraType.Preview;
            var sheltered = shown && Needed();
            var needed = sheltered || (shown && HBStyleLiquid.any);

            if (needed)
                m_Wanted.Add(camera);
            else
                m_Wanted.Remove(camera);

            if (sheltered)
                m_Sheltered.Add(camera);
            else
                m_Sheltered.Remove(camera);

            if (!needed)
            {
                m_Views.Remove(camera);
                m_FarViews.Remove(camera);
                RecordShelterOff(renderGraph);
                return;
            }

            if (!sheltered || !TryTake(camera, out var skyView))
            {
                m_Views.Remove(camera);
                RecordShelterOff(renderGraph);
                RecordFar(renderGraph, frameData, camera, out _, out _);
                return;
            }

            var resourceData = frameData.Get<UniversalResourceData>();
            var skyProjection = GL.GetGPUProjectionMatrix(skyView.projection, true);
            var skyViewProjection = skyProjection * skyView.view;

            var sky = renderGraph.CreateTexture(new TextureDesc(Resolution, Resolution)
            {
                format = GraphicsFormat.D16_UNorm,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                name = "_HB_SkyOcclusion"
            });

            using (var builder = renderGraph.AddRasterRenderPass<SkyData>(
                       "HiddenBull Style Sky Occlusion", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_DepthOnlyTag, frameData.Get<UniversalRenderingData>(), cameraData,
                    frameData.Get<UniversalLightData>(), SortingCriteria.CommonOpaque);

                var tags = new NativeArray<ShaderTagId>(1, Allocator.Temp);
                tags[0] = ShaderTagId.none;

                var states = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
                states[0] = new RenderStateBlock(RenderStateMask.Raster)
                {
                    rasterState = new RasterState(CullMode.Off)
                };

                passData.list = renderGraph.CreateRendererList(new RendererListParams(
                    skyView.culling, drawing, new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask))
                {
                    tagValues = tags,
                    stateBlocks = states,
                    isPassTagName = false
                });

                var liquidDrawing = RenderingUtils.CreateDrawingSettings(
                    s_SkyOcclusionTag, frameData.Get<UniversalRenderingData>(), cameraData,
                    frameData.Get<UniversalLightData>(), SortingCriteria.None);

                passData.liquids = renderGraph.CreateRendererList(new RendererListParams(
                    skyView.culling, liquidDrawing,
                    new FilteringSettings(RenderQueueRange.transparent, camera.cullingMask)));
                passData.skyView = skyView.view;
                passData.skyProjection = skyProjection;
                passData.cameraView = cameraData.GetViewMatrix();
                passData.cameraProjection = GL.GetGPUProjectionMatrix(
                    cameraData.GetProjectionMatrix(), !resourceData.isActiveTargetBackBuffer);

                builder.UseRendererList(passData.list);
                builder.UseRendererList(passData.liquids);
                builder.SetRenderAttachmentDepth(sky, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (SkyData data, RasterGraphContext context) =>
                {
                    RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.skyView, data.skyProjection, false);
                    context.cmd.SetGlobalFloat(s_SkyPassId, 1f);
                    context.cmd.DrawRendererList(data.list);
                    context.cmd.SetGlobalFloat(s_SkyPassId, 0f);
                    context.cmd.DrawRendererList(data.liquids);
                    RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.cameraView, data.cameraProjection, true);
                });
            }

            var liquid = renderGraph.CreateTexture(new TextureDesc(Resolution, Resolution)
            {
                format = GraphicsFormat.D16_UNorm,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                name = "_HB_SkyLiquid"
            });

            using (var builder = renderGraph.AddRasterRenderPass<SkyData>(
                       "HiddenBull Style Sky Liquids", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_SkyOcclusionTag, frameData.Get<UniversalRenderingData>(), cameraData,
                    frameData.Get<UniversalLightData>(), SortingCriteria.None);

                passData.liquids = renderGraph.CreateRendererList(new RendererListParams(
                    skyView.culling, drawing,
                    new FilteringSettings(RenderQueueRange.all, camera.cullingMask)));
                passData.skyView = skyView.view;
                passData.skyProjection = skyProjection;
                passData.cameraView = cameraData.GetViewMatrix();
                passData.cameraProjection = GL.GetGPUProjectionMatrix(
                    cameraData.GetProjectionMatrix(), !resourceData.isActiveTargetBackBuffer);

                builder.UseRendererList(passData.liquids);
                builder.SetRenderAttachmentDepth(liquid, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (SkyData data, RasterGraphContext context) =>
                {
                    RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.skyView, data.skyProjection, false);
                    context.cmd.DrawRendererList(data.liquids);
                    RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.cameraView, data.cameraProjection, true);
                });
            }

            occlusion.texture = sky;
            occlusion.viewProjection = skyViewProjection;
            occlusion.camera = camera;

            var farValid = RecordFar(renderGraph, frameData, camera, out var ground, out var groundParams);

            RecordShelter(renderGraph, camera, skyView, skyViewProjection, sky, liquid, farValid, ground, groundParams);
        }

        bool RecordFar(RenderGraph renderGraph, ContextContainer frameData, Camera camera, out TextureHandle ground,
                       out Vector4 groundParams)
        {
            ground = TextureHandle.nullHandle;
            groundParams = Vector4.zero;

            var taken = m_FarViews.TryGetValue(camera, out var farView);

            m_FarViews.Remove(camera);

            if (!m_Ready)
                return false;

            var history = Acquire(camera);
            var far = renderGraph.ImportTexture(history.far);
            var farFloor = renderGraph.ImportTexture(history.farFloor);

            RecordWide(renderGraph, camera, history);

            if (taken)
            {
                (history.seabed, history.seabedOld) = (history.seabedOld, history.seabed);
                (history.calm, history.calmOld) = (history.calmOld, history.calm);
                (history.shore, history.shoreOld) = (history.shoreOld, history.shore);
                history.floorOld = history.floor;
                history.shoreInfoOld = history.shoreInfo;
            }

            var seabed = renderGraph.ImportTexture(history.seabed);
            var seabedOld = renderGraph.ImportTexture(history.seabedOld);
            var calm = renderGraph.ImportTexture(history.calm);
            var calmOld = renderGraph.ImportTexture(history.calmOld);
            var shore = renderGraph.ImportTexture(history.shore);
            var shoreOld = renderGraph.ImportTexture(history.shoreOld);

            if (taken)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                var projection = GL.GetGPUProjectionMatrix(farView.projection, true);

                var depth = renderGraph.CreateTexture(new TextureDesc(FarResolution, FarResolution)
                {
                    format = GraphicsFormat.D16_UNorm,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    name = "_HB_SkyOcclusionFar"
                });

                using (var builder = renderGraph.AddRasterRenderPass<SkyData>(
                           "HiddenBull Style Sky Occlusion Far", out var passData, profilingSampler))
                {
                    var drawing = RenderingUtils.CreateDrawingSettings(
                        s_DepthOnlyTag, frameData.Get<UniversalRenderingData>(), cameraData,
                        frameData.Get<UniversalLightData>(), SortingCriteria.CommonOpaque);

                    var tags = new NativeArray<ShaderTagId>(1, Allocator.Temp);
                    tags[0] = ShaderTagId.none;

                    var states = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
                    states[0] = new RenderStateBlock(RenderStateMask.Raster)
                    {
                        rasterState = new RasterState(CullMode.Off)
                    };

                    passData.list = renderGraph.CreateRendererList(new RendererListParams(
                        farView.culling, drawing, new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask))
                    {
                        tagValues = tags,
                        stateBlocks = states,
                        isPassTagName = false
                    });

                    passData.skyView = farView.view;
                    passData.skyProjection = projection;
                    passData.cameraView = cameraData.GetViewMatrix();
                    passData.cameraProjection = GL.GetGPUProjectionMatrix(
                        cameraData.GetProjectionMatrix(), !resourceData.isActiveTargetBackBuffer);

                    builder.UseRendererList(passData.list);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc(static (SkyData data, RasterGraphContext context) =>
                    {
                        RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.skyView, data.skyProjection, false);
                        context.cmd.SetGlobalFloat(s_SkyPassId, 1f);
                        context.cmd.DrawRendererList(data.list);
                        context.cmd.SetGlobalFloat(s_SkyPassId, 0f);
                        RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.cameraView, data.cameraProjection,
                                                                    true);
                    });
                }

                var farLiquid = renderGraph.CreateTexture(new TextureDesc(FarResolution, FarResolution)
                {
                    format = GraphicsFormat.D16_UNorm,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    name = "_HB_SkyLiquidFar"
                });

                using (var builder = renderGraph.AddRasterRenderPass<SkyData>(
                           "HiddenBull Style Sky Liquids Far", out var passData, profilingSampler))
                {
                    var drawing = RenderingUtils.CreateDrawingSettings(
                        s_SkyOcclusionTag, frameData.Get<UniversalRenderingData>(), cameraData,
                        frameData.Get<UniversalLightData>(), SortingCriteria.None);

                    passData.liquids = renderGraph.CreateRendererList(new RendererListParams(
                        farView.culling, drawing, new FilteringSettings(RenderQueueRange.all, camera.cullingMask)));
                    passData.skyView = farView.view;
                    passData.skyProjection = projection;
                    passData.cameraView = cameraData.GetViewMatrix();
                    passData.cameraProjection = GL.GetGPUProjectionMatrix(
                        cameraData.GetProjectionMatrix(), !resourceData.isActiveTargetBackBuffer);

                    builder.UseRendererList(passData.liquids);
                    builder.SetRenderAttachmentDepth(farLiquid, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc(static (SkyData data, RasterGraphContext context) =>
                    {
                        RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.skyView, data.skyProjection, false);
                        context.cmd.DrawRendererList(data.liquids);
                        RenderingUtils.SetViewAndProjectionMatrices(context.cmd, data.cameraView, data.cameraProjection,
                                                                    true);
                    });
                }

                var seabedRaw = Seabed(renderGraph, "_HB_SeabedRaw");
                var seabedWorn = Seabed(renderGraph, "_HB_SeabedWorn");

                using (var builder = renderGraph.AddComputePass<ShelterData>(
                           "HiddenBull Style Shelter Far", out var passData, profilingSampler))
                {
                    var span = FarDepth - Near;
                    var reversed = SystemInfo.usesReversedZBuffer;

                    passData.compute = m_Compute;
                    passData.kernel = m_FarKernel;
                    passData.depth = depth;
                    passData.liquid = farLiquid;
                    passData.seabed = seabedRaw;
                    passData.shelter = far;
                    passData.skyViewProjection = projection * farView.view;
                    passData.height = new Vector4(
                        reversed ? span : -span,
                        reversed ? farView.origin.y - FarDepth : farView.origin.y - Near,
                        FarResolution,
                        FarExtent / FarResolution);
                    passData.window = new Vector4(farView.origin.x, farView.origin.z, 0f, 0f);
                    passData.maskCount = HBStyleWeatherMask.Collect(farView.origin, FarExtent * 0.5f, passData.masks,
                                                                  m_MaskDistances);

                    builder.UseTexture(depth, AccessFlags.Read);
                    builder.UseTexture(farLiquid, AccessFlags.Read);
                    builder.UseTexture(seabedRaw, AccessFlags.Write);
                    builder.UseTexture(far, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (ShelterData data, ComputeGraphContext context) =>
                    {
                        var cmd = context.cmd;

                        cmd.SetComputeTextureParam(data.compute, data.kernel, s_SkyDepthId, data.depth);
                        cmd.SetComputeTextureParam(data.compute, data.kernel, s_SkyLiquidId, data.liquid);
                        cmd.SetComputeTextureParam(data.compute, data.kernel, s_SeabedId, data.seabed);
                        cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterId, data.shelter);
                        cmd.SetComputeMatrixParam(data.compute, s_SkyViewProjId, data.skyViewProjection);
                        cmd.SetComputeVectorParam(data.compute, s_HeightId, data.height);
                        cmd.SetComputeVectorParam(data.compute, s_WindowId, data.window);
                        cmd.SetComputeFloatParam(data.compute, s_MaskCountId, data.maskCount);
                        cmd.SetComputeVectorArrayParam(data.compute, s_MasksId, data.masks);

                        var groups = (FarResolution + 7) / 8;
                        cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
                    });
                }

                RecordGround(renderGraph, seabedRaw, farView.origin, FarResolution, FarExtent);
                RecordFarCover(renderGraph, far, seabedRaw, farLiquid, projection * farView.view,
                               new Vector4(SystemInfo.usesReversedZBuffer ? FarDepth - Near : Near - FarDepth,
                                           SystemInfo.usesReversedZBuffer
                                               ? farView.origin.y - FarDepth
                                               : farView.origin.y - Near,
                                           FarResolution, FarExtent / FarResolution),
                               farView.origin);

                using (var builder = renderGraph.AddComputePass<OpenData>(
                           "HiddenBull Style Shelter Far Reach", out var passData, profilingSampler))
                {
                    passData.compute = m_Compute;
                    passData.kernel = m_ReachKernel;
                    passData.shelter = far;
                    passData.rain = farFloor;
                    passData.resolution = FarResolution;
                    passData.height = FarExtent / FarResolution;
                    passData.window = Vector4.zero;

                    builder.UseTexture(far, AccessFlags.Read);
                    builder.UseTexture(farFloor, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (OpenData data, ComputeGraphContext context) => Reach(data, context.cmd));
                }

                RecordSeabed(renderGraph, m_ErodeKernel, seabedRaw, seabedWorn);
                RecordSeabed(renderGraph, m_DilateKernel, seabedWorn, seabed);
                RecordSlope(renderGraph, seabed, calm, FarResolution, FarExtent / FarResolution, farView.origin,
                            HBStyleLiquidTerrain.TryGetSeaLevel(out var cap) ? cap : SlopeOpen);

                var ocean = HBStyleLiquidTerrain.TryGetSeaLevel(out var level);

                history.floor = new Vector4(farView.origin.x, farView.origin.z, 1f / FarExtent, 1f);
                history.shoreInfo = new Vector4(level, ocean ? 1f : 0f, 0f, 0f);
                history.switched = Time.realtimeSinceStartup;

                if (ocean)
                    RecordShore(renderGraph, seabed, shore, level, farView.origin);

                history.farOrigin = farView.origin;
                history.farCamera = farView.camera;
                history.farTime = Time.realtimeSinceStartup;
                history.farValid = true;
            }

            if (!history.farValid)
                return false;

            using (var builder = renderGraph.AddUnsafePass<FarData>("HiddenBull Style Shelter Far Bind", out var passData))
            {
                var texel = FarExtent / FarResolution;

                passData.parameters = new Vector4(history.farOrigin.x, history.farOrigin.z, 1f / FarExtent,
                                                  texel);
                passData.floor = history.floor;
                passData.floorOld = history.floorOld;
                passData.shoreOld = history.shoreInfoOld;
                passData.blend = Mathf.Clamp01((Time.realtimeSinceStartup - history.switched) / FloorBlend);
                passData.shore = history.shoreInfo;
                passData.shore.z = HBStyleLiquid.seaSwell;
                passData.wide = new Vector4(history.wideOrigin.x, history.wideOrigin.z, 1f / WideExtent,
                                            history.wideValid ? 1f : 0f);

                var wide = renderGraph.ImportTexture(history.wide);

                builder.UseTexture(wide, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(wide, s_WaterWideId);

                builder.UseTexture(far, AccessFlags.Read);
                builder.UseTexture(farFloor, AccessFlags.Read);
                builder.UseTexture(seabed, AccessFlags.Read);
                builder.UseTexture(shore, AccessFlags.Read);
                builder.UseTexture(seabedOld, AccessFlags.Read);
                builder.UseTexture(calm, AccessFlags.Read);
                builder.UseTexture(calmOld, AccessFlags.Read);
                builder.UseTexture(shoreOld, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(far, s_ShelterFarId);
                builder.SetGlobalTextureAfterPass(farFloor, s_ShelterFarFloorId);
                builder.SetGlobalTextureAfterPass(calm, s_WaterFloorId);
                builder.SetGlobalTextureAfterPass(seabed, s_WaterSeabedId);
                builder.SetGlobalTextureAfterPass(shore, s_WaterShoreId);
                builder.SetGlobalTextureAfterPass(calmOld, s_WaterFloorOldId);
                builder.SetGlobalTextureAfterPass(shoreOld, s_WaterShoreOldId);

                builder.SetRenderFunc(static (FarData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_FarParamsId, data.parameters);
                    context.cmd.SetGlobalVector(s_WaterFloorParamsId, data.floor);
                    context.cmd.SetGlobalVector(s_WaterShoreParamsId, data.shore);
                    context.cmd.SetGlobalVector(s_WaterFloorOldParamsId, data.floorOld);
                    context.cmd.SetGlobalVector(s_WaterShoreOldParamsId, data.shoreOld);
                    context.cmd.SetGlobalFloat(s_WaterBlendId, data.blend);
                    context.cmd.SetGlobalVector(s_WaterWideParamsId, data.wide);
                });
            }

            ground = seabed;
            groundParams = history.floor;

            return true;
        }

        class WaterData
        {
            public bool known;
            public TextureHandle floor;
            public TextureHandle shore;
            public TextureHandle floorOld;
            public TextureHandle seabed;
            public TextureHandle shoreOld;
            public TextureHandle wide;
            public FarData far;
        }

        public void RecordWater(RenderGraph renderGraph, Camera camera)
        {
            using var builder = renderGraph.AddUnsafePass<WaterData>("HiddenBull Style Water Bind", out var passData);

            passData.known = m_Histories.TryGetValue(camera, out var history) && history.farValid;
            passData.far ??= new FarData();

            if (passData.known)
            {
                passData.floor = renderGraph.ImportTexture(history.calm);
                passData.shore = renderGraph.ImportTexture(history.shore);
                passData.floorOld = renderGraph.ImportTexture(history.calmOld);
                passData.shoreOld = renderGraph.ImportTexture(history.shoreOld);
                passData.far.floor = history.floor;
                passData.far.floorOld = history.floorOld;
                passData.far.shore = history.shoreInfo;
                passData.far.shore.z = HBStyleLiquid.seaSwell;
                passData.far.shoreOld = history.shoreInfoOld;
                passData.far.blend = Mathf.Clamp01((Time.realtimeSinceStartup - history.switched) / FloorBlend);
                passData.far.wide = new Vector4(history.wideOrigin.x, history.wideOrigin.z, 1f / WideExtent,
                                                history.wideValid ? 1f : 0f);
                passData.wide = renderGraph.ImportTexture(history.wide);
                passData.seabed = renderGraph.ImportTexture(history.seabed);

                builder.UseTexture(passData.wide, AccessFlags.Read);
                builder.UseTexture(passData.seabed, AccessFlags.Read);

                builder.UseTexture(passData.floor, AccessFlags.Read);
                builder.UseTexture(passData.shore, AccessFlags.Read);
                builder.UseTexture(passData.floorOld, AccessFlags.Read);
                builder.UseTexture(passData.shoreOld, AccessFlags.Read);
            }

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc(static (WaterData data, UnsafeGraphContext context) =>
            {
                if (!data.known)
                {
                    context.cmd.SetGlobalVector(s_WaterFloorParamsId, Vector4.zero);
                    context.cmd.SetGlobalVector(s_WaterShoreParamsId, Vector4.zero);
                    context.cmd.SetGlobalVector(s_WaterWideParamsId, Vector4.zero);
                    return;
                }

                context.cmd.SetGlobalTexture(s_WaterFloorId, data.floor);
                context.cmd.SetGlobalTexture(s_WaterSeabedId, data.seabed);
                context.cmd.SetGlobalTexture(s_WaterShoreId, data.shore);
                context.cmd.SetGlobalTexture(s_WaterFloorOldId, data.floorOld);
                context.cmd.SetGlobalTexture(s_WaterShoreOldId, data.shoreOld);
                context.cmd.SetGlobalVector(s_WaterFloorParamsId, data.far.floor);
                context.cmd.SetGlobalVector(s_WaterShoreParamsId, data.far.shore);
                context.cmd.SetGlobalVector(s_WaterFloorOldParamsId, data.far.floorOld);
                context.cmd.SetGlobalVector(s_WaterShoreOldParamsId, data.far.shoreOld);
                context.cmd.SetGlobalFloat(s_WaterBlendId, data.far.blend);
                context.cmd.SetGlobalTexture(s_WaterWideId, data.wide);
                context.cmd.SetGlobalVector(s_WaterWideParamsId, data.far.wide);
            });
        }

        TextureHandle LevelMap(RenderGraph renderGraph)
        {
            var map = HBStyleLiquidTerrain.seaLevelMap;

            if (m_LevelHandle == null || m_LevelTexture != map)
            {
                m_LevelHandle?.Release();
                m_LevelHandle = RTHandles.Alloc(map);
                m_LevelTexture = map;
            }

            return renderGraph.ImportTexture(m_LevelHandle);
        }

        void RecordSlope(RenderGraph renderGraph, TextureHandle source, TextureHandle target, int resolution,
                         float texel, Vector3 origin, float cap)
        {
            var first = SlopeTexture(renderGraph, resolution, "_HB_SlopeFirst");
            var second = SlopeTexture(renderGraph, resolution, "_HB_SlopeSecond");
            var from = source;
            var flip = false;

            for (var step = SlopeReach; step >= 1; step /= 2)
            {
                var to = step == 1 ? target : (flip ? second : first);

                flip = !flip;

                using var builder = renderGraph.AddComputePass<OpenData>("HiddenBull Style Seabed Slope",
                                                                         out var passData, profilingSampler);

                passData.compute = m_Compute;
                passData.kernel = m_SlopeKernel;
                passData.shelter = from;
                passData.rain = to;
                passData.resolution = resolution;
                passData.height = texel;
                passData.window = new Vector4(step, cap, 0f, 0f);

                builder.UseTexture(from, AccessFlags.Read);
                builder.UseTexture(to, AccessFlags.Write);
                builder.AllowPassCulling(false);

                passData.levelMap = LevelMap(renderGraph);
                builder.UseTexture(passData.levelMap, AccessFlags.Read);
                passData.area = HBStyleLiquidTerrain.seaArea;
                passData.origin = new Vector4(origin.x, origin.z, 0f, 0f);

                builder.SetRenderFunc(static (OpenData data, ComputeGraphContext context) =>
                {
                    context.cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShoreLevelMapId, data.levelMap);
                    context.cmd.SetComputeVectorParam(data.compute, s_ShoreAreaId, data.area);
                    context.cmd.SetComputeVectorParam(data.compute, s_ShoreOriginId, data.origin);
                    Reach(data, context.cmd);
                });

                from = to;
            }
        }

        static TextureHandle SlopeTexture(RenderGraph renderGraph, int resolution, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
            {
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true,
                name = name
            });
        }

        void RecordSeabed(RenderGraph renderGraph, int kernel, TextureHandle source, TextureHandle target)
        {
            using var builder = renderGraph.AddComputePass<OpenData>("HiddenBull Style Seabed", out var passData,
                                                                     profilingSampler);

            passData.compute = m_Compute;
            passData.kernel = kernel;
            passData.shelter = source;
            passData.rain = target;
            passData.resolution = FarResolution;
            passData.height = FarExtent / FarResolution;
            passData.window = Vector4.zero;

            builder.UseTexture(source, AccessFlags.Read);
            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (OpenData data, ComputeGraphContext context) => Reach(data, context.cmd));
        }

        void ClearGround(RenderGraph renderGraph, TextureHandle target, int resolution)
        {
            using var builder = renderGraph.AddComputePass<GroundData>("HiddenBull Style Shelter Ground Clear",
                                                                       out var passData, profilingSampler);

            passData.compute = m_Compute;
            passData.kernel = m_GroundClearKernel;
            passData.seabed = target;
            passData.height = new Vector4(0f, 0f, resolution, 0f);

            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (GroundData data, ComputeGraphContext context) =>
            {
                var groups = ((int)data.height.z + 7) / 8;

                context.cmd.SetComputeTextureParam(data.compute, data.kernel, s_SeabedId, data.seabed);
                context.cmd.SetComputeVectorParam(data.compute, s_HeightId, data.height);
                context.cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
            });
        }

        static TextureHandle Seabed(RenderGraph renderGraph, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(FarResolution, FarResolution)
            {
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true,
                name = name
            });
        }

        class ShoreData
        {
            public ComputeShader compute;
            public int kernel;
            public TextureHandle source;
            public TextureHandle seedIn;
            public TextureHandle seedOut;
            public TextureHandle target;
            public Vector4 parameters;
            public TextureHandle levelMap;
            public Vector4 area;
            public Vector4 origin;
        }

        void RecordShore(RenderGraph renderGraph, TextureHandle seabed, TextureHandle shore, float level,
                         Vector3 origin)
        {
            var first = ShoreSeeds(renderGraph, "_HB_ShoreSeedsA");
            var second = ShoreSeeds(renderGraph, "_HB_ShoreSeedsB");
            var none = TextureHandle.nullHandle;

            RecordShoreStep(renderGraph, m_ShoreSeedKernel, seabed, none, first, none,
                            new Vector4(level, 0f, ShoreFar, 0f), origin);

            for (var step = FarResolution / 2; step >= 1; step /= 2)
            {
                RecordShoreStep(renderGraph, m_ShoreJumpKernel, none, first, second, none,
                                new Vector4(level, step, ShoreFar, 0f), origin);

                (first, second) = (second, first);
            }

            RecordShoreStep(renderGraph, m_ShoreFieldKernel, seabed, first, none, shore,
                            new Vector4(level, 0f, ShoreFar, 0f), origin);
        }

        void RecordShoreStep(RenderGraph renderGraph, int kernel, TextureHandle source, TextureHandle seedIn,
                             TextureHandle seedOut, TextureHandle target, Vector4 parameters, Vector3 origin)
        {
            using var builder = renderGraph.AddComputePass<ShoreData>("HiddenBull Style Shore", out var passData,
                                                                      profilingSampler);

            passData.compute = m_Compute;
            passData.kernel = kernel;
            passData.source = source;
            passData.seedIn = seedIn;
            passData.seedOut = seedOut;
            passData.target = target;
            passData.parameters = parameters;
            passData.levelMap = source.IsValid() ? LevelMap(renderGraph) : TextureHandle.nullHandle;
            passData.area = HBStyleLiquidTerrain.seaArea;
            passData.origin = new Vector4(origin.x, origin.z, 0f, 0f);

            if (source.IsValid())
            {
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(passData.levelMap, AccessFlags.Read);
            }

            if (seedIn.IsValid())
                builder.UseTexture(seedIn, AccessFlags.Read);

            if (seedOut.IsValid())
                builder.UseTexture(seedOut, AccessFlags.Write);

            if (target.IsValid())
                builder.UseTexture(target, AccessFlags.Write);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (ShoreData data, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                if (data.source.IsValid())
                {
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterSourceId, data.source);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShoreLevelMapId, data.levelMap);
                }

                if (data.seedIn.IsValid())
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShoreSeedInId, data.seedIn);

                if (data.seedOut.IsValid())
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShoreSeedOutId, data.seedOut);

                if (data.target.IsValid())
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShoreOutId, data.target);

                cmd.SetComputeVectorParam(data.compute, s_ShoreParamsId, data.parameters);
                cmd.SetComputeVectorParam(data.compute, s_ShoreAreaId, data.area);
                cmd.SetComputeVectorParam(data.compute, s_ShoreOriginId, data.origin);
                cmd.SetComputeVectorParam(data.compute, s_HeightId,
                                          new Vector4(0f, 0f, FarResolution, FarExtent / FarResolution));

                var groups = (FarResolution + 7) / 8;
                cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
            });
        }

        static TextureHandle ShoreSeeds(RenderGraph renderGraph, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(FarResolution, FarResolution)
            {
                format = GraphicsFormat.R32G32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true,
                name = name
            });
        }

        class GroundData
        {
            public ComputeShader compute;
            public int kernel;
            public TextureHandle seabed;
            public TextureHandle heightmap;
            public Vector4 origin;
            public Vector4 size;
            public Vector4 height;
            public Vector4 window;
        }

        void RecordWide(RenderGraph renderGraph, Camera camera, History history)
        {
            var eye = camera.transform.position;
            var away = new Vector2(eye.x - history.wideOrigin.x, eye.z - history.wideOrigin.z);
            var stale = !Application.isPlaying && Time.realtimeSinceStartup - history.wideTime > WideEdit;

            if (history.wideValid && !stale && away.sqrMagnitude < WideStep * WideStep)
                return;

            var texel = WideExtent / WideResolution;
            var centre = new Vector3(Mathf.Round(eye.x / texel) * texel, 0f, Mathf.Round(eye.z / texel) * texel);
            var wide = renderGraph.ImportTexture(history.wide);
            var raw = SlopeTexture(renderGraph, WideResolution, "_HB_WideRaw");

            using (var builder = renderGraph.AddComputePass<GroundData>("HiddenBull Style Wide Ground Clear",
                                                                       out var passData, profilingSampler))
            {
                passData.compute = m_Compute;
                passData.kernel = m_GroundClearKernel;
                passData.seabed = raw;

                builder.UseTexture(raw, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (GroundData data, ComputeGraphContext context) =>
                {
                    var groups = (WideResolution + 7) / 8;

                    context.cmd.SetComputeTextureParam(data.compute, data.kernel, s_SeabedId, data.seabed);
                    context.cmd.SetComputeVectorParam(data.compute, s_HeightId,
                                                      new Vector4(0f, 0f, WideResolution, 0f));
                    context.cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
                });
            }

            RecordGround(renderGraph, raw, centre, WideResolution, WideExtent);
            RecordSlope(renderGraph, raw, wide, WideResolution, texel, centre,
                        HBStyleLiquidTerrain.TryGetSeaLevel(out var cap) ? cap : SlopeOpen);

            history.wideOrigin = centre;
            history.wideTime = Time.realtimeSinceStartup;
            history.wideValid = true;
        }

        void RecordGround(RenderGraph renderGraph, TextureHandle seabed, Vector3 centre, int resolution,
                          float extent)
        {
            Terrain.GetActiveTerrains(m_Terrains);

            if (m_Heightmaps.Count > m_Terrains.Count)
                ReleaseHeightmaps();

            foreach (var terrain in m_Terrains)
            {
                var data = terrain.terrainData;

                if (data == null || data.heightmapTexture == null)
                    continue;

                var origin = terrain.GetPosition();
                var size = data.size;
                var reach = extent * 0.5f;

                if (origin.x > centre.x + reach || origin.x + size.x < centre.x - reach
                    || origin.z > centre.z + reach || origin.z + size.z < centre.z - reach)
                    continue;

                if (!m_Heightmaps.TryGetValue(data.heightmapTexture, out var wrapped))
                {
                    wrapped = RTHandles.Alloc(data.heightmapTexture);
                    m_Heightmaps.Add(data.heightmapTexture, wrapped);
                }

                var heightmap = renderGraph.ImportTexture(wrapped);

                using var builder = renderGraph.AddComputePass<GroundData>("HiddenBull Style Ground", out var passData,
                                                                       profilingSampler);

                passData.compute = m_Compute;
                passData.kernel = m_GroundKernel;
                passData.seabed = seabed;
                passData.heightmap = heightmap;
                passData.origin = origin;
                passData.size = new Vector4(size.x, size.y, size.z, data.heightmapResolution);
                passData.window = new Vector4(centre.x, centre.z, 0f, 0f);
                passData.height = new Vector4(0f, 0f, resolution, extent / resolution);

                builder.UseTexture(seabed, AccessFlags.ReadWrite);
                builder.UseTexture(heightmap, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (GroundData data, ComputeGraphContext context) =>
                {
                    var cmd = context.cmd;

                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_SeabedId, data.seabed);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_GroundHeightmapId, data.heightmap);
                    cmd.SetComputeVectorParam(data.compute, s_GroundOriginId, data.origin);
                    cmd.SetComputeVectorParam(data.compute, s_GroundSizeId, data.size);
                    cmd.SetComputeVectorParam(data.compute, s_WindowId, data.window);
                    cmd.SetComputeVectorParam(data.compute, s_HeightId, data.height);

                    var groups = ((int)data.height.z + 7) / 8;
                    cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
                });
            }

            m_Terrains.Clear();
        }

        void RecordFarCover(RenderGraph renderGraph, TextureHandle far, TextureHandle seabed, TextureHandle liquid,
                            Matrix4x4 skyViewProjection, Vector4 height, Vector3 centre)
        {
            using var builder = renderGraph.AddComputePass<ShelterData>("HiddenBull Style Shelter Far Cover",
                                                                    out var passData, profilingSampler);

            passData.compute = m_Compute;
            passData.kernel = m_FarCoverKernel;
            passData.shelter = far;
            passData.seabed = seabed;
            passData.liquid = liquid;
            passData.skyViewProjection = skyViewProjection;
            passData.height = height;
            passData.window = new Vector4(centre.x, centre.z, 0f, 0f);
            passData.maskCount = HBStyleWeatherMask.Collect(centre, FarExtent * 0.5f, passData.masks, m_MaskDistances);

            builder.UseTexture(far, AccessFlags.ReadWrite);
            builder.UseTexture(seabed, AccessFlags.Read);
            builder.UseTexture(liquid, AccessFlags.Read);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (ShelterData data, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterId, data.shelter);
                cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterSourceId, data.seabed);
                cmd.SetComputeTextureParam(data.compute, data.kernel, s_SkyLiquidId, data.liquid);
                cmd.SetComputeMatrixParam(data.compute, s_SkyViewProjId, data.skyViewProjection);
                cmd.SetComputeVectorParam(data.compute, s_HeightId, data.height);
                cmd.SetComputeVectorParam(data.compute, s_WindowId, data.window);
                cmd.SetComputeFloatParam(data.compute, s_MaskCountId, data.maskCount);
                cmd.SetComputeVectorArrayParam(data.compute, s_MasksId, data.masks);

                var groups = (FarResolution + 7) / 8;
                cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
            });
        }

        static void Reach(OpenData data, ComputeCommandBuffer cmd)
        {
            cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterSourceId, data.shelter);
            cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterFloorId, data.rain);
            cmd.SetComputeVectorParam(data.compute, s_WindowId, data.window);
            cmd.SetComputeVectorParam(data.compute, s_HeightId, new Vector4(0f, 0f, data.resolution, data.height));

            var groups = ((int)data.resolution + 7) / 8;
            cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
        }

        static void RecordShelterOff(RenderGraph renderGraph)
        {
            using var off = renderGraph.AddUnsafePass<OffData>("HiddenBull Style Sky Occlusion Off", out _);

            off.AllowPassCulling(false);
            off.AllowGlobalStateModification(true);
            off.SetRenderFunc(static (OffData data, UnsafeGraphContext context) =>
            {
                context.cmd.SetGlobalVector(s_StateId, Vector4.zero);
                context.cmd.SetGlobalVector(s_WaterFloorParamsId, Vector4.zero);
                context.cmd.SetGlobalVector(s_WaterShoreParamsId, Vector4.zero);
                context.cmd.SetGlobalVector(s_WaterWideParamsId, Vector4.zero);
            });
        }

        void RecordShelter(RenderGraph renderGraph, Camera camera, View skyView, Matrix4x4 skyViewProjection,
                           TextureHandle sky, TextureHandle liquid, bool farValid, TextureHandle ground,
                           Vector4 groundParams)
        {
            if (!m_Ready)
            {
                RecordShelterOff(renderGraph);
                return;
            }

            var history = Acquire(camera);
            var texel = Extent / Resolution;

            var low = new Vector2Int(
                Mathf.RoundToInt(skyView.origin.x / texel) - Resolution / 2,
                Mathf.RoundToInt(skyView.origin.z / texel) - Resolution / 2);

            ground = Seabed(renderGraph, "_HB_ShelterGround");
            groundParams = new Vector4(skyView.origin.x, skyView.origin.z, 1f / Extent, 1f);
            ClearGround(renderGraph, ground, Resolution);
            RecordGround(renderGraph, ground, skyView.origin, Resolution, Extent);

            var weather = VolumeManager.instance.stack?.GetComponent<HBStyleWeather>();
            var instant = !Application.isPlaying || (weather != null && weather.instant.value);
            var step = Time.deltaTime * (weather != null ? weather.speed.value : 1f);

            var span = Depth - Near;
            var reversed = SystemInfo.usesReversedZBuffer;

            var coverTime = weather != null ? weather.coverTime.value : CoverTime;
            var uncoverTime = weather != null ? weather.uncoverTime.value : UncoverTime;
            var baseHeight = skyView.origin.y - Above;

            var shelter = renderGraph.ImportTexture(history.texture);
            var fade = renderGraph.ImportTexture(history.fade);

            m_Rain ??= RTHandles.Alloc(Resolution, Resolution,
                colorFormat: GraphicsFormat.R16_SFloat,
                filterMode: FilterMode.Point,
                wrapMode: TextureWrapMode.Repeat,
                enableRandomWrite: true,
                name: "_HB_ShelterRain");

            var rain = renderGraph.ImportTexture(m_Rain);

            m_Floor ??= RTHandles.Alloc(Resolution, Resolution,
                colorFormat: GraphicsFormat.R32_SFloat,
                filterMode: FilterMode.Point,
                wrapMode: TextureWrapMode.Repeat,
                enableRandomWrite: true,
                name: "_HB_ShelterFloor");

            var floor = renderGraph.ImportTexture(m_Floor);

            using (var builder = renderGraph.AddComputePass<ShelterData>(
                       "HiddenBull Style Shelter", out var passData, profilingSampler))
            {
                passData.compute = m_Compute;
                passData.kernel = m_Kernel;
                passData.depth = sky;
                passData.liquid = liquid;
                passData.ground = ground;
                passData.groundParams = ground.IsValid() ? groundParams : Vector4.zero;
                passData.shelter = shelter;
                passData.skyViewProjection = skyViewProjection;
                passData.fade = fade;
                passData.height = new Vector4(
                    reversed ? span : -span,
                    reversed ? skyView.origin.y - Depth : skyView.origin.y - Near,
                    Resolution,
                    texel);
                passData.window = new Vector4(low.x, low.y, history.low.x, history.low.y);
                passData.maskCount = HBStyleWeatherMask.Collect(skyView.origin, Extent * 0.5f, passData.masks,
                                                              m_MaskDistances);
                passData.follow = new Vector4(
                    instant ? InstantStep : ShelterVisible * step / coverTime,
                    instant ? InstantStep : ShelterVisible * step / uncoverTime,
                    history.valid ? 1f : 0f,
                    ShelterVisible);
                passData.parameters = new Vector4(texel, 1f / Resolution, skyView.origin.x, skyView.origin.z);
                passData.state = new Vector4(1f, (Extent * 0.5f) - texel * ShelterMargin, baseHeight,
                                             farValid ? 1f : 0f);

                builder.UseTexture(sky, AccessFlags.Read);
                builder.UseTexture(liquid, AccessFlags.Read);

                if (ground.IsValid())
                    builder.UseTexture(ground, AccessFlags.Read);

                builder.UseTexture(shelter, AccessFlags.ReadWrite);
                builder.UseTexture(fade, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(shelter, s_ShelterId);
                builder.SetGlobalTextureAfterPass(fade, s_ShelterFadeId);

                builder.SetRenderFunc(static (ShelterData data, ComputeGraphContext context) =>
                {
                    var cmd = context.cmd;

                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_SkyDepthId, data.depth);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_SkyLiquidId, data.liquid);

                    if (data.ground.IsValid())
                        cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterGroundId, data.ground);
                    else
                        cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterGroundId, data.liquid);

                    cmd.SetComputeVectorParam(data.compute, s_ShelterGroundParamsId, data.groundParams);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterId, data.shelter);
                    cmd.SetComputeMatrixParam(data.compute, s_SkyViewProjId, data.skyViewProjection);
                    cmd.SetComputeVectorParam(data.compute, s_HeightId, data.height);
                    cmd.SetComputeVectorParam(data.compute, s_WindowId, data.window);
                    cmd.SetComputeFloatParam(data.compute, s_MaskCountId, data.maskCount);
                    cmd.SetComputeVectorArrayParam(data.compute, s_MasksId, data.masks);
                    cmd.SetComputeVectorParam(data.compute, s_FollowId, data.follow);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterFadeId, data.fade);

                    var groups = (Resolution + 7) / 8;
                    cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);

                    cmd.SetGlobalVector(s_ParamsId, data.parameters);
                    cmd.SetGlobalVector(s_StateId, data.state);
                });
            }

            using (var builder = renderGraph.AddComputePass<OpenData>(
                       "HiddenBull Style Shelter Rain", out var passData, profilingSampler))
            {
                passData.compute = m_Compute;
                passData.kernel = m_OpenKernel;
                passData.shelter = shelter;
                passData.rain = rain;
                passData.height = baseHeight;
                passData.resolution = Resolution;

                builder.UseTexture(shelter, AccessFlags.Read);
                builder.UseTexture(rain, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(rain, s_ShelterRainId);

                builder.SetRenderFunc(static (OpenData data, ComputeGraphContext context) =>
                {
                    var cmd = context.cmd;

                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterSourceId, data.shelter);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_ShelterRainId, data.rain);
                    cmd.SetComputeFloatParam(data.compute, s_ShelterBaseId, data.height);
                    cmd.SetComputeVectorParam(data.compute, s_HeightId, new Vector4(0f, 0f, data.resolution, 0f));

                    var groups = (Resolution + 7) / 8;
                    cmd.DispatchCompute(data.compute, data.kernel, groups, groups, 1);
                });
            }

            using (var builder = renderGraph.AddComputePass<OpenData>(
                       "HiddenBull Style Shelter Reach", out var passData, profilingSampler))
            {
                passData.compute = m_Compute;
                passData.kernel = m_ReachKernel;
                passData.shelter = shelter;
                passData.rain = floor;
                passData.resolution = Resolution;
                passData.height = texel;
                passData.window = new Vector4(low.x, low.y, 0f, 0f);

                builder.UseTexture(shelter, AccessFlags.Read);
                builder.UseTexture(floor, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(floor, s_ShelterFloorId);

                builder.SetRenderFunc(static (OpenData data, ComputeGraphContext context) => Reach(data, context.cmd));
            }

            history.low = low;
            history.valid = true;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= Prepare;
            m_LevelHandle?.Release();
            m_LevelHandle = null;
            m_LevelTexture = null;
            m_Views.Clear();
            m_FarViews.Clear();

            foreach (var history in m_Histories.Values)
                history.Dispose();

            m_Histories.Clear();

            m_Rain?.Release();
            m_Rain = null;

            m_Floor?.Release();
            m_Floor = null;

            ReleaseHeightmaps();
        }

        void ReleaseHeightmaps()
        {
            foreach (var heightmap in m_Heightmaps.Values)
                heightmap.Release();

            m_Heightmaps.Clear();
        }
    }
}
