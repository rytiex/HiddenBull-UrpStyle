using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainGrass
    {
        const string GrassName = "Grass";
        const float SetRate = 0.25f;
        const float ChangeRate = 0.1f;
        const float ShapeExtent = 1.4143f;
        const int LeastResolution = 256;
        const int Channels = 4;
        const int Density = 0;
        const int Height = 1;
        const int DensityShare = 2;
        const int HeightShare = 3;

        static Texture2D s_Region;

        internal static Texture2D Ensure(HBStyleTerrainData data)
        {
            var old = data.grass;

            if (old != null && old.format == TextureFormat.RGBA32)
                return old;

            var resolution = Mathf.Max(data.controlResolution / 2, LeastResolution);
            var grass = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = GrassName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            grass.SetPixelData(new byte[resolution * resolution * Channels], 0);
            grass.Apply(false, false);

            if (old != null)
            {
                AssetDatabase.RemoveObjectFromAsset(old);
                Object.DestroyImmediate(old, true);
            }

            AssetDatabase.AddObjectToAsset(grass, data);
            data.SetGrass(grass);
            EditorUtility.SetDirty(data);
            return grass;
        }

        internal static void Stamp(HBStyleTerrainData data, StyleTerrainBrushTool tool, Vector3 local, float radius,
                                   float hardness, float strength, bool shift, StyleTerrainShape shape)
        {
            var grass = data.grass;

            if (grass == null || grass.format != TextureFormat.RGBA32)
                return;

            var resolution = grass.width;
            var scale = resolution / data.size.x;
            var centreX = local.x * scale - 0.5f;
            var centreZ = local.z * scale - 0.5f;
            var reach = Mathf.Max(radius * scale, 1f);
            var extent = reach * (shape != null ? ShapeExtent : 1f);
            var x0 = Mathf.Clamp(Mathf.FloorToInt(centreX - extent), 0, resolution - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt(centreZ - extent), 0, resolution - 1);
            var x1 = Mathf.Clamp(Mathf.CeilToInt(centreX + extent), 0, resolution - 1);
            var z1 = Mathf.Clamp(Mathf.CeilToInt(centreZ + extent), 0, resolution - 1);
            var pixels = grass.GetPixelData<byte>(0);
            var setting = tool == StyleTerrainBrushTool.GrassSetDensity || tool == StyleTerrainBrushTool.GrassSetHeight;
            var rate = setting ? SetRate : ChangeRate;

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var amount = StyleTerrainStamp.Weight(shape, x - centreX, z - centreZ, reach, hardness) * strength *
                                 rate;

                    if (amount <= 0f)
                        continue;

                    var texel = (z * resolution + x) * Channels;

                    switch (tool)
                    {
                        case StyleTerrainBrushTool.GrassDensity:
                            Mix(pixels, texel + Density, texel + DensityShare, shift ? 0f : 1f, amount);
                            break;

                        case StyleTerrainBrushTool.GrassHeight:
                            Mix(pixels, texel + Height, texel + HeightShare, shift ? 0f : 1f, amount);
                            break;

                        case StyleTerrainBrushTool.GrassSetDensity:
                            Mix(pixels, texel + Density, texel + DensityShare, StyleTerrainBrush.grassDensity,
                                amount);
                            break;

                        case StyleTerrainBrushTool.GrassSetHeight:
                            Mix(pixels, texel + Height, texel + HeightShare,
                                StyleTerrainBrush.grassHeight / StyleTerrains.GrassMostHeight, amount);
                            break;

                        default:
                            pixels[texel + DensityShare] = Fade(pixels[texel + DensityShare], amount);
                            pixels[texel + HeightShare] = Fade(pixels[texel + HeightShare], amount);
                            break;
                    }
                }
            }

            Upload(grass, pixels, x0, z0, x1 - x0 + 1, z1 - z0 + 1);
        }

        static void Mix(NativeArray<byte> pixels, int value, int share, float target, float amount)
        {
            var painted = pixels[share] / 255f;
            var total = painted + amount;
            var mixed = (pixels[value] / 255f * painted + target * amount) / total;

            pixels[value] = Step(pixels[value], mixed);
            pixels[share] = Step(pixels[share], Mathf.Min(total, 1f));
        }

        static byte Step(byte old, float next)
        {
            var exact = next * 255f;
            var wanted = Mathf.RoundToInt(exact);

            if (wanted == old && Mathf.Abs(exact - old) > 0.01f)
                wanted += exact > old ? 1 : -1;

            return (byte)Mathf.Clamp(wanted, 0, 255);
        }

        static byte Fade(byte share, float amount)
        {
            return (byte)Mathf.Max(share - Mathf.Max(Mathf.RoundToInt(amount * 255f), 1), 0);
        }

        internal static Vector4 Painted(HBStyleTerrainData data, Vector3 local)
        {
            var grass = data.grass;

            if (grass == null || grass.format != TextureFormat.RGBA32)
                return Vector4.zero;

            var resolution = grass.width;
            var x = Mathf.Clamp(Mathf.FloorToInt(local.x / data.size.x * resolution), 0, resolution - 1);
            var z = Mathf.Clamp(Mathf.FloorToInt(local.z / data.size.z * resolution), 0, resolution - 1);
            var pixels = grass.GetPixelData<byte>(0);
            var texel = (z * resolution + x) * Channels;

            return new Vector4(pixels[texel + Density] / 255f,
                               pixels[texel + Height] / 255f * StyleTerrains.GrassMostHeight,
                               pixels[texel + DensityShare] / 255f, pixels[texel + HeightShare] / 255f);
        }

        static void Upload(Texture2D grass, NativeArray<byte> pixels, int x0, int z0, int width, int length)
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

            var row = width * Channels;
            var region = new byte[row * length];
            var resolution = grass.width;

            for (var z = 0; z < length; z++)
            {
                var start = ((z0 + z) * resolution + x0) * Channels;

                for (var i = 0; i < row; i++)
                    region[z * row + i] = pixels[start + i];
            }

            s_Region.SetPixelData(region, 0);
            s_Region.Apply(false, false);
            Graphics.CopyTexture(s_Region, 0, 0, 0, 0, width, length, grass, 0, 0, x0, z0);
        }
    }
}
