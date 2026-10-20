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

            EditorApplication.delayCall += StyleLiquidBakeLight.Clear;
        }

        static void OnBakeStarted()
        {
            Set(true);
        }

        static void OnBakeCompleted()
        {
            Set(false);

            EditorApplication.delayCall -= StripLightmapMipmaps;
            EditorApplication.delayCall += StripLightmapMipmaps;
        }

        public static bool UsesMipmapLimits(LightingSettings settings)
        {
            var property = new SerializedObject(settings).FindProperty("m_UseMipmapLimits");

            return property != null && property.boolValue;
        }

        public static void StripLightmapMipmaps()
        {
            if (!Lightmapping.TryGetLightingSettings(out var settings) || UsesMipmapLimits(settings))
                return;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var lightmap in LightmapSettings.lightmaps)
                {
                    StripMipmaps(lightmap.lightmapColor);
                    StripMipmaps(lightmap.lightmapDir);
                    StripMipmaps(lightmap.shadowMask);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        static void StripMipmaps(Texture texture)
        {
            if (texture == null)
                return;

            var path = AssetDatabase.GetAssetPath(texture);

            if (AssetImporter.GetAtPath(path) is not TextureImporter importer || !importer.mipmapEnabled)
                return;

            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        static void Watch()
        {
            if (HBStyleBakeState.baking && !Lightmapping.isRunning)
                Set(false);
        }

        static void Set(bool baking)
        {
            if (HBStyleBakeState.baking == baking)
                return;

            HBStyleBakeState.baking = baking;

            if (baking)
            {
                Shader.SetGlobalVector(s_FogParamsId, Vector4.zero);
                Shader.SetGlobalVector(s_FogScatterId, Vector4.zero);
            }

            if (baking)
            {
                LendSunColor();
                StyleLiquidBakeLight.Create();
            }
            else
            {
                ReturnSunColor();
                StyleLiquidBakeLight.Clear();
            }

            DynamicGI.UpdateEnvironment();
        }

        static void LendSunColor()
        {
            s_Sun = FindSun();

            if (s_Sun == null || s_Sun.TryGetComponent<HBStyleSun>(out _))
            {
                s_Sun = null;
                return;
            }

            s_SunColor = s_Sun.color;
            s_SunIntensity = s_Sun.intensity;

            var sky = VolumeManager.instance.stack?.GetComponent<HBStyleSky>();
            var elevation = -s_Sun.transform.forward.y;

            var color = HBStyleKeyLight.Resolve(sky, elevation, out var intensity);

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
