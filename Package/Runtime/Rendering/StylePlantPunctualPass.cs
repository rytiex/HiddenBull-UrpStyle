using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StylePlantPunctualPass : ScriptableRenderPass
    {
        class PassData
        {
            internal readonly Vector4[] lights = new Vector4[StylePlantSystem.LightLimit];
            internal int count;
            internal float distance;
        }

        readonly Vector4[] m_Lights = new Vector4[StylePlantSystem.LightLimit];

        internal StylePlantPunctualPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Plant Punctual Cull");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            if (!StylePlantSystem.Casts(cameraData.camera))
                return;

            var lightData = frameData.Get<UniversalLightData>();
            var shadowData = frameData.Get<UniversalShadowData>();

            if (!shadowData.supportsAdditionalLightShadows)
                return;

            var count = Collect(lightData);

            if (count == 0)
                return;

            using var builder = renderGraph.AddUnsafePass<PassData>("HB Style Plant Punctual Cull", out var data,
                                                                    profilingSampler);

            data.count = count;
            data.distance = cameraData.maxShadowDistance;
            System.Array.Copy(m_Lights, data.lights, StylePlantSystem.LightLimit);

            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, UnsafeGraphContext context) =>
                StylePlantSystem.CullPunctual(context.cmd, d.lights, d.count, d.distance));
        }

        int Collect(UniversalLightData lightData)
        {
            var visibleLights = lightData.visibleLights;
            var count = 0;

            for (var i = 0; i < visibleLights.Length && count < StylePlantSystem.LightLimit; i++)
            {
                var visibleLight = visibleLights[i];
                var light = visibleLight.light;

                if (i == lightData.mainLightIndex || light == null || light.shadows == LightShadows.None ||
                    visibleLight.lightType != LightType.Point && visibleLight.lightType != LightType.Spot)
                    continue;

                var position = visibleLight.localToWorldMatrix.GetColumn(3);

                m_Lights[count++] = new Vector4(position.x, position.y, position.z, visibleLight.range);
            }

            return count;
        }
    }
}
