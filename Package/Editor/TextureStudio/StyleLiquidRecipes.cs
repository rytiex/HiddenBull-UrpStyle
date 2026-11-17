using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleLiquidRecipes
    {
        internal static void Apply(SerializedObject layer, StyleLiquidRecipe recipe)
        {
            var water = recipe == StyleLiquidRecipe.Water;
            var acid = recipe == StyleLiquidRecipe.Acid;
            var lava = recipe == StyleLiquidRecipe.Lava;

            Set(layer, "m_Tiling", lava ? 0.25f : acid ? 0.145f : 0.3f);
            Set(layer, "m_Waves", 0.2f);
            layer.FindProperty("m_WindWaves").boolValue = water;
            Set(layer, "m_WaveHeight", lava ? 1.65f : 1f);
            Set(layer, "m_WaveSteepness", lava ? 2f : 1f);
            Set(layer, "m_WaveLength", lava ? 0.45f : 1f);
            Set(layer, "m_NormalStrength", lava ? 1f : acid ? 0.6f : 0.5f);
            Set(layer, "m_RippleDrift", lava ? 0f : 1f);
            Set(layer, "m_Smoothness", lava ? 0.25f : acid ? 0.65f : 0.8f);
            Set(layer, "m_SurfaceDistortion", lava ? 0f : 1f);
            Set(layer, "m_RefractionBlur", lava ? 0f : acid ? 0.125f : 0.05f);
            Set(layer, "m_Brush", water ? 0.2f : 0.5f);
            Set(layer, "m_BrushSize", water ? 2f : 1f);
            Set(layer, "m_DiffuseWrap", lava ? 0.246f : 0.125f);
            Set(layer, "m_DiffuseSoftness", lava ? 2f : 0.25f);
            Set(layer, "m_Caustics", lava ? 0f : acid ? 0.085f : 0.125f);
            Set(layer, "m_Glow", lava ? 1f : acid ? 0.2f : 0f);
            Set(layer, "m_CausticScale", 0.25f);
            Set(layer, "m_CrestSoftness", water ? 0.225f : 0f);
            Set(layer, "m_WaterlineDepth", water ? 1f : 3f);
            Set(layer, "m_FoamAmount", water ? 0.5f : 1f);
            Set(layer, "m_ShoreFoam", lava ? 0f : acid ? 2.5f : 0.75f);
            Set(layer, "m_Bubbles", lava ? 0.0225f : acid ? 0.03f : 0f);
            Set(layer, "m_Pulse", lava ? 0.125f : acid ? 1.5f : 0f);
            Set(layer, "m_PatternScale", water ? 0.5f : 3f);
            Set(layer, "m_Lines", lava ? 0f : acid ? 0.85f : 0.6f);
            Set(layer, "m_ToneAmount", lava ? 1f : acid ? 0.6f : 0.3f);
            Set(layer, "m_RockEdge", lava ? 0.6f : 0f);
            Set(layer, "m_RockSmoothness", lava ? 0.15f : 0.3f);
            Set(layer, "m_FlowSpeed", acid ? 0.2f : 0.15f);
            Set(layer, "m_FlowSwirl", lava ? 0.4f : acid ? 0.5f : 0.2f);
            Set(layer, "m_LightShafts", acid ? 0.4f : 1f);
            Set(layer, "m_UnderBubbles", lava ? 0f : acid ? 1f : 0.25f);
            Set(layer, "m_Distortion", lava ? 1f : 0.3f);
            Set(layer, "m_SnellWindow", lava ? 0f : acid ? 0.525f : 1f);
            Set(layer, "m_UnderBlur", water ? 0.25f : 0.5f);
            Set(layer, "m_UnderVisibility", lava ? 0.75f : acid ? 2f : 0.65f);
            Set(layer, "m_UnderLightDepth", lava ? 2f : acid ? 18f : 6f);
            layer.FindProperty("m_RainRipples").boolValue = true;

            layer.FindProperty("m_CrestColor").colorValue = lava ? new Color(0.44339618f, 0.44339618f, 0.44339618f, 1f)
                : acid ? new Color(0.7546641f, 1f, 0.3820755f, 1f)
                : new Color(0.5566038f, 0.5566038f, 0.5566038f, 1f);
            layer.FindProperty("m_FoamColor").colorValue = lava ? new Color(1f, 0.69112843f, 0f, 1f)
                : acid ? Color.white
                : new Color(0.8396226f, 0.8396226f, 0.8396226f, 1f);
            layer.FindProperty("m_RockColor").colorValue = new Color(0.26f, 0.17f, 0.18f);
            layer.FindProperty("m_LineColor").colorValue = acid ? new Color(0.62f, 0.9f, 0.68f)
                : new Color(0.95f, 0.99f, 1f);
            layer.FindProperty("m_DepthGradient").gradientValue = DepthGradient(recipe);
        }

        static void Set(SerializedObject layer, string name, float value)
        {
            layer.FindProperty(name).floatValue = value;
        }

        static Gradient DepthGradient(StyleLiquidRecipe recipe)
        {
            var gradient = new Gradient();

            switch (recipe)
            {
                case StyleLiquidRecipe.Lava:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.75f, 0.55f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.7f, 0.32f, 0.07f), 0.3f),
                        new GradientColorKey(new Color(0.58f, 0.15f, 0.07f), 0.65f),
                        new GradientColorKey(new Color(0.35f, 0.07f, 0.045f), 1f)
                    }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                    break;

                case StyleLiquidRecipe.Acid:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.45f, 0.58f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.33f, 0.52f, 0.36f), 0.5f),
                        new GradientColorKey(new Color(0.24f, 0.42f, 0.32f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.85f, 0.5f),
                        new GradientAlphaKey(0.95f, 0.85f)
                    });
                    break;

                default:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.5f, 0.82f, 0.95f), 0f),
                        new GradientColorKey(new Color(0.36f, 0.66f, 0.88f), 0.3f),
                        new GradientColorKey(new Color(0.24f, 0.5f, 0.78f), 0.67f),
                        new GradientColorKey(new Color(0.16f, 0.38f, 0.66f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.3137255f, 0.25f),
                        new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0.92f, 0.75f),
                        new GradientAlphaKey(1f, 1f)
                    });
                    break;
            }

            return gradient;
        }
    }
}
