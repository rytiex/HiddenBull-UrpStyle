using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HB Style/Terrain/HB Style Grass Bender")]
    public sealed class HBStyleGrassBender : MonoBehaviour
    {
        internal const int Limit = 8;

        static readonly List<HBStyleGrassBender> s_Enabled = new List<HBStyleGrassBender>();
        static readonly float[] s_Distances = new float[Limit];

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Radius in metres of a ball around this object. Grass and flowers bend only where the ball reaches " +
                 "their tips, so it must touch the grass. The 8 benders nearest the camera count.")]
        float m_Radius = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How flat the grass is pressed at the centre. 0 leaves it standing.")]
        float m_Strength = 1f;

        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, 0.05f);
        }

        public float strength
        {
            get => m_Strength;
            set => m_Strength = Mathf.Clamp01(value);
        }

        void OnEnable()
        {
            s_Enabled.Remove(this);
            s_Enabled.Add(this);
        }

        void OnDisable()
        {
            s_Enabled.Remove(this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, m_Radius);
        }

        internal static int Collect(Vector3 camera, float reach, Vector4[] benders, Vector4[] presses)
        {
            var count = 0;

            foreach (var bender in s_Enabled)
            {
                if (bender.m_Strength <= 0f)
                    continue;

                var position = bender.transform.position;
                var away = (position - camera).sqrMagnitude;
                var limit = reach + bender.m_Radius;

                if (away > limit * limit)
                    continue;

                var slot = count;

                if (count == Limit)
                {
                    slot = 0;

                    for (var other = 1; other < Limit; other++)
                    {
                        if (s_Distances[other] > s_Distances[slot])
                            slot = other;
                    }

                    if (s_Distances[slot] <= away)
                        continue;
                }
                else
                {
                    count++;
                }

                benders[slot] = new Vector4(position.x, position.y, position.z, bender.m_Radius);
                presses[slot] = new Vector4(bender.m_Strength, 0f, 0f, 0f);
                s_Distances[slot] = away;
            }

            return count;
        }
    }
}
