using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleCloudSettings
    {
        [SerializeField]
        [Tooltip("Cloud atlas: RG carry the relief, A the coverage. Leaving it empty turns clouds off.")]
        Texture2D m_Atlas;

        [SerializeField]
        [Tooltip("How much of the sky the clouds take up (0 clear to 1 overcast). Past about half coverage " +
                 "the clouds start to hide the sun: its light and shadows fade out and the ambient lifts a " +
                 "little to make up for it.")]
        [Range(0f, 1f)]
        float m_Coverage = 0.475f;

        [SerializeField]
        [ColorUsage(false, true)]
        [Tooltip("Tint over the finished cloud. The colour itself follows the time of day; this only pushes " +
                 "it warmer or cooler.")]
        Color m_Tint = Color.white;

        [SerializeField]
        [Tooltip("How far the tint gathers in the shaded parts of the cloud (0 even to 1 bases only).")]
        [Range(0f, 1f)]
        float m_TintShade = 1f;

        [SerializeField]
        [Tooltip("Size of the cloud forms (0.25 to 2). Lower is bigger and closer.")]
        [Range(0.25f, 2f)]
        float m_Scale = 0.815f;

        [SerializeField]
        [Tooltip("Direction and speed the cloud bank drifts, as a velocity.")]
        Vector2 m_Wind = new Vector2(1.25f, 0.2f);

        [SerializeField]
        [Tooltip("How softly the cloud edge dissolves into the sky (0.01 to 1).")]
        [Range(0.01f, 1f)]
        float m_Softness = 0.125f;

        [SerializeField]
        [Tooltip("How many shells the cloud is built from (1 to 8). Each one is a texture read per sky pixel.")]
        [Range(1, 8)]
        int m_Steps = 4;

        [SerializeField]
        [Tooltip("How far apart the shells sit (0 to 1). Higher pulls them into a deeper body.")]
        [Range(0f, 1f)]
        float m_StepSpacing = 0.125f;

        [SerializeField]
        [Tooltip("How much shading the cloud takes (0 to 1): light absorbed through the shells and the sun's " +
                 "shaping across each one, which gives a dark base under a bright top.")]
        [Range(0f, 1f)]
        float m_StepShading = 0.225f;

        [SerializeField]
        [Tooltip("How much the shells shrink away from the middle one (0 to 1), so the stack closes into a " +
                 "rounded body.")]
        [Range(0f, 1f)]
        float m_StepTaper = 0.563f;

        [SerializeField]
        [FormerlySerializedAs("m_Translucency")]
        [Tooltip("Bright thin edges where the cloud thins out toward the sun (0 to 1).")]
        [Range(0f, 1f)]
        float m_SilverLining = 1f;

        [SerializeField]
        [Tooltip("Glow through the body of the cloud the sun is behind, stronger where the cloud is thin " +
                 "(0 to 1).")]
        [Range(0f, 1f)]
        float m_SunThrough = 1f;

        [SerializeField]
        [Tooltip("Darkens the thin edges of clouds seen away from the sun, so bodies read rounder (0 to 1).")]
        [Range(0f, 1f)]
        float m_Powder = 0.5f;

        public Texture2D atlas => m_Atlas;
        public float coverage => m_Coverage;
        public Color tint => m_Tint;
        public float tintShade => m_TintShade;
        public float scale => m_Scale;
        public Vector2 wind => m_Wind;
        public float softness => m_Softness;
        public int steps => m_Steps;
        public float stepSpacing => m_StepSpacing;
        public float stepShading => m_StepShading;
        public float stepTaper => m_StepTaper;
        public float silverLining => m_SilverLining;
        public float sunThrough => m_SunThrough;
        public float powder => m_Powder;
    }
}
