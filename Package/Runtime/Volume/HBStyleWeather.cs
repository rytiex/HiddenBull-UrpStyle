using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/HB Style Weather")]
    [DisplayInfo(name = "HB Style Weather")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class HBStyleWeather : VolumeComponent
    {
        [Tooltip("How hard it rains. Drops fall around the camera out to about forty metres, so it " +
                 "can be seen through a window from deep inside a room, and stop at anything with " +
                 "something above it — a roof, a tree, an overhang — so it does not rain indoors. " +
                 "0 turns rain off at no cost.")]
        public ClampedFloatParameter rain = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Tints the falling rain on top of the colour it takes from the sky and the sun; white leaves " +
                 "that colour as it is.")]
        public ColorParameter rainColor = new ColorParameter(new Color(0.0745f, 0.0745f, 0.0745f), false, false, true);

        [Tooltip("How hard it snows. Flakes drift down around the camera and, like rain, stop under " +
                 "anything with something above it. Rain and snow are set on their own, so sleet is " +
                 "both at once. 0 turns snow off at no cost.")]
        public ClampedFloatParameter snow = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("How hard the wind blows. It slants the rain, pushes the snow sideways and sways the " +
                 "grass. Above a light breeze white streaks ride the wind through the air and low over " +
                 "the ground, more and faster the stronger it blows, and never under a roof. Snow is light and " +
                 "leans much further than rain at the same wind. Liquids whose material has Wind Affects " +
                 "Waves turned on take their wave height from it, swelling over the Cover Time and calming " +
                 "over the Uncover Time.")]
        public ClampedFloatParameter wind = new ClampedFloatParameter(0.2f, 0f, 1f);

        [Tooltip("The compass direction the wind blows toward, in degrees: 0 is world +Z, 90 is " +
                 "world +X.")]
        public ClampedFloatParameter windHeading = new ClampedFloatParameter(0f, 0f, 360f);

        [Tooltip("Scales every time below at once. Raise it to see a change sooner; lower it for a " +
                 "slow, long day. Outside Play mode surfaces always show the settled state straight " +
                 "away, so the look can be judged while editing.")]
        public ClampedFloatParameter speed = new ClampedFloatParameter(1f, 0.1f, 10f);

        [Tooltip("Skips the build-up in Play mode too: surfaces take the wetness, snow and shelter " +
                 "that the weather calls for straight away, whatever the times say. Use it for scenes " +
                 "that start already wet or snowed in, or to cut straight to a new weather.")]
        public BoolParameter instant = new BoolParameter(false);

        [Tooltip("Seconds of full rain until open surfaces are soaked through; lighter rain takes " +
                 "longer. Rain first spots the ground with drops that grow until they join; drying moves " +
                 "in patches, and slopes that water runs down stay wet while it rains and never dry before " +
                 "the flat ground around them.")]
        public MinFloatParameter wetTime = new MinFloatParameter(30f, 0.1f);

        [Tooltip("Seconds after the rain stops until surfaces are dry again. Melting snow keeps " +
                 "them wet while it lasts.")]
        public MinFloatParameter dryTime = new MinFloatParameter(15f, 0.1f);

        [Tooltip("Seconds of snowfall until the snow lies at the depth Snow asks for. It settles in " +
                 "patches, on tops before slopes and in the middle of a ledge before its edges.")]
        public MinFloatParameter settleTime = new MinFloatParameter(15f, 0.1f);

        [Tooltip("Seconds after the snow stops, or is turned down, until it has melted back.")]
        public MinFloatParameter meltTime = new MinFloatParameter(30f, 0.1f);

        [Tooltip("Seconds until something that comes to rest over a spot — a parked cart, a crate " +
                 "set down, a roof the player has just built — has cleared the snow and dried the " +
                 "ground beneath it. Anything that only passes over leaves no mark.")]
        public MinFloatParameter coverTime = new MinFloatParameter(2f, 0.1f);

        [Tooltip("Seconds until a spot that has been uncovered is snowed on and wetted like its " +
                 "surroundings again.")]
        public MinFloatParameter uncoverTime = new MinFloatParameter(5f, 0.1f);

        public bool isActive => rain.value > 0f || snow.value > 0f;

        public const float StreakWind = 0.25f;

        public float streaks => Mathf.InverseLerp(StreakWind, 1f, wind.value);

        public bool isVisible => isActive || wind.value > StreakWind;
    }
}
