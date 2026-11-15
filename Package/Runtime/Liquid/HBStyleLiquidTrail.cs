using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [AddComponentMenu("HiddenBull/HB Style/Liquid/HB Style Liquid Trail")]
    public sealed class HBStyleLiquidTrail : MonoBehaviour
    {
        internal const int Limit = 32;
        internal const int StampSize = 2;

        const float FullSpeed = 2f;
        const float LeastSpeed = 0.05f;
        const float LeastStep = 1e-4f;
        const float LongestStep = 0.25f;
        const float LeastReach = 0.5f;

        static readonly List<HBStyleLiquidTrail> s_Enabled = new List<HBStyleLiquidTrail>();
        static readonly float[] s_Distances = new float[Limit];

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Width in metres of the foam trail this object leaves on a liquid. Put the object at the " +
                 "waterline of the thing that moves: the trail only starts while it is at the surface.")]
        float m_Width = 1f;

        [SerializeField]
        [Range(0.1f, 30f)]
        [Tooltip("Seconds until the foam of the trail has faded away.")]
        float m_Lifetime = 4f;

        [SerializeField]
        [Range(0f, 3f)]
        [Tooltip("How strongly this object pushes the liquid: the size of the ripples that spread from its trail. " +
                 "0 leaves only the foam.")]
        float m_Ripples = 1f;

        Vector3 m_Last;
        Vector3 m_Before;
        double m_LastTime = double.NegativeInfinity;
        float m_Strength;

        public float width
        {
            get => m_Width;
            set => m_Width = Mathf.Max(value, 0.05f);
        }

        public float lifetime
        {
            get => m_Lifetime;
            set => m_Lifetime = Mathf.Clamp(value, 0.1f, 30f);
        }

        public float ripples
        {
            get => m_Ripples;
            set => m_Ripples = Mathf.Clamp(value, 0f, 3f);
        }

        internal static bool any => s_Enabled.Count > 0;

        void OnEnable()
        {
            s_Enabled.Remove(this);
            s_Enabled.Add(this);
            m_LastTime = double.NegativeInfinity;
        }

        void OnDisable()
        {
            s_Enabled.Remove(this);
        }

        void Advance(double now)
        {
            var position = transform.position;
            var step = (float)(now - m_LastTime);

            if (step < LeastStep)
                return;

            if (step > LongestStep)
            {
                m_Last = position;
                m_Before = position;
                m_LastTime = now;
                m_Strength = 0f;
                return;
            }

            var reach = Mathf.Max(m_Width, LeastReach);
            var moved = position - m_Last;
            var speed = new Vector2(moved.x, moved.z).magnitude / step;
            var afloat = HBStyleLiquid.TryGetUncut(position, out var surface) &&
                         Mathf.Abs(surface - position.y) <= reach;

            m_Before = m_Last;
            m_Last = position;
            m_LastTime = now;
            m_Strength = afloat && speed > LeastSpeed ? Mathf.Clamp01(speed / FullSpeed) : 0f;
        }

        internal static int Collect(Vector3 camera, float reach, Vector4[] stamps, double now)
        {
            var count = 0;

            foreach (var trail in s_Enabled)
            {
                if (trail == null)
                    continue;

                trail.Advance(now);

                if (trail.m_Strength <= 0f)
                    continue;

                var away = (trail.m_Last - camera).sqrMagnitude;
                var limit = reach + trail.m_Width;

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

                stamps[slot * StampSize] = new Vector4(trail.m_Last.x, trail.m_Last.z, trail.m_Width * 0.5f,
                                                       trail.m_Strength);
                stamps[slot * StampSize + 1] = new Vector4(trail.m_Before.x, trail.m_Before.z,
                                                           1f / trail.m_Lifetime, trail.m_Ripples);
                s_Distances[slot] = away;
            }

            return count;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, m_Width * 0.5f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_Enabled.Clear();
        }
    }
}
