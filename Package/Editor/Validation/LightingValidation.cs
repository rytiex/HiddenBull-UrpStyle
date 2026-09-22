using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HiddenBull.UrpStyle.Editor
{
    static class LightingValidation
    {
        const string k_MenuPath = "Tools/HiddenBull/URP Style/Validate Scene Lighting";

        [InitializeOnLoadMethod]
        static void Subscribe()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Validate(scene, logOnlyOnFailure: true);
        }

        [MenuItem(k_MenuPath)]
        static void ValidateActiveScene()
        {
            Validate(SceneManager.GetActiveScene(), logOnlyOnFailure: false);
        }

        static void Validate(Scene scene, bool logOnlyOnFailure)
        {
            var problems = new List<string>();

            CheckMixedLightingMode(problems);
            CheckBakedDirectLights(problems);

            if (problems.Count == 0)
            {
                if (!logOnlyOnFailure)
                    Debug.Log($"[HiddenBull URP Style] Lighting in '{scene.name}' is compatible with the style.");

                return;
            }

            var message = new StringBuilder();
            message.AppendLine($"[HiddenBull URP Style] Lighting in '{scene.name}' will not render as intended:");
            foreach (var problem in problems)
                message.AppendLine($"  • {problem}");

            Debug.LogWarning(message.ToString().TrimEnd());
        }

        static void CheckMixedLightingMode(ICollection<string> problems)
        {
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings == null)
                return;

            switch (settings.mixedBakeMode)
            {
                case MixedLightingMode.Shadowmask:
                    problems.Add("Lighting Settings use Shadowmask. It bakes shadows into lightmaps, " +
                                 "and baked shadows cannot carry the style's contact-hardening penumbra, " +
                                 "so the scene ends up with two different shadow languages. " +
                                 "Use Baked Indirect.");
                    break;

                case MixedLightingMode.Subtractive:
                    problems.Add("Lighting Settings use Subtractive. It bakes direct light and shadows, " +
                                 "which bypasses the style's diffuse model entirely. Use Baked Indirect.");
                    break;
            }
        }

        static void CheckBakedDirectLights(ICollection<string> problems)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);

            foreach (var light in lights)
            {
                if (light.lightmapBakeType != LightmapBakeType.Baked)
                    continue;

                problems.Add($"Light '{light.name}' is fully Baked, so its direct contribution is baked " +
                             "with Unity's lighting model rather than the style's. Set it to Mixed " +
                             "(direct stays realtime, indirect bakes) or Realtime.");
            }
        }
    }
}
