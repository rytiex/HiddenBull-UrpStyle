using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleDebugWindow : EditorWindow
    {
        static readonly GUIContent s_ViewLabel = new GUIContent("View",
            "Replaces the colour of every HB Style surface with one term of a module's calculation, so an " +
            "artefact can be traced to the step that made it. Closing this window turns the view off.\n\n" +
            "Shadows: Attenuation is the final main-light shadow; Unfiltered is URP's own filter without the " +
            "contact-hardening filter; Blockers shows the blocker share in red and the contact reach in green; " +
            "Cascades colours each cascade; Filter Values shows softness, contact and taps as red, green and " +
            "blue (black means the settings never arrived); Search Radius shows the blocker search size.\n\n" +
            "Atmosphere: Fog Amount shows the fog in front of each surface in red and the horizon haze (after " +
            "Horizon Layers) in green.");

        Vector2 m_Scroll;

        [MenuItem("Tools/HiddenBull/URP Style/Debug")]
        static void Open()
        {
            GetWindow<StyleDebugWindow>("HB Style Debug");
        }

        void OnDisable()
        {
            SetView(StyleDebugView.Off);
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            EditorGUILayout.LabelField("Debug View", EditorStyles.boldLabel);

            var view = (StyleDebugView)EditorGUILayout.EnumPopup(s_ViewLabel, StyleDebug.view);

            if (view != StyleDebug.view)
                SetView(view);

            EditorGUILayout.Space();
            DrawEnvironment();

            EditorGUILayout.Space();
            DrawCameras();

            EditorGUILayout.EndScrollView();
        }

        static void DrawEnvironment()
        {
            EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Sun", StyleEnvironment.sun, typeof(HBStyleSun), true);
                EditorGUILayout.ObjectField("Profile", StyleEnvironment.targetProfile,
                    typeof(HBStyleEnvironmentProfile), false);
            }

            var weather = StyleEnvironment.weather;

            EditorGUILayout.LabelField("Time of Day", StyleEnvironment.timeOfDay.ToString("0.00") + " h");
            EditorGUILayout.LabelField("Profile Blend", StyleEnvironment.profileBlend.ToString("0.00"));
            EditorGUILayout.LabelField("Rain / Snow / Wind",
                $"{weather.x:0.00} / {weather.y:0.00} / {weather.z:0.00}");
        }

        static void DrawCameras()
        {
            EditorGUILayout.LabelField("Camera States", EditorStyles.boldLabel);

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset)
            {
                EditorGUILayout.LabelField("The active render pipeline is not URP.");
                return;
            }

            var now = StyleCameraStates.clock;
            var found = false;

            foreach (var data in asset.rendererDataList)
            {
                if (data == null)
                    continue;

                foreach (var feature in data.rendererFeatures)
                {
                    if (feature is not HBStyleFeature style || style.cameras == null)
                        continue;

                    found = true;

                    EditorGUILayout.LabelField(data.name, $"{style.cameras.count} base camera(s)");

                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (var i = 0; i < style.cameras.count; i++)
                        {
                            var state = style.cameras[i];
                            var label = state.camera != null ? state.camera.name : "(destroyed)";

                            EditorGUILayout.LabelField(label, $"{now - state.lastRendered:0.00} s ago");
                        }
                    }
                }
            }

            if (!found)
                EditorGUILayout.LabelField("No HB Style Feature on the active URP renderers.");
        }

        static void SetView(StyleDebugView view)
        {
            StyleDebug.view = view;

            var keyword = GlobalKeyword.Create(StyleDebug.Keyword);

            if (view == StyleDebugView.Off)
                Shader.DisableKeyword(keyword);
            else
                Shader.EnableKeyword(keyword);

            SceneView.RepaintAll();
        }
    }
}
