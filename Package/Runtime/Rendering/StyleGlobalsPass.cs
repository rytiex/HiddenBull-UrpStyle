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

        readonly ConstantBuffer<StyleFrameGlobals> m_Buffer = new ConstantBuffer<StyleFrameGlobals>();

        class PassData
        {
            public ConstantBuffer<StyleFrameGlobals> buffer;
            public StyleFrameGlobals globals;
        }

        internal StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Globals");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData, profilingSampler);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            passData.buffer = m_Buffer;
            passData.globals = StyleFrameGlobals.Resolve();

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                data.buffer.PushGlobal(context.cmd, data.globals, s_FrameId));
        }

        public void Dispose()
        {
            m_Buffer.Release();
        }
    }
}
