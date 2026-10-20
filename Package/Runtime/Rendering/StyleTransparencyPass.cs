using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTransparencyPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/Style Transparency";

        const float FullPrecisionBias = 5e-7f;
        const float HalfPrecisionBias = 6e-4f;

        static readonly ShaderTagId s_MomentsTag = new ShaderTagId("HiddenBullOITMoments");
        static readonly ShaderTagId s_ColorTag = new ShaderTagId("HiddenBullOITColor");
        static readonly ShaderTagId s_LiquidTag = new ShaderTagId("HiddenBullLiquid");
        static readonly ShaderTagId s_PrimeTag = new ShaderTagId("HiddenBullLiquidPrime");
        static readonly ShaderTagId s_RefractionTag = new ShaderTagId("HiddenBullOITRefraction");
        static readonly int s_B0Id = Shader.PropertyToID("_HB_OITB0");
        static readonly int s_MomentsId = Shader.PropertyToID("_HB_OITMoments");
        static readonly int s_BehindMomentsId = Shader.PropertyToID("_HB_OITBehindMoments");
        static readonly int s_InfoId = Shader.PropertyToID("_HB_OITInfo");
        static readonly int s_FrontId = Shader.PropertyToID("_HB_OITFront");
        static readonly int s_BehindId = Shader.PropertyToID("_HB_OITBehind");
        static readonly int s_OffsetId = Shader.PropertyToID("_HB_OITOffset");
        static readonly int s_TintId = Shader.PropertyToID("_HB_OITTint");
        static readonly int s_BehindTintId = Shader.PropertyToID("_HB_OITBehindTint");
        static readonly int s_BackgroundId = Shader.PropertyToID("_HB_OITBackground");
        static readonly int s_SceneDepthId = Shader.PropertyToID("_HB_OITSceneDepth");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_OITParams");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_OITSize");
        static readonly int s_BackdropId = Shader.PropertyToID("_HB_LiquidBackdrop");

        Material m_Material;
        StylePrecipitationPass m_Precipitation;

        public StyleTransparencyPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            profilingSampler = new ProfilingSampler("HiddenBull Style Transparency");
            requiresIntermediateTexture = true;

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(Shader shader, StylePrecipitationPass precipitation)
        {
            m_Precipitation = precipitation;

            if (shader == null)
            {
                Dispose();
                return;
            }

            if (m_Material == null || m_Material.shader != shader)
            {
                Dispose();

                m_Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        public bool isReady => m_Material != null;

        class DrawData
        {
            public RendererListHandle list;
            public Vector4 parameters;
        }

        class CompositeData
        {
            public Material material;
            public TextureHandle background;
            public TextureHandle depth;
            public Vector4 size;
        }

        static TextureHandle Create(RenderGraph renderGraph, RenderTextureDescriptor descriptor,
                                    GraphicsFormat format, string name)
        {
            descriptor.graphicsFormat = format;

            return UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, name, true, FilterMode.Point);
        }

        static TextureHandle CreateTint(RenderGraph renderGraph, RenderTextureDescriptor descriptor, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(descriptor)
            {
                format = GraphicsFormat.R8G8B8A8_UNorm,
                filterMode = FilterMode.Point,
                clearBuffer = true,
                clearColor = Color.white,
                name = name
            });
        }

        class LiquidData
        {
            public RendererListHandle list;
            public TextureHandle backdrop;
        }

        class PrimeData
        {
            public RendererListHandle list;
        }

        class BackdropData
        {
            public TextureHandle source;
        }

        void RecordLiquid(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData,
                          UniversalRenderingData renderingData, UniversalLightData lightData)
        {
            var colour = resourceData.activeColorTexture;
            var descriptor = renderGraph.GetTextureDesc(colour);

            descriptor.name = "_HB_LiquidBackdrop";
            descriptor.clearBuffer = false;
            descriptor.msaaSamples = MSAASamples.None;

            var backdrop = renderGraph.CreateTexture(descriptor);

            using (var builder = renderGraph.AddRasterRenderPass<BackdropData>(
                       "HiddenBull Liquid Backdrop", out var passData, profilingSampler))
            {
                passData.source = colour;

                builder.UseTexture(colour, AccessFlags.Read);
                builder.SetRenderAttachment(backdrop, 0, AccessFlags.WriteAll);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (BackdropData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), 0f, false);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PrimeData>(
                       "HiddenBull Liquid Prime", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_PrimeTag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);

                passData.list = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing,
                                           new FilteringSettings(RenderQueueRange.transparent)));

                builder.UseRendererList(passData.list);
                builder.UseAllGlobalTextures(true);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PrimeData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.list);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<LiquidData>(
                       "HiddenBull Liquid", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_LiquidTag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);

                passData.list = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing,
                                           new FilteringSettings(RenderQueueRange.transparent)));
                passData.backdrop = backdrop;

                builder.UseRendererList(passData.list);
                builder.UseAllGlobalTextures(true);
                builder.UseTexture(backdrop, AccessFlags.Read);
                builder.SetRenderAttachment(colour, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (LiquidData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(s_BackdropId, data.backdrop);
                    context.cmd.DrawRendererList(data.list);
                });
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            if (HBStyleLiquid.any && cameraData.cameraType != CameraType.Preview)
                RecordLiquid(renderGraph, resourceData, cameraData, renderingData, lightData);

            if (m_Material == null)
                return;

            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthStencilFormat = GraphicsFormat.None;
            descriptor.depthBufferBits = 0;

            var fullPrecision = SystemInfo.IsFormatSupported(
                GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormatUsage.Blend);

            var momentsFormat = fullPrecision ? GraphicsFormat.R32G32B32A32_SFloat : GraphicsFormat.R16G16B16A16_SFloat;

            var b0 = Create(renderGraph, descriptor,
                fullPrecision ? GraphicsFormat.R32G32_SFloat : GraphicsFormat.R16G16_SFloat, "_HB_OITB0");
            var moments = Create(renderGraph, descriptor, momentsFormat, "_HB_OITMoments");
            var behindMoments = Create(renderGraph, descriptor, momentsFormat, "_HB_OITBehindMoments");
            var info = Create(renderGraph, descriptor, GraphicsFormat.R16G16B16A16_SFloat, "_HB_OITInfo");
            var front = Create(renderGraph, descriptor, GraphicsFormat.R16G16B16A16_SFloat, "_HB_OITFront");
            var behind = Create(renderGraph, descriptor, GraphicsFormat.R16G16B16A16_SFloat, "_HB_OITBehind");
            var offset = Create(renderGraph, descriptor, GraphicsFormat.R16G16B16A16_SFloat, "_HB_OITOffset");
            var tint = CreateTint(renderGraph, descriptor, "_HB_OITTint");
            var behindTint = CreateTint(renderGraph, descriptor, "_HB_OITBehindTint");

            var camera = cameraData.camera;
            var logNear = Mathf.Log(Mathf.Max(camera.nearClipPlane, 1e-4f));
            var logFar = Mathf.Log(Mathf.Max(camera.farClipPlane, camera.nearClipPlane + 1e-3f));

            var parameters = new Vector4(
                fullPrecision ? FullPrecisionBias : HalfPrecisionBias,
                logNear,
                1f / Mathf.Max(logFar - logNear, 1e-4f),
                0f);

            var filtering = new FilteringSettings(RenderQueueRange.transparent);

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>(
                       "HiddenBull OIT Refraction", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_RefractionTag, renderingData, cameraData, lightData, SortingCriteria.None);

                passData.list = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing, filtering));
                passData.parameters = parameters;

                builder.UseRendererList(passData.list);
                builder.UseAllGlobalTextures(true);
                builder.SetRenderAttachment(info, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(info, s_InfoId);

                builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ParamsId, data.parameters);
                    context.cmd.DrawRendererList(data.list);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>(
                       "HiddenBull OIT Moments", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_MomentsTag, renderingData, cameraData, lightData, SortingCriteria.None);

                passData.list = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing, filtering));

                builder.UseRendererList(passData.list);
                builder.UseAllGlobalTextures(true);
                builder.UseTexture(info, AccessFlags.Read);
                builder.SetRenderAttachment(b0, 0, AccessFlags.Write);
                builder.SetRenderAttachment(moments, 1, AccessFlags.Write);
                builder.SetRenderAttachment(behindMoments, 2, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(b0, s_B0Id);
                builder.SetGlobalTextureAfterPass(moments, s_MomentsId);
                builder.SetGlobalTextureAfterPass(behindMoments, s_BehindMomentsId);

                builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.list);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>(
                       "HiddenBull OIT Color", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_ColorTag, renderingData, cameraData, lightData, SortingCriteria.None);

                passData.list = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing, filtering));

                builder.UseRendererList(passData.list);
                builder.UseAllGlobalTextures(true);
                builder.UseTexture(b0, AccessFlags.Read);
                builder.UseTexture(moments, AccessFlags.Read);
                builder.UseTexture(behindMoments, AccessFlags.Read);
                builder.UseTexture(info, AccessFlags.Read);
                builder.SetRenderAttachment(front, 0, AccessFlags.Write);
                builder.SetRenderAttachment(behind, 1, AccessFlags.Write);
                builder.SetRenderAttachment(offset, 2, AccessFlags.Write);
                builder.SetRenderAttachment(tint, 3, AccessFlags.Write);
                builder.SetRenderAttachment(behindTint, 4, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(front, s_FrontId);
                builder.SetGlobalTextureAfterPass(behind, s_BehindId);
                builder.SetGlobalTextureAfterPass(offset, s_OffsetId);
                builder.SetGlobalTextureAfterPass(tint, s_TintId);
                builder.SetGlobalTextureAfterPass(behindTint, s_BehindTintId);

                builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.list);
                });
            }

            m_Precipitation?.RecordBehind(renderGraph, frameData, resourceData.activeColorTexture, b0, moments,
                                          behindMoments, info);

            var background = resourceData.activeColorTexture;

            var colorDescriptor = renderGraph.GetTextureDesc(background);
            colorDescriptor.name = "_HB_OITColor";
            colorDescriptor.clearBuffer = false;

            var composited = renderGraph.CreateTexture(colorDescriptor);

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>(
                       "HiddenBull OIT Composite", out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.background = background;
                passData.depth = resourceData.cameraDepthTexture;
                passData.size = new Vector4(descriptor.width, descriptor.height,
                                            1f / descriptor.width, 1f / descriptor.height);

                builder.UseTexture(b0, AccessFlags.Read);
                builder.UseTexture(info, AccessFlags.Read);
                builder.UseTexture(front, AccessFlags.Read);
                builder.UseTexture(behind, AccessFlags.Read);
                builder.UseTexture(offset, AccessFlags.Read);
                builder.UseTexture(tint, AccessFlags.Read);
                builder.UseTexture(behindTint, AccessFlags.Read);
                builder.UseTexture(background, AccessFlags.Read);

                if (passData.depth.IsValid())
                    builder.UseTexture(passData.depth, AccessFlags.Read);

                builder.SetRenderAttachment(composited, 0, AccessFlags.WriteAll);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(s_BackgroundId, data.background);

                    if (data.depth.IsValid())
                        context.cmd.SetGlobalTexture(s_SceneDepthId, data.depth);
                    context.cmd.SetGlobalVector(s_SizeId, data.size);

                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, 0);
                });
            }

            resourceData.cameraColor = composited;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);

            m_Material = null;
        }
    }
}
