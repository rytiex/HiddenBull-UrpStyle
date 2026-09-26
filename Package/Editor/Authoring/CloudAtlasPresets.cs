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
            settings.erosion = 0.325f;
            settings.erosionScale = 8;
            settings.contrast = 4f;
            settings.warp = 0.325f;
            settings.warpScale = 1;
            settings.brushAmount = 0.225f;
            settings.paint = 2.25f;
            settings.brushScale = 0.65f;
            settings.reliefStrength = 1f;
            settings.reliefSmoothing = 1f;
            settings.resolution = 512;
            settings.seed = 1;
        }
    }
}
