using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class CloudAtlasGeneratorWindow : EditorWindow
    {
        const int k_PreviewResolution = 320;
        const float k_MinPreviewSize = 180f;

        static readonly int[] k_Resolutions = { 256, 512, 1024, 2048 };
        static readonly string[] k_ResolutionLabels = { "256", "512", "1024", "2048" };
        static readonly string[] k_TileLabels = { "1 × 1", "2 × 2", "3 × 3", "4 × 4" };

        static class Styles
        {
            public static readonly GUIContent PuffScale = new GUIContent("Puff Scale",
                "How many billows fit across the tile. The single setting that decides whether the " +
                "sky reads as cumulus or as haze.");

            public static readonly GUIContent Puffiness = new GUIContent("Puffiness",
                "Blends between drifting noise and round billows. Low is stratus, high is cumulus.");

            public static readonly GUIContent ShapeScale = new GUIContent("Gathering",
                "Scale of the drift that decides where clouds gather and where the sky stays open.");

            public static readonly GUIContent Octaves = new GUIContent("Detail Levels",
                "How many levels of detail go into that drift.");

            public static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Output texture size. 512 is usually enough — the sky samples the atlas at two " +
                "scales, so detail comes from repetition rather than resolution.");

            public static readonly GUIContent Seed = new GUIContent("Seed",
                "The same seed always produces the same atlas.");

            public static readonly GUIContent Tiling = new GUIContent("Tiling",
                "Repeats the preview so the tile boundary can be inspected. A seam visible here " +
                "will be visible in the sky.");

            public static readonly GUIContent PreviewCoverage = new GUIContent("Coverage",
                "Preview only. Mirrors Cloud Coverage on the volume, so the same atlas can be " +
                "checked as an open sky and as an overcast one.");

            public static readonly GUIContent PreviewSoftness = new GUIContent("Softness",
                "Preview only. Mirrors Cloud Softness on the volume.");

            public const string ChannelHint =
                "RG store the relief the sun lights, B the height the detail layer parallaxes " +
                "against, and A the raw density the volume's Cloud Coverage thresholds. The " +
                "preview is lit from a fixed sun; in the scene the light comes from your own.";
        }

        [SerializeField] CloudAtlasSettings m_Settings = new CloudAtlasSettings();
        [SerializeField] int m_PreviewTiles = 2;
        [SerializeField] float m_PreviewCoverage = 0.5f;
        [SerializeField] float m_PreviewSoftness = 0.25f;
        [SerializeField] bool m_ShapeExpanded = true;
        [SerializeField] bool m_ReliefExpanded = true;
        [SerializeField] bool m_OutputExpanded;

        SerializedObject m_SerializedSelf;
        SerializedProperty m_SettingsProperty;

        Texture2D m_Preview;
        string m_PreviewSignature;
        float m_Coverage;
        Vector2 m_Scroll;

        [SerializeField] Texture2D m_Brush;

        float[] m_BrushPixels;
        int m_BrushResolution;
        Texture2D m_BrushRead;
        int m_BrushVersion;
        string m_Pending;

        struct CloudPreview
        {
            public Color[] pixels;
            public float coverage;
        }

        System.Threading.Tasks.Task<CloudPreview> m_Bake;

        void ReadBrush()
        {
            if (m_Brush == m_BrushRead)
                return;

            m_BrushRead = m_Brush;
            m_BrushVersion++;
            m_BrushPixels = null;
            m_BrushResolution = 0;

            if (m_Brush == null)
                return;

            var size = Mathf.Min(m_Brush.width, m_Brush.height);
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32,
                                                    RenderTextureReadWrite.Linear);

            Graphics.Blit(m_Brush, target);

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var readable = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            readable.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            readable.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            var pixels = readable.GetPixels();

            m_BrushPixels = new float[pixels.Length];
            m_BrushResolution = size;

            for (var i = 0; i < pixels.Length; i++)
                m_BrushPixels[i] = pixels[i].b;

            DestroyImmediate(readable);
        }

        [MenuItem("Tools/HiddenBull/URP Style/Cloud Atlas Generator")]
        static void Open()
        {
            var window = GetWindow<CloudAtlasGeneratorWindow>();
            window.titleContent = new GUIContent("Cloud Atlas");
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

            m_ShapeExpanded = Section("Shape", m_ShapeExpanded, DrawShape);
            m_ReliefExpanded = Section("Relief", m_ReliefExpanded, DrawRelief);
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
                var selected = EditorGUILayout.Popup(0, CloudAtlasPresets.Names);
                if (selected > 0)
                {
                    Undo.RecordObject(this, "Apply Cloud Preset");
                    CloudAtlasPresets.Apply(selected, m_Settings);

                    m_SerializedSelf.Update();
                    m_PreviewSignature = null;
                }

                if (GUILayout.Button("Randomise Seed", GUILayout.Width(120f)))
                {
                    Find(nameof(CloudAtlasSettings.seed)).intValue =
                        Random.Range(int.MinValue, int.MaxValue);
                }
            }

            EditorGUILayout.Space(2f);
        }

        void DrawShape()
        {
            var puffScale = Find(nameof(CloudAtlasSettings.puffScale));
            puffScale.intValue = EditorGUILayout.IntSlider(Styles.PuffScale, puffScale.intValue, 2, 16);

            EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.puffiness)), Styles.Puffiness);

            var shapeScale = Find(nameof(CloudAtlasSettings.shapeScale));
            shapeScale.intValue = EditorGUILayout.IntSlider(Styles.ShapeScale, shapeScale.intValue, 1, 12);

            var octaves = Find(nameof(CloudAtlasSettings.octaves));
            octaves.intValue = EditorGUILayout.IntSlider(Styles.Octaves, octaves.intValue, 1, 6);

            EditorGUILayout.Space(2f);

            var erosion = Find(nameof(CloudAtlasSettings.erosion));
            EditorGUILayout.PropertyField(erosion, new GUIContent("Erosion"));

            if (erosion.floatValue > 0f)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var erosionScale = Find(nameof(CloudAtlasSettings.erosionScale));
                    erosionScale.intValue = EditorGUILayout.IntSlider(
                        new GUIContent("Scale"), erosionScale.intValue, 2, 48);
                }
            }

            EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.contrast)),
                new GUIContent("Contrast"));

            EditorGUILayout.Space(2f);

            var warp = Find(nameof(CloudAtlasSettings.warp));
            EditorGUILayout.PropertyField(warp, new GUIContent("Warp"));

            if (warp.floatValue > 0f)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var warpScale = Find(nameof(CloudAtlasSettings.warpScale));
                    warpScale.intValue = EditorGUILayout.IntSlider(
                        new GUIContent("Scale"), warpScale.intValue, 1, 8);
                }
            }

            EditorGUILayout.Space(2f);

            m_Brush = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("Brush Atlas",
                    "A brush atlas generated by the Brush Atlas Generator. Its strokes are cut " +
                    "into the cloud's density before the atlas is baked, so they end up in the " +
                    "silhouette and in how solid the cloud is rather than sitting on top of it."),
                m_Brush, typeof(Texture2D), false);

            if (m_Brush == null)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.brushAmount)),
                    new GUIContent("Brush"));
                EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.brushScale)),
                    new GUIContent("Brush Scale"));
            }
        }

        void DrawRelief()
        {
            EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.reliefStrength)),
                new GUIContent("Strength"));
            EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.reliefSmoothing)),
                new GUIContent("Smoothing"));
        }

        void DrawOutput()
        {
            var resolution = Find(nameof(CloudAtlasSettings.resolution));
            var index = Mathf.Max(0, System.Array.IndexOf(k_Resolutions, resolution.intValue));
            index = EditorGUILayout.Popup(Styles.Resolution, index, k_ResolutionLabels);
            resolution.intValue = k_Resolutions[index];

            EditorGUILayout.PropertyField(Find(nameof(CloudAtlasSettings.seed)), Styles.Seed);
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

            m_PreviewCoverage = EditorGUILayout.Slider(Styles.PreviewCoverage, m_PreviewCoverage, 0f, 1f);
            m_PreviewSoftness = EditorGUILayout.Slider(Styles.PreviewSoftness, m_PreviewSoftness, 0.01f, 1f);

            RebuildPreviewIfNeeded();

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, m_Coverage, $"Sky covered {m_Coverage * 100f:0}%");

            if (m_Preview == null)
                return;

            var size = Mathf.Max(k_MinPreviewSize, EditorGUIUtility.currentViewWidth - 40f);
            var previewRect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            previewRect.x += (EditorGUIUtility.currentViewWidth - previewRect.width) * 0.5f - 4f;

            GUI.DrawTextureWithTexCoords(previewRect, m_Preview,
                new Rect(0f, 0f, m_PreviewTiles, m_PreviewTiles));

            EditorGUILayout.LabelField(
                m_PreviewTiles > 1
                    ? $"Lit from a fixed sun, {m_PreviewTiles} × {m_PreviewTiles} tiles — no seam should be visible."
                    : "Lit from a fixed sun, a single tile.",
                EditorStyles.miniLabel);
        }

        void RebuildPreviewIfNeeded()
        {
            ReadBrush();

            var signature = $"{JsonUtility.ToJson(m_Settings)}|{m_PreviewCoverage}" +
                            $"|{m_PreviewSoftness}|{m_BrushVersion}";

            if (signature != m_PreviewSignature)
            {
                m_PreviewSignature = signature;
                m_Pending = signature;
            }

            if (m_Pending != null && m_Bake == null)
            {
                var snapshot = JsonUtility.FromJson<CloudAtlasSettings>(
                    JsonUtility.ToJson(m_Settings));

                var brush = m_BrushPixels;
                var brushResolution = m_BrushResolution;
                var coverage = m_PreviewCoverage;
                var softness = m_PreviewSoftness;

                m_Pending = null;

                m_Bake = System.Threading.Tasks.Task.Run(() =>
                {
                    var atlas = CloudAtlasBaker.Bake(snapshot, k_PreviewResolution,
                                                     brush, brushResolution);

                    var shaded = CloudAtlasBaker.ShadePreview(
                        atlas, k_PreviewResolution, coverage, softness, out var visible);

                    return new CloudPreview { pixels = shaded, coverage = visible };
                });
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

            var shaded = completed.Result.pixels;
            m_Coverage = completed.Result.coverage;

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

            m_Preview.SetPixels(shaded);
            m_Preview.Apply();
        }

        void GenerateAndSave()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Cloud Atlas", "CloudAtlas", "png",
                "Choose where to save the generated cloud atlas.");

            if (string.IsNullOrEmpty(path))
                return;

            var resolution = m_Settings.resolution;

            try
            {
                EditorUtility.DisplayProgressBar("Cloud Atlas", "Building cloud field…", 0.4f);

                ReadBrush();

                var pixels = CloudAtlasBaker.Bake(m_Settings, resolution,
                                                  m_BrushPixels, m_BrushResolution);

                var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA64, false, true);
                texture.SetPixels(pixels);
                texture.Apply();

                var png = texture.EncodeToPNG();
                DestroyImmediate(texture);

                EditorUtility.DisplayProgressBar("Cloud Atlas", "Writing texture…", 0.8f);
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

            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;

            importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None;

            importer.filterMode = FilterMode.Bilinear;

            importer.anisoLevel = 8;

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
