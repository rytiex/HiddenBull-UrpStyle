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
            "Horizon Layers) in green.\n\n" +
            "Weather: Shelter is white where a surface has open sky above it and black where something covers " +
            "it, read from the top-down shelter map 160 m around the camera. Wetness shows how wet each surface " +
            "with Receive Weather is (white is soaked, after its material's Wetness); Snow shows how much snow " +
            "covers it. Precipitation draws the falling rain and snow that a roof should hide in red instead of " +
            "hiding it, so the roof check can be seen.");

        static readonly GUIContent s_FallingLabel = new GUIContent("Falling Drops / Flakes",
            "Rain drops and snow flakes sent to the Game camera this frame, before the ones under roofs, beyond " +
            "the box around the camera or in fog are dropped on the GPU.");

        static readonly GUIContent s_ShelterLabel = new GUIContent("Show Shelter Map",
            "Draws the shelter map below: the scene seen from above, 160 m around the camera. Brighter is " +
            "higher (10 m above the camera is white, 10 m below black); dark blue is open sky with nothing " +
            "below; the red dot is the camera.");

        static readonly GUIContent s_ShelterCameraLabel = new GUIContent("Camera",
            "Which camera's shelter map to draw.");

        static readonly string[] s_ShelterCameras = { "Scene View", "Game" };

        Vector2 m_Scroll;

        [MenuItem("Tools/HiddenBull/URP Style/Debug")]
        static void Open()
        {
            GetWindow<StyleDebugWindow>("HB Style Debug");
        }

        void OnDisable()
        {
            SetView(StyleDebugView.Off);
            StyleDebug.previewShelter = false;
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
            DrawShelter();

            EditorGUILayout.Space();
            DrawCameras();

            EditorGUILayout.EndScrollView();
        }

        static void DrawEnvironment()
        {
            EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Controller", StyleEnvironment.controller,
                    typeof(HBStyleEnvironmentController), true);
                EditorGUILayout.ObjectField("Profile", StyleEnvironment.targetProfile,
                    typeof(HBStyleEnvironmentProfile), false);
            }

            var weather = StyleEnvironment.weather;

            EditorGUILayout.LabelField("Time of Day", StyleEnvironment.timeOfDay.ToString("0.00") + " h");
            EditorGUILayout.LabelField("Profile Blend", StyleEnvironment.profileBlend.ToString("0.00"));
            EditorGUILayout.LabelField("Rain / Snow / Wind",
                $"{weather.x:0.00} / {weather.y:0.00} / {weather.z:0.00}");
            EditorGUILayout.LabelField("Wet / Snow Settled",
                $"{StyleWeatherState.wetness:0.00} / {StyleWeatherState.snow:0.00}");
        }

        void DrawShelter()
        {
            EditorGUILayout.LabelField("Weather", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(s_FallingLabel,
                new GUIContent($"{StyleDebug.rainDrops} / {StyleDebug.snowFlakes}"));

            StyleDebug.previewShelter = EditorGUILayout.Toggle(s_ShelterLabel, StyleDebug.previewShelter);

            if (!StyleDebug.previewShelter)
                return;

            var camera = EditorGUILayout.Popup(s_ShelterCameraLabel, StyleDebug.previewGameCamera ? 1 : 0,
                                               s_ShelterCameras);

            StyleDebug.previewGameCamera = camera == 1;

            var texture = StyleDebug.shelterPreview;

            if (texture == null)
            {
                EditorGUILayout.HelpBox("Waiting for the camera to render.", MessageType.None);
                return;
            }

            var size = Mathf.Min(position.width - 24f, 384f);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            EditorGUI.DrawPreviewTexture(rect, texture);
            SceneView.RepaintAll();
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
