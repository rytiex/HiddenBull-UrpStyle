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

            Set(layer, "m_Tiling", lava ? 0.25f : acid ? 0.145f : 1f);
            Set(layer, "m_Waves", 0.2f);
            layer.FindProperty("m_WindWaves").boolValue = water;
            Set(layer, "m_WaveHeight", lava ? 1.65f : 1f);
            Set(layer, "m_WaveSteepness", lava ? 2f : 1f);
            Set(layer, "m_WaveLength", lava ? 0.45f : 1f);
            Set(layer, "m_NormalStrength", lava ? 1.5f : acid ? 1f : 0.125f);
            Set(layer, "m_RippleDrift", lava ? 0f : 1f);
            Set(layer, "m_Smoothness", lava ? 0.25f : acid ? 0.65f : 0.8f);
            Set(layer, "m_SurfaceDistortion", lava ? 0f : 1f);
            Set(layer, "m_RefractionBlur", lava ? 0f : acid ? 0.125f : 0.05f);
            Set(layer, "m_Brush", water ? 0.2f : 0.5f);
            Set(layer, "m_BrushSize", water ? 2f : 1f);
            Set(layer, "m_DiffuseWrap", lava ? 0.246f : 0.125f);
            Set(layer, "m_DiffuseSoftness", lava ? 2f : 0.25f);
            Set(layer, "m_Caustics", lava ? 0f : acid ? 0.085f : 0.125f);
            Set(layer, "m_Glow", water ? 0f : 1f);
            Set(layer, "m_CausticScale", 0.25f);
            Set(layer, "m_CrestSoftness", water ? 0.225f : 0f);
            Set(layer, "m_WaterlineDepth", water ? 1f : 3f);
            Set(layer, "m_FoamAmount", water ? 0.5f : 1f);
            Set(layer, "m_ShoreFoam", lava ? 0f : acid ? 2.5f : 0.75f);
            Set(layer, "m_Bubbles", lava ? 0.0225f : acid ? 0.125f : 0f);
            Set(layer, "m_Pulse", lava ? 0.125f : acid ? 1.5f : 0f);
            Set(layer, "m_PatternScale", water ? 0.5f : 3f);
            Set(layer, "m_Crust", lava ? 0.7f : 0f);
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
            layer.FindProperty("m_CrustColor").colorValue = new Color(0.26f, 0.17f, 0.18f);
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
                        new GradientColorKey(new Color(0.69479233f, 0.7176471f, 0f), 0f),
                        new GradientColorKey(new Color(0.12174808f, 0.34117648f, 0f), 0.682353f),
                        new GradientColorKey(new Color(0.2601031f, 0.4245283f, 0f), 1f)
                    }, new[]
                    {
                        new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0.75f, 0.50588f),
                        new GradientAlphaKey(0.7764706f, 0.85f)
                    });
                    break;

                default:
                    gradient.SetKeys(new[]
                    {
                        new GradientColorKey(new Color(0.21586053f, 0.26225072f, 0.33245158f), 0f),
                        new GradientColorKey(new Color(0.038204376f, 0.06662595f, 0.09084173f), 0.30000764f),
                        new GradientColorKey(new Color(0.013702081f, 0.045186214f, 0.05448028f), 0.66764f),
                        new GradientColorKey(new Color(0.010329823f, 0.04091521f, 0.048171833f), 1f)
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
