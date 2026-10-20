using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class LiquidTextureGeneratorPage : StyleStudioPage
    {
        const int k_PreviewResolution = 256;
        const float k_MinPreviewSize = 180f;
        const string k_LiquidShader = "HB URP Style/Liquid";

        static readonly int[] k_Resolutions = { 256, 512, 1024, 2048 };
        static readonly string[] k_ResolutionLabels = { "256", "512", "1024", "2048" };

        static readonly Vector3 k_PreviewLight = new Vector3(0.45f, 0.55f, 0.7f).normalized;

        static class Styles
        {
            public static readonly GUIContent Mode = new GUIContent("Liquid",
                "Water makes rolling waves, Acid thick blobs and bubbles, Lava a churning, swirling " +
                "melt with a crust map the shader reads: bright where it has cooled into crust, dark " +
                "where it glows.");

            public static readonly GUIContent Scale = new GUIContent("Scale",
                "How many waves, blobs or swirls fit across one tile.");

            public static readonly GUIContent Strength = new GUIContent("Strength",
                "How steep the relief is. The material's Normal Strength scales it further.");

            public static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Output texture size. 512 is usually enough — the liquid samples the map at two " +
                "scales, so detail comes from layering rather than resolution.");

            public static readonly GUIContent Seed = new GUIContent("Seed",
                "The same seed always produces the same textures.");

            public const string Assign =
                "Select Liquid materials in the Project window before saving and the new textures " +
                "are assigned to them.";
        }

        [SerializeField] LiquidTextureMode m_Mode = LiquidTextureMode.Water;
        [SerializeField] int m_Scale = 6;
        [SerializeField] float m_Strength = 1f;
        [SerializeField] int m_Seed = 1;
        [SerializeField] int m_Resolution = 512;

        Texture2D m_Preview;
        string m_PreviewSignature;

        public override string title => "Liquid";

        void OnDisable()
        {
            if (m_Preview != null)
                DestroyImmediate(m_Preview);
        }

        public override void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            var mode = (LiquidTextureMode)EditorGUILayout.EnumPopup(Styles.Mode, m_Mode);

            if (mode != m_Mode)
            {
                Undo.RecordObject(this, "Liquid Texture Mode");
                m_Mode = mode;
                m_Scale = LiquidTextureBaker.DefaultScale(mode);
            }

            var scale = EditorGUILayout.IntSlider(Styles.Scale, m_Scale, 1, 16);
            var strength = EditorGUILayout.Slider(Styles.Strength, m_Strength, 0f, 3f);

            var index = Mathf.Max(0, System.Array.IndexOf(k_Resolutions, m_Resolution));
            var resolution = k_Resolutions[EditorGUILayout.Popup(Styles.Resolution, index, k_ResolutionLabels)];

            int seed;

            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField(Styles.Seed, m_Seed);

                if (GUILayout.Button("Randomise", GUILayout.Width(90f)))
                    seed = Random.Range(0, 100000);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Liquid Texture Settings");
                m_Scale = scale;
                m_Strength = strength;
                m_Resolution = resolution;
                m_Seed = seed;
            }

            EditorGUILayout.Space();
            DrawPreview();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.Assign, MessageType.None);

            if (GUILayout.Button("Generate and Save…", GUILayout.Height(28f)))
                GenerateAndSave();
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
            var signature = $"{m_Mode}|{m_Scale}|{m_Strength}|{m_Seed}";

            if (signature == m_PreviewSignature && m_Preview != null)
                return;

            m_PreviewSignature = signature;

            var result = LiquidTextureBaker.Bake(m_Mode, m_Scale, m_Strength, m_Seed, k_PreviewResolution);

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

        Color[] Shade(LiquidTextureBaker.Result result)
        {
            var pixels = new Color[result.normal.Length];

            for (var i = 0; i < pixels.Length; i++)
            {
                var encoded = result.normal[i];
                var normal = new Vector3(encoded.r * 2f - 1f, encoded.g * 2f - 1f, encoded.b * 2f - 1f);
                var light = Mathf.Clamp01(Vector3.Dot(normal, k_PreviewLight)) * 0.75f + 0.25f;

                Color colour;

                switch (m_Mode)
                {
                    case LiquidTextureMode.Lava:
                        var crust = result.crust[i].r;
                        colour = Color.Lerp(new Color(1f, 0.45f, 0.08f), new Color(0.2f, 0.14f, 0.12f) * light,
                                            Mathf.SmoothStep(0f, 1f, crust));
                        break;
                    case LiquidTextureMode.Acid:
                        colour = new Color(0.45f, 0.85f, 0.2f) * light;
                        break;
                    default:
                        colour = new Color(0.25f, 0.55f, 0.75f) * light;
                        break;
                }

                colour.a = 1f;
                pixels[i] = colour;
            }

            return pixels;
        }

        void GenerateAndSave()
        {
            var targets = Selection.GetFiltered<Material>(SelectionMode.Assets)
                                   .Where(material => material.shader != null &&
                                                      material.shader.name == k_LiquidShader)
                                   .ToArray();

            var folder = StyleTextureNames.PickFolder("Save Liquid Textures");

            if (folder == null)
                return;

            var normalPattern = $"TEX_Liquid_{m_Mode}_Normal_{{0}}.png";
            var crustPattern = $"TEX_Liquid_{m_Mode}_Crust_{{0}}.png";
            var index = StyleTextureNames.FreeIndex(folder, normalPattern, crustPattern);
            var normalPath = StyleTextureNames.Path(folder, normalPattern, index);
            var crustPath = StyleTextureNames.Path(folder, crustPattern, index);

            LiquidTextureBaker.Result result;

            try
            {
                EditorUtility.DisplayProgressBar("Liquid Textures", "Building relief…", 0.4f);
                result = LiquidTextureBaker.Bake(m_Mode, m_Scale, m_Strength, m_Seed, m_Resolution);

                EditorUtility.DisplayProgressBar("Liquid Textures", "Writing textures…", 0.8f);
                Write(normalPath, result.normal, m_Resolution);

                if (result.crust != null)
                    Write(crustPath, result.crust, m_Resolution);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var normal = Import(normalPath, true);
            var crustMap = result.crust != null ? Import(crustPath, false) : null;

            foreach (var material in targets)
            {
                Undo.RecordObject(material, "Assign Liquid Textures");
                material.SetTexture("_BumpMap", normal);

                if (crustMap != null)
                    material.SetTexture("_BaseMap", crustMap);

                EditorUtility.SetDirty(material);
            }

            EditorGUIUtility.PingObject(normal);
        }

        void Write(string path, Color[] pixels, int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            DestroyImmediate(texture);
        }

        Texture2D Import(string path, bool normalMap)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(m_Resolution), 32, 8192);
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
