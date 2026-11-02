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
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparencyComposite.shader";

        const float FullPrecisionBias = 5e-7f;
        const float HalfPrecisionBias = 6e-4f;
        const float FullRoundMargin = 1e-5f;
        const float HalfRoundMargin = 1e-3f;
        const int TransmitPass = 0;
        const int AddPass = 1;
        const int RefractPass = 2;
        const int BackgroundDepthPass = 3;
        const int DownsampleDepthPass = 4;
        const float SingleRound = 0f;
        const float BehindRound = 1f;
        const float FrontRound = 2f;

        static readonly ShaderTagId s_MomentsTag = new ShaderTagId("HBStyleOITMoments");
        static readonly ShaderTagId s_ColorTag = new ShaderTagId("HBStyleOITColor");
        static readonly ShaderTagId s_GlassTag = new ShaderTagId("HBStyleOITGlass");
        static readonly int s_B0Id = Shader.PropertyToID("_HB_OITB0");
        static readonly int s_MomentsId = Shader.PropertyToID("_HB_OITMoments");
        static readonly int s_ChromaId = Shader.PropertyToID("_HB_OITChroma");
        static readonly int s_AccumulatedId = Shader.PropertyToID("_HB_OITAccumulated");
        static readonly int s_TintId = Shader.PropertyToID("_HB_OITTint");
        static readonly int s_LightId = Shader.PropertyToID("_HB_OITLight");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_OITParams");
        static readonly int s_StateId = Shader.PropertyToID("_HB_OITState");
        static readonly int s_RefractionId = Shader.PropertyToID("_HB_OITRefraction");
        static readonly int s_GlassId = Shader.PropertyToID("_HB_OITGlass");
        static readonly int s_BackgroundId = Shader.PropertyToID("_HB_OITBackground");
        static readonly int s_BackgroundDepthId = Shader.PropertyToID("_HB_OITBackgroundDepth");
        static readonly int s_DepthId = Shader.PropertyToID("_HB_OITDepth");
        static readonly int s_ScaledScreenParamsId = Shader.PropertyToID("_ScaledScreenParams");

        class DrawData
        {
            internal RendererListHandle list;
            internal Vector4 parameters;
            internal Vector4 state;
            internal Vector4 halfScreen;
            internal Vector4 fullScreen;
        }

        class CompositeData
        {
            internal Material material;
            internal int pass;
            internal bool add;
        }

        class CopyData
        {
            internal TextureHandle source;
            internal TextureHandle background;
        }

        struct Frame
        {
            internal RenderGraph renderGraph;
            internal UniversalRenderingData renderingData;
            internal UniversalCameraData cameraData;
            internal UniversalLightData lightData;
            internal TextureDesc descriptor;
            internal TextureHandle colour;
            internal TextureHandle sceneDepth;
            internal TextureHandle depth;
            internal TextureHandle depthTexture;
            internal Vector4 parameters;
            internal Vector4 halfScreen;
            internal Vector4 fullScreen;
            internal bool fullPrecision;
        }

        struct Layer
        {
            internal TextureHandle b0;
            internal TextureHandle moments;
            internal TextureHandle chroma;
            internal TextureHandle accumulated;
            internal TextureHandle tint;
            internal TextureHandle light;
        }

        readonly List<RendererList> m_Probes = new List<RendererList>(2);

        Material m_Material;
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

        internal void Setup(Shader shader)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            var colour = resourceData.activeColorTexture;
            var sceneDepth = resourceData.cameraDepthTexture;

            if (!colour.IsValid() || !sceneDepth.IsValid())
                return;

            var drawn = Probe(s_ColorTag, renderingData, cameraData, lightData);
            var refracted = Probe(s_GlassTag, renderingData, cameraData, lightData);

            if (m_HasContext)
            {
                m_Probes.Clear();
                m_Probes.Add(drawn);
                m_Probes.Add(refracted);
                m_Context.PrepareRendererListsAsync(m_Probes);
            }

            if (!Visible(drawn))
                return;

            var refractive = Visible(refracted);
            var fullPrecision = SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat,
                                                             GraphicsFormatUsage.Blend);

            var camera = cameraData.camera;
            var logNear = Mathf.Log(Mathf.Max(camera.nearClipPlane, 1e-4f));
            var logFar = Mathf.Log(Mathf.Max(camera.farClipPlane, camera.nearClipPlane + 1e-3f));

            var colourDescriptor = renderGraph.GetTextureDesc(colour);
            var width = Mathf.Max(colourDescriptor.width, 1);
            var height = Mathf.Max(colourDescriptor.height, 1);
            var halfWidth = (width + 1) / 2;
            var halfHeight = (height + 1) / 2;

            var descriptor = colourDescriptor;
            descriptor.sizeMode = TextureSizeMode.Explicit;
            descriptor.width = halfWidth;
            descriptor.height = halfHeight;

            var frame = new Frame
            {
                renderGraph = renderGraph,
                renderingData = renderingData,
                cameraData = cameraData,
                lightData = lightData,
                descriptor = descriptor,
                colour = colour,
                sceneDepth = sceneDepth,
                fullPrecision = fullPrecision,
                parameters = new Vector4(fullPrecision ? FullPrecisionBias : HalfPrecisionBias, logNear,
                                         1f / Mathf.Max(logFar - logNear, 1e-4f),
                                         fullPrecision ? FullRoundMargin : HalfRoundMargin),
                halfScreen = new Vector4(halfWidth, halfHeight, 1f + 1f / halfWidth, 1f + 1f / halfHeight),
                fullScreen = new Vector4(width, height, 1f + 1f / width, 1f + 1f / height)
            };

            RecordDepth(ref frame);

            if (!refractive)
            {
                var unused = Create(renderGraph, descriptor, "_HB_OITRefraction", GraphicsFormat.R8_UNorm,
                                    Color.clear);
                var single = RecordLayer(frame, "", State(frame, 0f, SingleRound), default, unused);

                RecordComposite(frame, "HB Style OIT Composite", single, TransmitPass, default, default, default,
                                default);
                return;
            }

            var glass = Create(renderGraph, descriptor, "_HB_OITGlass",
                               fullPrecision ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat, Color.clear);

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style OIT Glass", out var data,
                                                                           profilingSampler))
            {
                data.list = CreateList(renderGraph, s_GlassTag, renderingData, cameraData, lightData);
                data.parameters = frame.parameters;
                data.halfScreen = frame.halfScreen;
                data.fullScreen = frame.fullScreen;

                builder.UseRendererList(data.list);
                builder.SetRenderAttachment(glass, 0);
                builder.SetRenderAttachmentDepth(frame.depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(glass, s_GlassId);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ParamsId, d.parameters);
                    context.cmd.SetGlobalVector(s_ScaledScreenParamsId, d.halfScreen);
                    context.cmd.DrawRendererList(d.list);
                    context.cmd.SetGlobalVector(s_ScaledScreenParamsId, d.fullScreen);
                });
            }

            var refraction = Create(renderGraph, descriptor, "_HB_OITRefraction",
                                    GraphicsFormat.R16G16B16A16_SFloat, Color.clear);
            var behind = RecordLayer(frame, " Behind", State(frame, 1f, BehindRound), glass, refraction);

            RecordComposite(frame, "HB Style OIT Composite Behind", behind, TransmitPass, default, default, glass,
                            default);

            var backgroundDepth = Create(renderGraph, descriptor, "_HB_OITBackgroundDepth",
                                         fullPrecision ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat,
                                         Color.clear);

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("HB Style OIT Background Depth",
                                                                                out var data, profilingSampler))
            {
                data.material = m_Material;
                data.pass = BackgroundDepthPass;

                builder.UseTexture(behind.b0, AccessFlags.Read);
                builder.UseTexture(behind.moments, AccessFlags.Read);
                builder.UseTexture(frame.depthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(backgroundDepth, 0);
                builder.SetGlobalTextureAfterPass(backgroundDepth, s_BackgroundDepthId);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CompositeData d, RasterGraphContext context) =>
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3));
            }

            var backgroundDescriptor = colourDescriptor;
            backgroundDescriptor.name = "_HB_OITBackground";
            backgroundDescriptor.msaaSamples = MSAASamples.None;
            backgroundDescriptor.clearBuffer = false;
            backgroundDescriptor.useMipMap = true;
            backgroundDescriptor.autoGenerateMips = false;
            backgroundDescriptor.filterMode = FilterMode.Trilinear;
            backgroundDescriptor.wrapMode = TextureWrapMode.Clamp;

            var background = renderGraph.CreateTexture(backgroundDescriptor);

            using (var builder = renderGraph.AddUnsafePass<CopyData>("HB Style OIT Background", out var data,
                                                                     profilingSampler))
            {
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
            }

            var front = RecordLayer(frame, " Front", State(frame, 1f, FrontRound), glass, refraction, true);

            RecordComposite(frame, "HB Style OIT Composite Front", front, RefractPass, refraction, background, glass,
                            backgroundDepth);
        }

        static Vector4 State(Frame frame, float refractive, float round)
        {
            return new Vector4(refractive, round, frame.descriptor.width, frame.descriptor.height);
        }

        void RecordDepth(ref Frame frame)
        {
            var renderGraph = frame.renderGraph;

            var depthDescriptor = frame.descriptor;
            depthDescriptor.name = "_HB_OITDepthBuffer";
            depthDescriptor.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            depthDescriptor.msaaSamples = MSAASamples.None;
            depthDescriptor.filterMode = FilterMode.Point;
            depthDescriptor.wrapMode = TextureWrapMode.Clamp;
            depthDescriptor.clearBuffer = false;
            depthDescriptor.enableRandomWrite = false;
            depthDescriptor.useMipMap = false;
            depthDescriptor.autoGenerateMips = false;
            depthDescriptor.bindTextureMS = false;

            frame.depth = renderGraph.CreateTexture(depthDescriptor);
            frame.depthTexture = Create(renderGraph, frame.descriptor, "_HB_OITDepth", GraphicsFormat.R32_SFloat,
                                        Color.clear);

            using var builder = renderGraph.AddRasterRenderPass<CompositeData>("HB Style OIT Depth", out var data,
                                                                               profilingSampler);

            data.material = m_Material;
            data.pass = DownsampleDepthPass;

            builder.UseTexture(frame.sceneDepth, AccessFlags.Read);
            builder.SetRenderAttachment(frame.depthTexture, 0);
            builder.SetRenderAttachmentDepth(frame.depth, AccessFlags.Write);
            builder.SetGlobalTextureAfterPass(frame.depthTexture, s_DepthId);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (CompositeData d, RasterGraphContext context) =>
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3));
        }

        Layer RecordLayer(Frame frame, string suffix, Vector4 state, TextureHandle glass, TextureHandle refraction,
                          bool publishRefraction = false)
        {
            var renderGraph = frame.renderGraph;
            var descriptor = frame.descriptor;

            var layer = new Layer
            {
                b0 = Create(renderGraph, descriptor, "_HB_OITB0",
                            frame.fullPrecision ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat,
                            Color.clear),
                moments = Create(renderGraph, descriptor, "_HB_OITMoments",
                                 frame.fullPrecision ? GraphicsFormat.R32G32B32A32_SFloat
                                                     : GraphicsFormat.R16G16B16A16_SFloat, Color.clear),
                chroma = Create(renderGraph, descriptor, "_HB_OITChroma",
                                frame.fullPrecision ? GraphicsFormat.R32G32B32A32_SFloat
                                                    : GraphicsFormat.R16G16B16A16_SFloat, Color.clear),
                accumulated = Create(renderGraph, descriptor, "_HB_OITAccumulated",
                                     GraphicsFormat.R16G16B16A16_SFloat, Color.clear),
                tint = Create(renderGraph, descriptor, "_HB_OITTint", GraphicsFormat.R16G16B16A16_SFloat,
                              Color.white),
                light = Create(renderGraph, descriptor, "_HB_OITLight", GraphicsFormat.B10G11R11_UFloatPack32,
                               Color.clear)
            };

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style OIT Moments" + suffix,
                                                                           out var data, profilingSampler))
            {
                data.list = CreateList(renderGraph, s_MomentsTag, frame.renderingData, frame.cameraData,
                                       frame.lightData);
                data.parameters = frame.parameters;
                data.state = state;
                data.halfScreen = frame.halfScreen;
                data.fullScreen = frame.fullScreen;

                Read(builder, glass);
                builder.UseRendererList(data.list);
                builder.SetRenderAttachment(layer.b0, 0);
                builder.SetRenderAttachment(layer.moments, 1);
                builder.SetRenderAttachment(layer.chroma, 2);
                builder.SetRenderAttachmentDepth(frame.depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(layer.b0, s_B0Id);
                builder.SetGlobalTextureAfterPass(layer.moments, s_MomentsId);
                builder.SetGlobalTextureAfterPass(layer.chroma, s_ChromaId);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ParamsId, d.parameters);
                    context.cmd.SetGlobalVector(s_StateId, d.state);
                    context.cmd.SetGlobalVector(s_ScaledScreenParamsId, d.halfScreen);
                    context.cmd.DrawRendererList(d.list);
                    context.cmd.SetGlobalVector(s_ScaledScreenParamsId, d.fullScreen);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style OIT Color" + suffix,
                                                                           out var data, profilingSampler))
            {
                data.list = CreateList(renderGraph, s_ColorTag, frame.renderingData, frame.cameraData,
                                       frame.lightData);
                data.halfScreen = frame.halfScreen;
                data.fullScreen = frame.fullScreen;

                Read(builder, glass);
                builder.UseRendererList(data.list);
                builder.UseTexture(layer.b0, AccessFlags.Read);
                builder.UseTexture(layer.moments, AccessFlags.Read);
                builder.UseTexture(layer.chroma, AccessFlags.Read);
                builder.SetRenderAttachment(layer.accumulated, 0);
                builder.SetRenderAttachment(layer.tint, 1);
                builder.SetRenderAttachment(layer.light, 2);
                builder.SetRenderAttachment(refraction, 3);
                builder.SetRenderAttachmentDepth(frame.depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(layer.accumulated, s_AccumulatedId);
                builder.SetGlobalTextureAfterPass(layer.tint, s_TintId);
                builder.SetGlobalTextureAfterPass(layer.light, s_LightId);

                if (publishRefraction)
                    builder.SetGlobalTextureAfterPass(refraction, s_RefractionId);

                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ScaledScreenParamsId, d.halfScreen);
                    context.cmd.DrawRendererList(d.list);
                    context.cmd.SetGlobalVector(s_ScaledScreenParamsId, d.fullScreen);
                });
            }

            return layer;
        }

        void RecordComposite(Frame frame, string name, Layer layer, int transmitPass, TextureHandle refraction,
                             TextureHandle background, TextureHandle glass, TextureHandle backgroundDepth)
        {
            using var builder = frame.renderGraph.AddRasterRenderPass<CompositeData>(name, out var data,
                                                                                     profilingSampler);

            data.material = m_Material;
            data.pass = transmitPass;
            data.add = transmitPass != RefractPass;

            builder.UseTexture(layer.b0, AccessFlags.Read);
            builder.UseTexture(layer.accumulated, AccessFlags.Read);
            builder.UseTexture(layer.tint, AccessFlags.Read);
            builder.UseTexture(layer.light, AccessFlags.Read);
            builder.UseTexture(frame.depthTexture, AccessFlags.Read);
            builder.UseTexture(frame.sceneDepth, AccessFlags.Read);

            Read(builder, refraction);
            Read(builder, background);
            Read(builder, glass);
            Read(builder, backgroundDepth);

            builder.SetRenderAttachment(frame.colour, 0);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (CompositeData d, RasterGraphContext context) =>
            {
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3);

                if (d.add)
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, AddPass, MeshTopology.Triangles, 3);
            });
        }

        static void Read(IRasterRenderGraphBuilder builder, TextureHandle handle)
        {
            if (handle.IsValid())
                builder.UseTexture(handle, AccessFlags.Read);
        }

        static TextureHandle Create(RenderGraph renderGraph, TextureDesc source, string name, GraphicsFormat format,
                                    Color clear)
        {
            var descriptor = source;
            descriptor.name = name;
            descriptor.format = format;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.filterMode = FilterMode.Point;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.clearBuffer = true;
            descriptor.clearColor = clear;
            descriptor.enableRandomWrite = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.bindTextureMS = false;

            return renderGraph.CreateTexture(descriptor);
        }

        static RendererListHandle CreateList(RenderGraph renderGraph, ShaderTagId tag,
                                             UniversalRenderingData renderingData, UniversalCameraData cameraData,
                                             UniversalLightData lightData)
        {
            var drawing = RenderingUtils.CreateDrawingSettings(tag, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);
            var filtering = new FilteringSettings(RenderQueueRange.transparent, cameraData.camera.cullingMask);

            return renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                         filtering));
        }

        RendererList Probe(ShaderTagId tag, UniversalRenderingData renderingData, UniversalCameraData cameraData,
                           UniversalLightData lightData)
        {
            if (!m_HasContext)
                return RendererList.nullRendererList;

            var drawing = RenderingUtils.CreateDrawingSettings(tag, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);
            var parameters = new RendererListParams(renderingData.cullResults, drawing,
                                                    new FilteringSettings(RenderQueueRange.transparent,
                                                                          cameraData.camera.cullingMask));

            return m_Context.CreateRendererList(ref parameters);
        }

        bool Visible(RendererList list)
        {
            if (!m_HasContext || !list.isValid)
                return true;

            return m_Context.QueryRendererListStatus(list) != RendererListStatus.kRendererListEmpty;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginContextRendering -= Capture;
            m_HasContext = false;

            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
