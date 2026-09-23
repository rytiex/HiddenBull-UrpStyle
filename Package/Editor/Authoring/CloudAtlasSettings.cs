using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    public class CloudAtlasSettings
    {
        [Range(2, 16)]
        [Tooltip("How many puffs fit across the tile. This is the size of the billows themselves, " +
                 "and the single setting that decides whether the sky reads as cumulus or as haze.")]
        public int puffScale = 5;

        [Range(0f, 1f)]
        [Tooltip("Blends between drifting noise and round billows. Low values give flat stratus, " +
                 "high values the stacked cauliflower shapes of a cumulus.")]
        public float puffiness = 0.75f;

        [Range(1, 12)]
        [Tooltip("Scale of the drift that decides where clouds gather and where the sky stays open. " +
                 "Kept whole so the field still tiles.")]
        public int shapeScale = 3;

        [Range(1, 6)]
        [Tooltip("How many levels of detail go into that drift.")]
        public int octaves = 4;

        [Range(0f, 1f)]
        [Tooltip("How far fine noise eats into the cloud. This is what turns a smooth blob into " +
                 "something with torn edges and holes.")]
        public float erosion = 0.35f;

        [Range(2, 48)]
        [Tooltip("Scale of that erosion noise. Kept whole so the field still tiles.")]
        public int erosionScale = 12;

        [Range(0.25f, 4f)]
        [Tooltip("Contrast on the finished density. Higher separates cloud from sky more sharply " +
                 "and leaves less wispy transition.")]
        public float contrast = 1.4f;

        [Range(0f, 1f)]
        [Tooltip("Strength of the relief baked into RG. This is what the sun lights, so it decides " +
                 "how much a cloud reads as a solid form rather than a flat patch.")]
        public float reliefStrength = 0.6f;

        [Range(0f, 1f)]
        [Tooltip("How much the relief is smoothed before the normals are taken. Low values follow " +
                 "every speck of noise and light up as sparkle; higher values give broad billows " +
                 "with a clear lit and shadowed side.")]
        public float reliefSmoothing = 0.35f;

        [Tooltip("Output texture size. 512 is usually enough — the atlas tiles, and the sky samples " +
                 "it at two scales, so detail comes from repetition rather than resolution.")]
        public int resolution = 512;

        [Tooltip("Random seed. The same seed always produces the same atlas.")]
        public int seed = 1;
    }
}
