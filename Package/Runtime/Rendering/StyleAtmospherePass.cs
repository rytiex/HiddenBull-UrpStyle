using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleAtmospherePass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Atmosphere/Atmosphere.shader";

        class PassData
        {
            internal Material material;
        }

        Material m_Material;
        HBStyleAtmosphereQuality m_Quality;

        internal StyleAtmospherePass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            profilingSampler = new ProfilingSampler("HB Style Atmosphere");
        }

        internal void Setup(Shader shader, HBStyleAtmosphereQuality quality)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Quality = quality;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Quality == null || m_Quality.level == HBStyleQualityLevel.Off)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var depth = resourceData.cameraDepthTexture;

            if (!depth.IsValid())
                return;

            using var builder = renderGraph.AddRasterRenderPass<PassData>("HB Style Atmosphere", out var data,
                                                                         profilingSampler);

            data.material = m_Material;

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.UseTexture(depth, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3));
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
