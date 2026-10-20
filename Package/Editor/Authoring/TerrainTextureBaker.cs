using System.Threading.Tasks;
using UnityEngine;
using Noise = HiddenBull.UrpStyle.Editor.LiquidTextureBaker;

namespace HiddenBull.UrpStyle.Editor
{
    public enum TerrainTextureKind
    {
        Rock,
        Dirt,
        Sand,
        GrassGround,
        Gravel,
        Mud,
        ForestFloor,
        Cobblestone
    }

    [System.Flags]
    public enum TerrainLeafTypes
    {
        Oval = 1,
        Oak = 2,
        Maple = 4,
        Birch = 8,
        Needle = 16
    }

    public static class TerrainTextureBaker
    {
        const float NormalScale = 0.15f;
        const float GrainRelief = 0.15f;
        const float GrainTone = 0.3f;
        const float CavityGain = 4f;
        const float PainterlyGrain = 1f;
        const float PainterlyTone = 0.8f;
        const float PainterlyBlur = 40f;
        const float PainterlyRelief = 0.4f;
        const float BandEdge = 0.3f;
        const float PuddleLevel = 0.9f;
        const float PuddleEdge = 0.05f;
        const float ClayCrackWidth = 0.06f;
        const float ClayCurl = 0.35f;
        const int LeavesPerCell = 2;
        const float LeafRim = 0.25f;
        const float LeafRimBurn = 0.35f;
        const float LeafVeinLift = 0.06f;
        const float LeafVeinTone = 0.2f;
        const float LeafMidrib = 0.03f;
        const float LeafVeinSlant = 1.4f;
        const float LeafSideVeins = 8f;
        const float LeafSideWidth = 0.02f;
        const float LeafSideStrength = 0.7f;
        const float LeafStemEdge = 0.3f;
        const float LeafStemLength = 0.3f;
        const float LeafStemOverlap = 0.1f;
        const float LeafStemWidth = 0.03f;
        const float OvalWidth = 0.42f;
        const float OakWidth = 0.62f;
        const float OakLobes = 3.5f;
        const float BirchWidth = 0.5f;
        const float BirchTeeth = 10f;
        const float BirchToothDepth = 0.12f;
        const float NeedleWidth = 0.3f;
        const float MapleCentre = 0.1f;
        const float MaplePitch = 1.2f;
        const float MapleRadius = 0.95f;
        const float MapleBody = 0.5f;
        const float MapleSharpness = 1.1f;
        const float MapleTeeth = 5f;
        const float MapleToothDepth = 0.1f;
        const float TwigLength = 1.2f;
        const float TwigWidth = 0.05f;
        const float TwigLift = 2f;
        const float CobbleDome = 0.9f;
        const float CobbleShift = 0.2f;
        const float CobbleShrink = 0.08f;
        const float CobbleEdge = 0.02f;
        const float PaintedReach = 96f;
        const float PaintedGain = 6f;
        const float PaintedLift = 0.35f;
        const float RidgeGain = 6f;
        const float RidgeLift = 0.3f;
        const float CavityCool = 0.5f;

        static readonly Vector2 s_PaintedLight = new Vector2(-0.6f, 0.8f);
        static readonly Vector3 s_Warm = new Vector3(1.06f, 1f, 0.9f);
        static readonly Vector3 s_Cool = new Vector3(0.88f, 0.94f, 1.1f);

        public struct Result
        {
            public Color[] albedo;
            public Color[] normal;
            public Color[] mask;
            public float[] height;
        }

        struct Sample
        {
            public float height;
            public float tone;
            public float second;
            public float smooth;
        }

        public static Result Bake(TerrainTextureSettings s, int resolution)
        {
            var samples = new Sample[resolution * resolution];
            var patches = new float[samples.Length];
            var grains = new float[samples.Length];
            var grainAmount = s.grain * (1f - s.painterly * PainterlyGrain);

            Parallel.For(0, resolution, y =>
            {
                for (var x = 0; x < resolution; x++)
                {
                    var u = (x + 0.5f) / resolution;
                    var v = (y + 0.5f) / resolution;
                    var i = y * resolution + x;
                    var grain = Noise.Fbm(u, v, s.grainCount, 2, s.seed + 77) - 0.5f;

                    var sample = Surface(s, u, v, x, y);
                    sample.height += grain * grainAmount * GrainRelief;

                    samples[i] = sample;
                    grains[i] = grain;
                    patches[i] = Noise.Fbm(u, v, s.tonePatches, 3, s.seed + 101) - 0.5f;
                }
            });

            var height = new float[samples.Length];

            for (var i = 0; i < samples.Length; i++)
                height[i] = samples[i].height;

            Noise.Normalise(height);

            var around = Blur(height, resolution, Mathf.Max(1, resolution / 64));
            var broad = Blur(height, resolution, Mathf.Max(1, Mathf.RoundToInt(resolution / PaintedReach)));
            var relief = height;

            var softening = s.painterly > 0f ? s.painterlyNormal : 0f;

            if (softening > 0f)
            {
                var radius = Mathf.Max(1, Mathf.RoundToInt(resolution / PainterlyBlur * softening));
                var soft = Blur(height, resolution, radius);
                relief = new float[height.Length];

                for (var i = 0; i < height.Length; i++)
                    relief[i] = Mathf.Lerp(height[i], soft[i], softening);
            }

            var albedo = new Color[samples.Length];
            var mask = s.maskMap ? new Color[samples.Length] : null;

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var cavity = Mathf.Clamp01((around[i] - height[i]) * CavityGain);
                var mix = Mathf.Clamp01(Mathf.Lerp(sample.tone, height[i], s.heightTint));
                var tone = 1f + patches[i] * s.toneVariation * 1.6f + grains[i] * grainAmount * GrainTone;
                tone *= 1f - cavity * s.cavityDarkening;

                var gathered = Mathf.Clamp01(mix + (tone - 1f) * PainterlyTone);
                mix = Mathf.Lerp(mix, Band(gathered, s.toneBands), s.painterly);
                tone = Mathf.Lerp(tone, 1f, s.painterly);

                var shade = Color.Lerp(s.hollowColor, s.topColor, mix);

                shade = Color.Lerp(shade, s.secondColor, sample.second);
                shade = Paint(s, shade, broad, around[i], height[i], cavity, i, resolution);

                var smoothness = Mathf.Lerp(s.smoothness.x, s.smoothness.y, Mathf.Clamp01(sample.smooth));

                shade = new Color(Mathf.Clamp01(shade.r * tone), Mathf.Clamp01(shade.g * tone),
                                  Mathf.Clamp01(shade.b * tone), Mathf.Clamp01(smoothness));
                albedo[i] = shade;

                if (mask != null)
                    mask[i] = new Color(0f, 1f - cavity * s.occlusion, height[i], shade.a);
            }

            return new Result
            {
                albedo = albedo,
                normal = Noise.BuildNormals(relief, resolution,
                                           s.normalStrength * NormalScale * (1f - softening * PainterlyRelief)),
                mask = mask,
                height = height
            };
        }

        static Color Paint(TerrainTextureSettings s, Color colour, float[] broad, float around, float height,
                           float cavity, int index, int resolution)
        {
            var x = index % resolution;
            var y = index / resolution;
            var right = broad[y * resolution + Noise.Wrap(x + 1, resolution)];
            var left = broad[y * resolution + Noise.Wrap(x - 1, resolution)];
            var up = broad[Noise.Wrap(y + 1, resolution) * resolution + x];
            var down = broad[Noise.Wrap(y - 1, resolution) * resolution + x];
            var slope = new Vector2(right - left, up - down) * (resolution * 0.5f / PaintedReach);
            var lit = Mathf.Clamp(-Vector2.Dot(slope, s_PaintedLight) * PaintedGain, -1f, 1f) * s.paintedLight;
            var ridge = Mathf.Clamp01((height - around) * RidgeGain) * s.edgeHighlight;
            var warm = Mathf.Clamp01(lit) * s.warmCool;
            var cool = (Mathf.Clamp01(-lit) + cavity * CavityCool) * s.warmCool;

            var bright = 1f + lit * PaintedLift + ridge * RidgeLift;
            var hue = Vector3.Lerp(Vector3.one, s_Warm, warm);
            hue = Vector3.Scale(hue, Vector3.Lerp(Vector3.one, s_Cool, Mathf.Clamp01(cool)));

            return new Color(colour.r * bright * hue.x, colour.g * bright * hue.y, colour.b * bright * hue.z, colour.a);
        }

        static float Band(float value, int bands)
        {
            var scaled = value * Mathf.Max(bands, 1);
            var step = Mathf.Floor(scaled);
            var edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((scaled - step - 0.5f) / BandEdge + 0.5f));

            return Mathf.Clamp01((step + edge) / Mathf.Max(bands, 1));
        }

        static float[] Blur(float[] values, int resolution, int radius)
        {
            var across = new float[values.Length];
            var blurred = new float[values.Length];
            var weight = 1f / (radius * 2 + 1);

            Parallel.For(0, resolution, y =>
            {
                for (var x = 0; x < resolution; x++)
                {
                    var sum = 0f;

                    for (var k = -radius; k <= radius; k++)
                        sum += values[y * resolution + Noise.Wrap(x + k, resolution)];

                    across[y * resolution + x] = sum * weight;
                }
            });

            Parallel.For(0, resolution, y =>
            {
                for (var x = 0; x < resolution; x++)
                {
                    var sum = 0f;

                    for (var k = -radius; k <= radius; k++)
                        sum += across[Noise.Wrap(y + k, resolution) * resolution + x];

                    blurred[y * resolution + x] = sum * weight;
                }
            });

            return blurred;
        }

        static Sample Surface(TerrainTextureSettings s, float u, float v, int x, int y)
        {
            switch (s.kind)
            {
                case TerrainTextureKind.Rock: return Rock(s, u, v);
                case TerrainTextureKind.Dirt: return Dirt(s, u, v);
                case TerrainTextureKind.Sand: return Sand(s, u, v, x, y);
                case TerrainTextureKind.GrassGround: return GrassGround(s, u, v);
                case TerrainTextureKind.Mud: return Mud(s, u, v);
                case TerrainTextureKind.ForestFloor: return ForestFloor(s, u, v);
                case TerrainTextureKind.Cobblestone: return Cobblestone(s, u, v);
                default: return Gravel(s, u, v);
            }
        }

        static Sample Rock(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.plateCount, s.seed + 19, s.warp);
            Cells(u, v, s.plateCount, s.seed, out var first, out var second, out var cell);

            var grain = Noise.Fbm(u, v, s.plateCount * 2, s.detailLevels, s.seed + 7);
            var strata = Noise.Fbm(u, v, Mathf.Max(1, s.plateCount / 2), 4, s.seed + 13);
            var opening = Noise.Fbm(u, v, s.plateCount, 2, s.seed + 29);
            var open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((opening - (0.85f - s.crackCoverage)) / 0.3f));
            var line = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((second - first) / s.crackWidth));
            var crack = (1f - line) * open;

            return new Sample
            {
                height = (strata * s.strata + cell * s.plateStep + grain * 0.45f) * (1f - s.crackDepth * crack),
                tone = grain * 0.45f + strata * 0.3f + cell * 0.1f + (1f - crack) * 0.15f,
                second = crack,
                smooth = grain
            };
        }

        static Sample Dirt(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.clodCount, s.seed + 19, s.warp);

            var ground = Noise.Fbm(u, v, s.clodCount, s.detailLevels, s.seed);
            var crumbs = Noise.Fbm(u, v, s.clodCount * 8, 2, s.seed + 9);

            Cells(u, v, s.pebbleCount, s.seed + 3, out var first, out _, out var cell);

            var size = s.pebbleSize * (0.6f + 0.4f * Fraction(cell * 7.13f));
            var pebble = cell > 1f - s.pebbleAmount ? Noise.Dome(first, size) : 0f;

            return new Sample
            {
                height = ground * 0.7f + pebble * 0.45f + crumbs * s.crumbs,
                tone = ground * 0.7f + pebble * 0.3f,
                second = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pebble / 0.3f)),
                smooth = pebble
            };
        }

        static Sample Sand(TerrainTextureSettings s, float u, float v, int x, int y)
        {
            Warp(ref u, ref v, s.duneCount, s.seed + 19, s.warp);

            var dunes = Noise.Fbm(u, v, s.duneCount, s.detailLevels, s.seed);
            var drift = Noise.Fbm(u, v, s.duneCount * 2, 2, s.seed + 5);
            var phase = v * s.rippleCount + (dunes * 2f + drift * 0.6f) * s.rippleBend * 2f;
            var r = phase - Mathf.Floor(phase);
            var crest = Mathf.Lerp(0.5f, 0.95f, s.rippleSharpness);
            var ripple = r < crest ? r / crest : (1f - r) / (1f - crest);
            var speck = Noise.Hash(x, y, s.seed + 17) < s.speckle * 0.2f ? 1f : 0f;

            ripple = ripple * ripple * (3f - 2f * ripple);

            return new Sample
            {
                height = ripple * 0.55f + dunes * s.duneAmount,
                tone = dunes * 0.6f + ripple * 0.3f,
                second = speck,
                smooth = ripple * 0.3f
            };
        }

        static Sample GrassGround(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.patchCount, s.seed + 19, s.warp);

            var patches = Noise.Fbm(u, v, s.patchCount, s.detailLevels, s.seed);
            var fibres = Noise.Fbm(u, v, s.tuftCount + s.tuftCount / 3, 2, s.seed + 11);

            Cells(u, v, s.tuftCount, s.seed + 5, out var first, out _, out var cell);

            var tuft = Noise.Dome(first, 0.55f) * (0.4f + 0.6f * cell);
            var edge = 0.02f + s.soilSoftness * 0.46f;
            var soil = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((patches - s.soilAmount * 0.6f) / edge));

            return new Sample
            {
                height = patches * 0.35f + tuft * s.tuftHeight * (1f - soil) + fibres * 0.2f,
                tone = fibres * 0.4f + tuft * 0.4f + cell * 0.2f,
                second = soil,
                smooth = tuft
            };
        }

        static Sample Gravel(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.stoneCount, s.seed + 19, s.warp);
            Cells(u, v, s.stoneCount, s.seed, out var first, out var second, out var cell);

            var edge = 1f - Mathf.Clamp01((second - first) / 0.6f);
            var dome = 1f - Mathf.Pow(edge, 1f + s.stoneRoundness * 2.5f);
            var stone = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((second - first) / s.gapWidth));
            var lift = 1f - s.stoneHeightVariation + s.stoneHeightVariation * cell;

            return new Sample
            {
                height = dome * lift * stone,
                tone = cell * 0.7f + dome * 0.3f,
                second = 1f - stone,
                smooth = stone
            };
        }

        static Sample Mud(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.mudCount, s.seed + 19, s.warp);

            var ground = Noise.Fbm(u, v, s.mudCount, s.detailLevels, s.seed);
            var water = s.puddles * PuddleLevel;
            var puddle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((water - ground) / PuddleEdge));
            var crack = 0f;
            var curl = 0f;

            if (s.cracks > 0f)
            {
                Cells(u, v, s.crackCells, s.seed + 3, out var first, out var second, out _);

                var gap = second - first;
                crack = (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(gap / ClayCrackWidth))) * s.cracks;
                curl = (1f - Mathf.Clamp01(gap / ClayCurl)) * s.cracks;
            }

            return new Sample
            {
                height = Mathf.Lerp(ground, water, puddle) + curl * 0.3f - crack * 0.5f,
                tone = ground * 0.7f + (1f - crack) * 0.3f,
                second = puddle,
                smooth = 0.3f + puddle * 0.7f
            };
        }

        static Sample ForestFloor(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.leafCount, s.seed + 19, s.warp);

            var count = s.leafCount;
            var x = u * count;
            var y = v * count;
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);
            var soil = Noise.Fbm(u, v, Mathf.Max(1, count / 8), 3, s.seed + 29);
            var sample = new Sample { height = soil * 0.2f, tone = soil * 0.3f, smooth = 0.05f };
            var top = -1f;

            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    var cx = ix + ox;
                    var cy = iy + oy;
                    var wx = Noise.Wrap(cx, count);
                    var wy = Noise.Wrap(cy, count);

                    for (var leaf = 0; leaf < LeavesPerCell; leaf++)
                        Leaf(s, x, y, cx, cy, wx, wy, s.seed + leaf * 13, ref top, ref sample);

                    if (Noise.Hash(wx, wy, s.seed + 40) >= s.twigs * 0.5f)
                        continue;

                    var start = new Vector2(cx + Noise.Hash(wx, wy, s.seed + 41), cy + Noise.Hash(wx, wy, s.seed + 42));
                    var turn = Noise.Hash(wx, wy, s.seed + 43) * Mathf.PI * 2f;
                    var end = start + new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * TwigLength;
                    var away = Segment(new Vector2(x, y), start, end);

                    if (away >= TwigWidth || TwigLift <= top)
                        continue;

                    top = TwigLift;
                    sample.height = 0.9f - away / TwigWidth * 0.2f;
                    sample.tone = 0.05f;
                    sample.second = 0f;
                    sample.smooth = 0.1f;
                }
            }

            return sample;
        }

        static void Leaf(TerrainTextureSettings s, float x, float y, int cx, int cy, int wx, int wy, int salt,
                         ref float top, ref Sample sample)
        {
            var z = Noise.Hash(wx, wy, salt + 4);

            if (z <= top)
                return;

            var px = cx + Noise.Hash(wx, wy, salt);
            var py = cy + Noise.Hash(wx, wy, salt + 1);
            var angle = Noise.Hash(wx, wy, salt + 2) * Mathf.PI * 2f;
            var length = s.leafSize * (0.6f + 0.4f * Noise.Hash(wx, wy, salt + 3)) * 0.5f;
            var type = PickLeaf(s.leafTypes, Noise.Hash(wx, wy, salt + 7));

            var dx = x - px;
            var dy = y - py;
            var along = (dx * Mathf.Cos(angle) + dy * Mathf.Sin(angle)) / length;
            var across = (-dx * Mathf.Sin(angle) + dy * Mathf.Cos(angle)) / length;

            if (!LeafShape(type, s.leafShape, along, Mathf.Abs(across), out var edge, out var vein))
                return;

            var veins = vein * s.leafVeins;
            var rim = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / LeafRim));
            var curl = 0.55f + 0.45f * (1f - edge) * (1f - edge);
            var profile = Mathf.Lerp(Mathf.Sqrt(edge), curl, s.leafCurl);

            top = z;
            sample.height = profile * 0.6f + z * 0.4f + 0.2f + veins * LeafVeinLift;
            sample.tone = Mathf.Clamp01(Noise.Hash(wx, wy, salt + 5) * (1f - rim * LeafRimBurn) + veins * LeafVeinTone);
            sample.second = Noise.Hash(wx, wy, salt + 6) > 0.5f ? 1f : 0f;
            sample.smooth = 0.3f;
        }

        static TerrainLeafTypes PickLeaf(TerrainLeafTypes types, float pick)
        {
            var count = 0;

            for (var bit = 1; bit <= (int)TerrainLeafTypes.Needle; bit <<= 1)
            {
                if (((int)types & bit) != 0)
                    count++;
            }

            if (count == 0)
                return TerrainLeafTypes.Oval;

            var chosen = Mathf.Min(Mathf.FloorToInt(pick * count), count - 1);

            for (var bit = 1; bit <= (int)TerrainLeafTypes.Needle; bit <<= 1)
            {
                if (((int)types & bit) == 0)
                    continue;

                if (chosen == 0)
                    return (TerrainLeafTypes)bit;

                chosen--;
            }

            return TerrainLeafTypes.Oval;
        }

        static bool LeafShape(TerrainLeafTypes type, float slender, float along, float across, out float edge,
                              out float vein)
        {
            edge = 0f;
            vein = 0f;

            if (type == TerrainLeafTypes.Maple)
                return Maple(slender, along, across, out edge, out vein);

            if (type != TerrainLeafTypes.Needle && Stem(along, across, -1f))
            {
                edge = LeafStemEdge;
                vein = 1f;
                return true;
            }

            if (along <= -1f || along >= 1f)
                return false;

            var round = 1f - along * along;
            float half;

            switch (type)
            {
                case TerrainLeafTypes.Needle:
                    half = NeedleWidth * (1f - slender * 0.85f) * Mathf.Sqrt(round);
                    break;
                case TerrainLeafTypes.Oak:
                    var lobes = Mathf.Abs(Mathf.Cos(Mathf.PI * OakLobes * (along + 1f) * 0.5f));
                    half = OakWidth * (1f - slender * 0.5f) * Mathf.Sqrt(round) * (1f + 0.2f * along) *
                           (0.45f + 0.55f * lobes);
                    break;
                case TerrainLeafTypes.Birch:
                    var tooth = Fraction((along + 1f) * BirchTeeth);
                    half = BirchWidth * (1f - slender * 0.6f) * round * (1f - 0.45f * along) *
                           (1f - BirchToothDepth * tooth);
                    break;
                default:
                    half = OvalWidth * (1f - slender * 0.75f) * round * (1f - 0.25f * along);
                    break;
            }

            if (across >= half)
                return false;

            edge = 1f - across / half;

            if (type == TerrainLeafTypes.Needle)
                return true;

            var midrib = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(across / LeafMidrib));
            var slant = (along - across * LeafVeinSlant) * LeafSideVeins * 0.5f;
            var line = Mathf.Abs(Fraction(slant) - 0.5f) * 2f / LeafSideVeins;
            var side = (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(line / LeafSideWidth))) *
                       Mathf.Clamp01(edge / LeafRim) * LeafSideStrength;

            vein = Mathf.Max(midrib, side);
            return true;
        }

        static bool Maple(float slender, float along, float across, out float edge, out float vein)
        {
            edge = 0f;
            vein = 0f;

            if (Stem(along, across, -MapleCentre))
            {
                edge = LeafStemEdge;
                vein = 1f;
                return true;
            }

            var up = along + MapleCentre;
            var reach = Mathf.Sqrt(up * up + across * across);
            var turn = Mathf.Atan2(across, up);
            var lobe = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(turn * Mathf.PI * 2f / MaplePitch), MapleSharpness);
            var teeth = 1f - MapleToothDepth * Fraction(turn * MapleTeeth);
            var radius = MapleRadius * (1f - slender * 0.4f) * (MapleBody + (1f - MapleBody) * lobe) * teeth *
                         (1f - 0.25f * turn / Mathf.PI);

            if (reach >= radius)
                return false;

            edge = 1f - reach / radius;

            var nearest = Mathf.Round(turn / MaplePitch) * MaplePitch;
            var off = Mathf.Abs(turn - nearest) * reach;

            vein = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(off / LeafMidrib));
            return true;
        }

        static bool Stem(float along, float across, float start)
        {
            return along < start + LeafStemOverlap && along > start - LeafStemLength && across < LeafStemWidth;
        }

        static Sample Cobblestone(TerrainTextureSettings s, float u, float v)
        {
            Warp(ref u, ref v, s.cobbleRows, s.seed + 19, s.warp);

            var rows = Mathf.Max(2, s.cobbleRows - s.cobbleRows % 2);
            var columns = Mathf.Max(1, Mathf.RoundToInt(rows / Mathf.Max(s.cobbleAspect, 1f)));
            var y = v * rows;
            var row = Mathf.FloorToInt(y);
            var x = u * columns + (Noise.Wrap(row, 2) == 1 ? 0.5f : 0f);
            var column = Mathf.FloorToInt(x);
            var wr = Noise.Wrap(row, rows);
            var wc = Noise.Wrap(column, columns);
            var stretch = (float)rows / columns;

            var jitterX = (Noise.Hash(wc, wr, s.seed) - 0.5f) * s.cobbleJitter * CobbleShift * stretch;
            var jitterY = (Noise.Hash(wc, wr, s.seed + 1) - 0.5f) * s.cobbleJitter * CobbleShift;
            var shrink = Noise.Hash(wc, wr, s.seed + 2) * s.cobbleJitter * CobbleShrink;
            var local = new Vector2((x - column - 0.5f) * stretch - jitterX, y - row - 0.5f - jitterY);
            var margin = s.mortarWidth * 0.5f + shrink;
            var half = new Vector2(0.5f * stretch - margin - Mathf.Abs(jitterX), 0.5f - margin - Mathf.Abs(jitterY));
            var corner = s.cobbleRoundness * Mathf.Min(half.x, half.y) * 0.8f;
            var q = new Vector2(Mathf.Abs(local.x), Mathf.Abs(local.y)) - half + Vector2.one * corner;
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            var inside = -(outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - corner);

            var edge = Mathf.Clamp01(inside / (Mathf.Min(half.x, half.y) * CobbleDome));
            var bevel = 1f - Mathf.Pow(1f - edge, 1f + s.cobbleRoundness * 2f);
            var round = Mathf.Sqrt(Mathf.Clamp01(1f - (1f - edge) * (1f - edge)));
            var dome = Mathf.Lerp(bevel, round, s.cobbleRoundness);
            var stone = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(inside / CobbleEdge));

            return new Sample
            {
                height = dome * (0.7f + 0.3f * Noise.Hash(wc, wr, s.seed + 3)) * stone,
                tone = Noise.Hash(wc, wr, s.seed + 4) * 0.7f + dome * 0.3f,
                second = 1f - stone,
                smooth = stone * 0.6f
            };
        }

        static float Segment(Vector2 point, Vector2 start, Vector2 end)
        {
            var line = end - start;
            var t = Mathf.Clamp01(Vector2.Dot(point - start, line) / Mathf.Max(line.sqrMagnitude, 1e-6f));
            return (point - (start + line * t)).magnitude;
        }

        static float Fraction(float value)
        {
            return value - Mathf.Floor(value);
        }

        static void Warp(ref float u, ref float v, int period, int seed, float amount)
        {
            if (amount <= 0f)
                return;

            var reach = amount / period;
            var du = Noise.Fbm(u, v, period, 3, seed) - 0.5f;
            var dv = Noise.Fbm(u, v, period, 3, seed + 41) - 0.5f;

            u += du * reach;
            v += dv * reach;
        }

        static void Cells(float u, float v, int period, int seed, out float first, out float second, out float cell)
        {
            var x = u * period;
            var y = v * period;
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);

            first = float.MaxValue;
            second = float.MaxValue;
            cell = 0f;

            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    var cx = ix + ox;
                    var cy = iy + oy;
                    var wx = Noise.Wrap(cx, period);
                    var wy = Noise.Wrap(cy, period);

                    var px = cx + Noise.Hash(wx, wy, seed) * 0.8f + 0.1f;
                    var py = cy + Noise.Hash(wx, wy, seed + 1) * 0.8f + 0.1f;

                    var distance = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));

                    if (distance < first)
                    {
                        second = first;
                        first = distance;
                        cell = Noise.Hash(wx, wy, seed + 2);
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }
        }
    }
}
