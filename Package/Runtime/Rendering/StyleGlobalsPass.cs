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
        StyleShelterPass m_Shelter;

        class PassData
        {
            public ConstantBuffer<StyleFrameGlobals> buffer;
            public StyleFrameGlobals globals;
            public Texture brushAtlas;
            public Texture cloudAtlas;
            public Texture skyLut;
            public bool resetShelter;
            public bool bindShelter;
            public bool noUnderside;
            public StyleShelterPass.Binding shelter;
        }

        internal StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Globals");
        }

        internal void Setup(HBStyleQualityProfile quality, StyleCameraStates cameras, StyleShelterPass shelter)
        {
            m_Quality = quality;
            m_Cameras = cameras;
            m_Shelter = shelter;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var cascades = frameData.Get<UniversalShadowData>().mainLightShadowCascadesCount;

            var kind = StyleCameraPolicy.Classify(cameraData.cameraType, cameraData.renderType);
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
            passData.bindShelter = false;

            if (kind == StyleCameraKind.Overlay && m_Shelter != null &&
                m_Shelter.TryGetMap(cameraData.baseCamera, out var shelter))
            {
                var depth = Bind(renderGraph, builder, shelter.depth, StyleShelterPass.depthId);

                Bind(renderGraph, builder, shelter.under, StyleShelterPass.underId);
                Bind(renderGraph, builder, shelter.far, StyleShelterPass.farDepthId);

                if (shelter.drops != null && shelter.drops != shelter.depth)
                    Bind(renderGraph, builder, shelter.drops, StyleShelterPass.dropsId);
                else if (depth.IsValid())
                    builder.SetGlobalTextureAfterPass(depth, StyleShelterPass.dropsId);

                passData.noUnderside = shelter.under == null;
                passData.bindShelter = true;
                passData.shelter = shelter;
            }

            passData.resetShelter = kind != StyleCameraKind.Overlay || !passData.bindShelter;

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                data.buffer.PushGlobal(context.cmd, data.globals, s_FrameId);
                context.cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);
                context.cmd.SetGlobalTexture(s_CloudAtlasId, data.cloudAtlas);
                context.cmd.SetGlobalTexture(s_SkyLutId, data.skyLut);

                if (data.bindShelter)
                {
                    if (data.noUnderside)
                        context.cmd.SetGlobalTexture(StyleShelterPass.underId, Texture2D.blackTexture);

                    context.cmd.SetGlobalMatrix(StyleShelterPass.viewProjectionId, data.shelter.viewProjection);
                    context.cmd.SetGlobalVector(StyleShelterPass.heightId, data.shelter.height);
                    context.cmd.SetGlobalMatrix(StyleShelterPass.farViewProjectionId, data.shelter.farViewProjection);
                    context.cmd.SetGlobalVector(StyleShelterPass.farHeightId, data.shelter.farHeight);
                    context.cmd.SetGlobalVector(StyleShelterPass.stateId,
                                                StyleShelterPass.State(data.shelter.far != null));
                }
                else if (data.resetShelter)
                {
                    context.cmd.SetGlobalVector(StyleShelterPass.stateId, Vector4.zero);
                }
            });
        }

        static TextureHandle Bind(RenderGraph renderGraph, IUnsafeRenderGraphBuilder builder, RTHandle texture, int id)
        {
            if (texture == null)
                return TextureHandle.nullHandle;

            var handle = renderGraph.ImportTexture(texture);

            builder.UseTexture(handle, AccessFlags.Read);
            builder.SetGlobalTextureAfterPass(handle, id);
            return handle;
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
