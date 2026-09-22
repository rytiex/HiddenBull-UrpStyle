using UnityEngine;

namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// Central settings asset for the HiddenBull URP visual style.
    /// Extend this class with the real style parameters you need:
    /// e.g. color palette, outline thickness, stylized shadow settings,
    /// grain/dither intensity, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "StyleSettings", menuName = "HiddenBull/URP Style/Style Settings")]
    public class StyleSettings : ScriptableObject
    {
        [Header("Placeholder")]
        [Tooltip("Example field — replace or remove with real style parameters.")]
        public Color accentColor = Color.white;
    }
}
