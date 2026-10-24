using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    sealed class StyleCameraState : IDisposable
    {
        internal readonly Camera camera;
        internal double lastRendered;

        internal StyleCameraState(Camera camera)
        {
            this.camera = camera;
        }

        public void Dispose()
        {
        }
    }

    sealed class StyleCameraStates : IDisposable
    {
        internal const double PruneSeconds = 2.0;

        readonly List<StyleCameraState> m_States = new List<StyleCameraState>();

        internal int count => m_States.Count;

        internal StyleCameraState this[int index] => m_States[index];

        internal static double clock => Time.realtimeSinceStartupAsDouble;

        internal StyleCameraState Touch(Camera camera)
        {
            var now = clock;

            Prune(now);

            var state = Find(camera);

            if (state == null)
            {
                state = new StyleCameraState(camera);
                m_States.Add(state);
            }

            state.lastRendered = now;

            return state;
        }

        internal StyleCameraState Find(Camera camera)
        {
            if (camera == null)
                return null;

            for (var i = 0; i < m_States.Count; i++)
            {
                if (m_States[i].camera == camera)
                    return m_States[i];
            }

            return null;
        }

        void Prune(double now)
        {
            for (var i = m_States.Count - 1; i >= 0; i--)
            {
                var state = m_States[i];

                if (state.camera != null && now - state.lastRendered <= PruneSeconds)
                    continue;

                state.Dispose();
                m_States.RemoveAt(i);
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < m_States.Count; i++)
                m_States[i].Dispose();

            m_States.Clear();
        }
    }
}
