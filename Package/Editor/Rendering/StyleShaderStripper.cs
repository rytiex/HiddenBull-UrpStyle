using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleShaderStripper : IPreprocessShaders, IPreprocessBuildWithReport
    {
        const string ProceduralKeyword = "PROCEDURAL_INSTANCING_ON";
        const string DotsKeyword = "DOTS_INSTANCING_ON";
        const string EntitiesGraphicsType = "Unity.Rendering.EntitiesGraphicsSystem, Unity.Entities.Graphics";

        static readonly string[] s_OwnPrefixes = { "HB URP Style/", "Hidden/HiddenBull/" };

        static bool s_Scanned;
        static bool s_KeepProcedural;
        static bool s_KeepDots;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_Scanned = false;
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || !Own(shader.name))
                return;

            Scan();

            var procedural = new ShaderKeyword(ProceduralKeyword);
            var dots = new ShaderKeyword(DotsKeyword);

            for (var index = data.Count - 1; index >= 0; index--)
            {
                var keywords = data[index].shaderKeywordSet;

                if ((!s_KeepProcedural && keywords.IsEnabled(procedural)) || (!s_KeepDots && keywords.IsEnabled(dots)))
                    data.RemoveAt(index);
            }
        }

        static bool Own(string name)
        {
            foreach (var prefix in s_OwnPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static void Scan()
        {
            if (s_Scanned)
                return;

            s_Scanned = true;
            s_KeepProcedural = AssetDatabase.FindAssets("t:" + nameof(HBStyleBillboardAsset)).Length > 0;
            s_KeepDots = Type.GetType(EntitiesGraphicsType) != null || ResidentDrawerUsed();
        }

        static bool ResidentDrawerUsed()
        {
            var assets = new List<RenderPipelineAsset> { GraphicsSettings.defaultRenderPipeline };

            for (var level = 0; level < QualitySettings.count; level++)
                assets.Add(QualitySettings.GetRenderPipelineAssetAt(level));

            foreach (var asset in assets)
            {
                if (asset is UniversalRenderPipelineAsset &&
                    new SerializedObject(asset).FindProperty("m_GPUResidentDrawerMode")?.intValue != 0)
                    return true;
            }

            return false;
        }
    }
}
