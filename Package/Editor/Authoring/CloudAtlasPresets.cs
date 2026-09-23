namespace HiddenBull.UrpStyle.Editor
{
    public static class CloudAtlasPresets
    {
        public static readonly string[] Names =
        {
            "Preset…",
            "Cumulus",
            "Towering",
            "Stratus",
            "Wispy"
        };

        public static void Apply(int index, CloudAtlasSettings settings)
        {
            if (settings == null || index <= 0)
                return;

            switch (index)
            {
                case 1:
                    settings.puffScale = 5;
                    settings.puffiness = 0.75f;
                    settings.shapeScale = 3;
                    settings.octaves = 4;
                    settings.erosion = 0.35f;
                    settings.erosionScale = 12;
                    settings.contrast = 1.4f;
                    settings.reliefStrength = 0.6f;
                    settings.reliefSmoothing = 0.35f;
                    break;

                case 2:
                    settings.puffScale = 3;
                    settings.puffiness = 0.95f;
                    settings.shapeScale = 2;
                    settings.octaves = 5;
                    settings.erosion = 0.22f;
                    settings.erosionScale = 16;
                    settings.contrast = 1.9f;
                    settings.reliefStrength = 0.85f;
                    settings.reliefSmoothing = 0.45f;
                    break;

                case 3:
                    settings.puffScale = 8;
                    settings.puffiness = 0.2f;
                    settings.shapeScale = 2;
                    settings.octaves = 3;
                    settings.erosion = 0.18f;
                    settings.erosionScale = 8;
                    settings.contrast = 0.8f;
                    settings.reliefStrength = 0.3f;
                    settings.reliefSmoothing = 0.6f;
                    break;

                case 4:
                    settings.puffScale = 10;
                    settings.puffiness = 0.4f;
                    settings.shapeScale = 4;
                    settings.octaves = 5;
                    settings.erosion = 0.62f;
                    settings.erosionScale = 26;
                    settings.contrast = 1.1f;
                    settings.reliefStrength = 0.35f;
                    settings.reliefSmoothing = 0.2f;
                    break;
            }
        }
    }
}
