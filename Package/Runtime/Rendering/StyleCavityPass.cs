using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleCavityPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Cavity/Cavity.shader";

        const int DebugPass = 1;

        class PassData
        {
            internal Material material;
            internal int pass;
        }

        Material m_Material;

        internal StyleCavityPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            profilingSampler = new ProfilingSampler("HB Style Cavity");
        }

        internal void Setup(Shader shader)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var depth = resourceData.cameraDepthTexture;
            var normals = resourceData.cameraNormalsTexture;

            if (!depth.IsValid() || !normals.IsValid())
                return;

            using var builder = renderGraph.AddRasterRenderPass<PassData>("HB Style Cavity", out var data,
                                                                         profilingSampler);

            data.material = m_Material;
            data.pass = StyleDebug.view == StyleDebugView.Cavity ? DebugPass : 0;

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.UseTexture(depth, AccessFlags.Read);
            builder.UseTexture(normals, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3));
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
