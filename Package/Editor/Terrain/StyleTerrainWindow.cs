using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class StyleTerrainWindow : EditorWindow
    {
        const float GraphHeight = 220f;
        const float AxisLeft = 48f;
        const float AxisBottom = 18f;
        const float EdgeReach = 6f;
        const int SlopeBins = 90;
        const int HeightBins = 64;
        const int GraphSamples = 256;
        const float PickReach = 100000f;
        const float MarchLimit = 20000f;
        const float RowHeight = 22f;
        const float HoverPad = 6f;
        const float HoverStrip = 4f;
        const float HoverOffset = 14f;

        static readonly Color[] s_Palette =
        {
            new Color(0.90f, 0.30f, 0.25f), new Color(0.30f, 0.70f, 0.30f), new Color(0.25f, 0.45f, 0.90f),
            new Color(0.95f, 0.80f, 0.20f), new Color(0.70f, 0.35f, 0.85f), new Color(0.20f, 0.80f, 0.80f),
            new Color(0.95f, 0.55f, 0.15f), new Color(0.85f, 0.85f, 0.85f)
        };

        static readonly int s_LayerMapId = Shader.PropertyToID("_HB_TerrainLayerMap");

        static readonly Color s_HoverBack = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        static GUIStyle s_HoverTitle;
        static GUIStyle s_HoverText;

        static readonly Color s_GraphBack = new Color(0.12f, 0.12f, 0.12f, 1f);
        static readonly Color s_GraphFill = new Color(0.75f, 0.75f, 0.75f, 1f);

        static class Styles
        {
            public static readonly GUIContent Terrain = new GUIContent("Terrain",
                "The terrain being set up. Selecting another terrain in the scene switches to it.");

            public static readonly GUIContent New = new GUIContent("New",
                "Creates a profile filled with the terrain's current layers.");

            public static readonly GUIContent Slope = new GUIContent("Slope",
                "Steepness range, in degrees, the layer covers. 0 is flat ground, 90 a vertical cliff.");

            public const string VariantMissing =
                "Variant Of must be another layer of this profile.";

            public static readonly GUIContent Lowest = new GUIContent("Lowest",
                "Lowest world height, in metres, the layer reaches. Any = no lower limit.");

            public static readonly GUIContent Highest = new GUIContent("Highest",
                "Highest world height, in metres, the layer reaches. Any = no upper limit.");

            public static readonly GUIContent Any = new GUIContent("Any", "No limit on this side.");

            public static readonly GUIContent Bake = new GUIContent("Bake",
                "Places the automatic layers into the terrain's painting. What you painted by hand since the " +
                "last bake is kept on top.");

            public static readonly GUIContent ClearHand = new GUIContent("Clear Hand Painting",
                "Bakes again and throws away everything painted by hand since the last bake.");

            public static readonly GUIContent Pick = new GUIContent("Pick in Scene",
                "Hover the terrain to see its slope, height and layer; click to select that layer here.");

            public static readonly GUIContent Preview = new GUIContent("Preview",
                "Shows the rules on the terrain live while you edit, without changing its painting. Bake keeps " +
                "exactly what the preview shows.");

            public static readonly GUIContent Shared = new GUIContent("Or Use Material",
                "An existing material on the HB URP Style/Terrain shader, for example one another terrain " +
                "already uses. Terrains can share it; each keeps its own layers.");

            public static readonly GUIContent LayerMap = new GUIContent("Layer Map",
                "Colours every layer with a flat colour from the list so you can see where each one lies.");

            public const string Base =
                "The first layer covers the whole terrain. The layers after it are laid over it, in list order, " +
                "where their rules match.";

            public const string PaintOnly =
                "Paint only: this layer is placed only where you paint it with the terrain's Paint Texture tool.";

            public const string Graph =
                "Grey shows how much of the terrain sits at each slope and height. Drag a layer's edges to change " +
                "its range; click inside a box to select it.";
        }

        enum Edge
        {
            None,
            SlopeLow,
            SlopeHigh,
            HeightLow,
            HeightHigh
        }

        [SerializeField] Terrain m_Terrain;
        [SerializeField] int m_Selected;
        [SerializeField] bool m_Pick;
        [SerializeField] bool m_WhereExpanded = true;
        [SerializeField] bool m_VariantExpanded;
        [SerializeField] bool m_LookExpanded;
        [SerializeField] bool m_WeatherExpanded;
        [SerializeField] bool m_GrassExpanded;
        [SerializeField] Material m_Shared;

        SerializedObject m_Owner;
        SerializedObject m_Profile;
        ReorderableList m_List;
        Vector2 m_Scroll;
        bool m_Changed;
        Edge m_Edge;

        Texture2D m_Histogram;
        StyleTerrainSampler m_Sampler;
        Terrain m_SampledTerrain;
        int m_SampledVersion = -1;
        float m_Low;
        float m_High = 1f;

        bool m_Hovering;
        Vector3 m_HoverPoint;
        Vector3 m_HoverNormal;
        float m_HoverSlope;
        float m_HoverHeight;
        string m_HoverLayer;
        int m_HoverRow = -1;

        [MenuItem("Tools/HiddenBull/URP Style/Terrain Layers")]
        static void Open()
        {
            Show(null);
        }

        public static void Show(Terrain terrain)
        {
            var window = GetWindow<StyleTerrainWindow>();
            window.titleContent = new GUIContent("Terrain Layers");
            window.minSize = new Vector2(380f, 560f);

            if (terrain != null)
                window.m_Terrain = terrain;

            window.Show();
        }

        static Color Tint(int index)
        {
            return index < s_Palette.Length ? s_Palette[index] : Color.HSVToRGB(index * 0.618034f % 1f, 0.55f, 0.9f);
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;
            Selection.selectionChanged += OnSelection;
            Undo.undoRedoPerformed += OnUndo;
            OnSelection();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
            Selection.selectionChanged -= OnSelection;
            Undo.undoRedoPerformed -= OnUndo;

            if (m_Histogram != null)
                DestroyImmediate(m_Histogram);
        }

        void OnSelection()
        {
            var selected = Selection.activeGameObject;

            if (selected != null && selected.TryGetComponent(out Terrain terrain))
            {
                m_Terrain = terrain;
                Repaint();
            }
        }

        void OnUndo()
        {
            if (m_Profile != null && m_Profile.targetObject != null)
                m_Profile.Update();
            else
                m_Profile = null;

            if (m_Owner != null && m_Owner.targetObject != null)
                m_Owner.Update();
            else
                m_Owner = null;

            m_SampledVersion = -1;

            if (m_Terrain != null && m_Terrain.TryGetComponent(out HBStyleTerrain owner) &&
                !StyleTerrainPreview.IsOn(owner))
            {
                owner.Refresh();
            }

            Repaint();
        }

        void OnGUI()
        {
            m_Terrain = (Terrain)EditorGUILayout.ObjectField(Styles.Terrain, m_Terrain, typeof(Terrain), true);

            if (m_Terrain == null || m_Terrain.terrainData == null)
            {
                EditorGUILayout.HelpBox("Select a terrain to set up its layers.", MessageType.Info);
                return;
            }

            if (!m_Terrain.TryGetComponent(out HBStyleTerrain owner))
            {
                EditorGUILayout.HelpBox("This terrain has no HB Style Terrain component yet. It keeps the layer " +
                                        "profile and the last bake.", MessageType.Info);

                if (GUILayout.Button("Add HB Style Terrain"))
                    Undo.AddComponent<HBStyleTerrain>(m_Terrain.gameObject);

                return;
            }

            if (m_Owner == null || m_Owner.targetObject != owner)
                m_Owner = new SerializedObject(owner);

            if (DrawProfileField(owner) || DrawSetup(owner))
                GUIUtility.ExitGUI();

            var profile = owner.profile;

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Assign a profile or create a new one.", MessageType.Info);
                return;
            }

            Bind(profile);

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            m_Profile.Update();
            m_Changed = false;

            EditorGUI.BeginChangeCheck();

            DrawGraph();
            EditorGUILayout.Space(4f);

            m_List.index = Mathf.Clamp(m_Selected, -1, m_List.count - 1);
            m_List.DoLayoutList();

            DrawDetails();

            m_Changed |= EditorGUI.EndChangeCheck();
            m_Profile.ApplyModifiedProperties();

            EditorGUILayout.EndScrollView();

            if (m_Changed)
                Changed(owner);

            DrawBake(owner);
        }

        bool DrawProfileField(HBStyleTerrain owner)
        {
            m_Owner.Update();

            bool create;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_Owner.FindProperty("m_Profile"));
                create = GUILayout.Button(Styles.New, GUILayout.Width(50f));
            }

            m_Owner.ApplyModifiedProperties();

            if (create)
                CreateProfile(owner);

            return create;
        }

        void CreateProfile(HBStyleTerrain owner)
        {
            var data = m_Terrain.terrainData;
            var dataPath = AssetDatabase.GetAssetPath(data);
            var folder = string.IsNullOrEmpty(dataPath) ? "Assets" : Path.GetDirectoryName(dataPath);
            var path = EditorUtility.SaveFilePanelInProject("New Terrain Profile", $"{m_Terrain.name} Profile",
                                                            "asset", "Choose where to save the terrain profile.",
                                                            folder);

            if (string.IsNullOrEmpty(path))
                return;

            var profile = CreateInstance<HBStyleTerrainProfile>();

            foreach (var layer in data.terrainLayers)
            {
                if (layer != null)
                    profile.layers.Add(new HBStyleTerrainProfileLayer { layer = layer, automatic = false });
            }

            AssetDatabase.CreateAsset(profile, path);

            Undo.RecordObject(owner, "New Terrain Profile");
            owner.profile = profile;
            EditorUtility.SetDirty(owner);
            m_Selected = 0;
        }

        bool DrawSetup(HBStyleTerrain owner)
        {
            if (StyleTerrainSetup.UsesLayerShader(m_Terrain))
                return false;

            EditorGUILayout.HelpBox("The terrain does not use the HB URP Style/Terrain shader yet. Use HB " +
                                    "Terrain Shader makes a copy of its material on that shader, or pick a " +
                                    "material that already uses it.", MessageType.Info);

            if (GUILayout.Button("Use HB Terrain Shader"))
            {
                StyleTerrainSetup.UseLayerShader(owner);
                return true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                m_Shared = (Material)EditorGUILayout.ObjectField(Styles.Shared, m_Shared, typeof(Material), false);

                var usable = m_Shared != null && m_Shared.shader != null &&
                             m_Shared.shader.name == HBStyleTerrain.ShaderName;

                using (new EditorGUI.DisabledScope(!usable))
                {
                    if (!GUILayout.Button("Use", GUILayout.Width(50f)))
                        return false;
                }
            }

            StyleTerrainSetup.UseMaterial(owner, m_Shared);
            return true;
        }

        void Changed(HBStyleTerrain owner)
        {
            StyleTerrainBaker.SyncLayers(owner, true);

            if (StyleTerrainSetup.UsesLayerShader(m_Terrain))
                StyleTerrainArrays.Ensure(owner);

            owner.Bind();
            StyleTerrainAutoBake.Request(owner);
        }

        void Bind(HBStyleTerrainProfile profile)
        {
            if (m_Profile != null && m_Profile.targetObject == profile && m_List != null)
                return;

            m_Profile = new SerializedObject(profile);

            var layers = m_Profile.FindProperty(nameof(HBStyleTerrainProfile.layers));

            m_List = new ReorderableList(m_Profile, layers, true, true, true, true)
            {
                elementHeight = RowHeight,
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Layers (first = base)"),
                drawElementCallback = DrawRow,
                onSelectCallback = list => m_Selected = list.index,
                onCanAddCallback = list => list.count < HBStyleTerrainProfile.MaxLayers,
                onReorderCallbackWithDetails = (list, from, to) =>
                {
                    m_Selected = to;
                    m_Changed = true;
                },
                onAddCallback = list =>
                {
                    var property = list.serializedProperty;
                    property.arraySize++;
                    Reset(property.GetArrayElementAtIndex(property.arraySize - 1));
                    m_Selected = list.index = property.arraySize - 1;
                    m_Changed = true;
                },
                onRemoveCallback = list =>
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    m_Selected = Mathf.Clamp(list.index, 0, list.count - 1);
                    m_Changed = true;
                }
            };
        }

        static void Reset(SerializedProperty row)
        {
            var open = HBStyleTerrainProfileLayer.Unlimited;

            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.layer)).objectReferenceValue = null;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.automatic)).boolValue = true;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variantOf)).objectReferenceValue = null;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variantAmount)).floatValue = 0.3f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variantSize)).floatValue = 6f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.slope)).vector2Value = new Vector2(0f, 90f);
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.slopeSoftness)).floatValue = 6f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.height)).vector2Value = new Vector2(-open, open);
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.heightSoftness)).floatValue = 2f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.breakup)).floatValue = 0.5f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.breakupSize)).floatValue = 8f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.opacity)).floatValue = 1f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.tint)).colorValue = Color.white;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variation)).colorValue =
                new Color(0.8f, 0.9f, 0.7f, 0f);
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variationSize)).floatValue = 30f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variationShape)).vector4Value =
                new Vector4(0.9f, 0.35f, 4f, 0.35f);
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variationCover)).floatValue = 0f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.wetness)).floatValue = 0.5f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.snowCover)).floatValue = 1f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.grass)).floatValue = 0f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.grassHeight)).floatValue = 0.5f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.grassWidth)).floatValue = 0.7f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.ownTip)).boolValue = false;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.tipColor)).colorValue =
                new Color(0.55f, 0.75f, 0.25f, 1f);
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.flowers)).floatValue = 0f;
            row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.flowerSize)).floatValue = 0.15f;
        }

        void DrawRow(Rect rect, int index, bool active, bool focused)
        {
            var row = m_List.serializedProperty.GetArrayElementAtIndex(index);
            var layer = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.layer)).objectReferenceValue
                as TerrainLayer;
            var automatic = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.automatic)).boolValue;

            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 4f, 4f, rect.height - 8f), Tint(index));

            var thumb = new Rect(rect.x + 8f, rect.y + 2f, 18f, 18f);

            if (layer != null && layer.diffuseTexture != null)
                GUI.DrawTexture(thumb, layer.diffuseTexture, ScaleMode.ScaleAndCrop);
            else
                EditorGUI.DrawRect(thumb, new Color(0.3f, 0.3f, 0.3f));

            var variant = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variantOf)).objectReferenceValue;
            var mode = index == 0 ? "Base" : automatic ? "Automatic" : "Paint only";

            if (index > 0 && variant != null)
                mode += " + Variant";

            EditorGUI.LabelField(new Rect(rect.x + 32f, rect.y + 2f, rect.width - 156f, 18f),
                                 layer != null ? layer.name : "(no Terrain Layer)");
            EditorGUI.LabelField(new Rect(rect.xMax - 120f, rect.y + 2f, 120f, 18f), mode, EditorStyles.miniLabel);
        }

        void DrawDetails()
        {
            var layers = m_List.serializedProperty;

            if (layers.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add a layer and give it a Terrain Layer.", MessageType.Info);
                return;
            }

            m_Selected = Mathf.Clamp(m_Selected, 0, layers.arraySize - 1);

            var row = layers.GetArrayElementAtIndex(m_Selected);
            var layer = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.layer));

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(layer);

                if (Duplicate(layers, m_Selected, layer.objectReferenceValue))
                {
                    EditorGUILayout.HelpBox("This Terrain Layer is already used higher in the list; this row is " +
                                            "ignored.", MessageType.Warning);
                }

                m_WhereExpanded = EditorGUILayout.Foldout(m_WhereExpanded, "Where", true);

                if (m_WhereExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                        DrawWhere(row);
                }

                if (m_Selected > 0)
                {
                    m_VariantExpanded = EditorGUILayout.Foldout(m_VariantExpanded, "Variant", true);

                    if (m_VariantExpanded)
                    {
                        using (new EditorGUI.IndentLevelScope())
                            DrawVariant(row);
                    }
                }

                m_LookExpanded = EditorGUILayout.Foldout(m_LookExpanded, "Look", true);

                if (m_LookExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                        DrawLook(row);
                }

                m_GrassExpanded = EditorGUILayout.Foldout(m_GrassExpanded, "Grass", true);

                if (m_GrassExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                        DrawGrass(row);
                }

                m_WeatherExpanded = EditorGUILayout.Foldout(m_WeatherExpanded, "Weather", true);

                if (m_WeatherExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        Relative(row, nameof(HBStyleTerrainProfileLayer.wetness));
                        Relative(row, nameof(HBStyleTerrainProfileLayer.snowCover));
                    }
                }
            }
        }

        static void Relative(SerializedProperty row, string name)
        {
            EditorGUILayout.PropertyField(row.FindPropertyRelative(name));
        }

        void DrawWhere(SerializedProperty row)
        {
            if (m_Selected == 0)
            {
                EditorGUILayout.HelpBox(Styles.Base, MessageType.None);
                return;
            }

            var automatic = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.automatic));
            var wasAutomatic = automatic.boolValue;
            EditorGUILayout.PropertyField(automatic);

            if (!wasAutomatic && automatic.boolValue && m_Terrain.TryGetComponent(out HBStyleTerrain owner))
            {
                var layer = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.layer)).objectReferenceValue;
                StyleTerrainBaker.ClearHand(owner, layer as TerrainLayer);
            }

            if (!automatic.boolValue)
            {
                EditorGUILayout.HelpBox(Styles.PaintOnly, MessageType.None);
                return;
            }

            DrawSlope(row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.slope)));
            Relative(row, nameof(HBStyleTerrainProfileLayer.slopeSoftness));

            var height = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.height));
            DrawLimit(height, Styles.Lowest, false);
            DrawLimit(height, Styles.Highest, true);
            Relative(row, nameof(HBStyleTerrainProfileLayer.heightSoftness));

            var breakup = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.breakup));
            EditorGUILayout.PropertyField(breakup);

            using (new EditorGUI.DisabledScope(breakup.floatValue <= 0f))
            using (new EditorGUI.IndentLevelScope())
                Relative(row, nameof(HBStyleTerrainProfileLayer.breakupSize));

            Relative(row, nameof(HBStyleTerrainProfileLayer.opacity));
        }

        static void DrawVariant(SerializedProperty row)
        {
            var variantOf = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variantOf));
            EditorGUILayout.PropertyField(variantOf);

            var parent = variantOf.objectReferenceValue as TerrainLayer;

            if (parent == null)
                return;

            var own = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.layer)).objectReferenceValue;
            var profile = (HBStyleTerrainProfile)row.serializedObject.targetObject;

            if (parent == own || !profile.layers.Exists(other => other != null && other.layer == parent))
            {
                EditorGUILayout.HelpBox(Styles.VariantMissing, MessageType.Warning);
                return;
            }

            Relative(row, nameof(HBStyleTerrainProfileLayer.variantAmount));
            Relative(row, nameof(HBStyleTerrainProfileLayer.variantSize));
        }

        static void DrawGrass(SerializedProperty row)
        {
            var grass = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.grass));
            EditorGUILayout.PropertyField(grass);

            using (new EditorGUI.DisabledScope(grass.floatValue <= 0f))
            {
                Relative(row, nameof(HBStyleTerrainProfileLayer.grassHeight));
                Relative(row, nameof(HBStyleTerrainProfileLayer.grassWidth));

                var ownTip = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.ownTip));
                EditorGUILayout.PropertyField(ownTip);

                using (new EditorGUI.DisabledScope(!ownTip.boolValue))
                using (new EditorGUI.IndentLevelScope())
                    Relative(row, nameof(HBStyleTerrainProfileLayer.tipColor));
            }

            var flowers = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.flowers));
            EditorGUILayout.PropertyField(flowers);

            using (new EditorGUI.DisabledScope(flowers.floatValue <= 0f))
            using (new EditorGUI.IndentLevelScope())
                Relative(row, nameof(HBStyleTerrainProfileLayer.flowerSize));
        }

        static void DrawLook(SerializedProperty row)
        {
            Relative(row, nameof(HBStyleTerrainProfileLayer.tint));

            var variation = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.variation));
            EditorGUILayout.PropertyField(variation);

            using (new EditorGUI.DisabledScope(variation.colorValue.a <= 0f))
            using (new EditorGUI.IndentLevelScope())
            {
                Relative(row, nameof(HBStyleTerrainProfileLayer.variationSize));
                Relative(row, nameof(HBStyleTerrainProfileLayer.variationShape));
                Relative(row, nameof(HBStyleTerrainProfileLayer.variationCover));
            }
        }

        static bool Duplicate(SerializedProperty layers, int index, Object layer)
        {
            if (layer == null)
                return false;

            for (var other = 0; other < index; other++)
            {
                var value = layers.GetArrayElementAtIndex(other)
                                  .FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.layer)).objectReferenceValue;

                if (value == layer)
                    return true;
            }

            return false;
        }

        static void DrawSlope(SerializedProperty slope)
        {
            var value = slope.vector2Value;
            var low = Mathf.Min(value.x, value.y);
            var high = Mathf.Max(value.x, value.y);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.MinMaxSlider(Styles.Slope, ref low, ref high, 0f, 90f);
                low = Mathf.Clamp(EditorGUILayout.FloatField(low, GUILayout.Width(40f)), 0f, 90f);
                high = Mathf.Clamp(EditorGUILayout.FloatField(high, GUILayout.Width(40f)), 0f, 90f);
            }

            var range = new Vector2(Mathf.Round(low * 2f) * 0.5f, Mathf.Round(Mathf.Max(low, high) * 2f) * 0.5f);

            if (GUI.changed && range != value)
                slope.vector2Value = range;
        }

        void DrawLimit(SerializedProperty height, GUIContent label, bool upper)
        {
            var value = height.vector2Value;
            var open = HBStyleTerrainProfileLayer.Unlimited;
            var current = upper ? value.y : value.x;
            var any = upper ? current >= open * 0.5f : current <= -open * 0.5f;

            using (new EditorGUILayout.HorizontalScope())
            {
                var field = EditorGUILayout.GetControlRect();
                field = EditorGUI.PrefixLabel(field, label);

                var toggle = new Rect(field.x, field.y, 48f, field.height);
                var number = new Rect(field.x + 52f, field.y, field.width - 52f, field.height);
                var indent = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                var nowAny = EditorGUI.ToggleLeft(toggle, Styles.Any, any);

                if (nowAny != any)
                    current = nowAny ? (upper ? open : -open) : (upper ? m_High : m_Low);

                using (new EditorGUI.DisabledScope(nowAny))
                {
                    var shown = nowAny ? 0f : current;
                    var typed = EditorGUI.FloatField(number, shown);

                    if (!nowAny)
                        current = typed;
                }

                EditorGUI.indentLevel = indent;
            }

            if (upper)
                value.y = current;
            else
                value.x = current;

            if (value.x > value.y)
            {
                if (upper)
                    value.x = value.y;
                else
                    value.y = value.x;
            }

            if (value != height.vector2Value)
                height.vector2Value = value;
        }

        void DrawGraph()
        {
            EnsureSamples();

            var rect = GUILayoutUtility.GetRect(0f, GraphHeight, GUILayout.ExpandWidth(true));
            var plot = new Rect(rect.x + AxisLeft, rect.y + 4f, rect.width - AxisLeft - 8f,
                                rect.height - AxisBottom - 8f);

            EditorGUI.DrawRect(plot, s_GraphBack);

            if (m_Histogram != null)
                GUI.DrawTexture(plot, m_Histogram, ScaleMode.StretchToFill);

            var mini = EditorStyles.miniLabel;
            GUI.Label(new Rect(rect.x, plot.y - 2f, AxisLeft - 4f, 14f), $"{m_High:0} m", mini);
            GUI.Label(new Rect(rect.x, plot.yMax - 12f, AxisLeft - 4f, 14f), $"{m_Low:0} m", mini);
            GUI.Label(new Rect(plot.x - 2f, plot.yMax + 1f, 40f, 14f), "0°", mini);
            GUI.Label(new Rect(plot.center.x - 30f, plot.yMax + 1f, 60f, 14f), "Slope 45°", mini);
            GUI.Label(new Rect(plot.xMax - 22f, plot.yMax + 1f, 30f, 14f), "90°", mini);

            var layers = m_List.serializedProperty;

            if (Event.current.type == EventType.Repaint)
            {
                for (var index = 1; index < layers.arraySize; index++)
                {
                    var row = layers.GetArrayElementAtIndex(index);

                    if (!Automatic(row))
                        continue;

                    var area = Area(row, plot);
                    var tint = Tint(index);
                    var fill = tint;
                    fill.a = index == m_Selected ? 0.25f : 0.07f;
                    tint.a = index == m_Selected ? 1f : 0.6f;

                    Handles.DrawSolidRectangleWithOutline(area, fill, tint);
                }

                if (m_Hovering)
                {
                    var point = ToGraph(plot, m_HoverSlope, m_HoverHeight);
                    EditorGUI.DrawRect(new Rect(point.x - 3f, point.y - 3f, 6f, 6f), Color.white);
                }
            }

            GraphInput(plot, layers);

            EditorGUILayout.LabelField(Styles.Graph, EditorStyles.wordWrappedMiniLabel);
        }

        void GraphInput(Rect plot, SerializedProperty layers)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var current = Event.current;
            var selected = m_Selected > 0 && m_Selected < layers.arraySize
                ? layers.GetArrayElementAtIndex(m_Selected)
                : null;
            var active = selected != null && Automatic(selected);

            if (active && current.type == EventType.Repaint)
            {
                var area = Area(selected, plot);
                EditorGUIUtility.AddCursorRect(new Rect(area.xMin - EdgeReach, area.y, EdgeReach * 2f, area.height),
                                               MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(new Rect(area.xMax - EdgeReach, area.y, EdgeReach * 2f, area.height),
                                               MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(new Rect(area.x, area.yMin - EdgeReach, area.width, EdgeReach * 2f),
                                               MouseCursor.ResizeVertical);
                EditorGUIUtility.AddCursorRect(new Rect(area.x, area.yMax - EdgeReach, area.width, EdgeReach * 2f),
                                               MouseCursor.ResizeVertical);
            }

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    if (current.button != 0 || !plot.Contains(current.mousePosition))
                        break;

                    m_Edge = active ? HitEdge(Area(selected, plot), current.mousePosition) : Edge.None;

                    if (m_Edge != Edge.None)
                    {
                        GUIUtility.hotControl = id;
                        current.Use();
                        break;
                    }

                    for (var index = layers.arraySize - 1; index > 0; index--)
                    {
                        var row = layers.GetArrayElementAtIndex(index);

                        if (Automatic(row) && Area(row, plot).Contains(current.mousePosition))
                        {
                            m_Selected = index;
                            current.Use();
                            break;
                        }
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl != id || selected == null)
                        break;

                    Drag(selected, plot, current.mousePosition);
                    GUI.changed = true;
                    current.Use();
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != id)
                        break;

                    GUIUtility.hotControl = 0;
                    m_Edge = Edge.None;
                    current.Use();
                    break;
                }
            }
        }

        void Drag(SerializedProperty row, Rect plot, Vector2 mouse)
        {
            var slope = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.slope));
            var height = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.height));
            var degrees = Mathf.Round(Mathf.Clamp01((mouse.x - plot.x) / plot.width) * 180f) * 0.5f;
            var metres = Mathf.Round(Mathf.Lerp(m_Low, m_High, Mathf.Clamp01((plot.yMax - mouse.y) / plot.height))
                                     * 10f) * 0.1f;
            var range = slope.vector2Value;
            var span = height.vector2Value;

            switch (m_Edge)
            {
                case Edge.SlopeLow:
                    range.x = Mathf.Min(degrees, range.y);
                    break;
                case Edge.SlopeHigh:
                    range.y = Mathf.Max(degrees, range.x);
                    break;
                case Edge.HeightLow:
                    span.x = Mathf.Min(metres, span.y);
                    break;
                case Edge.HeightHigh:
                    span.y = Mathf.Max(metres, span.x);
                    break;
            }

            slope.vector2Value = range;
            height.vector2Value = span;
        }

        static Edge HitEdge(Rect area, Vector2 mouse)
        {
            var insideX = mouse.x > area.xMin - EdgeReach && mouse.x < area.xMax + EdgeReach;
            var insideY = mouse.y > area.yMin - EdgeReach && mouse.y < area.yMax + EdgeReach;

            if (insideY && Mathf.Abs(mouse.x - area.xMin) <= EdgeReach)
                return Edge.SlopeLow;

            if (insideY && Mathf.Abs(mouse.x - area.xMax) <= EdgeReach)
                return Edge.SlopeHigh;

            if (insideX && Mathf.Abs(mouse.y - area.yMax) <= EdgeReach)
                return Edge.HeightLow;

            if (insideX && Mathf.Abs(mouse.y - area.yMin) <= EdgeReach)
                return Edge.HeightHigh;

            return Edge.None;
        }

        static bool Automatic(SerializedProperty row)
        {
            return row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.automatic)).boolValue;
        }

        Rect Area(SerializedProperty row, Rect plot)
        {
            var slope = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.slope)).vector2Value;
            var height = row.FindPropertyRelative(nameof(HBStyleTerrainProfileLayer.height)).vector2Value;

            var low = ToGraph(plot, slope.x, height.x);
            var high = ToGraph(plot, slope.y, height.y);

            return Rect.MinMaxRect(low.x, high.y, high.x, low.y);
        }

        Vector2 ToGraph(Rect plot, float slope, float height)
        {
            var x = plot.x + Mathf.Clamp01(slope / 90f) * plot.width;
            var t = Mathf.Clamp01((height - m_Low) / Mathf.Max(m_High - m_Low, 1e-3f));
            return new Vector2(x, plot.yMax - t * plot.height);
        }

        void EnsureSamples()
        {
            var version = StyleTerrainAutoBake.heightsVersion;

            if (m_SampledTerrain == m_Terrain && m_SampledVersion == version && m_Histogram != null)
                return;

            m_SampledTerrain = m_Terrain;
            m_SampledVersion = version;
            m_Sampler = new StyleTerrainSampler(m_Terrain);

            var slopes = new float[GraphSamples * GraphSamples];
            var heights = new float[slopes.Length];
            var low = float.MaxValue;
            var high = float.MinValue;

            for (var y = 0; y < GraphSamples; y++)
            {
                for (var x = 0; x < GraphSamples; x++)
                {
                    var u = (x + 0.5f) / GraphSamples;
                    var v = (y + 0.5f) / GraphSamples;
                    var index = y * GraphSamples + x;

                    slopes[index] = m_Sampler.Slope(u, v);
                    heights[index] = m_Sampler.Height(u, v);
                    low = Mathf.Min(low, heights[index]);
                    high = Mathf.Max(high, heights[index]);
                }
            }

            var pad = Mathf.Max((high - low) * 0.05f, 1f);
            m_Low = Mathf.Floor(low - pad);
            m_High = Mathf.Ceil(high + pad);

            var counts = new int[SlopeBins * HeightBins];
            var most = 1;

            for (var index = 0; index < slopes.Length; index++)
            {
                var column = Mathf.Clamp((int)(slopes[index] / 90f * SlopeBins), 0, SlopeBins - 1);
                var t = (heights[index] - m_Low) / (m_High - m_Low);
                var line = Mathf.Clamp((int)(t * HeightBins), 0, HeightBins - 1);
                var bin = line * SlopeBins + column;

                counts[bin]++;
                most = Mathf.Max(most, counts[bin]);
            }

            if (m_Histogram == null)
            {
                m_Histogram = new Texture2D(SlopeBins, HeightBins, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var pixels = new Color[counts.Length];
            var scale = 1f / Mathf.Log(1f + most);

            for (var index = 0; index < counts.Length; index++)
            {
                var amount = counts[index] > 0 ? 0.25f + 0.75f * Mathf.Log(1f + counts[index]) * scale : 0f;
                pixels[index] = Color.Lerp(s_GraphBack, s_GraphFill, amount);
            }

            m_Histogram.SetPixels(pixels);
            m_Histogram.Apply();
        }

        void DrawBake(HBStyleTerrain owner)
        {
            EditorGUILayout.Space(4f);

            var shader = StyleTerrainSetup.UsesLayerShader(m_Terrain);

            m_Owner.Update();
            EditorGUILayout.PropertyField(m_Owner.FindProperty("m_AutoBake"));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Owner.FindProperty("m_LayerResolution"));
            var resized = EditorGUI.EndChangeCheck();

            m_Owner.ApplyModifiedProperties();

            if (resized && shader)
                StyleTerrainArrays.Ensure(owner);

            if (!StyleTerrainBaker.HasBake(owner))
            {
                EditorGUILayout.HelpBox("Not baked yet. Press Bake once; after that Auto Bake follows sculpting and " +
                                        "profile changes.", MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!shader))
                {
                    var preview = GUILayout.Toggle(StyleTerrainPreview.IsOn(owner), Styles.Preview, "Button",
                                                   GUILayout.Height(26f), GUILayout.Width(80f));

                    if (preview != StyleTerrainPreview.IsOn(owner))
                        SetPreview(owner, preview);
                }

                if (GUILayout.Button(Styles.Bake, GUILayout.Height(26f)))
                    BakeNow(owner, true);

                if (GUILayout.Button(Styles.ClearHand, GUILayout.Height(26f)) &&
                    EditorUtility.DisplayDialog("Clear Hand Painting",
                                                "Bake again and throw away everything painted by hand since the " +
                                                "last bake? Undo brings it back.", "Clear", "Cancel"))
                {
                    BakeNow(owner, false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var pick = GUILayout.Toggle(m_Pick, Styles.Pick, "Button", GUILayout.Height(22f));

                if (pick != m_Pick)
                {
                    m_Pick = pick;
                    m_Hovering = false;
                    SceneView.RepaintAll();
                }

                var mapped = Shader.GetGlobalFloat(s_LayerMapId) > 0.5f;

                using (new EditorGUI.DisabledScope(!shader))
                {
                    var map = GUILayout.Toggle(mapped, Styles.LayerMap, "Button", GUILayout.Height(22f));

                    if (map != mapped)
                    {
                        Shader.SetGlobalFloat(s_LayerMapId, map ? 1f : 0f);
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        void SetPreview(HBStyleTerrain owner, bool on)
        {
            if (on)
            {
                StyleTerrainBaker.SyncLayers(owner, true);
                StyleTerrainArrays.Ensure(owner);
            }

            StyleTerrainPreview.Set(owner, on);
            SceneView.RepaintAll();
        }

        void BakeNow(HBStyleTerrain owner, bool keepHand)
        {
            if (!StyleTerrainBaker.HasBake(owner) && StyleTerrainBaker.HasPainting(owner) &&
                !EditorUtility.DisplayDialog("Bake Terrain Layers",
                                             "The first bake replaces the painting of the base and automatic layers. " +
                                             "Paint-only layers keep what is painted. Undo brings it all back.",
                                             "Bake", "Cancel"))
            {
                return;
            }

            if (!StyleTerrainBaker.Bake(owner, null, keepHand, true))
            {
                ShowNotification(new GUIContent("Nothing to bake: give the profile a Terrain Layer."));
                return;
            }

            if (StyleTerrainSetup.UsesLayerShader(m_Terrain))
                StyleTerrainArrays.Ensure(owner);

            AssetDatabase.SaveAssets();
            StyleTerrainPreview.MarkDirty(owner, true);
            m_SampledVersion = -1;
        }

        void OnScene(SceneView view)
        {
            if (!m_Pick || m_Terrain == null || m_Terrain.terrainData == null)
                return;

            var current = Event.current;

            switch (current.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    break;
                case EventType.MouseMove:
                case EventType.MouseDrag:
                    Hover(current.mousePosition, false);
                    view.Repaint();
                    Repaint();
                    break;
                case EventType.MouseDown:
                    if (current.button != 0 || current.alt)
                        break;

                    Hover(current.mousePosition, true);

                    if (m_HoverRow >= 0)
                        m_Selected = m_HoverRow;

                    current.Use();
                    Repaint();
                    break;
                case EventType.Repaint:
                    DrawHover();
                    break;
            }
        }

        void DrawHover()
        {
            if (!m_Hovering)
                return;

            var size = HandleUtility.GetHandleSize(m_HoverPoint) * 0.2f;
            var tint = m_HoverRow >= 0 ? Tint(m_HoverRow) : Color.white;

            Handles.color = tint;
            Handles.DrawWireDisc(m_HoverPoint, m_HoverNormal, size);

            if (s_HoverTitle == null)
            {
                s_HoverTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
                s_HoverTitle.normal.textColor = Color.white;
                s_HoverText = new GUIStyle(EditorStyles.label) { fontSize = 11 };
                s_HoverText.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            }

            var title = new GUIContent(m_HoverLayer);
            var detail = new GUIContent($"Slope {m_HoverSlope:0.0}°   Height {m_HoverHeight:0.0} m");
            var titleSize = s_HoverTitle.CalcSize(title);
            var detailSize = s_HoverText.CalcSize(detail);
            var width = Mathf.Max(titleSize.x, detailSize.x) + HoverPad * 2f + HoverStrip;
            var height = titleSize.y + detailSize.y + HoverPad * 2f;

            Handles.BeginGUI();

            var anchor = HandleUtility.WorldToGUIPoint(m_HoverPoint);
            var box = new Rect(anchor.x + HoverOffset, anchor.y - height - HoverOffset, width, height);
            var view = SceneView.currentDrawingSceneView != null
                ? SceneView.currentDrawingSceneView.position
                : new Rect(0f, 0f, float.MaxValue, float.MaxValue);

            box.x = Mathf.Min(box.x, view.width - width - 4f);
            box.y = Mathf.Max(box.y, 4f);

            EditorGUI.DrawRect(box, s_HoverBack);
            EditorGUI.DrawRect(new Rect(box.x, box.y, HoverStrip, box.height), tint);

            var left = box.x + HoverStrip + HoverPad;
            GUI.Label(new Rect(left, box.y + HoverPad, titleSize.x, titleSize.y), title, s_HoverTitle);
            GUI.Label(new Rect(left, box.y + HoverPad + titleSize.y, detailSize.x, detailSize.y), detail, s_HoverText);

            Handles.EndGUI();
        }

        void Hover(Vector2 mouse, bool sync)
        {
            m_Hovering = false;
            m_HoverRow = -1;

            var ray = HandleUtility.GUIPointToWorldRay(mouse);

            if (!Hit(ray, out var point))
                return;

            var data = m_Terrain.terrainData;
            var origin = m_Terrain.GetPosition();
            var u = (point.x - origin.x) / data.size.x;
            var v = (point.z - origin.z) / data.size.z;

            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return;

            EnsureSamples();

            if (sync)
                data.SyncTexture(TerrainData.AlphamapTextureName);

            var resolution = data.alphamapResolution;
            var x = Mathf.Clamp(Mathf.RoundToInt(u * (resolution - 1)), 0, resolution - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(v * (resolution - 1)), 0, resolution - 1);
            var weights = data.GetAlphamaps(x, y, 1, 1);
            var layers = data.terrainLayers;
            var best = -1;

            for (var k = 0; k < weights.GetLength(2); k++)
            {
                if (best < 0 || weights[0, 0, k] > weights[0, 0, best])
                    best = k;
            }

            var layer = best >= 0 && best < layers.Length ? layers[best] : null;
            var profile = m_Terrain.TryGetComponent(out HBStyleTerrain owner) ? owner.profile : null;

            m_HoverLayer = layer != null ? layer.name : "No layer";
            m_HoverRow = profile != null && layer != null ? profile.layers.FindIndex(row => row.layer == layer) : -1;
            m_HoverSlope = m_Sampler.Slope(u, v);
            m_HoverHeight = m_Sampler.Height(u, v);
            m_HoverNormal = data.GetInterpolatedNormal(u, v);
            m_HoverPoint = point;
            m_Hovering = true;
        }

        bool Hit(Ray ray, out Vector3 point)
        {
            if (m_Terrain.TryGetComponent(out TerrainCollider collider) && collider.enabled &&
                collider.Raycast(ray, out var hit, PickReach))
            {
                point = hit.point;
                return true;
            }

            var origin = m_Terrain.GetPosition().y;
            var travelled = 0f;

            while (travelled < MarchLimit)
            {
                var sample = ray.GetPoint(travelled);

                if (sample.y <= m_Terrain.SampleHeight(sample) + origin)
                {
                    point = sample;
                    return true;
                }

                travelled += Mathf.Max(0.5f, travelled * 0.01f);
            }

            point = default;
            return false;
        }
    }
}
