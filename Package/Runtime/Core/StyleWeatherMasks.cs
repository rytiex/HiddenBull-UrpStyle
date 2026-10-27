using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HiddenBull.UrpStyle
{
    static class StyleWeatherMasks
    {
        internal const int Limit = 16;

        const float SmallestRadius = 0.01f;
        const float LongestStep = 0.25f;

        sealed class Dome
        {
            internal HBStyleWeatherMask mask;
            internal Scene scene;
            internal Vector3 position;
            internal float radius;
            internal bool blocking;
            internal bool gradual;
            internal float clearTime;
            internal float coverTime;
            internal float open;
            internal bool leaving;
        }

        static readonly List<Dome> s_Domes = new List<Dome>();
        static readonly Vector4[] s_Picked = new Vector4[Limit];
        static readonly float[] s_Distances = new float[Limit];
        static readonly bool[] s_Blocking = new bool[Limit];

        static double s_LastClock = double.NegativeInfinity;

        internal static void Register(HBStyleWeatherMask mask)
        {
            var dome = Find(mask);

            if (dome == null)
            {
                dome = new Dome { mask = mask };
                s_Domes.Add(dome);
            }

            Capture(dome);
            dome.leaving = false;

            if (Instant(dome))
                dome.open = 1f;

            mask.open = dome.open;
        }

        internal static void Release(HBStyleWeatherMask mask)
        {
            var dome = Find(mask);

            if (dome == null)
                return;

            Capture(dome);
            dome.leaving = true;

            if (Instant(dome))
                Remove(dome);
        }

        internal static void Detach(HBStyleWeatherMask mask)
        {
            var dome = Find(mask);

            if (dome == null)
                return;

            dome.mask = null;

            if (!dome.leaving || Instant(dome))
                Remove(dome);
        }

        static Dome Find(HBStyleWeatherMask mask)
        {
            foreach (var dome in s_Domes)
            {
                if (ReferenceEquals(dome.mask, mask))
                    return dome;
            }

            return null;
        }

        static void Remove(Dome dome)
        {
            if (dome.mask != null)
                dome.mask.open = 0f;

            s_Domes.Remove(dome);
        }

        static void Capture(Dome dome)
        {
            var mask = dome.mask;

            dome.scene = mask.gameObject.scene;
            dome.position = mask.transform.position;
            dome.radius = mask.radius;
            dome.blocking = mask.blockPrecipitation;
            dome.gradual = mask.transition == HBStyleWeatherMaskTransition.Gradual;
            dome.clearTime = mask.clearTime;
            dome.coverTime = mask.coverTime;
        }

        static bool Instant(Dome dome)
        {
            return StyleWeatherState.instant || !dome.gradual;
        }

        static float Toward(float value, float target, float step, float rise, float fall)
        {
            if (value < target)
                return rise > 0f ? Mathf.Min(value + step / rise, target) : target;

            return fall > 0f ? Mathf.Max(value - step / fall, target) : target;
        }

        static void Tick()
        {
            var now = StyleEnvironment.clock;

            if (now == s_LastClock)
                return;

            var step = double.IsNegativeInfinity(s_LastClock) ? 0f : (float)(now - s_LastClock);
            s_LastClock = now;
            step = Mathf.Min(Mathf.Max(step, 0f), LongestStep);

            for (var i = s_Domes.Count - 1; i >= 0; i--)
            {
                var dome = s_Domes[i];

                if (dome.mask == null && !dome.leaving)
                {
                    s_Domes.RemoveAt(i);
                    continue;
                }

                if (dome.mask != null && !dome.leaving)
                    Capture(dome);

                var target = dome.leaving ? 0f : 1f;

                dome.open = Instant(dome) ? target : Toward(dome.open, target, step, dome.clearTime, dome.coverTime);

                if (dome.mask != null)
                    dome.mask.open = dome.open;

                if (dome.leaving && dome.open <= 0f)
                    Remove(dome);
            }
        }

        internal static int Collect(Vector2 centre, float reach, Vector4[] masks, out int blocking)
        {
            Tick();

            var picked = 0;

            foreach (var dome in s_Domes)
            {
                var radius = dome.radius * dome.open;

                if (radius < SmallestRadius)
                    continue;

                var position = dome.position;
                var offset = new Vector2(position.x - centre.x, position.z - centre.y);
                var limit = reach + radius;

                if (Mathf.Abs(offset.x) > limit || Mathf.Abs(offset.y) > limit)
                    continue;

                var away = offset.sqrMagnitude;
                var slot = picked;

                if (picked == Limit)
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
                    picked++;
                }

                s_Picked[slot] = new Vector4(position.x, position.y, position.z, radius);
                s_Distances[slot] = away;
                s_Blocking[slot] = dome.blocking;
            }

            blocking = 0;

            for (var i = 0; i < picked; i++)
            {
                if (s_Blocking[i])
                    masks[blocking++] = s_Picked[i];
            }

            var next = blocking;

            for (var i = 0; i < picked; i++)
            {
                if (!s_Blocking[i])
                    masks[next++] = s_Picked[i];
            }

            return picked;
        }

        static void OnSceneUnloaded(Scene scene)
        {
            for (var i = s_Domes.Count - 1; i >= 0; i--)
            {
                if (s_Domes[i].mask == null && s_Domes[i].scene == scene)
                    s_Domes.RemoveAt(i);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_Domes.Clear();
            s_LastClock = double.NegativeInfinity;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
    }
}
