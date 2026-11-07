using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainSink
    {
        internal const float Depth = 10f;

        const float Headroom = 1f;

        internal static void Sink(HBStyleTerrain terrain, float depth)
        {
            var data = terrain.data;
            var source = data.terrainData;
            var size = source.size;
            var resolution = source.heightmapResolution;
            var heights = source.GetHeights(0, 0, resolution, resolution);
            var top = 0f;

            foreach (var height in heights)
                top = Mathf.Max(top, height);

            var range = Mathf.Max(size.y, top * size.y + depth + Headroom);
            var grows = range > size.y;
            var water = grows && StyleTerrainWater.Ready(data);
            var transform = terrain.transform;
            var recorded = new List<Object> { transform };

            foreach (Transform child in transform)
                recorded.Add(child);

            if (water)
                recorded.AddRange(StyleTerrainWater.Recorded(data));

            Undo.RegisterCompleteObjectUndo(recorded.ToArray(), "Sink HB Style Terrain");

            if (grows)
                source.size = new Vector3(size.x, range, size.z);

            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                    heights[z, x] = (heights[z, x] * size.y + depth) / range;
            }

            source.SetHeightsDelayLOD(0, 0, heights);

            if (water)
            {
                StyleTerrainWater.Rescale(data, size.y / range);
                EditorUtility.SetDirty(data.waterLevel);
            }

            transform.position -= Vector3.up * depth;

            foreach (Transform child in transform)
                child.position += Vector3.up * depth;

            data.Sink(depth);
            data.RebuildBounds();
            StyleTerrainPlanter.FollowAll(data);
            EditorUtility.SetDirty(data);
        }
    }
}
