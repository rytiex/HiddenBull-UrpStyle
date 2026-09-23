using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/Style Celestial")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StyleCelestial : VolumeComponent
    {
        [Header("Sun")]
        [Tooltip("Brightness of the disc. Its colour is the Disc Color gradient on Style Sky, and " +
                 "the light it casts is Sky Light. Set this to zero for a sky with no sun in it.")]
        public ClampedFloatParameter sunIntensity = new ClampedFloatParameter(0.85f, 0f, 20f);

        [Tooltip("The brightness it reaches by dusk.")]
        public ClampedFloatParameter sunDuskIntensity = new ClampedFloatParameter(2f, 0f, 20f);

        [Tooltip("Angular radius of the disc, in degrees.")]
        public ClampedFloatParameter sunSize = new ClampedFloatParameter(0.1f, 0.1f, 30f);

        [Tooltip("The size it reaches by dusk. A low sun reading larger than a high one is most of " +
                 "what makes a sunset feel like one.")]
        public ClampedFloatParameter sunDuskSize = new ClampedFloatParameter(0.65f, 0.1f, 30f);

        [Tooltip("Size and strength of the glow around the disc, on one curve. The glow outlives " +
                 "the disc below the horizon, which is what leaves an afterglow at sunset.")]
        public ClampedFloatParameter sunGlow = new ClampedFloatParameter(0.75f, 0f, 1f);

        [Tooltip("How far the brush atlas breaks up the disc edge, so the sun reads as painted " +
                 "rather than as a circle. Needs a brush atlas on the renderer feature.")]
        public ClampedFloatParameter sunBrush = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Rays radiating from the sun. They are cut at the horizon with the disc rather " +
                 "than bleeding below it like the glow. 0 leaves the glow smooth.")]
        public ClampedFloatParameter sunRays = new ClampedFloatParameter(0.35f, 0f, 1f);

        [Header("Moon")]
        [Tooltip("Brightness of the moon. It sits exactly opposite the sun, so it rises as the sun " +
                 "sets without anything to place or switch on, and it takes its colour from the " +
                 "left half of the Disc Color gradient. Set this to zero for a moonless sky.")]
        public ClampedFloatParameter moonIntensity = new ClampedFloatParameter(2.5f, 0f, 20f);

        [Tooltip("Angular radius of the moon, in degrees.")]
        public ClampedFloatParameter moonSize = new ClampedFloatParameter(0.1f, 0.1f, 30f);

        [Tooltip("Size and strength of the glow around the moon.")]
        public ClampedFloatParameter moonGlow = new ClampedFloatParameter(0.1f, 0f, 1f);

        [Header("Stars")]
        [Tooltip("How many stars there are and how brightly they burn, on one curve. They fade out " +
                 "on their own as the sun rises.")]
        public ClampedFloatParameter stars = new ClampedFloatParameter(0.75f, 0f, 1f);

        [Tooltip("How much stars vary in brightness over time.")]
        public ClampedFloatParameter starTwinkle = new ClampedFloatParameter(0.4f, 0f, 1f);
    }
}
