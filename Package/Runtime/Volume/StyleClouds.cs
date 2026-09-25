using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    [VolumeComponentMenu("HiddenBull/Style Clouds")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StyleClouds : VolumeComponent
    {
        [Tooltip("How much of the sky the clouds take up. 0 is a clear sky. Needs a cloud atlas on " +
                 "the renderer feature.")]
        public ClampedFloatParameter coverage = new ClampedFloatParameter(0.695f, 0f, 1f);

        [Tooltip("Tint over the finished cloud. The colour itself comes from the Sky Light gradient " +
                 "and the ambient gradient, so clouds keep following the time of day — this only " +
                 "pushes them a little warmer or cooler than the sky would make them on its own.")]
        public ColorParameter tint = new ColorParameter(Color.white, true, false, true);

        [Tooltip("Size of the cloud forms. Lower is bigger and closer. Pushing this up packs more " +
                 "tiles into the sky, and the atlas starts to repeat visibly near the horizon " +
                 "where the dome compresses hardest.")]
        public ClampedFloatParameter scale = new ClampedFloatParameter(0.725f, 0.25f, 2f);

        [Tooltip("Direction and speed the bank drifts, as a velocity.")]
        public Vector2Parameter wind = new Vector2Parameter(new Vector2(1.25f, 0.2f));

        [Tooltip("How softly the cloud edge dissolves into the sky. It also sets how wide the light " +
                 "bleeding through that edge reads.")]
        public ClampedFloatParameter softness = new ClampedFloatParameter(0.135f, 0.01f, 1f);

        [Header("Volume")]
        [Tooltip("How many shells the cloud is built from. Each one is a slice at a slightly lower " +
                 "altitude, so every step adds another layer of cloud underneath and the bank gets " +
                 "deeper rather than just smoother. Light is absorbed on the way down, which is " +
                 "what gives the flat dark base and the bright top. This is the one setting that " +
                 "costs per pixel: it is a texture fetch each.")]
        public ClampedIntParameter steps = new ClampedIntParameter(4, 1, 8);

        [Tooltip("How far apart the shells sit. Low keeps them almost on top of each other and the " +
                 "bank reads as a sheet; high pulls them apart into a deep body, but past a point " +
                 "they stop overlapping and start reading as separate stacked copies.")]
        public ClampedFloatParameter stepSpacing = new ClampedFloatParameter(0.215f, 0f, 1f);

        [Tooltip("How much shading the cloud takes at all: light absorbed on the way down through " +
                 "the shells, and the shaping the sun does across each one. It is what produces " +
                 "the flat dark base under a bright top. 0 turns both off and leaves the bank " +
                 "evenly lit.")]
        public ClampedFloatParameter stepShading = new ClampedFloatParameter(0.265f, 0f, 1f);

        [Tooltip("How much the shells shrink away from the middle one. Each shell is cut at a " +
                 "higher density as it gets further from the centre, so the stack closes into a " +
                 "rounded body instead of standing like a pile of identical cut-outs.")]
        public ClampedFloatParameter stepTaper = new ClampedFloatParameter(0.325f, 0f, 1f);

        [Header("Detail")]
        [Tooltip("How much light bleeds through where the cloud thins out. It sits in the band " +
                 "where the edge dissolves into sky, not across the body, and it only lifts the " +
                 "shadowed side. Near the sun it also lights the body of whichever cloud the sun " +
                 "is behind.")]
        public ClampedFloatParameter translucency = new ClampedFloatParameter(1f, 0f, 1f);
    }
}
