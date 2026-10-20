using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public sealed class HBStyleTerrainLayerArrays : ScriptableObject
    {
        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Albedo;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Normal;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Mask;

        [SerializeField]
        [HideInInspector]
        TerrainLayer[] m_Layers = new TerrainLayer[0];

        [SerializeField]
        [HideInInspector]
        string m_Signature;

        public Texture2DArray albedo => m_Albedo;
        public Texture2DArray normal => m_Normal;
        public Texture2DArray mask => m_Mask;
        public TerrainLayer[] layers => m_Layers;
        public string signature => m_Signature;

        public bool valid => m_Albedo != null && m_Normal != null && m_Mask != null && m_Layers.Length > 0;

        public void Set(Texture2DArray albedo, Texture2DArray normal, Texture2DArray mask, TerrainLayer[] layers,
                        string signature)
        {
            m_Albedo = albedo;
            m_Normal = normal;
            m_Mask = mask;
            m_Layers = layers;
            m_Signature = signature;
        }
    }
}
