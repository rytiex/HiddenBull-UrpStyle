namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// Quality levels the style scales across.
    /// Every screen-space feature in the package must define a behaviour for each tier,
    /// including a way to be disabled entirely — the style has to survive on a budget.
    /// </summary>
    public enum StyleQualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// Render resolution a screen-space pass runs at, relative to the camera target.
    /// </summary>
    public enum StyleResolutionScale
    {
        Full = 0,
        Half = 1,
        Quarter = 2
    }

    /// <summary>
    /// Helpers for <see cref="StyleQualityTier"/> and <see cref="StyleResolutionScale"/>.
    /// </summary>
    public static class StyleQuality
    {
        /// <summary>Number of tiers defined by <see cref="StyleQualityTier"/>.</summary>
        public const int TierCount = 4;

        /// <summary>Divisor applied to a render target's dimensions for the given scale.</summary>
        public static int ToDivisor(this StyleResolutionScale scale)
        {
            switch (scale)
            {
                case StyleResolutionScale.Half: return 2;
                case StyleResolutionScale.Quarter: return 4;
                default: return 1;
            }
        }
    }
}
