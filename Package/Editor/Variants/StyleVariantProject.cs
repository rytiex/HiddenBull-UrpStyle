using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleVariantProject
    {
        internal const string LodCrossFadeKeyword = "LOD_FADE_CROSSFADE";
        internal const string CookiesKeyword = "_LIGHT_COOKIES";
        internal const string DotsKeyword = "DOTS_INSTANCING_ON";
        internal const string MixingKeyword = "LIGHTMAP_SHADOW_MIXING";

        const string QualityPath = "ProjectSettings/QualitySettings.asset";
        const string EntitiesGraphics = "com.unity.entities.graphics";

        internal static List<UniversalRenderPipelineAsset> UrpAssets()
        {
            var assets = new List<UniversalRenderPipelineAsset>();

            Add(assets, GraphicsSettings.defaultRenderPipeline);

            for (var i = 0; i < QualitySettings.count; i++)
                Add(assets, QualitySettings.GetRenderPipelineAssetAt(i));

            return assets;
        }

        static void Add(List<UniversalRenderPipelineAsset> assets, RenderPipelineAsset asset)
        {
            if (asset is UniversalRenderPipelineAsset urp && !assets.Contains(urp))
                assets.Add(urp);
        }

        internal static bool AnyAsset(string property)
        {
            foreach (var asset in UrpAssets())
            {
                var value = new SerializedObject(asset).FindProperty(property);

                if (value != null && value.boolValue)
                    return true;
            }

            return false;
        }

        internal static void SetAll(string property, bool value, string undo)
        {
            foreach (var asset in UrpAssets())
            {
                var serialized = new SerializedObject(asset);
                var field = serialized.FindProperty(property);

                if (field == null || field.boolValue == value)
                    continue;

                Undo.RecordObject(asset, undo);
                field.boolValue = value;
                serialized.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        internal static bool UsesResidentDrawer()
        {
            foreach (var asset in UrpAssets())
            {
                var mode = new SerializedObject(asset).FindProperty("m_GPUResidentDrawerMode");

                if (mode != null && mode.intValue != 0)
                    return true;
            }

            return false;
        }

        internal static bool HasEntitiesGraphics()
        {
            return UnityEditor.PackageManager.PackageInfo.FindForPackageName(EntitiesGraphics) != null;
        }

        internal static bool KeepsAllDotsVariants()
        {
            return EditorGraphicsSettings.batchRendererGroupShaderStrippingMode ==
                   BatchRendererGroupStrippingMode.KeepAll;
        }

        internal static void StripUnusedDotsVariants()
        {
            var settings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var mode = settings.FindProperty("m_BrgStripping");

            if (mode == null)
                return;

            mode.intValue = (int)BatchRendererGroupStrippingMode.KeepIfEntitiesGraphics;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        internal static bool AnyQualityShadowmaskAlways()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(QualityPath);

            if (assets.Length == 0)
                return true;

            var levels = new SerializedObject(assets[0]).FindProperty("m_QualitySettings");

            if (levels == null)
                return true;

            for (var i = 0; i < levels.arraySize; i++)
            {
                var mode = levels.GetArrayElementAtIndex(i).FindPropertyRelative("shadowmaskMode");

                if (mode == null || mode.intValue == (int)ShadowmaskMode.Shadowmask)
                    return true;
            }

            return false;
        }

        internal static bool AnySubtractiveBake()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:LightingSettings"))
            {
                var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(AssetDatabase.GUIDToAssetPath(guid));

                if (settings != null && settings.mixedBakeMode == MixedLightingMode.Subtractive)
                    return true;
            }

            return false;
        }

        internal static bool MixingUnused()
        {
            return !AnyQualityShadowmaskAlways() && !AnySubtractiveBake();
        }

        internal sealed class Scan
        {
            public int fadingLodGroups;
            public int cookieLights;
            public int files;
            public bool textAssets;
        }

        internal static Scan ScanProject()
        {
            var scan = new Scan { textAssets = EditorSettings.serializationMode == SerializationMode.ForceText };
            var paths = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                    paths.Add(scene.path);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            for (var i = 0; i < paths.Count; i++)
            {
                if (i % 50 == 0 &&
                    EditorUtility.DisplayCancelableProgressBar("HB Variants", "Scanning scenes and prefabs",
                                                               (float)i / paths.Count))
                    break;

                ScanFile(paths[i], scan);
            }

            EditorUtility.ClearProgressBar();
            return scan;
        }

        static void ScanFile(string path, Scan scan)
        {
            if (!File.Exists(path))
                return;

            scan.files++;

            foreach (var line in File.ReadLines(path))
            {
                var text = line.TrimStart();

                if (text.StartsWith("m_FadeMode: ") && text != "m_FadeMode: 0")
                    scan.fadingLodGroups++;
                else if (text.StartsWith("m_Cookie: {fileID: ") && !text.StartsWith("m_Cookie: {fileID: 0}"))
                    scan.cookieLights++;
            }
        }
    }
}
