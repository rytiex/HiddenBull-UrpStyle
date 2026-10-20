using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class MaskMapGeneratorPage : StyleStudioPage
    {
        public enum Source
        {
            Red,
            Green,
            Blue,
            Alpha,
            Gray
        }

        [Serializable]
        public sealed class Channel
        {
            public Texture2D texture;
            public Source source = Source.Gray;
            public bool invert;
            [Range(0f, 1f)] public float value;
        }

        const int k_PreviewResolution = 128;
        const string k_Pattern = "TEX_Mask_{0}_{{0}}.png";

        static readonly int[] k_Resolutions = { 0, 256, 512, 1024, 2048, 4096 };
        static readonly string[] k_ResolutionLabels = { "Largest Input", "256", "512", "1024", "2048", "4096" };

        static class Styles
        {
            public static readonly GUIContent Metallic = new GUIContent("Metallic (R)",
                "How metallic the surface is. Black is not metal, white is bare metal.");

            public static readonly GUIContent Occlusion = new GUIContent("Occlusion (G)",
                "Ambient occlusion: dark in creases and corners that light reaches less.");

            public static readonly GUIContent Height = new GUIContent("Height (B)",
                "Height of the surface. Terrain layers use it to blend layers by height; the Lit shader " +
                "ignores it.");

            public static readonly GUIContent Smoothness = new GUIContent("Smoothness (A)",
                "How smooth and glossy the surface is. Turn on Invert for a roughness map.");

            public static readonly GUIContent Texture = new GUIContent("Texture",
                "The map to take this channel from. Leave empty to fill the channel with Value.");

            public static readonly GUIContent SourceChannel = new GUIContent("Read From",
                "Which channel of the texture holds the map. Gray averages the color channels.");

            public static readonly GUIContent Invert = new GUIContent("Invert",
                "Flips the map, for example to turn a roughness map into smoothness.");

            public static readonly GUIContent Value = new GUIContent("Value",
                "Fills the channel when no texture is set.");

            public static readonly GUIContent Resolution = new GUIContent("Resolution",
                "Size of the saved map. Largest Input matches the biggest texture used above.");

            public static readonly GUIContent Name = new GUIContent("Name",
                "Saved as TEX_Mask_<Name>_<Index>.png in the folder you pick.");

            public const string Assign =
                "Select HB URP Style Lit or Foliage materials in the Project window before saving and the new " +
                "map is set as their Mask Map.";
        }

        [SerializeField] Channel m_Metallic = new Channel { value = 0f };
        [SerializeField] Channel m_Occlusion = new Channel { value = 1f };
        [SerializeField] Channel m_Height = new Channel { value = 0.5f };
        [SerializeField] Channel m_Smoothness = new Channel { value = 0.5f };
        [SerializeField] int m_Resolution;
        [SerializeField] string m_Name = "Material";

        Texture2D m_Preview;
        string m_PreviewSignature;

        public override string title => "Mask Map";

        void OnDisable()
        {
            if (m_Preview != null)
                DestroyImmediate(m_Preview);
        }

        public override void OnGUI()
        {
            DrawChannel(Styles.Metallic, m_Metallic);
            DrawChannel(Styles.Occlusion, m_Occlusion);
            DrawChannel(Styles.Height, m_Height);
            DrawChannel(Styles.Smoothness, m_Smoothness);

            EditorGUILayout.Space();

            var index = Mathf.Max(Array.IndexOf(k_Resolutions, m_Resolution), 0);
            m_Resolution = k_Resolutions[EditorGUILayout.Popup(Styles.Resolution, index, k_ResolutionLabels)];
            m_Name = EditorGUILayout.TextField(Styles.Name, m_Name);

            EditorGUILayout.Space();
            DrawPreview();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.Assign, MessageType.None);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_Name) ||
                                               m_Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                if (GUILayout.Button("Save Mask Map", GUILayout.Height(26f)))
                {
                    Save();
                    GUIUtility.ExitGUI();
                }
            }
        }

        static void DrawChannel(GUIContent label, Channel channel)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                channel.texture = (Texture2D)EditorGUILayout.ObjectField(Styles.Texture, channel.texture,
                                                                          typeof(Texture2D), false);

                if (channel.texture != null)
                {
                    channel.source = (Source)EditorGUILayout.EnumPopup(Styles.SourceChannel, channel.source);
                    channel.invert = EditorGUILayout.Toggle(Styles.Invert, channel.invert);
                }
                else
                {
                    channel.value = EditorGUILayout.Slider(Styles.Value, channel.value, 0f, 1f);
                }
            }
        }

        void DrawPreview()
        {
            var signature = Signature();

            if (m_Preview == null || signature != m_PreviewSignature)
            {
                if (m_Preview != null)
                    DestroyImmediate(m_Preview);

                m_Preview = Combine(k_PreviewResolution, k_PreviewResolution);
                m_PreviewSignature = signature;
            }

            var rect = GUILayoutUtility.GetRect(k_PreviewResolution, k_PreviewResolution, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, m_Preview, null, ScaleMode.ScaleToFit, 0f, -1, ColorWriteMask.All);
        }

        string Signature()
        {
            return string.Join("|", new[] { m_Metallic, m_Occlusion, m_Height, m_Smoothness }.Select(
                channel => (channel.texture != null
                               ? channel.texture.name + ":" + channel.texture.imageContentsHash
                               : "-") +
                           channel.source + channel.invert + channel.value));
        }

        Vector2Int Size()
        {
            if (m_Resolution > 0)
                return new Vector2Int(m_Resolution, m_Resolution);

            var size = new Vector2Int(0, 0);

            foreach (var channel in new[] { m_Metallic, m_Occlusion, m_Height, m_Smoothness })
            {
                if (channel.texture != null)
                    size = Vector2Int.Max(size, new Vector2Int(channel.texture.width, channel.texture.height));
            }

            return size.x > 0 ? size : new Vector2Int(1024, 1024);
        }

        Texture2D Combine(int width, int height)
        {
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color[width * height];
            var channels = new[] { m_Metallic, m_Occlusion, m_Height, m_Smoothness };

            for (var slot = 0; slot < channels.Length; slot++)
            {
                var values = Read(channels[slot], width, height);

                for (var index = 0; index < pixels.Length; index++)
                    pixels[index][slot] = values[index];
            }

            result.SetPixels(pixels);
            result.Apply(false);
            return result;
        }

        static float[] Read(Channel channel, int width, int height)
        {
            var values = new float[width * height];

            if (channel.texture == null)
            {
                for (var index = 0; index < values.Length; index++)
                    values[index] = channel.value;

                return values;
            }

            var srgb = GraphicsFormatUtility.IsSRGBFormat(channel.texture.graphicsFormat);
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                                                    srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            var readable = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb);

            try
            {
                Graphics.Blit(channel.texture, target);
                RenderTexture.active = target;
                readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                readable.Apply(false);

                var colours = readable.GetPixels32();

                for (var index = 0; index < values.Length; index++)
                {
                    var c = colours[index];
                    var value = channel.source switch
                    {
                        Source.Red => c.r,
                        Source.Green => c.g,
                        Source.Blue => c.b,
                        Source.Alpha => c.a,
                        _ => (c.r + c.g + c.b) / 3f
                    } / 255f;

                    values[index] = channel.invert ? 1f - value : value;
                }
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                DestroyImmediate(readable);
            }

            return values;
        }

        void Save()
        {
            var folder = StyleTextureNames.PickFolder("Save Mask Map");

            if (folder == null)
                return;

            var pattern = string.Format(k_Pattern, m_Name.Trim().Replace(' ', '_'));
            var path = StyleTextureNames.Path(folder, pattern, StyleTextureNames.FreeIndex(folder, pattern));
            var size = Size();
            var texture = Combine(size.x, size.y);

            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            foreach (var material in Selection.GetFiltered<Material>(SelectionMode.Assets))
            {
                if (!material.HasProperty("_MaskMap"))
                    continue;

                Undo.RecordObject(material, "Assign Mask Map");
                material.SetTexture("_MaskMap", saved);

                if (material.HasProperty("_MaskMapEnabled"))
                    material.SetFloat("_MaskMapEnabled", 1f);

                LitShaderGUI.Validate(material);
                EditorUtility.SetDirty(material);
            }

            EditorGUIUtility.PingObject(saved);
        }
    }
}
