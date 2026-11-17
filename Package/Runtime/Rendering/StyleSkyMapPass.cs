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
        static readonly int s_PartId = Shader.PropertyToID("_HB_SkyMapPart");
        static readonly Vector4 s_Ready = new Vector4(1f, 0f, 0f, 0f);
        static readonly Vector4 s_Whole = new Vector4(1f, 1f, 0f, 0f);

        const int Parts = 4;

        class DrawData
        {
            internal RTHandle map;
            internal Material material;
            internal Vector4 part;
            internal int size;
        }

        Material m_Material;
        RTHandle m_Map;
        int m_Size;
        int m_Part;
        bool m_Filled;

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
                m_Filled = false;
            }

            using var builder = renderGraph.AddUnsafePass<DrawData>("HB Style Sky Map", out var data,
                                                                    profilingSampler);

            var map = renderGraph.ImportTexture(m_Map);
            var whole = !m_Filled || !Application.isPlaying;

            data.map = m_Map;
            data.material = m_Material;
            data.size = m_Size;
            data.part = whole ? s_Whole : new Vector4(1f / Parts, 1f, (float)m_Part / Parts, 0f);
            m_Part = whole ? 0 : (m_Part + 1) % Parts;
            m_Filled = true;
            builder.UseTexture(map, whole ? AccessFlags.WriteAll : AccessFlags.ReadWrite);
            builder.SetGlobalTextureAfterPass(map, s_MapId);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (DrawData d, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                cmd.SetRenderTarget(d.map);
                cmd.SetViewport(new Rect(d.part.z * d.size, 0f, d.part.x * d.size, d.size));
                cmd.SetGlobalVector(s_PartId, d.part);
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
