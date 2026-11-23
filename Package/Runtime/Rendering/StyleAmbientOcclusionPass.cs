using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleAmbientOcclusionPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AmbientOcclusion/AmbientOcclusion.shader";

        const int HorizonPass = 0;
        const int BlurPass = 1;
        const int DebugPass = 2;
        const float FinalPower = 2.2f;
        const float MostScreen = 0.2f;
        const float LeastScreen = 0.04f;

        static readonly int s_SourceId = Shader.PropertyToID("_HB_AOSource");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_AOParams");
        static readonly int s_StepsId = Shader.PropertyToID("_HB_AOSteps");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_AOSize");
        static readonly int s_BlurId = Shader.PropertyToID("_HB_AOBlur");
        static readonly int s_DepthId = Shader.PropertyToID("_CameraDepthTexture");
        static readonly int s_NormalsId = Shader.PropertyToID("_CameraNormalsTexture");
        static readonly int s_OcclusionId = Shader.PropertyToID("_ScreenSpaceOcclusionTexture");
        static readonly int s_OcclusionParamId = Shader.PropertyToID("_AmbientOcclusionParam");

        static GlobalKeyword s_Keyword;
        static bool s_KeywordReady;

        sealed class FrameData : ContextItem
        {
            internal TextureHandle texture;

            public override void Reset()
            {
                texture = TextureHandle.nullHandle;
            }
        }

        class DrawData
        {
            internal Material material;
            internal MaterialPropertyBlock block;
            internal TextureHandle source;
            internal TextureHandle depth;
            internal TextureHandle normals;
            internal int pass;
            internal Vector4 parameters;
            internal Vector4 steps;
            internal Vector4 size;
            internal Vector4 blur;
        }

        class KeywordData
        {
            internal bool enable;
            internal float direct;
        }

        readonly MaterialPropertyBlock m_Block = new MaterialPropertyBlock();
        readonly EndPass m_End;
        readonly DebugView m_Debug;

        Material m_Material;
        HBStyleAmbientOcclusionQuality m_Quality;
        HBStyleAmbientOcclusionSettings m_Settings;

        internal StyleAmbientOcclusionPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            profilingSampler = new ProfilingSampler("HB Style Ambient Occlusion");
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            m_End = new EndPass(this);
            m_Debug = new DebugView(this);
        }

        internal static bool Wanted(HBStyleAmbientOcclusionQuality quality, HBStyleAmbientOcclusionSettings settings,
                                    Camera camera)
        {
            return quality != null && quality.level != HBStyleQualityLevel.Off && settings != null &&
                   settings.intensity > 0f && !camera.orthographic;
        }

        static ref readonly GlobalKeyword Keyword
        {
            get
            {
                if (!s_KeywordReady)
                {
                    s_Keyword = GlobalKeyword.Create("_SCREEN_SPACE_OCCLUSION");
                    s_KeywordReady = true;
                }

                return ref s_Keyword;
            }
        }

        internal void Setup(Shader shader, HBStyleAmbientOcclusionQuality quality,
                            HBStyleAmbientOcclusionSettings settings)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Quality = quality;
            m_Settings = settings;
        }

        internal void Enqueue(ScriptableRenderer renderer)
        {
            renderer.EnqueuePass(this);
            renderer.EnqueuePass(m_End);

            if (StyleDebug.view == StyleDebugView.AmbientOcclusion)
                renderer.EnqueuePass(m_Debug);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Quality == null || m_Settings == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var depth = resourceData.cameraDepthTexture;
            var normals = resourceData.cameraNormalsTexture;

            if (!depth.IsValid() || !normals.IsValid())
                return;

            var level = m_Quality.current;
            var target = cameraData.cameraTargetDescriptor;
            var width = Mathf.Max(1, target.width);
            var height = Mathf.Max(1, target.height);
            var shift = level.halfResolution ? 1 : 0;
            var aoWidth = Mathf.Max(1, width >> shift);
            var aoHeight = Mathf.Max(1, height >> shift);
            var projection = cameraData.GetProjectionMatrix();
            var parameters = new Vector4(m_Settings.radius, Mathf.Abs(projection.m11) * aoHeight * 0.5f,
                                         FinalPower * m_Settings.intensity, 0f);
            var steps = new Vector4(level.directions, level.samples, aoHeight * MostScreen, aoHeight * LeastScreen);
            var aoSize = new Vector4(aoWidth, aoHeight, 1f / aoWidth, 1f / aoHeight);
            var fullSize = new Vector4(width, height, 1f / width, 1f / height);
            var format = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Render)
                ? GraphicsFormat.R8_UNorm
                : GraphicsFormat.R8G8B8A8_UNorm;

            var horizon = Create(renderGraph, aoWidth, aoHeight, format, "HB Style AO Horizon");
            var across = Create(renderGraph, aoWidth, aoHeight, format, "HB Style AO Blur");
            var final = Create(renderGraph, width, height, format, "HB Style AO");

            Draw(renderGraph, TextureHandle.nullHandle, depth, normals, horizon, HorizonPass, parameters, steps, aoSize,
                 Vector4.zero);
            Draw(renderGraph, horizon, depth, TextureHandle.nullHandle, across, BlurPass, parameters, steps, aoSize,
                 new Vector4(aoSize.z, 0f, 0f, 0f));
            Draw(renderGraph, across, depth, TextureHandle.nullHandle, final, BlurPass, parameters, steps, fullSize,
                 new Vector4(0f, aoSize.w, 0f, 0f));

            frameData.GetOrCreate<FrameData>().texture = final;

            using var builder = renderGraph.AddRasterRenderPass<KeywordData>("HB Style AO Bind", out var data,
                                                                            profilingSampler);

            data.enable = true;
            data.direct = m_Settings.directLighting;
            builder.UseTexture(final, AccessFlags.Read);
            builder.SetGlobalTextureAfterPass(final, s_OcclusionId);
            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (KeywordData d, RasterGraphContext context) => SetKeyword(context.cmd, d));
        }

        static void SetKeyword(RasterCommandBuffer cmd, KeywordData data)
        {
            cmd.SetKeyword(Keyword, data.enable);
            cmd.SetGlobalVector(s_OcclusionParamId,
                                data.enable ? new Vector4(1f, 0f, 0f, data.direct) : Vector4.zero);
        }

        static TextureHandle Create(RenderGraph renderGraph, int width, int height, GraphicsFormat format, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(width, height)
            {
                name = name,
                format = format,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = false
            });
        }

        void Draw(RenderGraph renderGraph, TextureHandle source, TextureHandle depth, TextureHandle normals,
                  TextureHandle target, int pass, Vector4 parameters, Vector4 steps, Vector4 size, Vector4 blur)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawData>("HB Style AO", out var data,
                                                                         profilingSampler);

            data.material = m_Material;
            data.block = m_Block;
            data.source = source;
            data.depth = depth;
            data.normals = normals;
            data.pass = pass;
            data.parameters = parameters;
            data.steps = steps;
            data.size = size;
            data.blur = blur;

            if (source.IsValid())
                builder.UseTexture(source, AccessFlags.Read);

            if (depth.IsValid())
                builder.UseTexture(depth, AccessFlags.Read);

            if (normals.IsValid())
                builder.UseTexture(normals, AccessFlags.Read);

            builder.SetRenderAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext context) => Render(d, context.cmd));
        }

        static void Render(DrawData d, RasterCommandBuffer cmd)
        {
            d.block.Clear();

            if (d.source.IsValid())
                d.block.SetTexture(s_SourceId, d.source);

            if (d.depth.IsValid())
                d.block.SetTexture(s_DepthId, d.depth);

            if (d.normals.IsValid())
                d.block.SetTexture(s_NormalsId, d.normals);

            d.block.SetVector(s_ParamsId, d.parameters);
            d.block.SetVector(s_StepsId, d.steps);
            d.block.SetVector(s_SizeId, d.size);
            d.block.SetVector(s_BlurId, d.blur);
            cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1, d.block);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.SetKeyword(Keyword, false);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }

        sealed class EndPass : ScriptableRenderPass
        {
            internal EndPass(StyleAmbientOcclusionPass owner)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
                profilingSampler = owner.profilingSampler;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!frameData.Contains<FrameData>())
                    return;

                var texture = frameData.Get<FrameData>().texture;

                if (!texture.IsValid())
                    return;

                using var builder = renderGraph.AddRasterRenderPass<KeywordData>("HB Style AO End", out var data,
                                                                                profilingSampler);

                data.enable = false;
                builder.UseTexture(texture, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (KeywordData d, RasterGraphContext context) => SetKeyword(context.cmd, d));
            }
        }

        sealed class DebugView : ScriptableRenderPass
        {
            readonly StyleAmbientOcclusionPass m_Owner;

            internal DebugView(StyleAmbientOcclusionPass owner)
            {
                m_Owner = owner;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                profilingSampler = owner.profilingSampler;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                if (m_Owner.m_Material == null || resourceData.isActiveTargetBackBuffer ||
                    !frameData.Contains<FrameData>())
                    return;

                var texture = frameData.Get<FrameData>().texture;

                if (!texture.IsValid())
                    return;

                var target = resourceData.activeColorTexture;
                var description = renderGraph.GetTextureDesc(target);
                var size = new Vector4(description.width, description.height, 1f / Mathf.Max(1, description.width),
                                       1f / Mathf.Max(1, description.height));

                m_Owner.Draw(renderGraph, texture, TextureHandle.nullHandle, TextureHandle.nullHandle, target,
                             DebugPass, Vector4.zero, Vector4.zero, size, Vector4.zero);
            }
        }
    }
}
