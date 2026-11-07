using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    struct StyleTerrainWaterPatch
    {
        internal Vector2 at;
        internal float texel;
        internal float radius;
        internal int resolution;
        internal int left;
        internal int right;
        internal int bottom;
        internal int top;

        internal bool empty => left > right || bottom > top;

        internal RectInt rect => new RectInt(left, bottom, right - left + 1, top - bottom + 1);

        internal static StyleTerrainWaterPatch Cover(HBStyleTerrainData data, int resolution, Vector3 local,
                                                     float radius)
        {
            var patch = new StyleTerrainWaterPatch
            {
                resolution = resolution,
                radius = Mathf.Max(radius, 0.01f),
                texel = data.size.x / resolution
            };

            patch.at = new Vector2(local.x / patch.texel - 0.5f, local.z / patch.texel - 0.5f);
            patch.left = Mathf.Max(Mathf.FloorToInt(patch.at.x - radius / patch.texel), 0);
            patch.right = Mathf.Min(Mathf.CeilToInt(patch.at.x + radius / patch.texel), resolution - 1);
            patch.bottom = Mathf.Max(Mathf.FloorToInt(patch.at.y - radius / patch.texel), 0);
            patch.top = Mathf.Min(Mathf.CeilToInt(patch.at.y + radius / patch.texel), resolution - 1);
            return patch;
        }

        internal Vector2 Offset(int x, int y)
        {
            return new Vector2((x - at.x) * texel, (y - at.y) * texel);
        }

        internal float Weight(int x, int y)
        {
            var weight = 1f - Offset(x, y).magnitude / radius;

            return weight <= 0f ? 0f : weight * weight * (3f - 2f * weight);
        }
    }

    static class StyleTerrainWater
    {
        internal const float FlowMost = 6f;
        internal const float FlowStep = 0.25f;
        internal const float FlatSlope = 0.001f;

        const string LevelName = "Liquid Level";
        const string MixName = "Liquid Mix";
        const string FlowName = "Liquid Flow";
        const int LeastResolution = 256;
        const int Zero = 32768;
        const float Span = 32767f;
        const ushort DryValue = 65535;
        const float FlowLeast = 0.01f;
        const float FlowBlend = 0.5f;
        const float RaiseStep = 0.25f;
        const float FallFull = 0.1f;
        const float WhirlPull = 0.35f;
        const float WhirlDip = 0.2f;
        const float RampCore = 0.7f;
        const int FillFringe = 2;
        const byte Unknown = 0;
        const byte Hollow = 1;
        const byte Solid = 2;
        const byte Flooded = 3;
        const int Stride = 4;
        const int CalmChannel = 2;

        static readonly List<int> s_Filled = new List<int>();
        static readonly Dictionary<TextureFormat, Texture2D> s_Regions = new Dictionary<TextureFormat, Texture2D>();

        static byte[] s_State;
        static float[] s_Soil;
        static HBStyleTerrainData s_SoilData;
        static int s_SoilVersion = -1;
        static float[] s_Before;

        internal static bool Ready(HBStyleTerrainData data)
        {
            return data.waterFormat == HBStyleTerrainData.WaterFormat && data.waterLevel != null &&
                   data.waterMix != null && data.waterFlow != null && data.waterLevel.format == TextureFormat.RG32 &&
                   data.waterMix.format == TextureFormat.RGBA32 && data.waterFlow.format == TextureFormat.RGBAHalf;
        }

        internal static bool Ensure(HBStyleTerrainData data)
        {
            if (Ready(data))
                return true;

            if (data.terrainData == null)
                return false;

            var fine = Mathf.Max(data.controlResolution, LeastResolution);
            var coarse = Mathf.Max(data.controlResolution / 2, LeastResolution);

            Remove(data.waterLevel);
            Remove(data.waterMix);
            Remove(data.waterFlow);

            var level = Create(LevelName, fine, TextureFormat.RG32);
            var mix = Create(MixName, coarse, TextureFormat.RGBA32);
            var flow = Create(FlowName, coarse, TextureFormat.RGBAHalf);
            var levels = level.GetPixelData<ushort>(0);

            for (var texel = 0; texel < levels.Length; texel += 2)
                levels[texel] = Zero;

            level.Apply(false, false);
            mix.SetPixelData(new byte[coarse * coarse * 4], 0);
            mix.Apply(false, false);
            flow.SetPixelData(new ushort[coarse * coarse * Stride], 0);
            flow.Apply(false, false);

            AssetDatabase.AddObjectToAsset(level, data);
            AssetDatabase.AddObjectToAsset(mix, data);
            AssetDatabase.AddObjectToAsset(flow, data);
            data.SetWater(level, mix, flow, HBStyleTerrainData.WaterFormat);
            EditorUtility.SetDirty(data);
            return true;
        }

        static Texture2D Create(string name, int resolution, TextureFormat format)
        {
            return new Texture2D(resolution, resolution, format, false, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        static void Remove(Texture2D texture)
        {
            if (texture == null)
                return;

            AssetDatabase.RemoveObjectFromAsset(texture);
            Object.DestroyImmediate(texture, true);
        }

        internal static Object[] Recorded(HBStyleTerrainData data)
        {
            return new Object[] { data.waterLevel, data.waterMix, data.waterFlow, data };
        }

        internal static void Rescale(HBStyleTerrainData data, float factor)
        {
            var levels = data.waterLevel.GetPixelData<ushort>(0);

            for (var texel = 0; texel < levels.Length; texel += 2)
            {
                var scaled = Mathf.RoundToInt(Zero + (levels[texel] - Zero) * factor);

                levels[texel] = (ushort)Mathf.Clamp(scaled, 0, ushort.MaxValue);
            }

            UploadLevel(data, levels, new RectInt(0, 0, data.waterLevel.width, data.waterLevel.height));
        }

        internal static float Decode(HBStyleTerrainData data, ushort value)
        {
            return (value - Zero) / Span * data.size.y;
        }

        static ushort Encode(HBStyleTerrainData data, float offset)
        {
            return (ushort)Mathf.Clamp(Mathf.RoundToInt(offset / Mathf.Max(data.size.y, 0.01f) * Span) + Zero, 0,
                                       65535);
        }

        static StyleTerrainWaterPatch LevelPatch(HBStyleTerrainData data, Vector3 local, float radius)
        {
            return StyleTerrainWaterPatch.Cover(data, data.waterLevel.width, local, radius);
        }

        static StyleTerrainWaterPatch DetailPatch(HBStyleTerrainData data, Vector3 local, float radius)
        {
            return StyleTerrainWaterPatch.Cover(data, data.waterMix.width, local, radius);
        }

        internal static void Stroke(HBStyleTerrainData data, StyleTerrainLiquidTool tool, Vector3 local, float radius,
                                    float strength, bool lower, float target)
        {
            var patch = LevelPatch(data, local, radius);

            if (patch.empty)
                return;

            var levels = data.waterLevel.GetPixelData<ushort>(0);

            if (tool == StyleTerrainLiquidTool.Smooth)
                Keep(data, levels, patch);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * strength;

                    if (weight <= 0f)
                        continue;

                    var index = (y * patch.resolution + x) * 2;
                    var offset = Decode(data, levels[index]);

                    if (tool == StyleTerrainLiquidTool.Raise)
                        offset += (lower ? -RaiseStep : RaiseStep) * weight;
                    else if (tool == StyleTerrainLiquidTool.SetHeight)
                        offset = Mathf.Lerp(offset, target, weight);
                    else
                        offset = Mathf.Lerp(offset, Around(patch, x, y, 1, 0), weight);

                    levels[index] = Encode(data, offset);
                }
            }

            UploadLevel(data, levels, patch.rect);
        }

        internal static void Cut(HBStyleTerrainData data, Vector3 local, float radius, bool fill)
        {
            var cells = CutCells(data);
            var patch = StyleTerrainWaterPatch.Cover(data, cells, local, radius);

            if (patch.empty)
                return;

            var width = data.waterLevel.width;
            var levels = data.waterLevel.GetPixelData<ushort>(0);
            var value = fill ? (ushort)0 : DryValue;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    if (patch.Offset(x, y).magnitude <= patch.radius)
                        CutCell(levels, width, cells, x, y, value);
                }
            }

            var underX = Mathf.Clamp(Mathf.FloorToInt(local.x / patch.texel), patch.left, patch.right);
            var underY = Mathf.Clamp(Mathf.FloorToInt(local.z / patch.texel), patch.bottom, patch.top);

            CutCell(levels, width, cells, underX, underY, value);

            var left = CellFirst(patch.left, width, cells);
            var bottom = CellFirst(patch.bottom, width, cells);
            var right = CellFirst(patch.right + 1, width, cells);
            var top = CellFirst(patch.top + 1, width, cells);

            UploadLevel(data, levels, new RectInt(left, bottom, right - left, top - bottom));
        }

        internal static int CutCells(HBStyleTerrainData data)
        {
            var width = data.waterLevel.width;
            var holes = data.terrainData != null ? data.terrainData.holesResolution : 0;

            return holes > 0 ? Mathf.Min(holes, width) : width;
        }

        static void CutCell(NativeArray<ushort> levels, int width, int cells, int x, int y, ushort value)
        {
            var x1 = CellFirst(x + 1, width, cells);
            var y1 = CellFirst(y + 1, width, cells);

            for (var v = CellFirst(y, width, cells); v < y1; v++)
            {
                for (var u = CellFirst(x, width, cells); u < x1; u++)
                    levels[(v * width + u) * 2 + 1] = value;
            }
        }

        static int CellFirst(int cell, int width, int cells)
        {
            return Mathf.Clamp(Mathf.CeilToInt((float)cell * width / cells - 0.5f), 0, width);
        }

        internal static void Paint(HBStyleTerrainData data, int slot, Vector3 local, float radius, float strength)
        {
            var patch = DetailPatch(data, local, radius);
            var mixes = data.waterMix.GetPixelData<byte>(0);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * strength;

                    if (weight <= 0f)
                        continue;

                    var first = (y * patch.resolution + x) * 4;

                    for (var channel = 0; channel < HBStyleTerrainData.LiquidSlots; channel++)
                    {
                        var value = mixes[first + channel] / 255f;
                        var target = channel == slot;

                        value = target ? Mathf.Lerp(value, 1f, weight) : value * (1f - weight);
                        mixes[first + channel] = (byte)(target ? Mathf.CeilToInt(value * 255f)
                                                               : Mathf.FloorToInt(value * 255f));
                    }
                }
            }

            Upload(data.waterMix, mixes, patch.rect);
        }

        internal static void Lull(HBStyleTerrainData data, Vector3 local, float radius, float strength, bool back)
        {
            var patch = DetailPatch(data, local, radius);
            var flows = data.waterFlow.GetPixelData<ushort>(0);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * strength;

                    if (weight <= 0f)
                        continue;

                    var index = (y * patch.resolution + x) * Stride + CalmChannel;
                    var value = Mathf.Lerp(Mathf.HalfToFloat(flows[index]), back ? 0f : 1f, weight);

                    flows[index] = Mathf.FloatToHalf(back && value < 1f / 255f ? 0f
                                                     : !back && value > 254f / 255f ? 1f : value);
                }
            }

            UploadFlow(data, flows, patch.rect);
        }

        internal static void Run(HBStyleTerrainData data, Vector3 local, float radius, Vector2 wanted)
        {
            var patch = DetailPatch(data, local, radius);
            var flows = data.waterFlow.GetPixelData<ushort>(0);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * FlowBlend;

                    if (weight > 0f)
                        Steer(flows, (y * patch.resolution + x) * Stride, wanted, weight);
                }
            }

            UploadFlow(data, flows, patch.rect);
        }

        internal static void Settle(HBStyleTerrainData data, Vector3 local, float radius, float strength)
        {
            var patch = DetailPatch(data, local, radius);

            if (patch.empty)
                return;

            var flows = data.waterFlow.GetPixelData<ushort>(0);

            KeepFlow(flows, patch);

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * strength;

                    if (weight > 0f)
                    {
                        var wanted = new Vector2(Around(patch, x, y, 2, 0), Around(patch, x, y, 2, 1));

                        Steer(flows, (y * patch.resolution + x) * Stride, wanted, weight);
                    }
                }
            }

            UploadFlow(data, flows, patch.rect);
        }

        internal static Vector2 Fall(HBStyleTerrainData data, Vector3 local)
        {
            if (!Ready(data))
                return Vector2.zero;

            var step = data.size.x / data.waterFlow.width;
            var east = Offset(data, local + Vector3.right * step) - Offset(data, local - Vector3.right * step);
            var north = Offset(data, local + Vector3.forward * step) - Offset(data, local - Vector3.forward * step);

            return new Vector2(-east, -north) / (step * 2f);
        }

        internal static void Descend(HBStyleTerrainData data, Vector3 local, float radius, float speed)
        {
            var patch = DetailPatch(data, local, radius);
            var flows = data.waterFlow.GetPixelData<ushort>(0);
            var step = patch.texel;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var weight = patch.Weight(x, y) * FlowBlend;

                    if (weight <= 0f)
                        continue;

                    var fall = Fall(data, new Vector3((x + 0.5f) * step, 0f, (y + 0.5f) * step));
                    var steep = fall.magnitude;
                    var wanted = steep > 1e-5f ? fall / steep * (speed * Mathf.Clamp01(steep / FallFull))
                                               : Vector2.zero;

                    Steer(flows, (y * patch.resolution + x) * Stride, wanted, weight);
                }
            }

            UploadFlow(data, flows, patch.rect);
        }

        internal static void Swirl(HBStyleTerrainData data, Vector3 local, float radius, float speed, float strength,
                                   bool reverse)
        {
            var patch = DetailPatch(data, local, radius);
            var flows = data.waterFlow.GetPixelData<ushort>(0);
            var turn = reverse ? -1f : 1f;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var offset = patch.Offset(x, y);
                    var away = offset.magnitude;
                    var share = away / patch.radius;

                    if (share >= 1f)
                        continue;

                    var wanted = away > 1e-4f
                        ? (new Vector2(-offset.y, offset.x) * turn - offset * WhirlPull) *
                          (speed * 4f * share * (1f - share) / away)
                        : Vector2.zero;

                    Steer(flows, (y * patch.resolution + x) * Stride, wanted, 1f - share * share);
                }
            }

            UploadFlow(data, flows, patch.rect);

            var levelPatch = LevelPatch(data, local, radius);
            var levels = data.waterLevel.GetPixelData<ushort>(0);

            for (var y = levelPatch.bottom; y <= levelPatch.top; y++)
            {
                for (var x = levelPatch.left; x <= levelPatch.right; x++)
                {
                    var share = levelPatch.Offset(x, y).magnitude / levelPatch.radius;

                    if (share >= 1f)
                        continue;

                    var index = (y * levelPatch.resolution + x) * 2;
                    var dip = strength * levelPatch.radius * WhirlDip * (1f - share) * (1f - share);

                    levels[index] = Encode(data, Decode(data, levels[index]) - dip);
                }
            }

            UploadLevel(data, levels, levelPatch.rect);
        }

        static void Steer(NativeArray<ushort> flows, int first, Vector2 wanted, float weight)
        {
            for (var axis = 0; axis < 2; axis++)
            {
                var value = Mathf.Lerp(Mathf.HalfToFloat(flows[first + axis]), wanted[axis], weight);

                flows[first + axis] = Mathf.FloatToHalf(Mathf.Abs(value) < FlowLeast ? 0f : value);
            }
        }

        internal static RectInt Flood(HBStyleTerrainData data, Vector4 seed)
        {
            var resolution = data.waterLevel.width;
            var texel = data.size.x / resolution;
            var column = Mathf.FloorToInt(seed.x / texel);
            var row = Mathf.FloorToInt(seed.z / texel);
            var filled = s_Filled;
            var count = resolution * resolution;

            filled.Clear();

            if (column < 0 || row < 0 || column >= resolution || row >= resolution)
                return new RectInt();

            if (s_State == null || s_State.Length != count)
                s_State = new byte[count];
            else
                System.Array.Clear(s_State, 0, count);

            if (s_Soil == null || s_Soil.Length != count || s_SoilData != data || s_SoilVersion != data.version)
            {
                s_Soil = new float[count];
                s_SoilData = data;
                s_SoilVersion = data.version;

                for (var index = 0; index < count; index++)
                    s_Soil[index] = float.NaN;
            }

            var state = s_State;
            var terrain = data.terrainData;

            bool Open(int x, int y)
            {
                if (x < 0 || y < 0 || x >= resolution || y >= resolution)
                    return false;

                var index = y * resolution + x;

                if (state[index] == Unknown)
                {
                    if (float.IsNaN(s_Soil[index]))
                        s_Soil[index] = terrain.GetInterpolatedHeight((x + 0.5f) / resolution, (y + 0.5f) / resolution);

                    state[index] = s_Soil[index] < seed.y ? Hollow : Solid;
                }

                if (state[index] != Hollow)
                    return false;

                state[index] = Flooded;
                filled.Add(index);
                return true;
            }

            if (!Open(column, row))
                return new RectInt();

            for (var next = 0; next < filled.Count; next++)
            {
                var x = filled[next] % resolution;
                var y = filled[next] / resolution;

                Open(x - 1, y);
                Open(x + 1, y);
                Open(x, y - 1);
                Open(x, y + 1);
            }

            var first = 0;

            for (var pass = 0; pass < FillFringe; pass++)
            {
                var last = filled.Count;

                for (var next = first; next < last; next++)
                {
                    var x = filled[next] % resolution;
                    var y = filled[next] / resolution;

                    for (var side = 0; side < 9; side++)
                    {
                        var besideX = x + side % 3 - 1;
                        var besideY = y + side / 3 - 1;

                        if (besideX < 0 || besideY < 0 || besideX >= resolution || besideY >= resolution)
                            continue;

                        var beside = besideY * resolution + besideX;

                        if (state[beside] == Flooded)
                            continue;

                        state[beside] = Flooded;
                        filled.Add(beside);
                    }
                }

                first = last;
            }

            var levels = data.waterLevel.GetPixelData<ushort>(0);
            var wanted = Encode(data, seed.y - data.seaLevel);
            int left = resolution, right = -1, bottom = resolution, top = -1;

            foreach (var index in filled)
            {
                var x = index % resolution;
                var y = index / resolution;

                levels[index * 2] = wanted;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }

            return right < left ? new RectInt() : new RectInt(left, bottom, right - left + 1, top - bottom + 1);
        }

        internal static RectInt Pool(HBStyleTerrainData data, Vector4 pin, bool round)
        {
            var wide = Mathf.Max(pin.w, 0.5f);
            var patch = LevelPatch(data, new Vector3(pin.x, 0f, pin.z), wide);
            var levels = data.waterLevel.GetPixelData<ushort>(0);
            var wanted = pin.y - data.seaLevel;

            for (var y = patch.bottom; y <= patch.top; y++)
            {
                for (var x = patch.left; x <= patch.right; x++)
                {
                    var offset = patch.Offset(x, y);
                    var apart = round ? offset.magnitude : Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                    var weight = Mathf.Clamp01((1f - apart / wide) / (1f - RampCore));

                    if (weight <= 0f)
                        continue;

                    var index = (y * patch.resolution + x) * 2;
                    var blend = weight * weight * (3f - 2f * weight);

                    levels[index] = Encode(data, Mathf.Lerp(Decode(data, levels[index]), wanted, blend));
                }
            }

            return patch.empty ? new RectInt() : patch.rect;
        }

        internal static RectInt Ramp(HBStyleTerrainData data, IReadOnlyList<Vector4> path, bool course, float speed)
        {
            if (path.Count < 2)
                return new RectInt();

            var low = new Vector2(float.MaxValue, float.MaxValue);
            var high = new Vector2(float.MinValue, float.MinValue);

            foreach (var pin in path)
            {
                low = Vector2.Min(low, new Vector2(pin.x - pin.w, pin.z - pin.w));
                high = Vector2.Max(high, new Vector2(pin.x + pin.w, pin.z + pin.w));
            }

            var levels = data.waterLevel.GetPixelData<ushort>(0);
            var area = Area(data.waterLevel.width, data.size.x, low, high);
            var texel = data.size.x / data.waterLevel.width;

            for (var y = area.yMin; y < area.yMax; y++)
            {
                for (var x = area.xMin; x < area.xMax; x++)
                {
                    var place = new Vector2((x + 0.5f) * texel, (y + 0.5f) * texel);
                    var blend = Along(path, place, out var height, out _);

                    if (blend <= 0f)
                        continue;

                    var index = (y * data.waterLevel.width + x) * 2;

                    levels[index] = Encode(data, Mathf.Lerp(Decode(data, levels[index]), height - data.seaLevel,
                                                            blend));
                }
            }

            if (!course || speed <= 0f)
                return area;

            var flows = data.waterFlow.GetPixelData<ushort>(0);
            var detail = Area(data.waterFlow.width, data.size.x, low, high);
            var coarse = data.size.x / data.waterFlow.width;

            for (var y = detail.yMin; y < detail.yMax; y++)
            {
                for (var x = detail.xMin; x < detail.xMax; x++)
                {
                    var blend = Along(path, new Vector2((x + 0.5f) * coarse, (y + 0.5f) * coarse), out _,
                                      out var heading);

                    if (blend > 0f)
                        Steer(flows, (y * data.waterFlow.width + x) * Stride, heading * speed, blend);
                }
            }

            UploadFlow(data, flows, detail);
            return area;
        }

        static RectInt Area(int resolution, float size, Vector2 low, Vector2 high)
        {
            var texel = size / resolution;
            var left = Mathf.Max(Mathf.FloorToInt(low.x / texel), 0);
            var right = Mathf.Min(Mathf.CeilToInt(high.x / texel), resolution - 1);
            var bottom = Mathf.Max(Mathf.FloorToInt(low.y / texel), 0);
            var top = Mathf.Min(Mathf.CeilToInt(high.y / texel), resolution - 1);

            return right < left || top < bottom ? new RectInt()
                                                : new RectInt(left, bottom, right - left + 1, top - bottom + 1);
        }

        static float Along(IReadOnlyList<Vector4> path, Vector2 place, out float height, out Vector2 heading)
        {
            var nearest = 1f;

            height = 0f;
            heading = Vector2.zero;

            for (var index = 0; index + 1 < path.Count; index++)
            {
                var from = path[index];
                var to = path[index + 1];
                var start = new Vector2(from.x, from.z);
                var span = new Vector2(to.x, to.z) - start;
                var along = Mathf.Clamp01(Vector2.Dot(place - start, span) / Mathf.Max(span.sqrMagnitude, 1e-4f));
                var apart = (place - start - span * along).magnitude / Mathf.Max(Mathf.Lerp(from.w, to.w, along), 0.1f);

                if (apart >= nearest)
                    continue;

                nearest = apart;
                height = Mathf.Lerp(from.y, to.y, along);
                heading = (from.y >= to.y ? span : -span).normalized;
            }

            var weight = Mathf.Clamp01((1f - nearest) / (1f - RampCore));

            return weight * weight * (3f - 2f * weight);
        }

        internal static void RemoveLiquid(HBStyleTerrainData data, int slot)
        {
            var ready = Ready(data);

            Undo.RegisterCompleteObjectUndo(ready ? new Object[] { data.waterMix, data } : new Object[] { data },
                                            "Remove Liquid");

            if (ready)
            {
                var mixes = data.waterMix.GetPixelData<byte>(0);

                for (var first = 0; first < mixes.Length; first += 4)
                {
                    for (var channel = slot; channel < 3; channel++)
                        mixes[first + channel] = mixes[first + channel + 1];

                    mixes[first + 3] = 0;
                }

                data.waterMix.Apply(false, false);
                EditorUtility.SetDirty(data.waterMix);
            }

            data.RemoveLiquid(slot);
            data.WaterEdited();
            EditorUtility.SetDirty(data);
        }

        internal static void Clear(HBStyleTerrainData data, StyleTerrainWaterPart part)
        {
            if (!Ready(data))
                return;

            Undo.RegisterCompleteObjectUndo(Recorded(data), "Clear Liquid " + part);

            switch (part)
            {
                case StyleTerrainWaterPart.Level:
                case StyleTerrainWaterPart.NoLiquid:
                    var levels = data.waterLevel.GetPixelData<ushort>(0);
                    var start = part == StyleTerrainWaterPart.Level ? 0 : 1;

                    for (var index = start; index < levels.Length; index += 2)
                        levels[index] = part == StyleTerrainWaterPart.Level ? (ushort)Zero : (ushort)0;

                    data.waterLevel.Apply(false, false);
                    EditorUtility.SetDirty(data.waterLevel);
                    break;

                case StyleTerrainWaterPart.Liquids:
                    var mixes = data.waterMix.GetPixelData<byte>(0);

                    for (var index = 0; index < mixes.Length; index++)
                        mixes[index] = 0;

                    data.waterMix.Apply(false, false);
                    EditorUtility.SetDirty(data.waterMix);
                    break;

                default:
                    var flows = data.waterFlow.GetPixelData<ushort>(0);
                    var calm = part == StyleTerrainWaterPart.Calm;

                    for (var index = 0; index < flows.Length; index++)
                    {
                        var channel = index % Stride;

                        if (calm ? channel == CalmChannel : channel < CalmChannel)
                            flows[index] = 0;
                    }

                    data.waterFlow.Apply(false, false);
                    EditorUtility.SetDirty(data.waterFlow);
                    break;
            }

            data.WaterEdited();
            EditorUtility.SetDirty(data);
        }

        internal static float Offset(HBStyleTerrainData data, Vector3 local)
        {
            if (!Ready(data))
                return 0f;

            var levels = data.waterLevel.GetPixelData<ushort>(0);

            return Bilinear(data.waterLevel.width, data.size.x, local, (index) => Decode(data, levels[index * 2]));
        }

        internal static float Level(HBStyleTerrainData data, Vector3 local)
        {
            return data.seaLevel + Offset(data, local);
        }

        internal static bool DryTexel(HBStyleTerrainData data, Vector3 local)
        {
            if (!Ready(data))
                return false;

            var resolution = data.waterLevel.width;
            var x = Mathf.FloorToInt(local.x / data.size.x * resolution);
            var y = Mathf.FloorToInt(local.z / data.size.z * resolution);

            if (x < 0 || y < 0 || x >= resolution || y >= resolution)
                return false;

            return data.waterLevel.GetPixelData<ushort>(0)[(y * resolution + x) * 2 + 1] > 32767;
        }

        internal static Vector2 Stream(HBStyleTerrainData data, Vector3 local)
        {
            if (!Ready(data))
                return Vector2.zero;

            var flows = data.waterFlow.GetPixelData<ushort>(0);
            var resolution = data.waterFlow.width;

            return new Vector2(
                Bilinear(resolution, data.size.x, local, (index) => Mathf.HalfToFloat(flows[index * Stride])),
                Bilinear(resolution, data.size.x, local, (index) => Mathf.HalfToFloat(flows[index * Stride + 1])));
        }

        internal static float Still(HBStyleTerrainData data, Vector3 local)
        {
            if (!Ready(data))
                return 0f;

            var flows = data.waterFlow.GetPixelData<ushort>(0);

            return Bilinear(data.waterFlow.width, data.size.x, local,
                            (index) => Mathf.HalfToFloat(flows[index * Stride + CalmChannel]));
        }

        static float Bilinear(int resolution, float size, Vector3 local, System.Func<int, float> read)
        {
            var most = resolution - 1;
            var x = Mathf.Clamp(local.x / size * resolution - 0.5f, 0f, most);
            var y = Mathf.Clamp(local.z / size * resolution - 0.5f, 0f, most);
            var column = Mathf.Min((int)x, Mathf.Max(most - 1, 0));
            var row = Mathf.Min((int)y, Mathf.Max(most - 1, 0));
            var first = row * resolution + column;
            var second = first + resolution;
            var across = x - column;

            return Mathf.Lerp(Mathf.Lerp(read(first), read(first + 1), across),
                              Mathf.Lerp(read(second), read(second + 1), across), y - row);
        }

        static void Keep(HBStyleTerrainData data, NativeArray<ushort> levels, StyleTerrainWaterPatch patch)
        {
            Grow(patch, 1);

            for (var y = Mathf.Max(patch.bottom - 1, 0); y <= Mathf.Min(patch.top + 1, patch.resolution - 1); y++)
            {
                for (var x = Mathf.Max(patch.left - 1, 0); x <= Mathf.Min(patch.right + 1, patch.resolution - 1); x++)
                {
                    var index = y * patch.resolution + x;

                    s_Before[index] = Decode(data, levels[index * 2]);
                }
            }
        }

        static void KeepFlow(NativeArray<ushort> flows, StyleTerrainWaterPatch patch)
        {
            Grow(patch, 2);

            for (var y = Mathf.Max(patch.bottom - 1, 0); y <= Mathf.Min(patch.top + 1, patch.resolution - 1); y++)
            {
                for (var x = Mathf.Max(patch.left - 1, 0); x <= Mathf.Min(patch.right + 1, patch.resolution - 1); x++)
                {
                    var index = y * patch.resolution + x;

                    s_Before[index * 2] = Mathf.HalfToFloat(flows[index * Stride]);
                    s_Before[index * 2 + 1] = Mathf.HalfToFloat(flows[index * Stride + 1]);
                }
            }
        }

        static void Grow(StyleTerrainWaterPatch patch, int width)
        {
            var count = patch.resolution * patch.resolution * width;

            if (s_Before == null || s_Before.Length != count)
                s_Before = new float[count];
        }

        static float Around(StyleTerrainWaterPatch patch, int x, int y, int width, int channel)
        {
            var most = patch.resolution - 1;
            var west = s_Before[(y * patch.resolution + Mathf.Max(x - 1, 0)) * width + channel];
            var east = s_Before[(y * patch.resolution + Mathf.Min(x + 1, most)) * width + channel];
            var south = s_Before[(Mathf.Max(y - 1, 0) * patch.resolution + x) * width + channel];
            var north = s_Before[(Mathf.Min(y + 1, most) * patch.resolution + x) * width + channel];

            return (west + east + south + north) * 0.25f;
        }

        internal static ushort[] Hold(HBStyleTerrainData data)
        {
            return data.waterLevel.GetPixelData<ushort>(0).ToArray();
        }

        internal static void Restore(HBStyleTerrainData data, ushort[] held, RectInt shown)
        {
            if (!Ready(data))
                return;

            var levels = data.waterLevel.GetPixelData<ushort>(0);
            var width = data.waterLevel.width;

            if (held == null || held.Length != levels.Length || shown.width <= 0 || shown.height <= 0)
                return;

            for (var y = shown.yMin; y < shown.yMax; y++)
            {
                var first = (y * width + shown.xMin) * 2;

                NativeArray<ushort>.Copy(held, first, levels, first, shown.width * 2);
            }

            UploadLevel(data, levels, shown);
        }

        internal static void Forget()
        {
            s_SoilData = null;
        }

        internal static void Show(HBStyleTerrainData data, RectInt area)
        {
            UploadLevel(data, data.waterLevel.GetPixelData<ushort>(0), area);
        }

        static void UploadLevel(HBStyleTerrainData data, NativeArray<ushort> levels, RectInt area)
        {
            Upload(data.waterLevel, levels, area);
            data.WaterShown(area);
        }

        static void UploadFlow(HBStyleTerrainData data, NativeArray<ushort> flows, RectInt area)
        {
            Upload(data.waterFlow, flows, area);
            data.FlowEdited();
        }

        static void Upload<T>(Texture2D target, NativeArray<T> pixels, RectInt area) where T : struct
        {
            area.xMin = Mathf.Max(area.xMin, 0);
            area.yMin = Mathf.Max(area.yMin, 0);
            area.xMax = Mathf.Min(area.xMax, target.width);
            area.yMax = Mathf.Min(area.yMax, target.height);

            if (area.width <= 0 || area.height <= 0)
                return;

            if (!s_Regions.TryGetValue(target.format, out var region) || region == null ||
                region.width != area.width || region.height != area.height)
            {
                if (region != null)
                    Object.DestroyImmediate(region);

                region = new Texture2D(area.width, area.height, target.format, false, true)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                s_Regions[target.format] = region;
            }

            var across = pixels.Length / (target.width * target.height);
            var row = area.width * across;
            var part = new NativeArray<T>(row * area.height, Allocator.Temp);

            for (var y = 0; y < area.height; y++)
                NativeArray<T>.Copy(pixels, ((area.y + y) * target.width + area.x) * across, part, y * row, row);

            region.SetPixelData(part, 0);
            region.Apply(false, false);
            part.Dispose();
            Graphics.CopyTexture(region, 0, 0, 0, 0, area.width, area.height, target, 0, 0, area.x, area.y);
        }
    }

    enum StyleTerrainWaterPart
    {
        Level,
        Liquids,
        NoLiquid,
        Flow,
        Calm
    }
}
