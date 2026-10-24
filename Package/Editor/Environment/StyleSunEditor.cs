using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleSun))]
    [CanEditMultipleObjects]
    sealed class StyleSunEditor : UnityEditor.Editor
    {
        const float DialSize = 168f;
        const float DialStep = 1f / 60f;

        const string Hint =
            "This light is turned by Time of Day: toward the sun by day and toward the moon at night. " +
            "Rotating the light by hand is undone the next time it updates; change the hour instead.";

        static readonly string[] s_Compass = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        static readonly Color s_Day = new Color(0.98f, 0.78f, 0.38f, 0.35f);
        static readonly Color s_Night = new Color(0.22f, 0.27f, 0.5f, 0.55f);
        static readonly Color s_Ring = new Color(1f, 1f, 1f, 0.35f);
        static readonly Color s_Sun = new Color(1f, 0.82f, 0.35f, 1f);
        static readonly Color s_Moon = new Color(0.78f, 0.84f, 1f, 1f);

        static readonly Vector3[] s_Sector = new Vector3[26];

        static readonly (string label, float hour)[] s_Presets =
        {
            ("Dawn", 6f), ("Noon", 12f), ("Dusk", 18f), ("Midnight", 0f)
        };

        SerializedProperty m_TimeOfDay;
        SerializedProperty m_Heading;
        SerializedProperty m_Profile;

        GUIStyle m_Clock;
        GUIStyle m_Caption;

        void OnEnable()
        {
            m_TimeOfDay = serializedObject.FindProperty("m_TimeOfDay");
            m_Heading = serializedObject.FindProperty("m_Heading");
            m_Profile = serializedObject.FindProperty("m_Profile");
        }

        public override void OnInspectorGUI()
        {
            m_Clock ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            m_Caption ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Profile);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(Hint, MessageType.None);
            EditorGUILayout.Space();

            DrawDial();
            DrawTime();
            DrawPresets();

            EditorGUILayout.Space();

            DrawHeading();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawDial()
        {
            var row = GUILayoutUtility.GetRect(DialSize, DialSize, GUILayout.ExpandWidth(true));
            var rect = new Rect(row.center.x - DialSize * 0.5f, row.y, DialSize, DialSize);

            var centre = rect.center;
            var radius = DialSize * 0.5f - 6f;
            var hour = m_TimeOfDay.floatValue;
            var mixed = m_TimeOfDay.hasMultipleDifferentValues;

            HandleDrag(rect, centre);

            if (Event.current.type != EventType.Repaint)
                return;

            FillSector(centre, radius, 6f, 18f, s_Day);
            FillSector(centre, radius, 18f, 30f, s_Night);

            Handles.color = s_Ring;
            Handles.DrawWireDisc(centre, Vector3.forward, radius);

            for (var tick = 0; tick < 24; tick++)
            {
                var major = tick % 6 == 0;
                var direction = HourDirection(tick);
                var inner = major ? radius - 10f : radius - 5f;

                Handles.DrawAAPolyLine(major ? 2f : 1f,
                    centre + direction * inner, centre + direction * radius);

                if (major)
                {
                    var label = centre + direction * (radius - 20f);
                    GUI.Label(new Rect(label.x - 12f, label.y - 8f, 24f, 16f), tick.ToString("00"), m_Caption);
                }
            }

            if (mixed)
                return;

            var night = hour < 6f || hour > 18f;
            var tip = centre + HourDirection(hour) * (radius - 4f);

            Handles.color = night ? s_Moon : s_Sun;
            Handles.DrawAAPolyLine(3f, centre, tip);
            Handles.DrawSolidDisc(tip, Vector3.forward, 7f);
            Handles.DrawSolidDisc(centre, Vector3.forward, 3f);
        }

        void HandleDrag(Rect rect, Vector2 centre)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive, rect);
            var current = Event.current;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown when current.button == 0 && rect.Contains(current.mousePosition):
                    GUIUtility.hotControl = id;
                    SetHour(current.mousePosition - centre);
                    current.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    SetHour(current.mousePosition - centre);
                    current.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
        }

        void SetHour(Vector2 offset)
        {
            if (offset.sqrMagnitude < 1f)
                return;

            var angle = Mathf.Atan2(offset.x, -offset.y);
            var hour = Mathf.Repeat(12f + angle / (2f * Mathf.PI) * 24f, 24f);

            m_TimeOfDay.floatValue = Mathf.Round(hour / DialStep) * DialStep;
            GUI.changed = true;
        }

        void DrawTime()
        {
            var mixed = m_TimeOfDay.hasMultipleDifferentValues;
            var hour = m_TimeOfDay.floatValue;

            var night = hour < 6f || hour > 18f;

            EditorGUILayout.LabelField(mixed ? "—" : Format(hour), m_Clock, GUILayout.Height(28f));
            EditorGUILayout.LabelField(mixed ? string.Empty : night ? "Night — moonlight" : "Day — sunlight",
                m_Caption);

            EditorGUILayout.Slider(m_TimeOfDay, 0f, 24f, new GUIContent("Time of Day", m_TimeOfDay.tooltip));
        }

        void DrawPresets()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var (label, hour) in s_Presets)
                {
                    if (GUILayout.Button($"{label}  {Format(hour)}", EditorStyles.miniButton))
                        m_TimeOfDay.floatValue = hour;
                }
            }
        }

        void DrawHeading()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Slider(m_Heading, 0f, 360f, new GUIContent("Heading", m_Heading.tooltip));

                var point = m_Heading.hasMultipleDifferentValues
                    ? "—"
                    : "rises " + s_Compass[Mathf.RoundToInt(Mathf.Repeat(m_Heading.floatValue + 180f, 360f) / 45f) % 8];

                GUILayout.Label(point, EditorStyles.miniLabel, GUILayout.Width(52f));
            }
        }

        static string Format(float hour)
        {
            var minutes = Mathf.RoundToInt(Mathf.Repeat(hour, 24f) * 60f) % (24 * 60);

            return $"{minutes / 60:00}:{minutes % 60:00}";
        }

        static Vector2 HourDirection(float hour)
        {
            var angle = (hour - 12f) / 24f * 2f * Mathf.PI;

            return new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
        }

        static void FillSector(Vector2 centre, float radius, float from, float to, Color colour)
        {
            var last = s_Sector.Length - 1;

            s_Sector[0] = centre;

            for (var i = 1; i <= last; i++)
            {
                var hour = Mathf.Lerp(from, to, (i - 1f) / (last - 1f));
                s_Sector[i] = centre + HourDirection(hour) * radius;
            }

            Handles.color = colour;
            Handles.DrawAAConvexPolygon(s_Sector);
        }
    }
}
