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
    }
}
