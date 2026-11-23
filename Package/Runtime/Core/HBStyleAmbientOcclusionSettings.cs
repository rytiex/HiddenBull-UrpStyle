using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleAmbientOcclusionSettings
    {
        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("How dark corners, creases and the ground under objects get. 0 turns ambient occlusion off.")]
        float m_Intensity = 1f;

        [SerializeField]
        [Range(0.1f, 4f)]
        [Tooltip("How far (metres) a surface looks for nearby geometry that blocks its ambient light. Large values " +
                 "darken whole rooms, small values only tight creases. Far away the reach grows so distant " +
                 "buildings keep their shading.")]
        float m_Radius = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much ambient occlusion also darkens direct sunlight and lamps. 0 darkens only ambient light.")]
        float m_DirectLighting = 0.25f;

        public float intensity
        {
            get => m_Intensity;
            set => m_Intensity = Mathf.Clamp(value, 0f, 2f);
        }

        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Clamp(value, 0.1f, 4f);
        }

        public float directLighting
        {
            get => m_DirectLighting;
            set => m_DirectLighting = Mathf.Clamp01(value);
        }
    }
}
