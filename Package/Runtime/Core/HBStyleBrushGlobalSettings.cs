using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public class HBStyleBrushGlobalSettings
    {
        [SerializeField]
        [Tooltip("Brush atlas. Generate one on the Brush page of Tools > HiddenBull > URP Style > " +
                 "Texture Studio. Leaving this empty disables the brush on every material.")]
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

        [SerializeField]
        [Tooltip("How far the atlas is broken up so it never reads as a repeating tile. Surfaces are " +
                 "split into irregular patches, each taking the strokes from a different part of the " +
                 "atlas, and where two patches meet the fuller stroke is painted over the other, the " +
                 "way overlapping brush marks do — no seam line. The stroke direction turns slowly " +
                 "across the surface, so neighbouring patches lean the same way and a wall reads as " +
                 "one flowing layer of paint rather than a patchwork.\n\n" +
                 "Low values keep the direction close to the atlas's own; 1 lets it swing up to a " +
                 "right angle over several metres. 0 switches it off at no cost. Above 0 it costs a " +
                 "second atlas read only in the narrow band where patches meet.")]
        [Range(0f, 1f)]
        float m_Randomize = 1f;

        [SerializeField]
        [Tooltip("Paints the distance with broader strokes instead of fading them out, the way a " +
                 "painter loosens up toward the background. From Fade Start the strokes double in " +
                 "size at each doubling of distance, up to eight times, so they keep roughly the same " +
                 "size on screen; the fade then happens eight times further out.\n\n" +
                 "Each size is fixed in the world, so nothing slides as you move — the change of size " +
                 "sweeps through a narrow ring instead. Costs a second atlas read inside those rings.")]
        bool m_GrowWithDistance = true;

        const float MaxGrowth = 8f;

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
            var reach = m_GrowWithDistance ? MaxGrowth : 1f;
            var fadeStart = m_FadeStart * reach;
            var fadeEnd = Mathf.Max(m_FadeEnd * reach, fadeStart + 0.01f);

            return new Vector4(
                1f / Mathf.Max(0.01f, m_WorldSizePerTile),
                fadeStart,
                1f / (fadeEnd - fadeStart),
                m_Atlas != null ? 1f : 0f);
        }

        public Vector4 PackVariation()
        {
            return new Vector4(
                Mathf.Clamp01(m_Randomize),
                m_GrowWithDistance ? 1f : 0f,
                Mathf.Max(m_FadeStart, 0.01f),
                0f);
        }
    }
}
