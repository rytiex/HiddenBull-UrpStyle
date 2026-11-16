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

    enum StyleFieldView
    {
        Field,
        Single
    }

    sealed class StyleStudioPreview
    {
        const string LitShader = "HB URP Style/Lit";
        const string SkyShader = "HB URP Style/Sky";
        const float BarHeight = 20f;
        const float BarWidth = 240f;
        const float PanelWidth = 330f;
        const float PanelHeight = 100f;
        const float SunSize = 74f;
        const float PanelMargin = 8f;
        const float FlowerStem = 0.07f;
        const float FlowerTurn = 0.55f;

        static readonly int s_BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int s_BumpMap = Shader.PropertyToID("_BumpMap");
        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
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

            internal static readonly GUIContent[] FieldViews =
            {
                new GUIContent("Field", "The grass on a small HB terrain. Drag to turn, scroll to zoom."),
                new GUIContent("Single", "One grass plant on its own, coloured like the ground under it, or one " +
                                         "flower while the Flower tab is open. Drag to turn, scroll to zoom.")
            };

            internal static readonly GUIContent Environment =new GUIContent("Environment",
                "Environment profile the preview is lit with. Only the preview changes, not the scene.");

            internal static readonly GUIContent Time = new GUIContent("Time",
                "Hour of the day, 0 to 12. Changes the light's colour and strength like the Environment Controller.");

            internal static readonly GUIContent Sun = new GUIContent("Light",
                "Drag the dot to move the light. In the middle it shines from the camera, at the rim it grazes " +
                "the surface from that side.");

            internal static readonly GUIContent SkyTime = new GUIContent("Time",
                "Hour of the day, 0 to 12. Raises the sun like the Environment Controller: 6 is sunrise, 12 is noon.");

            internal static readonly GUIContent Heading = new GUIContent("Angle",
                "Compass direction the sun comes from, in degrees, like the Environment Controller's heading.");

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
        internal StyleFieldView fieldView = StyleFieldView.Field;
        internal bool singleFlower;
        float m_FieldDistance = 18f;
        float m_SingleDistance = 2.5f;
        Vector2 m_Orbit = new Vector2(35f, 24f);
        Material m_Card;
        Material m_Stem;
        Material m_Brush;
        Material m_Sky;
        Skybox m_Skybox;
        Vector2 m_SkyLook = new Vector2(0f, 25f);
        float m_SkyField = 60f;
        float m_Heading;
        Mesh m_Cross;
        Mesh m_Quad;

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

        internal void DrawBrush(Rect rect, Texture atlas, Color colour)
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
            {
                m_Brush.SetColor(s_BaseColor, colour);
                m_Environment.brushAtlas = atlas;

                try
                {
                    Draw(rect, m_Brush);
                }
                finally
                {
                    m_Environment.brushAtlas = null;
                }
            }

            DrawPanel(panel);
        }

        internal void DrawSky(Rect rect, Texture atlas, float coverage, float softness)
        {
            if (!Prepare() || !PrepareSky())
            {
                EditorGUI.HelpBox(rect, $"The shader '{SkyShader}' was not found.", MessageType.Warning);
                return;
            }

            var width = Mathf.Min(PanelWidth, rect.width - PanelMargin * 2f);
            var panel = new Rect(rect.xMax - width - PanelMargin, rect.yMax - PanelHeight - PanelMargin, width,
                                 PanelHeight);

            HandleSkyCamera(rect, panel);

            if (Event.current.type == EventType.Repaint)
                RenderSky(rect, atlas, coverage, softness);

            DrawPanel(panel, true);
        }

        bool PrepareSky()
        {
            if (m_Sky == null)
            {
                var shader = Shader.Find(SkyShader);

                if (shader == null)
                    return false;

                m_Sky = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (m_Skybox == null)
            {
                var holder = m_Utility.camera.gameObject;

                m_Skybox = holder.TryGetComponent<Skybox>(out var found) ? found : holder.AddComponent<Skybox>();
                m_Skybox.enabled = false;
            }

            m_Skybox.material = m_Sky;
            return true;
        }

        void RenderSky(Rect rect, Texture atlas, float coverage, float softness)
        {
            var turn = Quaternion.Euler(0f, m_SkyLook.x, 0f);
            var look = turn * Quaternion.Euler(-m_SkyLook.y, 0f, 0f);
            var camera = m_Utility.camera;
            var sunAngle = HBStyleEnvironmentController.SunAngle(m_Environment.timeOfDay);

            camera.fieldOfView = m_SkyField;
            camera.farClipPlane = 50f;
            camera.transform.SetPositionAndRotation(Vector3.zero, look);
            m_Environment.sunDirection = -(Quaternion.Euler(sunAngle, m_Heading, 0f) * Vector3.forward);
            m_Environment.cloudAtlas = atlas;
            m_Environment.cloudCoverage = coverage;
            m_Environment.cloudSoftness = softness;

            m_Skybox.enabled = true;
            camera.clearFlags = CameraClearFlags.Skybox;

            try
            {
                m_Utility.BeginPreview(rect, GUIStyle.none);
                camera.Render();
                GUI.DrawTexture(rect, m_Utility.EndPreview(), ScaleMode.StretchToFill, false);
            }
            finally
            {
                m_Environment.cloudAtlas = null;
                m_Skybox.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.fieldOfView = 30f;
            }
        }

        void HandleSkyCamera(Rect rect, Rect panel)
        {
            var current = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive, rect);
            var over = rect.Contains(current.mousePosition) && !panel.Contains(current.mousePosition);

            switch (current.GetTypeForControl(control))
            {
                case EventType.ScrollWheel when over:
                    m_SkyField = Mathf.Clamp(m_SkyField * (1f + current.delta.y * 0.03f), 25f, 100f);
                    current.Use();
                    GUI.changed = true;
                    break;
                case EventType.MouseDown when current.button == 0 && over:
                    GUIUtility.hotControl = control;
                    current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    m_SkyLook.x -= current.delta.x * 0.3f;
                    m_SkyLook.y = Mathf.Clamp(m_SkyLook.y + current.delta.y * 0.3f, 0f, 89f);
                    current.Use();
                    GUI.changed = true;
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
        }

        internal Rect DrawFieldBar(Rect rect)
        {
            var width = Mathf.Min(rect.width, BarWidth * 2f / 3f);
            var bar = new Rect(rect.center.x - width * 0.5f, rect.y, width, BarHeight);

            fieldView = (StyleFieldView)GUI.Toolbar(bar, (int)fieldView, Styles.FieldViews, EditorStyles.miniButton);
            return Rect.MinMaxRect(rect.xMin, rect.yMin + BarHeight + 4f, rect.xMax, rect.yMax);
        }

        internal void DrawField(Rect rect, StyleStudioField field, Texture card, Texture flower, int variants)
        {
            if (!Prepare())
            {
                EditorGUI.HelpBox(rect, $"The shader '{LitShader}' was not found.", MessageType.Warning);
                return;
            }

            field.Build(m_Utility);

            var width = Mathf.Min(PanelWidth, rect.width - PanelMargin * 2f);
            var panel = new Rect(rect.xMax - width - PanelMargin, rect.yMax - PanelHeight - PanelMargin, width,
                                 PanelHeight);

            HandleFieldCamera(rect, panel);

            if (Event.current.type == EventType.Repaint)
                RenderField(rect, field, card, flower, variants);

            DrawPanel(panel);
        }

        void RenderField(Rect rect, StyleStudioField field, Texture card, Texture flower, int variants)
        {
            var turn = Quaternion.Euler(0f, m_Orbit.x, 0f);
            var look = turn * Quaternion.Euler(m_Orbit.y, 0f, 0f);
            var camera = m_Utility.camera;
            var rise = Mathf.Sqrt(Mathf.Max(1f - m_Sun.sqrMagnitude, 0f));
            var single = fieldView == StyleFieldView.Single && field.layer != null;
            var bloom = single && singleFlower && flower != null;
            var height = single ? field.layer.grassHeight : 0f;
            var size = bloom ? field.layer.flowerSize : 0f;
            var target = single ? new Vector3(0f, bloom ? height : height * 0.5f, 0f) : field.centre;
            var reach = bloom ? Mathf.Max(height * 0.5f, size) : Mathf.Max(height, field.layer?.grassCardWidth ?? 0f);
            var distance = single ? m_SingleDistance * reach : m_FieldDistance;

            camera.farClipPlane = single ? 50f : 200f;
            camera.transform.SetPositionAndRotation(target - look * Vector3.forward * distance, look);
            m_Environment.sunDirection = (turn * new Vector3(m_Sun.x, Mathf.Max(rise, 0.05f), m_Sun.y)).normalized;
            m_Environment.KeyLight(out var colour, out var intensity);

            var light = m_Utility.lights[0];
            light.transform.rotation = Quaternion.LookRotation(-m_Environment.sunDirection);
            light.color = colour;
            light.intensity = intensity;

            m_Utility.BeginPreview(rect, GUIStyle.none);

            if (single)
            {
                var layer = field.layer;
                var tint = field.groundColour * layer.tint.linear;
                var ground = new Color(tint.r, tint.g, tint.b, 1f).gamma;

                if (bloom)
                {
                    var stem = size * FlowerStem;
                    var head = new Vector3(0f, height, 0f);
                    var toCamera = (camera.transform.position - head).normalized;
                    var face = (Vector3.up + toCamera * FlowerTurn).normalized;
                    var flat = new Vector3(toCamera.x, 0f, toCamera.z);
                    var facing = Quaternion.LookRotation(-face, flat.sqrMagnitude > 1e-6f ? flat : Vector3.forward);

                    m_Stem.SetColor(s_BaseColor, ground);
                    m_Utility.DrawMesh(Cross(), Matrix4x4.Scale(new Vector3(stem, height, stem)), m_Stem, 0);
                    m_Card.SetTexture(s_BaseMap, flower);
                    m_Card.SetTextureScale(s_BaseMap, new Vector2(1f / Mathf.Max(variants, 1), 1f));
                    m_Card.SetColor(s_BaseColor, Color.white);
                    m_Utility.DrawMesh(Quad(), Matrix4x4.TRS(head, facing, Vector3.one * size), m_Card, 0);
                }
                else
                {
                    m_Card.SetTexture(s_BaseMap, card);
                    m_Card.SetTextureScale(s_BaseMap, Vector2.one);
                    m_Card.SetColor(s_BaseColor, ground);
                    m_Utility.DrawMesh(Cross(), Matrix4x4.Scale(new Vector3(layer.grassCardWidth, height,
                                                                            layer.grassCardWidth)), m_Card, 0);
                }
            }

            camera.Render();
            GUI.DrawTexture(rect, m_Utility.EndPreview(), ScaleMode.StretchToFill, false);
        }

        Mesh Cross()
        {
            if (m_Cross != null)
                return m_Cross;

            var vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f),
                new Vector3(0.5f, 1f, 0f), new Vector3(0f, 0f, -0.5f), new Vector3(0f, 0f, 0.5f),
                new Vector3(0f, 1f, -0.5f), new Vector3(0f, 1f, 0.5f)
            };

            var uvs = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)
            };

            var normals = new Vector3[vertices.Length];

            for (var i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;

            m_Cross = new Mesh
            {
                name = "HB Studio Grass Cross",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                uv = uvs,
                normals = normals,
                triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7, 0, 1, 2, 1, 3, 2, 4, 5, 6, 5, 7, 6 }
            };

            return m_Cross;
        }

        Mesh Quad()
        {
            if (m_Quad != null)
                return m_Quad;

            m_Quad = new Mesh
            {
                name = "HB Studio Flower Head",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                triangles = new[] { 0, 2, 1, 1, 2, 3, 0, 1, 2, 1, 3, 2 }
            };

            return m_Quad;
        }

        void HandleFieldCamera(Rect rect, Rect panel)
        {
            var current = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive, rect);
            var over = rect.Contains(current.mousePosition) && !panel.Contains(current.mousePosition);

            switch (current.GetTypeForControl(control))
            {
                case EventType.ScrollWheel when over:
                    var zoom = 1f + current.delta.y * 0.03f;

                    if (fieldView == StyleFieldView.Single)
                        m_SingleDistance = Mathf.Clamp(m_SingleDistance * zoom, 1f, 8f);
                    else
                        m_FieldDistance = Mathf.Clamp(m_FieldDistance * zoom, 3f, 40f);

                    current.Use();
                    GUI.changed = true;
                    break;
                case EventType.MouseDown when current.button == 0 && over:
                    GUIUtility.hotControl = control;
                    current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    m_Orbit.x += current.delta.x * 0.4f;
                    m_Orbit.y = Mathf.Clamp(m_Orbit.y + current.delta.y * 0.3f, 5f, 85f);
                    current.Use();
                    GUI.changed = true;
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
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
            Draw(rect, m_Material);
        }

        void Draw(Rect rect, Material material)
        {
            var sphere = view == StyleStudioView.Sphere;
            var camera = m_Utility.camera;


            camera.farClipPlane = 50f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -m_Distance), Quaternion.identity);

            m_Environment.KeyLight(out var colour, out var intensity);

            var light = m_Utility.lights[0];
            light.transform.rotation = Quaternion.LookRotation(-m_Environment.sunDirection);
            light.color = colour;
            light.intensity = intensity;

            m_Utility.BeginPreview(rect, GUIStyle.none);

            if (sphere)
            {
                m_Utility.DrawMesh(m_Sphere, Matrix4x4.identity, material, 0);
            }
            else
            {
                var turn = Quaternion.Euler(-55f, 0f, 0f);
                m_Utility.DrawMesh(m_Plane, Matrix4x4.TRS(Vector3.zero, turn, Vector3.one * 0.2f), material, 0);
            }

            camera.Render();
            GUI.DrawTexture(rect, m_Utility.EndPreview(), ScaleMode.StretchToFill, false);
        }

        void DrawPanel(Rect rect, bool sky = false)
        {
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 0.85f));

            if (sky)
            {
                DrawSkyPanel(rect);
                return;
            }

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

        void DrawSkyPanel(Rect rect)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            var fields = Rect.MinMaxRect(rect.x + 6f, rect.y + 6f, rect.xMax - 8f, rect.yMax - 6f);

            GUILayout.BeginArea(fields);
            EditorGUIUtility.labelWidth = 76f;

            GUILayout.FlexibleSpace();
            DrawProfile();
            m_Environment.timeOfDay = EditorGUILayout.Slider(Styles.SkyTime, m_Environment.timeOfDay, 0f, 12f);
            m_Heading = EditorGUILayout.Slider(Styles.Heading, m_Heading, 0f, 360f);
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
            if (m_Utility != null && m_Material != null && m_Card != null && m_Stem != null && m_Brush != null)
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

            m_Card = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m_Card.SetFloat("_BaseMapEnabled", 1f);
            m_Card.SetFloat("_AlphaClipEnabled", 1f);
            m_Card.SetFloat("_Cutoff", 0.5f);
            m_Card.SetFloat("_Cull", 2f);
            m_Card.SetFloat("_Smoothness", 0.2f);
            m_Card.SetFloat("_BrushEnabled", 0f);
            m_Card.SetFloat("_ReceiveWeather", 0f);
            m_Card.EnableKeyword("_HB_BASE_MAP");
            m_Card.EnableKeyword("_ALPHATEST_ON");
            m_Card.DisableKeyword("_HB_BRUSH");
            m_Stem = new Material(m_Card) { hideFlags = HideFlags.HideAndDontSave };
            m_Stem.SetTexture(s_BaseMap, Texture2D.whiteTexture);

            m_Brush = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m_Brush.SetFloat("_BrushEnabled", 1f);
            m_Brush.SetFloat("_ReceiveWeather", 0f);
            m_Brush.SetFloat("_Smoothness", 0.25f);
            m_Brush.EnableKeyword("_HB_BRUSH");

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

            if (m_Brush != null)
                Object.DestroyImmediate(m_Brush);

            m_Brush = null;

            if (m_Sky != null)
                Object.DestroyImmediate(m_Sky);

            m_Sky = null;
            m_Skybox = null;

            if (m_Card != null)
                Object.DestroyImmediate(m_Card);

            if (m_Stem != null)
                Object.DestroyImmediate(m_Stem);

            if (m_Cross != null)
                Object.DestroyImmediate(m_Cross);

            if (m_Quad != null)
                Object.DestroyImmediate(m_Quad);

            m_Card = null;
            m_Stem = null;
            m_Quad = null;
            m_Cross = null;
        }
    }
}
