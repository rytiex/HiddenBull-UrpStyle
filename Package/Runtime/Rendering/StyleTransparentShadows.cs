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
    sealed class StyleTransparentShadows : ScriptableRenderPass, IDisposable
    {
        const int MaxCascades = 4;
        const int MinTile = 256;
        const int MaxSingleTile = 2048;
        const int MaxCascadeTile = 1024;

        const float Above = 60f;
        const float Bias = 0.05f;
        const float SphereFit = 0.95f;
        const float EmptyRetry = 0.5f;

        static readonly ShaderTagId s_TintTag = new ShaderTagId("HiddenBullShadowTint");
        static readonly Matrix4x4 s_FlipZ = Matrix4x4.Scale(new Vector3(1f, 1f, -1f));

        static readonly int s_TintId = Shader.PropertyToID("_HB_ShadowTint");
        static readonly int s_DepthId = Shader.PropertyToID("_HB_ShadowTintDepth");
        static readonly int s_MatricesId = Shader.PropertyToID("_HB_ShadowTintMatrices");
        static readonly int s_SpheresId = Shader.PropertyToID("_HB_ShadowTintSpheres");
        static readonly int s_BiasId = Shader.PropertyToID("_HB_ShadowTintBias");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_ShadowTintParams");

        sealed class View
        {
            public CullingResults culling;
            public Light light;
            public int count;
            public int tile;
            public int width;
            public int height;
            public readonly Matrix4x4[] views = new Matrix4x4[MaxCascades];
            public readonly Matrix4x4[] projections = new Matrix4x4[MaxCascades];
            public readonly Matrix4x4[] samples = new Matrix4x4[MaxCascades];
            public readonly Vector4[] spheres = new Vector4[MaxCascades];
            public Vector4 bias = Vector4.zero;
            public int frame;
            public bool ready;
            public float retry;
        }

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();
        readonly List<Camera> m_Stale = new List<Camera>();
        readonly float[] m_Splits = new float[MaxCascades + 1];

        public StyleTransparentShadows()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HiddenBull Style Transparent Shadows");

            RenderPipelineManager.beginCameraRendering += Prepare;
        }

        static Light MainLight()
        {
            var sun = RenderSettings.sun;

            if (sun == null && HBStyleSun.active != null)
                sun = HBStyleSun.active.GetComponent<Light>();

            if (sun == null || !sun.isActiveAndEnabled || sun.type != LightType.Directional ||
                sun.shadows == LightShadows.None)
                return null;

            return sun;
        }

        int Splits(UniversalRenderPipelineAsset asset, float near, float distance)
        {
            var count = Mathf.Clamp(asset.shadowCascadeCount, 1, MaxCascades);

            m_Splits[0] = near;
            m_Splits[count] = distance;

            if (count == 2)
                m_Splits[1] = asset.cascade2Split * distance;
            else if (count == 3)
            {
                m_Splits[1] = asset.cascade3Split.x * distance;
                m_Splits[2] = asset.cascade3Split.y * distance;
            }
            else if (count == 4)
            {
                m_Splits[1] = asset.cascade4Split.x * distance;
                m_Splits[2] = asset.cascade4Split.y * distance;
                m_Splits[3] = asset.cascade4Split.z * distance;
            }

            for (var i = 1; i <= count; i++)
                m_Splits[i] = Mathf.Max(m_Splits[i], m_Splits[i - 1] + 0.01f);

            return count;
        }

        static float Lateral(Camera camera, float depth)
        {
            var diagonal = Mathf.Sqrt(1f + camera.aspect * camera.aspect);

            return camera.orthographic
                ? camera.orthographicSize * diagonal
                : depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * diagonal;
        }

        static void Sphere(Camera camera, float near, float far, out Vector3 centre, out float radius)
        {
            var nearLateral = Lateral(camera, near);
            var farLateral = Lateral(camera, far);

            var along = (far * far + farLateral * farLateral - near * near - nearLateral * nearLateral)
                      / (2f * (far - near));
            along = Mathf.Clamp(along, near, far);

            radius = Mathf.Max(
                Mathf.Sqrt((along - near) * (along - near) + nearLateral * nearLateral),
                Mathf.Sqrt((far - along) * (far - along) + farLateral * farLateral));

            centre = camera.transform.position + camera.transform.forward * along;
        }

        static Matrix4x4 SampleTransform(Matrix4x4 projection, Matrix4x4 view)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                projection.m20 = -projection.m20;
                projection.m21 = -projection.m21;
                projection.m22 = -projection.m22;
                projection.m23 = -projection.m23;
            }

            var scaleBias = Matrix4x4.identity;
            scaleBias.m00 = 0.5f;
            scaleBias.m11 = 0.5f;
            scaleBias.m22 = 0.5f;
            scaleBias.m03 = 0.5f;
            scaleBias.m13 = 0.5f;
            scaleBias.m23 = 0.5f;

            return scaleBias * projection * view;
        }

        static Vector2Int TileOffset(int cascade, int tile) => new Vector2Int(cascade % 2 * tile, cascade / 2 * tile);

        void Prepare(ScriptableRenderContext context, Camera camera)
        {
            if (m_Views.TryGetValue(camera, out var previous))
                previous.ready = false;

            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;

            var light = MainLight();
            var asset = UniversalRenderPipeline.asset;

            if (light == null || asset == null || !asset.supportsMainLightShadows)
                return;

            var distance = Mathf.Min(asset.shadowDistance, camera.farClipPlane);
            var near = Mathf.Max(camera.nearClipPlane, 0.01f);

            if (distance <= near || !camera.TryGetCullingParameters(out var parameters))
                return;

            if (!m_Views.TryGetValue(camera, out var shadowView))
            {
                Prune();

                shadowView = new View();
                m_Views[camera] = shadowView;
            }

            if (Time.realtimeSinceStartup < shadowView.retry)
                return;

            var count = Splits(asset, near, distance);
            var resolution = asset.mainLightShadowmapResolution;

            shadowView.count = count;
            shadowView.tile = count == 1
                ? Mathf.Clamp(resolution, MinTile, MaxSingleTile)
                : Mathf.Clamp(resolution / 2, MinTile, MaxCascadeTile);
            shadowView.width = count == 1 ? shadowView.tile : shadowView.tile * 2;
            shadowView.height = count <= 2 ? shadowView.tile : shadowView.tile * 2;

            var rotation = light.transform.rotation;
            var inverse = Quaternion.Inverse(rotation);

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var i = 0; i < MaxCascades; i++)
            {
                if (i >= count)
                {
                    shadowView.spheres[i] = Vector4.zero;
                    shadowView.samples[i] = Matrix4x4.identity;
                    continue;
                }

                Sphere(camera, m_Splits[i], m_Splits[i + 1], out var centre, out var radius);

                var texel = 2f * radius / shadowView.tile;
                var local = inverse * centre;
                local.x = Mathf.Floor(local.x / texel) * texel;
                local.y = Mathf.Floor(local.y / texel) * texel;

                var depthRange = 2f * radius + Above;
                var origin = rotation * (local - Vector3.forward * (radius + Above));
                var view = s_FlipZ * Matrix4x4.TRS(origin, rotation, Vector3.one).inverse;
                var projection = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0f, depthRange);

                var offset = TileOffset(i, shadowView.tile);
                var slice = Matrix4x4.identity;
                slice.m00 = (float)shadowView.tile / shadowView.width;
                slice.m11 = (float)shadowView.tile / shadowView.height;
                slice.m03 = (float)offset.x / shadowView.width;
                slice.m13 = (float)offset.y / shadowView.height;

                var fit = radius * SphereFit;
                var snapped = rotation * local;

                shadowView.views[i] = view;
                shadowView.projections[i] = projection;
                shadowView.samples[i] = slice * SampleTransform(projection, view);
                shadowView.spheres[i] = new Vector4(snapped.x, snapped.y, snapped.z, fit * fit);
                shadowView.bias[i] = Bias / depthRange;

                min = Vector3.Min(min, local - new Vector3(radius, radius, radius + Above));
                max = Vector3.Max(max, local + new Vector3(radius, radius, radius));
            }

            var extent = (max - min) * 0.5f;
            var middle = (max + min) * 0.5f;
            var cullOrigin = rotation * new Vector3(middle.x, middle.y, min.z);
            var cullView = s_FlipZ * Matrix4x4.TRS(cullOrigin, rotation, Vector3.one).inverse;
            var cullProjection = Matrix4x4.Ortho(-extent.x, extent.x, -extent.y, extent.y, 0f, extent.z * 2f);
            var cullMatrix = cullProjection * cullView;
            var planes = GeometryUtility.CalculateFrustumPlanes(cullMatrix);

            parameters.cullingMatrix = cullMatrix;
            parameters.cullingPlaneCount = planes.Length;

            for (var i = 0; i < planes.Length; i++)
                parameters.SetCullingPlane(i, planes[i]);

            parameters.origin = cullOrigin;
            parameters.isOrthographic = true;
            parameters.cullingOptions = CullingOptions.None;
            parameters.shadowDistance = 0f;

            shadowView.culling = context.Cull(ref parameters);

            var probeList = new RendererListParams(shadowView.culling,
                new DrawingSettings(s_TintTag, new SortingSettings(camera)),
                new FilteringSettings(RenderQueueRange.transparent, camera.cullingMask));
            var probe = context.CreateRendererList(ref probeList);

            if (context.QueryRendererListStatus(probe) == RendererListStatus.kRendererListEmpty)
            {
                shadowView.retry = Time.realtimeSinceStartup + EmptyRetry;
                return;
            }

            shadowView.light = light;
            shadowView.frame = Time.frameCount;
            shadowView.ready = true;
        }

        void Prune()
        {
            if (m_Views.Count < 8)
                return;

            m_Stale.Clear();

            foreach (var pair in m_Views)
            {
                if (pair.Key == null || Time.frameCount - pair.Value.frame > 1)
                    m_Stale.Add(pair.Key);
            }

            foreach (var camera in m_Stale)
                m_Views.Remove(camera);
        }

        class DrawData
        {
            public readonly RendererListHandle[] depthLists = new RendererListHandle[MaxCascades];
            public readonly RendererListHandle[] tintLists = new RendererListHandle[MaxCascades];
            public View view;
            public Matrix4x4 cameraView;
            public Matrix4x4 cameraProjection;
        }

        class OffData
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var camera = cameraData.camera;

            var mainLight = lightData.mainLightIndex >= 0
                ? lightData.visibleLights[lightData.mainLightIndex].light
                : null;

            var ready = m_Views.TryGetValue(camera, out var shadowView) && shadowView.ready;

            if (ready)
                shadowView.ready = false;

            if (!ready || mainLight == null || mainLight != shadowView.light ||
                !frameData.Get<UniversalShadowData>().supportsMainLightShadows)
            {
                using var off = renderGraph.AddUnsafePass<OffData>("HiddenBull Transparent Shadows Off", out _);

                off.AllowPassCulling(false);
                off.AllowGlobalStateModification(true);
                off.SetRenderFunc(static (OffData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ParamsId, Vector4.zero);
                });

                return;
            }

            var resourceData = frameData.Get<UniversalResourceData>();

            var tint = renderGraph.CreateTexture(new TextureDesc(shadowView.width, shadowView.height)
            {
                format = GraphicsFormat.R8G8B8A8_UNorm,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                clearColor = Color.white,
                name = "_HB_ShadowTint"
            });

            var depth = renderGraph.CreateTexture(new TextureDesc(shadowView.width, shadowView.height)
            {
                format = GraphicsFormat.D16_UNorm,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                name = "_HB_ShadowTintDepth"
            });

            using var builder = renderGraph.AddRasterRenderPass<DrawData>(
                "HiddenBull Transparent Shadows", out var passData, profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(
                s_TintTag, frameData.Get<UniversalRenderingData>(), cameraData, lightData, SortingCriteria.None);

            var filtering = new FilteringSettings(RenderQueueRange.transparent, camera.cullingMask);

            var tags = new NativeArray<ShaderTagId>(1, Allocator.Temp);
            tags[0] = ShaderTagId.none;

            var states = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
            states[0] = new RenderStateBlock(RenderStateMask.Depth | RenderStateMask.Blend)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
                blendState = new BlendState { blendState0 = new RenderTargetBlendState((ColorWriteMask)0) }
            };

            for (var i = 0; i < shadowView.count; i++)
            {
                passData.depthLists[i] = renderGraph.CreateRendererList(
                    new RendererListParams(shadowView.culling, drawing, filtering)
                    {
                        tagValues = tags,
                        stateBlocks = states,
                        isPassTagName = false
                    });
                passData.tintLists[i] = renderGraph.CreateRendererList(
                    new RendererListParams(shadowView.culling, drawing, filtering));

                builder.UseRendererList(passData.depthLists[i]);
                builder.UseRendererList(passData.tintLists[i]);
            }

            passData.view = shadowView;
            passData.cameraView = cameraData.GetViewMatrix();
            passData.cameraProjection = GL.GetGPUProjectionMatrix(
                cameraData.GetProjectionMatrix(), !resourceData.isActiveTargetBackBuffer);

            builder.SetRenderAttachment(tint, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(tint, s_TintId);
            builder.SetGlobalTextureAfterPass(depth, s_DepthId);

            builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
            {
                var cmd = context.cmd;
                var view = data.view;

                for (var i = 0; i < view.count; i++)
                {
                    var offset = TileOffset(i, view.tile);

                    cmd.SetViewport(new Rect(offset.x, offset.y, view.tile, view.tile));
                    RenderingUtils.SetViewAndProjectionMatrices(cmd, view.views[i],
                        GL.GetGPUProjectionMatrix(view.projections[i], true), false);

                    cmd.DrawRendererList(data.depthLists[i]);
                    cmd.DrawRendererList(data.tintLists[i]);
                }

                RenderingUtils.SetViewAndProjectionMatrices(cmd, data.cameraView, data.cameraProjection, true);

                cmd.SetGlobalMatrixArray(s_MatricesId, view.samples);
                cmd.SetGlobalVectorArray(s_SpheresId, view.spheres);
                cmd.SetGlobalVector(s_BiasId, view.bias);
                cmd.SetGlobalVector(s_ParamsId, new Vector4(view.count, 0f, 0f, 0f));
            });
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= Prepare;
            m_Views.Clear();
        }
    }
}
