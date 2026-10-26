using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainShapes
    {
        const string Key = "HiddenBull.UrpStyle.Terrain.Brush.";
        const string BuiltinPath = "Brushes/builtin_brush_{0}.brush";
        const string Round = "round";
        const int MaskSize = 128;
        const int BuiltinLimit = 64;
        const int PreviewSize = 32;

        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        sealed class Entry
        {
            internal string id;
            internal StyleTerrainShape shape;
        }

        static readonly List<Entry> s_Entries = new List<Entry>();
        static readonly List<GUIContent> s_Contents = new List<GUIContent>();
        static readonly List<Texture2D> s_Previews = new List<Texture2D>();

        internal static float rotation
        {
            get => EditorPrefs.GetFloat(Key + "Rotation", 0f);
            set => EditorPrefs.SetFloat(Key + "Rotation", Mathf.Repeat(value, 360f));
        }

        static string selectedId
        {
            get => EditorPrefs.GetString(Key + "Shape", Round);
            set => EditorPrefs.SetString(Key + "Shape", value);
        }

        internal static bool round => selectedId == Round;

        internal static StyleTerrainShape current
        {
            get
            {
                var entry = Selected();

                if (entry == null || entry.shape == null)
                    return null;

                entry.shape.Rotate(rotation);
                return entry.shape;
            }
        }

        internal static GUIContent[] contents
        {
            get
            {
                if (s_Entries.Count == 0)
                    Refresh();

                return s_Contents.ToArray();
            }
        }

        internal static int selectedIndex
        {
            get
            {
                if (s_Entries.Count == 0)
                    Refresh();

                var id = selectedId;

                for (var index = 0; index < s_Entries.Count; index++)
                {
                    if (s_Entries[index].id == id)
                        return index;
                }

                return 0;
            }
            set
            {
                if (value >= 0 && value < s_Entries.Count)
                    selectedId = s_Entries[value].id;
            }
        }

        internal static void Refresh()
        {
            s_Entries.Clear();
            s_Contents.Clear();

            foreach (var preview in s_Previews)
                Object.DestroyImmediate(preview);

            s_Previews.Clear();

            Add(Round, "Round", RoundMask());

            for (var index = 1; index <= BuiltinLimit; index++)
            {
                var brush = EditorGUIUtility.Load(string.Format(BuiltinPath, index)) as ScriptableObject;
                var texture = Texture(brush);

                if (texture == null)
                    break;

                Add("builtin:" + index, brush.name, Read(texture));
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Brush"))
            {
                var brush = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                var texture = Texture(brush);

                if (texture != null)
                    Add(guid, brush.name, Read(texture));
            }
        }

        static Texture Texture(ScriptableObject brush)
        {
            if (brush == null)
                return null;

            if (brush.GetType().GetProperty("texture", Members)?.GetValue(brush) is Texture generated)
                return generated;

            var property = new SerializedObject(brush).GetIterator();

            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue is Texture2D mask)
                    return mask;
            }

            return null;
        }

        static void Add(string id, string name, float[] mask)
        {
            var preview = Preview(mask);

            s_Previews.Add(preview);
            s_Entries.Add(new Entry { id = id, shape = id == Round ? null : new StyleTerrainShape(mask, MaskSize) });
            s_Contents.Add(new GUIContent(preview, name));
        }

        static Entry Selected()
        {
            var index = selectedIndex;

            return index < s_Entries.Count ? s_Entries[index] : null;
        }

        static float[] RoundMask()
        {
            var mask = new float[MaskSize * MaskSize];

            for (var z = 0; z < MaskSize; z++)
            {
                for (var x = 0; x < MaskSize; x++)
                {
                    var dx = (x + 0.5f) / MaskSize * 2f - 1f;
                    var dz = (z + 0.5f) / MaskSize * 2f - 1f;

                    mask[z * MaskSize + x] = StyleTerrainStamp.Falloff(Mathf.Sqrt(dx * dx + dz * dz), 0.3f);
                }
            }

            return mask;
        }

        static Texture2D Preview(float[] mask)
        {
            var preview = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[PreviewSize * PreviewSize];
            var scale = MaskSize / PreviewSize;

            for (var z = 0; z < PreviewSize; z++)
            {
                for (var x = 0; x < PreviewSize; x++)
                {
                    var value = (byte)(Mathf.Clamp01(mask[(z * scale + scale / 2) * MaskSize + x * scale + scale / 2]) *
                                       255f);

                    pixels[z * PreviewSize + x] = new Color32(value, value, value, 255);
                }
            }

            preview.SetPixels32(pixels);
            preview.Apply(false, true);
            return preview;
        }

        static float[] Read(Texture texture)
        {
            var target = RenderTexture.GetTemporary(MaskSize, MaskSize, 0, RenderTextureFormat.ARGB32,
                                                    RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;

            Graphics.Blit(texture, target);
            RenderTexture.active = target;

            var readable = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false, true);

            readable.ReadPixels(new Rect(0f, 0f, MaskSize, MaskSize), 0, 0, false);
            readable.Apply(false, false);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            var alphaOnly = texture is Texture2D flat && flat.format == TextureFormat.Alpha8;
            var pixels = readable.GetPixels32();
            var mask = new float[pixels.Length];

            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                var brightest = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) / 255f;

                mask[index] = alphaOnly ? pixel.a / 255f : brightest * pixel.a / 255f;
            }

            Object.DestroyImmediate(readable);
            return mask;
        }
    }
}
