using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleGraphShaderGUI : ShaderGUI
    {
        const string UnlitGUI = "UnityEditor.ShaderGraphUnlitGUI";
        const string Metadata = "UnityEditor.ShaderGraph.ShaderGraphMetadata";

        static readonly HashSet<string> s_Internal = new HashSet<string> { "_ReceiveWeather" };

        readonly ShaderGUI m_Inner = CreateInner();
        readonly List<MaterialProperty> m_Shown = new List<MaterialProperty>();

        static ShaderGUI CreateInner()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(UnlitGUI);

                if (type != null && typeof(ShaderGUI).IsAssignableFrom(type))
                    return (ShaderGUI)Activator.CreateInstance(type);
            }

            return null;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            m_Shown.Clear();

            foreach (var property in properties)
            {
                if (!s_Internal.Contains(property.name))
                    m_Shown.Add(property);
            }

            var shown = m_Shown.ToArray();

            HideInternalCategories(materialEditor.target as Material);

            if (m_Inner != null)
                m_Inner.OnGUI(materialEditor, shown);
            else
                base.OnGUI(materialEditor, shown);
        }

        static void HideInternalCategories(Material material)
        {
            if (material == null || material.shader == null)
                return;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(material.shader)))
            {
                if (asset == null || asset.GetType().FullName != Metadata)
                    continue;

                var categories = Field(asset, "categoryDatas") as IList;

                if (categories == null)
                    return;

                for (var i = categories.Count - 1; i >= 0; i--)
                {
                    var entries = Field(categories[i], "propertyDatas") as IList;

                    if (entries != null && entries.Count > 0 && AllInternal(entries))
                        categories.RemoveAt(i);
                }

                return;
            }
        }

        static bool AllInternal(IList entries)
        {
            foreach (var entry in entries)
            {
                if (!(Field(entry, "referenceName") is string name) || !s_Internal.Contains(name))
                    return false;
            }

            return true;
        }

        static object Field(object target, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            return target?.GetType().GetField(name, flags)?.GetValue(target);
        }

        public override void ValidateMaterial(Material material)
        {
            if (m_Inner != null)
                m_Inner.ValidateMaterial(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (m_Inner != null)
                m_Inner.AssignNewShaderToMaterial(material, oldShader, newShader);
            else
                base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }

        public override void OnClosed(Material material)
        {
            if (m_Inner != null)
                m_Inner.OnClosed(material);
        }
    }
}
