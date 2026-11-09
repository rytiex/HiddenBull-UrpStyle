using UnityEngine;

namespace HiddenBull.UrpStyle
{
    public static class HBStyleLiquid
    {
        const int Passes = 3;
        const int Slots = StyleLiquidLook.Slots;
        const int Bands = StyleLiquidWaves.Bands;
        const int Count = StyleLiquidWaves.Count;
        const float NormalSpan = 0.5f;
        const float Buried = 0.5f;
        const float Above = 2f;
        const float SlopeSpan = 1f;
        const float ShareLeast = 1e-3f;
        const float LengthLeast = 0.05f;
        const float FoldLimit = 1f;
        const float ShoalDepth = 8f;
        const float Trough = 3f;
        const float ShoreShare = 0.7f;
        const float ShoreGain = 1.6f;
        const float ShoreDepth = 10f;
        const float ShoreLeast = 1e-3f;
        const float ShoreScatter = 0.137f;
        const int ShoreBand = 1;
        const float StreamTurn = 1f;
        const float HeadingSure = 0.25f;
        const float LevelZero = 32768f;
        const float LevelSpan = 32767f;
        const int CutFrom = 32768;

        static readonly Vector3 s_PoolShare = new Vector3(0f, 0.2f, 1f);
        static readonly Vector3 s_FullShare = Vector3.one;
        static readonly float[] s_Phases = new float[Bands * Count];
        static readonly Vector4[] s_SlotWaves = new Vector4[Slots];

        static double s_PhaseTime = double.NaN;
        static bool s_Synced;
        static double s_SyncTime;
        static double s_SyncAt;
        static float s_SyncWind;
        static float s_SyncHeading;
        static Vector2 s_Frame;

        struct Source
        {
            public HBStyleTerrain terrain;
            public HBStyleTerrainData data;
            public HBStyleLiquidPool pool;
            public Vector3 origin;
            public StyleLiquidCalm calm;
        }

        struct Water
        {
            public float level;
            public bool wet;
            public Vector4 mix;
            public Vector3 flow;
        }

        struct Shape
        {
            public float amount;
            public float height;
            public float steepness;
            public Vector4 mix;
        }

        struct Sea
        {
            public float wind;
            public float shore;
        }

        public static bool any
        {
            get
            {
                if (HBStyleLiquidPool.any)
                    return true;

                foreach (var terrain in HBStyleTerrain.active)
                {
                    if (terrain != null && terrain.data != null && terrain.data.liquidOn)
                        return true;
                }

                return false;
            }
        }

        public static bool synced => s_Synced;

        public static double time =>
            s_Synced ? s_SyncTime + (StyleEnvironment.clock - s_SyncAt) : StyleEnvironment.clock;

        public static float wind => s_Synced ? s_SyncWind : Mathf.Clamp01(StyleEnvironment.weather.z);

        public static float windHeading => s_Synced ? s_SyncHeading : StyleEnvironment.windHeading;

        public static void Sync(double time, float wind, float windHeading)
        {
            s_Synced = true;
            s_SyncTime = time;
            s_SyncAt = StyleEnvironment.clock;
            s_SyncWind = Mathf.Clamp01(wind);
            s_SyncHeading = windHeading;
        }

        public static void Unsync()
        {
            s_Synced = false;
        }

        public static bool TryGetHeight(Vector3 position, out float height, bool waves = true)
        {
            return TryGetHeight(position, out height, out _, waves);
        }

        public static bool TryGetHeight(Vector3 position, out float height, out Component liquid, bool waves = true)
        {
            return Find(position, true, waves, out height, out liquid);
        }

        public static bool TryGetSurface(Vector3 position, out float height, out Vector3 normal, bool waves = true)
        {
            return TryGetSurface(position, out height, out normal, out _, waves);
        }

        public static bool TryGetSurface(Vector3 position, out float height, out Vector3 normal,
                                         out Component liquid, bool waves = true)
        {
            normal = Vector3.up;

            if (!TryGetHeight(position, out height, out liquid, waves) || !TrySource(liquid, out var source))
                return false;

            var here = new Vector2(position.x, position.z);

            if (Surface(source, here + new Vector2(NormalSpan, 0f), waves, out var east, out _) &&
                Surface(source, here + new Vector2(0f, NormalSpan), waves, out var north, out _))
                normal = new Vector3(height - east, NormalSpan, height - north).normalized;

            return true;
        }

        public static bool TryGetDepth(Vector3 position, out float depth, bool waves = true)
        {
            return TryGetDepth(position, out depth, out _, waves);
        }

        public static bool TryGetDepth(Vector3 position, out float depth, out Component liquid, bool waves = true)
        {
            depth = 0f;

            if (!TryGetHeight(position, out var height, out liquid, waves) || !TrySource(liquid, out var source))
                return false;

            if (source.pool != null)
            {
                depth = Mathf.Max(height - source.pool.bottom, 0f);
                return true;
            }

            depth = Ground(source, new Vector2(position.x, position.z), out var seabed)
                ? Mathf.Max(height - seabed, 0f)
                : float.PositiveInfinity;

            return true;
        }

        internal static bool TryGetStill(Component liquid, Vector2 position, out float level, out Vector4 mix)
        {
            level = 0f;
            mix = Vector4.zero;

            if (!TrySource(liquid, out var source))
                return false;

            var water = WaterAt(source, position);

            level = water.level;
            mix = water.mix;
            return water.wet && (!Ground(source, position, out var ground) || ground < level);
        }

        internal static bool TryGetSubmerged(Vector3 position, out float height, out Component liquid)
        {
            return Find(position, false, true, out height, out liquid);
        }

        internal static bool TryGetView(Vector3 position, out float height, out Vector4 mix, out Vector2 slope,
                                         out Component liquid)
        {
            mix = Vector4.zero;
            slope = Vector2.zero;

            if (!Find(position, false, true, out height, out liquid) || !TrySource(liquid, out var source))
                return false;

            var here = new Vector2(position.x, position.z);

            mix = WaterAt(source, here).mix;

            if (source.pool != null)
                return true;

            var across = Vector2.right * SlopeSpan;
            var along = Vector2.up * SlopeSpan;

            slope.x = (WaterAt(source, here + across).level - WaterAt(source, here - across).level) / (SlopeSpan * 2f);
            slope.y = (WaterAt(source, here + along).level - WaterAt(source, here - along).level) / (SlopeSpan * 2f);
            return true;
        }

        internal static void BindLook(Component liquid, MaterialPropertyBlock block)
        {
            if (liquid is HBStyleLiquidPool pool)
                pool.BindLook(block);
            else if (liquid is HBStyleTerrain terrain)
                terrain.BindLiquidLook(block);
        }

        static bool Find(Vector3 position, bool over, bool waves, out float height, out Component liquid)
        {
            height = 0f;
            liquid = null;

            Refresh();

            var here = new Vector2(position.x, position.z);
            var nearest = float.MaxValue;

            foreach (var terrain in HBStyleTerrain.active)
            {
                if (!TrySource(terrain, out var source) || !Surface(source, here, waves, out var surface, out _))
                    continue;

                if (over && Cut(terrain, new Vector3(position.x, surface, position.z)) || Cut(terrain, position))
                    continue;

                if (Ground(source, here, out var seabed) && (seabed >= surface || position.y < seabed - Buried))
                    continue;

                var apart = Mathf.Abs(surface - position.y);

                if (apart >= nearest)
                    continue;

                nearest = apart;
                height = surface;
                liquid = terrain;
            }

            var owned = false;
            var smallest = float.MaxValue;

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool == null || position.y < pool.bottom || !TrySource(pool, out var source) ||
                    !Surface(source, here, waves, out var surface, out _))
                    continue;

                if (over && Cut(pool, new Vector3(position.x, surface, position.z)) || Cut(pool, position))
                    continue;

                var size = pool.half;
                var area = size.x * size.y;

                if (position.y <= surface + Above)
                {
                    if (owned && area >= smallest)
                        continue;

                    owned = true;
                    smallest = area;
                    height = surface;
                    liquid = pool;
                    continue;
                }

                var apart = Mathf.Abs(surface - position.y);

                if (owned || apart >= nearest)
                    continue;

                nearest = apart;
                height = surface;
                liquid = pool;
            }

            return liquid != null;
        }

        static bool Cut(Component liquid, Vector3 point)
        {
            var keeps = false;
            var kept = false;
            foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
            {
                if (cutter == null || !Reaches(cutter, liquid))
                    continue;

                var inside = cutter.Contains(point);

                if (cutter.mode != HBStyleLiquidCutMode.KeepInside)
                {
                    if (inside)
                        return true;

                    continue;
                }

                keeps = true;
                kept |= inside;
            }

            return keeps && !kept;
        }

        static bool Reaches(HBStyleLiquidCutter cutter, Component liquid)
        {
            if (liquid is HBStyleLiquidPool pool)
                return cutter.Reaches(pool, pool.bounds);

            return liquid is HBStyleTerrain terrain && cutter.Affects(HBStyleLiquidCutTargets.Terrain) &&
                   cutter.Spans(out var volume) && terrain.LiquidTouches(volume);
        }

        static bool TrySource(Component liquid, out Source source)
        {
            source = default;

            if (liquid is HBStyleLiquidPool pool)
            {
                if (pool == null || !pool.isActiveAndEnabled || pool.layer == null)
                    return false;

                source.pool = pool;
                return true;
            }

            if (!(liquid is HBStyleTerrain terrain) || terrain == null || !terrain.isActiveAndEnabled)
                return false;

            var data = terrain.data;

            if (data == null || !data.liquidOn || data.terrainData == null)
                return false;

            source.terrain = terrain;
            source.data = data;
            source.origin = terrain.transform.position;
            source.calm = terrain.liquidCalm;
            return true;
        }

        static bool Surface(in Source source, Vector2 position, bool waves, out float height, out bool wet)
        {
            var rest = position;
            var moved = Vector3.zero;
            var water = WaterAt(source, rest);

            if (waves)
            {
                for (var pass = 0; pass < Passes; pass++)
                {
                    moved = Offset(source, rest, water);
                    rest = position - new Vector2(moved.x, moved.z);
                    water = WaterAt(source, rest);
                }

                moved = Offset(source, rest, water);
            }

            height = water.level + moved.y;
            wet = water.wet;
            return wet;
        }

        static Vector3 Offset(in Source source, Vector2 rest, in Water water)
        {
            var shape = ShapeOf(source, water.mix);

            if (shape.amount <= 0f)
                return Vector3.zero;

            var sea = SeaAt(source, rest, shape, water);
            var windShare = shape.amount * sea.wind;
            var shoreShare = shape.amount * sea.shore;
            var moved = Vector3.zero;

            if (windShare > 0f)
                moved += WindOffset(source, rest, shape) * windShare;

            if (shoreShare > ShoreLeast && source.calm != null && source.calm.readable)
                moved += TileOffset(source, rest) * shoreShare;

            var push = Mathf.Min(shape.height * shape.steepness, FoldLimit / Mathf.Max(shape.amount * sea.wind, 1e-3f));

            return new Vector3(moved.x * push, moved.y * shape.height, moved.z * push);
        }

        static Water WaterAt(in Source source, Vector2 position)
        {
            var water = new Water();

            if (source.pool != null)
            {
                water.level = source.pool.level;
                water.wet = source.pool.Inward(position) >= 0f;
                water.mix = new Vector4(1f, 0f, 0f, 0f);
                return water;
            }

            var data = source.data;
            var size = data.size;
            var uv = new Vector2((position.x - source.origin.x) / Mathf.Max(size.x, 1e-3f),
                                 (position.y - source.origin.z) / Mathf.Max(size.z, 1e-3f));
            var inside = uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
            var ready = data.waterFormat == HBStyleTerrainData.WaterFormat && Readable(data.waterLevel) &&
                        Readable(data.waterMix) && Readable(data.waterFlow);
            var offset = 0f;
            var cut = false;
            var mix = Vector4.zero;

            if (ready && inside)
            {
                offset = (LevelAt(data.waterLevel, uv) - LevelZero) / LevelSpan * Mathf.Max(size.y, 0.01f);
                cut = CutAt(data.waterLevel, uv);
                mix = MixAt(data.waterMix, uv);
                water.flow = FlowAt(data.waterFlow, uv);
            }

            var rest = Mathf.Clamp01(1f - (mix.x + mix.y + mix.z + mix.w));

            mix[data.seaSlot] += rest;
            water.level = source.origin.y + data.seaLevel + offset;
            water.wet = !inside || !cut;
            water.mix = mix;
            return water;
        }

        static Shape ShapeOf(in Source source, Vector4 mix)
        {
            var shape = new Shape { mix = mix };
            var total = 0f;

            for (var slot = 0; slot < Slots; slot++)
            {
                var share = mix[slot];
                var wave = SlotWave(source, slot);

                shape.amount += (wave.x < 0f ? wind : wave.x) * share;
                shape.height += wave.y * share;
                shape.steepness += wave.z * share;
                s_SlotWaves[slot] = wave;
                total += share;
            }

            var scale = 1f / Mathf.Max(total, 1e-3f);

            shape.amount *= scale;
            shape.height *= scale;
            shape.steepness *= scale;
            return shape;
        }

        static Vector4 SlotWave(in Source source, int slot)
        {
            HBStyleLiquidLayer layer;

            if (source.pool != null)
                layer = slot == 0 ? source.pool.layer : null;
            else
                layer = slot < source.data.liquidCount ? source.data.Liquid(slot) : null;

            if (layer == null)
                return new Vector4(0f, 0f, 0f, 1f);

            return new Vector4(layer.windWaves ? -1f : layer.waves, layer.waveHeight, layer.waveSteepness,
                               layer.waveLength);
        }

        static Sea SeaAt(in Source source, Vector2 position, in Shape shape, in Water water)
        {
            var length = 0f;
            var total = 0f;

            for (var slot = 0; slot < Slots; slot++)
            {
                length += s_SlotWaves[slot].w * shape.mix[slot];
                total += shape.mix[slot];
            }

            length = Mathf.Max(length / Mathf.Max(total, 1e-3f), LengthLeast);

            var size = shape.height * length;
            var reach = ShoalDepth * Mathf.Max(size, 1f);
            var trough = Trough * shape.amount * size;
            var sea = new Sea();

            if (source.pool != null)
            {
                var pool = source.pool;
                var deep = Mathf.Max(pool.level - pool.bottom, 0f);
                var rim = Mathf.Clamp01(pool.Inward(position) / Mathf.Max(pool.edge, 1e-3f));

                sea.wind = Mathf.Min(Mathf.Clamp01(deep / reach), deep / Mathf.Max(trough, 1e-3f)) * rim;
                return sea;
            }

            var calm = new Vector4(reach, 0f, 0f, 0f);

            if (source.calm != null && source.calm.readable)
            {
                var extent = source.data.size;
                var uv = new Vector2((position.x - source.origin.x) / Mathf.Max(extent.x, 1e-3f),
                                     (position.y - source.origin.z) / Mathf.Max(extent.z, 1e-3f));
                var clamped = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                var outside = Vector2.Scale(uv - clamped, new Vector2(extent.x, extent.z));

                calm = source.calm.Calm(uv);
                calm.x += outside.magnitude * StyleLiquidCalm.Slope;
            }

            var open = Mathf.Clamp01(calm.x / reach);
            var shoal = Mathf.Min(open, calm.x / Mathf.Max(trough, 1e-3f)) * (1f - Mathf.Clamp01(water.flow.z));
            var close = 1f - Mathf.Clamp01(calm.x / ShoreDepth);
            var rush = Mathf.Clamp01(new Vector2(water.flow.x, water.flow.y).magnitude / StreamTurn);
            var turn = Mathf.Max(close * close * (3f - 2f * close) * calm.w, rush);

            sea.wind = shoal * (1f - turn * ShoreShare);
            sea.shore = shoal * turn * ShoreGain;
            return sea;
        }

        static Vector3 WindOffset(in Source source, Vector2 rest, in Shape shape)
        {
            var moved = Vector3.zero;
            var total = 0f;
            var shares = source.pool != null ? s_PoolShare : s_FullShare;
            var mirror = source.pool != null;

            for (var slot = 0; slot < Slots; slot++)
            {
                var share = shape.mix[slot];

                if (share <= ShareLeast)
                    continue;

                moved += WindOffsetAt(rest, Mathf.Max(s_SlotWaves[slot].w, LengthLeast), shares, mirror) * share;
                total += share;
            }

            return moved / Mathf.Max(total, ShareLeast);
        }

        static Vector3 WindOffsetAt(Vector2 rest, float wide, Vector3 shares, bool mirror)
        {
            var framed = Framed(rest / wide);
            var moved = Vector3.zero;

            for (var band = 0; band < Bands; band++)
            {
                if (shares[band] <= 0f)
                    continue;

                var ahead = Band(band, framed);

                if (mirror)
                {
                    var back = Band(band, -framed);

                    ahead = (ahead + new Vector3(-back.x, back.y, -back.z)) * 0.5f;
                }

                moved += ahead * shares[band];
            }

            var pushed = World(new Vector2(moved.x, moved.z));

            return new Vector3(pushed.x, moved.y, pushed.y) * wide;
        }

        static Vector3 TileOffset(in Source source, Vector2 rest)
        {
            var calm = source.calm;
            var origin = new Vector2(source.origin.x, source.origin.z);
            var grid = (rest - origin) / StyleLiquidCalm.Tile - new Vector2(0.5f, 0.5f);
            var base0 = new Vector2(Mathf.Floor(grid.x), Mathf.Floor(grid.y));
            var blend = grid - base0;
            var patch = StyleLiquidWaves.Patch(ShoreBand);
            var total = Vector3.zero;

            blend = new Vector2(blend.x * blend.x * (3f - 2f * blend.x), blend.y * blend.y * (3f - 2f * blend.y));

            for (var corner = 0; corner < 4; corner++)
            {
                var tile = new Vector2(corner & 1, corner >> 1);
                var centre = origin + (base0 + tile + new Vector2(0.5f, 0.5f)) * StyleLiquidCalm.Tile;
                var heading = calm.TileHeading((int)(base0.x + tile.x), (int)(base0.y + tile.y));

                if (heading.sqrMagnitude <= HeadingSure)
                    heading = s_Frame;

                var side = new Vector2(-heading.y, heading.x);
                var share = new Vector2(Mathf.Lerp(1f - blend.x, blend.x, tile.x),
                                        Mathf.Lerp(1f - blend.y, blend.y, tile.y));
                var local = rest - centre;
                var scatter = centre * ShoreScatter;
                var at = new Vector2(Vector2.Dot(local, heading), Vector2.Dot(local, side)) +
                         new Vector2(scatter.x - Mathf.Floor(scatter.x), scatter.y - Mathf.Floor(scatter.y)) * patch;
                var moved = Band(ShoreBand, at);
                var pushed = heading * moved.x + side * moved.z;

                total += new Vector3(pushed.x, moved.y, pushed.y) * (share.x * share.y);
            }

            return total;
        }

        static Vector3 Band(int band, Vector2 position)
        {
            var moved = Vector3.zero;

            for (var index = 0; index < Count; index++)
            {
                var slot = band * Count + index;
                var wave = StyleLiquidWaves.Wave(slot);
                var number = new Vector2(wave.x, wave.y);
                var length = Mathf.Max(number.magnitude, 1e-5f);
                var direction = number / length;
                var angle = Vector2.Dot(number, position) + s_Phases[slot];
                var push = wave.z * StyleLiquidWaves.Chop * Mathf.Cos(angle);

                moved.y += wave.z * Mathf.Sin(angle);
                moved.x += direction.x * push;
                moved.z += direction.y * push;
            }

            return moved;
        }

        static Vector2 Framed(Vector2 position)
        {
            return new Vector2(Vector2.Dot(position, s_Frame),
                               Vector2.Dot(position, new Vector2(-s_Frame.y, s_Frame.x)));
        }

        static Vector2 World(Vector2 framed)
        {
            return s_Frame * framed.x + new Vector2(-s_Frame.y, s_Frame.x) * framed.y;
        }

        static bool Ground(in Source source, Vector2 position, out float height)
        {
            height = 0f;

            if (source.pool != null)
            {
                height = source.pool.bottom;
                return true;
            }

            var size = source.data.size;
            var u = (position.x - source.origin.x) / Mathf.Max(size.x, 1e-3f);
            var v = (position.y - source.origin.z) / Mathf.Max(size.z, 1e-3f);

            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return false;

            var terrainData = source.data.terrainData;
            var holes = terrainData.holesResolution;

            if (holes > 0 && terrainData.IsHole(Mathf.Clamp((int)(u * holes), 0, holes - 1),
                                                   Mathf.Clamp((int)(v * holes), 0, holes - 1)))
                return false;

            height = source.data.terrainData.GetInterpolatedHeight(u, v) + source.origin.y;
            return true;
        }

        static void Refresh()
        {
            var heading = windHeading * Mathf.Deg2Rad;

            s_Frame = new Vector2(Mathf.Sin(heading), Mathf.Cos(heading));

            var now = time;

            if (now == s_PhaseTime)
                return;

            s_PhaseTime = now;

            for (var slot = 0; slot < s_Phases.Length; slot++)
                s_Phases[slot] = StyleLiquidWaves.Phase(slot, now);
        }

        static bool Readable(Texture2D texture)
        {
            return texture != null && texture.isReadable && texture.width > 0 && texture.height > 0;
        }

        static void Texel(Texture2D texture, Vector2 uv, out int x0, out int y0, out int x1, out int y1,
                          out float fx, out float fy)
        {
            var x = Mathf.Clamp01(uv.x) * texture.width - 0.5f;
            var y = Mathf.Clamp01(uv.y) * texture.height - 0.5f;

            x0 = Mathf.FloorToInt(x);
            y0 = Mathf.FloorToInt(y);
            fx = x - x0;
            fy = y - y0;
            x1 = Mathf.Clamp(x0 + 1, 0, texture.width - 1);
            y1 = Mathf.Clamp(y0 + 1, 0, texture.height - 1);
            x0 = Mathf.Clamp(x0, 0, texture.width - 1);
            y0 = Mathf.Clamp(y0, 0, texture.height - 1);
        }

        static float LevelAt(Texture2D level, Vector2 uv)
        {
            var pixels = level.GetPixelData<ushort>(0);
            var width = level.width;

            Texel(level, uv, out var x0, out var y0, out var x1, out var y1, out var fx, out var fy);

            var low = Mathf.Lerp(pixels[(y0 * width + x0) * 2], pixels[(y0 * width + x1) * 2], fx);
            var high = Mathf.Lerp(pixels[(y1 * width + x0) * 2], pixels[(y1 * width + x1) * 2], fx);

            return Mathf.Lerp(low, high, fy);
        }

        static bool CutAt(Texture2D level, Vector2 uv)
        {
            var pixels = level.GetPixelData<ushort>(0);
            var x = Mathf.Clamp(Mathf.FloorToInt(uv.x * level.width), 0, level.width - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(uv.y * level.height), 0, level.height - 1);

            return pixels[(y * level.width + x) * 2 + 1] >= CutFrom;
        }

        static Vector4 MixAt(Texture2D mix, Vector2 uv)
        {
            var pixels = mix.GetPixelData<Color32>(0);
            var width = mix.width;

            Texel(mix, uv, out var x0, out var y0, out var x1, out var y1, out var fx, out var fy);

            var low = Vector4.Lerp(Mix(pixels[y0 * width + x0]), Mix(pixels[y0 * width + x1]), fx);
            var high = Vector4.Lerp(Mix(pixels[y1 * width + x0]), Mix(pixels[y1 * width + x1]), fx);

            return Vector4.Lerp(low, high, fy);
        }

        static Vector4 Mix(Color32 texel)
        {
            return new Vector4(texel.r, texel.g, texel.b, texel.a) / 255f;
        }

        static Vector3 FlowAt(Texture2D flow, Vector2 uv)
        {
            var pixels = flow.GetPixelData<ushort>(0);
            var width = flow.width;

            Texel(flow, uv, out var x0, out var y0, out var x1, out var y1, out var fx, out var fy);

            var low = Vector3.Lerp(Flow(pixels, y0 * width + x0), Flow(pixels, y0 * width + x1), fx);
            var high = Vector3.Lerp(Flow(pixels, y1 * width + x0), Flow(pixels, y1 * width + x1), fx);

            return Vector3.Lerp(low, high, fy);
        }

        static Vector3 Flow(Unity.Collections.NativeArray<ushort> pixels, int texel)
        {
            var index = texel * 4;

            return new Vector3(Mathf.HalfToFloat(pixels[index]), Mathf.HalfToFloat(pixels[index + 1]),
                               Mathf.HalfToFloat(pixels[index + 2]));
        }
    }
}
