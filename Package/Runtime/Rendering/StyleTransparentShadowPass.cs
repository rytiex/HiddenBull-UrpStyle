using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTransparentShadowPass : ScriptableRenderPass, IDisposable
    {
        const int MaxCascades = 4;

        static readonly int s_TintMapId = Shader.PropertyToID("_HB_ShadowTintMap");
        static readonly int s_TintActiveId = Shader.PropertyToID("_HB_ShadowTintActive");
        static readonly int s_TintTransformsId = Shader.PropertyToID("_HB_ShadowTintWorldToShadow");
        static readonly int s_TintCasterId = Shader.PropertyToID("_HB_ShadowTint");
        static readonly int s_ShadowBiasId = Shader.PropertyToID("_ShadowBias");
        static readonly int s_LightDirectionId = Shader.PropertyToID("_LightDirection");
        static readonly int s_LightPositionId = Shader.PropertyToID("_LightPosition");

        class PassData
        {
            internal readonly RendererListHandle[] lists = new RendererListHandle[MaxCascades];
            internal readonly ShadowSliceData[] slices = new ShadowSliceData[MaxCascades];
            internal readonly Vector4[] biases = new Vector4[MaxCascades];
            internal readonly Matrix4x4[] transforms = new Matrix4x4[MaxCascades + 1];
            internal int count;
            internal Vector4 direction;
            internal Vector4 position;
            internal Matrix4x4 view;
            internal Matrix4x4 projection;
        }

        readonly ShadowSliceData[] m_Slices = new ShadowSliceData[MaxCascades];

        HBStyleTransparencyLevel m_Level;

        internal TextureHandle tint { get; private set; }

        internal StyleTransparentShadowPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Transparent Shadows");
        }

        internal void Setup(HBStyleTransparencyQuality quality)
        {
            m_Level = quality.current;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Shader.SetGlobalFloat(s_TintActiveId, 0f);
            Shader.SetGlobalFloat(s_TintCasterId, 0f);
            tint = TextureHandle.nullHandle;

            if (m_Level.glassShadowResolution == HBStyleGlassShadowResolution.Off)
                return;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var shadowData = frameData.Get<UniversalShadowData>();

            var index = lightData.mainLightIndex;
            var camera = cameraData.camera;

            if (index < 0 || !shadowData.supportsMainLightShadows)
                return;

            if (camera.targetTexture != null && camera.targetTexture.format == RenderTextureFormat.Depth)
                return;

#if UNITY_EDITOR
            if (CoreUtils.IsSceneLightingDisabled(camera))
                return;
#endif

            var visibleLight = lightData.visibleLights[index];
            var light = visibleLight.light;

            if (light == null || light.shadows == LightShadows.None || visibleLight.lightType != LightType.Directional)
                return;

            var cullResults = renderingData.cullResults;

            if (!cullResults.GetShadowCasterBounds(index, out _))
                return;

            var count = Mathf.Clamp(shadowData.mainLightShadowCascadesCount, 1, MaxCascades);
            var shift = m_Level.glassShadowResolution == HBStyleGlassShadowResolution.Half ? 1 : 2;
            var fullHeight = shadowData.mainLightShadowmapHeight;
            var width = shadowData.mainLightShadowmapWidth >> shift;
            var height = (count == 2 ? fullHeight >> 1 : fullHeight) >> shift;
            var resolution = ShadowUtils.GetMaxTileResolutionInAtlas(shadowData.mainLightShadowmapWidth, fullHeight,
                                                                     count) >> shift;

            if (width < 1 || height < 1 || resolution < 1)
                return;

            for (var i = 0; i < count; i++)
            {
                if (!ShadowUtils.ExtractDirectionalLightMatrix(ref cullResults, shadowData, index, i, width, height,
                                                               resolution, light.shadowNearPlane, out _,
                                                               out m_Slices[i]))
                    return;
            }

            var descriptor = new TextureDesc(width, height)
            {
                name = "_HB_ShadowTintMap",
                format = GraphicsFormat.R16G16B16A16_SFloat,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                clearColor = Color.clear
            };

            var tintMap = renderGraph.CreateTexture(descriptor);

            using var builder = renderGraph.AddRasterRenderPass<PassData>("HB Style Transparent Shadows", out var data,
                                                                         profilingSampler);

            var settings = new ShadowDrawingSettings(cullResults, index)
            {
                useRenderingLayerMaskTest = UniversalRenderPipeline.asset.useRenderingLayers
            };

            data.count = count;
            data.direction = -visibleLight.localToWorldMatrix.GetColumn(2);
            data.position = visibleLight.localToWorldMatrix.GetColumn(3);
            data.view = cameraData.GetViewMatrix();
            data.projection = cameraData.GetProjectionMatrix();

            for (var i = 0; i < count; i++)
            {
                var slice = m_Slices[i];

                settings.splitIndex = i;
                data.lists[i] = renderGraph.CreateShadowRendererList(ref settings);
                data.biases[i] = ShadowUtils.GetShadowBias(ref visibleLight, index, shadowData, slice.projectionMatrix,
                                                           slice.resolution);
                data.slices[i] = slice;
                data.transforms[i] = slice.shadowTransform;

                builder.UseRendererList(data.lists[i]);
            }

            var outside = Matrix4x4.zero;
            outside.m23 = SystemInfo.usesReversedZBuffer ? 1f : 0f;
            outside.m33 = 1f;

            for (var i = count; i <= MaxCascades; i++)
                data.transforms[i] = outside;

            builder.SetRenderAttachment(tintMap, 0);
            builder.SetGlobalTextureAfterPass(tintMap, s_TintMapId);
            tint = tintMap;
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetGlobalFloat(s_TintCasterId, 1f);
                cmd.SetGlobalVector(s_LightDirectionId, d.direction);
                cmd.SetGlobalVector(s_LightPositionId, d.position);

                for (var i = 0; i < d.count; i++)
                {
                    var slice = d.slices[i];

                    cmd.SetGlobalVector(s_ShadowBiasId, d.biases[i]);
                    cmd.SetViewport(new Rect(slice.offsetX, slice.offsetY, slice.resolution, slice.resolution));
                    cmd.SetViewProjectionMatrices(slice.viewMatrix, slice.projectionMatrix);
                    cmd.DrawRendererList(d.lists[i]);
                }

                cmd.DisableScissorRect();
                cmd.SetViewProjectionMatrices(d.view, d.projection);
                cmd.SetGlobalMatrixArray(s_TintTransformsId, d.transforms);
                cmd.SetGlobalFloat(s_TintCasterId, 0f);
                cmd.SetGlobalFloat(s_TintActiveId, 1f);
            });
        }

        public void Dispose()
        {
            Shader.SetGlobalFloat(s_TintActiveId, 0f);
            Shader.SetGlobalFloat(s_TintCasterId, 0f);
        }
    }
}
