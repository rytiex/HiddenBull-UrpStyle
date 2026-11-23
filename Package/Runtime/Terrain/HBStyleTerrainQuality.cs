using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleTerrainQualityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    [Serializable]
    public struct HBStyleTerrainLevel
    {
        [Tooltip("Distance from the camera, in metres, of the first detail ring. The terrain is drawn at the full " +
                 "heightmap resolution out to three quarters of it and each farther ring is twice as long with " +
                 "half the density. Below 96 m very steep cliffs can show hairline cracks between rings.")]
        [Min(96f)]
        public float detailRange;

        [Tooltip("Distance from the camera, in metres, out to which the ground's layers are drawn in full. Past it " +
                 "the layers' normal maps and the weakest of the three blended layers fade out over the last " +
                 "quarter, which makes the far ground cheaper to shade while its colours stay close to the full look.")]
        [Min(MinLayerDetail)]
        public float layerDetail;

        internal const float MinLayerDetail = 8f;

        public HBStyleTerrainLevel(float detailRange, float layerDetail)
        {
            this.detailRange = detailRange;
            this.layerDetail = layerDetail;
        }
    }

    [Serializable]
    public sealed class HBStyleTerrainQuality : ISerializationCallbackReceiver
    {
        const float LowDetail = 30f;
        const float MediumDetail = 50f;
        const float HighDetail = 80f;
        const float UltraDetail = 120f;

        [SerializeField]
        [Tooltip("Active terrain level: how fast the triangle density drops with distance and how far the ground's " +
                 "layers are drawn in full. The ground near the camera is always drawn at full resolution, so it " +
                 "matches the collider on every level.")]
        HBStyleTerrainQualityLevel m_Level = HBStyleTerrainQualityLevel.High;

        [SerializeField]
        [Tooltip("Settings used when the level is Low.")]
        HBStyleTerrainLevel m_Low = new HBStyleTerrainLevel(96f, LowDetail);

        [SerializeField]
        [Tooltip("Settings used when the level is Medium.")]
        HBStyleTerrainLevel m_Medium = new HBStyleTerrainLevel(112f, MediumDetail);

        [SerializeField]
        [Tooltip("Settings used when the level is High.")]
        HBStyleTerrainLevel m_High = new HBStyleTerrainLevel(128f, HighDetail);

        [SerializeField]
        [Tooltip("Settings used when the level is Ultra.")]
        HBStyleTerrainLevel m_Ultra = new HBStyleTerrainLevel(160f, UltraDetail);

        public HBStyleTerrainQualityLevel level
        {
            get => HBStyleRenderSettings.isSet ? HBStyleRenderSettings.current.terrain : m_Level;
            set => m_Level = value;
        }

        internal HBStyleTerrainQualityLevel authoredLevel => m_Level;

        public HBStyleTerrainLevel Get(HBStyleTerrainQualityLevel level)
        {
            return level switch
            {
                HBStyleTerrainQualityLevel.Low => m_Low,
                HBStyleTerrainQualityLevel.Medium => m_Medium,
                HBStyleTerrainQualityLevel.Ultra => m_Ultra,
                _ => m_High
            };
        }

        public HBStyleTerrainLevel current => Get(level);

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Fill(ref m_Low, LowDetail);
            Fill(ref m_Medium, MediumDetail);
            Fill(ref m_High, HighDetail);
            Fill(ref m_Ultra, UltraDetail);
        }

        static void Fill(ref HBStyleTerrainLevel level, float detail)
        {
            if (level.layerDetail < HBStyleTerrainLevel.MinLayerDetail)
                level.layerDetail = detail;
        }
    }
}
