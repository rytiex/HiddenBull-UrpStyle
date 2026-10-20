using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class TerrainShaderGUI : StyleShaderGUI, ITerrainLayerCustomUI
    {
        static class Styles
        {
            public static readonly GUIContent Diffuse = new GUIContent("Diffuse");
            public static readonly GUIContent Tint = new GUIContent("Color Tint", "Multiplies the layer's colour.");
            public static readonly GUIContent NormalMap = new GUIContent("Normal Map");
            public static readonly GUIContent NormalScale = new GUIContent("Normal Scale");

            public static readonly GUIContent MaskMap = new GUIContent("Mask Map",
                "R: Metallic, G: Ambient Occlusion, B: Height (shapes the borders between layers), A: Smoothness.");

            public static readonly GUIContent Remapping = new GUIContent("Channel Remapping",
                "Range each mask channel is stretched to.");

            public static readonly GUIContent Defaults = new GUIContent("Channel Default Values",
                "Values used while the layer has no mask map. Height then comes from the colour's brightness.");

            public static readonly GUIContent Metallic = new GUIContent("R: Metallic");
            public static readonly GUIContent Occlusion = new GUIContent("G: AO");
            public static readonly GUIContent Height = new GUIContent("B: Height");
            public static readonly GUIContent Smoothness = new GUIContent("A: Smoothness");
            public static readonly GUIContent SmoothnessSource = new GUIContent("Smoothness Source");

            public static readonly GUIContent Layers = new GUIContent("Layers");
            public static readonly GUIContent Shading = new GUIContent("Shading");
            public static readonly GUIContent Curvature = new GUIContent("Curvature");

            public static readonly GUIContent CurvatureAmount = new GUIContent(
                "Amount",
                "Darkens hollows and gullies and lifts ridges, read from the terrain's own shape, so the form " +
                "shows even on flat colours. 0 turns it off.");

            public static readonly GUIContent CurvatureRadius = new GUIContent(
                "Radius",
                "Size, in metres, of the bumps and hollows it picks up. Small finds ruts, large finds valleys.");

            public static readonly GUIContent CurvatureRidge = new GUIContent(
                "Ridge Light", "How much ridges and edges are lifted. 0 only darkens the hollows.");

            public static readonly GUIContent Weather = new GUIContent("Weather");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");
            public static readonly GUIContent Grass = new GUIContent("Grass");

            public const string GrassHint =
                "Grass grows around the camera on every layer whose Grass is above 0; set it per layer in Tools > " +
                "HiddenBull > URP Style > Terrain Layers. How dense it is and how far it reaches is set by the " +
                "Grass quality on the HiddenBull Style renderer feature.";

            public static readonly GUIContent GrassTipLift = new GUIContent(
                "Tip Brightness",
                "How much lighter than the ground the top of a clump is on layers without their own Tip Color. " +
                "0 leaves the clump almost one colour.");

            public static readonly GUIContent GrassTranslucency = new GUIContent(
                "Translucency",
                "Colours of the glow a low sun sends through the tips of the grass. Turned on and off with " +
                "Translucency under Grass on the HiddenBull Style renderer feature.");

            public static readonly GUIContent FlowerTexture = new GUIContent(
                "Flower Texture",
                "Row of flower pictures, side by side, from the Grass page of Tools > HiddenBull > URP Style > " +
                "Texture Studio. Left empty, no flowers grow.");

            public static readonly GUIContent FlowerVariants = new GUIContent(
                "Variants", "How many flowers the picture holds side by side.");

            public static readonly GUIContent GrassGlowSun = new GUIContent(
                "In Sun", "Tint of the glow where the sun reaches the grass. It is multiplied by the sun colour.");

            public static readonly GUIContent GrassGlowShade = new GUIContent(
                "In Shade",
                "Tint of the glow where the grass stands in shadow. Black leaves shaded grass without one.");

            public static readonly GUIContent GrassTexture = new GUIContent(
                "Clump Texture",
                "Picture of one clump from the Grass page of Tools > HiddenBull > URP Style > Texture Studio: " +
                "alpha is its shape, colour shades the layer's colours. Left empty, no grass is drawn.");

            public static readonly GUIContent LayerStrokes = new GUIContent(
                "Layer Strokes",
                "Where two terrain layers meet, the brush decides stroke by stroke which one shows, " +
                "so grass runs into rock along painted strokes instead of a soft blur. Inside a " +
                "layer nothing changes. Needs the brush on.");

            public static readonly GUIContent SteepMapping = new GUIContent(
                "Steep Slope Mapping",
                "On cliffs and steep banks the layers are laid on from the side instead of from " +
                "above, so their textures do not stretch into streaks. Each spot uses the side it faces most; " +
                "only the narrow bands where two sides meet read the textures twice.");

            public const string LayerWeatherHint =
                "Wetness and Snow Cover are set per layer in Tools > HiddenBull > URP Style > Terrain Layers.";

            public static readonly GUIContent DetailDistance = new GUIContent(
                "Detail Distance",
                "Metres from the camera where the layers' texture detail has fully faded into each layer's " +
                "average colour, so far hills read as calm painted patches. The fade starts at half this " +
                "distance; beyond it the layers cost one texture read each.");

            public static readonly GUIContent MacroVariation = new GUIContent(
                "Macro Variation",
                "Large, soft light and dark patches across the whole terrain so repeated textures do not read as " +
                "one flat carpet from afar. 0 turns it off.");

            public static readonly GUIContent EdgeSoftness = new GUIContent(
                "Edge Softness",
                "How soft the borders between layers are. Low values make crisp borders shaped by each layer's " +
                "height in its mask map and by the brush strokes.");

            public static readonly GUIContent Specular = new GUIContent(
                "Specular",
                "Highlights and reflections, set per layer by the Metallic and Smoothness of its mask map or " +
                "Terrain Layer. Off, the ground only shows them when it is wet.");

            public const string SpecularHint =
                "Metallic and Smoothness come from each Terrain Layer: its mask map (Channel Remapping) or, " +
                "without one, its Channel Default Values.";

            public static readonly GUIContent BrushToggle = new GUIContent(
                "Brush",
                "Paints the terrain with the project's brush: stroke-shaped light and shadow, layer " +
                "edges in strokes. The atlas and its scale are set on the renderer feature.");

            public static readonly GUIContent BrushScale = new GUIContent(
                "Size", "Stroke size relative to the project's. Keep it near 1 so the terrain and " +
                        "the objects on it read as one painting.");

            public static readonly GUIContent BrushRelief = new GUIContent(
                "Relief", "How far each stroke raises the surface along its middle.");

            public static readonly GUIContent BrushShading = new GUIContent(
                "Shading Break-up", "How far the strokes break the edge between light and shadow.");

            public static readonly GUIContent BrushAmbient = new GUIContent(
                "Ambient Break-up", "How far the strokes shift the ambient light.");

            public static readonly GUIContent BrushAlbedo = new GUIContent(
                "Albedo Variation", "How far the gaps between strokes darken the ground.");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap", "How softly light falls off toward the shadow side of hills.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness", "Width of each step between tones.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone", "Width of the middle tone between light and shadow.");

            public static readonly GUIContent BakedTones = new GUIContent(
                "Baked Tones", "How far lightmapped light is stepped into tones.");

            public static readonly GUIContent BakedSoftness = new GUIContent(
                "Softness", "Width of each lightmapped step.");

            public static readonly GUIContent ReceiveWeather = new GUIContent(
                "Receive Weather",
                "Rain wets the ground and snow settles on it wherever the sky is open, like every " +
                "other surface, starting on the high ground.");

            public static readonly GUIContent PerPixelNormal = new GUIContent(
                "Per-pixel Normal",
                "With GPU instancing on the terrain, reads the terrain's normal per pixel, so distant " +
                "low-detail patches keep their shape in the light.");
        }

        static bool s_MaskExpanded = true;

        MaterialProperty m_LayerStrokes;
        MaterialProperty m_SteepMapping;
        MaterialProperty m_HeightTransition;
        MaterialProperty m_MacroVariation;
        MaterialProperty m_DetailDistance;
        MaterialProperty m_BrushEnabled;
        MaterialProperty m_SpecularEnabled;
        MaterialProperty m_BrushScale;
        MaterialProperty m_BrushRelief;
        MaterialProperty m_BrushShading;
        MaterialProperty m_BrushAmbient;
        MaterialProperty m_BrushAlbedo;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_BakedTones;
        MaterialProperty m_BakedSoftness;
        MaterialProperty m_Curvature;
        MaterialProperty m_CurvatureRadius;
        MaterialProperty m_CurvatureRidge;
        MaterialProperty m_ReceiveWeather;
        MaterialProperty m_GrassTipLift;
        MaterialProperty m_FlowerTexture;
        MaterialProperty m_FlowerVariants;
        MaterialProperty m_GrassGlowSun;
        MaterialProperty m_GrassGlowShade;
        MaterialProperty m_GrassTexture;
        MaterialProperty m_PerPixelNormal;

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            EditorGUI.BeginChangeCheck();

            var diffuse = (Texture2D)EditorGUILayout.ObjectField(Styles.Diffuse, terrainLayer.diffuseTexture,
                                                                 typeof(Texture2D), false);
            var remap = terrainLayer.diffuseRemapMax;
            var tint = new Color(remap.x, remap.y, remap.z);

            using (new EditorGUI.IndentLevelScope())
                tint = EditorGUILayout.ColorField(Styles.Tint, tint, true, false, false);

            var normal = (Texture2D)EditorGUILayout.ObjectField(Styles.NormalMap, terrainLayer.normalMapTexture,
                                                                typeof(Texture2D), false);
            var normalScale = terrainLayer.normalScale;

            using (new EditorGUI.IndentLevelScope())
                normalScale = EditorGUILayout.FloatField(Styles.NormalScale, normalScale);

            var mask = (Texture2D)EditorGUILayout.ObjectField(Styles.MaskMap, terrainLayer.maskMapTexture,
                                                              typeof(Texture2D), false);
            var low = terrainLayer.maskMapRemapMin;
            var high = terrainLayer.maskMapRemapMax;
            var metallic = terrainLayer.metallic;
            var smoothness = terrainLayer.smoothness;
            var source = terrainLayer.smoothnessSource;

            using (new EditorGUI.IndentLevelScope())
            {
                var title = mask != null ? Styles.Remapping : Styles.Defaults;
                s_MaskExpanded = EditorGUILayout.Foldout(s_MaskExpanded, title, true);

                if (s_MaskExpanded && mask != null)
                {
                    Range(Styles.Metallic, ref low.x, ref high.x);
                    Range(Styles.Occlusion, ref low.y, ref high.y);
                    Range(Styles.Height, ref low.z, ref high.z);
                    Range(Styles.Smoothness, ref low.w, ref high.w);
                }
                else if (s_MaskExpanded)
                {
                    metallic = EditorGUILayout.Slider(Styles.Metallic, metallic, 0f, 1f);
                    high.y = EditorGUILayout.Slider(Styles.Occlusion, high.y, 0f, 1f);
                    low.y = Mathf.Min(low.y, high.y);
                    source = (TerrainLayerSmoothnessSource)EditorGUILayout.EnumPopup(Styles.SmoothnessSource, source);

                    if (source == TerrainLayerSmoothnessSource.Constant)
                        smoothness = EditorGUILayout.Slider(Styles.Smoothness, smoothness, 0f, 1f);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(terrainLayer, "Edit Terrain Layer");
                terrainLayer.diffuseTexture = diffuse;
                terrainLayer.diffuseRemapMin = Vector4.zero;
                terrainLayer.diffuseRemapMax = new Vector4(tint.r, tint.g, tint.b, 1f);
                terrainLayer.normalMapTexture = normal;
                terrainLayer.normalScale = normalScale;
                terrainLayer.maskMapTexture = mask;
                terrainLayer.maskMapRemapMin = low;
                terrainLayer.maskMapRemapMax = high;
                terrainLayer.metallic = metallic;
                terrainLayer.smoothness = smoothness;
                terrainLayer.smoothnessSource = source;
                EditorUtility.SetDirty(terrainLayer);
                StyleTerrainLayerSync.LayerEdited(terrainLayer);
            }

            var size = terrainLayer.tileSize;
            var offset = terrainLayer.tileOffset;

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(terrainLayer);

            if (terrainLayer.tileSize != size || terrainLayer.tileOffset != offset)
                StyleTerrainLayerSync.LayerEdited(terrainLayer);

            return true;
        }

        static void Range(GUIContent label, ref float low, ref float high)
        {
            EditorGUILayout.MinMaxSlider(label, ref low, ref high, 0f, 1f);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUI.BeginChangeCheck();

            DrawLayers(materialEditor);

            DrawGrass(materialEditor);

            DrawBrush(materialEditor);
            DrawShading(materialEditor);
            DrawSpecular(materialEditor);
            DrawCurvature(materialEditor);
            DrawWeather(materialEditor);
            DrawAdvanced(materialEditor);

            UnityEditor.Rendering.CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                ValidateTargets(materialEditor);
        }

        public override void ValidateMaterial(Material material) => Validate(material);

        public override void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
        }

        public override void OnMaterialInteractivePreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
        }

        public override void OnMaterialPreviewSettingsGUI(MaterialEditor materialEditor)
        {
        }

        public static void Validate(Material material)
        {
            SyncToggle(material, "_BrushEnabled", "_HB_BRUSH");
            SyncToggle(material, "_SpecularEnabled", "_HB_SPECULAR");
            SyncToggle(material, "_EnableInstancedPerPixelNormal", "_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_LayerStrokes = FindProperty("_LayerStrokes", properties);
            m_SteepMapping = FindProperty("_SteepMapping", properties);
            m_HeightTransition = FindProperty("_HeightTransition", properties);
            m_MacroVariation = FindProperty("_MacroVariation", properties);
            m_DetailDistance = FindProperty("_DetailDistance", properties);
            m_BrushEnabled = FindProperty("_BrushEnabled", properties);
            m_SpecularEnabled = FindProperty("_SpecularEnabled", properties);
            m_BrushScale = FindProperty("_BrushScale", properties);
            m_BrushRelief = FindProperty("_BrushRelief", properties);
            m_BrushShading = FindProperty("_BrushShading", properties);
            m_BrushAmbient = FindProperty("_BrushAmbient", properties);
            m_BrushAlbedo = FindProperty("_BrushAlbedo", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_BakedTones = FindProperty("_BakedTones", properties);
            m_BakedSoftness = FindProperty("_BakedSoftness", properties);
            m_Curvature = FindProperty("_Curvature", properties);
            m_CurvatureRadius = FindProperty("_CurvatureRadius", properties);
            m_CurvatureRidge = FindProperty("_CurvatureRidge", properties);
            m_ReceiveWeather = FindProperty("_ReceiveWeather", properties);
            m_GrassTipLift = FindProperty("_GrassTipLift", properties);
            m_FlowerTexture = FindProperty("_FlowerTexture", properties);
            m_FlowerVariants = FindProperty("_FlowerVariants", properties);
            m_GrassGlowSun = FindProperty("_GrassGlowSun", properties);
            m_GrassGlowShade = FindProperty("_GrassGlowShade", properties);
            m_GrassTexture = FindProperty("_GrassTexture", properties);
            m_PerPixelNormal = FindProperty("_EnableInstancedPerPixelNormal", properties);
        }

        void DrawLayers(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_LayerStrokes, m_SteepMapping, m_HeightTransition,
                                    m_MacroVariation, m_DetailDistance);
            var expanded = StyleMaterialHeader.Draw(Styles.Layers, "Terrain.Layers", materialEditor, reset: reset);

            if (expanded)
            {
                using (new EditorGUI.DisabledScope(!IsEnabled(m_BrushEnabled)))
                    materialEditor.ShaderProperty(m_LayerStrokes, Styles.LayerStrokes);

                materialEditor.ShaderProperty(m_SteepMapping, Styles.SteepMapping);
                materialEditor.ShaderProperty(m_HeightTransition, Styles.EdgeSoftness);
                materialEditor.ShaderProperty(m_MacroVariation, Styles.MacroVariation);
                materialEditor.ShaderProperty(m_DetailDistance, Styles.DetailDistance);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawSpecular(MaterialEditor materialEditor)
        {
            var expanded = StyleMaterialHeader.Draw(Styles.Specular, "Terrain.Specular", materialEditor,
                                                    m_SpecularEnabled);

            if (expanded)
                EditorGUILayout.HelpBox(Styles.SpecularHint, MessageType.None);

            StyleMaterialHeader.End(expanded);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BrushScale, m_BrushRelief, m_BrushShading, m_BrushAmbient,
                                    m_BrushAlbedo);
            var expanded = StyleMaterialHeader.Draw(Styles.BrushToggle, "Terrain.Brush", materialEditor,
                                                    m_BrushEnabled, reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_BrushScale, Styles.BrushScale);
                materialEditor.ShaderProperty(m_BrushRelief, Styles.BrushRelief);
                materialEditor.ShaderProperty(m_BrushShading, Styles.BrushShading);
                materialEditor.ShaderProperty(m_BrushAmbient, Styles.BrushAmbient);
                materialEditor.ShaderProperty(m_BrushAlbedo, Styles.BrushAlbedo);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawShading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseWrap, m_DiffuseSoftness, m_HalfTone, m_BakedTones,
                                    m_BakedSoftness);
            var expanded = StyleMaterialHeader.Draw(Styles.Shading, "Terrain.Shading", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
                materialEditor.ShaderProperty(m_BakedTones, Styles.BakedTones);

                if (m_BakedTones.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BakedSoftness, Styles.BakedSoftness);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawCurvature(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_Curvature, m_CurvatureRadius, m_CurvatureRidge);
            var expanded = StyleMaterialHeader.Draw(Styles.Curvature, "Terrain.Curvature", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_Curvature, Styles.CurvatureAmount);

                using (new EditorGUI.DisabledScope(m_Curvature.floatValue <= 0f))
                {
                    materialEditor.ShaderProperty(m_CurvatureRadius, Styles.CurvatureRadius);
                    materialEditor.ShaderProperty(m_CurvatureRidge, Styles.CurvatureRidge);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawGrass(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_GrassTipLift, m_GrassGlowSun,
                                    m_GrassGlowShade, m_GrassTexture, m_FlowerTexture, m_FlowerVariants);
            var expanded = StyleMaterialHeader.Draw(Styles.Grass, "Terrain.Grass", materialEditor, reset: reset);

            if (expanded)
            {
                EditorGUILayout.HelpBox(Styles.GrassHint, MessageType.None);
                materialEditor.ShaderProperty(m_GrassTipLift, Styles.GrassTipLift);
                EditorGUILayout.LabelField(Styles.GrassTranslucency);

                using (new EditorGUI.IndentLevelScope())
                {
                    materialEditor.ShaderProperty(m_GrassGlowSun, Styles.GrassGlowSun);
                    materialEditor.ShaderProperty(m_GrassGlowShade, Styles.GrassGlowShade);
                }

                materialEditor.TexturePropertySingleLine(Styles.GrassTexture, m_GrassTexture);
                materialEditor.TexturePropertySingleLine(Styles.FlowerTexture, m_FlowerTexture);

                using (new EditorGUI.DisabledScope(m_FlowerTexture.textureValue == null))
                using (new EditorGUI.IndentLevelScope())
                    materialEditor.ShaderProperty(m_FlowerVariants, Styles.FlowerVariants);
            }
            StyleMaterialHeader.End(expanded);
        }

        void DrawWeather(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_ReceiveWeather);
            var expanded = StyleMaterialHeader.Draw(Styles.Weather, "Terrain.Weather", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_ReceiveWeather, Styles.ReceiveWeather);

                EditorGUILayout.HelpBox(Styles.LayerWeatherHint, MessageType.None);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_PerPixelNormal);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Terrain.Advanced", materialEditor, reset: reset);

            if (expanded)
                materialEditor.ShaderProperty(m_PerPixelNormal, Styles.PerPixelNormal);

            StyleMaterialHeader.End(expanded);
        }
    }
}
