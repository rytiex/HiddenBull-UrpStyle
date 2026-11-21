using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleTerrainMaps
    {
        const string MovedKey = "HiddenBull.UrpStyle.TerrainMapsMoved.3";
        const string AssetExtension = ".asset";

        static readonly string[] s_LegacyNames =
            { "Hand", "Control", "Grass", "Liquid Level", "Liquid Mix", "Liquid Flow", "Albedo", "Normal", "Mask" };

        static StyleTerrainMaps()
        {
            AssemblyReloadEvents.beforeAssemblyReload += PackLoaded;
            Undo.undoRedoPerformed += TouchLoaded;

            if (!SessionState.GetBool(MovedKey, false))
                EditorApplication.delayCall += MoveAll;
        }

        internal static void Discard(Texture texture)
        {
            if (texture == null)
                return;

            if (AssetDatabase.Contains(texture))
                AssetDatabase.RemoveObjectFromAsset(texture);

            Object.DestroyImmediate(texture, true);
        }

        internal static bool IsData(string path)
        {
            return path.EndsWith(AssetExtension, System.StringComparison.OrdinalIgnoreCase) &&
                   AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(HBStyleTerrainData);
        }

        internal static void Move(HBStyleTerrainData data)
        {
            if (data == null)
                return;

            var moved = new List<Texture>();
            var failed = new List<string>();

            if (data.legacyMaps)
                data.MoveLegacyMaps(moved, failed);

            Orphans(data, moved);

            if (moved.Count == 0 && failed.Count == 0)
                return;

            foreach (var texture in moved)
                Discard(texture);

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);

            if (failed.Count > 0)
                Debug.LogError($"HB Style Terrain: {data.name} could not compress {string.Join(", ", failed)}; " +
                               "those maps were kept as they were.", data);
            else
                Debug.Log($"HB Style Terrain: {data.name} was converted to the compact terrain format.", data);
        }

        static void Orphans(HBStyleTerrainData data, List<Texture> moved)
        {
            foreach (var item in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(data)))
            {
                if (item is Texture texture && System.Array.IndexOf(s_LegacyNames, texture.name) >= 0 &&
                    !data.HoldsLegacy(texture) && !moved.Contains(texture))
                    moved.Add(texture);
            }
        }

        static void MoveAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SessionState.SetBool(MovedKey, true);

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(HBStyleTerrainData)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (IsData(path))
                    Move(AssetDatabase.LoadAssetAtPath<HBStyleTerrainData>(path));
            }
        }

        static void PackLoaded()
        {
            foreach (var data in Resources.FindObjectsOfTypeAll<HBStyleTerrainData>())
            {
                if (data.PackMaps(false))
                    EditorUtility.SetDirty(data);
            }
        }

        static void TouchLoaded()
        {
            foreach (var data in Resources.FindObjectsOfTypeAll<HBStyleTerrainData>())
                data.MapsTouched();
        }
    }

    sealed class StyleTerrainMapSaver : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!StyleTerrainMaps.IsData(path))
                    continue;

                var data = AssetDatabase.LoadAssetAtPath<HBStyleTerrainData>(path);

                if (data != null)
                    data.PackMaps(true);
            }

            return paths;
        }
    }

    sealed class StyleTerrainMapImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (!StyleTerrainMaps.IsData(path))
                    continue;

                var data = AssetDatabase.LoadAssetAtPath<HBStyleTerrainData>(path);

                if (data != null && data.legacyMaps)
                    EditorApplication.delayCall += () => StyleTerrainMaps.Move(data);
            }
        }
    }
}
