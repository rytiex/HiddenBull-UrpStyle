using UnityEngine;

namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// The single authority for how the HiddenBull style is configured in a project.
    ///
    /// A profile holds one <see cref="StyleTierSettings"/> per <see cref="StyleQualityTier"/>.
    /// Renderer features read <see cref="currentSettings"/> rather than storing their own copies,
    /// so switching tier at runtime reconfigures the whole style from one place — and so the
    /// editor-side shader variant stripper can walk the same data to decide what can be dropped.
    /// </summary>
    [CreateAssetMenu(fileName = "StyleProfile", menuName = "HiddenBull/URP Style/Style Profile")]
    public class StyleProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Tier used when nothing selects one explicitly at runtime.")]
        StyleQualityTier m_DefaultTier = StyleQualityTier.High;

        [SerializeField]
        StyleTierSettings[] m_Tiers;

        StyleQualityTier m_ActiveTier;
        bool m_ActiveTierInitialized;

        /// <summary>
        /// The profile the renderer features resolve their settings from.
        /// Assigned by the style renderer feature; games may override it to hot-swap profiles.
        /// </summary>
        public static StyleProfile active { get; set; }

        /// <inheritdoc cref="m_DefaultTier"/>
        public StyleQualityTier defaultTier => m_DefaultTier;

        /// <summary>
        /// Tier currently in use. Defaults to <see cref="defaultTier"/> and can be changed at
        /// runtime, for example from a graphics options menu.
        /// </summary>
        public StyleQualityTier activeTier
        {
            get
            {
                if (!m_ActiveTierInitialized)
                {
                    m_ActiveTier = m_DefaultTier;
                    m_ActiveTierInitialized = true;
                }

                return m_ActiveTier;
            }
            set
            {
                m_ActiveTier = value;
                m_ActiveTierInitialized = true;
            }
        }

        /// <summary>Settings for the <see cref="activeTier"/>.</summary>
        public StyleTierSettings currentSettings => GetSettings(activeTier);

        /// <summary>Settings for a specific tier. Never returns null.</summary>
        public StyleTierSettings GetSettings(StyleQualityTier tier)
        {
            EnsureTiers();

            var index = (int)tier;
            if (index < 0 || index >= m_Tiers.Length)
                return StyleTierSettings.CreateDefault(tier);

            return m_Tiers[index] ?? (m_Tiers[index] = StyleTierSettings.CreateDefault(tier));
        }

        void OnEnable() => EnsureTiers();

        void OnValidate() => EnsureTiers();

        /// <summary>
        /// Keeps the tier list exactly <see cref="StyleQuality.TierCount"/> long, filling any gap
        /// with that tier's defaults. Guards against an asset authored against an older version of
        /// the enum, which would otherwise index out of range.
        /// </summary>
        void EnsureTiers()
        {
            if (m_Tiers != null && m_Tiers.Length == StyleQuality.TierCount)
            {
                for (var i = 0; i < m_Tiers.Length; i++)
                    m_Tiers[i] ??= StyleTierSettings.CreateDefault((StyleQualityTier)i);

                return;
            }

            var resized = new StyleTierSettings[StyleQuality.TierCount];
            for (var i = 0; i < resized.Length; i++)
            {
                var tier = (StyleQualityTier)i;
                resized[i] = m_Tiers != null && i < m_Tiers.Length && m_Tiers[i] != null
                    ? m_Tiers[i]
                    : StyleTierSettings.CreateDefault(tier);
            }

            m_Tiers = resized;
        }
    }
}
