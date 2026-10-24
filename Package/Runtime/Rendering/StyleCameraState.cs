using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    sealed class StyleCameraState : IDisposable
    {
        internal readonly Camera camera;
        internal readonly StyleProfileMix mix = new StyleProfileMix();
        internal double lastRendered;

        StyleSkyLut m_MixLut;

        internal StyleCameraState(Camera camera)
        {
            this.camera = camera;
        }

        internal StyleSkyLut mixLut => m_MixLut ??= new StyleSkyLut();

        public void Dispose()
        {
            m_MixLut?.Dispose();
            m_MixLut = null;
        }
    }

    sealed class StyleCameraStates : IDisposable
    {
        internal const double PruneSeconds = 2.0;

        readonly List<StyleCameraState> m_States = new List<StyleCameraState>();

        internal int count => m_States.Count;

        internal StyleCameraState this[int index] => m_States[index];

        static Camera s_LastGameCamera;

        internal static double clock => Time.realtimeSinceStartupAsDouble;

        internal static Camera lastGameCamera =>
            s_LastGameCamera != null && s_LastGameCamera.isActiveAndEnabled ? s_LastGameCamera : null;

        internal StyleCameraState Touch(Camera camera)
        {
            var now = clock;

            if (camera.cameraType == CameraType.Game && camera.targetTexture == null)
                s_LastGameCamera = camera;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_LastGameCamera = null;
        }
    }
}
