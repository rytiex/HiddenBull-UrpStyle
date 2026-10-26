using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainPainter
    {
        const string HandName = "Hand";
        const float PaintRate = 0.5f;
        const float ShapeExtent = 1.4143f;

        static readonly byte[] s_Weights = new byte[HBStyleTerrainData.MaxLayers];

        static Texture2D s_Region;

        internal static int Channel(HBStyleTerrainData data, HBStyleTerrainLayer layer)
        {
            var channel = data.HandChannel(layer);

            if (channel >= 0 || layer == null)
                return channel;

            var old = data.handLayers ?? new HBStyleTerrainLayer[0];

            if (old.Length >= HBStyleTerrainData.MaxLayers)
            {
                Debug.LogWarning($"HB Style Terrain: {data.name} already holds hand painting for " +
                                 $"{HBStyleTerrainData.MaxLayers} layers; clear the hand painting to paint others.");
                return -1;
            }

            var layers = new HBStyleTerrainLayer[old.Length + 1];

            old.CopyTo(layers, 0);
            layers[old.Length] = layer;

            var hand = data.hand;
            var slices = (layers.Length + 3) / 4;

            if (hand == null || hand.depth < slices)
                hand = Grow(data, hand, slices);

            data.SetHand(hand, layers);
            EditorUtility.SetDirty(data);
            return old.Length;
        }

        static Texture2DArray Grow(HBStyleTerrainData data, Texture2DArray old, int slices)
        {
            var resolution = old != null ? old.width : data.controlResolution;
            var hand = new Texture2DArray(resolution, resolution, slices, TextureFormat.RGBA32, false, true)
            {
                name = HandName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var empty = new byte[resolution * resolution * 4];

            for (var slice = 0; slice < slices; slice++)
            {
                if (old != null && slice < old.depth)
                    hand.SetPixelData(old.GetPixelData<byte>(0, slice), 0, slice);
                else
                    hand.SetPixelData(empty, 0, slice);
            }

            hand.Apply(false, false);

            if (old != null)
            {
                AssetDatabase.RemoveObjectFromAsset(old);
                Object.DestroyImmediate(old, true);
            }

            AssetDatabase.AddObjectToAsset(hand, data);
            return hand;
        }

        internal static void Stamp(HBStyleTerrainData data, int channel, Vector3 local, float radius, float hardness,
                                   float strength, bool erase, StyleTerrainShape shape)
        {
            var hand = data.hand;

            if (hand == null || channel < 0)
                return;

            var resolution = hand.width;
            var scale = resolution / data.size.x;
            var centreX = local.x * scale - 0.5f;
            var centreZ = local.z * scale - 0.5f;
            var reach = Mathf.Max(radius * scale, 1f);
            var extent = reach * (shape != null ? ShapeExtent : 1f);
            var x0 = Mathf.Clamp(Mathf.FloorToInt(centreX - extent), 0, resolution - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt(centreZ - extent), 0, resolution - 1);
            var x1 = Mathf.Clamp(Mathf.CeilToInt(centreX + extent), 0, resolution - 1);
            var z1 = Mathf.Clamp(Mathf.CeilToInt(centreZ + extent), 0, resolution - 1);
            var count = data.handChannels;
            var slices = hand.depth;
            var pixels = new Unity.Collections.NativeArray<byte>[slices];

            for (var slice = 0; slice < slices; slice++)
                pixels[slice] = hand.GetPixelData<byte>(0, slice);

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var dx = x - centreX;
                    var dz = z - centreZ;
                    var amount = StyleTerrainStamp.Weight(shape, dx, dz, reach, hardness) * strength * PaintRate;

                    if (amount <= 0f)
                        continue;

                    var texel = (z * resolution + x) * 4;

                    for (var index = 0; index < count; index++)
                        s_Weights[index] = pixels[index >> 2][texel + (index & 3)];

                    StyleTerrainStamp.Mix(s_Weights, count, channel, amount, erase);

                    for (var index = 0; index < count; index++)
                        pixels[index >> 2][texel + (index & 3)] = s_Weights[index];
                }
            }

            Upload(hand, pixels, x0, z0, x1 - x0 + 1, z1 - z0 + 1);
        }

        static void Upload(Texture2DArray hand, Unity.Collections.NativeArray<byte>[] pixels, int x0, int z0,
                           int width, int length)
        {
            if (s_Region == null || s_Region.width != width || s_Region.height != length)
            {
                if (s_Region != null)
                    Object.DestroyImmediate(s_Region);

                s_Region = new Texture2D(width, length, TextureFormat.RGBA32, false, true)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            var row = width * 4;
            var region = new byte[row * length];
            var resolution = hand.width;

            for (var slice = 0; slice < pixels.Length; slice++)
            {
                for (var z = 0; z < length; z++)
                {
                    var start = ((z0 + z) * resolution + x0) * 4;

                    for (var i = 0; i < row; i++)
                        region[z * row + i] = pixels[slice][start + i];
                }

                s_Region.SetPixelData(region, 0);
                s_Region.Apply(false, false);
                Graphics.CopyTexture(s_Region, 0, 0, 0, 0, width, length, hand, slice, 0, x0, z0);
            }
        }

        internal static void Clear(HBStyleTerrainData data)
        {
            var hand = data.hand;

            if (hand == null)
                return;

            Undo.RegisterCompleteObjectUndo(new Object[] { hand, data }, "Clear Hand Painting");

            var empty = new byte[hand.width * hand.height * 4];

            for (var slice = 0; slice < hand.depth; slice++)
                hand.SetPixelData(empty, 0, slice);

            hand.Apply(false, false);
            data.HandEdited();
            EditorUtility.SetDirty(hand);
            EditorUtility.SetDirty(data);
        }
    }
}
