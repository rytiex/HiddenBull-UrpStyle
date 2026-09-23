using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleGlobalsPass : ScriptableRenderPass, IDisposable
    {
        static readonly int s_SkyLutId = Shader.PropertyToID("_HB_SkyLut");
        static readonly int s_AmbientParamsId = Shader.PropertyToID("_HB_AmbientParams");
        static readonly int s_BrushAtlasId = Shader.PropertyToID("_HB_BrushAtlas");
        static readonly int s_BrushParamsId = Shader.PropertyToID("_HB_BrushParams");
        static readonly int s_SkyParamsId = Shader.PropertyToID("_HB_SkyParams");
        static readonly int s_SunDirectionId = Shader.PropertyToID("_HB_SunDirection");
        static readonly int s_SunColorId = Shader.PropertyToID("_HB_SunColor");
        static readonly int s_SunDiscId = Shader.PropertyToID("_HB_SunDisc");
        static readonly int s_SunGlowId = Shader.PropertyToID("_HB_SunGlow");
        static readonly int s_MoonColorId = Shader.PropertyToID("_HB_MoonColor");
        static readonly int s_MoonDiscId = Shader.PropertyToID("_HB_MoonDisc");
        static readonly int s_MoonGlowId = Shader.PropertyToID("_HB_MoonGlow");
        static readonly int s_StarParamsId = Shader.PropertyToID("_HB_StarParams");
        static readonly int s_FogParamsId = Shader.PropertyToID("_HB_FogParams");
        static readonly int s_FogScatterId = Shader.PropertyToID("_HB_FogScatter");
        static readonly int s_FogTintId = Shader.PropertyToID("_HB_FogTint");

        class PassData
        {
            public Texture skyLut;
            public Vector4 ambientParams;
            public Texture brushAtlas;
            public Vector4 brushParams;
            public Vector4 skyParams;
            public Vector4 sunDirection;
            public Vector4 sunColor;
            public Vector4 sunDisc;
            public Vector4 sunGlow;
            public Vector4 moonColor;
            public Vector4 moonDisc;
            public Vector4 moonGlow;
            public Vector4 starParams;
            public Vector4 fogParams;
            public Vector4 fogScatter;
            public Vector4 fogTint;
        }

        readonly SkyLutBaker m_Lut = new SkyLutBaker();

        BrushGlobalSettings m_Brush;

        public StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            profilingSampler = new ProfilingSampler("HiddenBull Style Globals");
        }

        public void Setup(BrushGlobalSettings brush)
        {
            m_Brush = brush;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var sky = stack.GetComponent<StyleSky>();
            var fog = stack.GetComponent<StyleFog>();

            var sun = ResolveSunLight(frameData);
            var elevation = SunElevation(sun);

            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            PackBrush(m_Brush, passData);

            var sunVisibility = PackSky(sky, sun, elevation, passData);
            PackFog(fog, sunVisibility, passData);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                if (data.skyLut != null)
                    cmd.SetGlobalTexture(s_SkyLutId, data.skyLut);

                cmd.SetGlobalVector(s_AmbientParamsId, data.ambientParams);

                if (data.brushAtlas != null)
                    cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);

                cmd.SetGlobalVector(s_BrushParamsId, data.brushParams);

                cmd.SetGlobalVector(s_SkyParamsId, data.skyParams);
                cmd.SetGlobalVector(s_SunDirectionId, data.sunDirection);
                cmd.SetGlobalVector(s_SunColorId, data.sunColor);
                cmd.SetGlobalVector(s_SunDiscId, data.sunDisc);
                cmd.SetGlobalVector(s_SunGlowId, data.sunGlow);
                cmd.SetGlobalVector(s_MoonColorId, data.moonColor);
                cmd.SetGlobalVector(s_MoonDiscId, data.moonDisc);
                cmd.SetGlobalVector(s_MoonGlowId, data.moonGlow);
                cmd.SetGlobalVector(s_StarParamsId, data.starParams);

                cmd.SetGlobalVector(s_FogParamsId, data.fogParams);
                cmd.SetGlobalVector(s_FogScatterId, data.fogScatter);
                cmd.SetGlobalVector(s_FogTintId, data.fogTint);
            });
        }

        static float SunElevation(VisibleLight? sun)
        {
            return sun.HasValue ? -((Vector3)sun.Value.localToWorldMatrix.GetColumn(2)).y : 1f;
        }

        static VisibleLight? ResolveSunLight(ContextContainer frameData)
        {
            if (!frameData.Contains<UniversalLightData>())
                return null;

            var lightData = frameData.Get<UniversalLightData>();
            var index = lightData.mainLightIndex;

            if (index < 0 || index >= lightData.visibleLights.Length)
                return null;

            var light = lightData.visibleLights[index];
            return light.lightType == LightType.Directional ? light : (VisibleLight?)null;
        }

        static void PackBrush(BrushGlobalSettings brush, PassData passData)
        {
            if (brush == null || brush.atlas == null)
            {
                passData.brushAtlas = null;
                passData.brushParams = StyleGlobalDefaults.BrushParams;
                return;
            }

            passData.brushAtlas = brush.atlas;
            passData.brushParams = brush.Pack();
        }

        float PackSky(StyleSky sky, VisibleLight? sun, float sunElevation, PassData passData)
        {
            var scaleOffset = SkyLutBaker.ScaleOffset;

            if (sky == null)
            {
                passData.skyLut = StyleGlobalDefaults.SkyLut;
                passData.ambientParams = StyleGlobalDefaults.AmbientParams;
                passData.skyParams = StyleGlobalDefaults.SkyParams;
                passData.sunDirection = new Vector4(0f, 1f, 0f, 0f);
                passData.sunColor = Vector4.zero;
                passData.moonColor = Vector4.zero;
                passData.starParams = Vector4.zero;
                return 0f;
            }

            var duskLevel = Mathf.Max(sky.duskElevation.value, sky.nightElevation.value + 0.01f);
            var time = 1f - Mathf.Clamp01(
                Mathf.InverseLerp(sky.nightElevation.value, duskLevel, sunElevation));

            passData.skyLut = m_Lut.Bake(
                sky.daySky.value, sky.duskSky.value, sky.nightSky.value,
                sky.dayAmbient.value, sky.duskAmbient.value, sky.nightAmbient.value,
                time, sky.skyIntensity.value, sky.ambientIntensity.value);

            passData.ambientParams = new Vector4(
                sky.bakedWeight.value, scaleOffset.x, scaleOffset.y, 0f);

            passData.skyParams = new Vector4(0.005f, 0.15f, 0f, 0f);

            var sunTime = Mathf.Clamp01(time * 2f);
            var sunDirection = Vector3.up;
            var lightColor = Color.white;

            if (sun.HasValue)
            {
                sunDirection = -(Vector3)sun.Value.localToWorldMatrix.GetColumn(2);
                lightColor = sun.Value.light != null ? sun.Value.light.color : Color.white;
            }

            passData.sunDirection = new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 1f);

            var tintedSun = Color.Lerp(sky.sunColor.value, sky.sunDuskColor.value, sunTime) * lightColor;
            var sunPower = Mathf.Lerp(sky.sunIntensity.value, sky.sunDuskIntensity.value, sunTime);

            passData.sunColor = new Vector4(
                tintedSun.r, tintedSun.g, tintedSun.b, sun.HasValue ? sunPower : 0f);

            var afterglow = Mathf.Clamp01(Mathf.InverseLerp(sky.nightElevation.value, 0f, sunElevation));

            passData.sunDisc = PackDisc(
                Mathf.Lerp(sky.sunSize.value, sky.sunDuskSize.value, sunTime), sky.sunBrush.value);
            passData.sunGlow = PackGlow(sky.sunGlow.value, sky.sunRays.value, afterglow);

            var moon = sky.moonColor.value;
            var moonFade = Mathf.Clamp01(Mathf.InverseLerp(0.08f, -0.08f, sunElevation));

            passData.moonColor = new Vector4(moon.r, moon.g, moon.b, sky.moonIntensity.value * moonFade);
            passData.moonDisc = PackDisc(sky.moonSize.value, 0.2f);
            passData.moonGlow = PackGlow(sky.moonGlow.value, 0f);

            var amount = sky.stars.value;
            var fade = 1f - Mathf.Clamp01(Mathf.InverseLerp(-0.2f, 0.15f, sunElevation));

            passData.starParams = new Vector4(
                80f,
                amount * fade,
                sky.starTwinkle.value,
                Mathf.Lerp(0.97f, 0.88f, amount));

            return afterglow;
        }

        static Vector4 PackDisc(float sizeDegrees, float brush)
        {
            var radians = sizeDegrees * Mathf.Deg2Rad;

            return new Vector4(
                Mathf.Cos(radians),
                Mathf.Sin(radians * 0.25f),
                brush,
                0f);
        }

        static Vector4 PackGlow(float glow, float rays, float fade = 1f)
        {
            return new Vector4(
                Mathf.Lerp(2000f, 40f, glow),
                glow * fade,
                rays,
                0f);
        }

        static void PackFog(StyleFog fog, float sunVisibility, PassData passData)
        {
            passData.fogTint = Vector4.one;

            if (fog == null)
            {
                passData.fogParams = Vector4.zero;
                passData.fogScatter = Vector4.zero;
                return;
            }

            var start = Mathf.Max(fog.startDistance.value, 0f);
            var range = Mathf.Max(fog.endDistance.value - start, 0f);
            var density = range > 0.01f ? 3f / range : 0f;

            passData.fogParams = new Vector4(
                density, fog.heightFalloff.value, fog.baseHeight.value, fog.maxOpacity.value);

            passData.fogScatter = new Vector4(fog.sunScattering.value, 8f, start, sunVisibility);

            var tint = fog.tint.value;
            passData.fogTint = new Vector4(tint.r, tint.g, tint.b, 1f);
        }

        public void Dispose()
        {
            m_Lut.Dispose();
        }
    }

    public static class StyleGlobalDefaults
    {
        public static readonly Vector4 BrushParams = new Vector4(1f, 15f, 1f / 15f, 0f);
        public static readonly Vector4 SkyParams = new Vector4(0.005f, 0.15f, 0f, 0f);

        static readonly SkyLutBaker s_Lut = new SkyLutBaker();

        public static Vector4 AmbientParams
        {
            get
            {
                var scaleOffset = SkyLutBaker.ScaleOffset;
                return new Vector4(0f, scaleOffset.x, scaleOffset.y, 0f);
            }
        }

        public static Texture SkyLut => s_Lut.texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            var lut = s_Lut.Bake(null, null, null, null, null, null, 0f, 1f, 1f);

            Shader.SetGlobalTexture(Shader.PropertyToID("_HB_SkyLut"), lut);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientParams"), AmbientParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_BrushParams"), BrushParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_SkyParams"), SkyParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_FogTint"), Vector4.one);
        }
    }
}
