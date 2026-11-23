using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleBloomPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Bloom/Bloom.shader";

        const int PrefilterPass = 0;
        const int DownPass = 1;
        const int UpPass = 2;
        const int CompositePass = 3;
        const int MostSteps = 8;
        const string StableKeyword = "HB_BLOOM_STABLE";

        static readonly int s_SourceId = Shader.PropertyToID("_HB_BloomSource");
        static readonly int s_LowId = Shader.PropertyToID("_HB_BloomLow");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_BloomParams");
        static readonly int s_TintId = Shader.PropertyToID("_HB_BloomTint");

        class PassData
        {
            internal Material material;
            internal MaterialPropertyBlock block;
            internal TextureHandle source;
            internal TextureHandle low;
            internal int pass;
            internal Vector4 parameters;
            internal Vector4 tint;
        }

        readonly MaterialPropertyBlock m_Block = new MaterialPropertyBlock();
        readonly TextureHandle[] m_Down = new TextureHandle[MostSteps];

        Material m_Material;
        HBStyleBloomQuality m_Quality;
        HBStyleBloomSettings m_Settings;

        internal StyleBloomPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
            profilingSampler = new ProfilingSampler("HB Style Bloom");
        }

        internal static bool Wanted(HBStyleBloomQuality quality, HBStyleBloomSettings settings)
        {
            return quality != null && quality.level != HBStyleQualityLevel.Off && settings != null &&
                   settings.intensity > 0f;
        }

        internal void Setup(Shader shader, HBStyleBloomQuality quality, HBStyleBloomSettings settings)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Quality = quality;
            m_Settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Quality == null || m_Settings == null)
                return;

            var bloom = m_Settings;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            var full = renderGraph.GetTextureDesc(source);
            var width = Mathf.Max(1, full.width >> 1);
            var height = Mathf.Max(1, full.height >> 1);
            var level = m_Quality.current;
            var steps = Mathf.Clamp(Mathf.Min(level.steps, Mathf.FloorToInt(Mathf.Log(Mathf.Min(width, height), 2f))),
                                    1, MostSteps);
            var threshold = Mathf.GammaToLinearSpace(bloom.threshold);
            var parameters = new Vector4(threshold, threshold * Mathf.Max(bloom.softness, 1e-3f) * 0.5f,
                                         bloom.spread, bloom.intensity);
            var tint = (Vector4)bloom.tint.linear;
            var format = SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormatUsage.Render)
                ? GraphicsFormat.B10G11R11_UFloatPack32
                : GraphicsFormat.R16G16B16A16_SFloat;

            CoreUtils.SetKeyword(m_Material, StableKeyword, level.stable);

            for (var i = 0; i < steps; i++)
            {
                var description = new TextureDesc(Mathf.Max(1, width >> i), Mathf.Max(1, height >> i))
                {
                    name = "HB Style Bloom Down " + i,
                    format = format,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = false
                };

                m_Down[i] = renderGraph.CreateTexture(description);
                Draw(renderGraph, i == 0 ? source : m_Down[i - 1], TextureHandle.nullHandle, m_Down[i],
                     i == 0 ? PrefilterPass : DownPass, parameters, tint, false);
            }

            var glow = m_Down[steps - 1];

            for (var i = steps - 2; i >= 0; i--)
            {
                var description = renderGraph.GetTextureDesc(m_Down[i]);

                description.name = "HB Style Bloom Up " + i;

                var up = renderGraph.CreateTexture(description);

                Draw(renderGraph, m_Down[i], glow, up, UpPass, parameters, tint, false);
                glow = up;
            }

            Draw(renderGraph, TextureHandle.nullHandle, glow, source, CompositePass, parameters, tint, true);
        }

        void Draw(RenderGraph renderGraph, TextureHandle source, TextureHandle low, TextureHandle target, int pass,
                  Vector4 parameters, Vector4 tint, bool blend)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>("HB Style Bloom", out var data,
                                                                         profilingSampler);

            data.material = m_Material;
            data.block = m_Block;
            data.source = source;
            data.low = low;
            data.pass = pass;
            data.parameters = parameters;
            data.tint = tint;

            if (source.IsValid())
                builder.UseTexture(source, AccessFlags.Read);

            if (low.IsValid())
                builder.UseTexture(low, AccessFlags.Read);

            builder.SetRenderAttachment(target, 0, blend ? AccessFlags.ReadWrite : AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                d.block.Clear();

                if (d.source.IsValid())
                    d.block.SetTexture(s_SourceId, d.source);

                if (d.low.IsValid())
                    d.block.SetTexture(s_LowId, d.low);

                d.block.SetVector(s_ParamsId, d.parameters);
                d.block.SetVector(s_TintId, d.tint);
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1,
                                           d.block);
            });
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
