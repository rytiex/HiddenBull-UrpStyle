using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    public enum StyleFogDebug
    {
        Off,
        Reach,
        ReachRaw,
        Amount,
        Sunlit,
        Scattering
    }

    sealed class StyleFogPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/Style Fog";

        const int ReachPass = 0;
        const int CompositePass = 1;
        const int Divisor = 2;

        static readonly int s_ReachId = Shader.PropertyToID("_HB_FogReach");
        static readonly int s_ReachSizeId = Shader.PropertyToID("_HB_FogReachSize");
        static readonly int s_DebugId = Shader.PropertyToID("_HB_FogDebug");

        Material m_Material;
        StyleFogDebug m_Debug;

        public StyleFogPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            profilingSampler = new ProfilingSampler("HiddenBull Style Fog");

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(Shader shader, StyleFogDebug debug)
        {
            m_Debug = debug;

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

        class PassData
        {
            public Material material;
            public Vector4 reachSize;
            public float debug;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var stack = VolumeManager.instance.stack;
            var fog = stack.GetComponent<StyleFog>();

            if (fog == null || fog.endDistance.value <= fog.startDistance.value)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.width = Mathf.Max(1, descriptor.width / Divisor);
            descriptor.height = Mathf.Max(1, descriptor.height / Divisor);
            descriptor.depthStencilFormat = GraphicsFormat.None;
            descriptor.msaaSamples = 1;
            descriptor.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Blend)
                ? GraphicsFormat.R8_UNorm
                : GraphicsFormat.B8G8R8A8_UNorm;

            var reach = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, descriptor, "_HB_FogReach", false, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                       "HiddenBull Style Fog Reach", out var passData, profilingSampler))
            {
                passData.material = m_Material;

                builder.SetRenderAttachment(reach, 0, AccessFlags.WriteAll);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(reach, s_ReachId);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, ReachPass);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                       "HiddenBull Style Fog Composite", out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.reachSize = new Vector4(
                    1f / descriptor.width, 1f / descriptor.height, descriptor.width, descriptor.height);
                passData.debug = (int)m_Debug;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.UseTexture(reach, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_ReachSizeId, data.reachSize);
                    context.cmd.SetGlobalFloat(s_DebugId, data.debug);

                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, CompositePass);
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
