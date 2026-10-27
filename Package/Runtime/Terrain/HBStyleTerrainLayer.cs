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
        Vector2 m_Size = new Vector2(4f, 4f);

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
        float m_Smoothness = 0.2f;

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
        [Tooltip("How much snow this ground holds. Snow fills the low parts of the texture first.")]
        float m_SnowCover = 1f;

        [Header("Grass")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How thick grass grows on this layer: 0 for none, 1 for a tuft at every spot the quality level " +
                 "allows (about every 20 cm at Ultra). The Grass brush thins or thickens it place by place.")]
        float m_GrassDensity;

        [SerializeField]
        [Range(0.05f, 3f)]
        [Tooltip("Height of the grass on this layer, in metres. The Grass brush makes it shorter or taller place " +
                 "by place.")]
        float m_GrassHeight = 0.4f;

        [SerializeField]
        [Range(0.05f, 2f)]
        [Tooltip("Width of one tuft of grass, in metres.")]
        float m_GrassWidth = 0.35f;

        [SerializeField]
        [ColorUsage(false)]
        [Tooltip("Colour of the grass tips. The roots take the colour of the ground under them.")]
        Color m_GrassTip = new Color(0.75f, 0.85f, 0.35f);

        [SerializeField]
        [Tooltip("Picture of one tuft, cut out by its alpha. Leave empty for the package's own tuft.")]
        Texture2D m_GrassTexture;

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

        public float snowCover
        {
            get => m_SnowCover;
            set => m_SnowCover = Mathf.Clamp01(value);
        }

        public float grassDensity
        {
            get => m_GrassDensity;
            set => m_GrassDensity = Mathf.Clamp01(value);
        }

        public float grassHeight
        {
            get => m_GrassHeight;
            set => m_GrassHeight = Mathf.Clamp(value, 0.05f, 3f);
        }

        public float grassWidth
        {
            get => m_GrassWidth;
            set => m_GrassWidth = Mathf.Clamp(value, 0.05f, 2f);
        }

        public Color grassTip
        {
            get => m_GrassTip;
            set => m_GrassTip = value;
        }

        public Texture2D grassTexture
        {
            get => m_GrassTexture;
            set => m_GrassTexture = value;
        }
    }
}
