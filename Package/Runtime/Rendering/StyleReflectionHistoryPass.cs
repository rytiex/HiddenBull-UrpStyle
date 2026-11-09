using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleReflectionHistoryPass : ScriptableRenderPass, IDisposable
    {
        const string ColourName = "_HB_ReflectHistory";
        const string DepthName = "_HB_ReflectDepth";
        const int LeastSide = 16;
        const float Reach = 250f;

        internal const string DepthShaderPath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Reflection/ReflectDepth.shader";

        static readonly int s_ColourId = Shader.PropertyToID(ColourName);
        static readonly int s_DepthId = Shader.PropertyToID(DepthName);
        static readonly int s_PastId = Shader.PropertyToID("_HB_ReflectPast");
        static readonly int s_SetId = Shader.PropertyToID("_HB_ReflectSet");
        static readonly int s_GlassId = Shader.PropertyToID("_HB_ReflectGlass");

        sealed class History
        {
            internal RTHandle colour;
            internal RTHandle depth;
            internal Matrix4x4 past;
            internal int width;
            internal int height;
            internal GraphicsFormat format;
            internal bool written;
            internal double lastUsed;
        }

        class CopyData
        {
            internal TextureHandle colour;
            internal TextureHandle depth;
            internal TextureHandle colourTarget;
            internal TextureHandle depthTarget;
            internal TextureHandle glass;
            internal RTHandle mips;
            internal Material material;
        }

        class BindData
        {
            internal Matrix4x4 past;
            internal Vector4 settings;
        }

        internal sealed class BindPass : ScriptableRenderPass
        {
            readonly StyleReflectionHistoryPass m_History;
            bool m_Base;

            internal BindPass(StyleReflectionHistoryPass history)
            {
                m_History = history;
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
                profilingSampler = new ProfilingSampler("HB Style Reflection Bind");
            }

            internal void Setup(bool baseCamera)
            {
                m_Base = baseCamera;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();

                m_History.Bind(renderGraph, m_Base ? cameraData.camera : null, profilingSampler);
            }
        }

        readonly Dictionary<Camera, History> m_Histories = new Dictionary<Camera, History>();
        readonly List<Camera> m_Stale = new List<Camera>();

        Vector4 m_Settings;
        Material m_DepthMaterial;

        internal StyleReflectionHistoryPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler("HB Style Reflection History");
            binder = new BindPass(this);
        }

        internal BindPass binder { get; }

        internal TextureHandle glass { get; set; }

        internal void Setup(HBStyleReflectionQuality quality, Shader depthShader)
        {
            if (m_DepthMaterial == null && depthShader != null)
                m_DepthMaterial = CoreUtils.CreateEngineMaterial(depthShader);

            var steps = quality.steps;

            if (steps != (int)m_Settings.x)
                m_Settings = Settings(steps);

            ConfigureInput(steps > 0 ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None);
        }

        internal static Vector4 Settings(int steps)
        {
            return steps > 0 ? new Vector4(steps, 0f, Reach, 0f) : Vector4.zero;
        }

        internal bool Valid(Camera camera)
        {
            return camera != null && m_Settings.x > 0f && m_Histories.TryGetValue(camera, out var history) &&
                   history.written && history.colour != null && history.depth != null;
        }

        void Bind(RenderGraph renderGraph, Camera camera, ProfilingSampler sampler)
        {
            var valid = Valid(camera);

            using var builder = renderGraph.AddUnsafePass<BindData>("HB Style Reflection Bind", out var data, sampler);

            data.settings = m_Settings;
            data.settings.w = valid ? 1f : 0f;

            if (valid)
            {
                var history = m_Histories[camera];
                var colour = renderGraph.ImportTexture(history.colour);
                var depth = renderGraph.ImportTexture(history.depth);

                data.past = history.past;
                builder.UseTexture(colour, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(colour, s_ColourId);
                builder.SetGlobalTextureAfterPass(depth, s_DepthId);
            }

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (BindData d, UnsafeGraphContext context) =>
            {
                context.cmd.SetGlobalVector(s_SetId, d.settings);
                context.cmd.SetGlobalMatrix(s_PastId, d.past);
            });
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var glassNearest = glass;

            glass = default;

            if (m_Settings.x <= 0f || m_DepthMaterial == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var colour = resourceData.activeColorTexture;
            var depth = resourceData.cameraDepthTexture;

            if (!colour.IsValid() || !depth.IsValid())
                return;

            var target = cameraData.cameraTargetDescriptor;
            var width = target.width / 2;
            var height = target.height / 2;

            if (Mathf.Min(width, height) < LeastSide)
                return;

            var history = Acquire(cameraData.camera, width, height, target.graphicsFormat);
            var projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                       !resourceData.isActiveTargetBackBuffer);

            history.past = projection * cameraData.GetViewMatrix();
            history.written = true;

            using var builder = renderGraph.AddUnsafePass<CopyData>("HB Style Reflection History", out var data,
                                                                    profilingSampler);

            data.colour = colour;
            data.depth = depth;
            data.colourTarget = renderGraph.ImportTexture(history.colour);
            data.depthTarget = renderGraph.ImportTexture(history.depth);
            data.mips = history.colour;
            data.glass = glassNearest;
            data.material = m_DepthMaterial;
            builder.UseTexture(data.colour, AccessFlags.Read);
            builder.UseTexture(data.depth, AccessFlags.Read);
            builder.UseTexture(data.colourTarget, AccessFlags.WriteAll);
            builder.UseTexture(data.depthTarget, AccessFlags.WriteAll);

            if (glassNearest.IsValid())
                builder.UseTexture(glassNearest, AccessFlags.Read);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (CopyData d, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                Blitter.BlitCameraTexture(cmd, d.colour, d.colourTarget, 0f, true);
                RenderTargetIdentifier nearest = d.glass.IsValid()
                    ? (RTHandle)d.glass
                    : new RenderTargetIdentifier(Texture2D.blackTexture);

                cmd.SetGlobalTexture(s_GlassId, nearest);
                Blitter.BlitCameraTexture(cmd, d.depth, d.depthTarget, d.material, 0);
                cmd.GenerateMips(d.mips);
            });
        }

        History Acquire(Camera camera, int width, int height, GraphicsFormat format)
        {
            var now = StyleCameraStates.clock;

            m_Stale.Clear();

            foreach (var pair in m_Histories)
            {
                var unused = now - pair.Value.lastUsed > StyleCameraStates.PruneSeconds;

                if (pair.Key == null || (pair.Key != camera && unused))
                    m_Stale.Add(pair.Key);
            }

            foreach (var stale in m_Stale)
            {
                Release(m_Histories[stale]);
                m_Histories.Remove(stale);
            }

            if (!m_Histories.TryGetValue(camera, out var history))
            {
                history = new History();
                m_Histories.Add(camera, history);
            }

            if (history.colour == null || history.depth == null || history.width != width ||
                history.height != height || history.format != format)
            {
                Release(history);
                history.width = width;
                history.height = height;
                history.format = format;
                history.colour = RTHandles.Alloc(width, height, colorFormat: format, filterMode: FilterMode.Trilinear,
                                                 wrapMode: TextureWrapMode.Clamp, useMipMap: true,
                                                 autoGenerateMips: false, name: ColourName);
                history.depth = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R32_SFloat,
                                                filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                                                name: DepthName);
            }

            history.lastUsed = now;

            return history;
        }

        static void Release(History history)
        {
            history.colour?.Release();
            history.depth?.Release();
            history.colour = null;
            history.depth = null;
            history.written = false;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_DepthMaterial);
            m_DepthMaterial = null;

            foreach (var history in m_Histories.Values)
                Release(history);

            m_Histories.Clear();
        }
    }
}
