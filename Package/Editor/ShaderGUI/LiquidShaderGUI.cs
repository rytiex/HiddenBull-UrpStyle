using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class LiquidShaderGUI : StyleShaderGUI
    {
        const int Acid = 1;
        const int Lava = 2;
        const float OldLava = 1f;
        const float OldAcid = 2f;

        internal static class Styles
        {
            public static readonly GUIContent WavesHeader = new GUIContent("Waves");
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Colour = new GUIContent("Colour");
            public static readonly GUIContent Foam = new GUIContent("Foam & Bubbles");
            public static readonly GUIContent Crust = new GUIContent("Crust");
            public static readonly GUIContent Underwater = new GUIContent("Underwater");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");

            public static readonly GUIContent[] Presets =
            {
                new GUIContent("Water", "Sets every setting to the water defaults. Textures are kept."),
                new GUIContent("Acid", "Sets every setting to a glowing, bubbling acid. Textures are kept."),
                new GUIContent("Lava", "Sets every setting to glowing melt under a drifting crust. Textures are kept.")
            };

            public static readonly GUIContent Tiling = new GUIContent(
                "Tiling",
                "How many times the textures repeat per metre.");

            public static readonly GUIContent WindWaves = new GUIContent(
                "Wind Affects Waves",
                "Takes the height of the waves from Wind on Style Weather instead of the Waves slider: flat " +
                "at 0, tallest at 1, swelling over the Cover Time and calming over the Uncover Time. Turn it " +
                "off to set a fixed height here. While on, white caps follow the wind too: none in a " +
                "breeze, plenty in a storm.");

            public static readonly GUIContent WaveHeight = new GUIContent(
                "Wave Height",
                "Scales how tall the waves stand without changing how strong the wind or Waves setting is. " +
                "1 is the natural height.");

            public static readonly GUIContent WaveSteepness = new GUIContent(
                "Wave Steepness",
                "How sharp the crests are. 0 gives soft rolling swell, 1 the natural shape, higher values " +
                "pinch the crests into peaks and break more white caps.");

            public static readonly GUIContent WaveLength = new GUIContent(
                "Wave Length",
                "How far apart the waves are. Higher values give long, broad ocean swell, lower values short " +
                "choppy water. Longer waves also travel faster.");

            public static readonly GUIContent Waves = new GUIContent(
                "Waves",
                "How tall the waves are. They come from the shared wave simulation, follow the wind set on " +
                "Style Weather and never show tiling. They really lift the surface and bend the light on it. " +
                "High values break white caps on the crests.");

            public static readonly GUIContent NormalMap = new GUIContent(
                "Normal Map",
                "Ripples of the surface. Two copies drift across each other at different speeds and " +
                "angles, so the pattern never visibly repeats.");

            public static readonly GUIContent Smoothness = new GUIContent(
                "Smoothness", "How sharp the reflections and the sun's highlight are.");

            public static readonly GUIContent Refraction = new GUIContent(
                "Refraction", "How far the ripples bend what is seen through the surface.");

            public static readonly GUIContent RefractionBlur = new GUIContent(
                "Refraction Blur",
                "Blurs what is seen through the surface, on top of the blur murky liquid already adds " +
                "with depth. 0 leaves only that depth blur.");

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap", "How softly light wraps round foam and crust.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness", "Width of each step between the tones of foam and crust.");

            public static readonly GUIContent DepthGradient = new GUIContent(
                "Depth Gradient",
                "The liquid by how much of it you look through, from the shore on the left to deep water " +
                "on the right. Colour is the colour of the liquid there and alpha is how much it hides " +
                "what lies under it, so 0 leaves the bottom clear and 1 covers it. Raise alpha early for " +
                "murky water, keep it low for a clear lake. The middle of the gradient is about three " +
                "metres deep. The leftmost colour also tints what is seen through shallow water. Up to " +
                "six colour keys and six alpha keys are used.");

            public static readonly GUIContent Glow = new GUIContent(
                "Glow",
                "How much the liquid shines in its own colour on top of the light from the sun and sky. 0 is " +
                "ordinary water; higher values add light of its own on top, day and night, like acid or " +
                "lava. Raise the intensity of the gradient colours above 1 for a glow strong enough to bloom.");

            public static readonly GUIContent Caustics = new GUIContent(
                "Caustics",
                "The net of sunlight that ripples on the surface throw onto whatever lies under the liquid. " +
                "It follows the sun, stays out of shadow, is sharpest in the shallows and spreads and fades " +
                "as the gradient covers the bottom. 0 turns it off.");

            public static readonly GUIContent CausticScale = new GUIContent(
                "Caustic Scale",
                "The size of the cells in the net of light. 1 is about a metre across.");

            public static readonly GUIContent CrestColor = new GUIContent(
                "Crest Color",
                "Colour of the light that shows through the tops of the waves, strongest looking toward " +
                "the sun.");

            public static readonly GUIContent CrestSoftness = new GUIContent(
                "Crest Softness",
                "How gradually the crest colour fades down the wave. 0 keeps it to a sharp cap on the very " +
                "top; 1 spreads it softly over most of the wave.");

            public static readonly GUIContent FoamColor = new GUIContent("Foam Color");

            public static readonly GUIContent FoamDistance = new GUIContent(
                "Waterline Depth",
                "Depth of liquid, in metres, over which a thin line of foam hugs the shore and anything " +
                "standing in the liquid. Its width does not change with the viewing angle.");

            public static readonly GUIContent FoamAmount = new GUIContent(
                "Amount", "How strong all the foam is: waterline, shore foam and white caps. 0 turns it off.");

            public static readonly GUIContent ShoreFoam = new GUIContent(
                "Shore Foam",
                "How far from the coast, in metres, a loose band of foam reaches, with lines of foam walking " +
                "toward the shore inside it. The lines grow with the waves and nearly vanish on calm water. " +
                "0 turns it off.");

            public static readonly GUIContent BubbleAmount = new GUIContent(
                "Bubbles",
                "How many bubbles swell and burst across the surface, gathering along the shore. 0 turns " +
                "them off.");

            public static readonly GUIContent Pulse = new GUIContent(
                "Pulse", "How quickly bubbles swell and burst and how fast the glow under a crust throbs.");

            public static readonly GUIContent CrustAmount = new GUIContent(
                "Crust",
                "How much of the surface is covered by a solid, drifting crust, as on lava. 0 turns it off.");

            public static readonly GUIContent CrustColor = new GUIContent("Crust Color");

            public static readonly GUIContent CrustMap = new GUIContent(
                "Crust Map",
                "Its brightness decides where the crust forms, together with the brush pattern. Leave it " +
                "white for crust from the brush alone.");

            public static readonly GUIContent PatternScale = new GUIContent(
                "Pattern Scale",
                "Size of the foam cells, the sun glints, and the bubble and crust patches.");

            public static readonly GUIContent UnderShafts = new GUIContent(
                "Light Shafts",
                "Beams of sunlight seen from under the surface. They follow the sun, stay out of shadow and " +
                "fade with depth. 0 turns them off.");

            public static readonly GUIContent UnderBubbles = new GUIContent(
                "Bubbles", "Small bubbles drifting up past the camera while it is under the surface.");

            public static readonly GUIContent UnderDistortion = new GUIContent(
                "Distortion", "How much the view sways while the camera is under the surface.");

            public static readonly GUIContent ReceiveWeather = new GUIContent(
                "Rain Ripples",
                "Rings spread across the surface wherever rain falls on it, never under a roof.");
        }

        MaterialProperty m_Tiling;
        MaterialProperty m_Waves;
        MaterialProperty m_WindWaves;
        MaterialProperty m_WaveHeight;
        MaterialProperty m_WaveSteepness;
        MaterialProperty m_WaveLength;
        MaterialProperty m_BumpMap;
        MaterialProperty m_BumpScale;
        MaterialProperty m_Smoothness;
        MaterialProperty m_Refraction;
        MaterialProperty m_RefractionBlur;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        readonly MaterialProperty[] m_DepthColors = new MaterialProperty[DepthKeys];
        MaterialProperty m_DepthTimes0;
        MaterialProperty m_DepthTimes1;
        MaterialProperty m_DepthAlpha0;
        MaterialProperty m_DepthAlpha1;
        MaterialProperty m_DepthAlphaTimes0;
        MaterialProperty m_DepthAlphaTimes1;
        MaterialProperty m_Glow;
        MaterialProperty m_Caustics;
        MaterialProperty m_CausticScale;
        MaterialProperty m_CrestColor;
        MaterialProperty m_CrestSoftness;
        MaterialProperty m_FoamColor;
        MaterialProperty m_FoamDistance;
        MaterialProperty m_FoamAmount;
        MaterialProperty m_ShoreFoam;
        MaterialProperty m_BubbleAmount;
        MaterialProperty m_Pulse;
        MaterialProperty m_PatternScale;
        MaterialProperty m_CrustAmount;
        MaterialProperty m_CrustColor;
        MaterialProperty m_BaseMap;
        MaterialProperty m_UnderShafts;
        MaterialProperty m_UnderBubbles;
        MaterialProperty m_UnderDistortion;
        MaterialProperty m_ReceiveWeather;
        MaterialProperty m_QueueOffset;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            DrawPresets(materialEditor);

            EditorGUI.BeginChangeCheck();

            DrawWaves(materialEditor);
            DrawColour(materialEditor);
            DrawSurface(materialEditor);
            DrawFoam(materialEditor);
            DrawCrust(materialEditor);
            DrawUnderwater(materialEditor);
            DrawAdvanced(materialEditor);

            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                ValidateTargets(materialEditor);
        }

        public override void ValidateMaterial(Material material) => Validate(material);

        public static void Validate(Material material)
        {
            if (TakeSaved(material, "_Mode", out var old))
            {
                if (Mathf.Approximately(old, OldLava))
                    Molten(material, HotColour(material));
                else if (Mathf.Approximately(old, OldAcid))
                    material.SetFloat("_Glow", 1f);
                else
                    material.SetFloat("_BubbleAmount", 0f);

                EditorUtility.SetDirty(material);
            }

            SetKeyword(material, "_HB_LIQUID_CRUST", false);
            SetKeyword(material, "_HB_LIQUID_BUBBLES", false);

            material.SetShaderPassEnabled("HiddenBullOITMoments", true);
            material.SetShaderPassEnabled("HiddenBullOITRefraction", true);
            material.SetShaderPassEnabled("HiddenBullOITColor", true);
            material.SetShaderPassEnabled("HiddenBullSkyOcclusion", true);

            material.SetOverrideTag("RenderType", "Transparent");

            var queueOffset = material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            material.renderQueue = (int)RenderQueue.Transparent + queueOffset;
        }

        static Color HotColour(Material material)
        {
            using var serialized = new SerializedObject(material);

            var colours = serialized.FindProperty("m_SavedProperties.m_Colors");

            for (var index = 0; colours != null && index < colours.arraySize; index++)
            {
                var entry = colours.GetArrayElementAtIndex(index);

                if (entry.FindPropertyRelative("first").stringValue == "_HotColor")
                    return entry.FindPropertyRelative("second").colorValue;
            }

            return new Color(4f, 0.565445f, 0f, 1f);
        }

        static void Molten(Material material, Color hot)
        {
            WriteGradient(material, DepthGradient(new[]
            {
                (0f, hot),
                (1f, new Color(hot.r * 0.5f, hot.g * 0.15f, hot.b * 0.15f, 1f))
            }, new[] { (0f, 1f), (1f, 1f) }));
            material.SetFloat("_Glow", 1f);
            material.SetFloat("_Caustics", 0f);
            material.SetFloat("_FoamAmount", 0f);
            material.SetFloat("_Refraction", 0f);
            material.SetFloat("_RefractionBlur", 0f);
        }

        static bool TakeSaved(Material material, string name, out float value)
        {
            value = 0f;

            using var serialized = new SerializedObject(material);

            var floats = serialized.FindProperty("m_SavedProperties.m_Floats");

            if (floats == null)
                return false;

            for (var index = 0; index < floats.arraySize; index++)
            {
                var entry = floats.GetArrayElementAtIndex(index);

                if (entry.FindPropertyRelative("first").stringValue != name)
                    continue;

                value = entry.FindPropertyRelative("second").floatValue;
                floats.DeleteArrayElementAtIndex(index);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                return true;
            }

            return false;
        }

        void DrawPresets(MaterialEditor materialEditor)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var mode = 0; mode < Styles.Presets.Length; mode++)
                {
                    if (!GUILayout.Button(Styles.Presets[mode]))
                        continue;

                    materialEditor.RegisterPropertyChangeUndo(Styles.Presets[mode].text);

                    foreach (var target in materialEditor.targets)
                    {
                        if (target is not Material material)
                            continue;

                        ApplyPreset(material, mode);
                        Validate(material);
                        EditorUtility.SetDirty(material);
                    }

                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(2f);
        }

        static readonly string[] s_PresetFloats =
        {
            "_Tiling", "_Waves", "_WindWaves", "_WaveHeight", "_WaveSteepness", "_WaveLength", "_BumpScale",
            "_Smoothness", "_Refraction", "_RefractionBlur", "_DiffuseWrap", "_DiffuseSoftness", "_Caustics", "_Glow",
            "_CausticScale", "_CrestSoftness", "_FoamDistance", "_FoamAmount", "_ShoreFoam", "_BubbleAmount",
            "_Pulse", "_PatternScale", "_CrustAmount", "_UnderShafts", "_UnderBubbles", "_UnderDistortion"
        };

        static readonly float[][] s_PresetValues =
        {
            new[]
            {
                0.125f, 0.2f, 1f, 1f, 1f, 1f, 0.25f, 0.65f, 1f, 0.145f, 0.125f, 0.25f, 0.05f, 0f, 0.65f, 0.45f, 0.1f,
                0.3f, 2.5f, 0f, 1f, 1f, 0f, 0.5f, 0.25f, 0.3f
            },
            new[]
            {
                0.45f, 0.2f, 1f, 1f, 1f, 1f, 2f, 0.65f, 0f, 0f, 0.125f, 0.25f, 0.085f, 1f, 0.25f, 0f, 0.45f, 0.3f,
                2.5f, 0.125f, 1.5f, 3f, 0f, 0.4f, 0.65f, 0.3f
            },
            new[]
            {
                0.25f, 0.2f, 0f, 1.65f, 2f, 0.45f, 2f, 0.575f, 0f, 0f, 0.175f, 2f, 0f, 1f, 0.25f, 0f, 3f, 1f, 0f,
                0.0225f, 0.125f, 1f, 0.865f, 1f, 0f, 1f
            }
        };

        static readonly string[] s_PresetColours = { "_CrestColor", "_FoamColor", "_CrustColor" };

        static readonly Color[][] s_PresetTints =
        {
            new[]
            {
                new Color(0.7075472f, 0.7075472f, 0.7075472f, 1f), new Color(0.76495194f, 0.8158059f, 0.8490566f, 1f),
                new Color(0f, 0f, 0f, 1f)
            },
            new[]
            {
                new Color(0.7546641f, 1f, 0.3820755f, 1f), new Color(0.69319016f, 1f, 0.06132078f, 1f),
                new Color(0f, 0f, 0f, 1f)
            },
            new[]
            {
                new Color(0.44339618f, 0.44339618f, 0.44339618f, 1f), new Color(1f, 0.69112843f, 0f, 1f),
                new Color(0.16981131f, 0.07429245f, 0f, 1f)
            }
        };

        public static void ApplyPreset(Material material, int mode)
        {
            for (var index = 0; index < s_PresetFloats.Length; index++)
                material.SetFloat(s_PresetFloats[index], s_PresetValues[mode][index]);

            for (var index = 0; index < s_PresetColours.Length; index++)
                material.SetColor(s_PresetColours[index], s_PresetTints[mode][index]);

            material.SetFloat("_ReceiveWeather", 1f);

            if (mode == Lava)
            {
                WriteGradient(material, DepthGradient(new[]
                {
                    (0f, new Color(4f, 0.565445f, 0f, 1f)),
                    (1f, new Color(0.1509434f, 0.024598183f, 0f, 1f))
                }, new[] { (0f, 1f), (1f, 1f) }));

                return;
            }

            if (mode == Acid)
            {
                WriteGradient(material, DepthGradient(new[]
                {
                    (0f, new Color(0.1648759f, 0.7176471f, 0f, 1f)),
                    (0.30000764f, new Color(0.055667076f, 0.34117648f, 0f, 1f)),
                    (1f, new Color(0.2601031f, 0.4245283f, 0f, 1f))
                }, new[] { (0f, 0f), (0.6500038f, 0.7538165f), (1f, 1f) }));

                return;
            }

            WriteGradient(material, DepthGradient(new[]
            {
                (0f, new Color(0f, 0.7176471f, 0.6846459f, 1f)),
                (0.30000764f, new Color(0f, 0.21511805f, 0.33962262f, 1f)),
                (1f, new Color(0f, 0.0031320888f, 0.028301895f, 1f))
            }, new[]
            {
                (0f, 0f), (0.28824294f, 0.1254902f), (0.57941556f, 0.2509804f), (0.8f, 0.7538165f), (1f, 1f)
            }));
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_Tiling = FindProperty("_Tiling", properties);
            m_Waves = FindProperty("_Waves", properties);
            m_WindWaves = FindProperty("_WindWaves", properties);
            m_WaveHeight = FindProperty("_WaveHeight", properties);
            m_WaveSteepness = FindProperty("_WaveSteepness", properties);
            m_WaveLength = FindProperty("_WaveLength", properties);
            m_BumpMap = FindProperty("_BumpMap", properties);
            m_BumpScale = FindProperty("_BumpScale", properties);
            m_Smoothness = FindProperty("_Smoothness", properties);
            m_Refraction = FindProperty("_Refraction", properties);
            m_RefractionBlur = FindProperty("_RefractionBlur", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);

            for (var index = 0; index < DepthKeys; index++)
                m_DepthColors[index] = FindProperty(s_DepthColors[index], properties);

            m_DepthTimes0 = FindProperty("_DepthTimes0", properties);
            m_DepthTimes1 = FindProperty("_DepthTimes1", properties);
            m_DepthAlpha0 = FindProperty("_DepthAlpha0", properties);
            m_DepthAlpha1 = FindProperty("_DepthAlpha1", properties);
            m_DepthAlphaTimes0 = FindProperty("_DepthAlphaTimes0", properties);
            m_DepthAlphaTimes1 = FindProperty("_DepthAlphaTimes1", properties);
            m_Glow = FindProperty("_Glow", properties);
            m_Caustics = FindProperty("_Caustics", properties);
            m_CausticScale = FindProperty("_CausticScale", properties);
            m_CrestColor = FindProperty("_CrestColor", properties);
            m_CrestSoftness = FindProperty("_CrestSoftness", properties);
            m_FoamColor = FindProperty("_FoamColor", properties);
            m_FoamDistance = FindProperty("_FoamDistance", properties);
            m_FoamAmount = FindProperty("_FoamAmount", properties);
            m_ShoreFoam = FindProperty("_ShoreFoam", properties);
            m_BubbleAmount = FindProperty("_BubbleAmount", properties);
            m_Pulse = FindProperty("_Pulse", properties);
            m_PatternScale = FindProperty("_PatternScale", properties);
            m_CrustAmount = FindProperty("_CrustAmount", properties);
            m_CrustColor = FindProperty("_CrustColor", properties);
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_UnderShafts = FindProperty("_UnderShafts", properties);
            m_UnderBubbles = FindProperty("_UnderBubbles", properties);
            m_UnderDistortion = FindProperty("_UnderDistortion", properties);
            m_ReceiveWeather = FindProperty("_ReceiveWeather", properties);
            m_QueueOffset = FindProperty("_QueueOffset", properties);
        }

        void DrawWaves(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_WindWaves, m_Waves, m_WaveHeight, m_WaveSteepness, m_WaveLength,
                                    m_CrestColor, m_CrestSoftness);
            var expanded = StyleMaterialHeader.Draw(Styles.WavesHeader, "Liquid.Waves", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_WindWaves, Styles.WindWaves);

                if (m_WindWaves.hasMixedValue || m_WindWaves.floatValue < 0.5f)
                    materialEditor.ShaderProperty(m_Waves, Styles.Waves);

                materialEditor.ShaderProperty(m_WaveHeight, Styles.WaveHeight);
                materialEditor.ShaderProperty(m_WaveSteepness, Styles.WaveSteepness);
                materialEditor.ShaderProperty(m_WaveLength, Styles.WaveLength);
                materialEditor.ShaderProperty(m_CrestColor, Styles.CrestColor);
                materialEditor.ShaderProperty(m_CrestSoftness, Styles.CrestSoftness);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BumpScale, m_Tiling, m_Smoothness, m_Refraction, m_RefractionBlur,
                                    m_ReceiveWeather);
            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Liquid.Surface", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.TexturePropertySingleLine(Styles.NormalMap, m_BumpMap, m_BumpScale);
                materialEditor.ShaderProperty(m_Tiling, Styles.Tiling);
                materialEditor.ShaderProperty(m_Smoothness, Styles.Smoothness);
                materialEditor.ShaderProperty(m_Refraction, Styles.Refraction);
                materialEditor.ShaderProperty(m_RefractionBlur, Styles.RefractionBlur);
                materialEditor.ShaderProperty(m_ReceiveWeather, Styles.ReceiveWeather);
            }

            StyleMaterialHeader.End(expanded);
        }

        const int DepthKeys = 6;

        static readonly string[] s_DepthColors =
        {
            "_DepthColor0", "_DepthColor1", "_DepthColor2", "_DepthColor3", "_DepthColor4", "_DepthColor5"
        };

        public static Gradient ReadGradient(Material material)
        {
            var counts = material.GetVector("_DepthTimes1");
            var times = Pair(material.GetVector("_DepthTimes0"), counts);
            var alphaTimes = Pair(material.GetVector("_DepthAlphaTimes0"), material.GetVector("_DepthAlphaTimes1"));
            var covers = Pair(material.GetVector("_DepthAlpha0"), material.GetVector("_DepthAlpha1"));

            var colours = new GradientColorKey[Mathf.Clamp(Mathf.RoundToInt(counts.z), 2, DepthKeys)];
            var alphas = new GradientAlphaKey[Mathf.Clamp(Mathf.RoundToInt(counts.w), 2, DepthKeys)];

            for (var index = 0; index < colours.Length; index++)
            {
                var colour = material.GetColor(s_DepthColors[index]);

                colours[index] = new GradientColorKey(new Color(colour.r, colour.g, colour.b, 1f), times[index]);
            }

            if (counts.w < 2f)
            {
                var fade = Fade();

                alphas = new GradientAlphaKey[fade.Length];

                for (var index = 0; index < fade.Length; index++)
                    alphas[index] = new GradientAlphaKey(fade[index].cover, fade[index].time);
            }
            else
            {
                for (var index = 0; index < alphas.Length; index++)
                    alphas[index] = new GradientAlphaKey(covers[index], alphaTimes[index]);
            }

            var gradient = new Gradient();

            gradient.SetKeys(colours, alphas);

            return gradient;
        }

        public static void WriteGradient(Material material, Gradient gradient)
        {
            var colours = gradient.colorKeys;
            var alphas = gradient.alphaKeys;
            var colourCount = Mathf.Min(colours.Length, DepthKeys);
            var alphaCount = Mathf.Min(alphas.Length, DepthKeys);

            var times = new float[DepthKeys];
            var alphaTimes = new float[DepthKeys];
            var covers = new float[DepthKeys];

            for (var index = 0; index < DepthKeys; index++)
            {
                var colour = colours[Mathf.Min(index, colourCount - 1)];
                var alpha = alphas[Mathf.Min(index, alphaCount - 1)];

                material.SetColor(s_DepthColors[index], new Color(colour.color.r, colour.color.g, colour.color.b, 1f));

                times[index] = colour.time;
                alphaTimes[index] = alpha.time;
                covers[index] = alpha.alpha;
            }

            material.SetVector("_DepthTimes0", new Vector4(times[0], times[1], times[2], times[3]));
            material.SetVector("_DepthTimes1", new Vector4(times[4], times[5], colourCount, alphaCount));
            material.SetVector("_DepthAlpha0", new Vector4(covers[0], covers[1], covers[2], covers[3]));
            material.SetVector("_DepthAlpha1", new Vector4(covers[4], covers[5], 0f, 0f));
            material.SetVector("_DepthAlphaTimes0",
                               new Vector4(alphaTimes[0], alphaTimes[1], alphaTimes[2], alphaTimes[3]));
            material.SetVector("_DepthAlphaTimes1", new Vector4(alphaTimes[4], alphaTimes[5], 0f, 0f));
        }

        static float[] Pair(Vector4 first, Vector4 second)
        {
            return new[] { first.x, first.y, first.z, first.w, second.x, second.y };
        }

        static Gradient DepthGradient((float time, Color colour)[] colours, (float time, float cover)[] covers)
        {
            var colourKeys = new GradientColorKey[colours.Length];
            var alphaKeys = new GradientAlphaKey[covers.Length];

            for (var index = 0; index < colours.Length; index++)
                colourKeys[index] = new GradientColorKey(colours[index].colour, colours[index].time);

            for (var index = 0; index < covers.Length; index++)
                alphaKeys[index] = new GradientAlphaKey(covers[index].cover, covers[index].time);

            var gradient = new Gradient();

            gradient.SetKeys(colourKeys, alphaKeys);

            return gradient;
        }

        static (float time, float cover)[] Fade(float murk = 2f)
        {
            var fade = new (float time, float cover)[] { (0f, 0f), (0.125f, 0f), (0.25f, 0f), (0.5f, 0f), (1f, 1f) };

            for (var index = 1; index < fade.Length - 1; index++)
                fade[index].cover = 1f - Mathf.Pow(1f - fade[index].time, murk);

            return fade;
        }

        void DrawGradient(MaterialEditor materialEditor)
        {
            var first = (Material)materialEditor.target;

            EditorGUI.showMixedValue = m_DepthTimes0.hasMixedValue || m_DepthTimes1.hasMixedValue
                                    || m_DepthAlpha0.hasMixedValue || m_DepthAlphaTimes0.hasMixedValue
                                    || m_DepthColors[0].hasMixedValue;
            EditorGUI.BeginChangeCheck();

            var gradient = EditorGUILayout.GradientField(Styles.DepthGradient, ReadGradient(first), true);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materialEditor.targets, "Change Depth Gradient");

                foreach (var target in materialEditor.targets)
                {
                    WriteGradient((Material)target, gradient);
                    EditorUtility.SetDirty(target);
                }
            }

            EditorGUI.showMixedValue = false;
        }

        void DrawColour(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DepthColors[0], m_DepthColors[1], m_DepthColors[2],
                                    m_DepthColors[3], m_DepthColors[4], m_DepthColors[5], m_DepthTimes0,
                                    m_DepthTimes1, m_DepthAlpha0, m_DepthAlpha1, m_DepthAlphaTimes0,
                                    m_DepthAlphaTimes1, m_Glow, m_Caustics, m_CausticScale);
            var expanded = StyleMaterialHeader.Draw(Styles.Colour, "Liquid.Colour", materialEditor, reset: reset);

            if (expanded)
            {
                DrawGradient(materialEditor);
                materialEditor.ShaderProperty(m_Glow, Styles.Glow);
                materialEditor.ShaderProperty(m_Caustics, Styles.Caustics);
                materialEditor.ShaderProperty(m_CausticScale, Styles.CausticScale);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawFoam(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_FoamColor, m_FoamDistance, m_FoamAmount, m_ShoreFoam,
                                    m_BubbleAmount, m_Pulse, m_PatternScale, m_DiffuseWrap, m_DiffuseSoftness);
            var expanded = StyleMaterialHeader.Draw(Styles.Foam, "Liquid.Foam", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_FoamColor, Styles.FoamColor);
                materialEditor.ShaderProperty(m_FoamDistance, Styles.FoamDistance);
                materialEditor.ShaderProperty(m_FoamAmount, Styles.FoamAmount);
                materialEditor.ShaderProperty(m_ShoreFoam, Styles.ShoreFoam);
                materialEditor.ShaderProperty(m_BubbleAmount, Styles.BubbleAmount);

                using (new EditorGUI.DisabledScope(!m_BubbleAmount.hasMixedValue && m_BubbleAmount.floatValue <= 0f))
                    materialEditor.ShaderProperty(m_Pulse, Styles.Pulse);

                materialEditor.ShaderProperty(m_PatternScale, Styles.PatternScale);
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawCrust(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_CrustAmount, m_CrustColor);
            var expanded = StyleMaterialHeader.Draw(Styles.Crust, "Liquid.Crust", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_CrustAmount, Styles.CrustAmount);
                materialEditor.ShaderProperty(m_CrustColor, Styles.CrustColor);
                materialEditor.TexturePropertySingleLine(Styles.CrustMap, m_BaseMap);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawUnderwater(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_UnderShafts, m_UnderBubbles, m_UnderDistortion);
            var expanded = StyleMaterialHeader.Draw(Styles.Underwater, "Liquid.Underwater", materialEditor,
                                                    reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_UnderShafts, Styles.UnderShafts);
                materialEditor.ShaderProperty(m_UnderBubbles, Styles.UnderBubbles);
                materialEditor.ShaderProperty(m_UnderDistortion, Styles.UnderDistortion);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_QueueOffset);
            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Liquid.Advanced", materialEditor, reset: reset);

            if (expanded)
                materialEditor.ShaderProperty(m_QueueOffset, m_QueueOffset.displayName);

            StyleMaterialHeader.End(expanded);
        }
    }
}
