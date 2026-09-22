using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleGlobalsPass : ScriptableRenderPass
    {
        static readonly int s_SkyId = Shader.PropertyToID("_HB_AmbientSky");
        static readonly int s_HorizonId = Shader.PropertyToID("_HB_AmbientHorizon");
        static readonly int s_GroundId = Shader.PropertyToID("_HB_AmbientGround");
        static readonly int s_AmbientParamsId = Shader.PropertyToID("_HB_AmbientParams");
        static readonly int s_BrushAtlasId = Shader.PropertyToID("_HB_BrushAtlas");
        static readonly int s_BrushParamsId = Shader.PropertyToID("_HB_BrushParams");

        class PassData
        {
            public Vector4 sky;
            public Vector4 horizon;
            public Vector4 ground;
            public Vector4 ambientParams;
            public Texture brushAtlas;
            public Vector4 brushParams;
        }

        BrushGlobalSettings m_Brush;

        public StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            profilingSampler = new ProfilingSampler("HiddenBull Style Globals");
        }

        public void Setup(BrushGlobalSettings brush)
        {
            m_Brush = brush;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var ambient = VolumeManager.instance.stack.GetComponent<StyleAmbient>();

            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            PackAmbient(ambient, passData);
            PackBrush(m_Brush, passData);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                cmd.SetGlobalVector(s_SkyId, data.sky);
                cmd.SetGlobalVector(s_HorizonId, data.horizon);
                cmd.SetGlobalVector(s_GroundId, data.ground);
                cmd.SetGlobalVector(s_AmbientParamsId, data.ambientParams);

                if (data.brushAtlas != null)
                    cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);

                cmd.SetGlobalVector(s_BrushParamsId, data.brushParams);
            });
        }

        static void PackAmbient(StyleAmbient ambient, PassData passData)
        {
            if (ambient == null)
            {
                passData.sky = StyleGlobalDefaults.Sky;
                passData.horizon = StyleGlobalDefaults.Horizon;
                passData.ground = StyleGlobalDefaults.Ground;
                passData.ambientParams = StyleGlobalDefaults.AmbientParams;
                return;
            }

            passData.sky = ambient.skyColor.value;
            passData.horizon = ambient.horizonColor.value;
            passData.ground = ambient.groundColor.value;
            passData.ambientParams = new Vector4(
                ambient.skyFalloff.value,
                ambient.groundFalloff.value,
                ambient.intensity.value,
                ambient.bakedWeight.value);
        }

        static void PackBrush(BrushGlobalSettings brush, PassData passData)
        {
            if (brush == null || brush.atlas == null)
            {
                passData.brushAtlas = null;
                passData.brushParams = StyleGlobalDefaults.BrushParams;
                return;
            }

            passData.brushAtlas = brush.atlas;
            passData.brushParams = brush.Pack();
        }
    }

    public static class StyleGlobalDefaults
    {
        public static readonly Vector4 Sky = new Vector4(1f, 0.938866f, 0.891509f, 1f);
        public static readonly Vector4 Horizon = new Vector4(0.537736f, 0.537736f, 0.537736f, 1f);
        public static readonly Vector4 Ground = new Vector4(0.339623f, 0.339623f, 0.339623f, 1f);
        public static readonly Vector4 AmbientParams = new Vector4(0.6f, 0.8f, 1f, 0f);

        public static readonly Vector4 BrushParams = new Vector4(1f, 15f, 1f / 15f, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Apply()
        {
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientSky"), Sky);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientHorizon"), Horizon);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientGround"), Ground);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientParams"), AmbientParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_BrushParams"), BrushParams);
        }
    }
}
