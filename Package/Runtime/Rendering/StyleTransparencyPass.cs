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
        internal const string BlurShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/RefractionBlur.shader";

        internal const string CutShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidCut.shader";

        const int BlurLevels = 6;
        const float MinRefractionScale = 0.25f;
        const float WholeStage = 0f;
        const float BehindStage = 1f;
        const float FrontStage = 2f;

        static readonly ShaderTagId s_NearestTag = new ShaderTagId("HBStyleTransparentNearest");
        static readonly ShaderTagId s_RefractiveTag = new ShaderTagId("HBStyleTransparentRefractive");
        static readonly ShaderTagId s_LiquidTag = new ShaderTagId("HBStyleLiquid");
        static readonly ShaderTagId s_LiquidCutTag = new ShaderTagId("HBStyleLiquidCut");
        static readonly List<ShaderTagId> s_NearestTags = new List<ShaderTagId> { s_NearestTag, s_RefractiveTag };
        static readonly List<ShaderTagId> s_GlassTags = new List<ShaderTagId> { s_NearestTag };
        static readonly List<ShaderTagId> s_ForwardTags = new List<ShaderTagId>
        {
            new ShaderTagId("HBStyleTransparentTint"),
            new ShaderTagId("HBStyleTransparent")
        };

        static readonly int s_NearestId = Shader.PropertyToID("_HB_TransparentNearest");
        static readonly int s_BackgroundId = Shader.PropertyToID("_HB_RefractionBackground");
        static readonly int s_StageId = Shader.PropertyToID("_HB_TransparentStage");
        static readonly int s_SimpleId = Shader.PropertyToID("_HB_TransparentSimple");
        static readonly int s_BlurSourceId = Shader.PropertyToID("_HB_BlurSource");
        static readonly int s_BlurSourceTexelId = Shader.PropertyToID("_HB_BlurSourceTexel");
        static readonly int s_BlurTargetSizeId = Shader.PropertyToID("_HB_BlurTargetSize");
        static readonly int s_BlurLodId = Shader.PropertyToID("_HB_BlurLod");
        static readonly int s_CutIdsId = Shader.PropertyToID("_HB_LiquidCutIds");
        static readonly int s_CutMaskId = Shader.PropertyToID("_HB_LiquidCutMask");
        static readonly int s_CutStateId = Shader.PropertyToID("_HB_LiquidCutState");
        static readonly int s_CutWriteId = Shader.PropertyToID("_HB_CutWrite");
        static readonly int s_CutTargetId = Shader.PropertyToID("_HB_CutTarget");

        static readonly Vector4 s_CutOut = new Vector4(1f, 0f, 0f, 0f);
        static readonly Vector4 s_KeepIn = new Vector4(0f, 1f, 0f, 0f);
        static readonly Vector4 s_CutOn = new Vector4(1f, 0f, 0f, 0f);

        static readonly string[] s_ForwardNames =
        {
            "HB Style Transparent", "HB Style Transparent Behind", "HB Style Transparent Front"
        };

        class DrawData
        {
            internal RendererListHandle list;
            internal float stage;
            internal float simple;
            internal bool cut;
        }

        struct CutDraw
        {
            internal Mesh mesh;
            internal Matrix4x4 matrix;
            internal Vector4 write;
            internal float target;
        }

        class CutData
        {
            internal List<CutDraw> cuts;
            internal Material material;
            internal MaterialPropertyBlock block;
        }

        class CopyData
        {
            internal TextureHandle source;
            internal TextureHandle background;
            internal TextureHandle temp;
            internal Material material;
            internal Vector2Int sourceSize;
            internal Vector2Int size;
            internal int levels;
            internal bool full;
        }

        readonly List<RendererList> m_Probes = new List<RendererList>(3);
        readonly List<CutDraw> m_Cuts = new List<CutDraw>();
        readonly Plane[] m_Planes = new Plane[6];
        readonly MaterialPropertyBlock m_CutBlock = new MaterialPropertyBlock();

        StylePrecipitationPass m_Precipitation;
        HBStyleTransparencyLevel m_Level;
        Material m_BlurMaterial;
        Material m_CutMaterial;
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

        internal bool liquidCut { get; private set; }

        internal void Setup(StylePrecipitationPass precipitation, HBStyleTransparencyQuality quality, Shader blurShader,
                            Shader cutShader)
        {
            if (m_BlurMaterial == null && blurShader != null)
                m_BlurMaterial = CoreUtils.CreateEngineMaterial(blurShader);

            if (m_CutMaterial == null && cutShader != null)
                m_CutMaterial = CoreUtils.CreateEngineMaterial(cutShader);

            m_Precipitation = precipitation;
            m_Level = quality.current;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            liquidCut = false;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            var colour = resourceData.activeColorTexture;
            var depth = resourceData.activeDepthTexture;

            if (!colour.IsValid() || !depth.IsValid())
                return;

            var raining = m_Precipitation != null && m_Precipitation.Prepare(cameraData.camera);

            var refraction = m_BlurMaterial != null;

            Probe(renderingData, cameraData, lightData, refraction, out var glass, out var refractive, out var liquid);

            if (liquid)
            {
                var cut = Cutters(cameraData.camera);
                liquidCut = cut;

                if (cut)
                    RecordCut(renderGraph, colour, renderingData, cameraData, lightData);

                var below = refraction ? RecordBackground(renderGraph, colour, cameraData) : default;

                RecordLiquid(renderGraph, colour, depth, below, cut, resourceData, renderingData, cameraData,
                             lightData);
            }

            var splitRain = raining && glass;

            if (!splitRain && !refractive)
            {
                if (raining)
                    m_Precipitation.Draw(renderGraph, colour, depth, StylePrecipitationPass.PlainStage, default);

                RecordForward(renderGraph, colour, depth, WholeStage, default, default, resourceData, renderingData,
                              cameraData, lightData);
                return;
            }

            var nearest = RecordNearest(renderGraph, colour, depth, refractive, renderingData, cameraData, lightData);

            if (splitRain)
                m_Precipitation.Draw(renderGraph, colour, depth, StylePrecipitationPass.UnderStage, nearest);

            if (refractive)
            {
                RecordForward(renderGraph, colour, depth, BehindStage, nearest, default, resourceData, renderingData,
                              cameraData, lightData);

                var background = RecordBackground(renderGraph, colour, cameraData);

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

        TextureHandle RecordNearest(RenderGraph renderGraph, TextureHandle colour, TextureHandle depth, bool refractive,
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

            var drawing = RenderingUtils.CreateDrawingSettings(refractive ? s_NearestTags : s_GlassTags, renderingData,
                                                               cameraData, lightData, SortingCriteria.None);

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

        TextureHandle RecordBackground(RenderGraph renderGraph, TextureHandle colour, UniversalCameraData cameraData)
        {
            var descriptor = renderGraph.GetTextureDesc(colour);
            var target = cameraData.cameraTargetDescriptor;
            var explicitSize = descriptor.sizeMode == TextureSizeMode.Explicit;
            var scale = Mathf.Clamp(m_Level.refractionResolution, MinRefractionScale, 1f);
            var full = scale > 0.999f;
            var sourceSize = explicitSize ? new Vector2Int(descriptor.width, descriptor.height)
                                          : new Vector2Int(target.width, target.height);

            sourceSize = Vector2Int.Max(sourceSize, Vector2Int.one);

            var size = full ? sourceSize
                            : Vector2Int.Max(Vector2Int.RoundToInt((Vector2)sourceSize * scale), Vector2Int.one);
            var levels = Mathf.Min(BlurLevels, Mathf.FloorToInt(Mathf.Log(Mathf.Min(size.x, size.y), 2f)));

            descriptor.name = "_HB_RefractionBackground";
            descriptor.sizeMode = TextureSizeMode.Explicit;
            descriptor.width = size.x;
            descriptor.height = size.y;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.clearBuffer = false;
            descriptor.useMipMap = true;
            descriptor.autoGenerateMips = false;
            descriptor.filterMode = FilterMode.Trilinear;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.enableRandomWrite = false;
            descriptor.bindTextureMS = false;

            var background = renderGraph.CreateTexture(descriptor);
            var blurSize = Half(size, 1);

            descriptor.name = "_HB_RefractionBlur";
            descriptor.width = blurSize.x;
            descriptor.height = blurSize.y;

            var temp = renderGraph.CreateTexture(descriptor);

            using var builder = renderGraph.AddUnsafePass<CopyData>("HB Style Refraction Background", out var data,
                                                                   profilingSampler);

            data.source = colour;
            data.background = background;
            data.temp = temp;
            data.material = m_BlurMaterial;
            data.sourceSize = sourceSize;
            data.size = size;
            data.levels = levels;
            data.full = full;

            builder.UseTexture(colour, AccessFlags.Read);
            builder.UseTexture(background, AccessFlags.ReadWrite);
            builder.UseTexture(temp, AccessFlags.ReadWrite);
            builder.SetGlobalTextureAfterPass(background, s_BackgroundId);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (CopyData d, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                if (d.full)
                {
                    cmd.CopyTexture(d.source, 0, 0, d.background, 0, 0);
                }
                else
                {
                    cmd.SetRenderTarget(d.background, 0);
                    Downsample(cmd, d.material, d.source, d.sourceSize, 0, d.size);
                }

                for (var level = 1; level <= d.levels; level++)
                {
                    cmd.SetRenderTarget(d.temp, level - 1);
                    Downsample(cmd, d.material, d.background, Half(d.size, level - 1), level - 1, Half(d.size, level));
                    cmd.CopyTexture(d.temp, 0, level - 1, d.background, 0, level);
                }
            });

            return background;
        }

        static Vector2Int Half(Vector2Int size, int steps)
        {
            return new Vector2Int(Mathf.Max(size.x >> steps, 1), Mathf.Max(size.y >> steps, 1));
        }

        static void Downsample(CommandBuffer cmd, Material material, RenderTargetIdentifier source, Vector2Int from,
                               int lod, Vector2Int to)
        {
            cmd.SetViewport(new Rect(0f, 0f, to.x, to.y));
            cmd.SetGlobalTexture(s_BlurSourceId, source);
            cmd.SetGlobalVector(s_BlurSourceTexelId, new Vector4(1f / from.x, 1f / from.y, from.x, from.y));
            cmd.SetGlobalVector(s_BlurTargetSizeId, new Vector4(to.x, to.y, 1f / to.x, 1f / to.y));
            cmd.SetGlobalFloat(s_BlurLodId, lod);
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
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
            data.simple = m_Level.simpleLighting ? 1f : 0f;

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
                context.cmd.SetGlobalFloat(s_SimpleId, d.simple);
                context.cmd.DrawRendererList(d.list);
                context.cmd.SetGlobalFloat(s_StageId, WholeStage);
                context.cmd.SetGlobalFloat(s_SimpleId, 0f);
            });
        }

        bool Cutters(Camera camera)
        {
            m_Cuts.Clear();

            var cutters = HBStyleLiquidCutter.enabledCutters;
            var any = false;

            if (m_CutMaterial == null || cutters.Count == 0)
                return false;

            GeometryUtility.CalculateFrustumPlanes(camera, m_Planes);

            foreach (var cutter in cutters)
            {
                if (cutter == null || cutter.affects == 0 || !cutter.Spans(out var bounds))
                    continue;

                any = true;

                if (!GeometryUtility.TestPlanesAABB(m_Planes, bounds))
                    continue;

                m_Cuts.Add(new CutDraw
                {
                    mesh = cutter.volume,
                    matrix = cutter.transform.localToWorldMatrix,
                    write = cutter.mode == HBStyleLiquidCutMode.KeepInside ? s_KeepIn : s_CutOut,
                    target = (int)cutter.affects
                });
            }

            return any;
        }

        void RecordCut(RenderGraph renderGraph, TextureHandle colour, UniversalRenderingData renderingData,
                       UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var descriptor = renderGraph.GetTextureDesc(colour);

            descriptor.msaaSamples = MSAASamples.None;
            descriptor.filterMode = FilterMode.Point;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.clearBuffer = true;
            descriptor.clearColor = Color.clear;
            descriptor.enableRandomWrite = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.bindTextureMS = false;

            descriptor.name = "_HB_LiquidCutIds";
            descriptor.format = GraphicsFormat.R16_SFloat;

            var ids = renderGraph.CreateTexture(descriptor);

            descriptor.name = "_HB_LiquidCutMask";
            descriptor.format = GraphicsFormat.R16G16_SFloat;

            var mask = renderGraph.CreateTexture(descriptor);

            descriptor.name = "_HB_LiquidCutDepth";
            descriptor.depthBufferBits = DepthBits.Depth32;

            var depth = renderGraph.CreateTexture(descriptor);

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Liquid Cut Ids", out var data,
                                                                          profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(s_LiquidCutTag, renderingData, cameraData,
                                                                   lightData, SortingCriteria.CommonOpaque);

                data.list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                                  Filtering(cameraData)));

                builder.UseRendererList(data.list);
                builder.SetRenderAttachment(ids, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(ids, s_CutIdsId);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(d.list));
            }

            using (var builder = renderGraph.AddRasterRenderPass<CutData>("HB Style Liquid Cut Mask", out var data,
                                                                         profilingSampler))
            {
                data.cuts = m_Cuts;
                data.material = m_CutMaterial;
                data.block = m_CutBlock;

                builder.UseTexture(ids, AccessFlags.Read);
                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(mask, s_CutMaskId);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CutData d, RasterGraphContext context) =>
                {
                    foreach (var cut in d.cuts)
                    {
                        if (cut.mesh == null)
                            continue;

                        d.block.SetVector(s_CutWriteId, cut.write);
                        d.block.SetFloat(s_CutTargetId, cut.target);

                        for (var part = 0; part < cut.mesh.subMeshCount; part++)
                            context.cmd.DrawMesh(cut.mesh, cut.matrix, d.material, part, 0, d.block);
                    }
                });
            }
        }

        void RecordLiquid(RenderGraph renderGraph, TextureHandle colour, TextureHandle depth, TextureHandle background,
                          bool cut, UniversalResourceData resourceData, UniversalRenderingData renderingData,
                          UniversalCameraData cameraData, UniversalLightData lightData)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Liquid", out var data,
                                                                         profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_LiquidTag, renderingData, cameraData, lightData,
                                                               SortingCriteria.CommonOpaque);

            data.list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                              Filtering(cameraData)));
            data.cut = cut;

            Read(builder, resourceData.cameraDepthTexture);
            Read(builder, background);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachment(colour, 0);
            builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalVector(s_CutStateId, d.cut ? s_CutOn : Vector4.zero);
                context.cmd.DrawRendererList(d.list);
                context.cmd.SetGlobalVector(s_CutStateId, Vector4.zero);
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
                   bool refraction, out bool glass, out bool refractive, out bool liquid)
        {
            glass = true;
            refractive = refraction;
            liquid = true;

            if (!m_HasContext)
                return;

            var nearest = CreateProbe(s_NearestTag, renderingData, cameraData, lightData);
            var refracted = refraction ? CreateProbe(s_RefractiveTag, renderingData, cameraData, lightData) : default;
            var wet = CreateProbe(s_LiquidTag, renderingData, cameraData, lightData);

            m_Probes.Clear();
            m_Probes.Add(nearest);
            m_Probes.Add(wet);

            if (refraction)
                m_Probes.Add(refracted);

            m_Context.PrepareRendererListsAsync(m_Probes);

            glass = Visible(nearest);
            refractive = refraction && Visible(refracted);
            liquid = Visible(wet);
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
            CoreUtils.Destroy(m_BlurMaterial);
            m_BlurMaterial = null;
            CoreUtils.Destroy(m_CutMaterial);
            m_CutMaterial = null;
        }
    }
}
