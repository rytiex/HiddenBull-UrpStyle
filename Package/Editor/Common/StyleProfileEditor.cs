using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleProfilePage
    {
        internal readonly string title;
        internal readonly string[] entries;
        internal readonly StyleProfilePage[] pages;
        internal readonly string[] names;

        internal StyleProfilePage(string title, string[] entries)
        {
            this.title = title;
            this.entries = entries;
        }

        internal StyleProfilePage(string title, StyleProfilePage[] pages)
        {
            this.title = title;
            this.pages = pages;
            names = Names(pages);
        }

        internal static string[] Names(StyleProfilePage[] pages)
        {
            var names = new string[pages.Length];

            for (var i = 0; i < pages.Length; i++)
                names[i] = pages[i].title;

            return names;
        }

        internal static string[] Fields(string root, params string[] fields)
        {
            var entries = new string[fields.Length];

            for (var i = 0; i < fields.Length; i++)
                entries[i] = fields[i][0] == '#' ? fields[i] : root + "." + fields[i];

            return entries;
        }

        internal static string[] Join(params string[][] parts)
        {
            var entries = new List<string>();

            foreach (var part in parts)
                entries.AddRange(part);

            return entries.ToArray();
        }
    }

    abstract class StyleProfileEditor : UnityEditor.Editor
    {
        const string TabPrefix = "HiddenBull.UrpStyle.Tab.";
        const float TabWidth = 84f;

        protected void DrawSection(string property, string title, IReadOnlyDictionary<string, string> groups = null)
        {
            var section = serializedObject.FindProperty(property);

            if (section == null)
                return;

            var expanded = StyleSectionHeader.Draw(new GUIContent(title), target.GetType().Name + "." + property,
                                                   () => ResetSection(property));

            if (expanded)
            {
                var child = section.Copy();
                var end = section.GetEndProperty();
                var enter = true;

                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;

                    if (groups != null && groups.TryGetValue(child.name, out var group))
                    {
                        EditorGUILayout.Space(2f);
                        EditorGUILayout.LabelField(group, EditorStyles.miniBoldLabel);
                    }

                    EditorGUILayout.PropertyField(child, true);
                }
            }

            StyleSectionHeader.End(expanded);
        }

        protected void DrawPages(StyleProfilePage[] pages, string[] names)
        {
            var key = TabPrefix + target.GetType().Name;
            var page = pages[DrawTabs(names, key)];

            if (page.pages != null)
            {
                EditorGUILayout.Space(2f);
                page = page.pages[DrawTabs(page.names, key + "." + page.title)];
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(page.title, EditorStyles.boldLabel);

                if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.Width(52f)))
                    ResetPage(page);
            }

            foreach (var entry in page.entries)
            {
                if (entry[0] == '#')
                {
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField(entry.Substring(1), EditorStyles.miniBoldLabel);
                    continue;
                }

                var property = serializedObject.FindProperty(entry);

                if (property != null)
                    EditorGUILayout.PropertyField(property, true);
            }
        }

        static int DrawTabs(string[] names, string key)
        {
            var selected = Mathf.Clamp(SessionState.GetInt(key, 0), 0, names.Length - 1);
            var fit = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 24f) / TabWidth);
            var columns = fit >= names.Length ? names.Length : Mathf.CeilToInt(names.Length * 0.5f);
            var next = GUILayout.SelectionGrid(selected, names, Mathf.Max(1, columns));

            if (next != selected)
            {
                SessionState.SetInt(key, next);
                GUI.FocusControl(null);
            }

            return next;
        }

        void ResetPage(StyleProfilePage page)
        {
            var defaults = CreateInstance(target.GetType());

            try
            {
                using var source = new SerializedObject(defaults);

                serializedObject.Update();

                foreach (var entry in page.entries)
                {
                    if (entry[0] == '#')
                        continue;

                    var property = source.FindProperty(entry);

                    if (property != null)
                        serializedObject.CopyFromSerializedProperty(property);
                }

                serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                DestroyImmediate(defaults);
            }
        }

        void ResetSection(string property)
        {
            var defaults = CreateInstance(target.GetType());

            try
            {
                using var owner = new SerializedObject(defaults);
                var source = owner.FindProperty(property);

                serializedObject.Update();
                serializedObject.CopyFromSerializedProperty(source);
                serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                DestroyImmediate(defaults);
            }
        }
    }

    [CustomEditor(typeof(HBStyleEnvironmentProfile))]
    [CanEditMultipleObjects]
    sealed class StyleEnvironmentProfileEditor : StyleProfileEditor
    {
        static readonly StyleProfilePage[] s_Pages =
        {
            new StyleProfilePage("Sky", StyleProfilePage.Fields("m_Sky",
                "#Sky", "m_DaySky", "m_DuskSky", "m_NightSky", "m_SkyIntensity", "m_SunsetFocus",
                "#Brush", "m_SkyBrush", "m_SkyPaint", "m_SkyBrushScale", "m_SkyBrushSmoothness")),
            new StyleProfilePage("Light", StyleProfilePage.Fields("m_Sky",
                "#Ambient", "m_DayAmbient", "m_DuskAmbient", "m_NightAmbient", "m_AmbientIntensity",
                "m_AmbientLightBias",
                "#Key Light", "m_SkyLight", "m_SkyLightIntensity", "m_MoonLight",
                "#Day and Night", "m_DuskElevation", "m_NightElevation")),
            new StyleProfilePage("Sun & Moon", StyleProfilePage.Join(
                StyleProfilePage.Fields("m_Sky", "#Sun", "m_SunDiscColor"),
                StyleProfilePage.Fields("m_Celestial", "m_SunIntensity", "m_SunDuskIntensity", "m_SunSize",
                                        "m_SunDuskSize", "m_SunGlow", "m_SunBrush", "m_SunRays"),
                StyleProfilePage.Fields("m_Sky", "#Moon", "m_MoonDiscColor"),
                StyleProfilePage.Fields("m_Celestial", "m_MoonIntensity", "m_MoonSize", "m_MoonGlow"))),
            new StyleProfilePage("Night Sky", StyleProfilePage.Fields("m_Celestial",
                "#Stars", "m_Stars", "m_StarTwinkle", "m_StarColor", "m_StarSizeVariation",
                "#Aurora", "m_Aurora", "m_AuroraColor", "m_AuroraHeading", "m_AuroraCount", "m_AuroraScale",
                "m_AuroraSpeed", "m_AuroraHeight")),
            new StyleProfilePage("Clouds", StyleProfilePage.Fields("m_Clouds",
                "#Shape", "m_Atlas", "m_Coverage", "m_Tint", "m_TintShade", "m_Scale", "m_Softness",
                "#Depth", "m_Steps", "m_StepSpacing", "m_StepShading", "m_StepTaper",
                "#Light", "m_SilverLining", "m_SunThrough", "m_Powder")),
            new StyleProfilePage("Brush", StyleProfilePage.Fields("m_Brush",
                "m_Atlas", "m_WorldSizePerTile", "m_FadeStart", "m_FadeEnd", "m_Randomize", "m_GrowWithDistance"))
        };

        static readonly string[] s_Names = StyleProfilePage.Names(s_Pages);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var atmosphere = serializedObject.FindProperty("m_AtmosphereProfile");

            EditorGUILayout.PropertyField(atmosphere);

            if (!atmosphere.hasMultipleDifferentValues && atmosphere.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No Atmosphere Profile: there is no fog, and rain and snow do not " +
                                        "close the sky.", MessageType.Info);

            EditorGUILayout.Space(4f);
            DrawPages(s_Pages, s_Names);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(HBStyleAtmosphereProfile))]
    [CanEditMultipleObjects]
    sealed class StyleAtmosphereProfileEditor : StyleProfileEditor
    {
        static readonly StyleProfilePage[] s_Pages =
        {
            new StyleProfilePage("Fog", new[]
            {
                new StyleProfilePage("Clear", State("m_Clear")),
                new StyleProfilePage("Rain", State("m_Rain")),
                new StyleProfilePage("Snow", State("m_Snow")),
                new StyleProfilePage("Shared", StyleProfilePage.Fields("m_Fog",
                    "#Painting", "m_FogPaint", "m_Silhouette",
                    "#Wind", "m_WindClear"))
            }),
            new StyleProfilePage("Overcast", StyleProfilePage.Fields("m_Overcast",
                "#Clouds", "m_CloudResponse", "m_CloudDrift",
                "#Look", "m_SkyGrey", "m_FogGrey", "m_BakedToSky")),
            new StyleProfilePage("Precipitation", StyleProfilePage.Fields("m_Weather",
                "#Rain", "m_RainColor", "m_RainOpacity", "m_RainWidth",
                "#Snow", "m_SnowSize", "m_SnowOpacity")),
            new StyleProfilePage("Surfaces", StyleProfilePage.Fields("m_Weather",
                "#Timing", "m_Speed", "m_Instant", "m_WetTime", "m_DryTime", "m_SettleTime", "m_MeltTime",
                "m_UncoverTime",
                "#Roofs", "m_RoofMargin",
                "#Shores", "m_ShoreReach", "m_ShoreDarkening"))
        };

        static readonly string[] s_Names = StyleProfilePage.Names(s_Pages);

        static string[] State(string root)
        {
            return StyleProfilePage.Fields(root,
                "#Distance", "m_Start", "m_End", "m_MaxOpacity",
                "#Layer", "m_LayerTop", "m_FadeHeight", "m_LayerCover", "m_AirHaze",
                "#Colour", "m_DayFog", "m_DuskFog", "m_NightFog", "m_SunScattering",
                "#Patches", "m_Clumps", "m_ClumpSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPages(s_Pages, s_Names);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(HBStyleQualityProfile))]
    [CanEditMultipleObjects]
    sealed class StyleQualityProfileEditor : StyleProfileEditor
    {
        static readonly Dictionary<string, string> s_Levels = new Dictionary<string, string>
        {
            { "m_Low", "Levels" },
            { "m_Contact", "Shared" }
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("m_Shadows", "Shadows", s_Levels);
            DrawSection("m_Atmosphere", "Atmosphere", s_Levels);
            DrawSection("m_Weather", "Weather", s_Levels);
            DrawSection("m_Precipitation", "Precipitation", s_Levels);
            DrawSection("m_Terrain", "Terrain", s_Levels);
            DrawSection("m_Grass", "Grass", s_Levels);
            DrawSection("m_AntiAliasing", "Anti Aliasing", s_Levels);
            DrawSection("m_Transparency", "Transparency", s_Levels);
            DrawSection("m_Reflections", "Screen Space Reflections", s_Levels);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
