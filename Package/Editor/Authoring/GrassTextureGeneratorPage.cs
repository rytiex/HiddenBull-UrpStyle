using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class GrassTextureGeneratorPage : StyleStudioPage
    {
        enum Kind
        {
            GrassClump,
            Flowers
        }

        const int k_PreviewResolution = 256;
        const float k_MinPreviewSize = 180f;
        const float k_AlphaCutoff = 0.4f;
        const string k_ClumpProperty = "_GrassTexture";
        const string k_FlowerProperty = "_FlowerTexture";
        const string k_VariantsProperty = "_FlowerVariants";

        static readonly int[] k_Resolutions = { 128, 256, 512, 1024 };
        static readonly string[] k_ResolutionLabels = { "128", "256", "512", "1024" };
        static readonly Color k_Backdrop = new Color(0.16f, 0.2f, 0.12f, 1f);
        static readonly Color k_PreviewGrass = new Color(0.45f, 0.65f, 0.2f, 1f);

        static class Styles
        {
            public static readonly GUIContent Kind = new GUIContent("Texture",
                "Grass Clump draws one tuft of blades for the terrain's Clump Texture. Flowers draws a row of " +
                "flower pictures for the terrain's Flower Texture.");

            public static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of one picture. Flowers are saved side by side, so the file is this wide times the " +
                "number of variants.");

            public static readonly GUIContent Seed = new GUIContent("Seed",
                "The same seed always draws the same picture.");

            public static readonly GUIContent Blades = new GUIContent("Blades", "How many blades the tuft has.");

            public static readonly GUIContent Height = new GUIContent("Height",
                "How much of the picture the tallest blade fills.");

            public static readonly GUIContent HeightVariation = new GUIContent("Height Variation",
                "How much shorter the other blades may be.");

            public static readonly GUIContent Width = new GUIContent("Blade Width",
                "Thickness of one blade at its foot.");

            public static readonly GUIContent Fan = new GUIContent("Fan",
                "How far the outer blades curve away from the middle.");

            public static readonly GUIContent Spread = new GUIContent("Foot Spread",
                "How wide the feet of the blades stand apart. 0 grows them all from one point.");

            public static readonly GUIContent BaseShade = new GUIContent("Foot Shade",
                "How dark a blade is at its foot against its tip. 1 leaves it flat.");

            public static readonly GUIContent ShadeVariation = new GUIContent("Blade Variation",
                "How much darker single blades may be than the rest.");

            public static readonly GUIContent Variants = new GUIContent("Variants",
                "How many different flower heads are drawn side by side. The terrain picks one per flower.");

            public static readonly GUIContent Shape = new GUIContent("Petal Shape",
                "Round, Pointed or Heart petals. Mixed gives every variant its own.");

            public static readonly GUIContent Petals = new GUIContent("Petals",
                "Fewest and most petals one ring may have.");

            public static readonly GUIContent Rings = new GUIContent("Rings",
                "Rings of petals laid over each other, each smaller and lighter than the one below.");

            public static readonly GUIContent PetalLength = new GUIContent("Petal Length",
                "How much of the picture the head fills.");

            public static readonly GUIContent PetalWidth = new GUIContent("Petal Width",
                "Width of a petal against its length.");

            public static readonly GUIContent Irregularity = new GUIContent("Irregularity",
                "How unevenly the petals are spaced and sized. 0 draws a perfect, stamped flower.");

            public static readonly GUIContent CentreSize = new GUIContent("Centre Size",
                "Size of the disc in the middle.");

            public static readonly GUIContent Shading = new GUIContent("Shading",
                "How much darker petals are near the middle and along their edges, which is what gives the " +
                "head depth. 0 leaves it flat.");

            public static readonly GUIContent PetalVariation = new GUIContent("Petal Variation",
                "How much darker single petals may be than the rest.");

            public static readonly GUIContent PetalColours = new GUIContent("Petal Colors",
                "Variants take these in turn.");

            public static readonly GUIContent CentreColour = new GUIContent("Centre Color");

            public const string Assign =
                "Select Terrain materials in the Project window before saving and the new texture is assigned " +
                "to them.";
        }

        [SerializeField] Kind m_Kind = Kind.GrassClump;
        [SerializeField] int m_Resolution = 256;
        [SerializeField] GrassClumpSettings m_Clump = GrassClumpSettings.Default;
        [SerializeField] GrassFlowerSettings m_Flowers = GrassFlowerSettings.Default;

        Texture2D m_Preview;
        string m_PreviewSignature;

        public override string title => "Grass";

        void OnDisable()
        {
            if (m_Preview != null)
                DestroyImmediate(m_Preview);
        }

        public override void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            var kind = (Kind)EditorGUILayout.EnumPopup(Styles.Kind, m_Kind);
            var index = Mathf.Max(0, System.Array.IndexOf(k_Resolutions, m_Resolution));
            var resolution = k_Resolutions[EditorGUILayout.Popup(Styles.Resolution, index, k_ResolutionLabels)];
            var clump = m_Clump;
            var flowers = m_Flowers;

            EditorGUILayout.Space();

            if (kind == Kind.GrassClump)
                DrawClump(ref clump);
            else
                DrawFlowers(ref flowers);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Grass Texture Settings");
                m_Kind = kind;
                m_Resolution = resolution;
                m_Clump = clump;
                m_Flowers = flowers;
            }

            EditorGUILayout.Space();
            DrawPreview();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.Assign, MessageType.None);

            if (GUILayout.Button("Generate and Save…", GUILayout.Height(28f)))
                GenerateAndSave();
        }

        static void DrawClump(ref GrassClumpSettings clump)
        {
            clump.blades = EditorGUILayout.IntSlider(Styles.Blades, clump.blades, 1, 64);
            clump.height = EditorGUILayout.Slider(Styles.Height, clump.height, 0.2f, 1f);
            clump.heightVariation = EditorGUILayout.Slider(Styles.HeightVariation, clump.heightVariation, 0f, 0.9f);
            clump.width = EditorGUILayout.Slider(Styles.Width, clump.width, 0.005f, 0.15f);
            clump.fan = EditorGUILayout.Slider(Styles.Fan, clump.fan, 0f, 0.8f);
            clump.spread = EditorGUILayout.Slider(Styles.Spread, clump.spread, 0f, 0.9f);
            clump.baseShade = EditorGUILayout.Slider(Styles.BaseShade, clump.baseShade, 0.2f, 1f);
            clump.shadeVariation = EditorGUILayout.Slider(Styles.ShadeVariation, clump.shadeVariation, 0f, 0.6f);
            clump.seed = DrawSeed(clump.seed);
        }

        static void DrawFlowers(ref GrassFlowerSettings flowers)
        {
            flowers.variants = EditorGUILayout.IntSlider(Styles.Variants, flowers.variants, 1, 8);
            flowers.shape = (GrassPetalShape)EditorGUILayout.EnumPopup(Styles.Shape, flowers.shape);

            float fewest = flowers.petalsMin;
            float most = flowers.petalsMax;

            EditorGUILayout.MinMaxSlider(Styles.Petals, ref fewest, ref most, 3f, 16f);
            flowers.petalsMin = Mathf.RoundToInt(fewest);
            flowers.petalsMax = Mathf.RoundToInt(most);

            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.LabelField(" ", $"{flowers.petalsMin} to {flowers.petalsMax}", EditorStyles.miniLabel);

            flowers.rings = EditorGUILayout.IntSlider(Styles.Rings, flowers.rings, 1, 3);
            flowers.petalLength = EditorGUILayout.Slider(Styles.PetalLength, flowers.petalLength, 0.15f, 0.48f);
            flowers.petalWidth = EditorGUILayout.Slider(Styles.PetalWidth, flowers.petalWidth, 0.15f, 1.2f);
            flowers.irregularity = EditorGUILayout.Slider(Styles.Irregularity, flowers.irregularity, 0f, 1f);
            flowers.centreSize = EditorGUILayout.Slider(Styles.CentreSize, flowers.centreSize, 0f, 0.25f);
            flowers.shading = EditorGUILayout.Slider(Styles.Shading, flowers.shading, 0f, 1f);
            flowers.petalVariation = EditorGUILayout.Slider(Styles.PetalVariation, flowers.petalVariation, 0f, 0.6f);

            EditorGUILayout.LabelField(Styles.PetalColours);

            using (new EditorGUI.IndentLevelScope())
            {
                flowers.petalA = PetalField("First", flowers.petalA);
                flowers.petalB = PetalField("Second", flowers.petalB);
                flowers.petalC = PetalField("Third", flowers.petalC);
                flowers.petalD = PetalField("Fourth", flowers.petalD);
            }

            flowers.centre = EditorGUILayout.ColorField(Styles.CentreColour, flowers.centre, true, false, false);
            flowers.seed = DrawSeed(flowers.seed);
        }

        static Color PetalField(string label, Color colour)
        {
            return EditorGUILayout.ColorField(new GUIContent(label), colour, true, false, false);
        }

        static int DrawSeed(int seed)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField(Styles.Seed, seed);

                if (GUILayout.Button("Randomise", GUILayout.Width(90f)))
                    seed = Random.Range(0, 100000);
            }

            return seed;
        }

        Color[] Bake(int resolution, out int width)
        {
            if (m_Kind == Kind.GrassClump)
            {
                width = resolution;
                return GrassTextureBaker.BakeClump(m_Clump, resolution);
            }

            width = resolution * Mathf.Clamp(m_Flowers.variants, 1, 8);
            return GrassTextureBaker.BakeFlowers(m_Flowers, resolution);
        }

        void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            RebuildPreviewIfNeeded();

            if (m_Preview == null)
                return;

            var aspect = (float)m_Preview.width / m_Preview.height;
            var width = Mathf.Max(k_MinPreviewSize, EditorGUIUtility.currentViewWidth - 40f);
            var height = Mathf.Min(width / aspect, width);

            width = height * aspect;

            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
            rect.x += (EditorGUIUtility.currentViewWidth - rect.width) * 0.5f - 4f;

            GUI.DrawTexture(rect, m_Preview, ScaleMode.StretchToFill, false);
        }

        void RebuildPreviewIfNeeded()
        {
            var signature = m_Kind + JsonUtility.ToJson(m_Clump) + JsonUtility.ToJson(m_Flowers);

            if (signature == m_PreviewSignature && m_Preview != null)
                return;

            m_PreviewSignature = signature;

            var pixels = Bake(k_PreviewResolution, out var width);

            if (m_Preview != null && m_Preview.width != width)
            {
                DestroyImmediate(m_Preview);
                m_Preview = null;
            }

            if (m_Preview == null)
            {
                m_Preview = new Texture2D(width, k_PreviewResolution, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var colour = m_Kind == Kind.GrassClump
                    ? new Color(k_PreviewGrass.r * pixel.r, k_PreviewGrass.g * pixel.g, k_PreviewGrass.b * pixel.b, 1f)
                    : new Color(pixel.r, pixel.g, pixel.b, 1f);

                pixels[i] = Color.Lerp(k_Backdrop, colour, pixel.a >= k_AlphaCutoff ? 1f : 0f);
            }

            m_Preview.SetPixels(pixels);
            m_Preview.Apply();
        }

        void GenerateAndSave()
        {
            var clump = m_Kind == Kind.GrassClump;

            var targets = Selection.GetFiltered<Material>(SelectionMode.Assets)
                                   .Where(material => material.shader != null &&
                                                      material.shader.name == HBStyleTerrain.ShaderName)
                                   .ToArray();

            var folder = StyleTextureNames.PickFolder("Save Grass Texture");

            if (folder == null)
                return;

            var pattern = clump ? "TEX_Clump_Grass_{0}.png" : "TEX_Clump_Flowers_{0}.png";
            var path = StyleTextureNames.Path(folder, pattern, StyleTextureNames.FreeIndex(folder, pattern));

            var pixels = Bake(m_Resolution, out var width);
            var texture = new Texture2D(width, m_Resolution, TextureFormat.RGBA32, false, clump);

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            DestroyImmediate(texture);

            var saved = Import(path, clump, width);
            var property = clump ? k_ClumpProperty : k_FlowerProperty;

            foreach (var material in targets)
            {
                if (!material.HasProperty(property))
                    continue;

                Undo.RecordObject(material, "Assign Grass Texture");
                material.SetTexture(property, saved);

                if (!clump && material.HasProperty(k_VariantsProperty))
                    material.SetFloat(k_VariantsProperty, Mathf.Clamp(m_Flowers.variants, 1, 8));

                EditorUtility.SetDirty(material);
            }

            EditorGUIUtility.PingObject(saved);
        }

        Texture2D Import(string path, bool clump, int width)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = !clump;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = !clump;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = k_AlphaCutoff;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(width), 32, 8192);
                importer.anisoLevel = 4;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
