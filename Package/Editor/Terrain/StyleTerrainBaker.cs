using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public static class StyleTerrainBaker
    {
        public static List<HBStyleTerrainProfileLayer> Rows(HBStyleTerrainProfile profile)
        {
            var rows = new List<HBStyleTerrainProfileLayer>();

            if (profile == null)
                return rows;

            foreach (var row in profile.layers)
            {
                if (row != null && row.layer != null && rows.FindIndex(other => other.layer == row.layer) < 0)
                    rows.Add(row);

                if (rows.Count == HBStyleTerrainProfile.MaxLayers)
                    break;
            }

            return rows;
        }

        public static bool HasBake(HBStyleTerrain owner)
        {
            var data = Data(owner);
            var cache = data != null ? StyleTerrainBakeCache.Load(data) : null;
            return cache != null && cache.Matches(data.alphamapResolution);
        }

        public static bool HasPainting(HBStyleTerrain owner)
        {
            var data = Data(owner);
            return data != null && data.alphamapLayers > 1;
        }

        public static RectInt AlphamapRect(TerrainData data, RectInt heights)
        {
            var scale = (data.alphamapResolution - 1f) / Mathf.Max(data.heightmapResolution - 1f, 1f);
            var xMin = Mathf.FloorToInt(heights.xMin * scale) - 2;
            var yMin = Mathf.FloorToInt(heights.yMin * scale) - 2;
            var xMax = Mathf.CeilToInt(heights.xMax * scale) + 2;
            var yMax = Mathf.CeilToInt(heights.yMax * scale) + 2;

            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public static List<TerrainLayer> Target(HBStyleTerrain owner)
        {
            var layers = new List<TerrainLayer>();

            foreach (var row in Rows(owner != null ? owner.profile : null))
                layers.Add(row.layer);

            return layers;
        }

        public static bool SyncLayers(HBStyleTerrain owner, bool record)
        {
            var data = Data(owner);

            if (data == null || Rows(owner.profile).Count == 0)
                return false;

            var previous = data.terrainLayers;
            var layers = Target(owner);

            if (Same(previous, layers))
            {
                owner.syncedLayers = previous;
                return false;
            }

            data.SyncTexture(TerrainData.AlphamapTextureName);

            var resolution = data.alphamapResolution;
            var current = previous.Length > 0 ? data.GetAlphamaps(0, 0, resolution, resolution) : null;
            var remapped = new float[resolution, resolution, layers.Count];
            var from = new int[layers.Count];

            for (var index = 0; index < layers.Count; index++)
                from[index] = System.Array.IndexOf(previous, layers[index]);

            Parallel.For(0, resolution, y =>
            {
                for (var x = 0; x < resolution; x++)
                {
                    var total = 0f;

                    for (var k = 0; k < layers.Count; k++)
                    {
                        var weight = current != null && from[k] >= 0 ? current[y, x, from[k]] : 0f;
                        remapped[y, x, k] = weight;
                        total += weight;
                    }

                    if (total <= 0f)
                    {
                        remapped[y, x, 0] = 1f;
                        continue;
                    }

                    for (var k = 0; k < layers.Count; k++)
                        remapped[y, x, k] /= total;
                }
            });

            var cache = StyleTerrainBakeCache.Load(data);
            var known = cache != null && cache.Matches(resolution);

            if (record)
                Record(data, known ? cache : null, "Terrain Layers");

            data.terrainLayers = layers.ToArray();
            data.SetAlphamaps(0, 0, remapped);
            EditorUtility.SetDirty(data);

            if (known)
                Rewrite(cache, layers, remapped, resolution);

            owner.syncedLayers = data.terrainLayers;

            owner.Refresh();
            StyleTerrainPreview.MarkDirty(owner, true);
            return true;
        }

        static void Rewrite(StyleTerrainBakeCache cache, List<TerrainLayer> layers, float[,,] weights, int resolution)
        {
            cache.Prepare(resolution, layers);

            var written = new byte[layers.Count][];

            for (var k = 0; k < layers.Count; k++)
                written[k] = cache.Find(layers[k]).written;

            Parallel.For(0, resolution, y =>
            {
                for (var x = 0; x < resolution; x++)
                {
                    for (var k = 0; k < layers.Count; k++)
                        written[k][y * resolution + x] = (byte)Mathf.RoundToInt(Mathf.Clamp01(weights[y, x, k]) * 255f);
                }
            });

            EditorUtility.SetDirty(cache);
        }

        public static void ClearHand(HBStyleTerrain owner, TerrainLayer layer)
        {
            var data = Data(owner);
            var cache = data != null ? StyleTerrainBakeCache.Load(data) : null;
            var channel = cache != null && cache.Matches(data.alphamapResolution) ? cache.Find(layer) : null;

            if (channel == null)
                return;

            Undo.RegisterCompleteObjectUndo(cache, "Clear Layer Hand Painting");
            System.Array.Clear(channel.hand, 0, channel.hand.Length);
            EditorUtility.SetDirty(cache);

            StyleTerrainPreview.MarkDirty(owner, true);
        }

        public static bool Bake(HBStyleTerrain owner, RectInt? area, bool keepHand, bool record)
        {
            var terrain = owner != null ? owner.GetComponent<Terrain>() : null;
            var data = Data(owner);
            var rows = Rows(owner != null ? owner.profile : null);

            if (terrain == null || data == null || rows.Count == 0 || !StyleTerrainGpu.supported)
                return false;

            SyncLayers(owner, record);

            var layers = data.terrainLayers;
            var count = layers.Length;
            var resolution = data.alphamapResolution;
            var full = new RectInt(0, 0, resolution, resolution);
            var rect = area == null ? full : Clip(area.Value, full);

            if (rect.width <= 0 || rect.height <= 0)
                return false;

            var cache = StyleTerrainBakeCache.LoadOrCreate(data);

            if (cache == null)
                return false;

            owner.RefreshControl();

            var known = keepHand && cache.Matches(resolution);
            var adopt = keepHand && !known;
            Texture2DArray written = null;
            Texture2DArray hand = null;

            if (known)
            {
                written = StyleTerrainGpu.Upload(Channels(cache, layers, false), resolution);
                hand = StyleTerrainGpu.Upload(Channels(cache, layers, true), resolution);
            }
            else if (adopt)
            {
                written = StyleTerrainGpu.Upload(Adopted(data, rows, layers), resolution);
            }

            var slices = (count + 3) / 4;
            var result = StyleTerrainGpu.NewTarget(resolution, slices);
            var handOut = StyleTerrainGpu.NewTarget(resolution, slices);

            try
            {
                StyleTerrainGpu.Run(terrain, owner.control, rows, count, rect, written, hand, known || adopt, result,
                                    handOut);

                var results = new byte[slices][];
                var hands = new byte[slices][];

                for (var slice = 0; slice < slices; slice++)
                {
                    results[slice] = StyleTerrainGpu.Read(result, rect, slice);
                    hands[slice] = StyleTerrainGpu.Read(handOut, rect, slice);

                    if (results[slice] == null || hands[slice] == null)
                        return false;
                }

                if (record)
                    Record(data, cache, "Bake Terrain Layers");

                cache.Prepare(resolution, layers);
                Store(cache, layers, results, hands, rect, resolution, data);
            }
            finally
            {
                StyleTerrainGpu.Release(result);
                StyleTerrainGpu.Release(handOut);
                StyleTerrainGpu.Release(written);
                StyleTerrainGpu.Release(hand);
            }

            EditorUtility.SetDirty(cache);
            EditorUtility.SetDirty(data);

            owner.Refresh();
            return true;
        }

        static void Store(StyleTerrainBakeCache cache, TerrainLayer[] layers, byte[][] results, byte[][] hands,
                          RectInt rect, int resolution, TerrainData data)
        {
            var count = layers.Length;
            var weights = new float[rect.height, rect.width, count];
            var written = new byte[count][];
            var held = new byte[count][];

            for (var k = 0; k < count; k++)
            {
                var channel = cache.Find(layers[k]);
                written[k] = channel.written;
                held[k] = channel.hand;
            }

            Parallel.For(0, rect.height, row =>
            {
                for (var column = 0; column < rect.width; column++)
                {
                    var texel = (rect.y + row) * resolution + rect.x + column;
                    var source = (row * rect.width + column) * 4;

                    for (var k = 0; k < count; k++)
                    {
                        var value = results[k >> 2][source + (k & 3)];
                        weights[row, column, k] = value / 255f;
                        written[k][texel] = value;
                        held[k][texel] = hands[k >> 2][source + (k & 3)];
                    }
                }
            });

            data.SetAlphamaps(rect.x, rect.y, weights);
        }

        static List<byte[]> Channels(StyleTerrainBakeCache cache, TerrainLayer[] layers, bool hand)
        {
            var channels = new List<byte[]>(layers.Length);

            foreach (var layer in layers)
            {
                var channel = cache.Find(layer);
                channels.Add(channel == null ? null : hand ? channel.hand : channel.written);
            }

            return channels;
        }

        internal static List<byte[]> Adopted(TerrainData data, List<HBStyleTerrainProfileLayer> rows,
                                             TerrainLayer[] layers)
        {
            data.SyncTexture(TerrainData.AlphamapTextureName);

            var resolution = data.alphamapResolution;
            var current = data.GetAlphamaps(0, 0, resolution, resolution);
            var channels = new List<byte[]>(layers.Length);

            for (var k = 0; k < layers.Length; k++)
            {
                var row = k < rows.Count ? rows[k] : null;
                var painted = k > 0 && (row == null || !row.automatic);

                if (painted)
                {
                    channels.Add(null);
                    continue;
                }

                var bytes = new byte[resolution * resolution];
                var layer = k;

                Parallel.For(0, resolution, y =>
                {
                    for (var x = 0; x < resolution; x++)
                        bytes[y * resolution + x] = (byte)Mathf.RoundToInt(Mathf.Clamp01(current[y, x, layer]) * 255f);
                });

                channels.Add(bytes);
            }

            return channels;
        }

        static void Record(TerrainData data, Object cache, string name)
        {
            var targets = new List<Object> { data };

            if (cache != null)
                targets.Add(cache);

            targets.AddRange(data.alphamapTextures);
            Undo.RegisterCompleteObjectUndo(targets.ToArray(), name);
        }

        static TerrainData Data(HBStyleTerrain owner)
        {
            var terrain = owner != null ? owner.GetComponent<Terrain>() : null;
            return terrain != null ? terrain.terrainData : null;
        }

        static bool Same(TerrainLayer[] previous, List<TerrainLayer> layers)
        {
            if (previous.Length != layers.Count)
                return false;

            for (var index = 0; index < layers.Count; index++)
            {
                if (previous[index] != layers[index])
                    return false;
            }

            return true;
        }

        static RectInt Clip(RectInt rect, RectInt bounds)
        {
            var xMin = Mathf.Max(rect.xMin, bounds.xMin);
            var yMin = Mathf.Max(rect.yMin, bounds.yMin);
            var xMax = Mathf.Min(rect.xMax, bounds.xMax);
            var yMax = Mathf.Min(rect.yMax, bounds.yMax);

            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
