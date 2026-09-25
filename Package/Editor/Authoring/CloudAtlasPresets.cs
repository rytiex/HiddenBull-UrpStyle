namespace HiddenBull.UrpStyle.Editor
{
    public static class CloudAtlasPresets
    {
        public static readonly string[] Names =
        {
            "Preset…",
            "Developer Best"
        };

        public static void Apply(int index, CloudAtlasSettings settings)
        {
            if (settings == null || index != 1)
                return;

            settings.puffScale = 2;
            settings.puffiness = 1f;
            settings.shapeScale = 1;
            settings.octaves = 2;
            settings.erosion = 1f;
            settings.erosionScale = 12;
            settings.contrast = 1.86f;
            settings.warp = 0.5f;
            settings.warpScale = 2;
            settings.brushAmount = 0.55f;
            settings.brushScale = 0.95f;
            settings.reliefStrength = 0.5f;
            settings.reliefSmoothing = 0.645f;
        }
    }
}
