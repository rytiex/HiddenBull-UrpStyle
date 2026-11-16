using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleStudioView
    {
        Sphere,
        Plane,
        Flat
    }

    sealed class StyleStudioPreview
    {
        const string LitShader = "HB URP Style/Lit";
        const float BarHeight = 20f;
        const float BarWidth = 240f;
        const float PanelWidth = 330f;
        const float PanelHeight = 100f;
        const float SunSize = 74f;
        const float PanelMargin = 8f;

        static readonly int s_BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int s_BumpMap = Shader.PropertyToID("_BumpMap");
        static readonly int s_MaskMap = Shader.PropertyToID("_MaskMap");
        static readonly int s_EmissionMap = Shader.PropertyToID("_EmissionMap");
        static readonly int s_EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int s_EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");

        static class Styles
        {
            internal static readonly GUIContent[] Views =
            {
                new GUIContent("Sphere", "The material on a ball. Scroll to zoom."),
                new GUIContent("Plane", "The material on a tilted floor. Scroll to zoom."),
                new GUIContent("Flat", "The picked map repeated 2 x 2, to check that the edges join.")
            };

            internal static readonly GUIContent Environment = new GUIContent("Environment",
                "Environment profile the preview is lit with. Only the preview changes, not the scene.");

            internal static readonly GUIContent Time = new GUIContent("Time",
                "Hour of the day, 0 to 12. Changes the light's colour and strength like the Environment Controller.");

            internal static readonly GUIContent Sun = new GUIContent("Light",
                "Drag the dot to move the light. In the middle it shines from the camera, at the rim it grazes " +
                "the surface from that side.");

            internal static readonly GUIContent Default = new GUIContent("Default");
        }

        PreviewRenderUtility m_Utility;
        Material m_Material;
        Mesh m_Sphere;
        Mesh m_Plane;

        readonly StylePreviewEnvironment m_Environment = new StylePreviewEnvironment { timeOfDay = 12f };
        Vector2 m_Sun = new Vector2(0.5f, 0.45f);

        HBStyleEnvironmentProfile[] m_Profiles;
        GUIContent[] m_ProfileNames;

        internal StyleStudioView view = StyleStudioView.Sphere;
        float m_Distance = 4.2f;

        internal StyleStudioPreview()
        {
            EditorApplication.projectChanged += ForgetProfiles;
        }

        internal Rect DrawBar(Rect rect)
        {
            var width = Mathf.Min(rect.width, BarWidth);
            var bar = new Rect(rect.center.x - width * 0.5f, rect.y, width, BarHeight);

            view = (StyleStudioView)GUI.Toolbar(bar, (int)view, Styles.Views, EditorStyles.miniButton);
            return Rect.MinMaxRect(rect.xMin, rect.yMin + BarHeight + 4f, rect.xMax, rect.yMax);
        }

        internal void DrawFlat(Rect rect, Texture texture)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            if (texture == null)
                return;

            var side = Mathf.Min(rect.width, rect.height);
            var square = new Rect(rect.center.x - side * 0.5f, rect.center.y - side * 0.5f, side, side);

            GUI.DrawTextureWithTexCoords(square, texture, new Rect(0f, 0f, 2f, 2f), false);
        }

        internal void DrawModel(Rect rect, Texture albedo, Texture normal, Texture mask, Texture emission = null)
        {
            if (!Prepare())
            {
                EditorGUI.HelpBox(rect, $"The shader '{LitShader}' was not found.", MessageType.Warning);
                return;
            }

            var width = Mathf.Min(PanelWidth, rect.width - PanelMargin * 2f);
            var panel = new Rect(rect.xMax - width - PanelMargin, rect.yMax - PanelHeight - PanelMargin, width,
                                 PanelHeight);

            HandleZoom(rect, panel);

            if (Event.current.type == EventType.Repaint)
                Render(rect, albedo, normal, mask, emission);

            DrawPanel(panel);
        }

        void Render(Rect rect, Texture albedo, Texture normal, Texture mask, Texture emission)
        {
            var sphere = view == StyleStudioView.Sphere;

            m_Material.SetTexture(s_BaseMap, albedo);
            m_Material.SetTexture(s_BumpMap, normal);
            m_Material.SetTexture(s_MaskMap, mask);
            m_Material.SetTexture(s_EmissionMap, emission);
            m_Material.SetColor(s_EmissionColor, Color.white);
            m_Material.SetFloat(s_EmissionEnabled, emission != null ? 1f : 0f);

            if (emission != null)
                m_Material.EnableKeyword("_HB_EMISSION");
            else
                m_Material.DisableKeyword("_HB_EMISSION");
            m_Material.SetTextureScale(s_BaseMap, sphere ? new Vector2(2f, 1f) : new Vector2(2f, 2f));

            var camera = m_Utility.camera;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -m_Distance), Quaternion.identity);

            m_Environment.KeyLight(out var colour, out var intensity);

            var light = m_Utility.lights[0];
            light.transform.rotation = Quaternion.LookRotation(-m_Environment.sunDirection);
            light.color = colour;
            light.intensity = intensity;

            m_Utility.BeginPreview(rect, GUIStyle.none);

            if (sphere)
            {
                m_Utility.DrawMesh(m_Sphere, Matrix4x4.identity, m_Material, 0);
            }
            else
            {
                var turn = Quaternion.Euler(-55f, 0f, 0f);
                m_Utility.DrawMesh(m_Plane, Matrix4x4.TRS(Vector3.zero, turn, Vector3.one * 0.2f), m_Material, 0);
            }

            camera.Render();
            GUI.DrawTexture(rect, m_Utility.EndPreview(), ScaleMode.StretchToFill, false);
        }

        void DrawPanel(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 0.85f));

            var sun = new Rect(rect.xMax - SunSize - 8f, rect.y + 18f, SunSize, SunSize);
            var label = new Rect(sun.x - 8f, rect.y + 2f, SunSize + 16f, 16f);

            HandleSun(sun);
            GUI.Label(label, Styles.Sun, EditorStyles.centeredGreyMiniLabel);

            if (Event.current.type == EventType.Repaint)
                DrawSun(sun);

            var labelWidth = EditorGUIUtility.labelWidth;
            var fields = Rect.MinMaxRect(rect.x + 6f, rect.y + 8f, sun.xMin - 14f, rect.yMax - 6f);

            GUILayout.BeginArea(fields);
            EditorGUIUtility.labelWidth = 76f;

            GUILayout.FlexibleSpace();
            DrawProfile();
            m_Environment.timeOfDay = EditorGUILayout.Slider(Styles.Time, m_Environment.timeOfDay, 0f, 12f);
            GUILayout.FlexibleSpace();

            EditorGUIUtility.labelWidth = labelWidth;
            GUILayout.EndArea();
        }

        void DrawSun(Rect rect)
        {
            var centre = rect.center;
            var radius = rect.width * 0.5f;
            var dot = centre + new Vector2(m_Sun.x, -m_Sun.y) * radius;

            Handles.BeginGUI();
            Handles.color = new Color(0f, 0f, 0f, 0.45f);
            Handles.DrawSolidDisc(centre, Vector3.forward, radius);
            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            Handles.DrawWireDisc(centre, Vector3.forward, radius);
            Handles.DrawWireDisc(centre, Vector3.forward, radius * 0.5f);
            Handles.DrawLine(centre, dot);
            Handles.color = new Color(1f, 0.85f, 0.35f, 1f);
            Handles.DrawSolidDisc(dot, Vector3.forward, 5f);
            Handles.EndGUI();
        }

        void HandleSun(Rect rect)
        {
            var current = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive, rect);
            var inside = (current.mousePosition - rect.center).magnitude <= rect.width * 0.5f + 6f;

            switch (current.GetTypeForControl(control))
            {
                case EventType.MouseDown when current.button == 0 && inside:
                    GUIUtility.hotControl = control;
                    MoveSun(rect, current.mousePosition);
                    current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    MoveSun(rect, current.mousePosition);
                    current.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
        }

        void MoveSun(Rect rect, Vector2 mouse)
        {
            var spot = (mouse - rect.center) / (rect.width * 0.5f);

            m_Sun = Vector2.ClampMagnitude(new Vector2(spot.x, -spot.y), 1f);

            var rise = Mathf.Sqrt(Mathf.Max(1f - m_Sun.sqrMagnitude, 0f));
            m_Environment.sunDirection = new Vector3(m_Sun.x, m_Sun.y, -Mathf.Max(rise, 0.02f)).normalized;
            GUI.changed = true;
        }

        void DrawProfile()
        {
            if (m_Profiles == null)
                FindProfiles();

            var index = System.Array.IndexOf(m_Profiles, m_Environment.profile) + 1;
            var picked = EditorGUILayout.Popup(Styles.Environment, index, m_ProfileNames);

            if (picked != index)
                m_Environment.profile = picked > 0 ? m_Profiles[picked - 1] : null;
        }

        void FindProfiles()
        {
            var found = new List<HBStyleEnvironmentProfile>();

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(HBStyleEnvironmentProfile)))
            {
                var profile = AssetDatabase.LoadAssetAtPath<HBStyleEnvironmentProfile>(
                    AssetDatabase.GUIDToAssetPath(guid));

                if (profile != null)
                    found.Add(profile);
            }

            found.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            m_Profiles = found.ToArray();
            m_ProfileNames = new GUIContent[m_Profiles.Length + 1];
            m_ProfileNames[0] = Styles.Default;

            for (var i = 0; i < m_Profiles.Length; i++)
                m_ProfileNames[i + 1] = new GUIContent(m_Profiles[i].name, AssetDatabase.GetAssetPath(m_Profiles[i]));
        }

        void ForgetProfiles()
        {
            m_Profiles = null;
        }

        void HandleZoom(Rect rect, Rect panel)
        {
            var current = Event.current;

            if (current.type != EventType.ScrollWheel || !rect.Contains(current.mousePosition) ||
                panel.Contains(current.mousePosition))
                return;

            m_Distance = Mathf.Clamp(m_Distance * (1f + current.delta.y * 0.03f), 1.8f, 12f);
            current.Use();
            GUI.changed = true;
        }

        bool Prepare()
        {
            if (m_Utility != null && m_Material != null)
                return true;

            var shader = Shader.Find(LitShader);

            if (shader == null)
                return false;

            m_Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m_Material.SetColor("_BaseColor", Color.white);
            m_Material.SetFloat("_BaseMapEnabled", 1f);
            m_Material.SetFloat("_NormalMapEnabled", 1f);
            m_Material.SetFloat("_MaskMapEnabled", 1f);
            m_Material.SetFloat("_SpecularEnabled", 1f);
            m_Material.SetFloat("_BrushEnabled", 0f);
            m_Material.SetFloat("_ReceiveWeather", 0f);
            m_Material.SetFloat("_Smoothness", 1f);
            m_Material.SetFloat("_BumpScale", 1f);
            m_Material.EnableKeyword("_HB_BASE_MAP");
            m_Material.EnableKeyword("_NORMALMAP");
            m_Material.EnableKeyword("_HB_MASK_MAP");
            m_Material.EnableKeyword("_HB_SPECULAR");
            m_Material.DisableKeyword("_HB_BRUSH");

            m_Sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_Plane = Resources.GetBuiltinResource<Mesh>("New-Plane.fbx");

            m_Utility ??= new PreviewRenderUtility();

            var camera = m_Utility.camera;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            m_Utility.lights[1].intensity = 0f;
            m_Utility.ambientColor = new Color(0.22f, 0.24f, 0.28f);
            StylePreviewEnvironment.Attach(camera, m_Environment);
            return true;
        }

        internal void Release()
        {
            EditorApplication.projectChanged -= ForgetProfiles;

            if (m_Utility != null)
            {
                StylePreviewEnvironment.Detach(m_Utility.camera);
                m_Utility.Cleanup();
            }

            m_Utility = null;

            if (m_Material != null)
                Object.DestroyImmediate(m_Material);

            m_Material = null;
        }
    }
}
