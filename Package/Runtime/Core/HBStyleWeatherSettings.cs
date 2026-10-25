using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleWeatherSettings
    {
        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Tints the falling rain on top of the colour it takes from the sky and the sun; white leaves " +
                 "that colour as it is.")]
        Color m_RainColor = new Color(0.0745f, 0.0745f, 0.0745f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the falling rain streaks show (0 to 1).")]
        float m_RainOpacity = 0.55f;

        [SerializeField]
        [Range(0.001f, 0.01f)]
        [Tooltip("Width of a falling rain streak in metres. Far streaks never get thinner than about 1.5 pixels; " +
                 "they fade instead.")]
        float m_RainWidth = 0.003f;

        [SerializeField]
        [Range(0.005f, 0.05f)]
        [Tooltip("Average size of a falling snow flake in metres; each flake varies around it.")]
        float m_SnowSize = 0.015f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the falling snow flakes show (0 to 1).")]
        float m_SnowOpacity = 0.95f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far rain and snow close the sky (0 to 1): at 1 steady rain brings the cloud cover to " +
                 "overcast, which also dims the sunlight. Lighter rain closes it less. Has no effect without a " +
                 "cloud atlas in the Clouds section.")]
        float m_CloudResponse = 1f;

        [SerializeField]
        [Range(0f, 1.5f)]
        [Tooltip("How far, in metres, a roof keeps rain and snow off past its own edge. It closes the thin " +
                 "strip just inside walls and under eaves where the shelter map is too coarse to see the wall.")]
        float m_RoofMargin = 0.025f;

        [SerializeField]
        [Range(0.1f, 10f)]
        [Tooltip("Scales every time below at once. Raise it to see a change sooner; lower it for a slow, long " +
                 "day. Outside Play mode surfaces always show the settled state straight away, so the look can " +
                 "be judged while editing.")]
        float m_Speed = 1f;

        [SerializeField]
        [Tooltip("Skips the build-up in Play mode too: surfaces take the wetness, snow and shelter the weather " +
                 "calls for straight away. Use it for scenes that start already wet or snowed in. Weather Bodies " +
                 "still wet and dry gradually as they leave or enter cover.")]
        bool m_Instant;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds of full rain until open surfaces are soaked through; lighter rain takes longer. Rain " +
                 "first spots the ground with drops that grow until they join; slopes that water runs down stay " +
                 "wet while it rains.")]
        float m_WetTime = 30f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds after the rain stops until surfaces are dry again. Melting snow keeps them wet while " +
                 "it lasts.")]
        float m_DryTime = 15f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds of snowfall until the snow lies at the depth Snow asks for. It settles in patches, on " +
                 "tops before slopes and in the middle of a ledge before its edges.")]
        float m_SettleTime = 15f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds after the snow stops, or is turned down, until it has melted back.")]
        float m_MeltTime = 30f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds until an object with an HB Style Weather Body that leaves cover is as wet and snowy as " +
                 "its surroundings. Leaving the weather again it dries and melts over Dry Time and Melt Time.")]
        float m_UncoverTime = 5f;

        public Color rainColor => m_RainColor;

        public float rainOpacity => m_RainOpacity;

        public float rainWidth => m_RainWidth;

        public float snowSize => m_SnowSize;

        public float snowOpacity => m_SnowOpacity;

        public float cloudResponse => m_CloudResponse;

        public float roofMargin => m_RoofMargin;

        public float speed => m_Speed;

        public bool instant => m_Instant;

        public float wetTime => m_WetTime;

        public float dryTime => m_DryTime;

        public float settleTime => m_SettleTime;

        public float meltTime => m_MeltTime;

        public float uncoverTime => m_UncoverTime;
    }
}
