using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTransparencyPass : ScriptableRenderPass, IDisposable
    {
        const float WholeStage = 0f;
        const float BehindStage = 1f;
        const float FrontStage = 2f;

        static readonly ShaderTagId s_NearestTag = new ShaderTagId("HBStyleTransparentNearest");
        static readonly ShaderTagId s_RefractiveTag = new ShaderTagId("HBStyleTransparentRefractive");
        static readonly List<ShaderTagId> s_NearestTags = new List<ShaderTagId> { s_NearestTag, s_RefractiveTag };
        static readonly List<ShaderTagId> s_ForwardTags = new List<ShaderTagId>
        {
            new ShaderTagId("HBStyleTransparentTint"),
            new ShaderTagId("HBStyleTransparent")
        };

        static readonly int s_NearestId = Shader.PropertyToID("_HB_TransparentNearest");
        static readonly int s_BackgroundId = Shader.PropertyToID("_HB_RefractionBackground");
        static readonly int s_StageId = Shader.PropertyToID("_HB_TransparentStage");

        static readonly string[] s_ForwardNames =
        {
            "HB Style Transparent", "HB Style Transparent Behind", "HB Style Transparent Front"
        };

        class DrawData
        {
            internal RendererListHandle list;
            internal float stage;
        }

        class CopyData
        {
            internal TextureHandle source;
            internal TextureHandle background;
        }

        readonly List<RendererList> m_Probes = new List<RendererList>(2);

        StylePrecipitationPass m_Precipitation;
        ScriptableRenderContext m_Context;
        bool m_HasContext;

        internal StyleTransparencyPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            profilingSampler = new ProfilingSampler("HB Style Transparency");
            requiresIntermediateTexture = true;
            RenderPipelineManager.beginContextRendering += Capture;
        }

        void Capture(ScriptableRenderContext context, List<Camera> cameras)
        {
            m_Context = context;
            m_HasContext = true;
        }

        internal void Setup(StylePrecipitationPass precipitation)
        {
            m_Precipitation = precipitation;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            var colour = resourceData.activeColorTexture;
            var depth = resourceData.activeDepthTexture;

            if (!colour.IsValid() || !depth.IsValid())
                return;

            var raining = m_Precipitation != null && m_Precipitation.Prepare(cameraData.camera);

            Probe(renderingData, cameraData, lightData, out var glass, out var refractive);

            var splitRain = raining && glass;

            if (!splitRain && !refractive)
            {
                if (raining)
                    m_Precipitation.Draw(renderGraph, colour, depth, StylePrecipitationPass.PlainStage, default);

                RecordForward(renderGraph, colour, depth, WholeStage, default, default, resourceData, renderingData,
                              cameraData, lightData);
                return;
            }

            var nearest = RecordNearest(renderGraph, colour, depth, renderingData, cameraData, lightData);

            if (splitRain)
                m_Precipitation.Draw(renderGraph, colour, depth, StylePrecipitationPass.UnderStage, nearest);

            if (refractive)
            {
                RecordForward(renderGraph, colour, depth, BehindStage, nearest, default, resourceData, renderingData,
                              cameraData, lightData);

                var background = RecordBackground(renderGraph, colour);

                RecordForward(renderGraph, colour, depth, FrontStage, nearest, background, resourceData,
                              renderingData, cameraData, lightData);
            }
            else
            {
                RecordForward(renderGraph, colour, depth, WholeStage, nearest, default, resourceData, renderingData,
                              cameraData, lightData);
            }

            if (splitRain)
                m_Precipitation.Draw(renderGraph, colour, depth, StylePrecipitationPass.OverStage, nearest);
        }

        TextureHandle RecordNearest(RenderGraph renderGraph, TextureHandle colour, TextureHandle depth,
                                    UniversalRenderingData renderingData, UniversalCameraData cameraData,
                                    UniversalLightData lightData)
        {
            var descriptor = renderGraph.GetTextureDesc(colour);
            descriptor.name = "_HB_TransparentNearest";
            descriptor.format = GraphicsFormat.R16G16_SFloat;
            descriptor.filterMode = FilterMode.Point;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.clearBuffer = true;
            descriptor.clearColor = Color.clear;
            descriptor.enableRandomWrite = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.bindTextureMS = false;

            var nearest = renderGraph.CreateTexture(descriptor);

            using var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Transparent Nearest", out var data,
                                                                         profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_NearestTags, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);

            data.list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                              Filtering(cameraData)));

            builder.UseRendererList(data.list);
            builder.SetRenderAttachment(nearest, 0);
            builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
            builder.SetGlobalTextureAfterPass(nearest, s_NearestId);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
                context.cmd.DrawRendererList(d.list));

            return nearest;
        }

        TextureHandle RecordBackground(RenderGraph renderGraph, TextureHandle colour)
        {
            var descriptor = renderGraph.GetTextureDesc(colour);
            descriptor.name = "_HB_RefractionBackground";
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.clearBuffer = false;
            descriptor.useMipMap = true;
            descriptor.autoGenerateMips = false;
            descriptor.filterMode = FilterMode.Trilinear;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.enableRandomWrite = false;
            descriptor.bindTextureMS = false;

            var background = renderGraph.CreateTexture(descriptor);

            using var builder = renderGraph.AddUnsafePass<CopyData>("HB Style Refraction Background", out var data,
                                                                   profilingSampler);

            data.source = colour;
            data.background = background;

            builder.UseTexture(colour, AccessFlags.Read);
            builder.UseTexture(background, AccessFlags.Write);
            builder.SetGlobalTextureAfterPass(background, s_BackgroundId);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (CopyData d, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                cmd.CopyTexture(d.source, 0, 0, d.background, 0, 0);
                cmd.GenerateMips(d.background);
            });

            return background;
        }

        void RecordForward(RenderGraph renderGraph, TextureHandle colour, TextureHandle depth, float stage,
                           TextureHandle nearest, TextureHandle background, UniversalResourceData resourceData,
                           UniversalRenderingData renderingData, UniversalCameraData cameraData,
                           UniversalLightData lightData)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawData>(s_ForwardNames[(int)stage], out var data,
                                                                         profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_ForwardTags, renderingData, cameraData, lightData,
                                                               SortingCriteria.CommonTransparent);

            data.list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                              Filtering(cameraData)));
            data.stage = stage;

            Read(builder, resourceData.cameraDepthTexture);
            Read(builder, nearest);
            Read(builder, background);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachment(colour, 0);
            builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalFloat(s_StageId, d.stage);
                context.cmd.DrawRendererList(d.list);
                context.cmd.SetGlobalFloat(s_StageId, WholeStage);
            });
        }

        static void Read(IRasterRenderGraphBuilder builder, TextureHandle handle)
        {
            if (handle.IsValid())
                builder.UseTexture(handle, AccessFlags.Read);
        }

        static FilteringSettings Filtering(UniversalCameraData cameraData)
        {
            return new FilteringSettings(RenderQueueRange.transparent, cameraData.camera.cullingMask);
        }

        void Probe(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData,
                   out bool glass, out bool refractive)
        {
            glass = true;
            refractive = true;

            if (!m_HasContext)
                return;

            var nearest = CreateProbe(s_NearestTag, renderingData, cameraData, lightData);
            var refracted = CreateProbe(s_RefractiveTag, renderingData, cameraData, lightData);

            m_Probes.Clear();
            m_Probes.Add(nearest);
            m_Probes.Add(refracted);
            m_Context.PrepareRendererListsAsync(m_Probes);

            glass = Visible(nearest);
            refractive = Visible(refracted);
        }

        RendererList CreateProbe(ShaderTagId tag, UniversalRenderingData renderingData,
                                 UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var drawing = RenderingUtils.CreateDrawingSettings(tag, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);
            var parameters = new RendererListParams(renderingData.cullResults, drawing, Filtering(cameraData));

            return m_Context.CreateRendererList(ref parameters);
        }

        bool Visible(RendererList list)
        {
            return !list.isValid || m_Context.QueryRendererListStatus(list) != RendererListStatus.kRendererListEmpty;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginContextRendering -= Capture;
            m_HasContext = false;
        }
    }
}
