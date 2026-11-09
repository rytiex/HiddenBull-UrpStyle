using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleSkyMapPass : ScriptableRenderPass, IDisposable
    {
        internal const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Sky/SkyMap.shader";

        const string MapName = "_HB_SkyMap";

        static readonly int s_MapId = Shader.PropertyToID(MapName);
        static readonly int s_SetId = Shader.PropertyToID("_HB_SkyMapSet");
        static readonly Vector4 s_Ready = new Vector4(1f, 0f, 0f, 0f);

        class DrawData
        {
            internal RTHandle map;
            internal Material material;
        }

        Material m_Material;
        RTHandle m_Map;
        int m_Size;

        internal StyleSkyMapPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            profilingSampler = new ProfilingSampler("HB Style Sky Map");
        }

        internal void Setup(Shader shader, int size)
        {
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Size = size;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Size <= 0)
                return;

            if (m_Map == null || m_Map.rt == null || m_Map.rt.width != m_Size)
            {
                m_Map?.Release();
                m_Map = RTHandles.Alloc(m_Size, m_Size, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                                        filterMode: FilterMode.Trilinear, wrapMode: TextureWrapMode.Clamp,
                                        useMipMap: true, autoGenerateMips: false, name: MapName);
            }

            using var builder = renderGraph.AddUnsafePass<DrawData>("HB Style Sky Map", out var data,
                                                                    profilingSampler);

            var map = renderGraph.ImportTexture(m_Map);

            data.map = m_Map;
            data.material = m_Material;
            builder.UseTexture(map, AccessFlags.WriteAll);
            builder.SetGlobalTextureAfterPass(map, s_MapId);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                cmd.SetRenderTarget(d.map);
                cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3);
                cmd.GenerateMips(d.map);
                cmd.SetGlobalVector(s_SetId, s_Ready);
            });
        }

        public void Dispose()
        {
            Shader.SetGlobalVector(s_SetId, Vector4.zero);
            m_Map?.Release();
            m_Map = null;
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }
    }
}
