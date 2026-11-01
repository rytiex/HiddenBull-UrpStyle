using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainPlanter
    {
        const string PlantsName = "Plants";
        const float GridCell = 8f;
        const float SpotShare = 0.75f;
        const float ShapeExtent = 1.4143f;
        const float LeastRise = 0.001f;
        const uint SpotX = 1u;
        const uint SpotZ = 2u;
        const uint Chance = 3u;
        const uint Yaw = 4u;
        const uint Size = 5u;

        static readonly Dictionary<long, List<int>> s_Grid = new Dictionary<long, List<int>>();

        static HBStyleTerrainPlants s_Indexed;
        static int s_IndexedVersion = -1;

        internal static HBStyleTerrainPlants Ensure(HBStyleTerrainData data)
        {
            if (data.plants != null)
                return data.plants;

            var plants = ScriptableObject.CreateInstance<HBStyleTerrainPlants>();

            plants.name = PlantsName;
            AssetDatabase.AddObjectToAsset(plants, data);
            data.SetPlants(plants);
            EditorUtility.SetDirty(data);
            return plants;
        }

        internal static int Add(HBStyleTerrainData data, GameObject prefab)
        {
            if (!Baked(prefab, "Add Plant"))
                return -1;

            var plants = Ensure(data);

            for (var index = 0; index < plants.types.Count; index++)
            {
                if (plants.types[index].prefab == prefab)
                    return index;
            }

            Undo.RecordObject(plants, "Add Terrain Plant");
            plants.AddType(new HBStyleTerrainPlantType(prefab));
            plants.Edited();
            EditorUtility.SetDirty(plants);
            return plants.types.Count - 1;
        }

        internal static bool Replace(HBStyleTerrainPlants plants, int index, GameObject prefab)
        {
            if (!Baked(prefab, "Replace Plant"))
                return false;

            Undo.RecordObject(plants, "Replace Terrain Plant");
            plants.types[index].prefab = prefab;
            plants.Edited();
            EditorUtility.SetDirty(plants);
            return true;
        }

        static bool Baked(GameObject prefab, string title)
        {
            if (prefab.TryGetComponent<HBStylePlant>(out var plant) && plant.asset != null && plant.asset.valid)
                return true;

            EditorUtility.DisplayDialog(title,
                                        $"'{prefab.name}' has no baked HB Style Plant. Add the component to the " +
                                        "prefab and press Bake Plant first.", "OK");
            return false;
        }

        internal static void Remove(HBStyleTerrainPlants plants, int index)
        {
            Undo.RegisterCompleteObjectUndo(plants, "Remove Terrain Plant");
            plants.RemoveType(index);
            plants.Edited();
            EditorUtility.SetDirty(plants);
        }

        internal static void Clear(HBStyleTerrainPlants plants)
        {
            Undo.RegisterCompleteObjectUndo(plants, "Clear Terrain Plants");
            plants.records.Clear();
            plants.Edited();
            EditorUtility.SetDirty(plants);
        }

        internal static int Placed(HBStyleTerrainPlants plants, int type)
        {
            var count = 0;

            foreach (var record in plants.records)
            {
                if (record.type == type)
                    count++;
            }

            return count;
        }

        internal static void Stamp(HBStyleTerrainData data, int type, Vector3 local, float radius, float hardness,
                                   bool shift, StyleTerrainShape shape)
        {
            var plants = data.plants;

            if (plants == null || type < 0 || type >= plants.types.Count)
                return;

            if (shift)
                Erase(plants, type, local, radius, hardness, shape);
            else
                Paint(data, plants, type, local, radius, hardness, shape);
        }

        internal static void Single(HBStyleTerrainData data, int type, Vector3 local, float radius, bool shift)
        {
            var plants = data.plants;

            if (plants == null || type < 0 || type >= plants.types.Count)
                return;

            if (shift)
            {
                EraseNearest(plants, type, local, radius);
                return;
            }

            var kind = plants.types[type];
            var size = data.size;

            local.y = data.terrainData.GetInterpolatedHeight(local.x / size.x, local.z / size.z);
            plants.Plant(type, local, Random.value * kind.rotation,
                         Mathf.Lerp(kind.sizeLeast, kind.sizeMost, Random.value));
            plants.Edited();
        }

        static void EraseNearest(HBStyleTerrainPlants plants, int type, Vector3 local, float radius)
        {
            var records = plants.records;
            var nearest = radius * radius;
            var found = -1;

            for (var slot = 0; slot < records.Count; slot++)
            {
                var record = records[slot];

                if (record.type != type)
                    continue;

                var dx = record.position.x - local.x;
                var dz = record.position.z - local.z;
                var away = dx * dx + dz * dz;

                if (away > nearest)
                    continue;

                nearest = away;
                found = slot;
            }

            if (found < 0)
                return;

            records.RemoveAt(found);
            plants.Edited();
        }

        static void Paint(HBStyleTerrainData data, HBStyleTerrainPlants plants, int type, Vector3 local, float radius,
                          float hardness, StyleTerrainShape shape)
        {
            var kind = plants.types[type];
            var spacing = kind.minDistance;
            var cell = spacing * SpotShare;
            var extent = radius * (shape != null ? ShapeExtent : 1f);
            var size = data.size;
            var terrain = data.terrainData;
            var x0 = Mathf.Max(Mathf.FloorToInt((local.x - extent) / cell), 0);
            var z0 = Mathf.Max(Mathf.FloorToInt((local.z - extent) / cell), 0);
            var x1 = Mathf.Min(Mathf.FloorToInt((local.x + extent) / cell), Mathf.FloorToInt(size.x / cell));
            var z1 = Mathf.Min(Mathf.FloorToInt((local.z + extent) / cell), Mathf.FloorToInt(size.z / cell));
            var changed = false;

            Index(plants);

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var seed = Seed(x, z, type);
                    var spotX = (x + Share(seed, SpotX)) * cell;
                    var spotZ = (z + Share(seed, SpotZ)) * cell;

                    if (spotX > size.x || spotZ > size.z)
                        continue;

                    var weight = StyleTerrainStamp.Weight(shape, spotX - local.x, spotZ - local.z, radius, hardness);

                    if (weight <= 0f || Share(seed, Chance) >= kind.density * weight ||
                        Blocked(plants, spotX, spotZ, spacing))
                        continue;

                    var height = terrain.GetInterpolatedHeight(spotX / size.x, spotZ / size.z);
                    var yaw = Share(seed, Yaw) * kind.rotation;
                    var scale = Mathf.Lerp(kind.sizeLeast, kind.sizeMost, Share(seed, Size));

                    Insert(plants.records.Count, spotX, spotZ);
                    plants.Plant(type, new Vector3(spotX, height, spotZ), yaw, scale);
                    changed = true;
                }
            }

            if (!changed)
                return;

            plants.Edited();
            s_IndexedVersion = plants.version;
        }

        static void Erase(HBStyleTerrainPlants plants, int type, Vector3 local, float radius, float hardness,
                          StyleTerrainShape shape)
        {
            var records = plants.records;
            var extent = radius * (shape != null ? ShapeExtent : 1f);
            var total = records.Count;
            var kept = 0;

            for (var slot = 0; slot < total; slot++)
            {
                var record = records[slot];
                var x = record.position.x - local.x;
                var z = record.position.z - local.z;

                if (record.type == type && Mathf.Abs(x) <= extent && Mathf.Abs(z) <= extent &&
                    StyleTerrainStamp.Weight(shape, x, z, radius, hardness) > 0f)
                    continue;

                records[kept++] = record;
            }

            if (kept == total)
                return;

            records.RemoveRange(kept, total - kept);
            plants.Edited();
        }

        internal static void Follow(HBStyleTerrainData data, float x0, float z0, float x1, float z1)
        {
            var plants = data.plants;

            if (plants == null)
                return;

            var records = plants.records;
            var size = data.size;
            var terrain = data.terrainData;
            var changed = false;

            for (var slot = 0; slot < records.Count; slot++)
            {
                var record = records[slot];
                var position = record.position;

                if (position.x < x0 || position.x > x1 || position.z < z0 || position.z > z1)
                    continue;

                var height = terrain.GetInterpolatedHeight(position.x / size.x, position.z / size.z);

                if (Mathf.Abs(height - position.y) < LeastRise)
                    continue;

                record.position.y = height;
                records[slot] = record;
                changed = true;
            }

            if (!changed)
                return;

            plants.Edited();
            EditorUtility.SetDirty(plants);
        }

        internal static void FollowAll(HBStyleTerrainData data)
        {
            Follow(data, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
        }

        static void Index(HBStyleTerrainPlants plants)
        {
            if (s_Indexed == plants && s_IndexedVersion == plants.version)
                return;

            s_Indexed = plants;
            s_IndexedVersion = plants.version;
            s_Grid.Clear();

            var records = plants.records;

            for (var slot = 0; slot < records.Count; slot++)
                Insert(slot, records[slot].position.x, records[slot].position.z);
        }

        static void Insert(int slot, float x, float z)
        {
            var key = Key(Mathf.FloorToInt(x / GridCell), Mathf.FloorToInt(z / GridCell));

            if (!s_Grid.TryGetValue(key, out var list))
            {
                list = new List<int>();
                s_Grid.Add(key, list);
            }

            list.Add(slot);
        }

        static bool Blocked(HBStyleTerrainPlants plants, float x, float z, float spacing)
        {
            var records = plants.records;
            var types = plants.types;
            var x0 = Mathf.FloorToInt((x - spacing) / GridCell);
            var z0 = Mathf.FloorToInt((z - spacing) / GridCell);
            var x1 = Mathf.FloorToInt((x + spacing) / GridCell);
            var z1 = Mathf.FloorToInt((z + spacing) / GridCell);

            for (var cellZ = z0; cellZ <= z1; cellZ++)
            {
                for (var cellX = x0; cellX <= x1; cellX++)
                {
                    if (!s_Grid.TryGetValue(Key(cellX, cellZ), out var list))
                        continue;

                    foreach (var slot in list)
                    {
                        var record = records[slot];
                        var dx = record.position.x - x;
                        var dz = record.position.z - z;
                        var least = Mathf.Min(spacing, types[record.type].minDistance);

                        if (dx * dx + dz * dz < least * least)
                            return true;
                    }
                }
            }

            return false;
        }

        static long Key(int x, int z)
        {
            return ((long)z << 32) ^ (uint)x;
        }

        static uint Seed(int x, int z, int type)
        {
            var seed = (uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)type * 83492791u;

            seed ^= seed >> 13;
            seed *= 0x5bd1e995u;

            return seed ^ (seed >> 15);
        }

        static float Share(uint seed, uint step)
        {
            var value = seed + step * 0x9e3779b9u;

            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;

            return (value & 0xffffffu) / 16777216f;
        }
    }
}
