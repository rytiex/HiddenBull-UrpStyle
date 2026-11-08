using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleOverlayShadowPass : ScriptableRenderPass, IDisposable
    {
        const string ShadowName = "_HB_OverlayMainShadow";
        const string TintName = "_HB_OverlayShadowTint";
        const float FadeMultiplier = 1000f;
        const float FadeBorderLeast = 0.0001f;

        static readonly int s_ShadowmapId = Shader.PropertyToID("_MainLightShadowmapTexture");
        static readonly int s_ParamsId = Shader.PropertyToID("_MainLightShadowParams");
        static readonly int s_SizeId = Shader.PropertyToID("_MainLightShadowmapSize");
        static readonly int s_Offset0Id = Shader.PropertyToID("_MainLightShadowOffset0");
        static readonly int s_Offset1Id = Shader.PropertyToID("_MainLightShadowOffset1");
        static readonly int s_TintMapId = Shader.PropertyToID("_HB_ShadowTintMap");
        static readonly int s_TintActiveId = Shader.PropertyToID("_HB_ShadowTintActive");

        static readonly GlobalKeyword s_MainShadows = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS");
        static readonly GlobalKeyword s_MainCascades = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE");
        static readonly GlobalKeyword s_SoftShadows = GlobalKeyword.Create("_SHADOWS_SOFT");

        RTHandle m_Shadow;
        RTHandle m_Tint;
        StyleTransparentShadowPass m_Glass;
        bool m_TintReady;
        HBStyleQualityLevel m_Level;
        Camera m_Owner;
        bool m_Ready;
        Vector4 m_Params;
        Vector4 m_Size;
        bool m_Soft;
        int m_Cascades;

        class CopyData
        {
            public TextureHandle source;
            public TextureHandle target;
            public TextureHandle tintSource;
            public TextureHandle tintTarget;
            public bool tint;
        }

        class BindData
        {
            public Vector4 parameters;
            public Vector4 size;
            public bool soft;
            public int cascades;
            public bool tint;
        }

        internal StyleOverlayShadowPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Overlay Shadows");
        }

        internal void Setup(HBStyleQualityLevel level, StyleTransparentShadowPass glass)
        {
            m_Level = level;
            m_Glass = glass;
        }

        internal static bool Feeds(Camera camera)
        {
            return camera != null && camera.TryGetComponent(out UniversalAdditionalCameraData data) &&
                   data.cameraStack != null && data.cameraStack.Count > 0;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.renderType == CameraRenderType.Overlay)
                RecordBind(renderGraph, frameData, cameraData);
            else
                RecordCopy(renderGraph, frameData, cameraData);
        }

        void RecordCopy(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData)
        {
            m_Owner = cameraData.camera;
            m_Ready = false;
            m_TintReady = false;

            var shadowData = frameData.Get<UniversalShadowData>();
            var lightData = frameData.Get<UniversalLightData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            if (!shadowData.supportsMainLightShadows || lightData.mainLightIndex < 0)
                return;

            var source = resourceData.mainShadowsTexture;

            if (!source.IsValid())
                return;

            var desc = renderGraph.GetTextureDesc(source);

            if (desc.width < 2 || desc.height < 2)
                return;

            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_Shadow, desc.width, desc.height, (int)desc.depthBufferBits,
                                                   1, 0f, ShadowName);

            var light = lightData.visibleLights[lightData.mainLightIndex].light;

            m_Soft = light != null && light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;
            m_Cascades = shadowData.mainLightShadowCascadesCount;
            m_Size = new Vector4(1f / desc.width, 1f / desc.height, desc.width, desc.height);
            var reach = cameraData.maxShadowDistance;

            Fade(reach * reach, shadowData.mainLightShadowCascadeBorder, out var scale, out var bias);
            var strength = light != null ? light.shadowStrength : 1f;

            m_Params = new Vector4(strength, SoftValue(light, m_Soft, m_Level), scale, bias);
            m_Ready = true;

            var tint = m_Glass != null ? m_Glass.tint : TextureHandle.nullHandle;

            if (tint.IsValid())
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_Tint, renderGraph.GetTextureDesc(tint), TintName);
                m_TintReady = m_Tint != null;
            }

            using var builder = renderGraph.AddUnsafePass<CopyData>(passName, out var passData, profilingSampler);

            passData.source = source;
            passData.target = renderGraph.ImportTexture(m_Shadow);
            passData.tint = m_TintReady;
            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.target, AccessFlags.Write);

            if (m_TintReady)
            {
                passData.tintSource = tint;
                passData.tintTarget = renderGraph.ImportTexture(m_Tint);
                builder.UseTexture(passData.tintSource, AccessFlags.Read);
                builder.UseTexture(passData.tintTarget, AccessFlags.Write);
            }

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (CopyData data, UnsafeGraphContext context) =>
            {
                context.cmd.CopyTexture(data.source, data.target);

                if (data.tint)
                    context.cmd.CopyTexture(data.tintSource, data.tintTarget);
            });
        }

        void RecordBind(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData)
        {
            var shadowData = frameData.Get<UniversalShadowData>();

            if (!m_Ready || m_Shadow == null || m_Owner == null || m_Owner != cameraData.baseCamera ||
                shadowData.supportsMainLightShadows)
                return;

            using var builder = renderGraph.AddUnsafePass<BindData>(passName, out var passData, profilingSampler);

            var shadow = renderGraph.ImportTexture(m_Shadow);

            builder.UseTexture(shadow, AccessFlags.Read);
            builder.SetGlobalTextureAfterPass(shadow, s_ShadowmapId);
            passData.tint = m_TintReady && m_Tint != null;

            if (passData.tint)
            {
                var tint = renderGraph.ImportTexture(m_Tint);

                builder.UseTexture(tint, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(tint, s_TintMapId);
            }

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            passData.parameters = m_Params;
            passData.size = m_Size;
            passData.soft = m_Soft;
            passData.cascades = m_Cascades;

            builder.SetRenderFunc(static (BindData data, UnsafeGraphContext context) =>
            {
                var cmd = context.cmd;
                var halfWidth = data.size.x * 0.5f;
                var halfHeight = data.size.y * 0.5f;

                cmd.SetGlobalVector(s_ParamsId, data.parameters);
                cmd.SetGlobalVector(s_SizeId, data.size);
                cmd.SetGlobalVector(s_Offset0Id, new Vector4(-halfWidth, -halfHeight, halfWidth, -halfHeight));
                cmd.SetGlobalVector(s_Offset1Id, new Vector4(-halfWidth, halfHeight, halfWidth, halfHeight));
                cmd.SetKeyword(s_MainShadows, data.cascades == 1);
                cmd.SetKeyword(s_MainCascades, data.cascades > 1);
                cmd.SetKeyword(s_SoftShadows, data.soft);
                cmd.SetGlobalFloat(s_TintActiveId, data.tint ? 1f : 0f);
            });
        }

        static float SoftValue(Light light, bool soft, HBStyleQualityLevel level)
        {
            if (!soft || light == null)
                return 0f;

            var quality = light.TryGetComponent(out UniversalAdditionalLightData data)
                ? data.softShadowQuality
                : SoftShadowQuality.UsePipelineSettings;

            if (quality == SoftShadowQuality.UsePipelineSettings)
            {
                quality = level switch
                {
                    HBStyleQualityLevel.High => SoftShadowQuality.Medium,
                    HBStyleQualityLevel.Ultra => SoftShadowQuality.High,
                    _ => SoftShadowQuality.Low
                };
            }

            return Mathf.Max((int)quality, (int)SoftShadowQuality.Low);
        }

        static void Fade(float distance, float border, out float scale, out float bias)
        {
            if (border < FadeBorderLeast)
            {
                scale = FadeMultiplier;
                bias = -distance * FadeMultiplier;
                return;
            }

            border = 1f - border;
            border *= border;

            var near = border * distance;

            scale = 1f / (distance - near);
            bias = -near / (distance - near);
        }

        public void Dispose()
        {
            m_Shadow?.Release();
            m_Shadow = null;
            m_Tint?.Release();
            m_Tint = null;
            m_Glass = null;
            m_TintReady = false;
            m_Owner = null;
            m_Ready = false;
        }
    }
}
