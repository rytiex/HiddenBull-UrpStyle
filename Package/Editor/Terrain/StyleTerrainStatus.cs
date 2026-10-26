using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainStatus
    {
        const float Dot = 10f;

        static readonly Color s_Good = new Color(0.35f, 0.8f, 0.4f, 1f);
        static readonly Color s_Pending = new Color(1f, 0.7f, 0.2f, 1f);
        static readonly Color s_Idle = new Color(0.55f, 0.55f, 0.55f, 1f);

        static readonly GUIContent s_PreviewLabel = new GUIContent("Preview",
            "Shows the layer rules on the terrain live while this inspector is open, also while sculpting, " +
            "without changing the terrain data. Bake keeps exactly what the preview shows.");

        static readonly GUIContent s_BakeLabel = new GUIContent("Bake",
            "Writes the layers into the terrain data. Until then the terrain keeps its last bake outside the " +
            "preview, in builds and for the layer queries.");

        internal static bool Draw(HBStyleTerrainData data, bool preview)
        {
            var state = StyleTerrainBakeService.State(data);
            string text;
            Color colour;

            switch (state)
            {
                case StyleTerrainBakeState.NoHeights:
                    text = "No heights yet";
                    colour = s_Idle;
                    break;

                case StyleTerrainBakeState.NoLayers:
                    text = "No layers";
                    colour = s_Idle;
                    break;

                case StyleTerrainBakeState.UpToDate:
                    text = "Baked, up to date";
                    colour = s_Good;
                    break;

                default:
                    text = preview ? "Previewing changes, not baked yet" : "Needs bake";
                    colour = s_Pending;
                    break;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var rect = GUILayoutUtility.GetRect(Dot + 6f, 24f, GUILayout.Width(Dot + 6f));

                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(rect.x + 3f, rect.center.y - Dot * 0.5f, Dot, Dot), colour);

                EditorGUILayout.LabelField(text, EditorStyles.boldLabel, GUILayout.Height(24f));

                preview = GUILayout.Toggle(preview, s_PreviewLabel, "Button", GUILayout.Width(72f),
                                           GUILayout.Height(24f));

                using (new EditorGUI.DisabledScope(state == StyleTerrainBakeState.NoHeights ||
                                                   state == StyleTerrainBakeState.UpToDate))
                {
                    if (GUILayout.Button(s_BakeLabel, GUILayout.Width(72f), GUILayout.Height(24f)))
                        EditorApplication.delayCall += () => Bake(data);
                }
            }

            return preview;
        }

        static void Bake(HBStyleTerrainData data)
        {
            if (data == null)
                return;

            StyleTerrainBakeService.Run(data);
            StyleTerrainPreviewer.Restart(data);
        }
    }
}
