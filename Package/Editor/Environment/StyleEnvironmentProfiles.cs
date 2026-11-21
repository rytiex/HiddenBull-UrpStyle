using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleEnvironmentProfiles
    {
        const string MovedKey = "HiddenBull.UrpStyle.AtmosphereMoved.1";
        const string AssetExtension = ".asset";

        static StyleEnvironmentProfiles()
        {
            if (!SessionState.GetBool(MovedKey, false))
                EditorApplication.delayCall += MoveAll;
        }

        internal static bool IsProfile(string path)
        {
            return path.EndsWith(AssetExtension, System.StringComparison.OrdinalIgnoreCase) &&
                   AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(HBStyleEnvironmentProfile);
        }

        internal static bool CanWrite(HBStyleEnvironmentProfile profile)
        {
            var path = AssetDatabase.GetAssetPath(profile);

            if (string.IsNullOrEmpty(path))
                return false;

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);

            return package == null || package.source == PackageSource.Embedded || package.source == PackageSource.Local;
        }

        internal static void Move(HBStyleEnvironmentProfile profile)
        {
            if (profile == null || !profile.legacyAtmosphere || !CanWrite(profile))
                return;

            profile.MoveAtmosphere();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            Debug.Log($"HB Style: {profile.name} now keeps its atmosphere settings itself. The old Atmosphere " +
                      "Profile asset is no longer used and can be deleted.", profile);
        }

        static void MoveAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SessionState.SetBool(MovedKey, true);

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(HBStyleEnvironmentProfile)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (IsProfile(path))
                    Move(AssetDatabase.LoadAssetAtPath<HBStyleEnvironmentProfile>(path));
            }
        }
    }

    sealed class StyleEnvironmentProfileImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (!StyleEnvironmentProfiles.IsProfile(path))
                    continue;

                var profile = AssetDatabase.LoadAssetAtPath<HBStyleEnvironmentProfile>(path);

                if (profile != null && profile.legacyAtmosphere)
                    EditorApplication.delayCall += () => StyleEnvironmentProfiles.Move(profile);
            }
        }
    }
}
