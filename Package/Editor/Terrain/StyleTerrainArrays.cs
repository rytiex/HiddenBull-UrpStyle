using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public static class StyleTerrainArrays
    {
        const string Suffix = " HB Layers.asset";
        const int Version = 3;
        const string SliceShader = "Hidden/HB URP Style/Terrain Layer Slice";
        const int AlbedoPass = 0;
        const int NormalPass = 1;
        const int MaskPass = 2;

        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_AlbedoId = Shader.PropertyToID("_Albedo");
        static readonly int s_FillId = Shader.PropertyToID("_Fill");
        static readonly int s_HasSourceId = Shader.PropertyToID("_HasSource");
        static readonly int s_HasAlbedoId = Shader.PropertyToID("_HasAlbedo");

        public static string PathFor(TerrainData data)
        {
            var path = data != null ? AssetDatabase.GetAssetPath(data) : null;

            if (string.IsNullOrEmpty(path))
                return null;

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            return $"{folder}/{Path.GetFileNameWithoutExtension(path)}{Suffix}";
        }

        public static string Signature(TerrainLayer[] layers, int resolution)
        {
            var text = new StringBuilder();
            text.Append(Version).Append(':').Append(resolution);

            foreach (var layer in layers)
            {
                text.Append('|');

                if (layer == null)
                {
                    text.Append("none");
                    continue;
                }

                text.Append(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(layer)));
                Append(text, layer.diffuseTexture);
                Append(text, layer.normalMapTexture);
                Append(text, layer.maskMapTexture);
            }

            return text.ToString();
        }

        static void Append(StringBuilder text, Texture texture)
        {
            text.Append(',');

            if (texture == null)
                return;

            text.Append(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture)));
            text.Append(':').Append(texture.imageContentsHash);
        }

        public static bool Ensure(HBStyleTerrain owner)
        {
            var terrain = owner != null ? owner.GetComponent<Terrain>() : null;
            var data = terrain != null ? terrain.terrainData : null;

            if (data == null)
                return false;

            var layers = data.terrainLayers;
            var resolution = owner.layerResolution;
            var signature = Signature(layers, resolution);
            var container = owner.arrays;

            if (container != null && container.valid && container.signature == signature)
                return false;

            if (layers.Length == 0)
                return false;

            if (container == null)
                container = LoadOrCreate(data);

            if (container == null)
                return false;

            try
            {
                Build(container, layers, resolution, signature);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (owner.arrays != container)
            {
                Undo.RecordObject(owner, "Terrain Layer Arrays");
                owner.arrays = container;
                EditorUtility.SetDirty(owner);
            }
            else
            {
                owner.Bind();
            }

            return true;
        }

        static HBStyleTerrainLayerArrays LoadOrCreate(TerrainData data)
        {
            var path = PathFor(data);

            if (path == null)
                return null;

            var container = AssetDatabase.LoadAssetAtPath<HBStyleTerrainLayerArrays>(path);

            if (container != null)
                return container;

            container = ScriptableObject.CreateInstance<HBStyleTerrainLayerArrays>();
            AssetDatabase.CreateAsset(container, path);
            return container;
        }

        static void Build(HBStyleTerrainLayerArrays container, TerrainLayer[] layers, int resolution, string signature)
        {
            var shader = Shader.Find(SliceShader);

            if (shader == null)
            {
                Debug.LogError($"HB Terrain: shader '{SliceShader}' is missing.");
                return;
            }

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var albedoFormat = Pick(TextureFormat.BC7);
            var normalFormat = Pick(TextureFormat.BC5);
            var maskFormat = Pick(TextureFormat.BC7);

            var albedo = NewArray("Albedo", resolution, layers.Length, albedoFormat, false);
            var normal = NewArray("Normal", resolution, layers.Length, normalFormat, true);
            var mask = NewArray("Mask", resolution, layers.Length, maskFormat, true);

            try
            {
                for (var index = 0; index < layers.Length; index++)
                {
                    var layer = layers[index];
                    var name = layer != null ? layer.name : "(empty)";
                    EditorUtility.DisplayProgressBar("Terrain Layer Arrays", name, (float)index / layers.Length);

                    Prepare(material, layer);

                    material.SetTexture(s_SourceId, layer != null ? layer.diffuseTexture : null);
                    material.SetFloat(s_HasSourceId, layer != null && layer.diffuseTexture != null ? 1f : 0f);
                    Slice(material, AlbedoPass, albedo, index, resolution, false, albedoFormat);

                    material.SetTexture(s_SourceId, layer != null ? layer.normalMapTexture : null);
                    material.SetFloat(s_HasSourceId, layer != null && layer.normalMapTexture != null ? 1f : 0f);
                    Slice(material, NormalPass, normal, index, resolution, true, normalFormat);

                    material.SetTexture(s_SourceId, layer != null ? layer.maskMapTexture : null);
                    material.SetFloat(s_HasSourceId, layer != null && layer.maskMapTexture != null ? 1f : 0f);
                    Slice(material, MaskPass, mask, index, resolution, true, maskFormat);
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
            }

            albedo.Apply(false, false);
            normal.Apply(false, false);
            mask.Apply(false, false);

            var path = AssetDatabase.GetAssetPath(container);

            foreach (var old in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (old is Texture2DArray)
                {
                    AssetDatabase.RemoveObjectFromAsset(old);
                    Object.DestroyImmediate(old, true);
                }
            }

            AssetDatabase.AddObjectToAsset(albedo, container);
            AssetDatabase.AddObjectToAsset(normal, container);
            AssetDatabase.AddObjectToAsset(mask, container);

            container.Set(albedo, normal, mask, (TerrainLayer[])layers.Clone(), signature);
            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
        }

        static void Prepare(Material material, TerrainLayer layer)
        {
            var hasAlbedo = layer != null && layer.diffuseTexture != null;

            material.SetTexture(s_AlbedoId, hasAlbedo ? layer.diffuseTexture : null);
            material.SetFloat(s_HasAlbedoId, hasAlbedo ? 1f : 0f);
            material.SetColor(s_FillId, new Color(0.5f, 0.5f, 0.5f, 1f).linear);
        }

        static TextureFormat Pick(TextureFormat wanted)
        {
            return SystemInfo.SupportsTextureFormat(wanted) ? wanted : TextureFormat.RGBA32;
        }

        static Texture2DArray NewArray(string name, int resolution, int depth, TextureFormat format, bool linear)
        {
            return new Texture2DArray(resolution, resolution, depth, format, true, linear)
            {
                name = name,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
                wrapMode = TextureWrapMode.Repeat
            };
        }

        static void Slice(Material material, int pass, Texture2DArray array, int slice, int resolution, bool linear,
                          TextureFormat format)
        {
            var space = linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
            var target = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32, space);
            var commands = new CommandBuffer { name = "HB Terrain Layer Slice" };

            commands.SetRenderTarget(target);
            commands.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
            Graphics.ExecuteCommandBuffer(commands);
            commands.Release();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var readable = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, linear);
            readable.ReadPixels(new Rect(0f, 0f, resolution, resolution), 0, 0, false);
            readable.Apply(true, false);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            if (format != TextureFormat.RGBA32)
                EditorUtility.CompressTexture(readable, format, TextureCompressionQuality.Fast);

            for (var mip = 0; mip < readable.mipmapCount && mip < array.mipmapCount; mip++)
                array.SetPixelData(readable.GetPixelData<byte>(mip), mip, slice);

            Object.DestroyImmediate(readable);
        }
    }
}
