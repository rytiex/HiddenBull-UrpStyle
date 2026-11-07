using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Terrain Layer", order = 1,
                     menuName = "HiddenBull/URP Style/Terrain/Terrain Layer")]
    public sealed class HBStyleTerrainLayer : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Colour texture of the ground. Without one the layer is a flat grey tinted by Tint.")]
        Texture2D m_Albedo;

        [SerializeField]
        [Tooltip("Normal map of the ground.")]
        Texture2D m_Normal;

        [SerializeField]
        [Tooltip("Mask map like Unity's terrain layers: R metallic, G occlusion, B height, A smoothness. The " +
                 "height decides which layer shows through where two layers meet.")]
        Texture2D m_Mask;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Multiplies the colour texture.")]
        Color m_Tint = Color.white;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Size of one texture repeat on the ground, in metres.")]
        Vector2 m_Size = Vector2.one;

        [SerializeField]
        [Tooltip("Shift of the texture repeat, in metres.")]
        Vector2 m_Offset;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("Strength of the normal map.")]
        float m_NormalStrength = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Smoothness of the ground. With a mask map it multiplies the mask's smoothness.")]
        float m_Smoothness;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Metallic of the ground. With a mask map it multiplies the mask's metallic.")]
        float m_Metallic;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How wet this ground gets in rain: low for sand that drinks the water, high for rock and mud " +
                 "that darken and shine.")]
        float m_Wetness = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of that wet shows as shine: 1 for rock and mud under a film of water, 0 for ground " +
                 "that only darkens.")]
        float m_WetGloss = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much snow this ground holds. Snow fills the low parts of the texture first.")]
        float m_SnowCover = 1f;

        [SerializeField]
        [Tooltip("Large colour patches that break up this layer. Grass on it takes them too.")]
        bool m_Variation;

        [SerializeField]
        [Tooltip("Colour of the patches. Alpha is how strongly they show.")]
        Color m_VariationColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [SerializeField]
        [Min(1f)]
        [Tooltip("Size, in metres, of the variation patches.")]
        float m_VariationSize = 10f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How much the patch edges are warped.")]
        float m_VariationWarp = 0.9f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fine detail inside the patches.")]
        float m_VariationDetail = 0.35f;

        [SerializeField]
        [Range(1f, 8f)]
        [Tooltip("How sharp the patch edges are.")]
        float m_VariationContrast = 4f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Extra light and dark shading across the patches.")]
        float m_VariationShading;

        [SerializeField]
        [Tooltip("Off tints the layer with the colour's hue. On paints the colour over the layer.")]
        bool m_VariationPaint;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How thick grass grows on this layer: 0 for none, 1 for a tuft at every spot the quality level " +
                 "allows (about every 10 cm at Ultra). The Grass brush thins or thickens it place by place.")]
        float m_GrassDensity;

        [SerializeField]
        [Range(0.05f, 1f)]
        [Tooltip("Height of the grass on this layer, in metres. The Grass brush makes it shorter or taller place " +
                 "by place.")]
        float m_GrassHeight = 0.095f;

        [SerializeField]
        [Range(0.05f, 1f)]
        [Tooltip("Width of one grass leaf near the camera. 1 is 20 cm.")]
        float m_GrassWidth = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far each grass leaf bends over: 0 stands straight, 1 droops back toward the ground.")]
        float m_GrassCurl = 0.125f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Breaks the grass into patches of taller, lighter and shorter, darker tufts. 0 keeps one height " +
                 "and tone.")]
        float m_GrassVariation = 0.5f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour the upper part of each leaf turns toward, by Tip Strength. The leaf itself takes the " +
                 "colour of the ground under it.")]
        Color m_GrassTip = Color.white;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far the tips take the Tip colour: 0 for off, so the grass is the ground's colour all the " +
                 "way up, 1 for full Tip colour at the very top.")]
        float m_GrassTipStrength;

        [SerializeField]
        [Range(0f, 10f)]
        [Tooltip("How far from a layer without grass, in metres, this grass starts to get shorter and sparser, " +
                 "until none is left at the border. 0 ends it sharply.")]
        float m_GrassTransition = 0.1f;

        [SerializeField]
        [Tooltip("Picture of the flower heads, cut out by alpha. It may hold several flowers side by side; set how " +
                 "many with Flower Variants. Flowers grow on this layer only when it is set.")]
        Texture2D m_FlowerTexture;

        [SerializeField]
        [Range(1, 8)]
        [Tooltip("How many flowers sit side by side in the Flower Texture.")]
        int m_FlowerVariants = 1;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of this layer is in flower. Flowers grow in patches, and each patch keeps mostly to one " +
                 "kind.")]
        float m_FlowerShare = 0.3f;

        [SerializeField]
        [Range(0.02f, 1f)]
        [Tooltip("Size of the biggest flower heads, in metres.")]
        float m_FlowerSize = 0.15f;

        [SerializeField]
        [Tooltip("Shape of one grass plant, cut out by alpha. Only the shape is used: the colour comes from the " +
                 "ground and the Tip colour, as for the leaves. Cards grow on this layer only when it is set.")]
        Texture2D m_GrassCardTexture;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of this layer's grass is drawn as cards instead of leaves: 0 for leaves only, 1 for " +
                 "cards only. Cards grow in patches. A card is as tall as the grass there.")]
        float m_GrassCardShare = 0.2f;

        [SerializeField]
        [Range(0.02f, 2f)]
        [Tooltip("Width of a card, in metres. It stays the same however tall the grass grows; the card's height " +
                 "follows the grass Height and the Card Texture stretches to fit.")]
        float m_GrassCardSpan = 0.3f;

        public Texture2D albedo
        {
            get => m_Albedo;
            set => m_Albedo = value;
        }

        public Texture2D normal
        {
            get => m_Normal;
            set => m_Normal = value;
        }

        public Texture2D mask
        {
            get => m_Mask;
            set => m_Mask = value;
        }

        public Color tint
        {
            get => m_Tint;
            set => m_Tint = value;
        }

        public Vector2 size
        {
            get => m_Size;
            set => m_Size = Vector2.Max(value, Vector2.one * 0.01f);
        }

        public Vector2 offset
        {
            get => m_Offset;
            set => m_Offset = value;
        }

        public float normalStrength
        {
            get => m_NormalStrength;
            set => m_NormalStrength = value;
        }

        public float smoothness
        {
            get => m_Smoothness;
            set => m_Smoothness = value;
        }

        public float metallic
        {
            get => m_Metallic;
            set => m_Metallic = value;
        }

        public float wetness
        {
            get => m_Wetness;
            set => m_Wetness = Mathf.Clamp01(value);
        }

        public float wetGloss
        {
            get => m_WetGloss;
            set => m_WetGloss = Mathf.Clamp01(value);
        }

        public float snowCover
        {
            get => m_SnowCover;
            set => m_SnowCover = Mathf.Clamp01(value);
        }

        public bool variation
        {
            get => m_Variation;
            set => m_Variation = value;
        }

        public Color variationColor
        {
            get => m_VariationColor;
            set => m_VariationColor = value;
        }

        public float variationSize
        {
            get => m_VariationSize;
            set => m_VariationSize = Mathf.Max(value, 1f);
        }

        public float variationWarp
        {
            get => m_VariationWarp;
            set => m_VariationWarp = Mathf.Clamp(value, 0f, 2f);
        }

        public float variationDetail
        {
            get => m_VariationDetail;
            set => m_VariationDetail = Mathf.Clamp01(value);
        }

        public float variationContrast
        {
            get => m_VariationContrast;
            set => m_VariationContrast = Mathf.Clamp(value, 1f, 8f);
        }

        public float variationShading
        {
            get => m_VariationShading;
            set => m_VariationShading = Mathf.Clamp01(value);
        }

        internal Vector4 variationShape =>
            new Vector4(m_VariationWarp, m_VariationDetail, m_VariationContrast, m_VariationShading);

        public bool variationPaint
        {
            get => m_VariationPaint;
            set => m_VariationPaint = value;
        }

        public float grassDensity
        {
            get => m_GrassDensity;
            set => m_GrassDensity = Mathf.Clamp01(value);
        }

        public float grassHeight
        {
            get => m_GrassHeight;
            set => m_GrassHeight = Mathf.Clamp(value, 0.05f, 1f);
        }

        public float grassWidth
        {
            get => m_GrassWidth;
            set => m_GrassWidth = Mathf.Clamp(value, 0.05f, 1f);
        }

        public float grassCurl
        {
            get => m_GrassCurl;
            set => m_GrassCurl = Mathf.Clamp01(value);
        }

        public float grassVariation
        {
            get => m_GrassVariation;
            set => m_GrassVariation = Mathf.Clamp01(value);
        }

        public Color grassTip
        {
            get => m_GrassTip;
            set => m_GrassTip = value;
        }

        public float grassTipStrength
        {
            get => m_GrassTipStrength;
            set => m_GrassTipStrength = Mathf.Clamp01(value);
        }

        public float grassTransition
        {
            get => m_GrassTransition;
            set => m_GrassTransition = Mathf.Clamp(value, 0f, 10f);
        }

        public Texture2D flowerTexture
        {
            get => m_FlowerTexture;
            set => m_FlowerTexture = value;
        }

        public int flowerVariants
        {
            get => m_FlowerVariants;
            set => m_FlowerVariants = Mathf.Clamp(value, 1, 8);
        }

        public float flowerShare
        {
            get => m_FlowerShare;
            set => m_FlowerShare = Mathf.Clamp01(value);
        }

        public float flowerSize
        {
            get => m_FlowerSize;
            set => m_FlowerSize = Mathf.Clamp(value, 0.02f, 1f);
        }

        public Texture2D grassCardTexture
        {
            get => m_GrassCardTexture;
            set => m_GrassCardTexture = value;
        }

        public float grassCardShare
        {
            get => m_GrassCardShare;
            set => m_GrassCardShare = Mathf.Clamp01(value);
        }

        public float grassCardWidth
        {
            get => m_GrassCardSpan;
            set => m_GrassCardSpan = Mathf.Clamp(value, 0.02f, 2f);
        }
    }
}
