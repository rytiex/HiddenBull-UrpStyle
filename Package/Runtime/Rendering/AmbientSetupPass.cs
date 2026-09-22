using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// Publishes the gradient ambient model to global shader constants, once per camera, before
    /// anything is drawn.
    ///
    /// These are set through a Render Graph pass rather than with <see cref="Shader.SetGlobalVector"/>
    /// from script, so the values are correct per camera. Setting them outside the graph would leak
    /// one camera's ambient into the next when several cameras render in a frame — a bug that only
    /// shows up once a project adds a second camera, which is exactly when it is hardest to find.
    /// </summary>
    sealed class AmbientSetupPass : ScriptableRenderPass
    {
        static readonly int s_SkyId = Shader.PropertyToID("_HB_AmbientSky");
        static readonly int s_HorizonId = Shader.PropertyToID("_HB_AmbientHorizon");
        static readonly int s_GroundId = Shader.PropertyToID("_HB_AmbientGround");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_AmbientParams");

        class PassData
        {
            public Vector4 sky;
            public Vector4 horizon;
            public Vector4 ground;
            public Vector4 parameters;
        }

        public AmbientSetupPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            profilingSampler = new ProfilingSampler("HiddenBull Ambient Setup");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var ambient = VolumeManager.instance.stack.GetComponent<StyleAmbient>();

            // passName rather than profilingSampler: the sampler is null in release builds.
            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);

            // Nothing reads the outputs of this pass through the graph, so without this the pass
            // would be culled and every shader would sample an ambient of zero.
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            Pack(ambient, passData);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                cmd.SetGlobalVector(s_SkyId, data.sky);
                cmd.SetGlobalVector(s_HorizonId, data.horizon);
                cmd.SetGlobalVector(s_GroundId, data.ground);
                cmd.SetGlobalVector(s_ParamsId, data.parameters);
            });
        }

        static void Pack(StyleAmbient ambient, PassData passData)
        {
            if (ambient == null)
            {
                passData.sky = AmbientDefaults.Sky;
                passData.horizon = AmbientDefaults.Horizon;
                passData.ground = AmbientDefaults.Ground;
                passData.parameters = AmbientDefaults.Parameters;
                return;
            }

            passData.sky = ambient.skyColor.value;
            passData.horizon = ambient.horizonColor.value;
            passData.ground = ambient.groundColor.value;
            passData.parameters = new Vector4(
                ambient.skyFalloff.value,
                ambient.groundFalloff.value,
                ambient.intensity.value,
                ambient.bakedWeight.value);
        }
    }

    /// <summary>
    /// Fallback ambient written at startup.
    ///
    /// Without this, a project that has not yet added <see cref="HiddenBullStyleFeature"/> to its
    /// renderer would sample an ambient of zero and render everything black — which reads as "the
    /// package is broken" rather than "a step is missing". Flat but lit is a far more legible
    /// failure than black.
    /// </summary>
    public static class AmbientDefaults
    {
        public static readonly Vector4 Sky = new Vector4(0.25f, 0.42f, 0.65f, 1f);
        public static readonly Vector4 Horizon = new Vector4(0.55f, 0.45f, 0.38f, 1f);
        public static readonly Vector4 Ground = new Vector4(0.18f, 0.15f, 0.14f, 1f);
        public static readonly Vector4 Parameters = new Vector4(0.6f, 0.8f, 1f, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Apply()
        {
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientSky"), Sky);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientHorizon"), Horizon);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientGround"), Ground);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientParams"), Parameters);
        }
    }
}
