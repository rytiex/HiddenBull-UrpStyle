using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleAtmosphereSettings
    {
        [SerializeField]
        [Tooltip("Distance from the camera, in metres, before which the fog does not start.")]
        [Min(0f)]
        [FormerlySerializedAs("m_StartDistance")]
        float m_FogStart;

        [SerializeField]
        [Tooltip("Distance, in metres, by which the fog has nearly reached Fog Max Opacity. 0 turns the fog off.")]
        [Min(0f)]
        [FormerlySerializedAs("m_EndDistance")]
        float m_FogEnd;

        [SerializeField]
        [Tooltip("How quickly fog density drops with height (0 to 1). 0 gives the same fog at every height; " +
                 "higher values keep it in the valleys.")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("m_HeightFalloff")]
        float m_FogHeightFalloff;

        [SerializeField]
        [Tooltip("World height, in metres, at which the fog and the horizon haze have their full density.")]
        float m_BaseHeight;

        [SerializeField]
        [Tooltip("Ceiling on how much the fog can take over (0 to 1).")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("m_MaxOpacity")]
        float m_FogMaxOpacity = 1f;

        [SerializeField]
        [Tooltip("Fog colour by day, read along the view direction: left is looking straight down, the middle " +
                 "is the horizon, right is straight up.")]
        Gradient m_DayFog = HBStyleSkySettings.DefaultDayAmbient();

        [SerializeField]
        [Tooltip("Fog colour while the sun is near the horizon, read the same way as Day Fog.")]
        Gradient m_DuskFog = HBStyleSkySettings.DefaultDuskAmbient();

        [SerializeField]
        [Tooltip("Fog colour after dark, read the same way as Day Fog.")]
        Gradient m_NightFog = DefaultNightFog();

        [SerializeField]
        [Tooltip("How strongly the fog takes on the sun's colour when looking toward it (0 to 1). It fades as " +
                 "the sun sets and as the clouds close in.")]
        [Range(0f, 1f)]
        float m_SunScattering = 1f;

        [SerializeField]
        [Tooltip("How far the fog colour is broken up by brush strokes, using the sky's Sky Paint and Sky Brush " +
                 "amounts (0 to 1). Over surfaces the strokes sit on the ground behind the fog and grow with " +
                 "distance, so they stay put as you walk or turn; where the fog swallows the scene they hand over " +
                 "to the sky's own strokes, so the horizon meets the sky without a seam. 0 leaves the fog smooth.")]
        [Range(0f, 1f)]
        float m_FogPaint = 1f;

        [SerializeField]
        [Tooltip("How far outlines dissolve into the fog behind them (0 to 2). Each stroke takes its fog from its " +
                 "centre, so where a stroke straddles an edge the farther, foggier side is carried across it and " +
                 "the outline breaks up into brush marks instead of a clean cut. It only ever adds fog.")]
        [Range(0f, 2f)]
        float m_Silhouette = 1f;

        [SerializeField]
        [Tooltip("Distance from the camera, in metres, where the horizon haze starts. Distant surfaces fade " +
                 "toward the sky colour behind them, so the horizon meets the sky without a seam.")]
        [Min(0f)]
        float m_HorizonStart = 200f;

        [SerializeField]
        [Tooltip("Distance, in metres, by which distant surfaces have nearly become the sky colour. 0 turns the " +
                 "horizon haze off.")]
        [Min(0f)]
        float m_HorizonEnd;

        [SerializeField]
        [Tooltip("How quickly the horizon haze thins with height (0 to 1), so peaks stay clearer than valleys.")]
        [Range(0f, 1f)]
        float m_HorizonHeightFalloff = 0.01f;

        [SerializeField]
        [Tooltip("Number of steps the horizon haze recedes in when Horizon Layers are on in the Quality " +
                 "Profile (2 to 8). Ridges at different distances snap to these steps.")]
        [Range(2, 8)]
        int m_HorizonLayers = 4;

        [SerializeField]
        [Tooltip("How soft the edge between two horizon layers is (0 hard to 1 smooth).")]
        [Range(0f, 1f)]
        float m_LayerSoftness = 0.35f;

        public float fogStart => m_FogStart;
        public float fogEnd => m_FogEnd;
        public float fogHeightFalloff => m_FogHeightFalloff;
        public float baseHeight => m_BaseHeight;
        public float fogMaxOpacity => m_FogMaxOpacity;
        public Gradient dayFog => m_DayFog;
        public Gradient duskFog => m_DuskFog;
        public Gradient nightFog => m_NightFog;
        public float sunScattering => m_SunScattering;
        public float fogPaint => m_FogPaint;
        public float silhouette => m_Silhouette;
        public float horizonStart => m_HorizonStart;
        public float horizonEnd => m_HorizonEnd;
        public float horizonHeightFalloff => m_HorizonHeightFalloff;
        public int horizonLayers => m_HorizonLayers;
        public float layerSoftness => m_LayerSoftness;

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
    }
}
