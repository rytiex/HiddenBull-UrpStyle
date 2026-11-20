using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public static class HBStyleMaterial
    {
        static readonly string[] s_Shaders =
        {
            "HB URP Style/Lit", "HB URP Style/Transparent", "HB URP Style/Foliage", "HB URP Style/Foliage Body"
        };

        static readonly int s_BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int s_BumpMapId = Shader.PropertyToID("_BumpMap");
        static readonly int s_MaskMapId = Shader.PropertyToID("_MaskMap");
        static readonly int s_ThicknessMapId = Shader.PropertyToID("_ThicknessMap");
        static readonly int s_SubsurfaceId = Shader.PropertyToID("_SubsurfaceEnabled");
        static readonly int s_SkinModeId = Shader.PropertyToID("_SkinMode");

        public static void SetTexture(Material material, string name, Texture texture)
        {
            SetTexture(material, Shader.PropertyToID(name), texture);
        }

        public static void SetTexture(Material material, int nameID, Texture texture)
        {
            if (material == null)
                return;

            material.SetTexture(nameID, texture);
            Refresh(material);
        }

        public static void Refresh(Material material)
        {
            if (material == null || !Supported(material.shader))
                return;

            var subsurface = On(material, s_SubsurfaceId);
            var cloth = subsurface && On(material, s_SkinModeId);

            Apply(material, "_HB_BASE_MAP", Has(material, s_BaseMapId));
            Apply(material, "_NORMALMAP", Has(material, s_BumpMapId));
            Apply(material, "_HB_MASK_MAP", Has(material, s_MaskMapId));
            Apply(material, "_HB_THICKNESS_MAP", subsurface && !cloth && Has(material, s_ThicknessMapId));
        }

        static bool Supported(Shader shader)
        {
            return shader != null && System.Array.IndexOf(s_Shaders, shader.name) >= 0;
        }

        static bool Has(Material material, int id)
        {
            return material.HasProperty(id) && material.GetTexture(id) != null;
        }

        static bool On(Material material, int id)
        {
            return material.HasProperty(id) && material.GetFloat(id) >= 0.5f;
        }

        static void Apply(Material material, string name, bool on)
        {
            var keyword = material.shader.keywordSpace.FindKeyword(name);

            if (keyword.isValid && material.IsKeywordEnabled(keyword) != on)
                material.SetKeyword(keyword, on);
        }
    }
}
