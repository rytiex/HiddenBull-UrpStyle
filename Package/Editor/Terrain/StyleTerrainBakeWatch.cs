using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleTerrainBakeWatch
    {
        const string AutoKey = "HiddenBull.UrpStyle.Terrain.AutoBake";
        const double Interval = 0.5;
        const double Settle = 1.5;
        const float BannerWidth = 360f;
        const float BannerHeight = 30f;
        const float BannerTop = 6f;
        const float Stripe = 4f;

        static readonly Color s_Back = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        static readonly Color s_Warn = new Color(1f, 0.7f, 0.2f, 1f);

        static readonly GUIContent s_BakeLabel = new GUIContent("Bake", "Writes the layers into the terrain data.");

        static readonly GUIContent s_AutoLabel = new GUIContent("Auto Bake",
            "Bakes by itself a moment after the last change, so the terrain data always matches what you see.");

        static readonly List<HBStyleTerrainData> s_Pending = new List<HBStyleTerrainData>();
        static readonly Dictionary<HBStyleTerrainData, Entry> s_Entries = new Dictionary<HBStyleTerrainData, Entry>();

        static double s_Next;
        static string s_Message;

        sealed class Entry
        {
            internal string key;
            internal double changed;
            internal string baked;
        }

        static StyleTerrainBakeWatch()
        {
            EditorApplication.update += Update;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        internal static bool autoBake
        {
            get => EditorPrefs.GetBool(AutoKey, false);
            set
            {
                if (value == autoBake)
                    return;

                EditorPrefs.SetBool(AutoKey, value);
                s_Next = 0.0;
                SceneView.RepaintAll();
            }
        }

        static void Update()
        {
            var now = EditorApplication.timeSinceStartup;

            if (now < s_Next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            s_Next = now + Interval;

            var count = s_Pending.Count;

            s_Pending.Clear();

            foreach (var terrain in HBStyleTerrain.active)
            {
                var data = terrain != null ? terrain.data : null;

                if (data == null || s_Pending.Contains(data) ||
                    StyleTerrainBakeService.State(data) != StyleTerrainBakeState.NeedsBake)
                    continue;

                s_Pending.Add(data);
                Watch(data, now);
            }

            Message();

            if (autoBake && GUIUtility.hotControl == 0)
            {
                foreach (var data in s_Pending)
                {
                    var entry = s_Entries[data];

                    if (now - entry.changed < Settle || entry.baked == entry.key)
                        continue;

                    entry.baked = entry.key;
                    Bake(data);
                    s_Next = 0.0;
                    return;
                }
            }

            if (count != s_Pending.Count)
                SceneView.RepaintAll();
        }

        static void Watch(HBStyleTerrainData data, double now)
        {
            var key = StyleTerrainBakeService.Key(data);

            if (!s_Entries.TryGetValue(data, out var entry))
                s_Entries[data] = entry = new Entry { changed = now };

            if (entry.key == key)
                return;

            entry.key = key;
            entry.changed = now;
        }

        static void Message()
        {
            if (s_Pending.Count == 0)
            {
                s_Message = null;
                return;
            }

            s_Message = s_Pending.Count == 1 ? $"{s_Pending[0].name} needs Bake"
                                             : $"{s_Pending.Count} terrains need Bake";
        }

        internal static void Bake(HBStyleTerrainData data)
        {
            if (data == null)
                return;

            StyleTerrainBakeService.Run(data);
            StyleTerrainPreviewer.Restart(data);
            SceneView.RepaintAll();
        }

        static void BakeAll()
        {
            foreach (var data in s_Pending.ToArray())
                Bake(data);

            s_Next = 0.0;
        }

        static void OnSceneGUI(SceneView view)
        {
            if (s_Message == null || autoBake)
                return;

            var rect = new Rect((view.position.width - BannerWidth) * 0.5f, BannerTop, BannerWidth, BannerHeight);

            Handles.BeginGUI();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, s_Back);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, Stripe, rect.height), s_Warn);
            }

            GUILayout.BeginArea(new Rect(rect.x + Stripe * 2f, rect.y + 4f, rect.width - Stripe * 3f,
                                         rect.height - 8f));

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(s_Message, EditorStyles.whiteBoldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(s_BakeLabel, EditorStyles.miniButtonLeft, GUILayout.Width(56f)))
                    EditorApplication.delayCall += BakeAll;

                autoBake = GUILayout.Toggle(autoBake, s_AutoLabel, EditorStyles.miniButtonRight,
                                            GUILayout.Width(76f));
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
