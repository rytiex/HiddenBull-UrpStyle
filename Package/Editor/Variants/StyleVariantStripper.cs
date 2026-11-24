using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleVariantStripper : IPreprocessBuildWithReport, IPreprocessShaders
    {
        static readonly ShaderKeyword s_Mixing = new ShaderKeyword(StyleVariantProject.MixingKeyword);
        static bool? s_StripMixing;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_StripMixing = StyleVariantProject.MixingUnused();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            s_StripMixing ??= StyleVariantProject.MixingUnused();

            if (!s_StripMixing.Value)
                return;

            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (data[i].shaderKeywordSet.IsEnabled(s_Mixing))
                    data.RemoveAt(i);
            }
        }
    }
}
