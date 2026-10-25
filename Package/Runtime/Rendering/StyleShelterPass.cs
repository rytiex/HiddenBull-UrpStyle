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
    sealed class StyleShelterPass : ScriptableRenderPass, IDisposable
    {
        internal const string ComputePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute";

        internal const string UndersidePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUnderside.shader";

        internal const float Extent = 160f;
        const float FarExtent = 1000f;
        const int FarResolution = 512;
        const float FarStep = 50f;
        const float Above = 100f;
        const float Depth = 1000f;
        const float Near = 0.1f;
        const double FreshSeconds = 0.5;

        sealed class Map
        {
            internal RTHandle depth;
            internal RTHandle dilated;
            internal RTHandle under;
            internal RTHandle far;
            internal RTHandle bound;
            internal RTHandle boundUnder;
            internal double lastUsed;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Matrix4x4 farViewProjection;
            internal Vector4 farHeight;
            internal Vector3 farCentre;
            internal bool farValid;
        }

        internal struct Binding
        {
            internal RTHandle depth;
            internal RTHandle under;
            internal RTHandle far;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Matrix4x4 farViewProjection;
            internal Vector4 farHeight;
        }

        struct View
        {
            internal CullingResults culling;
            internal Matrix4x4 view;
            internal Matrix4x4 projection;
            internal Vector3 origin;
            internal bool farDue;
            internal CullingResults farCulling;
            internal Matrix4x4 farView;
            internal Matrix4x4 farProjection;
            internal Vector3 farOrigin;
            internal Vector3 farCentre;
        }

        class SkyData
        {
            internal RendererListHandle list;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class UnderData
        {
            internal RendererListHandle list;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class BindData
        {
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Matrix4x4 farViewProjection;
            internal Vector4 farHeight;
            internal bool noUnderside;
            internal bool hasFar;
        }

        class DilateData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle source;
            internal TextureHandle extra;
            internal TextureHandle target;
            internal int extraId;
            internal int targetId;
            internal Vector4 height;
            internal Vector4 reach;
            internal int groups;
        }

        class PreviewData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle depth;
            internal TextureHandle under;
            internal bool withUnder;
            internal TextureHandle preview;
            internal int previewId;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Vector4 area;
            internal int groups;
        }

        static readonly Matrix4x4 s_FlipZ = Matrix4x4.Scale(new Vector3(1f, 1f, -1f));
        static readonly Quaternion s_Down = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly ShaderTagId s_DepthOnlyTag = new ShaderTagId("DepthOnly");

        static readonly int s_DepthId = Shader.PropertyToID("_HB_ShelterDepth");
        static readonly int s_ViewProjId = Shader.PropertyToID("_HB_ShelterViewProj");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_ShelterHeight");
        static readonly int s_StateId = Shader.PropertyToID("_HB_ShelterState");
        static readonly int s_PassId = Shader.PropertyToID("_HB_ShelterPass");
        static readonly int s_PreviewId = Shader.PropertyToID("_HB_ShelterPreview");
        static readonly int s_HeightsId = Shader.PropertyToID("_HB_ShelterHeights");
        static readonly int s_PreviewAreaId = Shader.PropertyToID("_HB_ShelterPreviewArea");
        static readonly int s_SourceId = Shader.PropertyToID("_HB_ShelterSource");
        static readonly int s_TargetId = Shader.PropertyToID("_HB_ShelterTarget");
        static readonly int s_UnderId = Shader.PropertyToID("_HB_ShelterUnder");
        static readonly int s_CarryId = Shader.PropertyToID("_HB_ShelterCarry");
        static readonly int s_CarryTargetId = Shader.PropertyToID("_HB_ShelterCarryTarget");
        static readonly int s_DilateId = Shader.PropertyToID("_HB_ShelterDilate");
        static readonly int s_FarDepthId = Shader.PropertyToID("_HB_ShelterFarDepth");
        static readonly int s_FarViewProjId = Shader.PropertyToID("_HB_ShelterFarViewProj");
        static readonly int s_FarHeightId = Shader.PropertyToID("_HB_ShelterFarHeight");

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();
        readonly HashSet<Camera> m_Wanted = new HashSet<Camera>();
        readonly Dictionary<Camera, Map> m_Maps = new Dictionary<Camera, Map>();
        readonly List<Camera> m_Stale = new List<Camera>();

        ComputeShader m_Compute;
        Shader m_UndersideShader;
        int m_PreviewKernel = -1;
        int m_HeightsKernel = -1;
        int m_DilateXKernel = -1;
        int m_DilateZKernel = -1;
        int m_Resolution = 512;
        RTHandle m_Preview;

        internal StyleShelterPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Shelter");

            RenderPipelineManager.beginCameraRendering += Prepare;
        }

        internal static int stateId => s_StateId;

        internal static int depthId => s_DepthId;

        internal static int viewProjectionId => s_ViewProjId;

        internal static int heightId => s_HeightId;

        internal static int underId => s_UnderId;

        internal static int farDepthId => s_FarDepthId;

        internal static int farViewProjectionId => s_FarViewProjId;

        internal static int farHeightId => s_FarHeightId;

        internal bool TryGetMap(Camera camera, out Binding binding)
        {
            binding = default;

            if (camera == null || !m_Maps.TryGetValue(camera, out var map) || map.bound == null ||
                StyleCameraStates.clock - map.lastUsed > FreshSeconds)
                return false;

            binding.depth = map.bound;
            binding.under = map.boundUnder;
            binding.far = map.farValid ? map.far : null;
            binding.viewProjection = map.viewProjection;
            binding.height = map.height;
            binding.farViewProjection = map.farViewProjection;
            binding.farHeight = map.farHeight;
            return true;
        }

        internal static Vector4 State(bool far) => new Vector4(1f, far ? 1f : 0f, 0f, 0f);

        internal void Setup(ComputeShader compute, Shader underside, HBStyleWeatherQuality quality)
        {
            m_UndersideShader = underside;
            m_Resolution = (int)quality.current.shelterResolution;
            m_Compute = compute;
            m_PreviewKernel = Kernel(compute, "Preview");
            m_HeightsKernel = Kernel(compute, "Heights");
            m_DilateXKernel = Kernel(compute, "DilateX");
            m_DilateZKernel = Kernel(compute, "DilateZ");
        }

        static bool Needed() => StyleWeatherState.active || StyleDebug.view == StyleDebugView.WeatherShelter ||
                                StyleDebug.previewShelter;

        void Prepare(ScriptableRenderContext context, Camera camera)
        {
            m_Views.Remove(camera);

            if (!m_Wanted.Contains(camera))
                return;

            var position = camera.transform.position;
            var origin = Snap(position, Extent / m_Resolution);

            if (!CullBox(context, camera, origin, Extent, out var culling, out var view, out var projection))
                return;

            var sky = new View { culling = culling, view = view, projection = projection, origin = origin };

            m_Maps.TryGetValue(camera, out var map);

            if (map == null || !map.farValid || (position - map.farCentre).sqrMagnitude > FarStep * FarStep)
            {
                var farOrigin = Snap(position, FarExtent / FarResolution);

                sky.farDue = CullBox(context, camera, farOrigin, FarExtent, out sky.farCulling, out sky.farView,
                                     out sky.farProjection);
                sky.farOrigin = farOrigin;
                sky.farCentre = position;
            }

            m_Views[camera] = sky;
        }

        static Vector3 Snap(Vector3 position, float texel)
        {
            return new Vector3(Mathf.Floor(position.x / texel) * texel, position.y + Above,
                               Mathf.Floor(position.z / texel) * texel);
        }

        static bool CullBox(ScriptableRenderContext context, Camera camera, Vector3 origin, float extent,
                            out CullingResults culling, out Matrix4x4 view, out Matrix4x4 projection)
        {
            culling = default;
            view = s_FlipZ * Matrix4x4.TRS(origin, s_Down, Vector3.one).inverse;
            projection = Matrix4x4.Ortho(-extent * 0.5f, extent * 0.5f, -extent * 0.5f, extent * 0.5f, Near, Depth);

            if (!camera.TryGetCullingParameters(out var parameters))
                return false;

            var box = projection * view;
            var planes = GeometryUtility.CalculateFrustumPlanes(box);

            parameters.cullingMatrix = box;
            parameters.cullingPlaneCount = planes.Length;

            for (var i = 0; i < planes.Length; i++)
                parameters.SetCullingPlane(i, planes[i]);

            parameters.origin = origin;
            parameters.isOrthographic = true;
            parameters.cullingOptions = CullingOptions.None;
            parameters.shadowDistance = 0f;

            culling = context.Cull(ref parameters);
            return true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;

            m_Wanted.RemoveWhere(static stale => stale == null);

            if (Needed())
                m_Wanted.Add(camera);
            else
                m_Wanted.Remove(camera);

            if (!m_Views.TryGetValue(camera, out var sky))
                return;

            m_Views.Remove(camera);

            var resolution = m_Resolution;
            var skyProjection = GL.GetGPUProjectionMatrix(sky.projection, true);
            var viewProjection = skyProjection * sky.view;
            var height = HeightParams(sky.origin.y, resolution);

            var map = AcquireMap(camera, resolution);
            var depth = renderGraph.ImportTexture(map.depth);

            map.viewProjection = viewProjection;
            map.height = height;

            RecordTopDepth(renderGraph, frameData, "HB Style Shelter Depth", sky.culling, sky.view, skyProjection,
                           depth);

            var far = map.farValid ? renderGraph.ImportTexture(map.far) : TextureHandle.nullHandle;

            if (sky.farDue)
            {
                var farProjection = GL.GetGPUProjectionMatrix(sky.farProjection, true);

                far = renderGraph.ImportTexture(AcquireFar(map));

                RecordTopDepth(renderGraph, frameData, "HB Style Shelter Far", sky.farCulling, sky.farView,
                               farProjection, far);

                map.farViewProjection = farProjection * sky.farView;
                map.farHeight = HeightParams(sky.farOrigin.y, FarResolution);
                map.farCentre = sky.farCentre;
                map.farValid = true;
            }

            var bound = depth;
            var reach = Reach(StyleEnvironment.targetProfile.weather.roofMargin, resolution);

            var under = TextureHandle.nullHandle;

            map.bound = map.depth;
            map.boundUnder = null;

            if (m_UndersideShader != null)
            {
                map.boundUnder = AcquireUnder(map, resolution);
                under = renderGraph.ImportTexture(map.boundUnder);

                RecordUnderside(renderGraph, frameData, sky, skyProjection, under);
            }

            if (reach > 0 && m_DilateXKernel >= 0 && m_DilateZKernel >= 0 && under.IsValid())
            {
                var carry = renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
                {
                    format = GraphicsFormat.R32G32_SFloat,
                    enableRandomWrite = true,
                    name = "HB Style Shelter Overhangs"
                });

                map.bound = AcquireDilated(map, resolution);
                bound = renderGraph.ImportTexture(map.bound);

                RecordDilate(renderGraph, "HB Style Shelter Margin X", m_DilateXKernel, depth, under, s_UnderId,
                             carry, s_CarryTargetId, height, reach);
                RecordDilate(renderGraph, "HB Style Shelter Margin Z", m_DilateZKernel, depth, carry, s_CarryId,
                             bound, s_TargetId, height, reach);
            }

            using (var builder = renderGraph.AddUnsafePass<BindData>("HB Style Shelter Bind", out var data))
            {
                data.viewProjection = viewProjection;
                data.height = height;
                data.farViewProjection = map.farViewProjection;
                data.farHeight = map.farHeight;
                data.noUnderside = !under.IsValid();
                data.hasFar = far.IsValid();

                builder.UseTexture(bound, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(bound, s_DepthId);

                if (under.IsValid())
                {
                    builder.UseTexture(under, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(under, s_UnderId);
                }

                if (far.IsValid())
                {
                    builder.UseTexture(far, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(far, s_FarDepthId);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (BindData d, UnsafeGraphContext context) =>
                {
                    if (d.noUnderside)
                        context.cmd.SetGlobalTexture(s_UnderId, Texture2D.blackTexture);

                    context.cmd.SetGlobalMatrix(s_ViewProjId, d.viewProjection);
                    context.cmd.SetGlobalVector(s_HeightId, d.height);
                    context.cmd.SetGlobalMatrix(s_FarViewProjId, d.farViewProjection);
                    context.cmd.SetGlobalVector(s_FarHeightId, d.farHeight);
                    context.cmd.SetGlobalVector(s_StateId, State(d.hasFar));
                });
            }

            if (StyleDebug.previewShelter && m_PreviewKernel >= 0 && StyleDebug.IsPreviewCamera(camera))
            {
                var preview = PreviewTarget(resolution);
                var area = new Vector4(sky.origin.x, sky.origin.z, Extent, camera.transform.position.y);

                StyleDebug.shelterPreview = preview.rt;
                RecordKernel(renderGraph, "HB Style Shelter Preview", m_PreviewKernel, s_PreviewId, preview, bound,
                             TextureHandle.nullHandle, viewProjection, height, area, false);
            }

            if (m_HeightsKernel >= 0 && StyleWeatherBodies.wanted && camera == KeyCamera())
            {
                var area = new Vector4(sky.origin.x, sky.origin.z, Extent, resolution);

                RecordKernel(renderGraph, "HB Style Shelter Heights", m_HeightsKernel, s_HeightsId,
                             StyleWeatherBodies.Target(resolution), bound, under, viewProjection, height, area,
                             true);
                StyleWeatherBodies.Written(area, camera);
            }
        }

        Map AcquireMap(Camera camera, int resolution)
        {
            var now = StyleCameraStates.clock;

            m_Stale.Clear();

            foreach (var pair in m_Maps)
            {
                var unused = now - pair.Value.lastUsed > StyleCameraStates.PruneSeconds;

                if (pair.Key == null || (pair.Key != camera && unused))
                    m_Stale.Add(pair.Key);
            }

            foreach (var stale in m_Stale)
            {
                m_Maps[stale].depth?.Release();
                m_Maps[stale].dilated?.Release();
                m_Maps[stale].under?.Release();
                m_Maps[stale].far?.Release();
                m_Maps.Remove(stale);
            }

            if (!m_Maps.TryGetValue(camera, out var map))
            {
                map = new Map();
                m_Maps.Add(camera, map);
            }

            if (map.depth == null || map.depth.rt.width != resolution)
            {
                map.depth?.Release();

                var descriptor = new RenderTextureDescriptor(resolution, resolution, GraphicsFormat.None,
                                                             GraphicsFormat.D16_UNorm);

                map.depth = RTHandles.Alloc(descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                                            name: "_HB_ShelterDepth");
            }

            map.lastUsed = now;

            return map;
        }

        static RTHandle AcquireDilated(Map map, int resolution)
        {
            if (map.dilated == null || map.dilated.rt.width != resolution)
            {
                map.dilated?.Release();
                map.dilated = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R32_SFloat,
                                              filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                                              enableRandomWrite: true, name: "_HB_ShelterMargin");
            }

            return map.dilated;
        }

        static int Reach(float margin, int resolution)
        {
            return margin > 0f ? Mathf.Max(1, Mathf.RoundToInt(margin / (Extent / resolution))) : 0;
        }

        void RecordTopDepth(RenderGraph renderGraph, ContextContainer frameData, string name, CullingResults culling,
                            Matrix4x4 skyView, Matrix4x4 skyProjection, TextureHandle target)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<SkyData>(name, out var data, profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_DepthOnlyTag, frameData.Get<UniversalRenderingData>(),
                                                               cameraData, frameData.Get<UniversalLightData>(),
                                                               SortingCriteria.CommonOpaque);

            var tags = new NativeArray<ShaderTagId>(1, Allocator.Temp);
            tags[0] = ShaderTagId.none;

            var states = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
            states[0] = new RenderStateBlock(RenderStateMask.Raster)
            {
                rasterState = new RasterState(CullMode.Off)
            };

            data.list = renderGraph.CreateRendererList(new RendererListParams(
                culling, drawing, new FilteringSettings(RenderQueueRange.opaque, cameraData.camera.cullingMask))
            {
                tagValues = tags,
                stateBlocks = states,
                isPassTagName = false
            });
            data.skyView = skyView;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachmentDepth(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (SkyData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1f, 0u);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.skyView, d.skyProjection, false);
                context.cmd.SetGlobalFloat(s_PassId, 1f);
                context.cmd.DrawRendererList(d.list);
                context.cmd.SetGlobalFloat(s_PassId, 0f);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.cameraView, d.cameraProjection, true);
            });
        }

        static RTHandle AcquireFar(Map map)
        {
            if (map.far == null)
            {
                var descriptor = new RenderTextureDescriptor(FarResolution, FarResolution, GraphicsFormat.None,
                                                             GraphicsFormat.D16_UNorm);

                map.far = RTHandles.Alloc(descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                                          name: "_HB_ShelterFarDepth");
            }

            return map.far;
        }

        static RTHandle AcquireUnder(Map map, int resolution)
        {
            if (map.under == null || map.under.rt.width != resolution)
            {
                map.under?.Release();

                var descriptor = new RenderTextureDescriptor(resolution, resolution, GraphicsFormat.None,
                                                             GraphicsFormat.D16_UNorm);

                map.under = RTHandles.Alloc(descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                                            name: "_HB_ShelterUnder");
            }

            return map.under;
        }

        void RecordUnderside(RenderGraph renderGraph, ContextContainer frameData, View sky, Matrix4x4 skyProjection,
                             TextureHandle under)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<UnderData>("HB Style Shelter Underside", out var data,
                                                                          profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_DepthOnlyTag, frameData.Get<UniversalRenderingData>(),
                                                               cameraData, frameData.Get<UniversalLightData>(),
                                                               SortingCriteria.None);

            drawing.overrideShader = m_UndersideShader;
            drawing.overrideShaderPassIndex = 0;

            data.list = renderGraph.CreateRendererList(new RendererListParams(
                sky.culling, drawing, new FilteringSettings(RenderQueueRange.opaque, cameraData.camera.cullingMask)));
            data.skyView = sky.view;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachmentDepth(under, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (UnderData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 0f, 0u);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.skyView, d.skyProjection, false);
                context.cmd.DrawRendererList(d.list);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.cameraView, d.cameraProjection, true);
            });
        }

        void RecordDilate(RenderGraph renderGraph, string name, int kernel, TextureHandle source, TextureHandle extra,
                          int extraId, TextureHandle target, int targetId, Vector4 height, int reach)
        {
            using var builder = renderGraph.AddComputePass<DilateData>(name, out var data, profilingSampler);

            data.compute = m_Compute;
            data.kernel = kernel;
            data.source = source;
            data.extra = extra;
            data.extraId = extraId;
            data.target = target;
            data.targetId = targetId;
            data.height = height;
            data.reach = new Vector4(reach, 0f, 0f, 0f);
            data.groups = ((int)height.z + 7) / 8;

            builder.UseTexture(source, AccessFlags.Read);
            builder.UseTexture(extra, AccessFlags.Read);
            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (DilateData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_SourceId, d.source);
                cmd.SetComputeTextureParam(d.compute, d.kernel, d.extraId, d.extra);
                cmd.SetComputeTextureParam(d.compute, d.kernel, d.targetId, d.target);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_DilateId, d.reach);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        static Camera KeyCamera()
        {
            var camera = Camera.main;

            return camera != null ? camera : StyleCameraStates.lastGameCamera;
        }

        RTHandle PreviewTarget(int resolution)
        {
            if (m_Preview == null || m_Preview.rt.width != resolution)
            {
                m_Preview?.Release();
                m_Preview = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                                            filterMode: FilterMode.Point, enableRandomWrite: true,
                                            name: "HB Style Shelter Preview");
            }

            return m_Preview;
        }

        static int Kernel(ComputeShader compute, string name)
        {
            return compute != null && compute.HasKernel(name) ? compute.FindKernel(name) : -1;
        }

        void RecordKernel(RenderGraph renderGraph, string name, int kernel, int outputId, RTHandle output,
                          TextureHandle depth, TextureHandle under, Matrix4x4 viewProjection, Vector4 height,
                          Vector4 area, bool withUnder)
        {
            var resolution = (int)height.z;
            var preview = renderGraph.ImportTexture(output);

            using var builder = renderGraph.AddComputePass<PreviewData>(name, out var data, profilingSampler);

            data.compute = m_Compute;
            data.kernel = kernel;
            data.depth = depth;
            data.under = under;
            data.withUnder = withUnder;
            data.preview = preview;
            data.previewId = outputId;
            data.viewProjection = viewProjection;
            data.height = height;
            data.area = area;
            data.groups = (resolution + 7) / 8;

            builder.UseTexture(depth, AccessFlags.Read);

            if (under.IsValid())
                builder.UseTexture(under, AccessFlags.Read);

            builder.UseTexture(preview, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PreviewData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_DepthId, d.depth);

                if (d.withUnder)
                    cmd.SetComputeTextureParam(d.compute, d.kernel, s_UnderId, d.under.IsValid() ? d.under : d.depth);

                cmd.SetComputeTextureParam(d.compute, d.kernel, d.previewId, d.preview);
                cmd.SetComputeMatrixParam(d.compute, s_ViewProjId, d.viewProjection);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_PreviewAreaId, d.area);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        static Vector4 HeightParams(float originY, int resolution)
        {
            var span = Depth - Near;
            var reversed = SystemInfo.usesReversedZBuffer;

            return new Vector4(reversed ? span : -span, reversed ? originY - Depth : originY - Near, resolution, 0f);
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= Prepare;

            m_Views.Clear();
            m_Wanted.Clear();

            foreach (var map in m_Maps.Values)
            {
                map.depth?.Release();
                map.dilated?.Release();
                map.under?.Release();
                map.far?.Release();
            }

            m_Maps.Clear();
            m_Preview?.Release();
            m_Preview = null;
            StyleDebug.shelterPreview = null;
        }
    }
}
