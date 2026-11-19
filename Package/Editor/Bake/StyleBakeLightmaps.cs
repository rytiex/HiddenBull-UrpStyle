using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleBakeLightmaps
    {
        internal static bool UsesMipmapLimits(LightingSettings settings)
        {
            using var serialized = new SerializedObject(settings);
            var property = serialized.FindProperty("m_UseMipmapLimits");

            return property != null && property.boolValue;
        }

        internal static void QueueStrip()
        {
            EditorApplication.delayCall -= StripMipmaps;
            EditorApplication.delayCall += StripMipmaps;
        }

        internal static void StripMipmaps()
        {
            if (!Lightmapping.TryGetLightingSettings(out var settings) || UsesMipmapLimits(settings))
                return;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var lightmap in LightmapSettings.lightmaps)
                {
                    Strip(lightmap.lightmapColor);
                    Strip(lightmap.lightmapDir);
                    Strip(lightmap.shadowMask);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        internal static long Bytes()
        {
            long bytes = 0;

            foreach (var lightmap in LightmapSettings.lightmaps)
                bytes += Size(lightmap.lightmapColor) + Size(lightmap.lightmapDir) + Size(lightmap.shadowMask);

            return bytes;
        }

        static long Size(Texture texture)
        {
            return texture != null ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture) : 0;
        }

        static void Strip(Texture texture)
        {
            if (texture == null)
                return;

            var path = AssetDatabase.GetAssetPath(texture);

            if (AssetImporter.GetAtPath(path) is not TextureImporter importer || !importer.mipmapEnabled)
                return;

            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
