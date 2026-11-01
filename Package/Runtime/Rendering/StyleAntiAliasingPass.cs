using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleAntiAliasingPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AntiAliasing/AntiAliasing.shader";

        static readonly int s_SourceId = Shader.PropertyToID("_HB_AntiAliasingSource");
        static readonly int s_StyleId = Shader.PropertyToID("_HB_AntiAliasingStyle");

        class PassData
        {
            internal Material material;
            internal MaterialPropertyBlock block;
            internal TextureHandle source;
            internal Vector4 style;
        }

        readonly MaterialPropertyBlock m_Block = new MaterialPropertyBlock();

        Material m_Material;
        HBStyleAntiAliasingQuality m_Quality;

        internal StyleAntiAliasingPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
            profilingSampler = new ProfilingSampler("HB Style Anti Aliasing");
        }

        internal void Setup(Shader shader, HBStyleAntiAliasingQuality quality)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Quality = quality;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Quality == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            var description = renderGraph.GetTextureDesc(source);

            description.name = "HB Style Anti Aliasing";
            description.clearBuffer = false;
            description.msaaSamples = MSAASamples.None;

            var target = renderGraph.CreateTexture(description);
            var settings = m_Quality.current;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("HB Style Anti Aliasing", out var data,
                                                                           profilingSampler))
            {
                data.material = m_Material;
                data.block = m_Block;
                data.source = source;
                data.style = new Vector4(settings.edgeSteps, settings.softness, 0f, 0f);

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(target, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                {
                    d.block.SetTexture(s_SourceId, d.source);
                    d.block.SetVector(s_StyleId, d.style);
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1, d.block);
                });
            }

            resourceData.cameraColor = target;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
