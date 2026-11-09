using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleUnderwaterPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/Underwater.shader";

        const float BlurReach = 1f;
        const int BasePass = 0;
        const int OverlayPass = 1;

        static readonly ShaderTagId s_MaskTag = new ShaderTagId("HBStyleLiquidMask");

        static readonly int s_MapId = Shader.PropertyToID("_HB_UnderMap");
        static readonly int s_ViewId = Shader.PropertyToID("_HB_UnderView");
        static readonly int s_MixId = Shader.PropertyToID("_HB_UnderMix");
        static readonly int s_ColourId = Shader.PropertyToID("_HB_UnderColour");
        static readonly int s_MaskId = Shader.PropertyToID("_HB_UnderMask");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_UnderHeight");
        static readonly int s_DepthId = Shader.PropertyToID("_HB_UnderDepth");
        static readonly int s_BlurId = Shader.PropertyToID("_HB_RefractionBackground");
        static readonly int s_CutStateId = Shader.PropertyToID("_HB_LiquidCutState");
        static readonly int s_QualityId = Shader.PropertyToID("_HB_UnderQuality");
        static readonly int s_OriginId = Shader.PropertyToID("_HB_TerrainOrigin");
        static readonly int s_AheadId = Shader.PropertyToID("_HB_UnderAhead");

        static readonly Vector4 s_CutOn = new Vector4(1f, 0f, 0f, 0f);
        static readonly Vector4 s_Whole = new Vector4(1f, 1f, 0f, 0f);

        class DrawData
        {
            internal RendererListHandle list;
            internal bool cut;
        }

        class CopyData
        {
            internal TextureHandle source;
        }

        class CompositeData
        {
            internal Material material;
            internal MaterialPropertyBlock block;
            internal int pass;
            internal TextureHandle colour;
            internal TextureHandle mask;
            internal TextureHandle height;
            internal TextureHandle depth;
            internal TextureHandle blur;
        }

        readonly MaterialPropertyBlock m_Block = new MaterialPropertyBlock();

        Material m_Material;
        StyleTransparencyPass m_Transparency;
        StyleUnderwaterHeightPass m_Height;
        bool m_BaseReady;
        Vector4 m_Quality;

        internal StyleUnderwaterPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler("HB Style Underwater");
        }

        internal void Setup(Shader shader, StyleTransparencyPass transparency, StyleUnderwaterHeightPass height,
                           bool overlay, HBStyleTransparencyQualityLevel level)
        {
            m_Quality = level switch
            {
                HBStyleTransparencyQualityLevel.Low => new Vector4(0f, 0f, 0.5f, 0f),
                HBStyleTransparencyQualityLevel.Medium => new Vector4(4f, 8f, 0.5f, 0f),
                HBStyleTransparencyQualityLevel.High => new Vector4(8f, 16f, 1f, 0f),
                _ => new Vector4(12f, 16f, 1f, 0f)
            };

            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Transparency = transparency;
            m_Height = height;
            ConfigureInput(overlay ? ScriptableRenderPassInput.None : ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.renderType == CameraRenderType.Overlay)
            {
                RecordOverlay(renderGraph, frameData, cameraData);
                return;
            }

            m_BaseReady = false;

            if (m_Material == null || m_Height == null || !m_Height.ready || !HBStyleLiquid.any)
                return;

            var eye = cameraData.camera.transform.position;

            if (!Bind(eye, out var level))
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var colour = resourceData.activeColorTexture;
            var depth = resourceData.cameraDepthTexture;

            if (resourceData.isActiveTargetBackBuffer || !colour.IsValid() || !depth.IsValid())
                return;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var mask = RecordMask(renderGraph, colour, depth, renderingData, cameraData, lightData);
            var surface = renderGraph.ImportTexture(m_Height.map);
            var blur = RecordBlur(renderGraph, colour, cameraData, eye.y < level + BlurReach, "_HB_UnderBlur");

            m_Block.SetVector(s_MapId, m_Height.area);

            var descriptor = renderGraph.GetTextureDesc(colour);

            descriptor.name = "_HB_UnderwaterColour";
            descriptor.clearBuffer = false;

            var target = renderGraph.CreateTexture(descriptor);

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("HB Style Underwater", out var data,
                                                                               profilingSampler))
            {
                data.material = m_Material;
                data.block = m_Block;
                data.pass = BasePass;
                data.colour = colour;
                data.mask = mask;
                data.height = surface;
                data.blur = blur;

                builder.UseTexture(colour, AccessFlags.Read);
                builder.UseTexture(mask, AccessFlags.Read);
                builder.UseTexture(surface, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);

                if (blur.IsValid())
                    builder.UseTexture(blur, AccessFlags.Read);

                builder.SetRenderAttachment(target, 0);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CompositeData d, RasterGraphContext context) => Composite(d, context));
            }

            resourceData.cameraColor = target;
            m_BaseReady = true;
        }

        bool Bind(Vector3 eye, out float level)
        {
            if (!HBStyleLiquid.TryGetView(eye, out level, out var mix, out var slope, out var liquid) ||
                eye.y > level + StyleUnderwaterHeightPass.Band)
                return false;

            m_Block.Clear();
            HBStyleLiquid.BindLook(liquid, m_Block);
            StyleLiquidWaves.BindBaked(m_Block);

            var origin = liquid is HBStyleTerrain terrain ? terrain.transform.position : Vector3.zero;

            m_Block.SetVector(s_OriginId, new Vector4(origin.x, origin.y, origin.z, 1f));
            m_Block.SetVector(s_QualityId, m_Quality);
            m_Block.SetVector(s_ViewId, new Vector4(level, eye.y < level ? 1f : 0f, slope.x, slope.y));
            m_Block.SetVector(s_MixId, mix);
            m_Block.SetVector(s_AheadId, new Vector4(liquid is HBStyleTerrain ? 1f : 0f, 0f, 0f, 0f));
            return true;
        }

        TextureHandle RecordBlur(RenderGraph renderGraph, TextureHandle colour, UniversalCameraData cameraData,
                                 bool wanted, string name)
        {
            var blur = wanted && m_Transparency != null && m_Transparency.canBlur
                ? m_Transparency.RecordChain(renderGraph, colour, cameraData, name, false)
                : TextureHandle.nullHandle;

            m_Block.SetVector(s_QualityId, new Vector4(m_Quality.x, m_Quality.y, m_Quality.z, blur.IsValid() ? 1f : 0f));
            return blur;
        }

        static void Composite(CompositeData d, RasterGraphContext context)
        {
            d.block.SetTexture(s_ColourId, d.colour);
            d.block.SetTexture(s_HeightId, d.height);

            if (d.mask.IsValid())
                d.block.SetTexture(s_MaskId, d.mask);

            if (d.depth.IsValid())
                d.block.SetTexture(s_DepthId, d.depth);

            if (d.blur.IsValid())
                d.block.SetTexture(s_BlurId, d.blur);

            context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1, d.block);
        }

        void RecordOverlay(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData)
        {
            if (!m_BaseReady || m_Height == null || m_Height.map == null || m_Material == null || !HBStyleLiquid.any)
                return;

            var camera = cameraData.camera;

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additional) && !additional.clearDepth)
                return;

            var eye = camera.transform.position;

            if (!Bind(eye, out var level))
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var colour = resourceData.activeColorTexture;
            var depth = resourceData.activeDepthTexture;

            if (resourceData.isActiveTargetBackBuffer || !colour.IsValid() || !depth.IsValid())
                return;

            m_Block.SetVector(s_MapId, m_Height.area);

            var descriptor = renderGraph.GetTextureDesc(colour);

            descriptor.name = "_HB_UnderwaterOverlaySource";
            descriptor.clearBuffer = false;

            var copy = renderGraph.CreateTexture(descriptor);

            using (var builder = renderGraph.AddRasterRenderPass<CopyData>("HB Style Underwater Overlay Copy",
                                                                          out var data, profilingSampler))
            {
                data.source = colour;

                builder.UseTexture(colour, AccessFlags.Read);
                builder.SetRenderAttachment(copy, 0, AccessFlags.WriteAll);
                builder.SetRenderFunc(static (CopyData d, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, d.source, s_Whole, 0f, false);
                });
            }

            var surface = renderGraph.ImportTexture(m_Height.map);
            var blur = RecordBlur(renderGraph, copy, cameraData, eye.y < level + BlurReach, "_HB_UnderOverlayBlur");

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("HB Style Underwater Overlay",
                                                                               out var data, profilingSampler))
            {
                data.material = m_Material;
                data.block = m_Block;
                data.pass = OverlayPass;
                data.colour = copy;
                data.height = surface;
                data.depth = depth;
                data.blur = blur;

                builder.UseTexture(copy, AccessFlags.Read);
                builder.UseTexture(surface, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);

                if (blur.IsValid())
                    builder.UseTexture(blur, AccessFlags.Read);

                builder.SetRenderAttachment(colour, 0);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CompositeData d, RasterGraphContext context) => Composite(d, context));
            }
        }

        TextureHandle RecordMask(RenderGraph renderGraph, TextureHandle colour, TextureHandle depth,
                                 UniversalRenderingData renderingData, UniversalCameraData cameraData,
                                 UniversalLightData lightData)
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
            descriptor.name = "_HB_UnderMask";
            descriptor.format = GraphicsFormat.R32_SFloat;

            var mask = renderGraph.CreateTexture(descriptor);

            descriptor.name = "_HB_UnderMaskDepth";
            descriptor.depthBufferBits = DepthBits.Depth32;

            var maskDepth = renderGraph.CreateTexture(descriptor);

            using var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Underwater Mask", out var data,
                                                                         profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_MaskTag, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);
            var filtering = new FilteringSettings(RenderQueueRange.transparent, cameraData.camera.cullingMask);

            data.list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                              filtering));
            data.cut = m_Transparency != null && m_Transparency.liquidCut;

            builder.UseRendererList(data.list);
            builder.UseTexture(depth, AccessFlags.Read);
            builder.SetRenderAttachment(mask, 0);
            builder.SetRenderAttachmentDepth(maskDepth, AccessFlags.Write);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalVector(s_CutStateId, d.cut ? s_CutOn : Vector4.zero);
                context.cmd.DrawRendererList(d.list);
                context.cmd.SetGlobalVector(s_CutStateId, Vector4.zero);
            });

            return mask;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            m_Transparency = null;
            m_Height = null;
            m_BaseReady = false;
        }
    }
}
