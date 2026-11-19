using System;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public sealed class HBStyleFogShared
    {
        [SerializeField]
        [Tooltip("How far the fog colour is broken up by brush strokes, using the sky's Sky Paint and Sky Brush " +
                 "amounts (0 to 1). Over surfaces the strokes sit on the ground behind the fog and grow with " +
                 "distance, so they stay put as you walk or turn; where the fog swallows the scene they hand over " +
                 "to the sky's own strokes, so the horizon meets the sky without a seam. 0 leaves the fog smooth.")]
        [Range(0f, 1f)]
        float m_FogPaint = 1f;

        [SerializeField]
        [Tooltip("How far outlines dissolve into the fog behind them (0 to 2). Each stroke takes its fog from its " +
                 "centre, so where a stroke straddles an edge the farther, foggier side is carried across it and " +
                 "the outline breaks up into brush marks instead of a clean cut. It only ever adds fog.")]
        [Range(0f, 2f)]
        float m_Silhouette = 1f;

        [SerializeField]
        [Tooltip("How much a full wind thins the fog in any weather (0 to 1). 0 keeps the fog the same in any wind.")]
        [Range(0f, 1f)]
        float m_WindClear = 0.3f;

        public float fogPaint => m_FogPaint;
        public float silhouette => m_Silhouette;
        public float windClear => m_WindClear;
    }
}
