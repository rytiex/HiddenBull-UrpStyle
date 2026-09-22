using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public class BrushGlobalSettings
    {
        [SerializeField]
        [Tooltip("Brush atlas. Generate one with Tools > HiddenBull > URP Style > Brush Atlas " +
                 "Generator. Leaving this empty disables the brush on every material.")]
        Texture2D m_Atlas;

        [SerializeField]
        [Tooltip("World size of one atlas tile, in metres. This is the single scale the whole " +
                 "project shares — changing it rescales every brushed surface at once, which is " +
                 "the point.")]
        [Min(0.01f)]
        float m_WorldSizePerTile = 1f;

        [SerializeField]
        [Tooltip("Distance at which strokes begin fading toward flat.")]
        [Min(0f)]
        float m_FadeStart = 15f;

        [SerializeField]
        [Tooltip("Distance at which strokes have faded out completely. Strokes are a fixed world " +
                 "size, so beyond some distance they fall below a pixel and alias into noise no " +
                 "matter how good the mipmaps are.")]
        [Min(0.01f)]
        float m_FadeEnd = 30f;

        public Texture2D atlas
        {
            get => m_Atlas;
            set => m_Atlas = value;
        }

        public float worldSizePerTile
        {
            get => m_WorldSizePerTile;
            set => m_WorldSizePerTile = Mathf.Max(0.01f, value);
        }

        public Vector4 Pack()
        {
            var fadeEnd = Mathf.Max(m_FadeEnd, m_FadeStart + 0.01f);

            return new Vector4(
                1f / Mathf.Max(0.01f, m_WorldSizePerTile),
                m_FadeStart,
                1f / (fadeEnd - m_FadeStart),
                m_Atlas != null ? 1f : 0f);
        }
    }
}
