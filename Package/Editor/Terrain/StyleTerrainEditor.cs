using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleTerrain))]
    sealed class StyleTerrainEditor : UnityEditor.Editor
    {
        const string TabKey = "HiddenBull.UrpStyle.Terrain.Tab";
        const string PreviewKey = "HiddenBull.UrpStyle.Terrain.Preview";
        const int LayersTab = 0;
        const int BrushesTab = 1;
        const int LookTab = 2;

        static readonly GUIContent[] s_Tabs =
        {
            new GUIContent("Layers", "Ground layers, their rules, preview and bake."),
            new GUIContent("Brushes", "Shape and paint the ground in the Scene view."),
            new GUIContent("Look", "Brush strokes, curvature and colour variation of the ground."),
            new GUIContent("Settings", "Map resolutions and the terrain's heights.")
        };

        static readonly string[] s_StrokeFields =
        {
            "m_BrushSize", "m_BrushPaint", "m_ToneStrokes", "m_BrushRelief", "m_ShadingBreakup", "m_AmbientBreakup",
            "m_AlbedoVariation"
        };

        static readonly string[] s_ShapeFields = { "m_Curvature", "m_MacroVariation" };

        static readonly GUIContent s_NewDataLabel = new GUIContent("New",
            "Creates a new flat terrain data asset next to the scene and uses it.");

        static readonly GUIContent s_NewSetLabel = new GUIContent("New",
            "Creates an empty layer set next to the terrain data and uses it.");

        static readonly GUIContent s_DuplicateSetLabel = new GUIContent("Duplicate",
            "Copies the current layer set next to it and uses the copy, so this terrain can vary it alone.");

        static readonly GUIContent s_ControlLabel = new GUIContent("Layer Map Resolution",
            "Size of the maps that hold where each layer lies. 2048 over a 4 km terrain is one value every 2 m; " +
            "the textures' heights sharpen the borders between those values.");

        static readonly GUIContent s_TextureLabel = new GUIContent("Texture Resolution",
            "Size every layer texture is resampled to for the terrain shader.");

        static readonly GUIContent s_SourceLabel = new GUIContent("Unity Terrain Data",
            "A Unity TerrainData asset to copy the heights and holes from. Layers, trees and details are not copied.");

        static readonly GUIContent s_ResolutionLabel = new GUIContent("Resolution",
            "Heightmap samples per side. The terrain size divided by this is the spacing of the finest triangles.");

        static readonly GUIContent s_WidthLabel = new GUIContent("Width",
            "Width and length of a new flat terrain, in metres. HB Style terrains are square.");

        static readonly GUIContent s_HeightLabel = new GUIContent("Height Range",
            "Highest point a new flat terrain can be sculpted to, in metres above its origin.");

        static readonly GUIContent[] s_ControlNames =
        {
            new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"), new GUIContent("4096")
        };

        static readonly int[] s_ControlSizes = { 512, 1024, 2048, 4096 };

        static readonly GUIContent[] s_TextureNames =
        {
            new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048")
        };

        static readonly int[] s_TextureSizes = { 512, 1024, 2048 };

        static readonly string[] s_ResolutionNames = { "513", "1025", "2049", "4097" };
        static readonly int[] s_Resolutions = { 513, 1025, 2049, 4097 };

        static bool s_ShowImport;
        static bool s_ShowFlat;

        SerializedProperty m_DataProperty;
        SerializedObject m_DataObject;
        StyleTerrainLayerList m_Layers;
        HBStyleTerrainData m_Previewed;

        TerrainData m_Source;
        int m_Resolution = 1;
        float m_Width = 1024f;
        float m_Height = 300f;

        HBStyleTerrain terrain => (HBStyleTerrain)target;

        void OnEnable()
        {
            m_DataProperty = serializedObject.FindProperty("m_Data");
        }

        void OnDisable()
        {
            m_Layers?.Release();
            m_Layers = null;
            StyleTerrainBrush.End(terrain);
            SetPreview(null);
        }

        public override void OnInspectorGUI()
        {
            DrawDataField();

            var data = terrain.data;

            if (data == null)
            {
                StyleTerrainBrush.End(terrain);
                SetPreview(null);
                EditorGUILayout.HelpBox("Pick a Terrain Data, or press New to start a flat terrain.",
                                        MessageType.Info);
                return;
            }

            if (m_DataObject == null || m_DataObject.targetObject != data)
                m_DataObject = new SerializedObject(data);

            var tab = SessionState.GetInt(TabKey, LayersTab);
            var painting = tab == BrushesTab && StyleTerrainBrush.armed &&
                           StyleTerrainBrush.tool == StyleTerrainBrushTool.PaintLayer;

            if (painting && !EditorPrefs.GetBool(PreviewKey, true))
                EditorPrefs.SetBool(PreviewKey, true);

            var wanted = EditorPrefs.GetBool(PreviewKey, true);
            var preview = StyleTerrainStatus.Draw(data, wanted);

            if (preview != wanted)
                EditorPrefs.SetBool(PreviewKey, preview);

            SetPreview(preview ? data : null);

            EditorGUILayout.Space(2f);
            DrawGuide(tab, data);
            EditorGUILayout.Space(2f);

            tab = GUILayout.Toolbar(tab, s_Tabs, GUILayout.Height(24f));
            SessionState.SetInt(TabKey, tab);

            if (tab != LayersTab)
                m_Layers?.Hide();

            if (tab == BrushesTab && StyleTerrainBrush.armed)
                StyleTerrainBrush.Begin(terrain);
            else
                StyleTerrainBrush.End(terrain);

            switch (tab)
            {
                case LayersTab:
                    DrawLayers(data);
                    break;

                case BrushesTab:
                    StyleTerrainBrushPanel.Draw(data);
                    break;

                case LookTab:
                    DrawLook();
                    break;

                default:
                    DrawSettings(data);
                    break;
            }
        }

        static void DrawGuide(int tab, HBStyleTerrainData data)
        {
            switch (tab)
            {
                case LayersTab:
                    StyleTerrainGUI.Guide(StyleTerrainGUI.layersTint, "Layers",
                                          "Each automatic layer is a box in the graph: drag its edges to set where " +
                                          "it grows, or pick its slope and height in the Scene view. The preview " +
                                          "shows changes at once; Bake keeps them.", null);
                    break;

                case BrushesTab:
                    StyleTerrainBrushPanel.Guide(data);
                    break;

                case LookTab:
                    StyleTerrainGUI.Guide(StyleTerrainGUI.lookTint, "Look",
                                          "How the ground is drawn: brush strokes, hollows and ridges, and wide " +
                                          "colour changes. Changes show at once.", null);
                    break;

                default:
                    StyleTerrainGUI.Guide(StyleTerrainGUI.settingsTint, "Settings",
                                          "Map sizes and the terrain's heights: import a Unity terrain or start a " +
                                          "flat one.", null);
                    break;
            }
        }

        void DrawLook()
        {
            m_DataObject.Update();

            var look = m_DataObject.FindProperty("m_Look");

            EditorGUI.BeginChangeCheck();
            LookSection("Brush Strokes", look, s_StrokeFields);
            LookSection("Shape And Variation", look, s_ShapeFields);

            if (m_DataObject.ApplyModifiedProperties() || EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        static void LookSection(string title, SerializedProperty look, string[] fields)
        {
            StyleTerrainGUI.Header(title, StyleTerrainGUI.lookTint);

            foreach (var field in fields)
                EditorGUILayout.PropertyField(look.FindPropertyRelative(field));
        }

        void SetPreview(HBStyleTerrainData data)
        {
            if (m_Previewed == data)
                return;

            StyleTerrainPreviewer.End(m_Previewed);
            m_Previewed = data;
            StyleTerrainPreviewer.Begin(data);
        }

        void DrawDataField()
        {
            serializedObject.Update();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_DataProperty);

                if (GUILayout.Button(s_NewDataLabel, EditorStyles.miniButton, GUILayout.Width(44f)))
                    m_DataProperty.objectReferenceValue = StyleTerrainHeights.NewData();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawLayers(HBStyleTerrainData data)
        {
            StyleTerrainGUI.Header("Layer Set", StyleTerrainGUI.layersTint);

            m_DataObject.Update();

            using (new EditorGUILayout.HorizontalScope())
            {
                var setProperty = m_DataObject.FindProperty("m_LayerSet");

                EditorGUILayout.PropertyField(setProperty, GUIContent.none);

                if (GUILayout.Button(s_NewSetLabel, EditorStyles.miniButtonLeft, GUILayout.Width(40f)))
                    setProperty.objectReferenceValue = NewSet(data);

                using (new EditorGUI.DisabledScope(data.layerSet == null))
                {
                    if (GUILayout.Button(s_DuplicateSetLabel, EditorStyles.miniButtonRight, GUILayout.Width(64f)))
                        setProperty.objectReferenceValue = Duplicate(data.layerSet);
                }
            }

            m_DataObject.ApplyModifiedProperties();

            var set = data.layerSet;

            if (set == null)
            {
                m_Layers?.Release();
                m_Layers = null;
                EditorGUILayout.HelpBox("Pick a layer set, or press New to start one.", MessageType.Info);
                return;
            }

            if (m_Layers == null || m_Layers.set != set)
            {
                m_Layers?.Release();
                m_Layers = new StyleTerrainLayerList(set);
            }

            m_Layers.Draw(terrain, data);
        }

        void DrawSettings(HBStyleTerrainData data)
        {
            StyleTerrainGUI.Header("Bake Settings", StyleTerrainGUI.settingsTint);

            m_DataObject.Update();
            EditorGUILayout.IntPopup(m_DataObject.FindProperty("m_ControlResolution"), s_ControlNames, s_ControlSizes,
                                     s_ControlLabel);
            EditorGUILayout.IntPopup(m_DataObject.FindProperty("m_TextureResolution"), s_TextureNames, s_TextureSizes,
                                     s_TextureLabel);
            m_DataObject.ApplyModifiedProperties();

            StyleTerrainGUI.Header("Heights", StyleTerrainGUI.settingsTint);
            DrawHeights(data);
        }

        void DrawHeights(HBStyleTerrainData data)
        {
            if (data.terrainData != null)
            {
                var size = data.size;

                EditorGUILayout.LabelField("Size", $"{size.x:0} × {size.z:0} m, {size.y:0} m high, " +
                                                   $"{data.resolution}² samples");
            }
            else
            {
                EditorGUILayout.HelpBox("No heights yet. Import a Unity terrain or create a flat terrain.",
                                        MessageType.Info);
            }

            s_ShowImport = EditorGUILayout.Foldout(s_ShowImport, "Import From Unity Terrain", true);

            if (s_ShowImport)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    m_Source = (TerrainData)EditorGUILayout.ObjectField(s_SourceLabel, m_Source, typeof(TerrainData),
                                                                        false);

                    using (new EditorGUI.DisabledScope(m_Source == null))
                    {
                        if (GUILayout.Button("Import Heights And Holes") && StyleTerrainHeights.Square(m_Source) &&
                            StyleTerrainHeights.Confirm(data))
                            StyleTerrainHeights.Import(data, m_Source);
                    }
                }
            }

            s_ShowFlat = EditorGUILayout.Foldout(s_ShowFlat, "New Flat Terrain", true);

            if (!s_ShowFlat)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                m_Resolution = EditorGUILayout.Popup(s_ResolutionLabel, m_Resolution, s_ResolutionNames);
                m_Width = Mathf.Max(EditorGUILayout.FloatField(s_WidthLabel, m_Width), 1f);
                m_Height = Mathf.Max(EditorGUILayout.FloatField(s_HeightLabel, m_Height), 1f);

                if (GUILayout.Button("Create Flat Terrain") && StyleTerrainHeights.Confirm(data))
                {
                    StyleTerrainHeights.CreateFlat(data, s_Resolutions[m_Resolution],
                                                   new Vector3(m_Width, m_Height, m_Width));
                }
            }
        }

        static HBStyleTerrainLayerSet NewSet(HBStyleTerrainData data)
        {
            var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(data))?.Replace('\\', '/');
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{data.name} Layers.asset");
            var set = CreateInstance<HBStyleTerrainLayerSet>();

            AssetDatabase.CreateAsset(set, path);
            return set;
        }

        static HBStyleTerrainLayerSet Duplicate(HBStyleTerrainLayerSet set)
        {
            var path = AssetDatabase.GetAssetPath(set);
            var copy = AssetDatabase.GenerateUniqueAssetPath(path);

            return AssetDatabase.CopyAsset(path, copy) ? AssetDatabase.LoadAssetAtPath<HBStyleTerrainLayerSet>(copy)
                                                       : set;
        }
    }
}
