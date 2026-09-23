using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class BrushDistortionPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/URP Style/Brush Distortion";

        Material m_Material;

        public BrushDistortionPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("HiddenBull Brush Distortion");

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public bool Setup(Shader shader)
        {
            if (shader == null)
                return false;

            if (m_Material == null || m_Material.shader != shader)
            {
                CoreUtils.Destroy(m_Material);
                m_Material = CoreUtils.CreateEngineMaterial(shader);
            }

            return m_Material != null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var resources = frameData.Get<UniversalResourceData>();

            if (resources.isActiveTargetBackBuffer)
                return;

            var source = resources.activeColorTexture;

            var description = renderGraph.GetTextureDesc(source);
            description.name = "HB Brush Distortion";
            description.clearBuffer = false;
            description.depthBufferBits = 0;
            description.msaaSamples = MSAASamples.None;
            description.bindTextureMS = false;

            var destination = renderGraph.CreateTexture(description);

            renderGraph.AddBlitPass(
                new RenderGraphUtils.BlitMaterialParameters(source, destination, m_Material, 0),
                passName);

            resources.cameraColor = destination;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
