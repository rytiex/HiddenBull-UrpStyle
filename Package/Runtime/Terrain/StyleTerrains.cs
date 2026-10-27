using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    readonly struct StyleTerrainPreview
    {
        internal readonly Texture control;
        internal readonly HBStyleTerrainLayer[] layers;

        internal StyleTerrainPreview(Texture control, HBStyleTerrainLayer[] layers)
        {
            this.control = control;
            this.layers = layers;
        }
    }

    static class StyleTerrains
    {
        internal const float GrassMostHeight = 3f;

        static readonly HBStyleTerrainQuality s_Default = new HBStyleTerrainQuality();

        static readonly Dictionary<HBStyleTerrainData, StyleTerrainPreview> s_Previews =
            new Dictionary<HBStyleTerrainData, StyleTerrainPreview>();

        static HBStyleTerrainQuality s_Quality;

        internal static HBStyleTerrain highlightTerrain;
        internal static int highlightRow = -1;
        internal static bool layerMap;

        internal static HBStyleTerrainQuality quality
        {
            get => s_Quality ?? s_Default;
            set => s_Quality = value;
        }

        internal static bool TryGetGround(Vector3 position, out HBStyleTerrainData data, out Vector3 origin)
        {
            foreach (var terrain in HBStyleTerrain.active)
            {
                data = terrain.data;
                origin = terrain.transform.position;

                if (data == null || data.terrainData == null)
                    continue;

                var local = position - origin;
                var size = data.size;

                if (local.x >= 0f && local.z >= 0f && local.x <= size.x && local.z <= size.z)
                    return true;
            }

            data = null;
            origin = Vector3.zero;
            return false;
        }

        internal static void SetPreview(HBStyleTerrainData data, Texture control, HBStyleTerrainLayer[] layers)
        {
            s_Previews[data] = new StyleTerrainPreview(control, layers);
        }

        internal static void ClearPreview(HBStyleTerrainData data)
        {
            s_Previews.Remove(data);
        }

        internal static bool TryGetPreview(HBStyleTerrainData data, out StyleTerrainPreview preview)
        {
            return s_Previews.TryGetValue(data, out preview);
        }
    }
}
