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

        static readonly int s_B0Id = Shader.PropertyToID("_HB_OITB0");
        static readonly int s_MomentsId = Shader.PropertyToID("_HB_OITMoments");
        static readonly int s_AccumId = Shader.PropertyToID("_HB_OITAccum");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_OITParams");

        Material m_Material;

        public StyleTransparencyPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            profilingSampler = new ProfilingSampler("HiddenBull Style Transparency");
            requiresIntermediateTexture = true;
        }

        public void Setup(Shader shader)
        {
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
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthStencilFormat = GraphicsFormat.None;
            descriptor.depthBufferBits = 0;

            var fullPrecision = SystemInfo.IsFormatSupported(
                GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormatUsage.Blend);

            descriptor.graphicsFormat = fullPrecision ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat;
            var b0 = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, descriptor, "_HB_OITB0", true, FilterMode.Point);

            descriptor.graphicsFormat = fullPrecision
                ? GraphicsFormat.R32G32B32A32_SFloat
                : GraphicsFormat.R16G16B16A16_SFloat;
            var moments = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, descriptor, "_HB_OITMoments", true, FilterMode.Point);

            descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            var accum = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, descriptor, "_HB_OITAccum", true, FilterMode.Point);

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
                       "HiddenBull OIT Moments", out var passData, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_MomentsTag, renderingData, cameraData, lightData, SortingCriteria.None);

                passData.list = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing, filtering));
                passData.parameters = parameters;

                builder.UseRendererList(passData.list);
                builder.SetRenderAttachment(b0, 0, AccessFlags.Write);
                builder.SetRenderAttachment(moments, 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(b0, s_B0Id);
                builder.SetGlobalTextureAfterPass(moments, s_MomentsId);

                builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ParamsId, data.parameters);
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
                builder.SetRenderAttachment(accum, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(accum, s_AccumId);

                builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.list);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>(
                       "HiddenBull OIT Composite", out var passData, profilingSampler))
            {
                passData.material = m_Material;

                builder.UseTexture(b0, AccessFlags.Read);
                builder.UseTexture(accum, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, 0);
                });
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);

            m_Material = null;
        }
    }
}
