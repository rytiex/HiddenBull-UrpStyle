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
        static readonly int s_BrushDistortionId = Shader.PropertyToID("_HB_BrushDistortion");
        static readonly int s_SkyParamsId = Shader.PropertyToID("_HB_SkyParams");
        static readonly int s_SunDirectionId = Shader.PropertyToID("_HB_SunDirection");
        static readonly int s_SunColorId = Shader.PropertyToID("_HB_SunColor");
        static readonly int s_SunDiscId = Shader.PropertyToID("_HB_SunDisc");
        static readonly int s_SunGlowId = Shader.PropertyToID("_HB_SunGlow");
        static readonly int s_MoonColorId = Shader.PropertyToID("_HB_MoonColor");
        static readonly int s_MoonDiscId = Shader.PropertyToID("_HB_MoonDisc");
        static readonly int s_MoonGlowId = Shader.PropertyToID("_HB_MoonGlow");
        static readonly int s_StarParamsId = Shader.PropertyToID("_HB_StarParams");
        static readonly int s_CloudAtlasId = Shader.PropertyToID("_HB_CloudAtlas");
        static readonly int s_CloudParamsId = Shader.PropertyToID("_HB_CloudParams");
        static readonly int s_CloudMotionId = Shader.PropertyToID("_HB_CloudMotion");
        static readonly int s_CloudLightId = Shader.PropertyToID("_HB_CloudLight");
        static readonly int s_CloudAwayLightId = Shader.PropertyToID("_HB_CloudAwayLight");
        static readonly int s_CloudSlabId = Shader.PropertyToID("_HB_CloudSlab");
        static readonly int s_KeyDirectionId = Shader.PropertyToID("_HB_KeyDirection");
        static readonly int s_KeyColorId = Shader.PropertyToID("_HB_KeyColor");
        static readonly int s_CloudTintId = Shader.PropertyToID("_HB_CloudTint");
        static readonly int s_FogParamsId = Shader.PropertyToID("_HB_FogParams");
        static readonly int s_FogScatterId = Shader.PropertyToID("_HB_FogScatter");

        static readonly bool s_LinearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;

        const float HB_Transition = 0.12f;
        const float HB_CloudElevationLift = 0.09f;

        struct SkyTiming
        {
            public float sunTime;
            public float afterglow;
            public Color sunDisc;
            public Color moonDisc;
            public Color cloudLight;
            public Color cloudAway;
            public float cloudFlip;
        }

        class PassData
        {
            public Texture skyLut;
            public Vector4 ambientParams;
            public Texture brushAtlas;
            public Vector4 brushParams;
            public Vector4 brushDistortion;
            public Vector4 skyParams;
            public Vector4 sunDirection;
            public Vector4 sunColor;
            public Vector4 sunDisc;
            public Vector4 sunGlow;
            public Vector4 moonColor;
            public Vector4 moonDisc;
            public Vector4 moonGlow;
            public Vector4 starParams;
            public Texture cloudAtlas;
            public Vector4 cloudParams;
            public Vector4 cloudMotion;
            public Vector4 cloudLight;
            public Vector4 cloudAwayLight;
            public Vector4 cloudSlab;
            public Vector4 cloudTint;
            public Vector4 keyDirection;
            public Vector4 keyColor;
            public Vector4 fogParams;
            public Vector4 fogScatter;
        }

        readonly SkyLutBaker m_Lut = new SkyLutBaker();

        BrushGlobalSettings m_Brush;
        CloudGlobalSettings m_Clouds;

        public StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            profilingSampler = new ProfilingSampler("HiddenBull Style Globals");
        }

        public void Setup(BrushGlobalSettings brush, CloudGlobalSettings clouds)
        {
            m_Brush = brush;
            m_Clouds = clouds;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var sky = stack.GetComponent<StyleSky>();
            var celestial = stack.GetComponent<StyleCelestial>();
            var clouds = stack.GetComponent<StyleClouds>();
            var fog = stack.GetComponent<StyleFog>();

            var sun = ResolveSunLight(frameData);
            var direction = SunDirection(sun);
            var elevation = direction == Vector3.zero ? 1f : direction.y;

            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            PackBrush(m_Brush, passData);

            var timing = PackSky(sky, fog, direction, elevation, passData);

            PackCelestial(celestial, direction, elevation, timing, passData);
            PackClouds(clouds, m_Clouds, timing, passData);
            PackFog(fog, timing.afterglow, passData);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                if (data.skyLut != null)
                    cmd.SetGlobalTexture(s_SkyLutId, data.skyLut);

                cmd.SetGlobalVector(s_AmbientParamsId, data.ambientParams);

                if (data.brushAtlas != null)
                    cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);

                cmd.SetGlobalVector(s_BrushParamsId, data.brushParams);
                cmd.SetGlobalVector(s_BrushDistortionId, data.brushDistortion);

                cmd.SetGlobalVector(s_SkyParamsId, data.skyParams);
                cmd.SetGlobalVector(s_SunDirectionId, data.sunDirection);
                cmd.SetGlobalVector(s_SunColorId, data.sunColor);
                cmd.SetGlobalVector(s_SunDiscId, data.sunDisc);
                cmd.SetGlobalVector(s_SunGlowId, data.sunGlow);
                cmd.SetGlobalVector(s_MoonColorId, data.moonColor);
                cmd.SetGlobalVector(s_MoonDiscId, data.moonDisc);
                cmd.SetGlobalVector(s_MoonGlowId, data.moonGlow);
                cmd.SetGlobalVector(s_StarParamsId, data.starParams);

                if (data.cloudAtlas != null)
                    cmd.SetGlobalTexture(s_CloudAtlasId, data.cloudAtlas);

                cmd.SetGlobalVector(s_CloudParamsId, data.cloudParams);
                cmd.SetGlobalVector(s_CloudMotionId, data.cloudMotion);
                cmd.SetGlobalVector(s_CloudLightId, data.cloudLight);
                cmd.SetGlobalVector(s_CloudAwayLightId, data.cloudAwayLight);
                cmd.SetGlobalVector(s_CloudSlabId, data.cloudSlab);
                cmd.SetGlobalVector(s_KeyDirectionId, data.keyDirection);
                cmd.SetGlobalVector(s_KeyColorId, data.keyColor);
                cmd.SetGlobalVector(s_CloudTintId, data.cloudTint);

                cmd.SetGlobalVector(s_FogParamsId, data.fogParams);
                cmd.SetGlobalVector(s_FogScatterId, data.fogScatter);
            });
        }

        static Vector3 SunDirection(VisibleLight? sun)
        {
            return sun.HasValue
                ? -(Vector3)sun.Value.localToWorldMatrix.GetColumn(2)
                : Vector3.zero;
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
                passData.brushDistortion = Vector4.zero;
                return;
            }

            passData.brushAtlas = brush.atlas;
            passData.brushParams = brush.Pack();
            passData.brushDistortion = brush.PackDistortion();
        }

        SkyTiming PackSky(StyleSky sky, StyleFog fog, Vector3 sunDirection, float sunElevation,
                          PassData passData)
        {
            var scaleOffset = SkyLutBaker.ScaleOffset;

            if (sky == null)
            {
                passData.skyLut = StyleGlobalDefaults.SkyLut;
                passData.ambientParams = StyleGlobalDefaults.AmbientParams;
                passData.skyParams = StyleGlobalDefaults.SkyParams;
                passData.keyDirection = StyleGlobalDefaults.KeyDirection;
                passData.keyColor = new Vector4(0f, 0f, 0f, 1f);
                return new SkyTiming
                {
                    sunDisc = Color.white,
                    moonDisc = Color.white,
                    cloudLight = Color.white,
                    cloudFlip = 1f
                };
            }

            var nightLevel = Mathf.Sin(sky.nightElevation.value * Mathf.Deg2Rad);
            var duskLevel = Mathf.Max(Mathf.Sin(sky.duskElevation.value * Mathf.Deg2Rad),
                                      nightLevel + 0.01f);

            var time = 1f - Mathf.Clamp01(
                Mathf.InverseLerp(nightLevel, duskLevel, sunElevation));

            var spread = sky.sunsetFocus.value * 4f * time * (1f - time);
            var away = Mathf.Clamp01(time + spread * 0.45f);

            passData.skyLut = m_Lut.Bake(
                sky.daySky.value, sky.duskSky.value, sky.nightSky.value,
                sky.dayAmbient.value, sky.duskAmbient.value, sky.nightAmbient.value,
                fog?.dayFog.value, fog?.duskFog.value, fog?.nightFog.value,
                time, away, sky.skyIntensity.value, sky.ambientIntensity.value);

            passData.ambientParams = new Vector4(
                sky.bakedWeight.value, scaleOffset.x, scaleOffset.y, sky.ambientLightBias.value);

            passData.skyParams = new Vector4(
                0.005f, 0.15f, sky.skyBrush.value * 0.15f, sky.skyBrush.value * 0.22f);

            PackKeyLight(sky, sunDirection, sunElevation, passData);

            var skyLight = sky.skyLight.value ?? StyleSky.DefaultSkyLight();
            var lifted = sunElevation + HB_CloudElevationLift;

            var reach = sky.skyLightIntensity.value * Handoff(lifted);
            var drop = spread * 0.35f;

            var cloudLight = EvaluateOverElevation(skyLight, lifted) * reach;
            var cloudAway = EvaluateOverElevation(skyLight, lifted - drop) * reach;

            var straight = sunElevation >= 0f || lifted < 0f;

            return new SkyTiming
            {
                sunTime = Mathf.Clamp01(time * 2f),
                afterglow = Mathf.Clamp01(Mathf.InverseLerp(nightLevel, 0f, sunElevation)),
                sunDisc = EvaluateOverElevation(
                    sky.sunDiscColor.value ?? StyleSky.DefaultSunDiscColor(), sunElevation),
                moonDisc = EvaluateOverElevation(
                    sky.moonDiscColor.value ?? StyleSky.DefaultMoonDiscColor(), -sunElevation),
                cloudLight = cloudLight,
                cloudAway = cloudAway,
                cloudFlip = straight ? 1f : -1f
            };
        }

        static void PackKeyLight(StyleSky sky, Vector3 sunDirection, float sunElevation,
                                 PassData passData)
        {
            if (sunDirection == Vector3.zero)
            {
                passData.keyDirection = StyleGlobalDefaults.KeyDirection;
                passData.keyColor = new Vector4(0f, 0f, 0f, 1f);
                return;
            }

            var gradient = sky.skyLight.value ?? StyleSky.DefaultSkyLight();

            var key = EvaluateOverElevation(gradient, sunElevation)
                    * (sky.skyLightIntensity.value * Handoff(sunElevation));

            var direction = sunElevation >= 0f ? sunDirection : -sunDirection;
            var daylight = Smooth01(sunElevation / HB_Transition);

            passData.keyDirection = new Vector4(
                direction.x, direction.y, direction.z, Handoff(sunElevation));

            passData.keyColor = new Vector4(key.r, key.g, key.b, daylight);
        }

        static void PackCelestial(StyleCelestial celestial, Vector3 sunDirection, float sunElevation,
                                  SkyTiming timing, PassData passData)
        {
            passData.sunDirection = sunDirection == Vector3.zero
                ? new Vector4(0f, 1f, 0f, 0f)
                : new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 1f);

            if (celestial == null)
            {
                passData.sunColor = Vector4.zero;
                passData.moonColor = Vector4.zero;
                passData.starParams = Vector4.zero;
                return;
            }

            var sunTime = timing.sunTime;
            var sun = timing.sunDisc;

            var power = Mathf.Lerp(celestial.sunIntensity.value, celestial.sunDuskIntensity.value, sunTime);

            passData.sunColor = new Vector4(
                sun.r, sun.g, sun.b, sunDirection == Vector3.zero ? 0f : power);

            passData.sunDisc = PackDisc(
                Mathf.Lerp(celestial.sunSize.value, celestial.sunDuskSize.value, sunTime),
                celestial.sunBrush.value);

            passData.sunGlow = PackGlow(
                celestial.sunGlow.value, celestial.sunRays.value, timing.afterglow);

            var moon = timing.moonDisc;
            var moonFade = Smooth01(0.5f - sunElevation / (2f * HB_Transition));

            passData.moonColor = new Vector4(
                moon.r, moon.g, moon.b, celestial.moonIntensity.value * moonFade);

            passData.moonDisc = PackDisc(celestial.moonSize.value, 0.2f);
            passData.moonGlow = PackGlow(celestial.moonGlow.value, 0f);

            var amount = celestial.stars.value;
            var fade = 1f - Mathf.Clamp01(Mathf.InverseLerp(-0.2f, 0.15f, sunElevation));

            passData.starParams = new Vector4(
                80f,
                amount * fade,
                celestial.starTwinkle.value,
                Mathf.Lerp(0.97f, 0.88f, amount));
        }

        static void PackClouds(StyleClouds sky, CloudGlobalSettings settings, SkyTiming timing,
                               PassData passData)
        {
            var atlas = settings != null ? settings.atlas : null;

            if (atlas == null || sky == null)
            {
                passData.cloudAtlas = null;
                passData.cloudParams = Vector4.zero;
                return;
            }

            passData.cloudAtlas = atlas;

            var light = timing.cloudLight;
            var away = timing.cloudAway;

            passData.cloudLight = new Vector4(light.r, light.g, light.b, timing.cloudFlip);
            passData.cloudAwayLight = new Vector4(away.r, away.g, away.b, 0f);

            passData.cloudParams = new Vector4(
                sky.coverage.value,
                sky.scale.value * 0.03f,
                sky.softness.value * 0.5f,
                sky.steps.value);

            var wind = sky.wind.value * 0.004f;

            passData.cloudMotion = new Vector4(
                wind.x, wind.y, sky.brush.value, sky.translucency.value);

            passData.cloudSlab = new Vector4(
                sky.stepSpacing.value * 0.12f,
                sky.stepTaper.value * 0.5f,
                sky.stepShading.value,
                0f);

            var tint = sky.tint.value;
            passData.cloudTint = new Vector4(tint.r, tint.g, tint.b, 1f);
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static float Handoff(float elevation)
        {
            return Smooth01(Mathf.Abs(elevation) / HB_Transition);
        }

        static Color EvaluateOverElevation(Gradient gradient, float sunElevation)
        {
            var authored = gradient.Evaluate(sunElevation * 0.5f + 0.5f);

            return s_LinearColorSpace ? authored.linear : authored;
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
        public static readonly Vector4 KeyDirection = new Vector4(0f, 0f, 0f, 1f);

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
            var lut = s_Lut.Bake(null, null, null, null, null, null, null, null, null,
                                 0f, 0f, 1f, 1f);

            Shader.SetGlobalTexture(Shader.PropertyToID("_HB_SkyLut"), lut);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientParams"), AmbientParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_BrushParams"), BrushParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_SkyParams"), SkyParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_KeyDirection"), KeyDirection);
        }
    }
}
