using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
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

        protected void DrawCardPages(StyleProfilePage[] pages, string[] names, string key)
        {
            var page = pages[DrawCardTabs(names, key)];

            DrawCards(page.entries, page.title, key);
        }

        protected int DrawCardTabs(string[] names, string key)
        {
            var index = DrawTabs(names, TabPrefix + target.GetType().Name + "." + key);

            EditorGUILayout.Space(4f);
            return index;
        }

        protected void DrawCards(string[] entries, string title, string key)
        {
            var index = 0;
            var prefix = target.GetType().Name + "." + key + ".";

            while (index < entries.Length)
            {
                var name = title;

                if (entries[index][0] == '#')
                {
                    name = entries[index].Substring(1);
                    index++;
                }

                var group = new List<string>();

                while (index < entries.Length && entries[index][0] != '#')
                    group.Add(entries[index++]);

                var fields = group.ToArray();
                var open = StyleSectionHeader.Draw(new GUIContent(name), prefix + name, () => ResetEntries(fields));

                if (open)
                {
                    foreach (var field in fields)
                    {
                        var property = serializedObject.FindProperty(field);

                        if (property != null)
                            EditorGUILayout.PropertyField(property, true);
                    }
                }

                StyleSectionHeader.End(open);
            }

            CoreEditorUtils.DrawSplitter();
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

        void ResetEntries(string[] entries)
        {
            var defaults = CreateInstance(target.GetType());

            try
            {
                using var source = new SerializedObject(defaults);

                serializedObject.Update();

                foreach (var entry in entries)
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
        const string SideKey = "HiddenBull.UrpStyle.EnvironmentSide";
        const string WeatherKey = "HiddenBull.UrpStyle.EnvironmentFogWeather";
        const string Atmosphere = "m_Atmosphere";
        const float SideHeight = 28f;

        static readonly StyleProfilePage[] s_Environment =
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
                "#Light", "m_SilverLining", "m_SunThrough", "m_Powder"))
        };

        static readonly StyleProfilePage[] s_Atmosphere =
        {
            new StyleProfilePage("Fog", new string[0]),
            new StyleProfilePage("Overcast", StyleProfilePage.Fields(Atmosphere + ".m_Overcast",
                "#Clouds", "m_CloudResponse", "m_CloudDrift",
                "#Look", "m_SkyGrey", "m_FogGrey", "m_BakedToSky")),
            new StyleProfilePage("Precipitation", StyleProfilePage.Fields(Atmosphere + ".m_Weather",
                "#Rain", "m_RainColor", "m_RainOpacity", "m_RainWidth",
                "#Snow", "m_SnowSize", "m_SnowOpacity")),
            new StyleProfilePage("Surfaces", StyleProfilePage.Fields(Atmosphere + ".m_Weather",
                "#Timing", "m_Speed", "m_Instant", "m_WetTime", "m_DryTime", "m_SettleTime", "m_MeltTime",
                "m_UncoverTime",
                "#Roofs", "m_RoofMargin",
                "#Shores", "m_ShoreReach", "m_ShoreDarkening"))
        };

        static readonly string[] s_FogRoots = { "m_Clear", "m_Rain", "m_Snow" };

        static readonly string[][] s_FogStates = { State(s_FogRoots[0]), State(s_FogRoots[1]), State(s_FogRoots[2]) };

        const string CopyTip = "Copy another weather's Distance, Layer, Colour and Patches into the weather " +
                               "shown. Undo brings the old values back.";

        static readonly string[] s_FogShared = StyleProfilePage.Fields(Atmosphere + ".m_Fog",
            "#Painting (All Weather)", "m_FogPaint", "m_Silhouette",
            "#Wind (All Weather)", "m_WindClear");

        static readonly string[] s_EnvironmentNames = StyleProfilePage.Names(s_Environment);
        static readonly string[] s_AtmosphereNames = StyleProfilePage.Names(s_Atmosphere);

        static readonly GUIContent[] s_Weathers =
        {
            new GUIContent("Clear", "The fog with no rain or snow."),
            new GUIContent("Rain", "The fog at full rain. Light rain blends between Clear and this."),
            new GUIContent("Snow", "The fog at full snow. Light snow blends between Clear and this.")
        };

        static readonly GUIContent s_Enabled = new GUIContent("Enabled",
            "Turns the atmosphere on: fog, rain and snow closing the sky, and the rain, snow and wet surface looks " +
            "set here. Off means no fog and default weather looks.");

        static readonly GUIContent s_Convert = new GUIContent("Convert Now",
            "Copies the old Atmosphere Profile's settings into this profile and stops using that asset.");

        static GUIContent[] s_Sides;
        static GUIStyle s_SideStyle;

        static string[] State(string root)
        {
            return StyleProfilePage.Fields(Atmosphere + "." + root,
                "#Distance", "m_Start", "m_End", "m_MaxOpacity",
                "#Layer", "m_LayerTop", "m_FadeHeight", "m_LayerCover", "m_AirHaze",
                "#Colour", "m_DayFog", "m_DuskFog", "m_NightFog", "m_SunScattering",
                "#Patches", "m_Clumps", "m_ClumpSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLegacy();

            var side = DrawSides();

            EditorGUILayout.Space(6f);

            if (side == 0)
                DrawCardPages(s_Environment, s_EnvironmentNames, "Environment");
            else
                DrawAtmosphere();

            serializedObject.ApplyModifiedProperties();
        }

        static int DrawSides()
        {
            if (s_Sides == null)
            {
                var prefix = EditorGUIUtility.isProSkin ? "d_" : "";

                s_Sides = new[]
                {
                    new GUIContent(" Environment", EditorGUIUtility.FindTexture(prefix + "DirectionalLight Icon"),
                                   "Sky, light, sun and moon, night sky and clouds."),
                    new GUIContent(" Atmosphere", EditorGUIUtility.FindTexture(prefix + "WindZone Icon"),
                                   "Fog, overcast, falling rain and snow, and wet and snowy surfaces.")
                };

                s_SideStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fixedHeight = SideHeight,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }

            var selected = Mathf.Clamp(SessionState.GetInt(SideKey, 0), 0, s_Sides.Length - 1);
            var next = GUILayout.Toolbar(selected, s_Sides, s_SideStyle, GUILayout.Height(SideHeight));

            if (next != selected)
            {
                SessionState.SetInt(SideKey, next);
                GUI.FocusControl(null);
            }

            return next;
        }

        void DrawAtmosphere()
        {
            var enabled = serializedObject.FindProperty(Atmosphere + ".m_Enabled");

            EditorGUILayout.PropertyField(enabled, s_Enabled);

            var off = !enabled.hasMultipleDifferentValues && !enabled.boolValue;

            if (off)
                EditorGUILayout.HelpBox("The atmosphere is off: there is no fog, and rain and snow do not close " +
                                        "the sky.", MessageType.Info);

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(off))
            {
                var page = DrawCardTabs(s_AtmosphereNames, "Atmosphere");

                if (page == 0)
                    DrawFog();
                else
                    DrawCards(s_Atmosphere[page].entries, s_Atmosphere[page].title, "Atmosphere");
            }
        }

        void DrawFog()
        {
            var selected = Mathf.Clamp(SessionState.GetInt(WeatherKey, 0), 0, s_Weathers.Length - 1);
            int next;

            using (new EditorGUILayout.HorizontalScope())
            {
                next = GUILayout.Toolbar(selected, s_Weathers, EditorStyles.miniButton);
                DrawFogCopy(next);
            }

            if (next != selected)
            {
                SessionState.SetInt(WeatherKey, next);
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(4f);
            DrawCards(s_FogStates[next], "Fog", "Atmosphere.Fog");
            DrawCards(s_FogShared, "Fog", "Atmosphere.Fog");
        }

        void DrawFogCopy(int selected)
        {
            var icon = new GUIContent(CoreEditorStyles.contextMenuIcon.image, CopyTip);

            if (!GUILayout.Button(icon, CoreEditorStyles.contextMenuStyle, GUILayout.Width(16f)))
                return;

            var menu = new GenericMenu();

            for (var i = 0; i < s_Weathers.Length; i++)
            {
                var from = i;
                var label = new GUIContent("Copy From " + s_Weathers[i].text);

                if (i == selected)
                    menu.AddDisabledItem(label);
                else
                    menu.AddItem(label, false, () => CopyFog(from, selected));
            }

            menu.ShowAsContext();
        }

        void CopyFog(int from, int to)
        {
            serializedObject.ApplyModifiedProperties();

            foreach (var item in targets)
            {
                var data = new SerializedObject(item);
                var source = data.FindProperty(Atmosphere + "." + s_FogRoots[from]);
                var target = data.FindProperty(Atmosphere + "." + s_FogRoots[to]);

                if (source == null || target == null)
                    continue;

                target.boxedValue = source.boxedValue;
                data.ApplyModifiedProperties();
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        void DrawLegacy()
        {
            var legacy = false;

            foreach (var item in targets)
                legacy |= item is HBStyleEnvironmentProfile profile && profile.legacyAtmosphere;

            if (!legacy)
                return;

            EditorGUILayout.HelpBox("This profile still reads an old Atmosphere Profile asset. Its settings are " +
                                    "copied into this profile when the project can write it.", MessageType.Info);

            if (!GUILayout.Button(s_Convert))
                return;

            serializedObject.ApplyModifiedProperties();

            foreach (var item in targets)
            {
                if (item is HBStyleEnvironmentProfile profile)
                    StyleEnvironmentProfiles.Move(profile);
            }

            serializedObject.Update();
        }
    }

    [CustomEditor(typeof(HBStyleAtmosphereProfile))]
    sealed class StyleAtmosphereProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Atmosphere settings now live in the Environment Profile, on its Atmosphere " +
                                    "side. Environment Profiles that used this asset copy its settings when the " +
                                    "project opens; after that this asset is no longer used and can be deleted.",
                                    MessageType.Info);
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
            DrawSection("m_Bloom", "Bloom", s_Levels);
            DrawSection("m_AmbientOcclusion", "Ambient Occlusion", s_Levels);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
