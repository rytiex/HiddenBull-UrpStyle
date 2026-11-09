using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleLiquidLook
    {
        internal const int Slots = HBStyleTerrainData.LiquidSlots;

        const int GradientSize = 64;
        const int GradientKeys = 6;
        const float KeyGap = 1e-4f;
        const float Opaque = 0.999f;

        static readonly int s_GradientId = Shader.PropertyToID("_HB_LiquidGradient");
        static readonly int s_OpticsId = Shader.PropertyToID("_HB_LiquidOptics");
        static readonly int s_StyleId = Shader.PropertyToID("_HB_LiquidStyle");
        static readonly int s_FoamId = Shader.PropertyToID("_HB_LiquidFoam");
        static readonly int s_FoamShapeId = Shader.PropertyToID("_HB_LiquidFoamShape");
        static readonly int s_CrestId = Shader.PropertyToID("_HB_LiquidCrest");
        static readonly int s_WavesId = Shader.PropertyToID("_HB_LiquidWaves");
        static readonly int s_StrokesId = Shader.PropertyToID("_HB_LiquidBrush");
        static readonly int s_RippleId = Shader.PropertyToID("_HB_LiquidRipple");
        static readonly int s_UnderId = Shader.PropertyToID("_HB_LiquidUnder");
        static readonly int s_UnderFogId = Shader.PropertyToID("_HB_LiquidUnderFog");
        static readonly int s_MoltenId = Shader.PropertyToID("_HB_LiquidMolten");
        static readonly int s_CrustToneId = Shader.PropertyToID("_HB_LiquidCrustTone");

        static readonly int[] s_NormalIds =
        {
            Shader.PropertyToID("_HB_LiquidNormal0"), Shader.PropertyToID("_HB_LiquidNormal1"),
            Shader.PropertyToID("_HB_LiquidNormal2"), Shader.PropertyToID("_HB_LiquidNormal3")
        };

        static readonly int[] s_PlateIds =
        {
            Shader.PropertyToID("_HB_LiquidPlates0"), Shader.PropertyToID("_HB_LiquidPlates1"),
            Shader.PropertyToID("_HB_LiquidPlates2"), Shader.PropertyToID("_HB_LiquidPlates3")
        };

        static readonly Color s_Missing = new Color(0.2f, 0.2f, 0.2f, 1f);

        readonly Vector4[] m_Optics = new Vector4[Slots];
        readonly Vector4[] m_Style = new Vector4[Slots];
        readonly Vector4[] m_Foam = new Vector4[Slots];
        readonly Vector4[] m_FoamShape = new Vector4[Slots];
        readonly Vector4[] m_Crest = new Vector4[Slots];
        readonly Vector4[] m_WaveShape = new Vector4[Slots];
        readonly Vector4[] m_Strokes = new Vector4[Slots];
        readonly Vector4[] m_Ripple = new Vector4[Slots];
        readonly Vector4[] m_Under = new Vector4[Slots];
        readonly Vector4[] m_UnderFog = new Vector4[Slots];
        readonly Vector4[] m_Molten = new Vector4[Slots];
        readonly Vector4[] m_CrustTone = new Vector4[Slots];
        readonly HBStyleLiquidLayer[] m_Layers = new HBStyleLiquidLayer[Slots];
        readonly int[] m_Versions = new int[Slots];
        readonly Texture2D[] m_Plates = new Texture2D[Slots];
        readonly ushort[] m_Pixels = new ushort[GradientSize * Slots * 4];
        readonly Color[] m_Colours = new Color[GradientKeys];
        readonly float[] m_ColourTimes = new float[GradientKeys];
        readonly float[] m_Alphas = new float[GradientKeys];
        readonly float[] m_AlphaTimes = new float[GradientKeys];

        Texture2D m_Gradient;

        internal void Bind(MaterialPropertyBlock block, HBStyleLiquidLayer[] layers)
        {
            var changed = m_Gradient == null;

            for (var slot = 0; slot < Slots; slot++)
            {
                var liquid = slot < layers.Length ? layers[slot] : null;
                var version = liquid != null ? liquid.version : -1;

                if (liquid == m_Layers[slot] && version == m_Versions[slot])
                    continue;

                m_Layers[slot] = liquid;
                m_Versions[slot] = version;
                changed = true;

                var plates = liquid != null && liquid.crust > 0f ? liquid.normalMap : null;

                if (plates == m_Plates[slot])
                    continue;

                StyleLiquidPlates.Release(m_Plates[slot]);
                StyleLiquidPlates.Acquire(plates);
                m_Plates[slot] = plates;
            }

            if (changed)
                Rebuild();

            block.SetTexture(s_GradientId, m_Gradient);
            block.SetVectorArray(s_OpticsId, m_Optics);
            block.SetVectorArray(s_StyleId, m_Style);
            block.SetVectorArray(s_FoamId, m_Foam);
            block.SetVectorArray(s_FoamShapeId, m_FoamShape);
            block.SetVectorArray(s_CrestId, m_Crest);
            block.SetVectorArray(s_WavesId, m_WaveShape);
            block.SetVectorArray(s_StrokesId, m_Strokes);
            block.SetVectorArray(s_RippleId, m_Ripple);
            block.SetVectorArray(s_UnderId, m_Under);
            block.SetVectorArray(s_UnderFogId, m_UnderFog);
            block.SetVectorArray(s_MoltenId, m_Molten);
            block.SetVectorArray(s_CrustToneId, m_CrustTone);

            for (var slot = 0; slot < Slots; slot++)
            {
                var normal = m_Layers[slot] != null ? m_Layers[slot].normalMap : null;
                block.SetTexture(s_NormalIds[slot], normal != null ? normal : Texture2D.normalTexture);
                block.SetTexture(s_PlateIds[slot], StyleLiquidPlates.Get(m_Plates[slot]));
            }
        }

        internal void Dispose()
        {
            CoreUtils.Destroy(m_Gradient);
            m_Gradient = null;

            for (var slot = 0; slot < Slots; slot++)
            {
                m_Layers[slot] = null;
                m_Versions[slot] = -1;
                StyleLiquidPlates.Release(m_Plates[slot]);
                m_Plates[slot] = null;
            }
        }

        void Rebuild()
        {
            if (m_Gradient == null)
            {
                m_Gradient = new Texture2D(GradientSize, Slots, TextureFormat.RGBAHalf, false, true)
                {
                    name = "HB Style Liquid Gradient",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            for (var slot = 0; slot < Slots; slot++)
            {
                var liquid = m_Layers[slot];

                if (liquid == null)
                {
                    m_Optics[slot] = new Vector4(0.65f, 0f, 0f, 0f);
                    m_Style[slot] = new Vector4(0.125f, 0.25f, 1f, 1f);
                    m_Foam[slot] = Vector4.zero;
                    m_FoamShape[slot] = new Vector4(0.1f, 0f, 0.125f, 0f);
                    m_Crest[slot] = new Vector4(0.7f, 0.7f, 0.7f, 0.45f);
                    m_WaveShape[slot] = new Vector4(0f, 0f, 0f, 1f);
                    m_Strokes[slot] = Vector4.zero;
                    m_Ripple[slot] = new Vector4(0f, 0f, 1f, 1f);
                    m_Under[slot] = Vector4.zero;
                    m_UnderFog[slot] = new Vector4(1f, 18f, 0f, 0f);
                    m_Molten[slot] = new Vector4(0f, 0f, 0f, 1f);
                    m_CrustTone[slot] = Vector4.zero;

                    for (var x = 0; x < GradientSize; x++)
                        Write(slot, x, s_Missing, 1f);

                    continue;
                }

                Keys(liquid.depthGradient);

                m_Optics[slot] = new Vector4(liquid.smoothness, liquid.caustics, liquid.refractionBlur, liquid.glow);
                m_Style[slot] = new Vector4(liquid.diffuseWrap, liquid.diffuseSoftness, liquid.pulse,
                                            liquid.causticScale);

                var foam = liquid.foamColor.linear;

                m_Foam[slot] = new Vector4(foam.r, foam.g, foam.b, liquid.foamAmount);
                m_FoamShape[slot] = new Vector4(liquid.waterlineDepth, liquid.shoreFoam,
                                                liquid.tiling * liquid.patternScale,
                                                liquid.rainRipples ? 1f : 0f);

                var crest = liquid.crestColor.linear;

                m_Crest[slot] = new Vector4(crest.r, crest.g, crest.b, liquid.crestSoftness);
                m_WaveShape[slot] = new Vector4(liquid.windWaves ? -1f : liquid.waves, liquid.waveHeight,
                                                liquid.waveSteepness, liquid.waveLength);
                m_Strokes[slot] = new Vector4(liquid.brush, liquid.brushSize, 0f, 0f);
                m_Ripple[slot] = new Vector4(liquid.tiling, liquid.normalMap != null ? liquid.normalStrength : 0f,
                                             liquid.snellWindow, liquid.rippleDrift);
                m_Under[slot] = new Vector4(liquid.lightShafts, liquid.underBubbles, liquid.distortion,
                                            liquid.underBlur);
                m_UnderFog[slot] = new Vector4(liquid.underVisibility, liquid.underLightDepth, 0f, 0f);
                m_Molten[slot] = new Vector4(liquid.crust, liquid.bubbles, liquid.surfaceDistortion, OpaqueFrom());

                var crustTone = liquid.crustColor.linear;

                m_CrustTone[slot] = new Vector4(crustTone.r, crustTone.g, crustTone.b, 0f);

                for (var x = 0; x < GradientSize; x++)
                {
                    var at = (float)x / (GradientSize - 1);

                    Write(slot, x, Colour(at), Alpha(at));
                }
            }

            m_Gradient.SetPixelData(m_Pixels, 0);
            m_Gradient.Apply(false, false);
        }

        void Keys(Gradient gradient)
        {
            var colours = gradient.colorKeys;
            var alphas = gradient.alphaKeys;
            var colourCount = Mathf.Clamp(colours.Length, 1, GradientKeys);
            var alphaCount = Mathf.Clamp(alphas.Length, 1, GradientKeys);

            for (var i = 0; i < GradientKeys; i++)
            {
                var colour = colours.Length > 0 ? colours[Mathf.Min(i, colourCount - 1)]
                                                : new GradientColorKey(s_Missing, 0f);
                var alpha = alphas.Length > 0 ? alphas[Mathf.Min(i, alphaCount - 1)] : new GradientAlphaKey(1f, 0f);
                var linear = colour.color.linear;

                m_Colours[i] = new Color(linear.r, linear.g, linear.b, 1f);
                m_ColourTimes[i] = colour.time;
                m_Alphas[i] = alpha.alpha;
                m_AlphaTimes[i] = alpha.time;
            }
        }

        float OpaqueFrom()
        {
            for (var x = 0; x < GradientSize; x++)
            {
                var at = (float)x / (GradientSize - 1);

                if (Alpha(at) >= Opaque)
                    return at;
            }

            return 1f;
        }

        Color Colour(float at)
        {
            var colour = m_Colours[0];

            for (var stop = 1; stop < GradientKeys; stop++)
            {
                var gap = Mathf.Max(m_ColourTimes[stop] - m_ColourTimes[stop - 1], KeyGap);

                colour = Color.LerpUnclamped(colour, m_Colours[stop],
                                             Mathf.Clamp01((at - m_ColourTimes[stop - 1]) / gap));
            }

            return colour;
        }

        float Alpha(float at)
        {
            var alpha = m_Alphas[0];

            for (var stop = 1; stop < GradientKeys; stop++)
            {
                var gap = Mathf.Max(m_AlphaTimes[stop] - m_AlphaTimes[stop - 1], KeyGap);

                alpha = Mathf.LerpUnclamped(alpha, m_Alphas[stop], Mathf.Clamp01((at - m_AlphaTimes[stop - 1]) / gap));
            }

            return Mathf.Clamp01(alpha);
        }

        void Write(int slot, int x, Color colour, float alpha)
        {
            var index = (slot * GradientSize + x) * 4;

            m_Pixels[index] = Mathf.FloatToHalf(colour.r);
            m_Pixels[index + 1] = Mathf.FloatToHalf(colour.g);
            m_Pixels[index + 2] = Mathf.FloatToHalf(colour.b);
            m_Pixels[index + 3] = Mathf.FloatToHalf(alpha);
        }
    }
}
