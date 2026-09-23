using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleBakeHook
    {
        static readonly int s_FogParamsId = Shader.PropertyToID("_HB_FogParams");
        static readonly int s_FogScatterId = Shader.PropertyToID("_HB_FogScatter");

        static Light s_Sun;
        static Color s_SunColor;
        static float s_SunIntensity;

        static StyleBakeHook()
        {
            Set(false);

            Lightmapping.bakeStarted -= OnBakeStarted;
            Lightmapping.bakeStarted += OnBakeStarted;

            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Lightmapping.bakeCompleted += OnBakeCompleted;

            EditorApplication.update -= Watch;
            EditorApplication.update += Watch;
        }

        static void OnBakeStarted()
        {
            Set(true);
        }

        static void OnBakeCompleted()
        {
            Set(false);
        }

        static void Watch()
        {
            if (StyleBakeState.baking && !Lightmapping.isRunning)
                Set(false);
        }

        static void Set(bool baking)
        {
            if (StyleBakeState.baking == baking)
                return;

            StyleBakeState.baking = baking;

            if (baking)
            {
                Shader.SetGlobalVector(s_FogParamsId, Vector4.zero);
                Shader.SetGlobalVector(s_FogScatterId, Vector4.zero);
            }

            if (baking)
                LendSunColor();
            else
                ReturnSunColor();

            DynamicGI.UpdateEnvironment();
        }

        static void LendSunColor()
        {
            s_Sun = FindSun();

            if (s_Sun == null)
                return;

            s_SunColor = s_Sun.color;
            s_SunIntensity = s_Sun.intensity;

            var sky = VolumeManager.instance.stack?.GetComponent<StyleSky>();
            var elevation = -s_Sun.transform.forward.y;

            var color = StyleKeyLight.Resolve(sky, elevation, out var intensity);

            s_Sun.color = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? color.gamma
                : color;

            s_Sun.intensity = intensity;
        }

        static void ReturnSunColor()
        {
            if (s_Sun == null)
                return;

            s_Sun.color = s_SunColor;
            s_Sun.intensity = s_SunIntensity;
            s_Sun = null;
        }

        static Light FindSun()
        {
            if (RenderSettings.sun != null)
                return RenderSettings.sun;

            Light brightest = null;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type != LightType.Directional)
                    continue;

                if (brightest == null || light.intensity > brightest.intensity)
                    brightest = light;
            }

            return brightest;
        }
    }
}
