using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class TerrainTextureGeneratorPage : StyleStudioPage
    {
        const int k_PreviewResolution = 256;
        const float k_MinPreviewSize = 180f;

        static readonly int[] k_Resolutions = { 256, 512, 1024, 2048 };
        static readonly string[] k_ResolutionLabels = { "256", "512", "1024", "2048" };

        static readonly Vector3 k_PreviewLight = new Vector3(0.45f, 0.55f, 0.7f).normalized;

        static class Styles
        {
            public static readonly GUIContent Kind = new GUIContent("Surface",
                "Rock makes cracked plates, Dirt crumbly soil with pebbles, Sand wind ripples, Grass Ground " +
                "tufts with bare soil patches, Gravel packed stones, Mud wet or cracked ground, Forest Floor " +
                "fallen leaves or needles, Cobblestone laid stones. Changing it loads that surface's first preset.");

            public static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Output texture size. All maps tile seamlessly at any size.");

            public static readonly GUIContent Smoothness = new GUIContent("Smoothness",
                "Smoothness of the roughest and the smoothest parts. Stored in the albedo alpha and in the " +
                "mask map.");

            public static readonly GUIContent CrackColour = new GUIContent("Crack Color",
                "Colour inside the open cracks.");

            public static readonly GUIContent PebbleColour = new GUIContent("Pebble Color",
                "Colour of the pebbles lying in the soil.");

            public static readonly GUIContent SpeckColour = new GUIContent("Speck Color",
                "Colour of the mineral specks.");

            public static readonly GUIContent SoilColour = new GUIContent("Soil Color",
                "Colour of the bare soil patches.");

            public static readonly GUIContent GapColour = new GUIContent("Gap Color",
                "Colour of the dirt between the stones.");

            public static readonly GUIContent PuddleColour = new GUIContent("Puddle Color",
                "Colour of the standing water in the hollows.");

            public static readonly GUIContent LeafColour = new GUIContent("Second Leaf Color",
                "About half of the leaves take this colour instead of the Top and Hollow colours.");

            public static readonly GUIContent MortarColour = new GUIContent("Mortar Color",
                "Colour of the mortar between the stones.");

            public const string Output =
                "Saves an albedo map (alpha holds smoothness), a normal map and, if enabled, a mask map. They " +
                "can also be used as an automatic layer's Map and Normal on the Terrain material.";
        }

        static readonly StyleStudioPresets s_Presets = new StyleStudioPresets("Terrain");

        [SerializeField] TerrainTextureSettings m_Settings = new TerrainTextureSettings();
        [SerializeField] string m_Preset;
        [SerializeField] bool m_ShapeExpanded = true;
        [SerializeField] bool m_ColourExpanded = true;
        [SerializeField] bool m_DetailExpanded;
        [SerializeField] bool m_OutputExpanded;

        SerializedObject m_SerializedSelf;
        SerializedProperty m_SettingsProperty;

        Texture2D m_Preview;
        string m_PreviewSignature;

        public override string title => "Terrain";

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

        public override void OnGUI()
        {
            m_SerializedSelf.Update();

            DrawPresets();

            m_ShapeExpanded = Section("Shape", m_ShapeExpanded, DrawShape);
            m_ColourExpanded = Section("Color", m_ColourExpanded, DrawColour);
            m_DetailExpanded = Section("Detail", m_DetailExpanded, DrawDetail);
            m_OutputExpanded = Section("Output", m_OutputExpanded, DrawOutput);

            m_SerializedSelf.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawPreview();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.Output, MessageType.None);

            if (GUILayout.Button("Generate and Save…", GUILayout.Height(28f)))
                GenerateAndSave();
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

        SerializedProperty Find(string name)
        {
            return m_SettingsProperty.FindPropertyRelative(name);
        }

        void Field(string name)
        {
            EditorGUILayout.PropertyField(Find(name));
        }

        void Load(System.Action<TerrainTextureSettings> apply, string undo)
        {
            m_SerializedSelf.ApplyModifiedProperties();
            Undo.RecordObject(this, undo);
            apply(m_Settings);

            m_SerializedSelf.Update();
            m_PreviewSignature = null;
        }

        void LoadPreset(string name)
        {
            Load(settings =>
            {
                var resolution = settings.resolution;
                var maskMap = settings.maskMap;
                var createLayer = settings.createLayer;

                s_Presets.Load(name, settings);

                settings.resolution = resolution;
                settings.maskMap = maskMap;
                settings.createLayer = createLayer;
            }, "Apply Terrain Preset");
        }

        void LoadFirst(TerrainTextureKind kind)
        {
            var probe = new TerrainTextureSettings();

            foreach (var name in s_Presets.names)
            {
                if (s_Presets.Load(name, probe) && probe.kind == kind)
                {
                    m_Preset = name;
                    LoadPreset(name);
                    return;
                }
            }

            m_Preset = null;
            Load(settings => settings.kind = kind, "Terrain Texture Surface");
        }

        void DrawPresets()
        {
            var kind = (TerrainTextureKind)EditorGUILayout.EnumPopup(Styles.Kind, m_Settings.kind);

            if (kind != m_Settings.kind)
                LoadFirst(kind);

            s_Presets.Draw(ref m_Preset, m_Settings, LoadPreset);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Randomise Seed", GUILayout.Width(120f)))
                    Find(nameof(TerrainTextureSettings.seed)).intValue = Random.Range(0, 100000);
            }

            EditorGUILayout.Space(2f);
        }

        void DrawShape()
        {
            switch (m_Settings.kind)
            {
                case TerrainTextureKind.Rock:
                    Field(nameof(TerrainTextureSettings.plateCount));
                    Field(nameof(TerrainTextureSettings.crackWidth));
                    Field(nameof(TerrainTextureSettings.crackDepth));
                    Field(nameof(TerrainTextureSettings.crackCoverage));
                    Field(nameof(TerrainTextureSettings.plateStep));
                    Field(nameof(TerrainTextureSettings.strata));
                    break;
                case TerrainTextureKind.Dirt:
                    Field(nameof(TerrainTextureSettings.clodCount));
                    Field(nameof(TerrainTextureSettings.crumbs));
                    Field(nameof(TerrainTextureSettings.pebbleCount));
                    Field(nameof(TerrainTextureSettings.pebbleAmount));
                    Field(nameof(TerrainTextureSettings.pebbleSize));
                    break;
                case TerrainTextureKind.Sand:
                    Field(nameof(TerrainTextureSettings.rippleCount));
                    Field(nameof(TerrainTextureSettings.rippleSharpness));
                    Field(nameof(TerrainTextureSettings.rippleBend));
                    Field(nameof(TerrainTextureSettings.duneCount));
                    Field(nameof(TerrainTextureSettings.duneAmount));
                    Field(nameof(TerrainTextureSettings.speckle));
                    break;
                case TerrainTextureKind.GrassGround:
                    Field(nameof(TerrainTextureSettings.patchCount));
                    Field(nameof(TerrainTextureSettings.soilAmount));
                    Field(nameof(TerrainTextureSettings.soilSoftness));
                    Field(nameof(TerrainTextureSettings.tuftCount));
                    Field(nameof(TerrainTextureSettings.tuftHeight));
                    break;
                case TerrainTextureKind.Gravel:
                    Field(nameof(TerrainTextureSettings.stoneCount));
                    Field(nameof(TerrainTextureSettings.stoneRoundness));
                    Field(nameof(TerrainTextureSettings.gapWidth));
                    Field(nameof(TerrainTextureSettings.stoneHeightVariation));
                    break;
                case TerrainTextureKind.Mud:
                    Field(nameof(TerrainTextureSettings.mudCount));
                    Field(nameof(TerrainTextureSettings.puddles));
                    Field(nameof(TerrainTextureSettings.cracks));

                    using (new EditorGUI.DisabledScope(m_Settings.cracks <= 0f))
                    using (new EditorGUI.IndentLevelScope())
                        Field(nameof(TerrainTextureSettings.crackCells));

                    break;
                case TerrainTextureKind.ForestFloor:
                    Field(nameof(TerrainTextureSettings.leafTypes));
                    Field(nameof(TerrainTextureSettings.leafCount));
                    Field(nameof(TerrainTextureSettings.leafSize));
                    Field(nameof(TerrainTextureSettings.leafShape));
                    Field(nameof(TerrainTextureSettings.leafVeins));
                    Field(nameof(TerrainTextureSettings.leafCurl));
                    Field(nameof(TerrainTextureSettings.twigs));
                    break;
                default:
                    Field(nameof(TerrainTextureSettings.cobbleRows));
                    Field(nameof(TerrainTextureSettings.cobbleAspect));
                    Field(nameof(TerrainTextureSettings.cobbleJitter));
                    Field(nameof(TerrainTextureSettings.mortarWidth));
                    Field(nameof(TerrainTextureSettings.cobbleRoundness));
                    break;
            }
        }

        GUIContent SecondColourLabel()
        {
            switch (m_Settings.kind)
            {
                case TerrainTextureKind.Rock: return Styles.CrackColour;
                case TerrainTextureKind.Dirt: return Styles.PebbleColour;
                case TerrainTextureKind.Sand: return Styles.SpeckColour;
                case TerrainTextureKind.GrassGround: return Styles.SoilColour;
                case TerrainTextureKind.Gravel: return Styles.GapColour;
                case TerrainTextureKind.Mud: return Styles.PuddleColour;
                case TerrainTextureKind.ForestFloor: return Styles.LeafColour;
                default: return Styles.MortarColour;
            }
        }

        void DrawColour()
        {
            Field(nameof(TerrainTextureSettings.topColor));
            Field(nameof(TerrainTextureSettings.hollowColor));
            EditorGUILayout.PropertyField(Find(nameof(TerrainTextureSettings.secondColor)), SecondColourLabel());

            EditorGUILayout.Space(2f);

            Field(nameof(TerrainTextureSettings.heightTint));
            Field(nameof(TerrainTextureSettings.cavityDarkening));
            Field(nameof(TerrainTextureSettings.paintedLight));
            Field(nameof(TerrainTextureSettings.warmCool));
            Field(nameof(TerrainTextureSettings.edgeHighlight));
            Field(nameof(TerrainTextureSettings.toneVariation));

            using (new EditorGUI.DisabledScope(m_Settings.toneVariation <= 0f))
            using (new EditorGUI.IndentLevelScope())
                Field(nameof(TerrainTextureSettings.tonePatches));

            var smoothness = Find(nameof(TerrainTextureSettings.smoothness));
            var range = smoothness.vector2Value;
            var min = Mathf.Min(range.x, range.y);
            var max = Mathf.Max(range.x, range.y);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.MinMaxSlider(Styles.Smoothness, ref min, ref max, 0f, 1f);
                min = Mathf.Clamp01(EditorGUILayout.FloatField(min, GUILayout.Width(40f)));
                max = Mathf.Clamp01(EditorGUILayout.FloatField(max, GUILayout.Width(40f)));
            }

            smoothness.vector2Value = new Vector2(min, Mathf.Max(min, max));
        }

        void DrawDetail()
        {
            Field(nameof(TerrainTextureSettings.detailLevels));
            Field(nameof(TerrainTextureSettings.warp));
            Field(nameof(TerrainTextureSettings.grain));

            using (new EditorGUI.DisabledScope(m_Settings.grain <= 0f))
            using (new EditorGUI.IndentLevelScope())
                Field(nameof(TerrainTextureSettings.grainCount));

            Field(nameof(TerrainTextureSettings.normalStrength));
            Field(nameof(TerrainTextureSettings.painterly));

            using (new EditorGUI.DisabledScope(m_Settings.painterly <= 0f))
            using (new EditorGUI.IndentLevelScope())
            {
                Field(nameof(TerrainTextureSettings.toneBands));
                Field(nameof(TerrainTextureSettings.painterlyNormal));
            }
        }

        void DrawOutput()
        {
            var resolution = Find(nameof(TerrainTextureSettings.resolution));
            var index = Mathf.Max(0, System.Array.IndexOf(k_Resolutions, resolution.intValue));
            index = EditorGUILayout.Popup(Styles.Resolution, index, k_ResolutionLabels);
            resolution.intValue = k_Resolutions[index];

            Field(nameof(TerrainTextureSettings.seed));
            Field(nameof(TerrainTextureSettings.maskMap));

            using (new EditorGUI.DisabledScope(!m_Settings.maskMap))
            using (new EditorGUI.IndentLevelScope())
                Field(nameof(TerrainTextureSettings.occlusion));

            Field(nameof(TerrainTextureSettings.createLayer));

            using (new EditorGUI.DisabledScope(!m_Settings.createLayer))
            using (new EditorGUI.IndentLevelScope())
            {
                var tileSize = Find(nameof(TerrainTextureSettings.tileSize));
                EditorGUILayout.PropertyField(tileSize);
                tileSize.floatValue = Mathf.Max(0.1f, tileSize.floatValue);
            }
        }

        void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            RebuildPreviewIfNeeded();

            if (m_Preview == null)
                return;

            var size = Mathf.Max(k_MinPreviewSize, EditorGUIUtility.currentViewWidth - 40f);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            rect.x += (EditorGUIUtility.currentViewWidth - rect.width) * 0.5f - 4f;

            GUI.DrawTextureWithTexCoords(rect, m_Preview, new Rect(0f, 0f, 2f, 2f));

            EditorGUILayout.LabelField("Lit from a fixed light, 2 × 2 tiles — no seam should be visible.",
                                       EditorStyles.miniLabel);
        }

        void RebuildPreviewIfNeeded()
        {
            var signature = JsonUtility.ToJson(m_Settings);

            if (signature == m_PreviewSignature && m_Preview != null)
                return;

            m_PreviewSignature = signature;

            var result = TerrainTextureBaker.Bake(m_Settings, k_PreviewResolution);

            if (m_Preview == null)
            {
                m_Preview = new Texture2D(k_PreviewResolution, k_PreviewResolution, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
            }

            m_Preview.SetPixels(Shade(result));
            m_Preview.Apply();
        }

        static Color[] Shade(TerrainTextureBaker.Result result)
        {
            var pixels = new Color[result.albedo.Length];

            for (var i = 0; i < pixels.Length; i++)
            {
                var encoded = result.normal[i];
                var normal = new Vector3(encoded.r * 2f - 1f, encoded.g * 2f - 1f, encoded.b * 2f - 1f);
                var light = Mathf.Clamp01(Vector3.Dot(normal, k_PreviewLight)) * 0.8f + 0.3f;
                var colour = result.albedo[i] * light;

                colour.a = 1f;
                pixels[i] = colour;
            }

            return pixels;
        }

        void GenerateAndSave()
        {
            var settings = m_Settings;
            var folder = StyleTextureNames.PickFolder("Save Terrain Textures");

            if (folder == null)
                return;

            var albedoPattern = $"TEX_Terrain_{settings.kind}_Albedo_{{0}}.png";
            var normalPattern = $"TEX_Terrain_{settings.kind}_Normal_{{0}}.png";
            var maskPattern = $"TEX_Terrain_{settings.kind}_Mask_{{0}}.png";
            var layerPattern = $"TL_Terrain_{settings.kind}_{{0}}.terrainlayer";
            var index = StyleTextureNames.FreeIndex(folder, albedoPattern, normalPattern, maskPattern, layerPattern);

            var albedoPath = StyleTextureNames.Path(folder, albedoPattern, index);
            var normalPath = StyleTextureNames.Path(folder, normalPattern, index);
            var maskPath = StyleTextureNames.Path(folder, maskPattern, index);
            var layerPath = StyleTextureNames.Path(folder, layerPattern, index);

            try
            {
                EditorUtility.DisplayProgressBar("Terrain Textures", "Building surface…", 0.4f);
                var result = TerrainTextureBaker.Bake(settings, settings.resolution);

                EditorUtility.DisplayProgressBar("Terrain Textures", "Writing textures…", 0.8f);
                Write(albedoPath, result.albedo, settings.resolution);
                Write(normalPath, result.normal, settings.resolution);

                if (result.mask != null)
                    Write(maskPath, result.mask, settings.resolution);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var albedo = Import(albedoPath, TextureImporterType.Default, true);
            var normal = Import(normalPath, TextureImporterType.NormalMap, false);
            var mask = settings.maskMap ? Import(maskPath, TextureImporterType.Default, false) : null;

            if (!settings.createLayer)
            {
                EditorGUIUtility.PingObject(albedo);
                return;
            }

            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            var created = layer == null;

            if (created)
                layer = new TerrainLayer();
            else
                Undo.RecordObject(layer, "Update Terrain Layer");

            layer.diffuseTexture = albedo;
            layer.normalMapTexture = normal;
            layer.maskMapTexture = mask;
            layer.normalScale = 1f;
            layer.smoothness = 1f;
            layer.metallic = 0f;
            layer.tileSize = new Vector2(settings.tileSize, settings.tileSize);
            layer.tileOffset = Vector2.zero;

            if (created)
                AssetDatabase.CreateAsset(layer, layerPath);
            else
                EditorUtility.SetDirty(layer);

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(layer);
        }

        static void Write(string path, Color[] pixels, int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            DestroyImmediate(texture);
        }

        Texture2D Import(string path, TextureImporterType type, bool colour)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = type;
                importer.sRGBTexture = colour;
                importer.alphaSource = type == TextureImporterType.NormalMap
                    ? TextureImporterAlphaSource.None
                    : TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(m_Settings.resolution), 32, 8192);
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
