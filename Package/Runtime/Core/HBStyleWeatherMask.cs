using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Environment/HB Style Weather Mask")]
    public sealed class HBStyleWeatherMask : MonoBehaviour
    {
        public const int Limit = 16;

        static readonly List<HBStyleWeatherMask> s_Enabled = new List<HBStyleWeatherMask>();

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("How far from this object surfaces are kept free of snow and rain, in metres, as if a dome " +
                 "stood over it. The ground clears and covers again over the Cover Time and Uncover Time set " +
                 "on Style Weather, so nothing melts or soaks at once.")]
        float m_Radius = 3f;

        [SerializeField]
        [Tooltip("Also stops falling rain and snow inside the dome, fading the drops out as they reach it. " +
                 "Turn it off to keep them falling, onto a camp fire for instance.")]
        bool m_BlockPrecipitation = true;

        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, 0.1f);
        }

        public bool blockPrecipitation
        {
            get => m_BlockPrecipitation;
            set => m_BlockPrecipitation = value;
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
            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, m_Radius);
        }

        internal static int Collect(Vector3 camera, float reach, Vector4[] masks, float[] distances,
                                     bool precipitation = false)
        {
            var count = 0;

            foreach (var mask in s_Enabled)
            {
                if (precipitation && !mask.m_BlockPrecipitation)
                    continue;

                var position = mask.transform.position;
                var offset = new Vector2(position.x - camera.x, position.z - camera.z);
                var limit = reach + mask.m_Radius;

                if (Mathf.Abs(offset.x) > limit || Mathf.Abs(offset.y) > limit)
                    continue;

                var away = offset.sqrMagnitude;
                var slot = count;

                if (count == Limit)
                {
                    slot = 0;

                    for (var other = 1; other < Limit; other++)
                    {
                        if (distances[other] > distances[slot])
                            slot = other;
                    }

                    if (distances[slot] <= away)
                        continue;
                }
                else
                {
                    count++;
                }

                masks[slot] = new Vector4(position.x, position.y, position.z, mask.m_Radius);
                distances[slot] = away;
            }

            return count;
        }
    }
}
