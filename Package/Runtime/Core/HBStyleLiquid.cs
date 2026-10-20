using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    public static class HBStyleLiquid
    {
        const int Passes = 3;
        const float Edge = 32f;
        const float FoldLimit = 1f;
        const float ShortestLength = 0.05f;
        const float NormalSpan = 0.5f;
        const float Crest = 1.8f;
        const float Buried = 0.5f;
        const float BoxEdge = 0.5f;
        const float Above = 2f;
        const float LayerRough = 0.25f;
        const float LayerSoft = 0.5f;
        const float LayerFloor = 1e-3f;
        const float StreamTurn = 1f;
        const float StreamLeast = 0.01f;
        const float ShoalDepth = 8f;
        const float Trough = 3f;
        const float Drift = 0.25f;
        const float DriftFrom = 0.4f;
        const float ShoreShare = 0.7f;
        const float ShoreGain = 1.6f;
        const float ShoreTile = 12f;
        const float ShoreReach = 40f;
        const float ShoreScatter = 0.137f;
        const int ShoreBand = 1;

        static readonly int s_WindWavesId = Shader.PropertyToID("_WindWaves");
        static readonly int s_WavesId = Shader.PropertyToID("_Waves");
        static readonly int s_WaveHeightId = Shader.PropertyToID("_WaveHeight");
        static readonly int s_WaveSteepnessId = Shader.PropertyToID("_WaveSteepness");
        static readonly int s_WaveLengthId = Shader.PropertyToID("_WaveLength");

        static readonly Vector3 s_Pool = new Vector3(0f, 0.2f, 1f);

        static readonly float[] s_Phases = new float[StyleWavesPass.Bands * StyleWavesPass.Count];

        static double s_PhaseTime = double.NaN;
        static bool s_Synced;
        static double s_SyncTime;
        static double s_SyncAt;
        static float s_SyncWind;
        static float s_SyncHeading;

        struct Shape
        {
            public float amount;
            public float tall;
            public float steep;
            public float wide;
            public HBStyleLiquidTerrain terrain;
            public HBStyleLiquidPool pool;
            public float level;
            public Vector2 half;
            public Vector2 turn;
            public Vector2 centre;
        }

        static double clock => Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;

        public static bool any => HBStyleLiquidTerrain.any || HBStyleLiquidPool.any;

        public static bool synced => s_Synced;

        public static double time => s_Synced ? s_SyncTime + (clock - s_SyncAt) : clock;

        public static float wind
        {
            get
            {
                if (s_Synced)
                    return s_SyncWind;

                return StyleWeatherState.swell;
            }
        }

        public static float windHeading
        {
            get
            {
                if (s_Synced)
                    return s_SyncHeading;

                var weather = VolumeManager.instance.stack?.GetComponent<HBStyleWeather>();

                return weather != null ? weather.windHeading.value : 0f;
            }
        }

        internal static float seaSwell
        {
            get
            {
                foreach (var terrain in HBStyleLiquidTerrain.terrains)
                {
                    if (terrain != null && TryShape(terrain, 0, out var shape))
                        return shape.amount * shape.tall * shape.wide * Crest;
                }

                return 0f;
            }
        }

        public static void Sync(double time, float wind, float windHeading)
        {
            s_Synced = true;
            s_SyncTime = time;
            s_SyncAt = clock;
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

        public static bool TryGetHeight(Vector3 position, out float height, out Component liquid,
                                        bool waves = true)
        {
            return Find(position, true, true, waves, out height, out liquid);
        }

        internal static bool TryGetUncutHeight(Vector3 position, out float height)
        {
            return Find(position, false, false, true, out height, out _);
        }

        internal static bool TryGetSubmerged(Vector3 position, out float height, out Component liquid)
        {
            return Find(position, true, false, true, out height, out liquid);
        }

        static bool Find(Vector3 position, bool cut, bool over, bool waves, out float height,
                         out Component liquid)
        {
            height = 0f;
            liquid = null;

            var nearest = float.MaxValue;
            var heading = windHeading * Mathf.Deg2Rad;
            var frame = new Vector2(Mathf.Sin(heading), Mathf.Cos(heading));

            Refresh();

            foreach (var candidate in HBStyleLiquidTerrain.terrains)
            {
                var here = new Vector2(position.x, position.z);

                if (candidate == null || candidate.Masked(here) || !TryShape(candidate, 0, out var shape))
                    continue;

                if (candidate != HBStyleLiquidTerrain.sea && candidate.Inside(here) < 0f)
                    continue;

                if (!TryHeight(shape, frame, new Vector2(position.x, position.z), waves, out var surface))
                    continue;

                if (cut && ((over && Cut(candidate, new Vector3(position.x, surface, position.z)))
                            || Cut(candidate, position)))
                    continue;

                if (cut && candidate.GroundAt(here, out var seabed, out _) && position.y < seabed - Buried)
                    continue;

                var apart = Mathf.Abs(surface - position.y);

                if (apart >= nearest)
                    continue;

                nearest = apart;
                height = surface;
                liquid = candidate;
            }

            var owned = false;
            var smallest = float.MaxValue;

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool == null || position.y < pool.bottom || !TryShape(pool, out var shape))
                    continue;

                if (!TryHeight(shape, frame, new Vector2(position.x, position.z), waves, out var surface))
                    continue;

                if (cut && ((over && Cut(pool, new Vector3(position.x, surface, position.z))) || Cut(pool, position)))
                    continue;

                var area = shape.half.x * shape.half.y;

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

        public static bool TryGetSurface(Vector3 position, out float height, out Vector3 normal,
                                         bool waves = true)
        {
            return TryGetSurface(position, out height, out normal, out _, waves);
        }

        public static bool TryGetSurface(Vector3 position, out float height, out Vector3 normal,
                                         out Component liquid, bool waves = true)
        {
            normal = Vector3.up;

            if (!TryGetHeight(position, out height, out liquid, waves))
                return false;

            if (!TryShape(liquid, out var shape))
                return false;

            var heading = windHeading * Mathf.Deg2Rad;
            var frame = new Vector2(Mathf.Sin(heading), Mathf.Cos(heading));
            var here = new Vector2(position.x, position.z);

            if (TryHeight(shape, frame, here + new Vector2(NormalSpan, 0f), waves, out var east)
                && TryHeight(shape, frame, here + new Vector2(0f, NormalSpan), waves, out var north))
                normal = new Vector3(height - east, NormalSpan, height - north).normalized;

            return true;
        }

        public static bool TryGetDepth(Vector3 position, out float depth, bool waves = true)
        {
            return TryGetDepth(position, out depth, out _, waves);
        }

        public static bool TryGetDepth(Vector3 position, out float depth, out Component liquid,
                                       bool waves = true)
        {
            depth = 0f;

            if (!TryGetHeight(position, out var height, out liquid, waves))
                return false;

            if (liquid is HBStyleLiquidPool pool)
            {
                depth = Mathf.Max(height - pool.bottom, 0f);
                return true;
            }

            var here = new Vector2(position.x, position.z);

            depth = ((HBStyleLiquidTerrain)liquid).GroundAt(here, out var seabed, out _)
                ? Mathf.Max(height - seabed, 0f)
                : float.PositiveInfinity;

            return true;
        }

        static bool Cut(Component liquid, Vector3 point)
        {
            var keeps = false;
            var kept = false;

            foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
            {
                if (cutter == null)
                    continue;

                if (liquid is HBStyleLiquidPool pool ? !cutter.Reaches(pool)
                                                   : !cutter.Affects((HBStyleLiquidTerrain)liquid))
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

        static float Calm(in Shape shape, Vector2 rest)
        {
            var floor = 0f;

            if (shape.pool != null)
                floor = shape.pool.bottom;
            else if (!shape.terrain.GroundAt(rest, out _, out floor))
                return 1f;

            var size = shape.tall * shape.wide;
            var deep = Mathf.Max(Level(shape, rest) - floor, 0f);
            var open = Mathf.Clamp01(deep / (ShoalDepth * Mathf.Max(size, 1f)));

            return Mathf.Min(open * (2f - open), deep / Mathf.Max(Trough * shape.amount * size, 1e-3f));
        }

        static float Turn(in Shape shape, Vector2 rest)
        {
            if (shape.pool != null)
                return 0f;

            var rush = Mathf.Clamp01(shape.terrain.StreamAt(rest).magnitude / StreamTurn);

            if (!shape.terrain.ShoreAt(rest, out var field) || field.x <= 0f)
                return rush;

            var close = Mathf.Clamp01(1f - field.x / ShoreReach);

            return Mathf.Max(rush, Mathf.Clamp01(new Vector2(field.y, field.z).magnitude) * close * close
                                   * (3f - 2f * close));
        }

        static Vector2 Heading(HBStyleLiquidTerrain terrain, Vector2 frame, Vector2 centre)
        {
            var stream = terrain.StreamAt(centre);

            if (stream.sqrMagnitude > StreamLeast * StreamLeast)
                return stream.normalized;

            if (!terrain.ShoreAt(centre, out var field))
                return frame;

            var toward = new Vector2(field.y, field.z) * (field.x < 0f ? -1f : 1f);
            var sure = toward.sqrMagnitude;

            return sure > 0.25f ? toward / Mathf.Sqrt(sure) : frame;
        }

        static Vector3 Shoreward(HBStyleLiquidTerrain terrain, Vector2 frame, Vector2 position)
        {
            var grid = position / ShoreTile - new Vector2(0.5f, 0.5f);
            var corner = new Vector2(Mathf.Floor(grid.x), Mathf.Floor(grid.y));
            var blend = grid - corner;
            var patch = StyleWavesPass.Patch(ShoreBand);
            var total = Vector3.zero;

            blend = new Vector2(blend.x * blend.x * (3f - 2f * blend.x), blend.y * blend.y * (3f - 2f * blend.y));

            for (var index = 0; index < 4; index++)
            {
                var tile = new Vector2(index & 1, index >> 1);
                var centre = (corner + tile + new Vector2(0.5f, 0.5f)) * ShoreTile;
                var local = position - centre;
                var heading = Heading(terrain, frame, centre);
                var side = new Vector2(-heading.y, heading.x);
                var share = Mathf.Lerp(1f - blend.x, blend.x, tile.x) * Mathf.Lerp(1f - blend.y, blend.y, tile.y);
                var scatter = centre * ShoreScatter;
                var moved = Vector3.zero;

                scatter = new Vector2(scatter.x - Mathf.Floor(scatter.x), scatter.y - Mathf.Floor(scatter.y)) * patch;

                Band(ShoreBand, new Vector2(Vector2.Dot(local, heading), Vector2.Dot(local, side)) + scatter, 1f,
                     ref moved);

                var pushed = heading * moved.x + side * moved.z;

                total += new Vector3(pushed.x, moved.y, pushed.y) * share;
            }

            return total;
        }

        static void Refresh()
        {
            var now = time;

            if (now == s_PhaseTime)
                return;

            s_PhaseTime = now;

            for (var slot = 0; slot < s_Phases.Length; slot++)
                s_Phases[slot] = StyleWavesPass.Phase(slot, now);
        }

        static bool TryMaterial(Material material, ref Shape shape)
        {
            if (material == null || !material.HasProperty(s_WaveHeightId))
                return false;

            shape.amount = material.GetFloat(s_WindWavesId) > 0.5f ? wind : material.GetFloat(s_WavesId);
            shape.tall = material.GetFloat(s_WaveHeightId);
            shape.steep = material.GetFloat(s_WaveSteepnessId);
            shape.wide = Mathf.Max(material.GetFloat(s_WaveLengthId), ShortestLength);

            return true;
        }

        static bool TryShape(Component liquid, out Shape shape)
        {
            shape = default;

            if (liquid is HBStyleLiquidPool pool)
                return TryShape(pool, out shape);

            return liquid is HBStyleLiquidTerrain terrain && TryShape(terrain, 0, out shape);
        }

        static bool TryShape(HBStyleLiquidPool pool, out Shape shape)
        {
            shape = default;

            if (!TryMaterial(pool.material, ref shape))
                return false;

            var position = pool.transform.position;
            var box = pool.box;

            shape.pool = pool;
            shape.level = pool.level;
            shape.centre = new Vector2(position.x, position.z);
            shape.half = new Vector2(box.x, box.y);
            shape.turn = new Vector2(box.z, box.w);

            return true;
        }

        static bool TryShape(HBStyleLiquidTerrain terrain, int layer, out Shape shape)
        {
            shape = default;

            var layers = terrain.layers;

            if (!TryMaterial(layer < layers.Count && layers[layer] != null ? layers[layer] : terrain.material,
                             ref shape))
                return false;

            shape.terrain = terrain;

            return true;
        }

        static float Level(in Shape shape, Vector2 position)
        {
            return shape.pool != null ? shape.level : shape.terrain.LevelAt(position);
        }

        static Vector2 Inward(in Shape shape, Vector2 position)
        {
            if (shape.pool == null)
                return new Vector2(float.MaxValue, float.MaxValue);

            var past = position - shape.centre;
            var local = new Vector2(past.x * shape.turn.y - past.y * shape.turn.x,
                                    past.x * shape.turn.x + past.y * shape.turn.y);

            return new Vector2(shape.half.x - Mathf.Abs(local.x), shape.half.y - Mathf.Abs(local.y));
        }

        static bool TryHeight(in Shape shape, Vector2 frame, Vector2 position, bool waves, out float height)
        {
            height = Level(shape, position);

            var inward = Inward(shape, position);

            if (inward.x < 0f || inward.y < 0f)
                return false;

            if (!waves)
                return true;

            var rest = position;

            for (var pass = 0; pass < Passes; pass++)
            {
                var moved = Displace(shape, frame, rest);

                rest = position - new Vector2(moved.x, moved.z);
            }

            height = Level(shape, rest) + Displace(shape, frame, rest).y;

            return true;
        }

        static float Share(float share)
        {
            var past = Mathf.InverseLerp(LayerRough, LayerRough + LayerSoft, 1f - share);

            return 1f - past * past * (3f - 2f * past) + share * LayerFloor;
        }

        static Vector3 Displace(in Shape shape, Vector2 frame, Vector2 rest)
        {
            var own = Single(shape, frame, rest);
            var terrain = shape.terrain;

            if (terrain == null || terrain.data == null)
                return own;

            var count = Mathf.Min(terrain.layers.Count, StyleLiquidLayers.Most);

            if (count < 2)
                return own;

            var painted = terrain.PaintedAt(rest);
            var weights = new Vector3(Share(painted.x), Share(painted.y), Share(painted.z));
            var total = Share(Mathf.Clamp01(1f - painted.x - painted.y - painted.z)) + weights.x + weights.y
                      + weights.z;
            var moved = own;

            for (var slot = 0; slot < count - 1; slot++)
            {
                if (weights[slot] > 0f && TryShape(terrain, slot + 1, out var other))
                    moved += (Single(other, frame, rest) - own) * (weights[slot] / total);
            }

            return moved;
        }

        static Vector3 Single(in Shape shape, Vector2 frame, Vector2 rest)
        {
            var rim = 1f;

            if (shape.pool != null)
            {
                var inward = Inward(shape, rest);

                rim = Mathf.Clamp01(Mathf.Min(inward.x, inward.y)
                                    / Mathf.Min(Edge, Mathf.Min(shape.half.x, shape.half.y) * BoxEdge));
            }
            else if (shape.terrain.data != null)
            {
                rim = 1f - shape.terrain.StillAt(rest);
            }

            if (rim <= 0f || shape.amount <= 0f)
                return Vector3.zero;

            var calm = Calm(shape, rest);
            var turn = Turn(shape, rest);
            var moved = Moved(shape, frame, rest, calm, turn, rim);
            var drift = Mathf.Sqrt(moved.x * moved.x + moved.z * moved.z);

            if (calm <= 0f || drift <= Drift * DriftFrom)
                return moved;

            var landed = Mathf.Min(calm, Calm(shape, rest + new Vector2(moved.x, moved.z)));

            if (landed >= calm)
                return moved;

            var pull = Mathf.Clamp01((drift / Drift - DriftFrom) / (1f - DriftFrom));

            return Moved(shape, frame, rest, Mathf.Lerp(calm, landed, pull), turn, rim);
        }

        static Vector3 Sum(in Shape shape, Vector2 framed, float open)
        {
            var moved = Vector3.zero;

            for (var band = 0; band < StyleWavesPass.Bands; band++)
                Band(band, framed, (shape.pool != null ? s_Pool[band] : 1f) * shape.amount * open, ref moved);

            return moved;
        }

        static Vector3 Moved(in Shape shape, Vector2 frame, Vector2 rest, float calm, float turn, float rim)
        {
            var open = calm * (1f - turn * ShoreShare);
            var scaled = rest / shape.wide;
            var side = new Vector2(-frame.y, frame.x);
            var framed = new Vector2(Vector2.Dot(scaled, frame), Vector2.Dot(scaled, side));
            var moved = Sum(shape, framed, open);

            if (shape.pool != null)
            {
                var back = Sum(shape, -framed, open);

                moved = (moved + new Vector3(-back.x, back.y, -back.z)) * 0.5f;
            }

            var flat = (frame * moved.x + side * moved.z) * shape.wide;
            var rise = moved.y * shape.wide;

            if (turn > 0f)
            {
                var toward = Shoreward(shape.terrain, frame, rest) * (calm * turn * shape.amount * ShoreGain);

                flat += new Vector2(toward.x, toward.z);
                rise += toward.y;
            }

            var push = Mathf.Min(shape.tall * shape.steep, FoldLimit / Mathf.Max(shape.amount * open, 1e-3f));

            return new Vector3(flat.x * push * rim, rise * shape.tall * rim, flat.y * push * rim);
        }

        static void Band(int band, Vector2 framed, float weight, ref Vector3 moved)
        {
            if (weight <= 0f)
                return;

            for (var index = 0; index < StyleWavesPass.Count; index++)
            {
                var slot = band * StyleWavesPass.Count + index;
                var wave = StyleWavesPass.Wave(slot);
                var phase = wave.x * framed.x + wave.y * framed.y + s_Phases[slot];
                var rise = wave.z * weight;
                var length = Mathf.Sqrt(wave.x * wave.x + wave.y * wave.y);
                var push = rise * StyleWavesPass.Chop * Mathf.Cos(phase) / length;

                moved.x += wave.x * push;
                moved.y += rise * Mathf.Sin(phase);
                moved.z += wave.y * push;
            }
        }
    }
}
