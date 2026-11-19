using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleFogState
    {
        [SerializeField]
        [Tooltip("Distance from the camera, in metres, before which the fog does not start.")]
        [Min(0f)]
        float m_Start;

        [SerializeField]
        [Tooltip("Distance, in metres, by which the fog has nearly reached Max Opacity. 0 turns the fog off in " +
                 "this weather.")]
        [Min(0f)]
        float m_End;

        [SerializeField]
        [Tooltip("World height, in metres, of the top of the fog layer. Below it the fog has the same density " +
                 "everywhere; above it the fog fades out over Fade Height. Seen from above, the layer lies on the " +
                 "ground like a sea of fog.")]
        float m_LayerTop = 10f;

        [SerializeField]
        [Tooltip("Height, in metres, over which the fog fades out above Layer Top. 0 gives the same fog at every " +
                 "height, with no layer.")]
        [Min(0f)]
        float m_FadeHeight = 20f;

        [SerializeField]
        [Tooltip("How thick the layer looks from above, like a sea of fog seen from a mountain (0 to 4). Ground " +
                 "inside the layer fades out the deeper it sits and the farther it is from the camera: at 1 it is " +
                 "nearly hidden at End distance, at 2 at half of it. It comes in as the camera rises a Fade Height " +
                 "above Layer Top and has no effect from inside the fog; hills that rise out of the layer stay clear.")]
        [Range(0f, 4f)]
        float m_LayerCover = 1f;

        [SerializeField]
        [Tooltip("Thin haze in the air above the layer (0 to 1): how far the sky just above the horizon fades " +
                 "into the fog, so the horizon stays soft when you look out over the layer. At 1 the lowest few " +
                 "degrees of sky are fully hazed, at 0.5 about half; higher up the sky stays nearly clear. It also " +
                 "hazes far ground seen from high up. 0 leaves the air above the layer clear.")]
        [Range(0f, 1f)]
        float m_AirHaze = 0.2f;

        [SerializeField]
        [Tooltip("Ceiling on how much the fog can take over (0 to 1).")]
        [Range(0f, 1f)]
        float m_MaxOpacity = 1f;

        [SerializeField]
        [Tooltip("Fog colour by day, read along the view direction: left is looking straight down, the middle " +
                 "is the horizon, right is straight up.")]
        Gradient m_DayFog;

        [SerializeField]
        [Tooltip("Fog colour while the sun is near the horizon, read the same way as Day Fog.")]
        Gradient m_DuskFog;

        [SerializeField]
        [Tooltip("Fog colour after dark, read the same way as Day Fog.")]
        Gradient m_NightFog;

        [SerializeField]
        [Tooltip("How strongly the fog takes on the sun's colour when looking toward it (0 to 1). It fades as " +
                 "the sun sets and as the clouds close in.")]
        [Range(0f, 1f)]
        float m_SunScattering = 1f;

        [SerializeField]
        [Tooltip("How much the fog gathers into thick patches with clearer gaps between them (0 to 1). The " +
                 "patches drift slowly with the wind heading and faster as the wind rises; they are strongest " +
                 "near the camera and blend into even fog with distance. 0 leaves the fog even.")]
        [Range(0f, 1f)]
        float m_Clumps;

        [SerializeField]
        [Tooltip("Width of one fog patch, in metres. Patches are about a third as tall as they are wide.")]
        [Min(5f)]
        float m_ClumpSize = 40f;

        public float start => m_Start;
        public float end => m_End;
        public float layerTop => m_LayerTop;
        public float fadeHeight => m_FadeHeight;
        public float layerCover => m_LayerCover;
        public float airHaze => m_AirHaze;
        public float maxOpacity => m_MaxOpacity;
        public Gradient dayFog => m_DayFog;
        public Gradient duskFog => m_DuskFog;
        public Gradient nightFog => m_NightFog;
        public float sunScattering => m_SunScattering;
        public float clumps => m_Clumps;
        public float clumpSize => m_ClumpSize;

        internal static HBStyleFogState Clear()
        {
            return new HBStyleFogState
            {
                m_Start = 200f,
                m_End = 1000f,
                m_LayerTop = 5f,
                m_SunScattering = 1f,
                m_Clumps = 0.6f,
                m_ClumpSize = 16f,
                m_DayFog = HBStyleSkySettings.DefaultDayAmbient(),
                m_DuskFog = HBStyleSkySettings.DefaultDuskAmbient(),
                m_NightFog = DefaultNightFog()
            };
        }

        internal static HBStyleFogState Rain()
        {
            var tint = new Color(0.82f, 0.85f, 0.9f);

            return new HBStyleFogState
            {
                m_Start = 30f,
                m_End = 400f,
                m_SunScattering = 0.4f,
                m_Clumps = 0.4f,
                m_ClumpSize = 24f,
                m_LayerCover = 2f,
                m_AirHaze = 0.5f,
                m_DayFog = Shade(HBStyleSkySettings.DefaultDayAmbient(), tint, 0.6f),
                m_DuskFog = Shade(HBStyleSkySettings.DefaultDuskAmbient(), tint, 0.6f),
                m_NightFog = Shade(DefaultNightFog(), tint, 0.6f)
            };
        }

        internal static HBStyleFogState Snow()
        {
            var tint = new Color(0.94f, 0.97f, 1f);

            return new HBStyleFogState
            {
                m_Start = 15f,
                m_End = 200f,
                m_SunScattering = 0.25f,
                m_Clumps = 0.5f,
                m_ClumpSize = 20f,
                m_LayerCover = 3f,
                m_AirHaze = 0.6f,
                m_DayFog = Shade(HBStyleSkySettings.DefaultDayAmbient(), tint, 0.7f),
                m_DuskFog = Shade(HBStyleSkySettings.DefaultDuskAmbient(), tint, 0.7f),
                m_NightFog = Shade(DefaultNightFog(), tint, 0.7f)
            };
        }

        public static Gradient DefaultNightFog()
        {
            var gradient = new Gradient();

            gradient.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.0000000f, 0.0000000f, 0.0000000f), 0.0676f),
                new GradientColorKey(new Color(0.0788092f, 0.0788092f, 0.1037736f), 0.1882f),
                new GradientColorKey(new Color(0.1513884f, 0.1513884f, 0.1698113f), 0.4206f),
                new GradientColorKey(new Color(0.1668298f, 0.1748540f, 0.2169811f), 0.6676f),
                new GradientColorKey(new Color(0.1392399f, 0.1457814f, 0.1981132f), 0.8088f),
                new GradientColorKey(new Color(0.0943396f, 0.0943396f, 0.0943396f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }

        static Gradient Shade(Gradient source, Color tint, float grey)
        {
            var keys = source.colorKeys;

            for (var i = 0; i < keys.Length; i++)
            {
                var colour = keys[i].color;
                var lightness = colour.grayscale;

                keys[i].color = Color.Lerp(colour, new Color(lightness, lightness, lightness), grey) * tint;
            }

            source.SetKeys(keys, source.alphaKeys);

            return source;
        }
    }
}
