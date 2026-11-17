using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleGrassPage : StyleStudioPage
    {
        const float MostPicture = 220f;
        const int PictureColumns = 4;
        const string RenameControl = "HB Studio Plant Rename";

        static class Styles
        {
            internal static readonly GUIContent Title = new GUIContent("Grass",
                "Grass cards and flower heads, shown on a small HB terrain.");

            internal static readonly GUIContent[] Targets =
            {
                new GUIContent("Card", "Shape of one grass plant for terrain layers' Card Texture. Only the shape " +
                                       "is used: the colour comes from the ground and the Tip colour."),
                new GUIContent("Flower", "Flower heads for terrain layers' Flower Texture, in full colour. Every " +
                                         "flower in the list is one variant; Save puts them side by side.")
            };

            internal static readonly GUIContent Preview = new GUIContent("Preview",
                "Ground and grass of the preview. Nothing here is saved.");

            internal static readonly GUIContent Ground = new GUIContent("Ground",
                "Terrain layer the preview ground is painted with. Only its textures, tint, size and surface are " +
                "used; the asset does not change.");

            internal static readonly GUIContent GrassHeight = new GUIContent("Grass Height",
                "Height of the grass in the preview, in metres. Never saved.");

            internal static readonly GUIContent GrassShare = new GUIContent("Grass Share",
                "How much of the preview grass is drawn with the card: 0 for leaves only, 1 for cards only.");

            internal static readonly GUIContent FlowerShare = new GUIContent("Flower Share",
                "How much of the preview grass is in flower.");

            internal static readonly GUIContent Export = new GUIContent("Export",
                "Name, folder and Save for the open tab's texture.");

            internal static readonly GUIContent Output = new GUIContent("Output");
            internal static readonly GUIContent Name = new GUIContent("Name",
                "Name the texture is saved under, as TEX_Words_Like_This: \"clump grass\" saves as TEX_Clump_Grass.");

            internal static readonly GUIContent Save = new GUIContent("Save",
                "Saves the open tab's texture as a PNG in the folder, ready for a terrain layer. Flowers are saved " +
                "side by side; set the terrain layer's Flower Variants to how many are shown.");

            internal static readonly GUIContent Flowers = new GUIContent("Flowers",
                "Each flower is one variant of the Flower Texture, in this order from the left. Hidden flowers are " +
                "left out. Click one to edit it below; double-click to rename it.");

            internal static readonly GUIContent AddFlower = new GUIContent("+",
                "Adds a flower with a random look after the picked one. Up to 8.");
            internal static readonly GUIContent DuplicateFlower = new GUIContent("Duplicate",
                "Copies the picked flower after itself.");
            internal static readonly GUIContent RemoveFlower = new GUIContent("−", "Removes the picked flower.");
            internal static readonly GUIContent EarlierFlower = new GUIContent("▲",
                "Moves the picked flower up, to the left in the texture.");
            internal static readonly GUIContent LaterFlower = new GUIContent("▼",
                "Moves the picked flower down, to the right in the texture.");

            internal static readonly GUIContent Shape = new GUIContent("Shape");
            internal static readonly GUIContent Colour = new GUIContent("Colour");
            internal static readonly GUIContent Placement = new GUIContent("Placement");

            internal static readonly GUIContent Blades = new GUIContent("Blades", "How many leaves the plant has.");
            internal static readonly GUIContent Height = new GUIContent("Height",
                "Height of the tallest leaf, as a share of the picture.");
            internal static readonly GUIContent HeightVariation = new GUIContent("Height Variation",
                "How much shorter some leaves are: 0 keeps every leaf at Height.");
            internal static readonly GUIContent Width = new GUIContent("Width",
                "Width of a leaf at its foot, as a share of the picture.");
            internal static readonly GUIContent Fan = new GUIContent("Fan",
                "How far the outer leaves lean out to the sides.");
            internal static readonly GUIContent Spread = new GUIContent("Spread",
                "How far apart the leaves' feet stand: 0 grows them all from one point.");

            internal static readonly GUIContent PetalShape = new GUIContent("Petal Shape", "Outline of the petals.");
            internal static readonly GUIContent PetalsMin = new GUIContent("Petals Min",
                "Fewest petals in one ring.");
            internal static readonly GUIContent PetalsMax = new GUIContent("Petals Max",
                "Most petals in one ring. The flower picks a count between Min and Max by its Seed.");
            internal static readonly GUIContent Rings = new GUIContent("Rings",
                "Rings of petals, each one smaller and brighter than the one under it.");
            internal static readonly GUIContent PetalLength = new GUIContent("Petal Length",
                "Length of the outer petals, from the middle, as a share of the picture.");
            internal static readonly GUIContent PetalWidth = new GUIContent("Petal Width",
                "Width of a petal at its widest, as a share of its length.");
            internal static readonly GUIContent Irregularity = new GUIContent("Irregularity",
                "Uneven spacing, length and width of the petals.");
            internal static readonly GUIContent CentreSize = new GUIContent("Centre Size",
                "Size of the middle disc, as a share of the picture. 0 hides it.");
            internal static readonly GUIContent PetalVariation = new GUIContent("Petal Variation",
                "How much the petals' brightness differs.");

            internal static readonly GUIContent Petal = new GUIContent("Petal", "Colour of the petals.");
            internal static readonly GUIContent Centre = new GUIContent("Centre", "Colour of the middle disc.");
            internal static readonly GUIContent Shading = new GUIContent("Shading",
                "Darkens the petals' feet and edges and rings the middle disc, for depth.");

            internal static readonly GUIContent Offset = new GUIContent("Offset",
                "Moves the drawing in the picture. Blades stand on the bottom edge; a flower sits in the middle.");
            internal static readonly GUIContent Scale = new GUIContent("Scale", "Size of the drawing.");
            internal static readonly GUIContent Seed = new GUIContent("Seed",
                "Changes the random look without changing the settings.");

            internal static readonly GUIContent Visible = EditorGUIUtility.TrIconContent("animationvisibilitytoggleon",
                "Shown. Click to leave the flower out of the texture.");
            internal static readonly GUIContent Hidden = EditorGUIUtility.TrIconContent("animationvisibilitytoggleoff",
                "Left out. Click to put the flower back in the texture.");

            internal static readonly Color selection = new Color(0.24f, 0.48f, 0.9f, 0.35f);
            internal static readonly Color backdrop = new Color(0.12f, 0.12f, 0.12f, 1f);
        }

        [SerializeField]
        HBStyleTerrainLayer m_Ground;

        [SerializeField]
        StyleGrassDesign m_Design = new StyleGrassDesign();

        [SerializeField]
        StylePlantKind m_Target;

        [SerializeField]
        int m_FlowerSelected;

        [NonSerialized]
        StyleStudioPreview m_Preview;

        [NonSerialized]
        StyleStudioField m_Field;

        [NonSerialized]
        StylePlantGenerator m_CardGenerator;

        [NonSerialized]
        StylePlantGenerator m_FlowerGenerator;

        [NonSerialized]
        StyleStudioUndoState m_Undo;

        [NonSerialized]
        SerializedObject m_Layer;

        [NonSerialized]
        bool m_Growing;

        [NonSerialized]
        float m_PictureWidth = 260f;

        [NonSerialized]
        System.Random m_Random;

        [NonSerialized]
        bool m_Shown;

        [NonSerialized]
        bool m_CardDirty = true;

        [NonSerialized]
        bool m_FlowerDirty = true;

        [NonSerialized]
        int m_Renaming = -1;

        [NonSerialized]
        bool m_Focus;

        StyleStudioPreview preview => m_Preview ??= new StyleStudioPreview();

        StyleStudioField field => m_Field ??= new StyleStudioField();

        StylePlantGenerator cardGenerator => m_CardGenerator ??= new StylePlantGenerator();

        StylePlantGenerator flowerGenerator => m_FlowerGenerator ??= new StylePlantGenerator();

        StylePlantGenerator generator => m_Target == StylePlantKind.Flower ? flowerGenerator : cardGenerator;

        StyleStudioUndoState undo =>
            m_Undo != null ? m_Undo : m_Undo = StyleStudioUndoState.Create(m_Design, Restored);

        StylePlant plant => m_Target == StylePlantKind.Flower ? m_Design.flower : m_Design.card;

        int selected
        {
            get => Mathf.Clamp(m_FlowerSelected, 0, Mathf.Max(m_Design.flower.parts.Count - 1, 0));
            set => m_FlowerSelected = value;
        }

        internal override GUIContent title => Styles.Title;

        internal override bool hasLayers => false;

        void MarkDirty()
        {
            if (m_Target == StylePlantKind.Flower)
                m_FlowerDirty = true;
            else
                m_CardDirty = true;
        }

        internal override void DrawSettings()
        {
            var history = undo;

            m_Design.flower.parts.RemoveAll(item => item.kind != StylePlantPartKind.PetalHead);
            EditorGUILayout.Space(4f);
            DrawTargets();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.Preview, EditorStyles.boldLabel);
            DrawGround();
            DrawGrowth();
            EditorGUILayout.Space();
            DrawPicture();
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            StylePlantPart current;

            if (m_Target == StylePlantKind.Flower)
            {
                DrawFlowers();
                current = m_Design.flower.parts.Count > 0 ? m_Design.flower.parts[selected] : null;
                EditorGUILayout.Space();
            }
            else
            {
                current = m_Design.card.Card();
            }

            if (current != null)
                DrawPart(current);

            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            history.Commit("Change Texture Studio Plant");
        }

        void DrawPart(StylePlantPart current)
        {
            var flower = current.kind == StylePlantPartKind.PetalHead;
            var open = StyleSectionHeader.Draw(Styles.Shape, "TextureStudio.Plant.Shape",
                                               () => Reset(current.ResetShape));
            if (open)
            {
                if (flower)
                    DrawPetals(current);
                else
                    DrawBlades(current);
            }

            StyleSectionHeader.End(open);

            if (flower)
            {
                open = StyleSectionHeader.Draw(Styles.Colour, "TextureStudio.Plant.Colour",
                                               () => Reset(current.ResetColour));
                if (open)
                {
                    current.petal = EditorGUILayout.ColorField(Styles.Petal, current.petal, true, false, false);
                    current.centre = EditorGUILayout.ColorField(Styles.Centre, current.centre, true, false, false);
                    current.shading = EditorGUILayout.Slider(Styles.Shading, current.shading, 0f, 1f);
                }

                StyleSectionHeader.End(open);
            }

            open = StyleSectionHeader.Draw(Styles.Placement, "TextureStudio.Plant.Placement",
                                           () => Reset(current.ResetPlacement));
            if (open)
            {
                var offset = EditorGUILayout.Vector2Field(Styles.Offset, current.offset);

                current.offset = new Vector2(Mathf.Clamp(offset.x, -0.5f, 0.5f), Mathf.Clamp(offset.y, -0.5f, 0.5f));
                current.scale = EditorGUILayout.Slider(Styles.Scale, current.scale, 0.1f, 2f);
                current.seed = EditorGUILayout.IntSlider(Styles.Seed, current.seed, 1, 999);
            }

            StyleSectionHeader.End(open);
            CoreEditorUtils.DrawSplitter();
        }

        static void DrawBlades(StylePlantPart item)
        {
            item.blades = EditorGUILayout.IntSlider(Styles.Blades, item.blades, 1, StylePlantPart.MostBlades);
            item.height = EditorGUILayout.Slider(Styles.Height, item.height, 0.1f, 1f);
            item.heightVariation = EditorGUILayout.Slider(Styles.HeightVariation, item.heightVariation, 0f, 1f);
            item.width = EditorGUILayout.Slider(Styles.Width, item.width, 0.005f, 0.15f);
            item.fan = EditorGUILayout.Slider(Styles.Fan, item.fan, 0f, 1f);
            item.spread = EditorGUILayout.Slider(Styles.Spread, item.spread, 0f, 1f);
        }

        static void DrawPetals(StylePlantPart item)
        {
            item.shape = (StylePetalShape)EditorGUILayout.EnumPopup(Styles.PetalShape, item.shape);
            item.petalsMin = EditorGUILayout.IntSlider(Styles.PetalsMin, item.petalsMin, 3, StylePlantPart.MostPetals);
            item.petalsMax = EditorGUILayout.IntSlider(Styles.PetalsMax, item.petalsMax, 3, StylePlantPart.MostPetals);
            item.rings = EditorGUILayout.IntSlider(Styles.Rings, item.rings, 1, StylePlantPart.MostRings);
            item.petalLength = EditorGUILayout.Slider(Styles.PetalLength, item.petalLength, 0.1f, 0.5f);
            item.petalWidth = EditorGUILayout.Slider(Styles.PetalWidth, item.petalWidth, 0.1f, 1.5f);
            item.irregularity = EditorGUILayout.Slider(Styles.Irregularity, item.irregularity, 0f, 1f);
            item.centreSize = EditorGUILayout.Slider(Styles.CentreSize, item.centreSize, 0f, 0.3f);
            item.petalVariation = EditorGUILayout.Slider(Styles.PetalVariation, item.petalVariation, 0f, 1f);
        }

        void Reset(Action reset)
        {
            reset();
            MarkDirty();
            undo.Commit("Reset Texture Studio Section");
            host?.Repaint();
        }

        void DrawTargets()
        {
            var target = (StylePlantKind)GUILayout.Toolbar((int)m_Target, Styles.Targets, EditorStyles.miniButton);

            if (target == m_Target)
                return;

            m_Target = target;
            m_Renaming = -1;
            GUI.FocusControl(null);
        }

        void DrawFlowers()
        {
            var flowers = m_Design.flower.parts;

            EditorGUILayout.LabelField(Styles.Flowers, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (var i = 0; i < flowers.Count; i++)
                    DrawFlowerRow(flowers, i);
            }

            var index = selected;
            var count = flowers.Count;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(count >= StylePlant.MostVariants))
                {
                    if (GUILayout.Button(Styles.AddFlower, EditorStyles.miniButtonLeft))
                    {
                        var random = m_Random ??= new System.Random();

                        flowers.Insert(count == 0 ? 0 : index + 1, StylePlantPart.RandomFlower(random));
                        Pick(count == 0 ? 0 : index + 1);
                    }

                    using (new EditorGUI.DisabledScope(count == 0))
                    {
                        if (GUILayout.Button(Styles.DuplicateFlower, EditorStyles.miniButtonMid))
                        {
                            flowers.Insert(index + 1, flowers[index].Clone());
                            Pick(index + 1);
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(count <= 1))
                {
                    if (GUILayout.Button(Styles.RemoveFlower, EditorStyles.miniButtonMid))
                    {
                        flowers.RemoveAt(index);
                        Pick(Mathf.Max(index - 1, 0));
                    }
                }

                using (new EditorGUI.DisabledScope(index <= 0))
                {
                    if (GUILayout.Button(Styles.EarlierFlower, EditorStyles.miniButtonMid))
                        Swap(flowers, index, index - 1);
                }

                using (new EditorGUI.DisabledScope(index >= count - 1))
                {
                    if (GUILayout.Button(Styles.LaterFlower, EditorStyles.miniButtonRight))
                        Swap(flowers, index, index + 1);
                }
            }
        }

        void Pick(int index)
        {
            selected = index;
            m_Renaming = -1;
            GUI.FocusControl(null);
        }

        void Swap(List<StylePlantPart> flowers, int a, int b)
        {
            (flowers[a], flowers[b]) = (flowers[b], flowers[a]);
            selected = b;
        }

        void DrawFlowerRow(List<StylePlantPart> flowers, int index)
        {
            var row = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            var eye = new Rect(row.x + 4f, row.y + 12f, 16f, 16f);
            var picture = new Rect(eye.xMax + 6f, row.y + 4f, 32f, 32f);
            var text = new Rect(picture.xMax + 8f, row.y + 4f, row.xMax - picture.xMax - 12f, 16f);
            var sub = new Rect(text.x, text.yMax, text.width, 16f);
            var item = flowers[index];

            if (Event.current.type == EventType.Repaint)
            {
                if (index == selected)
                    EditorGUI.DrawRect(row, Styles.selection);

                EditorGUI.DrawRect(picture, Styles.backdrop);

                var thumbnail = flowerGenerator.Thumbnail(index);

                if (thumbnail != null)
                    GUI.DrawTexture(picture, thumbnail, ScaleMode.ScaleToFit, true);

                var faded = GUI.color;

                if (!item.visible)
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);

                if (m_Renaming != index)
                    EditorStyles.boldLabel.Draw(text, item.label, false, false, false, false);

                EditorStyles.miniLabel.Draw(sub, Subtitle(item), false, false, false, false);
                GUI.color = faded;
            }

            item.visible = GUI.Toggle(eye, item.visible, item.visible ? Styles.Visible : Styles.Hidden,
                                      GUIStyle.none);

            if (m_Renaming == index)
                DrawRename(item, text);

            var mouse = Event.current.mousePosition;

            if (Event.current.type != EventType.MouseDown || !row.Contains(mouse) || eye.Contains(mouse) ||
                (m_Renaming == index && text.Contains(mouse)))
                return;

            selected = index;
            m_Renaming = Event.current.clickCount == 2 ? index : -1;
            m_Focus = m_Renaming == index;
            GUI.FocusControl(null);
            Event.current.Use();
        }

        void DrawRename(StylePlantPart item, Rect text)
        {
            GUI.SetNextControlName(RenameControl);
            EditorGUI.BeginChangeCheck();

            var label = EditorGUI.DelayedTextField(text, item.label);

            if (EditorGUI.EndChangeCheck())
            {
                item.label = label;
                m_Renaming = -1;
            }
            else if (GUI.GetNameOfFocusedControl() == RenameControl)
            {
                m_Focus = false;
            }
            else if (m_Focus)
            {
                EditorGUI.FocusTextInControl(RenameControl);
            }
            else if (Event.current.type == EventType.Repaint)
            {
                m_Renaming = -1;
            }
        }

        static string Subtitle(StylePlantPart item)
        {
            var low = Mathf.Min(item.petalsMin, item.petalsMax);
            var high = Mathf.Max(item.petalsMin, item.petalsMax);

            return low == high ? $"{item.shape} · {low}" : $"{item.shape} · {low}–{high}";
        }

        void DrawPicture()
        {
            var cells = plant.variantCount;
            var columns = Mathf.Min(cells, PictureColumns);
            var rows = (cells + columns - 1) / columns;
            var line = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint && line.width > 1f)
                m_PictureWidth = line.width;

            var across = m_Target == StylePlantKind.Flower ? PictureColumns : 1;
            var side = Mathf.Min(MostPicture, (m_PictureWidth - 4f) / across);
            var rect = GUILayoutUtility.GetRect(side * columns, side * rows, GUILayout.ExpandWidth(true));
            var left = rect.center.x - side * columns * 0.5f;

            if (Event.current.type != EventType.Repaint)
                return;

            for (var cell = 0; cell < cells; cell++)
            {
                var area = new Rect(left + cell % columns * side, rect.y + cell / columns * side, side, side);
                var inner = new Rect(area.x + 1f, area.y + 1f, area.width - 2f, area.height - 2f);

                EditorGUI.DrawRect(inner, Styles.backdrop);

                if (generator.ready)
                    GUI.DrawTextureWithTexCoords(inner, generator.output, new Rect((float)cell / cells, 0f,
                                                                                   1f / cells, 1f), true);
            }
        }

        internal override void DrawLayers()
        {
        }

        internal override void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var export = GUILayoutUtility.GetRect(Styles.Export, EditorStyles.toolbarDropDown,
                                                      GUILayout.Width(72f));

                if (GUI.Button(export, Styles.Export, EditorStyles.toolbarDropDown))
                    PopupWindow.Show(export, new ExportPopup(this));
            }

            GUILayout.EndArea();
        }

        sealed class ExportPopup : PopupWindowContent
        {
            readonly StyleGrassPage m_Page;

            internal ExportPopup(StyleGrassPage page)
            {
                m_Page = page;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(320f, 96f);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUIUtility.labelWidth = 80f;
                EditorGUILayout.LabelField(Styles.Output, EditorStyles.boldLabel);

                var current = m_Page.plant;

                current.name = EditorGUILayout.TextField(Styles.Name, current.name);
                m_Page.undo.Commit("Change Texture Studio Output");
                StyleStudioExport.DrawFolder();

                var blocked = !m_Page.generator.ready || string.IsNullOrWhiteSpace(current.name);

                using (new EditorGUI.DisabledScope(blocked))
                {
                    if (GUILayout.Button(Styles.Save))
                    {
                        editorWindow.Close();
                        m_Page.SaveOutput();
                    }
                }
            }
        }

        internal override void DrawPreview(Rect rect)
        {
            rect = preview.DrawFieldBar(rect);
            preview.singleFlower = m_Target == StylePlantKind.Flower;
            preview.singleVariant = SingleVariant();
            preview.DrawField(rect, field, cardGenerator.output, flowerGenerator.output,
                              m_Design.flower.variantCount);

            if (field.built && !m_Shown)
            {
                field.SetGround(m_Ground);
                m_Shown = true;
                m_CardDirty = true;
                m_FlowerDirty = true;
            }

            if (field.built && m_CardDirty)
            {
                cardGenerator.Generate(m_Design.card);
                field.SetCard(cardGenerator.output);
                m_CardDirty = false;
            }

            if (field.built && m_FlowerDirty)
            {
                flowerGenerator.Generate(m_Design.flower);
                field.SetFlower(flowerGenerator.output, m_Design.flower.variantCount);
                m_FlowerDirty = false;
            }

            if (Event.current.type == EventType.Repaint)
                host?.Repaint();
        }

        int SingleVariant()
        {
            var parts = m_Design.flower.parts;

            if (parts.Count == 0)
                return 0;

            return Mathf.Max(m_Design.flower.Drawn().IndexOf(parts[selected]), 0);
        }

        void DrawGround()
        {
            EditorGUI.BeginChangeCheck();
            m_Ground = (HBStyleTerrainLayer)EditorGUILayout.ObjectField(Styles.Ground, m_Ground,
                                                                          typeof(HBStyleTerrainLayer), false);

            if (EditorGUI.EndChangeCheck())
                m_Shown = false;
        }

        void DrawGrowth()
        {
            if (Event.current.type == EventType.Layout)
                m_Growing = field.layer != null;

            if (!m_Growing || field.layer == null)
                return;

            if (m_Layer == null || m_Layer.targetObject != field.layer)
                m_Layer = new SerializedObject(field.layer);

            m_Layer.Update();
            DrawGrowthField("m_GrassHeight", Styles.GrassHeight);
            DrawGrowthField("m_GrassCardShare", Styles.GrassShare);
            DrawGrowthField("m_FlowerShare", Styles.FlowerShare);
            m_Layer.ApplyModifiedProperties();
        }

        void DrawGrowthField(string name, GUIContent label)
        {
            var property = m_Layer.FindProperty(name);

            if (property != null)
                EditorGUILayout.PropertyField(property, label);
        }

        void SaveOutput()
        {
            var path = StyleStudioExport.Save(generator.Export(), plant.name.Trim(), StyleExportKind.Cutout);

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            GUIUtility.ExitGUI();
        }

        void Restored()
        {
            m_CardDirty = true;
            m_FlowerDirty = true;
            m_Renaming = -1;
            host?.Repaint();
        }

        internal override void Release()
        {
            m_Field?.Release();
            m_Field = null;
            m_Preview?.Release();
            m_Preview = null;
            m_CardGenerator?.Release();
            m_CardGenerator = null;
            m_FlowerGenerator?.Release();
            m_FlowerGenerator = null;

            if (m_Undo != null)
                m_Undo.Release();

            m_Undo = null;
            m_Layer = null;
            m_Shown = false;
            m_CardDirty = true;
            m_FlowerDirty = true;
            m_Renaming = -1;
        }
    }
}
