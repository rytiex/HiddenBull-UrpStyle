using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    static class StyleLiquidLayers
    {
        public const int Most = 4;

        const int Slots = Most - 1;

        static readonly int s_CountId = Shader.PropertyToID("_WaterLayers");
        static readonly int s_WaveId = Shader.PropertyToID("_OtherWave");
        static readonly int s_ShapeId = Shader.PropertyToID("_OtherShape");
        static readonly int s_FoamId = Shader.PropertyToID("_OtherFoam");
        static readonly int s_SurfaceId = Shader.PropertyToID("_OtherSurface");
        static readonly int s_FoamColorId = Shader.PropertyToID("_OtherFoamColor");
        static readonly int s_CrestColorId = Shader.PropertyToID("_OtherCrestColor");
        static readonly int s_CrustColorId = Shader.PropertyToID("_OtherCrustColor");
        static readonly int s_OpticsId = Shader.PropertyToID("_OtherOptics");
        static readonly int s_UnderId = Shader.PropertyToID("_OtherUnder");

        static readonly int[] s_BumpMapIds =
        {
            Shader.PropertyToID("_OtherBumpMap0"), Shader.PropertyToID("_OtherBumpMap1"),
            Shader.PropertyToID("_OtherBumpMap2")
        };

        static readonly int[] s_BaseMapIds =
        {
            Shader.PropertyToID("_OtherBaseMap0"), Shader.PropertyToID("_OtherBaseMap1"),
            Shader.PropertyToID("_OtherBaseMap2")
        };

        static readonly string[] s_Depth =
        {
            "DepthColor0", "DepthColor1", "DepthColor2", "DepthColor3", "DepthColor4", "DepthColor5",
            "DepthTimes0", "DepthTimes1", "DepthAlpha0", "DepthAlpha1", "DepthAlphaTimes0", "DepthAlphaTimes1"
        };

        static readonly int[] s_DepthIds = Ids("_");
        static readonly int[] s_OtherDepthIds = Ids("_Other");

        static readonly int s_WavesId = Shader.PropertyToID("_Waves");
        static readonly int s_WindWavesId = Shader.PropertyToID("_WindWaves");
        static readonly int s_WaveHeightId = Shader.PropertyToID("_WaveHeight");
        static readonly int s_WaveSteepnessId = Shader.PropertyToID("_WaveSteepness");
        static readonly int s_WaveLengthId = Shader.PropertyToID("_WaveLength");
        static readonly int s_BubbleAmountId = Shader.PropertyToID("_BubbleAmount");
        static readonly int s_GlowId = Shader.PropertyToID("_Glow");
        static readonly int s_FoamAmountId = Shader.PropertyToID("_FoamAmount");
        static readonly int s_ShoreFoamId = Shader.PropertyToID("_ShoreFoam");
        static readonly int s_FoamDistanceId = Shader.PropertyToID("_FoamDistance");
        static readonly int s_CrustAmountId = Shader.PropertyToID("_CrustAmount");
        static readonly int s_TilingId = Shader.PropertyToID("_Tiling");
        static readonly int s_BumpScaleId = Shader.PropertyToID("_BumpScale");
        static readonly int s_PatternScaleId = Shader.PropertyToID("_PatternScale");
        static readonly int s_SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int s_OwnFoamColorId = Shader.PropertyToID("_FoamColor");
        static readonly int s_OwnCrestColorId = Shader.PropertyToID("_CrestColor");
        static readonly int s_OwnCrustColorId = Shader.PropertyToID("_CrustColor");
        static readonly int s_RefractionId = Shader.PropertyToID("_Refraction");
        static readonly int s_RefractionBlurId = Shader.PropertyToID("_RefractionBlur");
        static readonly int s_CausticsId = Shader.PropertyToID("_Caustics");
        static readonly int s_CrestSoftnessId = Shader.PropertyToID("_CrestSoftness");
        static readonly int s_UnderShaftsId = Shader.PropertyToID("_UnderShafts");
        static readonly int s_UnderBubblesId = Shader.PropertyToID("_UnderBubbles");
        static readonly int s_UnderDistortionId = Shader.PropertyToID("_UnderDistortion");
        static readonly int s_BumpMapId = Shader.PropertyToID("_BumpMap");
        static readonly int s_BaseMapId = Shader.PropertyToID("_BaseMap");

        static readonly Vector4[] s_Wave = new Vector4[Slots];
        static readonly Vector4[] s_Shape = new Vector4[Slots];
        static readonly Vector4[] s_Foam = new Vector4[Slots];
        static readonly Vector4[] s_Surface = new Vector4[Slots];
        static readonly Vector4[] s_FoamColor = new Vector4[Slots];
        static readonly Vector4[] s_CrestColor = new Vector4[Slots];
        static readonly Vector4[] s_CrustColor = new Vector4[Slots];
        static readonly Vector4[] s_Optics = new Vector4[Slots];
        static readonly Vector4[] s_Under = new Vector4[Slots];
        static readonly Vector4[][] s_DepthValues = Rows(12);
        static readonly List<Vector4> s_Kept = new List<Vector4>(Slots);

        public static bool Lost(List<Material> layers, Material target)
        {
            if (!target.HasFloat(s_CountId) || target.GetFloat(s_CountId) != Mathf.Min(layers.Count, Most) - 1)
                return true;

            target.GetVectorArray(s_WaveId, s_Kept);

            return s_Kept.Count != Slots;
        }

        public static void Write(List<Material> layers, MaterialPropertyBlock block, Material target)
        {
            var count = Mathf.Min(layers.Count, Most);
            var own = count > 0 ? layers[0] : null;

            for (var slot = 0; slot < Slots; slot++)
            {
                var source = slot + 1 < count && layers[slot + 1] != null ? layers[slot + 1] : own;

                if (source == null)
                    continue;

                s_Wave[slot] = new Vector4(Read(source, s_WavesId), Read(source, s_WindWavesId),
                                           Read(source, s_WaveHeightId), Read(source, s_WaveSteepnessId));
                s_Shape[slot] = new Vector4(Read(source, s_WaveLengthId), 0f, Read(source, s_BubbleAmountId),
                                            Read(source, s_GlowId));
                s_Foam[slot] = new Vector4(Read(source, s_FoamAmountId), Read(source, s_ShoreFoamId),
                                           Read(source, s_FoamDistanceId), Read(source, s_CrustAmountId));
                s_Surface[slot] = new Vector4(Read(source, s_TilingId), Read(source, s_BumpScaleId),
                                              Read(source, s_PatternScaleId), Read(source, s_SmoothnessId));
                s_FoamColor[slot] = Shade(source, s_OwnFoamColorId);
                s_CrestColor[slot] = Shade(source, s_OwnCrestColorId);
                s_CrustColor[slot] = Shade(source, s_OwnCrustColorId);
                s_Optics[slot] = new Vector4(Read(source, s_RefractionId), Read(source, s_RefractionBlurId),
                                             Read(source, s_CausticsId), Read(source, s_CrestSoftnessId));
                s_Under[slot] = new Vector4(Read(source, s_UnderShaftsId), Read(source, s_UnderBubblesId),
                                            Read(source, s_UnderDistortionId), 0f);

                for (var index = 0; index < s_DepthIds.Length; index++)
                {
                    s_DepthValues[index][slot] = source.HasProperty(s_DepthIds[index])
                        ? source.GetVector(s_DepthIds[index])
                        : Vector4.zero;
                }

                Paint(block, target, s_BumpMapIds[slot], source, s_BumpMapId, Texture2D.normalTexture);
                Paint(block, target, s_BaseMapIds[slot], source, s_BaseMapId, Texture2D.whiteTexture);
            }

            if (own == null)
                return;

            if (block != null)
                block.SetFloat(s_CountId, count - 1);
            else
                target.SetFloat(s_CountId, count - 1);

            Rows(block, target, s_WaveId, s_Wave);
            Rows(block, target, s_ShapeId, s_Shape);
            Rows(block, target, s_FoamId, s_Foam);
            Rows(block, target, s_SurfaceId, s_Surface);
            Rows(block, target, s_FoamColorId, s_FoamColor);
            Rows(block, target, s_CrestColorId, s_CrestColor);
            Rows(block, target, s_CrustColorId, s_CrustColor);
            Rows(block, target, s_OpticsId, s_Optics);
            Rows(block, target, s_UnderId, s_Under);

            for (var index = 0; index < s_OtherDepthIds.Length; index++)
                Rows(block, target, s_OtherDepthIds[index], s_DepthValues[index]);
        }

        static float Read(Material material, int id)
        {
            return material.HasFloat(id) ? material.GetFloat(id) : 0f;
        }

        static Vector4 Shade(Material material, int id)
        {
            if (!material.HasColor(id))
                return Vector4.zero;

            var colour = material.GetColor(id);

            return QualitySettings.activeColorSpace == ColorSpace.Linear ? colour.linear : colour;
        }

        static void Rows(MaterialPropertyBlock block, Material target, int id, Vector4[] values)
        {
            if (block != null)
                block.SetVectorArray(id, values);
            else
                target.SetVectorArray(id, values);
        }

        static void Paint(MaterialPropertyBlock block, Material target, int id, Material source, int from,
                          Texture fallback)
        {
            var texture = source.HasTexture(from) ? source.GetTexture(from) : null;

            if (texture == null)
                texture = fallback;

            if (block != null)
                block.SetTexture(id, texture);
            else
                target.SetTexture(id, texture);
        }

        static int[] Ids(string prefix)
        {
            var ids = new int[s_Depth.Length];

            for (var index = 0; index < ids.Length; index++)
                ids[index] = Shader.PropertyToID(prefix + s_Depth[index]);

            return ids;
        }

        static Vector4[][] Rows(int count)
        {
            var rows = new Vector4[count][];

            for (var index = 0; index < count; index++)
                rows[index] = new Vector4[Slots];

            return rows;
        }
    }
}
