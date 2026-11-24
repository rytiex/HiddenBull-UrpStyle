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
        const int FlarePass = 4;
        const int SunPass = 5;
        const int DebugPass = 6;
        const int LightSharp = 1;
        const int LightWide = 3;
        const int MostSteps = 8;
        const string StableKeyword = "HB_BLOOM_STABLE";

        static readonly int s_SourceId = Shader.PropertyToID("_HB_BloomSource");
        static readonly int s_LowId = Shader.PropertyToID("_HB_BloomLow");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_BloomParams");
        static readonly int s_TintId = Shader.PropertyToID("_HB_BloomTint");
        static readonly int s_FlareParamsId = Shader.PropertyToID("_HB_FlareParams");
        static readonly int s_FlareSunId = Shader.PropertyToID("_HB_FlareSun");
        static readonly int s_DepthId = Shader.PropertyToID("_CameraDepthTexture");

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

        class FlareData
        {
            internal Material material;
            internal MaterialPropertyBlock block;
            internal TextureHandle sharp;
            internal TextureHandle wide;
            internal TextureHandle sun;
            internal TextureHandle depth;
            internal int pass;
            internal Vector4 parameters;
            internal Vector4 shape;
            internal Vector4 strength;
        }

        readonly MaterialPropertyBlock m_Block = new MaterialPropertyBlock();
        readonly TextureHandle[] m_Down = new TextureHandle[MostSteps];

        Material m_Material;
        HBStyleBloomQuality m_Quality;
        HBStyleBloomSettings m_Settings;
        HBStyleLensFlareSettings m_Flare;
        StyleGlobalsPass m_Globals;

        internal StyleBloomPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
            profilingSampler = new ProfilingSampler("HB Style Bloom");
        }

        internal static bool Wanted(HBStyleBloomQuality quality, HBStyleBloomSettings settings,
                                    HBStyleLensFlareSettings flare)
        {
            return quality != null && quality.level != HBStyleQualityLevel.Off && settings != null &&
                   (settings.intensity > 0f ||
                    flare != null && HBStyleRenderSettings.lensFlareOn &&
                    (flare.sunIntensity > 0f || flare.lightIntensity > 0f));
        }

        internal void Setup(Shader shader, HBStyleBloomQuality quality, HBStyleBloomSettings settings,
                           HBStyleLensFlareSettings flare, StyleGlobalsPass globals)
        {
            m_Globals = globals;
            if (m_Material == null && shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Quality = quality;
            m_Settings = settings;
            m_Flare = HBStyleRenderSettings.lensFlareOn ? flare : null;
            ConfigureInput(m_Flare != null && m_Flare.sunIntensity > 0f
                               ? ScriptableRenderPassInput.Depth
                               : ScriptableRenderPassInput.None);
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

            var lights = m_Flare != null && m_Flare.lightIntensity > 0f;

            if (bloom.intensity <= 0f)
                steps = lights ? Mathf.Min(steps, LightWide + 1) : 0;

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

            if (bloom.intensity > 0f)
            {
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

            if (m_Flare == null)
                return;

            var wide = Mathf.Min(LightWide, steps - 1);
            var depth = resourceData.cameraDepthTexture;
            var cameraData = frameData.Get<UniversalCameraData>();
            var screen = new Vector2(Mathf.Max(1, cameraData.cameraTargetDescriptor.width),
                                     Mathf.Max(1, cameraData.cameraTargetDescriptor.height));
            var place = SunPlace(cameraData);
            var sun = m_Flare.sunIntensity > 0f ? DrawSun(renderGraph, depth, place, screen) : TextureHandle.nullHandle;

            if (StyleDebug.view == StyleDebugView.LensFlareSun && depth.IsValid())
                DrawFlare(renderGraph, TextureHandle.nullHandle, TextureHandle.nullHandle, sun, depth, source,
                          DebugPass, new Vector4(sun.IsValid() ? 1f : 0f, 0f, 1f / screen.x, 1f / screen.y),
                          Vector4.zero, place);

            lights &= wide > LightSharp;

            if (!sun.IsValid() && !lights)
                return;

            var settings = m_Flare;
            var target = renderGraph.CreateTexture(new TextureDesc(width, height)
            {
                name = "HB Style Lens Flare",
                format = format,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = false
            });
            var flare = new Vector4(settings.lightThreshold, settings.ghosts, settings.ghostSpacing, settings.haloSize);
            var shape = new Vector4(settings.halo, settings.colourFringe, 1f / width, 1f / height);
            var strength = new Vector4(sun.IsValid() ? settings.sunIntensity : 0f,
                                       lights ? settings.lightIntensity : 0f, settings.rays, 0f);

            DrawFlare(renderGraph, lights ? m_Down[LightSharp] : TextureHandle.nullHandle,
                      lights ? m_Down[wide] : TextureHandle.nullHandle, sun, TextureHandle.nullHandle, target,
                      FlarePass, flare, shape, strength);
            Draw(renderGraph, TextureHandle.nullHandle, target, source, CompositePass, new Vector4(0f, 0f, 0f, 1f),
                 (Vector4)settings.tint.linear, true);
        }

        Vector4 SunPlace(UniversalCameraData cameraData)
        {
            var size = cameraData.cameraTargetDescriptor;
            var aspect = (float)Mathf.Max(1, size.width) / Mathf.Max(1, size.height);

            if (m_Globals == null || cameraData.camera.orthographic || Underwater(cameraData.camera))
                return new Vector4(0f, 0f, 0f, aspect);

            var direction = m_Globals.sunDirection;
            var clip = cameraData.GetProjectionMatrix() *
                       (cameraData.GetViewMatrix() * new Vector4(direction.x, direction.y, direction.z, 0f));

            if (clip.w <= 1e-4f)
                return new Vector4(0f, 0f, 0f, aspect);

            return new Vector4(clip.x / clip.w * 0.5f + 0.5f, clip.y / clip.w * 0.5f + 0.5f, 1f, aspect);
        }

        static bool Underwater(Camera camera)
        {
            var eye = camera.transform.position;

            return HBStyleLiquid.any && HBStyleLiquid.TryGetSubmerged(eye, out var level, out _) && eye.y < level;
        }

        TextureHandle DrawSun(RenderGraph renderGraph, TextureHandle depth, Vector4 place, Vector2 screen)
        {
            if (!depth.IsValid() || place.z <= 0f)
                return TextureHandle.nullHandle;

            var target = renderGraph.CreateTexture(new TextureDesc(1, 1)
            {
                name = "HB Style Lens Flare Sun",
                format = GraphicsFormat.R16G16B16A16_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = false
            });

            DrawFlare(renderGraph, TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle,
                      depth, target, SunPass, new Vector4(screen.x, screen.y, 0f, 0f), Vector4.zero, place);
            return target;
        }

        void DrawFlare(RenderGraph renderGraph, TextureHandle sharp, TextureHandle wide, TextureHandle sun,
                       TextureHandle depth, TextureHandle target, int pass, Vector4 parameters, Vector4 shape,
                       Vector4 strength)
        {
            using var builder = renderGraph.AddRasterRenderPass<FlareData>("HB Style Lens Flare", out var data,
                                                                          profilingSampler);

            data.material = m_Material;
            data.block = m_Block;
            data.sharp = sharp;
            data.wide = wide;
            data.sun = sun;
            data.depth = depth;
            data.pass = pass;
            data.parameters = parameters;
            data.shape = shape;
            data.strength = strength;

            if (sharp.IsValid())
                builder.UseTexture(sharp, AccessFlags.Read);

            if (wide.IsValid())
                builder.UseTexture(wide, AccessFlags.Read);

            if (sun.IsValid())
                builder.UseTexture(sun, AccessFlags.Read);

            if (depth.IsValid())
                builder.UseTexture(depth, AccessFlags.Read);

            builder.SetRenderAttachment(target, 0, AccessFlags.WriteAll);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (FlareData d, RasterGraphContext context) =>
            {
                d.block.Clear();

                if (d.sharp.IsValid())
                    d.block.SetTexture(s_SourceId, d.sharp);

                if (d.wide.IsValid())
                    d.block.SetTexture(s_LowId, d.wide);

                if (d.sun.IsValid())
                    d.block.SetTexture(s_FlareSunId, d.sun);

                if (d.depth.IsValid())
                    d.block.SetTexture(s_DepthId, d.depth);

                d.block.SetVector(s_ParamsId, d.parameters);
                d.block.SetVector(s_TintId, d.shape);
                d.block.SetVector(s_FlareParamsId, d.strength);
                context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1,
                                           d.block);
            });
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
