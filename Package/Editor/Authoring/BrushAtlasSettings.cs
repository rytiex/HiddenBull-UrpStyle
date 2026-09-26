using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    public class BrushAtlasSettings
    {
        [Tooltip("Number of strokes laid down. Aim for a coverage around 50%: that is where gaps " +
                 "and strokes are symmetric and both read at full strength.")]
        public int strokeCount = 800;

        [Tooltip("Shortest and longest stroke, as a fraction of the texture.")]
        public Vector2 lengthRange = new Vector2(0.1f, 0.25f);

        [Tooltip("Thinnest and thickest stroke, as a fraction of the texture.")]
        public Vector2 widthRange = new Vector2(0.006f, 0.0265f);

        [Tooltip("Dominant stroke direction. Painters rarely stroke in every direction equally, " +
                 "and a dominant angle is a large part of why a surface reads as painted.")]
        public float angle = 35f;

        [Tooltip("How far individual strokes deviate from the dominant angle. Zero gives a rigid " +
                 "hatch; 180 gives no direction at all.")]
        public float angleJitter = 18f;

        [Range(0f, 1f)]
        [Tooltip("How far each stroke thins toward its ends. A brush loses pressure entering and " +
                 "leaving the surface; without this, strokes end in rounded caps and read as " +
                 "capsules rather than brushwork.")]
        public float taper = 0.565f;

        [Range(0f, 1f)]
        [Tooltip("Softness of a stroke's edge. Low values give hard-edged marks, high values give " +
                 "a loaded, blended brush.")]
        public float edgeSoftness = 0.25f;

        [Range(0f, 1f)]
        [Tooltip("How much stroke opacity varies. Uniform strokes read as a pattern rather than " +
                 "as brushwork.")]
        public float opacityVariation = 0.55f;

        [Range(0f, 1f)]
        [Tooltip("How far apart the strokes are in tone. Each stroke is laid down over what is " +
                 "already there rather than added to it, so a dark stroke can cover a light one " +
                 "and the surface builds up in layers.\n\n" +
                 "At 0 every stroke is the same value and only the gaps between them read. This is " +
                 "the setting that decides whether the result looks like scratched metal or like " +
                 "paint.")]
        public float toneVariation = 0.75f;

        [Range(0f, 1f)]
        [Tooltip("How far each stroke bends away from a straight line. A brush follows the wrist, " +
                 "and a field of perfectly straight marks reads as hatching rather than painting.\n\n" +
                 "Costs a little: a curved stroke is measured against several short segments " +
                 "instead of one.")]
        public float curvature = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How much the bristle lines break up along the stroke rather than running its " +
                 "whole length. Bristles lift and catch as the brush travels; without this they " +
                 "read as ruled lines.")]
        public float bristleBreakup = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Fine lines running along each stroke, left by the bristles. This is what separates " +
                 "a brush stroke from a smooth capsule — usually the single highest-impact setting " +
                 "on this panel.")]
        public float bristleAmount = 0.435f;

        [Range(2f, 40f)]
        [Tooltip("How many bristle lines fit across a stroke's width.")]
        public float bristleDensity = 40f;

        [Range(0f, 1f)]
        [Tooltip("Ragged the stroke's boundary, instead of a clean curve. Also feeds the warp " +
                 "channel, so it shows up along stylized shadow edges too.")]
        public float edgeBreakup = 0.35f;

        [Range(1f, 20f)]
        [Tooltip("How rapidly the edge wanders along the stroke. Low values give long swells, " +
                 "high values a chewed edge.")]
        public float edgeBreakupScale = 20f;

        [Range(0f, 1f)]
        [Tooltip("Low-frequency variation in how thickly paint sits — where it pooled and where it " +
                 "ran thin.")]
        public float pigmentAmount = 1f;

        [Range(1, 32)]
        [Tooltip("Pigment variation cells across the tile. Kept whole so the pattern still tiles.")]
        public int pigmentScale = 32;

        [Range(0f, 1f)]
        [Tooltip("Fine tooth of the canvas showing through the paint. Rarely noticed on its own; " +
                 "its absence is what makes a surface look digital.")]
        public float canvasAmount = 0.25f;

        [Range(4, 256)]
        [Tooltip("Canvas grain cells across the tile. Kept whole so the grain still tiles.")]
        public int canvasScale = 225;

        [Tooltip("Output texture size. 512 is usually enough — the atlas tiles, so detail comes " +
                 "from repetition rather than resolution.")]
        public int resolution = 512;

        [Range(0.25f, 4f)]
        [Tooltip("Contrast applied to the finished stroke field.")]
        public float contrast = 1.65f;

        [Tooltip("Random seed. The same seed always produces the same atlas.")]
        public int seed = 1;
    }
}
