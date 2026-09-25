using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class BrushAtlasGeneratorWindow : EditorWindow
    {
        const int k_PreviewResolution = 320;
        const float k_MinPreviewSize = 180f;

        static readonly int[] k_Resolutions = { 256, 512, 1024, 2048 };
        static readonly string[] k_ResolutionLabels = { "256", "512", "1024", "2048" };
        static readonly string[] k_TileLabels = { "1 × 1", "2 × 2", "3 × 3", "4 × 4" };

        static class Styles
        {
            public static readonly GUIContent StrokeCount = new GUIContent("Count",
                "Number of strokes laid down. Watch the coverage readout below — around 50% is " +
                "where gaps and strokes are symmetric.");

            public static readonly GUIContent LengthRange = new GUIContent("Length",
                "Shortest and longest stroke, as a fraction of the texture.");

            public static readonly GUIContent WidthRange = new GUIContent("Width",
                "Thinnest and thickest stroke, as a fraction of the texture.");

            public static readonly GUIContent Angle = new GUIContent("Angle",
                "Dominant stroke direction. A dominant angle is a large part of why a surface " +
                "reads as painted rather than noisy.");

            public static readonly GUIContent AngleJitter = new GUIContent("Angle Jitter",
                "How far strokes deviate from that direction. Zero is a rigid hatch; 180 has no " +
                "direction at all.");

            public static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Output texture size. 512 is usually enough — the atlas tiles, so detail comes " +
                "from repetition rather than resolution.");

            public static readonly GUIContent Seed = new GUIContent("Seed",
                "The same seed always produces the same atlas.");

            public static readonly GUIContent Tiling = new GUIContent("Tiling",
                "Repeats the preview so the tile boundary can be inspected. A seam visible here " +
                "will be visible in the scene.");

            public const string ChannelHint =
                "RG store the warp vector that breaks up shadow edges; B stores stroke coverage, " +
                "centred so brush strength redistributes brightness rather than darkening the surface.";
        }

        [SerializeField] BrushAtlasSettings m_Settings = new BrushAtlasSettings();
        [SerializeField] int m_PreviewTiles = 2;
        [SerializeField] bool m_StrokesExpanded = true;
        [SerializeField] bool m_DetailExpanded = true;
        [SerializeField] bool m_OutputExpanded;

        SerializedObject m_SerializedSelf;
        SerializedProperty m_SettingsProperty;

        Texture2D m_Preview;
        string m_PreviewSignature;
        string m_Pending;
        float m_Coverage;
        Vector2 m_Scroll;

        System.Threading.Tasks.Task<BrushAtlasResult> m_Bake;

        [MenuItem("Tools/HiddenBull/URP Style/Brush Atlas Generator")]
        static void Open()
        {
            var window = GetWindow<BrushAtlasGeneratorWindow>();
            window.titleContent = new GUIContent("Brush Atlas");
            window.minSize = new Vector2(400f, 640f);
            window.Show();
        }

        void OnEnable()
        {
            m_SerializedSelf = new SerializedObject(this);
            m_SettingsProperty = m_SerializedSelf.FindProperty(nameof(m_Settings));

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (m_Preview != null)
                DestroyImmediate(m_Preview);
        }

        void OnUndoRedo()
        {
            m_SerializedSelf?.Update();
            m_PreviewSignature = null;
            Repaint();
        }

        void OnGUI()
        {
            m_SerializedSelf.Update();
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            DrawPresets();

            m_StrokesExpanded = Section("Strokes", m_StrokesExpanded, DrawStrokes);
            m_DetailExpanded = Section("Brush Detail", m_DetailExpanded, DrawDetail);
            m_OutputExpanded = Section("Output", m_OutputExpanded, DrawOutput);

            m_SerializedSelf.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawPreview();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.ChannelHint, MessageType.None);

            if (GUILayout.Button("Generate and Save…", GUILayout.Height(28f)))
                GenerateAndSave();

            EditorGUILayout.EndScrollView();
        }

        static bool Section(string title, bool expanded, System.Action body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);

                if (expanded)
                {
                    EditorGUILayout.Space(2f);
                    body();
                }
            }

            return expanded;
        }

        void DrawPresets()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var selected = EditorGUILayout.Popup(0, BrushAtlasPresets.Names);
                if (selected > 0)
                {
                    Undo.RecordObject(this, "Apply Brush Preset");
                    BrushAtlasPresets.Apply(selected, m_Settings);

                    m_SerializedSelf.Update();
                    m_PreviewSignature = null;
                }

                if (GUILayout.Button("Randomise Seed", GUILayout.Width(120f)))
                {
                    Find(nameof(BrushAtlasSettings.seed)).intValue =
                        Random.Range(int.MinValue, int.MaxValue);
                }
            }

            EditorGUILayout.Space(2f);
        }

        void DrawStrokes()
        {
            var strokeCount = Find(nameof(BrushAtlasSettings.strokeCount));
            strokeCount.intValue = EditorGUILayout.IntSlider(Styles.StrokeCount, strokeCount.intValue, 10, 800);

            DrawRange(Find(nameof(BrushAtlasSettings.lengthRange)), Styles.LengthRange, 0.01f, 0.6f);
            DrawRange(Find(nameof(BrushAtlasSettings.widthRange)), Styles.WidthRange, 0.002f, 0.25f);

            var angle = Find(nameof(BrushAtlasSettings.angle));
            angle.floatValue = EditorGUILayout.Slider(Styles.Angle, angle.floatValue, -180f, 180f);

            var jitter = Find(nameof(BrushAtlasSettings.angleJitter));
            jitter.floatValue = EditorGUILayout.Slider(Styles.AngleJitter, jitter.floatValue, 0f, 180f);

            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.taper)),
                new GUIContent("Taper"));
            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.edgeSoftness)),
                new GUIContent("Edge Softness"));
            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.curvature)),
                new GUIContent("Curvature"));
            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.opacityVariation)),
                new GUIContent("Opacity Variation"));
            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.toneVariation)),
                new GUIContent("Tone Variation"));
        }

        void DrawDetail()
        {
            DrawLayer("Bristles",
                Find(nameof(BrushAtlasSettings.bristleAmount)),
                Find(nameof(BrushAtlasSettings.bristleDensity)), "Density");

            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.bristleBreakup)),
                new GUIContent("Bristle Break-up"));

            DrawLayer("Edge Break-up",
                Find(nameof(BrushAtlasSettings.edgeBreakup)),
                Find(nameof(BrushAtlasSettings.edgeBreakupScale)), "Rate");

            DrawLayer("Pigment Density",
                Find(nameof(BrushAtlasSettings.pigmentAmount)),
                Find(nameof(BrushAtlasSettings.pigmentScale)), "Scale");

            DrawLayer("Canvas Grain",
                Find(nameof(BrushAtlasSettings.canvasAmount)),
                Find(nameof(BrushAtlasSettings.canvasScale)), "Scale");

            EditorGUILayout.Space(2f);
            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.warpSpread)),
                new GUIContent("Warp Spread"));
        }

        static void DrawLayer(string title, SerializedProperty amount, SerializedProperty scale,
                              string scaleLabel)
        {
            EditorGUILayout.PropertyField(amount, new GUIContent(title));

            if (amount.floatValue > 0f)
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.PropertyField(scale, new GUIContent(scaleLabel));
            }
        }

        void DrawOutput()
        {
            var resolution = Find(nameof(BrushAtlasSettings.resolution));
            var index = Mathf.Max(0, System.Array.IndexOf(k_Resolutions, resolution.intValue));
            index = EditorGUILayout.Popup(Styles.Resolution, index, k_ResolutionLabels);
            resolution.intValue = k_Resolutions[index];

            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.contrast)),
                new GUIContent("Contrast"));
            EditorGUILayout.PropertyField(Find(nameof(BrushAtlasSettings.seed)), Styles.Seed);
        }

        static void DrawRange(SerializedProperty property, GUIContent label, float limitMin, float limitMax)
        {
            var value = property.vector2Value;
            var min = Mathf.Min(value.x, value.y);
            var max = Mathf.Max(value.x, value.y);

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, label);

            const float fieldWidth = 46f;
            const float gap = 4f;

            var minRect = new Rect(rect.x, rect.y, fieldWidth, rect.height);
            var sliderRect = new Rect(rect.x + fieldWidth + gap, rect.y,
                rect.width - (fieldWidth + gap) * 2f, rect.height);
            var maxRect = new Rect(rect.xMax - fieldWidth, rect.y, fieldWidth, rect.height);

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                min = EditorGUI.FloatField(minRect, min);
                EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, limitMin, limitMax);
                max = EditorGUI.FloatField(maxRect, max);
            }

            min = Mathf.Clamp(min, limitMin, limitMax);
            max = Mathf.Clamp(max, min, limitMax);

            var clamped = new Vector2(min, max);
            if (clamped != value)
                property.vector2Value = clamped;
        }

        void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel, GUILayout.Width(60f));
                GUILayout.FlexibleSpace();

                EditorGUIUtility.labelWidth = 48f;
                m_PreviewTiles = EditorGUILayout.Popup(Styles.Tiling, m_PreviewTiles - 1,
                    k_TileLabels, GUILayout.Width(120f)) + 1;
                EditorGUIUtility.labelWidth = 0f;
            }

            RebuildPreviewIfNeeded();
            DrawCoverage();

            if (m_Preview == null)
                return;

            var size = Mathf.Max(k_MinPreviewSize, EditorGUIUtility.currentViewWidth - 40f);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            rect.x += (EditorGUIUtility.currentViewWidth - rect.width) * 0.5f - 4f;

            GUI.DrawTextureWithTexCoords(rect, m_Preview,
                new Rect(0f, 0f, m_PreviewTiles, m_PreviewTiles));

            EditorGUILayout.LabelField(
                m_PreviewTiles > 1
                    ? $"Stroke coverage, {m_PreviewTiles} × {m_PreviewTiles} tiles — no seam should be visible."
                    : "Stroke coverage, a single tile.",
                EditorStyles.miniLabel);
        }

        void DrawCoverage()
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, m_Coverage, $"Coverage {m_Coverage * 100f:0}%");

            if (m_Coverage > 0.72f)
            {
                EditorGUILayout.HelpBox(
                    "Coverage is high: the gaps will take most of the contrast and the strokes " +
                    "will read as dark cracks. Fewer or thinner strokes bring it back.",
                    MessageType.Warning);
            }
            else if (m_Coverage < 0.2f)
            {
                EditorGUILayout.HelpBox(
                    "Coverage is low: isolated strokes on a flat field. More or wider strokes " +
                    "fill it out.",
                    MessageType.Warning);
            }
        }

        void RebuildPreviewIfNeeded()
        {
            var signature = JsonUtility.ToJson(m_Settings);

            if (signature != m_PreviewSignature)
            {
                m_PreviewSignature = signature;
                m_Pending = signature;
            }

            if (m_Pending != null && m_Bake == null)
            {
                var snapshot = JsonUtility.FromJson<BrushAtlasSettings>(m_Pending);

                m_Pending = null;

                m_Bake = System.Threading.Tasks.Task.Run(
                    () => BrushAtlasBaker.BakeResult(snapshot, k_PreviewResolution));
            }

            if (m_Bake == null)
                return;

            if (!m_Bake.IsCompleted)
            {
                Repaint();
                return;
            }

            var completed = m_Bake;
            m_Bake = null;

            if (completed.IsFaulted)
            {
                Debug.LogException(completed.Exception);
                return;
            }

            var pixels = completed.Result.pixels;
            m_Coverage = completed.Result.stats.coverage;

            if (m_Preview == null)
            {
                m_Preview = new Texture2D(k_PreviewResolution, k_PreviewResolution,
                    TextureFormat.RGBA32, false, true)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
            }

            m_Preview.SetPixels(BrushAtlasBaker.ExtractCoveragePreview(pixels));
            m_Preview.Apply();
        }

        void GenerateAndSave()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Brush Atlas", "BrushAtlas", "png",
                "Choose where to save the generated brush atlas.");

            if (string.IsNullOrEmpty(path))
                return;

            var resolution = m_Settings.resolution;

            try
            {
                EditorUtility.DisplayProgressBar("Brush Atlas", "Laying down strokes…", 0.4f);

                var pixels = BrushAtlasBaker.Bake(m_Settings, resolution);

                var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA64, false, true);
                texture.SetPixels(pixels);
                texture.Apply();

                var png = texture.EncodeToPNG();
                DestroyImmediate(texture);

                EditorUtility.DisplayProgressBar("Brush Atlas", "Writing texture…", 0.8f);
                File.WriteAllBytes(path, png);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path, resolution);

            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        static void ConfigureImporter(string path, int resolution)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;

            importer.sRGBTexture = false;
            importer.ignorePngGamma = true;

            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;

            importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None;

            importer.filterMode = FilterMode.Bilinear;

            importer.anisoLevel = 4;

            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.streamingMipmaps = false;

            importer.borderMipmap = false;
            importer.fadeout = false;

            importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(resolution), 32, 8192);

            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.crunchedCompression = false;

            importer.isReadable = false;

            importer.SaveAndReimport();
        }

        SerializedProperty Find(string name) => m_SettingsProperty.FindPropertyRelative(name);
    }
}
