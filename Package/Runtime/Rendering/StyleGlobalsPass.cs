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
        static readonly int s_AmbientFloorId = Shader.PropertyToID("_HB_AmbientFloor");
        static readonly int s_SkyLutRemapId = Shader.PropertyToID("_HB_SkyLutRemap");
        static readonly int s_BrushAtlasId = Shader.PropertyToID("_HB_BrushAtlas");
        static readonly int s_BrushParamsId = Shader.PropertyToID("_HB_BrushParams");
        static readonly int s_SkyParamsId = Shader.PropertyToID("_HB_SkyParams");
        static readonly int s_SkyPaintId = Shader.PropertyToID("_HB_SkyPaint");
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
        static readonly int s_ShadowBrushId = Shader.PropertyToID("_HB_ShadowBrush");
        static readonly int s_ShadowFilterId = Shader.PropertyToID("_HB_ShadowFilter");
        static readonly int s_ShadowDebugId = Shader.PropertyToID("_HB_ShadowDebug");

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
            public Vector4 ambientFloor;
            public Vector4 skyLutRemap;
            public Texture brushAtlas;
            public Vector4 brushParams;
            public Vector4 skyParams;
            public Vector4 skyPaint;
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
            public Vector4 shadowBrush;
            public Vector4 shadowFilter;
            public Vector4 shadowDebug;
        }

        readonly SkyLutBaker m_Lut = new SkyLutBaker();

        BrushGlobalSettings m_Brush;
        CloudGlobalSettings m_Clouds;
        ShadowQualitySettings m_Shadows;
        Light m_Sun;
        int m_SunSearched = -60;

        public StyleGlobalsPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            profilingSampler = new ProfilingSampler("HiddenBull Style Globals");
        }

        public void Setup(BrushGlobalSettings brush, CloudGlobalSettings clouds,
                          ShadowQualitySettings shadows)
        {
            m_Brush = brush;
            m_Clouds = clouds;
            m_Shadows = shadows;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var sky = stack.GetComponent<StyleSky>();
            var celestial = stack.GetComponent<StyleCelestial>();
            var clouds = stack.GetComponent<StyleClouds>();
            var fog = stack.GetComponent<StyleFog>();

            var direction = ResolveSunDirection(frameData, out var realtimeSun);
            var elevation = direction == Vector3.zero ? 1f : direction.y;

            using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            PackBrush(m_Brush, passData);

            var cascades = frameData.Get<UniversalShadowData>().mainLightShadowCascadesCount;

            passData.shadowBrush = m_Shadows == null
                ? StyleGlobalDefaults.ShadowBrush
                : m_Shadows.PackBrush(passData.brushAtlas != null, cascades);

            passData.shadowFilter = m_Shadows == null
                ? StyleGlobalDefaults.ShadowFilter
                : m_Shadows.PackFilter();

            passData.shadowDebug = m_Shadows == null
                ? StyleGlobalDefaults.ShadowDebug
                : m_Shadows.PackDebug();

            var timing = PackSky(sky, fog, direction, elevation, realtimeSun, passData);

            PackCelestial(celestial, direction, elevation, timing, passData);
            PackClouds(clouds, m_Clouds, timing, passData);
            PackFog(fog, timing.afterglow, passData);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                if (data.skyLut != null)
                    cmd.SetGlobalTexture(s_SkyLutId, data.skyLut);

                cmd.SetGlobalVector(s_AmbientParamsId, data.ambientParams);
                cmd.SetGlobalVector(s_AmbientFloorId, data.ambientFloor);
                cmd.SetGlobalVector(s_SkyLutRemapId, data.skyLutRemap);

                if (data.brushAtlas != null)
                    cmd.SetGlobalTexture(s_BrushAtlasId, data.brushAtlas);

                cmd.SetGlobalVector(s_BrushParamsId, data.brushParams);

                cmd.SetGlobalVector(s_SkyParamsId, data.skyParams);
                cmd.SetGlobalVector(s_SkyPaintId, data.skyPaint);
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
                cmd.SetGlobalVector(s_ShadowBrushId, data.shadowBrush);
                cmd.SetGlobalVector(s_ShadowFilterId, data.shadowFilter);
                cmd.SetGlobalVector(s_ShadowDebugId, data.shadowDebug);
            });
        }

        Vector3 ResolveSunDirection(ContextContainer frameData, out bool realtime)
        {
            realtime = false;

            if (frameData.Contains<UniversalLightData>())
            {
                var lightData = frameData.Get<UniversalLightData>();
                var index = lightData.mainLightIndex;

                if (index >= 0 && index < lightData.visibleLights.Length)
                {
                    var visible = lightData.visibleLights[index];

                    if (visible.lightType == LightType.Directional)
                    {
                        realtime = true;
                        return -(Vector3)visible.localToWorldMatrix.GetColumn(2);
                    }
                }
            }

            var sun = ScenesSun();

            return sun != null ? -sun.transform.forward : Vector3.zero;
        }

        Light ScenesSun()
        {
            if (RenderSettings.sun != null)
                return RenderSettings.sun;

            if (m_Sun != null)
                return m_Sun;

            if (Time.frameCount - m_SunSearched < 60)
                return null;

            m_SunSearched = Time.frameCount;

            var brightest = 0f;

            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Exclude))
            {
                if (light.type != LightType.Directional || light.intensity <= brightest)
                    continue;

                brightest = light.intensity;
                m_Sun = light;
            }

            return m_Sun;
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

        SkyTiming PackSky(StyleSky sky, StyleFog fog, Vector3 sunDirection, float sunElevation,
                          bool realtimeSun, PassData passData)
        {
            if (sky == null)
            {
                passData.skyLut = StyleGlobalDefaults.SkyLut;
                passData.ambientParams = StyleGlobalDefaults.AmbientParams;
                passData.skyLutRemap = StyleGlobalDefaults.SkyLutRemap;
                passData.ambientFloor = StyleGlobalDefaults.AmbientFloor;
                passData.skyParams = StyleGlobalDefaults.SkyParams;
                passData.skyPaint = StyleGlobalDefaults.SkyPaint;
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
                fog?.dayFog.value, fog?.duskFog.value, fog?.nightFog.value);

            var scaleOffset = SkyLutBaker.ScaleOffset;

            passData.ambientParams = new Vector4(
                sky.ambientLightBias.value,
                sky.ambientIntensity.value,
                sky.skyIntensity.value,
                ProbeLighting());

            passData.skyLutRemap = new Vector4(
                scaleOffset.x, scaleOffset.y,
                SkyLutBaker.TimeCoordinate(time), SkyLutBaker.TimeCoordinate(away));

            passData.ambientFloor = PackAmbientFloor(
                m_Lut.GroundAmbient(time, sky.ambientIntensity.value),
                m_Lut.HorizonAmbient(time, sky.ambientIntensity.value));

            passData.skyParams = new Vector4(
                sky.skyBrushScale.value, sky.sunsetFocus.value,
                sky.skyBrush.value, sky.skyBrushSmoothness.value);

            passData.skyPaint = new Vector4(sky.skyPaint.value, 0f, 0f, 0f);

            PackKeyLight(sky, sunDirection, sunElevation, realtimeSun, passData);

            var skyLight = sky.skyLight.value ?? StyleSky.DefaultSkyLight();
            var lifted = sunElevation + HB_CloudElevationLift;

            var reach = sky.skyLightIntensity.value * StyleKeyLight.Handoff(lifted);
            var drop = spread * 0.35f;

            var cloudLight = StyleKeyLight.EvaluateOverElevation(skyLight, lifted) * reach;
            var cloudAway = StyleKeyLight.EvaluateOverElevation(skyLight, lifted - drop) * reach;

            var straight = sunElevation >= 0f || lifted < 0f;

            return new SkyTiming
            {
                sunTime = Mathf.Clamp01(time * 2f),
                afterglow = Mathf.Clamp01(Mathf.InverseLerp(nightLevel, 0f, sunElevation)),
                sunDisc = StyleKeyLight.EvaluateOverElevation(
                    sky.sunDiscColor.value ?? StyleSky.DefaultSunDiscColor(), sunElevation),
                moonDisc = StyleKeyLight.EvaluateOverElevation(
                    sky.moonDiscColor.value ?? StyleSky.DefaultMoonDiscColor(), -sunElevation),
                cloudLight = cloudLight,
                cloudAway = cloudAway,
                cloudFlip = straight ? 1f : -1f
            };
        }

        static void PackKeyLight(StyleSky sky, Vector3 sunDirection, float sunElevation,
                                 bool realtime, PassData passData)
        {
            if (sunDirection == Vector3.zero)
            {
                passData.keyDirection = StyleGlobalDefaults.KeyDirection;
                passData.keyColor = new Vector4(0f, 0f, 0f, 1f);
                return;
            }

            var gradient = sky.skyLight.value ?? StyleSky.DefaultSkyLight();

            var key = realtime
                ? StyleKeyLight.EvaluateOverElevation(gradient, sunElevation)
                  * (sky.skyLightIntensity.value * StyleKeyLight.Handoff(sunElevation))
                : Color.black;

            var direction = sunElevation >= 0f ? sunDirection : -sunDirection;
            var daylight = realtime ? StyleKeyLight.Smooth01(sunElevation / StyleKeyLight.Transition) : 1f;

            passData.keyDirection = new Vector4(
                direction.x, direction.y, direction.z, StyleKeyLight.Handoff(sunElevation));

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
            var moonFade = StyleKeyLight.Smooth01(0.5f - sunElevation / (2f * StyleKeyLight.Transition));

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
                wind.x, wind.y, 0f, sky.translucency.value);

            passData.cloudSlab = new Vector4(
                sky.stepSpacing.value * 0.12f,
                sky.stepTaper.value * 0.5f,
                sky.stepShading.value,
                0f);

            var tint = sky.tint.value;
            passData.cloudTint = new Vector4(tint.r, tint.g, tint.b, sky.tintShade.value);
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

        internal static bool ProbeVolumesBaked()
        {
            var probes = ProbeReferenceVolume.instance;

            return probes != null && probes.isInitialized && probes.DataHasBeenLoaded();
        }

        static bool ProbeLightingBaked()
        {
            var volumes = ProbeReferenceVolume.instance;

            if (volumes != null && volumes.isInitialized)
                return volumes.DataHasBeenLoaded();

            var probes = LightmapSettings.lightProbes;

            return probes != null && probes.count > 0;
        }

        float ProbeLighting()
        {
            if (!ProbeLightingBaked())
                return 0f;

            var sun = ScenesSun();

            return sun != null && !SunInProbes(sun.bakingOutput) ? 2f : 1f;
        }

        static bool SunInProbes(LightBakingOutput baking)
        {
            if (!baking.isBaked)
                return false;

            return baking.lightmapBakeType == LightmapBakeType.Baked
                || (baking.lightmapBakeType == LightmapBakeType.Mixed
                    && baking.mixedLightingMode == MixedLightingMode.Subtractive);
        }

        internal static Vector4 PackAmbientFloor(Color ground, Color horizon)
        {
            return new Vector4(ground.r, ground.g, ground.b,
                horizon.r * 0.2126f + horizon.g * 0.7152f + horizon.b * 0.0722f);
        }

        static void PackFog(StyleFog fog, float sunVisibility, PassData passData)
        {
            if (fog == null || StyleBakeState.baking)
            {
                passData.fogParams = Vector4.zero;
                passData.fogScatter = Vector4.zero;
                passData.skyPaint.y = 0f;
                passData.skyPaint.z = 0f;
                return;
            }

            passData.skyPaint.y = fog.paint.value;
            passData.skyPaint.z = fog.silhouette.value;

            var start = Mathf.Max(fog.startDistance.value, 0f);
            var range = Mathf.Max(fog.endDistance.value - start, 0f);
            var density = range > 0.01f ? 3f / range : 0f;

            passData.fogParams = new Vector4(
                density, fog.heightFalloff.value, fog.baseHeight.value, fog.maxOpacity.value);

            passData.fogScatter = new Vector4(
                fog.sunScattering.value,
                ProbeVolumesBaked() ? Mathf.Clamp01(fog.shade.value) : 0f,
                start, sunVisibility);
        }

        public void Dispose()
        {
            m_Lut.Dispose();
        }
    }

    public static class StyleGlobalDefaults
    {
        public static readonly Vector4 BrushParams = new Vector4(1f, 15f, 1f / 15f, 0f);
        public static readonly Vector4 ShadowFilter = new Vector4(0.225f, 4.5f, 12f, 0f);
        public static readonly Vector4 ShadowBrush = new Vector4(0f, 1f, 1f, 1f);
        public static readonly Vector4 ShadowDebug = new Vector4(0f, 1f, 1f, 1f);
        public static readonly Vector4 SkyParams = new Vector4(3f, 0f, 0f, 0f);
        public static readonly Vector4 SkyPaint = Vector4.zero;
        public static readonly Vector4 KeyDirection = new Vector4(0f, 0f, 0f, 1f);

        static readonly SkyLutBaker s_Lut = new SkyLutBaker();

        public static readonly Vector4 AmbientParams = new Vector4(0f, 1f, 1f, 0f);

        public static Vector4 SkyLutRemap
        {
            get
            {
                var scaleOffset = SkyLutBaker.ScaleOffset;
                var noon = SkyLutBaker.TimeCoordinate(0f);

                return new Vector4(scaleOffset.x, scaleOffset.y, noon, noon);
            }
        }

        public static Texture SkyLut => s_Lut.texture;

        public static Vector4 AmbientFloor => StyleGlobalsPass.PackAmbientFloor(
            s_Lut.GroundAmbient(0f, 1f), s_Lut.HorizonAmbient(0f, 1f));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            var lut = s_Lut.Bake(null, null, null, null, null, null, null, null, null);

            Shader.SetGlobalTexture(Shader.PropertyToID("_HB_SkyLut"), lut);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientParams"), AmbientParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_SkyLutRemap"), SkyLutRemap);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_AmbientFloor"), AmbientFloor);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_BrushParams"), BrushParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_SkyParams"), SkyParams);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_SkyPaint"), SkyPaint);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_KeyDirection"), KeyDirection);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_ShadowBrush"), ShadowBrush);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_ShadowFilter"), ShadowFilter);
            Shader.SetGlobalVector(Shader.PropertyToID("_HB_ShadowDebug"), ShadowDebug);
        }
    }
}
