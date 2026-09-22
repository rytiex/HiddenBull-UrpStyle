using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// The complete set of style switches for a single <see cref="StyleQualityTier"/>.
    ///
    /// Each development phase appends its own block here. Two rules keep this file honest:
    ///   1. Every feature must be switchable off — a tier that disables everything is a valid tier.
    ///   2. Nothing lands here speculatively. A field is only added once the feature it drives
    ///      has an agreed design in Docs/Architecture.md, even if the implementation follows later.
    /// </summary>
    [Serializable]
    public class StyleTierSettings
    {
        // --- Phase 2: Stylized shadows -------------------------------------------------
        // Contact-hardening shadow mask resolved in screen space, stylized once in a single
        // pass rather than per-material. See Docs/Architecture.md.

        [SerializeField]
        [Tooltip("Resolve the main directional light's shadows into a stylized screen-space mask. " +
                 "When off, materials fall back to stock URP shadow sampling.")]
        bool m_StylizedShadowMask = true;

        [SerializeField]
        [Tooltip("Resolution the shadow mask is resolved and stylized at.")]
        StyleResolutionScale m_ShadowMaskResolution = StyleResolutionScale.Full;

        // --- Phase 3: Directional occlusion --------------------------------------------
        // Ground-truth ambient occlusion with bent normals, so occlusion picks up the colour
        // of the sky gradient and the dominant light instead of multiplying everything by grey.

        [SerializeField]
        [Tooltip("Compute ambient occlusion together with bent normals, letting ambient light " +
                 "be sampled along the unoccluded direction.")]
        bool m_DirectionalOcclusion = true;

        [SerializeField]
        [Tooltip("Resolution the occlusion and bent normal buffer is computed at.")]
        StyleResolutionScale m_OcclusionResolution = StyleResolutionScale.Half;

        /// <inheritdoc cref="m_StylizedShadowMask"/>
        public bool stylizedShadowMask
        {
            get => m_StylizedShadowMask;
            set => m_StylizedShadowMask = value;
        }

        /// <inheritdoc cref="m_ShadowMaskResolution"/>
        public StyleResolutionScale shadowMaskResolution
        {
            get => m_ShadowMaskResolution;
            set => m_ShadowMaskResolution = value;
        }

        /// <inheritdoc cref="m_DirectionalOcclusion"/>
        public bool directionalOcclusion
        {
            get => m_DirectionalOcclusion;
            set => m_DirectionalOcclusion = value;
        }

        /// <inheritdoc cref="m_OcclusionResolution"/>
        public StyleResolutionScale occlusionResolution
        {
            get => m_OcclusionResolution;
            set => m_OcclusionResolution = value;
        }

        /// <summary>
        /// Builds the defaults for a tier. Used when a profile is created or when its tier
        /// list is resized, so a fresh profile is immediately usable.
        /// </summary>
        public static StyleTierSettings CreateDefault(StyleQualityTier tier)
        {
            switch (tier)
            {
                case StyleQualityTier.Low:
                    return new StyleTierSettings
                    {
                        m_StylizedShadowMask = false,
                        m_ShadowMaskResolution = StyleResolutionScale.Quarter,
                        m_DirectionalOcclusion = false,
                        m_OcclusionResolution = StyleResolutionScale.Quarter
                    };

                case StyleQualityTier.Medium:
                    return new StyleTierSettings
                    {
                        m_StylizedShadowMask = true,
                        m_ShadowMaskResolution = StyleResolutionScale.Half,
                        m_DirectionalOcclusion = true,
                        m_OcclusionResolution = StyleResolutionScale.Quarter
                    };

                case StyleQualityTier.Ultra:
                    return new StyleTierSettings
                    {
                        m_StylizedShadowMask = true,
                        m_ShadowMaskResolution = StyleResolutionScale.Full,
                        m_DirectionalOcclusion = true,
                        m_OcclusionResolution = StyleResolutionScale.Full
                    };

                default: // High
                    return new StyleTierSettings();
            }
        }
    }
}
