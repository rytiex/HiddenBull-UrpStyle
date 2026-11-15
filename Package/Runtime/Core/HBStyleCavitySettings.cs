using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleCavitySettings
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the outer edges of objects are brightened into a thin light line. 0 turns it off.")]
        float m_Ridge = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much inner corners and creases are darkened. 0 turns it off.")]
        float m_Valley = 0.3f;

        [SerializeField]
        [Range(0.5f, 4f)]
        [Tooltip("Thickness of the lines in screen pixels from about 8 m on. Closer to the camera they grow, up to " +
                 "four times, so they stay visible on near objects.")]
        float m_Width = 0.5f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Distance from the camera, in metres, by which the lines have faded out. Far objects are only " +
                 "a few pixels wide, so lines there would cover them.")]
        float m_Distance = 30f;

        public float ridge => m_Ridge;

        public float valley => m_Valley;

        public float width => m_Width;

        public float distance => m_Distance;
    }
}
