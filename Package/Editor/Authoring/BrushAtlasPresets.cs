using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class BrushAtlasPresets
    {
        public static readonly string[] Names =
        {
            "Preset…",
            "Developer Best",
            "Dry Brush",
            "Loaded Brush",
            "Fine Hatch",
            "Painterly Patches"
        };

        public static void Apply(int index, BrushAtlasSettings settings)
        {
            if (settings == null || index <= 0)
                return;

            switch (index)
            {
                case 1:
                    settings.strokeCount = 800;
                    settings.lengthRange = new Vector2(0.1f, 0.26f);
                    settings.widthRange = new Vector2(0.006f, 0.016f);
                    settings.angle = 35f;
                    settings.angleJitter = 18f;
                    settings.taper = 0.584f;
                    settings.edgeSoftness = 0.25f;
                    settings.opacityVariation = 0.55f;
                    settings.bristleAmount = 0.75f;
                    settings.bristleDensity = 16f;
                    settings.edgeBreakup = 0.5f;
                    settings.edgeBreakupScale = 9f;
                    settings.pigmentAmount = 0.35f;
                    settings.pigmentScale = 5;
                    settings.canvasAmount = 0.35f;
                    settings.canvasScale = 80;
                    settings.contrast = 1.63f;
                    settings.warpSpread = 0.01f;
                    settings.seed = -1351948961;
                    break;

                case 2:
                    settings.strokeCount = 220;
                    settings.lengthRange = new Vector2(0.1f, 0.26f);
                    settings.widthRange = new Vector2(0.006f, 0.016f);
                    settings.angleJitter = 18f;
                    settings.taper = 0.7f;
                    settings.edgeSoftness = 0.25f;
                    settings.opacityVariation = 0.55f;
                    settings.bristleAmount = 0.75f;
                    settings.bristleDensity = 16f;
                    settings.edgeBreakup = 0.5f;
                    settings.edgeBreakupScale = 9f;
                    settings.pigmentAmount = 0.35f;
                    settings.pigmentScale = 5;
                    settings.canvasAmount = 0.35f;
                    settings.canvasScale = 80;
                    settings.contrast = 1.1f;
                    settings.warpSpread = 0.010f;
                    break;

                case 3:
                    settings.strokeCount = 150;
                    settings.lengthRange = new Vector2(0.12f, 0.3f);
                    settings.widthRange = new Vector2(0.016f, 0.038f);
                    settings.angleJitter = 32f;
                    settings.taper = 0.45f;
                    settings.edgeSoftness = 0.85f;
                    settings.opacityVariation = 0.3f;
                    settings.bristleAmount = 0.2f;
                    settings.bristleDensity = 7f;
                    settings.edgeBreakup = 0.15f;
                    settings.edgeBreakupScale = 3f;
                    settings.pigmentAmount = 0.25f;
                    settings.pigmentScale = 3;
                    settings.canvasAmount = 0.12f;
                    settings.canvasScale = 48;
                    settings.contrast = 0.9f;
                    settings.warpSpread = 0.018f;
                    break;

                case 4:
                    settings.strokeCount = 420;
                    settings.lengthRange = new Vector2(0.06f, 0.14f);
                    settings.widthRange = new Vector2(0.003f, 0.008f);
                    settings.angleJitter = 6f;
                    settings.taper = 0.8f;
                    settings.edgeSoftness = 0.4f;
                    settings.opacityVariation = 0.5f;
                    settings.bristleAmount = 0.35f;
                    settings.bristleDensity = 22f;
                    settings.edgeBreakup = 0.25f;
                    settings.edgeBreakupScale = 12f;
                    settings.pigmentAmount = 0.2f;
                    settings.pigmentScale = 6;
                    settings.canvasAmount = 0.3f;
                    settings.canvasScale = 96;
                    settings.contrast = 1.2f;
                    settings.warpSpread = 0.006f;
                    break;

                case 5:
                    settings.strokeCount = 90;
                    settings.lengthRange = new Vector2(0.14f, 0.34f);
                    settings.widthRange = new Vector2(0.03f, 0.07f);
                    settings.angleJitter = 45f;
                    settings.taper = 0.35f;
                    settings.edgeSoftness = 0.55f;
                    settings.opacityVariation = 0.4f;
                    settings.bristleAmount = 0.3f;
                    settings.bristleDensity = 6f;
                    settings.edgeBreakup = 0.45f;
                    settings.edgeBreakupScale = 4f;
                    settings.pigmentAmount = 0.5f;
                    settings.pigmentScale = 3;
                    settings.canvasAmount = 0.2f;
                    settings.canvasScale = 56;
                    settings.contrast = 0.85f;
                    settings.warpSpread = 0.022f;
                    break;
            }
        }
    }
}
