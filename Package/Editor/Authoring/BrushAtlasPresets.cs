using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class BrushAtlasPresets
    {
        public static readonly string[] Names =
        {
            "Preset…",
            "Developer Best"
        };

        public static void Apply(int index, BrushAtlasSettings settings)
        {
            if (settings == null || index != 1)
                return;

            settings.strokeCount = 800;
            settings.lengthRange = new Vector2(0.1f, 0.25f);
            settings.widthRange = new Vector2(0.006f, 0.0265f);
            settings.angle = 35f;
            settings.angleJitter = 18f;
            settings.taper = 0.565f;
            settings.edgeSoftness = 0.25f;
            settings.curvature = 0.5f;
            settings.opacityVariation = 0.55f;
            settings.toneVariation = 0.75f;
            settings.bristleAmount = 0.435f;
            settings.bristleDensity = 40f;
            settings.bristleBreakup = 0.5f;
            settings.edgeBreakup = 0.35f;
            settings.edgeBreakupScale = 20f;
            settings.pigmentAmount = 1f;
            settings.pigmentScale = 32;
            settings.canvasAmount = 0.25f;
            settings.canvasScale = 225;
            settings.resolution = 512;
            settings.contrast = 1.65f;
            settings.seed = 1;
        }
    }
}
