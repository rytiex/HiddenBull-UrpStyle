using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainArrays
    {
        const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Editor/Terrain/StyleTerrainLayerSlice.shader";
        const int Version = 1;
        const int AlbedoPass = 0;
        const int NormalPass = 1;
        const int MaskPass = 2;

        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_AlbedoId = Shader.PropertyToID("_Albedo");
        static readonly int s_FillId = Shader.PropertyToID("_Fill");
        static readonly int s_HasSourceId = Shader.PropertyToID("_HasSource");
        static readonly int s_HasAlbedoId = Shader.PropertyToID("_HasAlbedo");

        internal static string Signature(HBStyleTerrainData data)
        {
            var text = new StringBuilder();

            text.Append(Version).Append(':').Append(data.textureResolution).Append(':').Append(data.layerCount);

            for (var index = 0; index < data.layerCount; index++)
            {
                var layer = data.layers[index].layer;

                text.Append('|');

                if (layer == null)
                {
                    text.Append("none");
                    continue;
                }

                Append(text, layer.albedo);
                Append(text, layer.normal);
                Append(text, layer.mask);
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

        internal static void Build(HBStyleTerrainData data)
        {
            var layers = data.layerCount;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            if (shader == null)
                return;

            var resolution = data.textureResolution;
            var depth = Mathf.Max(layers, 1);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var albedoFormat = Pick(TextureFormat.BC7);
            var normalFormat = Pick(TextureFormat.BC5);
            var maskFormat = Pick(TextureFormat.BC7);

            var albedo = NewArray("Albedo", resolution, depth, albedoFormat, false);
            var normal = NewArray("Normal", resolution, depth, normalFormat, true);
            var mask = NewArray("Mask", resolution, depth, maskFormat, true);

            try
            {
                for (var index = 0; index < layers; index++)
                {
                    var layer = data.layers[index].layer;

                    EditorUtility.DisplayProgressBar("HB Style Terrain Layers", layer != null ? layer.name : "(empty)",
                                                     (float)index / layers);

                    var hasAlbedo = layer != null && layer.albedo != null;

                    material.SetTexture(s_AlbedoId, hasAlbedo ? layer.albedo : null);
                    material.SetFloat(s_HasAlbedoId, hasAlbedo ? 1f : 0f);
                    material.SetColor(s_FillId, new Color(0.5f, 0.5f, 0.5f, 1f).linear);

                    Slice(material, AlbedoPass, layer != null ? layer.albedo : null, albedo, index, resolution,
                          false, albedoFormat);
                    Slice(material, NormalPass, layer != null ? layer.normal : null, normal, index, resolution, true,
                          normalFormat);
                    Slice(material, MaskPass, layer != null ? layer.mask : null, mask, index, resolution, true,
                          maskFormat);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Object.DestroyImmediate(material);
            }

            albedo.Apply(false, false);
            normal.Apply(false, false);
            mask.Apply(false, false);

            StyleTerrainBaker.Remove(data, "Albedo");
            StyleTerrainBaker.Remove(data, "Normal");
            StyleTerrainBaker.Remove(data, "Mask");

            AssetDatabase.AddObjectToAsset(albedo, data);
            AssetDatabase.AddObjectToAsset(normal, data);
            AssetDatabase.AddObjectToAsset(mask, data);

            data.SetTextures(albedo, normal, mask, Signature(data));
            EditorUtility.SetDirty(data);
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

        static void Slice(Material material, int pass, Texture source, Texture2DArray array, int slice, int resolution,
                          bool linear, TextureFormat format)
        {
            material.SetTexture(s_SourceId, source);
            material.SetFloat(s_HasSourceId, source != null ? 1f : 0f);

            var space = linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
            var target = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32, space);
            var commands = new CommandBuffer { name = "HB Style Terrain Layer Slice" };

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
