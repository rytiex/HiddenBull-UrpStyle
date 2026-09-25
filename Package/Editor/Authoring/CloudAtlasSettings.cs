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
        public int puffScale = 2;

        [Range(0f, 1f)]
        [Tooltip("Blends between drifting noise and round billows. Low values give flat stratus, " +
                 "high values the stacked cauliflower shapes of a cumulus.")]
        public float puffiness = 1f;

        [Range(1, 12)]
        [Tooltip("Scale of the drift that decides where clouds gather and where the sky stays open. " +
                 "Kept whole so the field still tiles.")]
        public int shapeScale = 1;

        [Range(1, 6)]
        [Tooltip("How many levels of detail go into that drift.")]
        public int octaves = 2;

        [Range(0f, 1f)]
        [Tooltip("How far fine noise eats into the cloud. This is what turns a smooth blob into " +
                 "something with torn edges and holes.")]
        public float erosion = 1f;

        [Range(2, 48)]
        [Tooltip("Scale of that erosion noise. Kept whole so the field still tiles.")]
        public int erosionScale = 12;

        [Range(0.25f, 4f)]
        [Tooltip("Contrast on the finished density. Higher separates cloud from sky more sharply " +
                 "and leaves less wispy transition.")]
        public float contrast = 1.86f;

        [Range(0f, 1f)]
        [Tooltip("Strength of the relief baked into RG. This is what the sun lights, so it decides " +
                 "how much a cloud reads as a solid form rather than a flat patch.")]
        public float reliefStrength = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How much the relief is smoothed before the normals are taken. Low values follow " +
                 "every speck of noise and light up as sparkle; higher values give broad billows " +
                 "with a clear lit and shadowed side.")]
        public float reliefSmoothing = 0.645f;

        [Range(0f, 1f)]
        [Tooltip("Pushes the whole density field around with a slow drift before the shapes are " +
                 "read out of it. The puffs come from a regular grid of cells, and a grid is what " +
                 "the eye finds when a sky looks like it repeats — bending the coordinates first " +
                 "is what stops the cells lining up.\n\n" +
                 "It costs nothing at runtime: the bend is baked into the atlas.")]
        public float warp = 0.5f;

        [Range(1, 8)]
        [Tooltip("How quickly the drift changes across the tile. Low values move whole regions " +
                 "together and keep the clouds intact; high values churn the shapes up.")]
        public int warpScale = 2;

        [Range(0f, 1f)]
        [Tooltip("How far a brush atlas cuts into the cloud before it is baked. It works on the " +
                 "density field itself, so the strokes end up in the silhouette and in how solid " +
                 "the cloud is — they are part of the shape rather than a pattern laid over it.\n\n" +
                 "Baking it in rather than sampling the brush at runtime means the cloud march " +
                 "stays one texture read per step, whatever this is set to.")]
        public float brushAmount = 0.55f;

        [Range(0.25f, 8f)]
        [Tooltip("How many times the brush atlas repeats across the cloud tile. Low values give " +
                 "broad sweeps through the cloud body, high values a finer tooth on the edge.")]
        public float brushScale = 0.95f;

        [Tooltip("Output texture size. 512 is usually enough — the atlas tiles, and the sky samples " +
                 "it at two scales, so detail comes from repetition rather than resolution.")]
        public int resolution = 512;

        [Tooltip("Random seed. The same seed always produces the same atlas.")]
        public int seed = 1;
    }
}
