using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleSunInstaller
    {
        static bool s_Pending;

        static StyleSunInstaller()
        {
            EditorSceneManager.sceneOpened += (_, _) => Schedule();
            EditorApplication.hierarchyChanged += Schedule;

            Schedule();
        }

        static void Schedule()
        {
            if (s_Pending)
                return;

            s_Pending = true;
            EditorApplication.delayCall += Install;
        }

        static void Install()
        {
            s_Pending = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode || HBStyleSun.active != null ||
                PrefabStageUtility.GetCurrentPrefabStage() != null || !StyleInUse())
                return;

            var sun = FindSun();

            if (sun == null || sun.TryGetComponent<HBStyleSun>(out _))
                return;

            Add(sun);
        }

        public static HBStyleSun Add(Light sun)
        {
            var towardSun = -sun.transform.forward;

            var added = Undo.AddComponent<HBStyleSun>(sun.gameObject);
            added.Match(towardSun);

            EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);

            return added;
        }

        public static Light FindSun()
        {
            if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional)
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

        static bool StyleInUse()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset)
                return false;

            foreach (var data in asset.rendererDataList)
            {
                if (data == null)
                    continue;

                foreach (var feature in data.rendererFeatures)
                {
                    if (feature is HBStyleFeature && feature.isActive)
                        return true;
                }
            }

            return false;
        }
    }
}
