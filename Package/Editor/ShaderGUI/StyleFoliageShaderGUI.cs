using System;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleFoliageShaderGUI : StyleLitShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Maps = new GUIContent("Maps");
            public static readonly GUIContent Leaves = new GUIContent("Leaves");

            public static readonly GUIContent BrushSpace = new GUIContent(
                "Brush Space",
                "World keeps strokes running on from plant to plant. Object keeps them on a plant that gets moved " +
                "or placed many times. In both, strokes stay put while the wind moves the leaves.");

            public static readonly GUIContent CanopyShape = new GUIContent(
                "Canopy Shape",
                "How far the leaves light as the rounded canopy baked by Bake Plant instead of by their own normals " +
                "(0 to 1). 1 reads as one soft volume.");

            public static readonly GUIContent WindFlutter = new GUIContent(
                "Flutter", "How far single leaves shake, in metres. Sway is on HB Style Plant.");

            public static readonly GUIContent BodyMask = new GUIContent(
                "Mask Map", "Green is Occlusion. Import it with sRGB off.");

            public const string NotBaked =
                "A renderer in this selection is not baked, so it does not move in the wind, and leaves light by " +
                "their own normals. Add HB Style Plant to the plant's root and press Bake Plant.";
        }

        MaterialProperty m_CanopyShape;
        MaterialProperty m_WindFlutter;

        protected override GUIContent BrushSpaceLabel => Styles.BrushSpace;

        protected override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            m_CanopyShape = Optional("_CanopyShape", properties);
            m_WindFlutter = Optional("_WindFlutter", properties);
        }

        protected override void DrawSections(MaterialEditor materialEditor)
        {
            if (materialEditor.target is Material material && SelectionNotBaked(material))
                EditorGUILayout.HelpBox(Styles.NotBaked, MessageType.Warning);

            DrawSurface(materialEditor);
            DrawPlantMaps(materialEditor);
            DrawShading(materialEditor);
            DrawBrush(materialEditor);

            if (m_CanopyShape != null)
                DrawLeaves(materialEditor);

            DrawWeather(materialEditor);
            DrawAdvanced(materialEditor);
        }

        void DrawPlantMaps(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BaseMapEnabled, m_NormalMapEnabled, m_BumpScale,
                                    m_MaskMapEnabled);
            var expanded = StyleMaterialHeader.Draw(Styles.Maps, "Foliage.Maps", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_BaseMapEnabled, m_BaseMapEnabled.displayName);

                if (IsEnabled(m_BaseMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.TexturePropertySingleLine(new GUIContent(m_BaseMap.displayName), m_BaseMap);
                        materialEditor.TextureScaleOffsetProperty(m_BaseMap);
                    }
                }

                if (m_NormalMapEnabled != null)
                {
                    materialEditor.ShaderProperty(m_NormalMapEnabled, m_NormalMapEnabled.displayName);

                    if (IsEnabled(m_NormalMapEnabled))
                    {
                        using (new EditorGUI.IndentLevelScope())
                            materialEditor.TexturePropertySingleLine(new GUIContent(m_BumpMap.displayName), m_BumpMap,
                                                                     m_BumpScale);
                    }

                    materialEditor.ShaderProperty(m_MaskMapEnabled, m_MaskMapEnabled.displayName);

                    if (IsEnabled(m_MaskMapEnabled))
                    {
                        using (new EditorGUI.IndentLevelScope())
                            materialEditor.TexturePropertySingleLine(Styles.BodyMask, m_MaskMap);
                    }
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawLeaves(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_CanopyShape, m_WindFlutter);
            var expanded = StyleMaterialHeader.Draw(Styles.Leaves, "Foliage.Leaves", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_CanopyShape, Styles.CanopyShape);
                materialEditor.ShaderProperty(m_WindFlutter, Styles.WindFlutter);
            }

            StyleMaterialHeader.End(expanded);
        }

        static bool SelectionNotBaked(Material material)
        {
            foreach (var selected in Selection.gameObjects)
            {
                foreach (var filter in selected.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!filter.TryGetComponent<Renderer>(out var renderer) ||
                        Array.IndexOf(renderer.sharedMaterials, material) < 0)
                        continue;

                    if (filter.sharedMesh == null || !StylePlantBaker.IsBaked(filter.sharedMesh))
                        return true;
                }
            }

            return false;
        }
    }
}
