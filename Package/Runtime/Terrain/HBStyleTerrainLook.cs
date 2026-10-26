using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleTerrainLook
    {
        [SerializeField]
        [Range(0.25f, 4f)]
        [Tooltip("Size of the brush strokes on the ground.")]
        float m_BrushSize = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How far the strokes drag the layer textures along, for a painted look.")]
        float m_BrushPaint = 1f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How much the strokes move the light and shadow tones.")]
        float m_ToneStrokes = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the strokes bend the ground's lighting, like thick paint.")]
        float m_BrushRelief = 0.235f;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("How much the strokes break up the edge between light and shadow.")]
        float m_ShadingBreakup = 0.5f;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("How much the strokes break up the ambient light.")]
        float m_AmbientBreakup = 0.125f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the strokes vary the ground colour.")]
        float m_AlbedoVariation = 0.125f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Darkens hollows and lightens ridges of the ground's shape.")]
        float m_Curvature = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Large, slow brightness changes over the ground so wide areas do not look flat.")]
        float m_MacroVariation = 0.5f;

        public float brushSize
        {
            get => m_BrushSize;
            set => m_BrushSize = Mathf.Clamp(value, 0.25f, 4f);
        }

        public float brushPaint
        {
            get => m_BrushPaint;
            set => m_BrushPaint = Mathf.Clamp01(value);
        }

        public float toneStrokes
        {
            get => m_ToneStrokes;
            set => m_ToneStrokes = Mathf.Clamp(value, 0f, 2f);
        }

        public float brushRelief
        {
            get => m_BrushRelief;
            set => m_BrushRelief = Mathf.Clamp01(value);
        }

        public float shadingBreakup
        {
            get => m_ShadingBreakup;
            set => m_ShadingBreakup = Mathf.Clamp(value, 0f, 0.5f);
        }

        public float ambientBreakup
        {
            get => m_AmbientBreakup;
            set => m_AmbientBreakup = Mathf.Clamp(value, 0f, 0.5f);
        }

        public float albedoVariation
        {
            get => m_AlbedoVariation;
            set => m_AlbedoVariation = Mathf.Clamp01(value);
        }

        public float curvature
        {
            get => m_Curvature;
            set => m_Curvature = Mathf.Clamp01(value);
        }

        public float macroVariation
        {
            get => m_MacroVariation;
            set => m_MacroVariation = Mathf.Clamp01(value);
        }

        internal Vector4 brush => new Vector4(m_BrushSize, m_BrushPaint, m_ToneStrokes, m_BrushRelief);

        internal Vector4 style => new Vector4(m_ShadingBreakup, m_AmbientBreakup, m_AlbedoVariation, m_Curvature);

        internal Vector4 variation => new Vector4(m_MacroVariation, 0f, 0f, 0f);
    }
}
