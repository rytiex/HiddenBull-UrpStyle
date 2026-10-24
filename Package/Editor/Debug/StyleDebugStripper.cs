using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleDebugStripper : IPreprocessShaders
    {
        static readonly ShaderKeyword s_Keyword = new ShaderKeyword(StyleDebug.Keyword);

        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (data[i].shaderKeywordSet.IsEnabled(s_Keyword))
                    data.RemoveAt(i);
            }
        }
    }
}
