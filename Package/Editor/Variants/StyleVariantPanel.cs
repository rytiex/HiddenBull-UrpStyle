using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleVariantPanel : EditorWindow
    {
        const string TabKey = "HiddenBull.UrpStyle.VariantPanel.Tab";
        const string LodCrossFade = "m_EnableLODCrossFade";
        const string Cookies = "m_SupportsLightCookies";
        const int ShownShaders = 40;

        static readonly string[] s_Tabs = { "Checks", "Variants" };

        enum State
        {
            Ok,
            Advice,
            Info
        }

        readonly struct Check
        {
            public readonly string label;
            public readonly string tooltip;
            public readonly State state;
            public readonly Action fix;

            public Check(string label, string tooltip, State state, Action fix)
            {
                this.label = label;
                this.tooltip = tooltip;
                this.state = state;
                this.fix = fix;
            }
        }

        readonly List<Check> m_Checks = new List<Check>();
        readonly HashSet<string> m_Open = new HashSet<string>();
        StyleVariantProject.Scan m_Scan;
        Vector2 m_Scroll;

        [MenuItem("Tools/HiddenBull/URP Style/Optimize Variants")]
        static void Open()
        {
            GetWindow<StyleVariantPanel>("HB Variants");
        }

        void OnEnable()
        {
            EditorApplication.delayCall += Rescan;
        }

        void Rescan()
        {
            if (this == null)
                return;

            m_Scan = StyleVariantProject.ScanProject();
            Repaint();
        }

        void OnGUI()
        {
            var data = StyleVariantData.Load();

            Gather(data);

            var tab = Mathf.Clamp(SessionState.GetInt(TabKey, 0), 0, s_Tabs.Length - 1);
            var next = GUILayout.Toolbar(tab, s_Tabs);

            if (next != tab)
                SessionState.SetInt(TabKey, next);

            EditorGUILayout.Space(4f);
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            if (next == 0)
                DrawChecks();
            else
                DrawVariants(data);

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        void DrawChecks()
        {
            EditorGUILayout.HelpBox("Each check below finds a shader keyword the project does not use. Unity " +
                                    "compiles every material once with and once without such a keyword, so one " +
                                    "unused keyword can double the variants and the build time. Fix turns it off.",
                                    MessageType.None);
            EditorGUILayout.Space(4f);

            foreach (var check in m_Checks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var icon = check.state == State.Ok ? "TestPassed"
                        : check.state == State.Advice ? "console.warnicon.sml" : "console.infoicon.sml";

                    GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20f), GUILayout.Height(18f));
                    var label = new GUIContent(check.label, check.tooltip);

                    EditorGUILayout.LabelField(label, EditorStyles.wordWrappedLabel);

                    using (new EditorGUI.DisabledScope(check.fix == null))
                    {
                        if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            check.fix();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                var files = m_Scan == null ? "not scanned yet" : $"{m_Scan.files} scenes and prefabs scanned";

                EditorGUILayout.LabelField(files, EditorStyles.miniLabel);

                if (GUILayout.Button("Scan Again", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    Rescan();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DrawVariants(StyleVariantData data)
        {
            if (data == null)
            {
                EditorGUILayout.HelpBox("No build has been recorded yet. Make a player build: the variants are " +
                                        "counted while Unity prepares the shaders. If a build reuses the shaders " +
                                        "of an earlier build, choose Clean Build.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox($"Build of {data.built} ({data.target}): {data.total:N0} variants in " +
                                    $"{data.shaders.Count} shaders. Open a shader to see which keywords multiply " +
                                    "its largest pass. Builds that reuse earlier shaders keep the last counts.",
                                    MessageType.None);
            EditorGUILayout.Space(4f);

            var shown = Mathf.Min(data.shaders.Count, ShownShaders);

            for (var i = 0; i < shown; i++)
            {
                var shader = data.shaders[i];
                var open = m_Open.Contains(shader.shader);
                var now = EditorGUILayout.Foldout(open, $"{shader.shader}   {shader.variants:N0}", true);

                if (now != open)
                {
                    if (now)
                        m_Open.Add(shader.shader);
                    else
                        m_Open.Remove(shader.shader);
                }

                if (!now)
                    continue;

                using (new EditorGUI.IndentLevelScope())
                {
                    var largest = shader.largest;

                    EditorGUILayout.LabelField($"{largest.pass} ({largest.stage}): {largest.variants.Count:N0}",
                                               EditorStyles.miniBoldLabel);

                    var multipliers = StyleVariantData.Multipliers(largest);

                    if (multipliers.Count == 0)
                        EditorGUILayout.LabelField("No keyword multiplies this pass.", EditorStyles.miniLabel);

                    foreach (var pair in multipliers)
                        EditorGUILayout.LabelField(pair.Key, $"×{pair.Value:0.##}");
                }
            }

            if (data.shaders.Count > shown)
                EditorGUILayout.LabelField($"{data.shaders.Count - shown} smaller shaders not shown.",
                                           EditorStyles.miniLabel);
        }

        void DrawFooter()
        {
            var advice = 0;

            foreach (var check in m_Checks)
            {
                if (check.state == State.Advice)
                    advice++;
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var icon = advice > 0 ? "console.warnicon.sml" : "TestPassed";
                var summary = advice > 0 ? $"{advice} unused keyword(s) still compiled." : "No unused keyword found.";

                GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20f), GUILayout.Height(18f));
                EditorGUILayout.LabelField(summary);
            }

            EditorGUILayout.Space(2f);
        }

        void Gather(StyleVariantData data)
        {
            m_Checks.Clear();
            AddAssetCheck(data, LodCrossFade, StyleVariantProject.LodCrossFadeKeyword, "LOD Cross Fade",
                          m_Scan?.fadingLodGroups ?? 0, "LOD Group(s) cross-fade",
                          "Counts LOD Groups whose Fade Mode is Cross Fade or SpeedTree in the build scenes and the " +
                          "project's prefabs.");
            AddAssetCheck(data, Cookies, StyleVariantProject.CookiesKeyword, "Light Cookies",
                          m_Scan?.cookieLights ?? 0, "light(s) use a cookie",
                          "Counts lights with a cookie texture in the build scenes and the project's prefabs. " +
                          "Cookies assigned from scripts at runtime are not seen.");
            AddDotsCheck(data);
            AddMixingCheck(data);
        }

        void AddAssetCheck(StyleVariantData data, string property, string keyword, string setting, int users,
                           string usersLabel, string tooltip)
        {
            if (!StyleVariantProject.AnyAsset(property))
            {
                m_Checks.Add(new Check($"{setting} is off on the URP assets.", tooltip, State.Ok, null));
                return;
            }

            if (m_Scan == null)
            {
                m_Checks.Add(new Check($"{setting}: scanning…", tooltip, State.Info, null));
                return;
            }

            if (!m_Scan.textAssets)
            {
                m_Checks.Add(new Check($"{setting}: the scan needs Asset Serialization set to Force Text.",
                                       tooltip, State.Info, null));
                return;
            }

            if (users > 0)
            {
                m_Checks.Add(new Check($"{users} {usersLabel}, so {setting} stays on.", tooltip, State.Ok, null));
                return;
            }

            m_Checks.Add(new Check($"Nothing uses {setting}, yet it is on{Cost(data, keyword)}. Fix turns it off " +
                                   "on every URP asset.", tooltip, State.Advice,
                                   () => StyleVariantProject.SetAll(property, false, $"Turn Off {setting}")));
        }

        void AddDotsCheck(StyleVariantData data)
        {
            const string tooltip = "Project Settings, Graphics, Shader Stripping, BatchRendererGroup Variants.";

            if (!StyleVariantProject.KeepsAllDotsVariants())
            {
                m_Checks.Add(new Check("BatchRendererGroup variants are kept only with Entities Graphics.", tooltip,
                                       State.Ok, null));
                return;
            }

            if (StyleVariantProject.UsesResidentDrawer() || StyleVariantProject.HasEntitiesGraphics())
            {
                m_Checks.Add(new Check("GPU Resident Drawer or Entities Graphics draws through BatchRendererGroup, " +
                                       "so its variants stay.", tooltip, State.Ok, null));
                return;
            }

            m_Checks.Add(new Check("BatchRendererGroup Variants is Keep All, but neither GPU Resident Drawer nor " +
                                   $"Entities Graphics is used{Cost(data, StyleVariantProject.DotsKeyword)}. Fix " +
                                   "sets it to Keep If Entities Graphics.", tooltip, State.Advice,
                                   StyleVariantProject.StripUnusedDotsVariants));
        }

        void AddMixingCheck(StyleVariantData data)
        {
            const string tooltip = "URP turns this keyword on only for Subtractive mixed lighting or when the " +
                                   "quality level's Shadowmask Mode is Shadowmask. Distance Shadowmask never uses it.";

            if (StyleVariantProject.MixingUnused())
            {
                m_Checks.Add(new Check("Subtractive and full Shadowmask lighting are not used, so their variants " +
                                       "are stripped automatically.", tooltip, State.Ok, null));
                return;
            }

            var reason = StyleVariantProject.AnySubtractiveBake()
                ? "a Lighting Settings asset bakes Subtractive"
                : "a quality level sets Shadowmask Mode to Shadowmask";

            m_Checks.Add(new Check($"Mixed lighting variants stay because {reason}" +
                                   $"{Cost(data, StyleVariantProject.MixingKeyword)}.", tooltip, State.Info, null));
        }

        static string Cost(StyleVariantData data, string keyword)
        {
            if (data == null)
                return string.Empty;

            var factor = data.Factor(keyword);

            return factor > 1.01f ? $" (×{factor:0.0} variants in the last build)" : string.Empty;
        }
    }
}
