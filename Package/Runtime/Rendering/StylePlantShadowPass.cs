using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StylePlantShadowPass : ScriptableRenderPass
    {
        const int MaxCascades = StylePlantSystem.CascadeLimit;

        static readonly int s_ShadowBiasId = Shader.PropertyToID("_ShadowBias");
        static readonly int s_LightDirectionId = Shader.PropertyToID("_LightDirection");
        static readonly int s_LightPositionId = Shader.PropertyToID("_LightPosition");
        static readonly GlobalKeyword s_Punctual = GlobalKeyword.Create("_CASTING_PUNCTUAL_LIGHT_SHADOW");

        class CullData
        {
            internal readonly Vector4[] spheres = new Vector4[MaxCascades];
            internal int count;
        }

        class DrawData
        {
            internal readonly ShadowSliceData[] slices = new ShadowSliceData[MaxCascades];
            internal readonly Vector4[] biases = new Vector4[MaxCascades];
            internal int count;
            internal int mask;
            internal Vector4 direction;
            internal Vector4 position;
            internal Matrix4x4 view;
            internal Rect full;
            internal Matrix4x4 projection;
        }

        readonly ShadowSliceData[] m_Slices = new ShadowSliceData[MaxCascades];
        readonly ProfilingSampler m_CullSampler = new ProfilingSampler("HB Style Plant Shadow Cull");

        internal StylePlantShadowPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Plant Shadows");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;

            if (!StylePlantSystem.Casts(camera))
                return;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var lightData = frameData.Get<UniversalLightData>();
            var shadowData = frameData.Get<UniversalShadowData>();
            var index = lightData.mainLightIndex;
            var shadowmap = resourceData.mainShadowsTexture;

            if (index < 0 || !shadowData.supportsMainLightShadows || !shadowmap.IsValid())
                return;

            var visibleLight = lightData.visibleLights[index];
            var light = visibleLight.light;

            if (light == null || light.shadows == LightShadows.None || visibleLight.lightType != LightType.Directional)
                return;

            var cullResults = renderingData.cullResults;

            if (!cullResults.GetShadowCasterBounds(index, out _))
                return;

            var count = Mathf.Clamp(shadowData.mainLightShadowCascadesCount, 1, MaxCascades);
            var fullHeight = shadowData.mainLightShadowmapHeight;
            var width = shadowData.mainLightShadowmapWidth;
            var height = count == 2 ? fullHeight >> 1 : fullHeight;
            var resolution = ShadowUtils.GetMaxTileResolutionInAtlas(width, fullHeight, count);

            for (var i = 0; i < count; i++)
            {
                if (!ShadowUtils.ExtractDirectionalLightMatrix(ref cullResults, shadowData, index, i, width, height,
                                                               resolution, light.shadowNearPlane, out _,
                                                               out m_Slices[i]))
                    return;
            }

            using (var builder = renderGraph.AddUnsafePass<CullData>("HB Style Plant Shadow Cull", out var cull,
                                                                     m_CullSampler))
            {
                cull.count = count;

                for (var i = 0; i < MaxCascades; i++)
                    cull.spheres[i] = i < count ? m_Slices[i].splitData.cullingSphere : Vector4.zero;

                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (CullData d, UnsafeGraphContext context) =>
                    StylePlantSystem.CullCascades(context.cmd, d.spheres, d.count));
            }

            using (var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style Plant Shadows", out var draw,
                                                                         profilingSampler))
            {
                draw.count = count;
                draw.mask = camera.cullingMask;
                draw.direction = -visibleLight.localToWorldMatrix.GetColumn(2);
                draw.position = visibleLight.localToWorldMatrix.GetColumn(3);
                draw.view = cameraData.GetViewMatrix();
                draw.projection = cameraData.GetProjectionMatrix();
                var size = renderGraph.GetTextureDesc(shadowmap);
                draw.full = new Rect(0f, 0f, size.width, size.height);

                for (var i = 0; i < count; i++)
                {
                    draw.slices[i] = m_Slices[i];
                    draw.biases[i] = ShadowUtils.GetShadowBias(ref visibleLight, index, shadowData,
                                                               m_Slices[i].projectionMatrix, m_Slices[i].resolution);
                }

                builder.SetRenderAttachmentDepth(shadowmap, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) => Draw(d, context.cmd));
            }
        }

        static void Draw(DrawData d, RasterCommandBuffer cmd)
        {
            cmd.SetKeyword(s_Punctual, false);
            cmd.SetGlobalVector(s_LightDirectionId, d.direction);
            cmd.SetGlobalVector(s_LightPositionId, d.position);
            cmd.SetGlobalDepthBias(1f, 2.5f);

            for (var i = 0; i < d.count; i++)
            {
                var slice = d.slices[i];

                cmd.SetGlobalVector(s_ShadowBiasId, d.biases[i]);
                cmd.SetViewport(new Rect(slice.offsetX, slice.offsetY, slice.resolution, slice.resolution));
                cmd.SetViewProjectionMatrices(slice.viewMatrix, slice.projectionMatrix);
                StylePlantSystem.DrawCascade(cmd, i, d.mask);
            }

            cmd.SetGlobalDepthBias(0f, 0f);
            cmd.SetViewport(d.full);
            cmd.SetViewProjectionMatrices(d.view, d.projection);
        }
    }
}
