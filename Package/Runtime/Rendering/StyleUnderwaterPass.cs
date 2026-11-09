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

        const string MapName = "_HB_UnderHeight";
        const float Band = 2f;
        const float MapHalf = 1f;
        const int MapSize = 64;
        const float Empty = -10000f;
        const int BasePass = 0;
        const int OverlayPass = 1;

        static readonly ShaderTagId s_MaskTag = new ShaderTagId("HBStyleLiquidMask");
        static readonly ShaderTagId s_HeightTag = new ShaderTagId("HBStyleLiquidHeight");

        static readonly int s_MapId = Shader.PropertyToID("_HB_UnderMap");
        static readonly int s_ViewId = Shader.PropertyToID("_HB_UnderView");
        static readonly int s_MixId = Shader.PropertyToID("_HB_UnderMix");
        static readonly int s_ColourId = Shader.PropertyToID("_HB_UnderColour");
        static readonly int s_MaskId = Shader.PropertyToID("_HB_UnderMask");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_UnderHeight");
        static readonly int s_DepthId = Shader.PropertyToID("_HB_UnderDepth");
        static readonly int s_CutStateId = Shader.PropertyToID("_HB_LiquidCutState");
        static readonly int s_QualityId = Shader.PropertyToID("_HB_UnderQuality");
        static readonly int s_OriginId = Shader.PropertyToID("_HB_TerrainOrigin");

        static readonly Vector4 s_CutOn = new Vector4(1f, 0f, 0f, 0f);
        static readonly Vector4 s_Whole = new Vector4(1f, 1f, 0f, 0f);
        static readonly Color s_Empty = new Color(Empty, Empty, Empty, Empty);

        class DrawData
        {
            internal RendererListHandle list;
            internal bool cut;
            internal Vector4 map;
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
        }

        readonly MaterialPropertyBlock m_Block = new MaterialPropertyBlock();

        Material m_Material;
        StyleTransparencyPass m_Transparency;
        RTHandle m_Map;
        Vector4 m_BaseMap;
        bool m_BaseReady;
        Vector4 m_Quality;

        internal StyleUnderwaterPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler("HB Style Underwater");
        }

        internal void Setup(Shader shader, StyleTransparencyPass transparency, bool overlay,
                           HBStyleTransparencyQualityLevel level)
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

            if (m_Material == null || !HBStyleLiquid.any)
                return;

            var eye = cameraData.camera.transform.position;

            if (!Bind(eye))
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var colour = resourceData.activeColorTexture;
            var depth = resourceData.cameraDepthTexture;

            if (resourceData.isActiveTargetBackBuffer || !colour.IsValid() || !depth.IsValid())
                return;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var map = new Vector4(eye.x, eye.z, 1f / MapHalf, MapHalf);
            var mask = RecordMask(renderGraph, colour, depth, renderingData, cameraData, lightData);
            var surface = RecordHeight(renderGraph, map, renderingData, cameraData, lightData);

            m_Block.SetVector(s_MapId, map);

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

                builder.UseTexture(colour, AccessFlags.Read);
                builder.UseTexture(mask, AccessFlags.Read);
                builder.UseTexture(surface, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);
                builder.SetRenderAttachment(target, 0);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CompositeData d, RasterGraphContext context) => Composite(d, context));
            }

            resourceData.cameraColor = target;
            m_BaseMap = map;
            m_BaseReady = true;
        }

        bool Bind(Vector3 eye)
        {
            if (!HBStyleLiquid.TryGetView(eye, out var height, out var mix, out var slope, out var liquid) ||
                eye.y > height + Band)
                return false;

            m_Block.Clear();
            HBStyleLiquid.BindLook(liquid, m_Block);
            StyleLiquidWaves.BindBaked(m_Block);

            var origin = liquid is HBStyleTerrain terrain ? terrain.transform.position : Vector3.zero;

            m_Block.SetVector(s_OriginId, new Vector4(origin.x, origin.y, origin.z, 1f));
            m_Block.SetVector(s_QualityId, m_Quality);
            m_Block.SetVector(s_ViewId, new Vector4(height, eye.y < height ? 1f : 0f, slope.x, slope.y));
            m_Block.SetVector(s_MixId, mix);
            return true;
        }

        static void Composite(CompositeData d, RasterGraphContext context)
        {
            d.block.SetTexture(s_ColourId, d.colour);
            d.block.SetTexture(s_HeightId, d.height);

            if (d.mask.IsValid())
                d.block.SetTexture(s_MaskId, d.mask);

            if (d.depth.IsValid())
                d.block.SetTexture(s_DepthId, d.depth);

            context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1, d.block);
        }

        void RecordOverlay(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData)
        {
            if (!m_BaseReady || m_Map == null || m_Material == null || !HBStyleLiquid.any)
                return;

            var camera = cameraData.camera;

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additional) && !additional.clearDepth)
                return;

            if (!Bind(camera.transform.position))
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var colour = resourceData.activeColorTexture;
            var depth = resourceData.activeDepthTexture;

            if (resourceData.isActiveTargetBackBuffer || !colour.IsValid() || !depth.IsValid())
                return;

            m_Block.SetVector(s_MapId, m_BaseMap);

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

            var surface = renderGraph.ImportTexture(m_Map);

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("HB Style Underwater Overlay",
                                                                               out var data, profilingSampler))
            {
                data.material = m_Material;
                data.block = m_Block;
                data.pass = OverlayPass;
                data.colour = copy;
                data.height = surface;
                data.depth = depth;

                builder.UseTexture(copy, AccessFlags.Read);
                builder.UseTexture(surface, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);
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

            data.list = renderGraph.CreateRendererList(List(s_MaskTag, renderingData, cameraData, lightData));
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

        TextureHandle RecordHeight(RenderGraph renderGraph, Vector4 map, UniversalRenderingData renderingData,
                                   UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var descriptor = new TextureDesc(MapSize, MapSize)
            {
                name = MapName,
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_Map, descriptor, MapName);

            var surface = renderGraph.ImportTexture(m_Map);

            using var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Underwater Height", out var data,
                                                                         profilingSampler);

            data.list = renderGraph.CreateRendererList(List(s_HeightTag, renderingData, cameraData, lightData));
            data.map = map;

            builder.UseRendererList(data.list);
            builder.SetRenderAttachment(surface, 0, AccessFlags.WriteAll);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(false, true, s_Empty);
                context.cmd.SetGlobalVector(s_MapId, d.map);
                context.cmd.DrawRendererList(d.list);
            });

            return surface;
        }

        static RendererListParams List(ShaderTagId tag, UniversalRenderingData renderingData,
                                       UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var drawing = RenderingUtils.CreateDrawingSettings(tag, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);
            var filtering = new FilteringSettings(RenderQueueRange.transparent, cameraData.camera.cullingMask);

            return new RendererListParams(renderingData.cullResults, drawing, filtering);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Map?.Release();
            m_Material = null;
            m_Map = null;
            m_Transparency = null;
            m_BaseReady = false;
        }
    }
}
