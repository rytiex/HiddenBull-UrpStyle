using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleTerrainProfileLayer
    {
        public const float Unlimited = 100000f;

        [Tooltip("The Terrain Layer placed by this row: its textures, mask map and tiling.")]
        public TerrainLayer layer;

        [Tooltip("Placed automatically by the slope and height rules below. Off leaves the layer for painting " +
                 "only.")]
        public bool automatic = true;

        [Tooltip("Also scatters this layer in patches over another layer's area, such as mud inside grass, " +
                 "on top of where its own rules place it. With Automatic off, or Opacity 0, only the patches " +
                 "place it.")]
        public TerrainLayer variantOf;

        [Range(0f, 1f)]
        [Tooltip("Roughly how much of the other layer's area the patches take over.")]
        public float variantAmount = 0.3f;

        [Min(0.5f)]
        [Tooltip("Size, in metres, of the patches.")]
        public float variantSize = 6f;

        [Tooltip("Steepness range, in degrees, the layer covers. 0 is flat ground, 90 a vertical cliff.")]
        public Vector2 slope = new Vector2(0f, 90f);

        [Min(0f)]
        [Tooltip("How many degrees past each end of the slope range the layer fades out over.")]
        public float slopeSoftness = 6f;

        [Tooltip("World height range, in metres, the layer covers.")]
        public Vector2 height = new Vector2(-Unlimited, Unlimited);

        [Min(0f)]
        [Tooltip("How many metres past each end of the height range the layer fades out over.")]
        public float heightSoftness = 2f;

        [Range(0f, 1f)]
        [Tooltip("Breaks the borders up with noise so they wander instead of following the slope and height " +
                 "exactly. Scales with the softness.")]
        public float breakup = 0.5f;

        [Min(0.5f)]
        [Tooltip("Size, in metres, of the bumps the breakup puts into the borders.")]
        public float breakupSize = 8f;

        [Range(0f, 1f)]
        [Tooltip("How fully the layer covers what lies under it where its rules match.")]
        public float opacity = 1f;

        [ColorUsage(false)]
        [Tooltip("Multiplies the layer's colour.")]
        public Color tint = Color.white;

        [Tooltip("Hue of the large variation patches: they take this colour's tint, not its brightness. Alpha is " +
                 "how strongly they show; 0 turns variation off.")]
        public Color variation = new Color(0.8f, 0.9f, 0.7f, 0f);

        [Min(1f)]
        [Tooltip("Size, in metres, of the variation patches.")]
        public float variationSize = 30f;

        [Tooltip("X: how much the patches are warped. Y: fine detail inside them. Z: edge contrast. W: extra " +
                 "light and dark shading.")]
        public Vector4 variationShape = new Vector4(0.9f, 0.35f, 4f, 0.35f);

        [Range(0f, 1f)]
        [Tooltip("0 tints the layer with the variation colour, 1 paints the colour over it.")]
        public float variationCover;

        [Range(0f, 1f)]
        [Tooltip("How dark and glossy the layer gets when it rains.")]
        public float wetness = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How deep snow settles on the layer. Low values leave it patchy.")]
        public float snowCover = 1f;

        [Range(0f, 1f)]
        [Tooltip("How much of this layer is covered in grass. 0 grows none. Needs a Clump Texture on the terrain " +
                 "material.")]
        public float grass;

        [Range(0.05f, 2f)]
        [Tooltip("Height of the tallest clumps, in metres. Each clump is a little shorter at random.")]
        public float grassHeight = 0.5f;

        [Range(0.05f, 3f)]
        [Tooltip("Width of one clump, in metres. It does not change with Height.")]
        public float grassWidth = 0.7f;

        [Tooltip("Off, the top of the clump is the ground colour a little lighter. On, it takes Tip Color.")]
        public bool ownTip;

        [ColorUsage(false)]
        [Tooltip("Tint at the top of the clump, mixed with the layer's texture the same way its Tint is.")]
        public Color tipColor = new Color(0.55f, 0.75f, 0.25f, 1f);

        [Range(0f, 1f)]
        [Tooltip("How much of this layer is in flower. Needs a Flower Texture on the terrain material.")]
        public float flowers;

        [Range(0.03f, 1f)]
        [Tooltip("Size of the biggest flowers, in metres.")]
        public float flowerSize = 0.15f;
    }

    public sealed class HBStyleTerrainProfile : ScriptableObject
    {
        public const int MaxLayers = 32;

        [Tooltip("Terrain layers from bottom to top. The first one covers the whole terrain; every other " +
                 "automatic layer is laid over the ones above it in the list where its rules match.")]
        public List<HBStyleTerrainProfileLayer> layers = new List<HBStyleTerrainProfileLayer>();

#if UNITY_EDITOR
        internal static event Action<HBStyleTerrainProfile> edited;

        void OnValidate()
        {
            edited?.Invoke(this);
        }
#endif
    }
}
