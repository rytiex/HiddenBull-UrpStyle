using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Terrain Layer Set", order = 2,
                     menuName = "HiddenBull/URP Style/Terrain/Terrain Layer Set")]
    public sealed class HBStyleTerrainLayerSet : ScriptableObject
    {
        public const int MaxLayers = 16;

        [SerializeField]
        [Tooltip("Ground layers from bottom to top: the first covers the whole terrain, each later one covers " +
                 "the ones before it where its rules match or where it is painted. Up to 16.")]
        List<HBStyleTerrainLayerRule> m_Layers = new List<HBStyleTerrainLayerRule>();

        public IReadOnlyList<HBStyleTerrainLayerRule> layers => m_Layers;

        internal int count => Mathf.Min(m_Layers.Count, MaxLayers);

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_Layers.Count > MaxLayers)
                m_Layers.RemoveRange(MaxLayers, m_Layers.Count - MaxLayers);
        }
#endif
    }
}
