using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleUnderwaterPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/Style Underwater";

        const float Margin = 1f;
        const float Span = 1f;
        const int LineColumns = 64;

        static readonly int s_LevelId = Shader.PropertyToID("_HB_UnderLevel");
        static readonly int s_SlopeId = Shader.PropertyToID("_HB_UnderSlope");
        static readonly int s_GridId = Shader.PropertyToID("_WaterGrid");
        static readonly int s_BoxId = Shader.PropertyToID("_WaterBox");
        static readonly int s_LayersId = Shader.PropertyToID("_WaterLayers");
        static readonly int s_AreaId = Shader.PropertyToID("_WaterArea");
        static readonly int s_LevelMapId = Shader.PropertyToID("_WaterLevelMap");
        static readonly int s_WeightMapId = Shader.PropertyToID("_WaterWeightMap");
        static readonly int s_GlowId = Shader.PropertyToID("_Glow");
        static readonly int s_LineId = Shader.PropertyToID("_HB_UnderLine");
        static readonly int s_LineSizeId = Shader.PropertyToID("_HB_UnderLineSize");
        static readonly Vector4 s_Whole = new Vector4(1f, 1f, 0f, 0f);

        Material m_Material;

        public StyleUnderwaterPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("HiddenBull Style Underwater");
            requiresIntermediateTexture = true;

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public bool isReady => m_Material != null;

        public void Setup(Shader shader)
        {
            if (shader == null)
            {
                Dispose();
                return;
            }

            if (m_Material == null || m_Material.shader != shader)
            {
                Dispose();

                m_Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle line;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            if (m_Material == null || !HBStyleLiquid.any || cameraData.cameraType == CameraType.Preview)
                return;

            var eye = cameraData.camera.transform.position;

            if (!HBStyleLiquid.TryGetSubmerged(eye, out var height, out var found) || eye.y > height + Margin)
                return;

            var terrain = found as HBStyleLiquidTerrain;
            var pool = found as HBStyleLiquidPool;
            var liquid = pool != null ? pool.material : terrain.material;

            if (liquid == null || !liquid.HasFloat(s_GlowId))
                return;

            var slope = new Vector4(0f, 0f, eye.x, eye.z);

            m_Material.CopyPropertiesFromMaterial(liquid);

            if (pool != null)
            {
                m_Material.SetVector(s_GridId, pool.cells);
                m_Material.SetVector(s_BoxId, pool.box);
                m_Material.SetVector(s_AreaId, Vector4.zero);
                m_Material.SetTexture(s_LevelMapId, Texture2D.blackTexture);
                m_Material.SetTexture(s_WeightMapId, Texture2D.blackTexture);
                m_Material.SetFloat(s_LayersId, 0f);
                m_Material.SetFloat(s_LevelId, pool.level);
            }
            else
            {
                var here = new Vector2(eye.x, eye.z);

                m_Material.SetVector(s_GridId, terrain.cells);
                m_Material.SetVector(s_AreaId, terrain.area);
                m_Material.SetTexture(s_LevelMapId, terrain.levelMap);
                m_Material.SetTexture(s_WeightMapId, terrain.weightMap);
                StyleLiquidLayers.Write(terrain.layers, null, m_Material);
                m_Material.SetFloat(s_LevelId, terrain.level);

                slope.x = (terrain.LevelAt(here + Vector2.right * Span) - terrain.LevelAt(here - Vector2.right * Span))
                        / (Span * 2f);
                slope.y = (terrain.LevelAt(here + Vector2.up * Span) - terrain.LevelAt(here - Vector2.up * Span))
                        / (Span * 2f);
            }

            m_Material.SetVector(s_SlopeId, slope);

            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;
            var descriptor = renderGraph.GetTextureDesc(source);

            descriptor.name = "_HB_UnderwaterColor";
            descriptor.clearBuffer = false;

            var target = renderGraph.CreateTexture(descriptor);
            var rows = Mathf.Max(Mathf.RoundToInt(LineColumns * (float)descriptor.height /
                                                  Mathf.Max(descriptor.width, 1)), 2);
            var line = renderGraph.CreateTexture(new TextureDesc(LineColumns, rows)
            {
                format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "_HB_UnderLine"
            });

            m_Material.SetVector(s_LineSizeId, new Vector4(LineColumns, rows, 1f / LineColumns, 1f / rows));

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("HiddenBull Style Underwater Line",
                                                                         out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.source = source;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(line, 0, AccessFlags.WriteAll);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, s_Whole, data.material, 1);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("HiddenBull Style Underwater",
                                                                         out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.source = source;
                passData.line = line;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(line, AccessFlags.Read);

                if (resourceData.cameraDepthTexture.IsValid())
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                builder.SetRenderAttachment(target, 0, AccessFlags.WriteAll);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    data.material.SetTexture(s_LineId, data.line);
                    Blitter.BlitTexture(context.cmd, data.source, s_Whole, data.material, 0);
                });
            }

            resourceData.cameraColor = target;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);

            m_Material = null;
        }
    }
}
