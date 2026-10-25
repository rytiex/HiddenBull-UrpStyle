using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StylePrecipitationPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/Precipitation.shader";

        const int RainPass = 0;
        const int SnowPass = 1;
        const int FarRainPass = 2;
        const int FarSnowPass = 3;
        const int WindPass = 4;
        const int VerticesPerDrop = 6;
        const float RainSeed = 0f;
        const float SnowSeed = 104729f;
        const float WindSeed = 7919f;

        class PassData
        {
            internal Material material;
            internal MaterialPropertyBlock rainBlock;
            internal MaterialPropertyBlock snowBlock;
            internal MaterialPropertyBlock farRainBlock;
            internal MaterialPropertyBlock farSnowBlock;
            internal MaterialPropertyBlock windBlock;
            internal int rain;
            internal int snow;
            internal int ribbons;
            internal bool farRain;
            internal bool farSnow;
        }

        static readonly int s_OffsetId = Shader.PropertyToID("_HB_PrecipitationOffset");
        static readonly int s_VelocityId = Shader.PropertyToID("_HB_PrecipitationVelocity");

        readonly MaterialPropertyBlock m_RainBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock m_SnowBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock m_FarRainBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock m_FarSnowBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock m_WindBlock = new MaterialPropertyBlock();

        Material m_Material;
        HBStylePrecipitationQuality m_Quality;

        internal StylePrecipitationPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            profilingSampler = new ProfilingSampler("HB Style Precipitation");
        }

        internal void Setup(Shader shader, HBStylePrecipitationQuality quality)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Quality = quality;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var camera = frameData.Get<UniversalCameraData>().camera;
            var weather = StyleEnvironment.weather;
            var on = m_Material != null && m_Quality != null && m_Quality.level != HBStyleQualityLevel.Off &&
                     Visible(camera);
            var density = on ? m_Quality.current.density : 0f;
            var rain = Count(StylePrecipitation.MaxRainDrops, weather.x, density);
            var snow = Count(StylePrecipitation.MaxSnowFlakes, weather.y, density);
            var windShare = StylePrecipitation.windShare;
            var ribbons = windShare > 0f ? Mathf.RoundToInt(StylePrecipitation.MaxWindRibbons * density) : 0;

            if (camera.cameraType == CameraType.Game)
            {
                StyleDebug.rainDrops = rain;
                StyleDebug.snowFlakes = snow;
            }

            if (rain == 0 && snow == 0 && ribbons == 0)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();

            m_RainBlock.SetVector(s_OffsetId, Offset(StylePrecipitation.rainOffset, RainSeed));
            m_RainBlock.SetVector(s_VelocityId, StylePrecipitation.rainVelocity);
            m_SnowBlock.SetVector(s_OffsetId, Offset(StylePrecipitation.snowOffset, SnowSeed));
            m_SnowBlock.SetVector(s_VelocityId, StylePrecipitation.snowVelocity);

            var far = on && m_Quality.current.far;
            var farRainShare = Mathf.Clamp01(weather.x) * density * StylePrecipitation.FarRainShare;
            var farSnowShare = Mathf.Clamp01(weather.y) * density * StylePrecipitation.FarSnowShare;

            var farRain = new Vector3(StylePrecipitation.rainScroll, farRainShare);
            var farSnow = new Vector3(StylePrecipitation.snowScroll, farSnowShare);

            m_FarRainBlock.SetVector(s_OffsetId, Offset(farRain, RainSeed));
            m_FarRainBlock.SetVector(s_VelocityId, StylePrecipitation.rainVelocity);
            m_FarSnowBlock.SetVector(s_OffsetId, Offset(farSnow, SnowSeed));
            m_FarSnowBlock.SetVector(s_VelocityId, StylePrecipitation.snowVelocity);

            m_WindBlock.SetVector(s_OffsetId, new Vector4(windShare, StylePrecipitation.windSpeed, 0f, WindSeed));
            m_WindBlock.SetVector(s_VelocityId, StylePrecipitation.windMotion);

            using var builder = renderGraph.AddRasterRenderPass<PassData>("HB Style Precipitation", out var data,
                                                                         profilingSampler);

            data.material = m_Material;
            data.rainBlock = m_RainBlock;
            data.snowBlock = m_SnowBlock;
            data.farRainBlock = m_FarRainBlock;
            data.farSnowBlock = m_FarSnowBlock;
            data.windBlock = m_WindBlock;
            data.rain = rain;
            data.snow = snow;
            data.ribbons = ribbons;
            data.farRain = far && rain > 0;
            data.farSnow = far && snow > 0;

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                if (d.farRain)
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, FarRainPass, MeshTopology.Triangles,
                                               StylePrecipitation.FarVertices, 1, d.farRainBlock);
                }

                if (d.farSnow)
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, FarSnowPass, MeshTopology.Triangles,
                                               StylePrecipitation.FarVertices, 1, d.farSnowBlock);
                }

                if (d.rain > 0)
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, RainPass, MeshTopology.Triangles,
                                               d.rain * VerticesPerDrop, 1, d.rainBlock);
                }

                if (d.snow > 0)
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, SnowPass, MeshTopology.Triangles,
                                               d.snow * VerticesPerDrop, 1, d.snowBlock);
                }

                if (d.ribbons > 0)
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, WindPass, MeshTopology.Triangles,
                                               d.ribbons * StylePrecipitation.WindVertices, 1, d.windBlock);
                }
            });
        }

        static bool Visible(Camera camera)
        {
            var controller = StyleEnvironment.controller;

            return controller == null || (camera.cullingMask & (1 << controller.gameObject.layer)) != 0;
        }

        static int Count(int most, float amount, float density)
        {
            return Mathf.RoundToInt(most * Mathf.Clamp01(amount) * density);
        }

        static Vector4 Offset(Vector3 offset, float seed)
        {
            return new Vector4(offset.x, offset.y, offset.z, seed);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
