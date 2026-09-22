using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
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
    /// Performance settings for the style, serialized on <see cref="HiddenBullStyleFeature"/>.
    ///
    /// These live on the renderer feature rather than in an asset of their own because that is where
    /// Unity already expects per-quality-level rendering settings to be: a Quality Level points at a
    /// URP Asset, which points at a Renderer, which carries its features' settings. Scaling quality
    /// therefore means authoring one renderer per quality level — the same way URP handles its own
    /// shadow resolution and cascade counts — instead of this package inventing a parallel tier
    /// system beside the one Unity ships (see Architecture D7).
    ///
    /// Artistic settings do not belong here. Those go on a Volume, so they blend as the camera moves
    /// between areas; <see cref="StyleAmbient"/> is the example.
    ///
    /// Each development phase appends its own block. A field is only added once the feature it
    /// drives has an agreed design in Docs/Architecture.md, even if the implementation follows later,
    /// and every feature must be switchable off — settings with everything disabled are valid.
    /// </summary>
    [Serializable]
    public class StyleSettings
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
    }

    /// <summary>
    /// Helpers for <see cref="StyleResolutionScale"/>.
    /// </summary>
    public static class StyleResolution
    {
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
