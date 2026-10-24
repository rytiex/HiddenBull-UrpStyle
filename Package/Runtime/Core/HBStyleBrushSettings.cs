using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleBrushSettings
    {
        const float MaxGrowth = 8f;

        [SerializeField]
        [Tooltip("Brush atlas read by every HB Style shader. Leaving it empty turns the brush off on every " +
                 "material.")]
        Texture2D m_Atlas;

        [SerializeField]
        [Tooltip("World size of one atlas tile, in metres. This is the one scale the whole project shares: " +
                 "changing it rescales every brushed surface at once.")]
        [Min(0.01f)]
        float m_WorldSizePerTile = 1f;

        [SerializeField]
        [Tooltip("Distance from the camera, in metres, at which strokes begin to fade toward flat.")]
        [Min(0f)]
        float m_FadeStart = 15f;

        [SerializeField]
        [Tooltip("Distance from the camera, in metres, at which strokes have faded out completely. Strokes " +
                 "are a fixed world size, so beyond some distance they fall below a pixel and alias.")]
        [Min(0.01f)]
        float m_FadeEnd = 30f;

        [SerializeField]
        [Tooltip("How far the atlas is broken up so it never reads as a repeating tile (0 to 1). Surfaces " +
                 "are split into irregular patches with slowly turning stroke direction; where two patches " +
                 "meet the fuller stroke is painted over the other. 0 switches it off at no cost.")]
        [Range(0f, 1f)]
        float m_Randomize = 1f;

        [SerializeField]
        [Tooltip("Paints the distance with broader strokes instead of fading them out. From Fade Start the " +
                 "strokes double in size at each doubling of distance, up to eight times, and the fade " +
                 "happens eight times further out. Costs a second atlas read inside the size-change rings.")]
        bool m_GrowWithDistance = true;

        public Texture2D atlas => m_Atlas;

        public float worldSizePerTile => m_WorldSizePerTile;

        public float fadeStart => m_FadeStart;

        public float fadeEnd => m_FadeEnd;

        public float randomize => m_Randomize;

        public bool growWithDistance => m_GrowWithDistance;

        internal Vector4 Pack()
        {
            var reach = m_GrowWithDistance ? MaxGrowth : 1f;
            var start = m_FadeStart * reach;
            var end = Mathf.Max(m_FadeEnd * reach, start + 0.01f);

            return new Vector4(1f / Mathf.Max(0.01f, m_WorldSizePerTile), start, 1f / (end - start),
                               m_Atlas != null ? 1f : 0f);
        }

        internal Vector4 PackVariation()
        {
            return new Vector4(Mathf.Clamp01(m_Randomize), m_GrowWithDistance ? 1f : 0f,
                               Mathf.Max(m_FadeStart, 0.01f), 0f);
        }
    }
}
