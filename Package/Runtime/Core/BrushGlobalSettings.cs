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

        [SerializeField]
        [Tooltip("How far the finished image is dragged around by the brush beyond Distortion " +
                 "Start. A stroke painted on a surface falls below a pixel at distance and mipmaps " +
                 "flatten it away, so nothing survives out there. This warps the rendered image " +
                 "instead, breaking distant silhouettes into strokes the way a painter would. " +
                 "0 skips the pass entirely.\n\n" +
                 "Costs one fullscreen pass and a depth texture, and it leaves the sky alone " +
                 "because the sky has a brush of its own.")]
        [Range(0f, 1f)]
        float m_DistortionStrength;

        [SerializeField]
        [Tooltip("Size of those strokes, in tiles across the view. Low values give broad sweeps " +
                 "over the whole horizon, high values a finer tooth. It is measured in view angle " +
                 "rather than in metres, because the strokes live on the image, not on a surface.")]
        [Range(0.5f, 12f)]
        float m_DistortionScale = 2f;

        [SerializeField]
        [Tooltip("Distance, in metres, at which the distortion starts.")]
        [Min(0f)]
        float m_DistortionStart = 40f;

        [SerializeField]
        [Tooltip("Distance, in metres, by which it has reached full strength.")]
        [Min(0.01f)]
        float m_DistortionEnd = 120f;

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

        public float distortionStrength
        {
            get => m_DistortionStrength;
            set => m_DistortionStrength = Mathf.Clamp01(value);
        }

        public float distortionScale
        {
            get => m_DistortionScale;
            set => m_DistortionScale = Mathf.Clamp(value, 0.5f, 12f);
        }

        public bool distortionEnabled => m_Atlas != null && m_DistortionStrength > 0f;

        public Vector4 Pack()
        {
            var fadeEnd = Mathf.Max(m_FadeEnd, m_FadeStart + 0.01f);

            return new Vector4(
                1f / Mathf.Max(0.01f, m_WorldSizePerTile),
                m_FadeStart,
                1f / (fadeEnd - m_FadeStart),
                m_Atlas != null ? 1f : 0f);
        }

        public Vector4 PackDistortion()
        {
            var end = Mathf.Max(m_DistortionEnd, m_DistortionStart + 0.01f);

            return new Vector4(
                m_Atlas != null ? m_DistortionStrength : 0f,
                m_DistortionScale,
                m_DistortionStart,
                1f / (end - m_DistortionStart));
        }
    }
}
