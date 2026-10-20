using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public abstract class StyleShaderGUI : ShaderGUI
    {
        protected void ValidateTargets(MaterialEditor materialEditor)
        {
            foreach (var target in materialEditor.targets)
            {
                if (target is Material material)
                    ValidateMaterial(material);
            }
        }

        protected Action ResetAction(MaterialEditor materialEditor, params MaterialProperty[] section)
        {
            return () =>
            {
                if (materialEditor.target is not Material first)
                    return;

                var shader = first.shader;

                materialEditor.RegisterPropertyChangeUndo("Reset");

                foreach (var property in section)
                {
                    if (property == null)
                        continue;

                    var index = shader.FindPropertyIndex(property.name);

                    if (index < 0)
                        continue;

                    switch (shader.GetPropertyType(index))
                    {
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                        case ShaderPropertyType.Int:
                            property.floatValue = shader.GetPropertyDefaultFloatValue(index);
                            break;
                        case ShaderPropertyType.Color:
                            property.colorValue = shader.GetPropertyDefaultVectorValue(index);
                            break;
                        case ShaderPropertyType.Vector:
                            property.vectorValue = shader.GetPropertyDefaultVectorValue(index);
                            break;
                    }
                }

                ValidateTargets(materialEditor);
            };
        }

        protected static bool IsEnabled(MaterialProperty property) => property != null && property.floatValue >= 0.5f;

        protected static void DrawPopup(MaterialEditor materialEditor, MaterialProperty property, GUIContent label,
                                        string[] options)
        {
            var rect = EditorGUILayout.GetControlRect();

            MaterialEditor.BeginProperty(rect, property);
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();

            var value = EditorGUI.Popup(rect, label, Mathf.RoundToInt(property.floatValue),
                                        EditorGUIUtility.TrTempContent(options));

            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = value;
            }

            EditorGUI.showMixedValue = false;
            MaterialEditor.EndProperty();
        }

        protected static void SetKeyword(Material material, string keyword, bool on)
        {
            CoreUtils.SetKeyword(material, keyword, on);
        }

        protected static bool GetToggle(Material material, string property)
        {
            return material.HasProperty(property) && material.GetFloat(property) >= 0.5f;
        }

        protected static void SyncToggle(Material material, string property, string keyword)
        {
            if (material.HasProperty(property))
                CoreUtils.SetKeyword(material, keyword, material.GetFloat(property) >= 0.5f);
        }

        protected static void SyncEmission(Material material, string toggle, string colour)
        {
            if (!material.HasProperty(toggle))
                return;

            var flags = material.globalIlluminationFlags;

            flags = material.GetFloat(toggle) >= 0.5f && material.HasProperty(colour)
                ? MaterialEditor.FixupEmissiveFlag(material.GetColor(colour), flags)
                : flags | MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            if (material.globalIlluminationFlags != flags)
                material.globalIlluminationFlags = flags;
        }

        protected static MaterialProperty Optional(string name, MaterialProperty[] properties)
        {
            return FindProperty(name, properties, false);
        }
    }
}
