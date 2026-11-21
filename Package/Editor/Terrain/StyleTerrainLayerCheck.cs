using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainLayerCheck
    {
        const string Intro = "These layer textures cannot be copied straight to the GPU, so the terrain resamples " +
                             "them and uses about four times the memory for that map:";

        static readonly GUIContent s_Fix = new GUIContent("Match Import Settings",
            "Sets High Quality compression and mipmaps on every layer texture, and turns sRGB off for normal and " +
            "mask maps, so the terrain copies them straight to the GPU. Size and shape are not changed.");

        static readonly StyleTerrainLayerMap[] s_Maps =
            { StyleTerrainLayerMap.Albedo, StyleTerrainLayerMap.Normal, StyleTerrainLayerMap.Mask };

        static readonly List<string> s_Problems = new List<string>();
        static readonly List<GraphicsFormat> s_Formats = new List<GraphicsFormat>();

        internal static void Draw(HBStyleTerrainData data)
        {
            if (!Check(data))
                return;

            EditorGUILayout.HelpBox(Intro + "\n" + string.Join("\n", s_Problems), MessageType.Warning);

            if (GUILayout.Button(s_Fix))
                Fix(data);
        }

        static bool Check(HBStyleTerrainData data)
        {
            s_Problems.Clear();

            var resolution = data.textureResolution;

            foreach (var map in s_Maps)
            {
                s_Formats.Clear();

                for (var index = 0; index < data.layerCount; index++)
                {
                    var source = StyleTerrainLayerArrays.Source(data.layers[index].layer, map);

                    if (source == null)
                        continue;

                    var problem = StyleTerrainLayerArrays.Problem(source, resolution, map);

                    if (problem != null)
                        s_Problems.Add($"• {source.name} ({map}) {problem}");
                    else if (!s_Formats.Contains(source.graphicsFormat))
                        s_Formats.Add(source.graphicsFormat);
                }

                if (s_Formats.Count > 1)
                    s_Problems.Add($"• {map} textures mix {string.Join(", ", s_Formats)}");
            }

            return s_Problems.Count > 0;
        }

        static void Fix(HBStyleTerrainData data)
        {
            var importers = new List<TextureImporter>();

            for (var index = 0; index < data.layerCount; index++)
            {
                foreach (var map in s_Maps)
                {
                    var source = StyleTerrainLayerArrays.Source(data.layers[index].layer, map);
                    var importer = source != null
                        ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source)) as TextureImporter
                        : null;

                    if (importer == null || importers.Contains(importer))
                        continue;

                    importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.crunchedCompression = false;
                    importer.streamingMipmaps = false;

                    if (map != StyleTerrainLayerMap.Albedo)
                        importer.sRGBTexture = false;

                    if (importer.maxTextureSize < data.textureResolution)
                        importer.maxTextureSize = data.textureResolution;

                    importers.Add(importer);
                }
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var importer in importers)
                    importer.SaveAndReimport();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
