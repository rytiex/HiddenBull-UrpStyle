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
        static readonly int s_BodiesId = Shader.PropertyToID("_HB_WeatherBodies");
        static readonly int s_OverlayShadowId = Shader.PropertyToID("_HB_OverlayShadow");

        readonly ConstantBuffer<StyleFrameGlobals> m_Buffer = new ConstantBuffer<StyleFrameGlobals>();
        readonly StyleEnvironmentMixer m_Mixer = new StyleEnvironmentMixer();
        readonly StyleProfileMix m_SharedMix = new StyleProfileMix();
        readonly StyleProfileMix m_PreviewMix = new StyleProfileMix();
        readonly StyleSkyLut m_SharedMixLut = new StyleSkyLut();

        HBStyleQualityProfile m_Quality;
        StyleCameraStates m_Cameras;
        StyleShelterPass m_Shelter;
        GraphicsBuffer m_Bodies;
        float m_OverlayShadowNear;

        class PassData
        {
            public ConstantBuffer<StyleFrameGlobals> buffer;
            public StyleFrameGlobals globals;
            public Texture brushAtlas;
            public Texture cloudAtlas;
            public Texture skyLut;
            public bool resetShelter;
            public bool bindShelter;
            public Vector4[] shelterAreas;
            public Vector4 shelterState;
            public Texture noShelter;
            public GraphicsBuffer bodies;
            public Vector4[] bodyStates;
            public int bodyCount;
            public float overlayShadow;
        }

        internal StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Globals");
        }

        internal void Setup(HBStyleQualityProfile quality, StyleCameraStates cameras, StyleShelterPass shelter,
                            float overlayShadowNear)
        {
            m_OverlayShadowNear = overlayShadowNear;
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
            var preview = kind == StyleCameraKind.Isolated ? StylePreviewEnvironment.Find(cameraData.camera) : null;
            var mix = state != null ? state.mix : preview != null ? m_PreviewMix : m_SharedMix;
            var mixLut = state != null ? state.mixLut : m_SharedMixLut;
            StyleMixResult resolved;

            if (preview != null)
                StyleEnvironment.BeginPreview(preview);

            try
            {
                mix.Evaluate(state != null ? state.camera.transform.position : (Vector3?)null);
                m_Mixer.Prune();

                resolved = m_Mixer.Resolve(mix, mixLut, m_Quality, cascades);
            }
            finally
            {
                StyleEnvironment.EndPreview();
            }

            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData, profilingSampler);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            passData.buffer = m_Buffer;
            passData.globals = resolved.globals;
            passData.brushAtlas = resolved.brushAtlas != null ? resolved.brushAtlas : Texture2D.grayTexture;
            passData.cloudAtlas = resolved.cloudAtlas != null ? resolved.cloudAtlas : Texture2D.blackTexture;
            passData.skyLut = resolved.skyLut;
            passData.bindShelter = false;
            passData.overlayShadow = kind == StyleCameraKind.Overlay
                ? m_OverlayShadowNear * m_OverlayShadowNear
                : 0f;

            var states = StyleWeatherBodies.states;

            if (m_Bodies == null || m_Bodies.count != states.Length)
            {
                m_Bodies?.Release();
                m_Bodies = new GraphicsBuffer(GraphicsBuffer.Target.Structured, states.Length, sizeof(float) * 4);
            }

            passData.bodies = m_Bodies;
            passData.bodyStates = states;
            passData.bodyCount = StyleWeatherBodies.slotCount;

            passData.shelterAreas ??= new Vector4[StyleShelterPass.Cascades];
            passData.noShelter = StyleShelterPass.empty;

            if (kind == StyleCameraKind.Overlay && m_Shelter != null &&
                m_Shelter.TryGetMap(cameraData.baseCamera, out var shelter))
            {
                Bind(renderGraph, builder, shelter.map, StyleShelterPass.mapId);
                Bind(renderGraph, builder, shelter.drops, StyleShelterPass.dropsId);
                Bind(renderGraph, builder, shelter.age, StyleShelterPass.ageId);
                Array.Copy(shelter.areas, passData.shelterAreas, StyleShelterPass.Cascades);
                passData.shelterState = shelter.state;
                passData.bindShelter = true;
            }

            passData.resetShelter = kind != StyleCameraKind.Overlay || !passData.bindShelter;

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                data.buffer.PushGlobal(context.cmd, data.globals, s_FrameId);
                context.cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);
                context.cmd.SetGlobalTexture(s_CloudAtlasId, data.cloudAtlas);
                context.cmd.SetGlobalTexture(s_SkyLutId, data.skyLut);
                context.cmd.SetGlobalFloat(s_OverlayShadowId, data.overlayShadow);

                if (data.bodyCount > 0)
                    context.cmd.SetBufferData(data.bodies, data.bodyStates, 0, 0, data.bodyCount);

                context.cmd.SetGlobalBuffer(s_BodiesId, data.bodies);

                if (data.bindShelter)
                {
                    context.cmd.SetGlobalVectorArray(StyleShelterPass.areaId, data.shelterAreas);
                    context.cmd.SetGlobalVector(StyleShelterPass.stateId, data.shelterState);
                }
                else if (data.resetShelter)
                {
                    context.cmd.SetGlobalTexture(StyleShelterPass.mapId, data.noShelter);
                    context.cmd.SetGlobalTexture(StyleShelterPass.ageId, Texture2D.whiteTexture);
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
            m_Bodies?.Release();
            m_Bodies = null;
            m_Mixer.Dispose();
            m_SharedMixLut.Dispose();
        }
    }
}
