#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    sealed class StylePreviewEnvironment
    {
        static readonly Dictionary<Camera, StylePreviewEnvironment> s_Cameras =
            new Dictionary<Camera, StylePreviewEnvironment>();

        internal HBStyleEnvironmentProfile profile;
        internal float timeOfDay = 12f;
        internal Vector3 sunDirection = new Vector3(0.5f, 0.45f, -0.74f).normalized;
        internal Texture brushAtlas;
        internal Texture cloudAtlas;
        internal float cloudCoverage = 0.5f;
        internal float cloudSoftness = 0.25f;

        internal HBStyleEnvironmentProfile shownProfile =>
            profile != null ? profile : StyleEnvironment.defaultProfile;

        internal void KeyLight(out Color colour, out float intensity)
        {
            StyleKeyLight.Resolve(shownProfile, StyleKeyLight.SunElevation(timeOfDay), out colour, out intensity);
        }

        internal static void Attach(Camera camera, StylePreviewEnvironment environment)
        {
            if (camera != null)
                s_Cameras[camera] = environment;
        }

        internal static void Detach(Camera camera)
        {
            if (camera != null)
                s_Cameras.Remove(camera);
        }

        internal static StylePreviewEnvironment Find(Camera camera)
        {
            return camera != null && s_Cameras.TryGetValue(camera, out var environment) ? environment : null;
        }
    }
}
#endif
