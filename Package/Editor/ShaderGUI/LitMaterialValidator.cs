using System;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class LitMaterialValidator : AssetPostprocessor
    {
        const string VersionKey = "HiddenBull.UrpStyle.LitMaterialVersion";
        const string Version = "14";

        static readonly (string shader, Action<Material> validate)[] s_Validators =
        {
            ("HB URP Style/Lit", LitShaderGUI.Validate),
            ("HB URP Style/Foliage", LitShaderGUI.Validate),
            ("HB URP Style/Liquid", LiquidShaderGUI.Validate),
            ("HB URP Style/Particle", ParticleShaderGUI.Validate)
        };

        static readonly string[] s_Passes =
        {
            "UniversalForward", "UniversalForwardOnly", "DepthOnly", "DepthNormals", "ShadowCaster",
            "HiddenBullShadowTint",
            "HiddenBullOITMoments", "HiddenBullOITColor", "HiddenBullOITRefraction", "HiddenBullSkyOcclusion",
            "Meta"
        };

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (EditorUserSettings.GetConfigValue(VersionKey) != Version)
                EditorApplication.delayCall += ValidateAll;
        }

        static void ValidateAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var material = Validate(AssetDatabase.GUIDToAssetPath(guid));

                if (material != null)
                    AssetDatabase.SaveAssetIfDirty(material);
            }

            EditorUserSettings.SetConfigValue(VersionKey, Version);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                           string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
                Validate(path);
        }

        static Material Validate(string path)
        {
            if (!path.EndsWith(".mat"))
                return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null || material.shader == null || !AssetDatabase.IsOpenForEdit(material))
                return null;

            var validate = Find(material.shader.name);

            if (validate == null)
                return null;

            var before = State(material);

            validate(material);

            if (State(material) == before)
                return null;

            EditorUtility.SetDirty(material);

            return material;
        }

        static Action<Material> Find(string shader)
        {
            foreach (var entry in s_Validators)
            {
                if (entry.shader == shader)
                    return entry.validate;
            }

            return null;
        }

        static string State(Material material)
        {
            var state = material.shader.name + "|" + string.Join(" ", material.shaderKeywords) + "|"
                      + material.renderQueue + "|" + material.GetTag("RenderType", false) + "|"
                      + (int)material.globalIlluminationFlags + "|"
                      + (material.HasFloat("_Glow") ? material.GetFloat("_Glow") : 0f) + "|"
                      + (material.HasFloat("_BubbleAmount") ? material.GetFloat("_BubbleAmount") : 0f) + "|";

            foreach (var pass in s_Passes)
                state += material.GetShaderPassEnabled(pass) ? "1" : "0";

            return state;
        }
    }
}
