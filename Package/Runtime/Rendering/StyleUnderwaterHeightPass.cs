using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleUnderwaterHeightPass : ScriptableRenderPass, IDisposable
    {
        internal const float Band = 2f;

        const string MapName = "_HB_UnderHeight";
        const float MapHalf = 1f;
        const int MapSize = 64;
        const float Empty = -10000f;

        static readonly ShaderTagId s_HeightTag = new ShaderTagId("HBStyleLiquidHeight");

        static readonly int s_MapId = Shader.PropertyToID("_HB_UnderMap");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_UnderHeight");

        static readonly Color s_Empty = new Color(Empty, Empty, Empty, Empty);

        class DrawData
        {
            internal RendererListHandle list;
            internal Vector4 map;
        }

        RTHandle m_Map;

        internal StyleUnderwaterHeightPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            profilingSampler = new ProfilingSampler("HB Style Underwater Height");
        }

        internal bool ready { get; private set; }

        internal RTHandle map => m_Map;

        internal Vector4 area { get; private set; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            ready = false;

            if (!HBStyleLiquid.any)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var eye = cameraData.camera.transform.position;

            if (!HBStyleLiquid.TryGetView(eye, out var height, out _, out _, out _) || eye.y > height + Band)
                return;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var descriptor = new TextureDesc(MapSize, MapSize)
            {
                name = MapName,
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_Map, descriptor, MapName);

            var surface = renderGraph.ImportTexture(m_Map);
            var drawing = RenderingUtils.CreateDrawingSettings(s_HeightTag, renderingData, cameraData, lightData,
                                                               SortingCriteria.None);
            var filtering = new FilteringSettings(RenderQueueRange.transparent, cameraData.camera.cullingMask);

            area = new Vector4(eye.x, eye.z, 1f / MapHalf, MapHalf);

            using var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Underwater Height", out var data,
                                                                         profilingSampler);

            data.list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing,
                                                                              filtering));
            data.map = area;

            builder.UseRendererList(data.list);
            builder.SetRenderAttachment(surface, 0, AccessFlags.WriteAll);
            builder.SetGlobalTextureAfterPass(surface, s_HeightId);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(false, true, s_Empty);
                context.cmd.SetGlobalVector(s_MapId, d.map);
                context.cmd.DrawRendererList(d.list);
            });

            ready = true;
        }

        public void Dispose()
        {
            m_Map?.Release();
            m_Map = null;
            ready = false;
        }
    }
}
