using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleGlobalsPass : ScriptableRenderPass, IDisposable
    {
        static readonly int s_FrameId = Shader.PropertyToID(StyleFrameGlobals.BufferName);
        static readonly int s_BrushAtlasId = Shader.PropertyToID("_HB_BrushAtlas");
        static readonly int s_CloudAtlasId = Shader.PropertyToID("_HB_CloudAtlas");
        static readonly int s_SkyLutId = Shader.PropertyToID("_HB_SkyLut");

        readonly ConstantBuffer<StyleFrameGlobals> m_Buffer = new ConstantBuffer<StyleFrameGlobals>();
        readonly StyleEnvironmentMixer m_Mixer = new StyleEnvironmentMixer();
        readonly StyleProfileMix m_SharedMix = new StyleProfileMix();
        readonly StyleSkyLut m_SharedMixLut = new StyleSkyLut();

        HBStyleQualityProfile m_Quality;
        StyleCameraStates m_Cameras;

        class PassData
        {
            public ConstantBuffer<StyleFrameGlobals> buffer;
            public StyleFrameGlobals globals;
            public Texture brushAtlas;
            public Texture cloudAtlas;
            public Texture skyLut;
        }

        internal StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Globals");
        }

        internal void Setup(HBStyleQualityProfile quality, StyleCameraStates cameras)
        {
            m_Quality = quality;
            m_Cameras = cameras;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var cascades = frameData.Get<UniversalShadowData>().mainLightShadowCascadesCount;

            var state = CameraState(cameraData);
            var mix = state != null ? state.mix : m_SharedMix;
            var mixLut = state != null ? state.mixLut : m_SharedMixLut;

            mix.Evaluate(state != null ? state.camera.transform.position : (Vector3?)null);
            m_Mixer.Prune();

            var resolved = m_Mixer.Resolve(mix, mixLut, m_Quality, cascades);

            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData, profilingSampler);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            passData.buffer = m_Buffer;
            passData.globals = resolved.globals;
            passData.brushAtlas = resolved.brushAtlas != null ? resolved.brushAtlas : Texture2D.grayTexture;
            passData.cloudAtlas = resolved.cloudAtlas != null ? resolved.cloudAtlas : Texture2D.blackTexture;
            passData.skyLut = resolved.skyLut;

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                data.buffer.PushGlobal(context.cmd, data.globals, s_FrameId);
                context.cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);
                context.cmd.SetGlobalTexture(s_CloudAtlasId, data.cloudAtlas);
                context.cmd.SetGlobalTexture(s_SkyLutId, data.skyLut);
            });
        }

        StyleCameraState CameraState(UniversalCameraData cameraData)
        {
            if (m_Cameras == null)
                return null;

            return StyleCameraPolicy.Classify(cameraData.cameraType, cameraData.renderType) switch
            {
                StyleCameraKind.Base => m_Cameras.Find(cameraData.camera),
                StyleCameraKind.Overlay => m_Cameras.Find(cameraData.baseCamera),
                _ => null
            };
        }

        public void Dispose()
        {
            m_Buffer.Release();
            m_Mixer.Dispose();
            m_SharedMixLut.Dispose();
        }
    }
}
