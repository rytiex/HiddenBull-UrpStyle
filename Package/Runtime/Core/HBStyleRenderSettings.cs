using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleRenderSettings : IEquatable<HBStyleRenderSettings>
    {
        [Tooltip("Main-light shadow level.")]
        public HBStyleQualityLevel shadows;

        [Tooltip("Atmosphere level. Off turns the fog off.")]
        public HBStyleQualityLevel atmosphere;

        [Tooltip("Weather level: shelter map size and how far the small wet and snow details reach.")]
        public HBStyleWeatherQualityLevel weather;

        [Tooltip("How much falling rain and snow is drawn. Off draws none.")]
        public HBStyleQualityLevel precipitation;

        [Tooltip("How fast the HB Style Terrain's triangle density drops with distance.")]
        public HBStyleTerrainQualityLevel terrain;

        [Tooltip("How far grass reaches and how thick it can grow. Off draws no grass.")]
        public HBStyleGrassQualityLevel grass;

        [Tooltip("Anti aliasing level. Off turns it off.")]
        public HBStyleQualityLevel antiAliasing;

        [Tooltip("Refraction and the shadows glass casts.")]
        public HBStyleTransparencyQualityLevel transparency;

        [Tooltip("Screen space reflection level for liquid surfaces.")]
        public HBStyleQualityLevel reflections;

        [Tooltip("Bloom level. Off turns the glow off.")]
        public HBStyleQualityLevel bloom;

        [Tooltip("Ambient occlusion level. Off turns the soft corner shading off.")]
        public HBStyleQualityLevel ambientOcclusion;

        [Tooltip("Lens flare from the sun and small bright lights. It also needs Bloom above Off.")]
        public bool lensFlare;

        static HBStyleRenderSettings s_Current;
        static bool s_Active;

        public static event Action changed;

        public static bool isSet => s_Active;

        internal static ref readonly HBStyleRenderSettings current => ref s_Current;

        internal static bool lensFlareOn => !s_Active || s_Current.lensFlare;

        public static HBStyleRenderSettings authored => From(HBStyleFeature.authoredQuality);

        public static HBStyleRenderSettings Get()
        {
            return s_Active ? s_Current : authored;
        }

        public static void Set(HBStyleRenderSettings settings)
        {
            if (s_Active && s_Current.Equals(settings))
                return;

            s_Current = settings;
            s_Active = true;
            Changed();
        }

        public static void Restore()
        {
            if (!s_Active)
                return;

            s_Active = false;
            Changed();
        }

        public static HBStyleRenderSettings From(HBStyleQualityProfile profile)
        {
            if (profile == null)
            {
                return new HBStyleRenderSettings
                {
                    shadows = new HBStyleShadowQuality().authoredLevel,
                    atmosphere = new HBStyleAtmosphereQuality().authoredLevel,
                    weather = new HBStyleWeatherQuality().authoredLevel,
                    precipitation = new HBStylePrecipitationQuality().authoredLevel,
                    terrain = new HBStyleTerrainQuality().authoredLevel,
                    grass = new HBStyleGrassQuality().authoredLevel,
                    antiAliasing = new HBStyleAntiAliasingQuality().authoredLevel,
                    transparency = new HBStyleTransparencyQuality().authoredLevel,
                    reflections = new HBStyleReflectionQuality().authoredLevel,
                    bloom = new HBStyleBloomQuality().authoredLevel,
                    ambientOcclusion = new HBStyleAmbientOcclusionQuality().authoredLevel,
                    lensFlare = true
                };
            }

            return new HBStyleRenderSettings
            {
                shadows = profile.shadows.authoredLevel,
                atmosphere = profile.atmosphere.authoredLevel,
                weather = profile.weather.authoredLevel,
                precipitation = profile.precipitation.authoredLevel,
                terrain = profile.terrain.authoredLevel,
                grass = profile.grass.authoredLevel,
                antiAliasing = profile.antiAliasing.authoredLevel,
                transparency = profile.transparency.authoredLevel,
                reflections = profile.reflections.authoredLevel,
                bloom = profile.bloom.authoredLevel,
                ambientOcclusion = profile.ambientOcclusion.authoredLevel,
                lensFlare = true
            };
        }

        public bool Equals(HBStyleRenderSettings other)
        {
            return shadows == other.shadows && atmosphere == other.atmosphere && weather == other.weather &&
                   precipitation == other.precipitation && terrain == other.terrain && grass == other.grass &&
                   antiAliasing == other.antiAliasing && transparency == other.transparency &&
                   reflections == other.reflections && bloom == other.bloom &&
                   ambientOcclusion == other.ambientOcclusion && lensFlare == other.lensFlare;
        }

        public override bool Equals(object obj)
        {
            return obj is HBStyleRenderSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(shadows, atmosphere, weather, precipitation, terrain);

            return HashCode.Combine(hash, grass, antiAliasing, transparency, reflections, bloom, ambientOcclusion,
                                lensFlare);
        }

        public static bool operator ==(HBStyleRenderSettings left, HBStyleRenderSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HBStyleRenderSettings left, HBStyleRenderSettings right)
        {
            return !left.Equals(right);
        }

        static void Changed()
        {
            changed?.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Active = false;
            changed = null;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void WatchPlayMode()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= RestoreOnEditMode;
            UnityEditor.EditorApplication.playModeStateChanged += RestoreOnEditMode;
        }

        static void RestoreOnEditMode(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
                Restore();
        }
#endif
    }
}
