using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [StructLayout(LayoutKind.Sequential)]
    struct StyleFrameGlobals
    {
        internal const string BufferName = "HBStyleFrame";

        const float OvercastAmbientLift = 0.75f;
        const float CloudElevationLift = 0.09f;

        static Gradient s_DefaultAurora;

        public Vector4 _HB_Environment;
        public Vector4 _HB_AmbientFloor;
        public Vector4 _HB_Weather;
        public Vector4 _HB_WeatherSurface;
        public Vector4 _HB_WeatherLook;
        public Vector4 _HB_Precipitation;
        public Vector4 _HB_Brush;
        public Vector4 _HB_BrushVariation;
        public Vector4 _HB_ShadowFilter;
        public Vector4 _HB_ShadowBrush;
        public Vector4 _HB_Sky;
        public Vector4 _HB_SkyBrush;
        public Vector4 _HB_SunDirection;
        public Vector4 _HB_SunColor;
        public Vector4 _HB_SunDisc;
        public Vector4 _HB_SunGlow;
        public Vector4 _HB_MoonColor;
        public Vector4 _HB_MoonDisc;
        public Vector4 _HB_MoonGlow;
        public Vector4 _HB_Stars;
        public Vector4 _HB_StarLook;
        public Vector4 _HB_Aurora;
        public Vector4 _HB_AuroraLow;
        public Vector4 _HB_AuroraMid;
        public Vector4 _HB_AuroraHigh;
        public Vector4 _HB_CloudParams;
        public Vector4 _HB_CloudMotion;
        public Vector4 _HB_CloudLight;
        public Vector4 _HB_CloudAwayLight;
        public Vector4 _HB_CloudDirection;
        public Vector4 _HB_CloudSlab;
        public Vector4 _HB_CloudTint;
        public Vector4 _HB_CloudLook;
        public Vector4 _HB_Fog;
        public Vector4 _HB_FogScatter;
        public Vector4 _HB_Horizon;
        public Vector4 _HB_Debug;

        internal static StyleFrameGlobals Resolve(HBStyleEnvironmentProfile profile, StyleSkyLut lut,
                                                  HBStyleQualityProfile quality, int cascades)
        {
            var globals = new StyleFrameGlobals();

            var timeOfDay = StyleEnvironment.timeOfDay;
            var sky = profile.sky;
            var elevation = StyleKeyLight.SunElevation(timeOfDay);
            var controller = StyleEnvironment.controller;
            var sunDirection = controller != null
                ? controller.sunDirection
                : -(Quaternion.Euler(HBStyleEnvironmentController.SunAngle(timeOfDay), 0f, 0f) * Vector3.forward);

            var blend = StyleKeyLight.DayBlend(sky, elevation);
            var spread = sky.sunsetFocus * 4f * blend * (1f - blend);
            var away = Mathf.Clamp01(blend + spread * 0.45f);
            var overcast = StyleKeyLight.Overcast(profile);
            var night = Mathf.Sin(sky.nightElevation * Mathf.Deg2Rad);
            var afterglow = Mathf.Clamp01(Mathf.InverseLerp(night, 0f, elevation));

            var ambientIntensity = sky.ambientIntensity * (1f + overcast * OvercastAmbientLift);
            var ground = lut.Ground(blend) * ambientIntensity;

            globals._HB_Environment = new Vector4(timeOfDay, StyleSkyLut.RowCoordinate(blend),
                                                  sky.ambientLightBias * StyleKeyLight.Handoff(elevation)
                                                  * (1f - overcast), ambientIntensity);
            globals._HB_AmbientFloor = new Vector4(ground.r, ground.g, ground.b, ProbeLightingBaked() ? 1f : 0f);

            PackWeather(profile.weather, quality.weather, ref globals);

            PackBrush(profile.brush, quality.shadows, cascades, ref globals);

            globals._HB_Sky = new Vector4(sky.skyIntensity, sky.sunsetFocus, sky.skyBrush, sky.skyBrushSmoothness);
            globals._HB_SkyBrush = new Vector4(sky.skyBrushScale, sky.skyPaint, StyleSkyLut.RowCoordinate(away), blend);
            globals._HB_SunDirection = new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 1f);

            PackCelestial(profile, elevation, blend, afterglow, ref globals);
            PackClouds(profile, sunDirection, elevation, spread, ref globals);
            PackFog(profile.atmosphere, quality.atmosphere, afterglow * (1f - overcast), ref globals);

            globals._HB_Debug = new Vector4((int)StyleDebug.view, 0f, 0f, 0f);

            return globals;
        }

        internal static void Mix(ref StyleFrameGlobals current, in StyleFrameGlobals other, float t,
                                 HBStyleProfileSections sections)
        {
            var half = t >= 0.5f;

            if ((sections & HBStyleProfileSections.Brush) != 0)
            {
                current._HB_Brush = L(current._HB_Brush, other._HB_Brush, t);
                current._HB_BrushVariation = L(current._HB_BrushVariation, other._HB_BrushVariation, t);
                current._HB_ShadowBrush = L(current._HB_ShadowBrush, other._HB_ShadowBrush, t);
            }

            if ((sections & HBStyleProfileSections.Sky) != 0)
            {
                var environment = L(current._HB_Environment, other._HB_Environment, t);
                var floor = L(current._HB_AmbientFloor, other._HB_AmbientFloor, t);

                current._HB_Environment = new Vector4(current._HB_Environment.x, environment.y, environment.z,
                                                      environment.w);
                current._HB_AmbientFloor = new Vector4(floor.x, floor.y, floor.z, current._HB_AmbientFloor.w);
                current._HB_Sky = L(current._HB_Sky, other._HB_Sky, t);
                current._HB_SkyBrush = L(current._HB_SkyBrush, other._HB_SkyBrush, t);
                current._HB_CloudLight = L(current._HB_CloudLight, other._HB_CloudLight, t);
                current._HB_CloudAwayLight = L(current._HB_CloudAwayLight, other._HB_CloudAwayLight, t);
            }

            if ((sections & HBStyleProfileSections.Celestial) != 0)
            {
                var count = half ? other._HB_AuroraLow.w : current._HB_AuroraLow.w;

                current._HB_SunColor = L(current._HB_SunColor, other._HB_SunColor, t);
                current._HB_SunDisc = L(current._HB_SunDisc, other._HB_SunDisc, t);
                current._HB_SunGlow = L(current._HB_SunGlow, other._HB_SunGlow, t);
                current._HB_MoonColor = L(current._HB_MoonColor, other._HB_MoonColor, t);
                current._HB_MoonDisc = L(current._HB_MoonDisc, other._HB_MoonDisc, t);
                current._HB_MoonGlow = L(current._HB_MoonGlow, other._HB_MoonGlow, t);
                current._HB_Stars = L(current._HB_Stars, other._HB_Stars, t);
                current._HB_StarLook = L(current._HB_StarLook, other._HB_StarLook, t);
                current._HB_Aurora = L(current._HB_Aurora, other._HB_Aurora, t);
                current._HB_AuroraLow = L(current._HB_AuroraLow, other._HB_AuroraLow, t);
                current._HB_AuroraMid = L(current._HB_AuroraMid, other._HB_AuroraMid, t);
                current._HB_AuroraHigh = L(current._HB_AuroraHigh, other._HB_AuroraHigh, t);
                current._HB_AuroraLow.w = count;
            }

            if ((sections & HBStyleProfileSections.Clouds) != 0)
            {
                var steps = half ? other._HB_CloudParams.w : current._HB_CloudParams.w;

                current._HB_CloudParams = L(current._HB_CloudParams, other._HB_CloudParams, t);
                current._HB_CloudParams.w = steps;
                current._HB_CloudMotion = L(current._HB_CloudMotion, other._HB_CloudMotion, t);
                current._HB_CloudSlab = L(current._HB_CloudSlab, other._HB_CloudSlab, t);
                current._HB_CloudTint = L(current._HB_CloudTint, other._HB_CloudTint, t);
                current._HB_CloudLook = L(current._HB_CloudLook, other._HB_CloudLook, t);
            }

            if ((sections & HBStyleProfileSections.Weather) != 0)
            {
                var look = L(current._HB_WeatherLook, other._HB_WeatherLook, t);

                current._HB_WeatherLook = new Vector4(look.x, look.y, look.z, current._HB_WeatherLook.w);
                current._HB_Precipitation = L(current._HB_Precipitation, other._HB_Precipitation, t);
            }

            if ((sections & HBStyleProfileSections.Atmosphere) != 0)
            {
                var layers = half ? other._HB_Horizon.w : current._HB_Horizon.w;

                current._HB_Fog = L(current._HB_Fog, other._HB_Fog, t);
                current._HB_FogScatter = L(current._HB_FogScatter, other._HB_FogScatter, t);
                current._HB_Horizon = L(current._HB_Horizon, other._HB_Horizon, t);
                current._HB_Horizon.w = layers;
            }
        }

        static Vector4 L(Vector4 a, Vector4 b, float t) => Vector4.LerpUnclamped(a, b, t);

        static void PackWeather(HBStyleWeatherSettings weather, HBStyleWeatherQuality quality,
                                ref StyleFrameGlobals globals)
        {
            StyleWeatherState.Advance();

            var falling = StyleEnvironment.weather;
            var heading = StyleEnvironment.windHeading * Mathf.Deg2Rad;
            var rainColor = StyleKeyLight.Linear(weather.rainColor);

            globals._HB_Weather = new Vector4(falling.x, falling.y, falling.z, heading);
            globals._HB_WeatherSurface = new Vector4(StyleWeatherState.wetness, StyleWeatherState.snow,
                                                     StyleWeatherState.rain, (float)StyleWeatherState.fallClock);
            globals._HB_WeatherLook = new Vector4(rainColor.r, rainColor.g, rainColor.b,
                                                  quality.current.detailDistance);
            globals._HB_Precipitation = new Vector4(weather.rainOpacity, weather.rainWidth, weather.snowSize,
                                                    weather.snowOpacity);
        }

        static void PackBrush(HBStyleBrushSettings brush, HBStyleShadowQuality shadows, int cascades,
                              ref StyleFrameGlobals globals)
        {
            var level = shadows.current;
            var tile = StyleShadowSettings.AtlasTile(cascades);

            globals._HB_Brush = brush.Pack();
            globals._HB_BrushVariation = brush.PackVariation();
            globals._HB_ShadowFilter = new Vector4(level.softness, shadows.contact, Mathf.Clamp(level.taps, 1, 16),
                                                   HBStyleShadowQuality.ContactReach);
            globals._HB_ShadowBrush = new Vector4(brush.atlas != null ? shadows.edgeBrush : 0f,
                                                  1f / Mathf.Max(0.05f, shadows.edgeBrushSize), tile.x, tile.y);
        }

        static void PackCelestial(HBStyleEnvironmentProfile profile, float elevation, float blend, float afterglow,
                                  ref StyleFrameGlobals globals)
        {
            var sky = profile.sky;
            var celestial = profile.celestial;
            var sunTime = Mathf.Clamp01(blend * 2f);

            var sun = StyleKeyLight.Linear(StyleKeyLight.OverElevation(StyleKeyLight.SunDisc(sky), elevation));
            var power = Mathf.Lerp(celestial.sunIntensity, celestial.sunDuskIntensity, sunTime);

            globals._HB_SunColor = new Vector4(sun.r, sun.g, sun.b, power);
            globals._HB_SunDisc = PackDisc(Mathf.Lerp(celestial.sunSize, celestial.sunDuskSize, sunTime),
                                           celestial.sunBrush);
            globals._HB_SunGlow = PackGlow(celestial.sunGlow, celestial.sunRays, afterglow);

            var moon = StyleKeyLight.Linear(StyleKeyLight.OverElevation(StyleKeyLight.MoonDisc(sky), -elevation));
            var moonFade = StyleKeyLight.Smooth01(0.5f - elevation / (2f * StyleKeyLight.Transition));

            globals._HB_MoonColor = new Vector4(moon.r, moon.g, moon.b, celestial.moonIntensity * moonFade);
            globals._HB_MoonDisc = PackDisc(celestial.moonSize, 0.2f);
            globals._HB_MoonGlow = PackGlow(celestial.moonGlow, 0f, 1f);

            var amount = celestial.stars;
            var fade = 1f - Mathf.Clamp01(Mathf.InverseLerp(-0.2f, 0.15f, elevation));

            globals._HB_Stars = new Vector4(80f, amount * fade, celestial.starTwinkle,
                                            Mathf.Lerp(0.97f, 0.88f, amount));

            var heading = celestial.auroraHeading * Mathf.Deg2Rad;

            globals._HB_StarLook = new Vector4(celestial.starColor, celestial.starSizeVariation, Mathf.Sin(heading),
                                               Mathf.Cos(heading));

            var aurora = celestial.auroraColor ?? (s_DefaultAurora ??= HBStyleCelestialSettings.DefaultAuroraColor());

            globals._HB_Aurora = new Vector4(celestial.aurora * fade, celestial.auroraScale, celestial.auroraSpeed,
                                             celestial.auroraHeight);
            globals._HB_AuroraLow = StyleKeyLight.Linear(aurora.Evaluate(0f));
            globals._HB_AuroraMid = StyleKeyLight.Linear(aurora.Evaluate(0.5f));
            globals._HB_AuroraHigh = StyleKeyLight.Linear(aurora.Evaluate(1f));
            globals._HB_AuroraLow.w = Mathf.Clamp(celestial.auroraCount, 1, 4);
        }

        static void PackClouds(HBStyleEnvironmentProfile profile, Vector3 sunDirection, float elevation, float spread,
                               ref StyleFrameGlobals globals)
        {
            var clouds = profile.clouds;

            if (clouds.atlas == null)
                return;

            var sky = profile.sky;
            var lifted = elevation + CloudElevationLift;
            var reach = sky.skyLightIntensity * StyleKeyLight.Handoff(lifted);
            var gradient = StyleKeyLight.SkyLight(sky);

            var light = StyleKeyLight.Linear(StyleKeyLight.OverElevation(gradient, lifted)) * reach;
            var away = StyleKeyLight.Linear(StyleKeyLight.OverElevation(gradient, lifted - spread * 0.35f)) * reach;
            var direction = lifted >= 0f ? sunDirection : -sunDirection;
            var wind = clouds.wind * 0.004f;
            var tint = StyleKeyLight.Linear(clouds.tint);

            globals._HB_CloudParams = new Vector4(StyleKeyLight.Coverage(profile), clouds.scale * 0.03f,
                                                  clouds.softness * 0.5f, clouds.steps);
            globals._HB_CloudMotion = new Vector4(wind.x, wind.y, 0f, 0f);
            globals._HB_CloudLook = new Vector4(clouds.silverLining, clouds.sunThrough, clouds.powder, 0f);
            globals._HB_CloudLight = new Vector4(light.r, light.g, light.b, 0f);
            globals._HB_CloudAwayLight = new Vector4(away.r, away.g, away.b, 0f);
            globals._HB_CloudDirection = new Vector4(direction.x, direction.y, direction.z, 0f);
            globals._HB_CloudSlab = new Vector4(clouds.stepSpacing * 0.12f, clouds.stepTaper * 0.5f,
                                                clouds.stepShading, 0f);
            globals._HB_CloudTint = new Vector4(tint.r, tint.g, tint.b, clouds.tintShade);
        }

        static void PackFog(HBStyleAtmosphereSettings atmosphere, HBStyleAtmosphereQuality quality,
                            float sunVisibility, ref StyleFrameGlobals globals)
        {
            if (quality.level == HBStyleQualityLevel.Off)
                return;

            var level = quality.current;
            var fogStart = Mathf.Max(atmosphere.fogStart, 0f);
            var horizonStart = Mathf.Max(atmosphere.horizonStart, 0f);

            globals._HB_Fog = new Vector4(Density(fogStart, atmosphere.fogEnd), atmosphere.fogHeightFalloff,
                                          atmosphere.baseHeight, atmosphere.fogMaxOpacity);
            globals._HB_FogScatter = new Vector4(level.sunInscatter ? atmosphere.sunScattering * sunVisibility : 0f,
                                                 fogStart, atmosphere.layerSoftness, 0f);
            globals._HB_Horizon = new Vector4(Density(horizonStart, atmosphere.horizonEnd),
                                              atmosphere.horizonHeightFalloff, horizonStart,
                                              level.horizonLayers ? atmosphere.horizonLayers : 0f);
        }

        static float Density(float start, float end)
        {
            var range = Mathf.Max(end - start, 0f);

            return end > 0f && range > 0.01f ? 3f / range : 0f;
        }

        static Vector4 PackDisc(float sizeDegrees, float brush)
        {
            var radians = sizeDegrees * Mathf.Deg2Rad;

            return new Vector4(Mathf.Cos(radians), Mathf.Sin(radians * 0.25f), brush, 0f);
        }

        static Vector4 PackGlow(float glow, float rays, float fade)
        {
            return new Vector4(Mathf.Lerp(2000f, 40f, glow), glow * fade, rays, 0f);
        }

        static bool ProbeLightingBaked()
        {
            var volumes = ProbeReferenceVolume.instance;

            if (volumes != null && volumes.isInitialized)
                return volumes.DataHasBeenLoaded();

            var probes = LightmapSettings.lightProbes;

            return probes != null && probes.count > 0;
        }
    }
}
