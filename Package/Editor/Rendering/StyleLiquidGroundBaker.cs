using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleLiquidGroundBaker
    {
        const string ShaderName = "Hidden/HB URP Style/Ground Height";
        const float Open = -30000f;
        const float Dry = 10000f;
        const float Worn = 6f;
        const int WornMost = 32;
        const float SlopeReach = 64f;
        const float SlopeRise = 0.25f;
        const float ShoreFar = 64f;
        const float Unseeded = -1e6f;

        static readonly int s_FrameId = Shader.PropertyToID("_HB_GroundFrame");

        static readonly Dictionary<HBStyleLiquidTerrainData, Hash128> s_Baked =
            new Dictionary<HBStyleLiquidTerrainData, Hash128>();

        static readonly Vector2Int[] s_Around =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        static StyleLiquidGroundBaker()
        {
            EditorSceneManager.sceneSaving -= Saving;
            EditorSceneManager.sceneSaving += Saving;

            EditorApplication.playModeStateChanged -= Playing;
            EditorApplication.playModeStateChanged += Playing;

            EditorApplication.delayCall += Mend;
        }

        static void Mend()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var terrain in HBStyleLiquidTerrain.terrains)
            {
                if (terrain != null && terrain.data != null && !terrain.data.grounded)
                    Bake(terrain);
            }
        }

        static void Saving(Scene scene, string path)
        {
            foreach (var terrain in HBStyleLiquidTerrain.terrains)
            {
                if (terrain != null && terrain.gameObject.scene == scene)
                    Bake(terrain);
            }
        }

        static void Playing(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode)
                return;

            foreach (var terrain in HBStyleLiquidTerrain.terrains)
            {
                if (terrain != null)
                    Bake(terrain);
            }
        }

        internal static void Bake(HBStyleLiquidTerrain terrain)
        {
            var data = terrain.data;

            if (data == null)
                return;

            var size = data.resolution;
            var count = size * size;
            var span = terrain.size;
            var centre = terrain.transform.position;
            var texel = new Vector2(span.x / size, span.y / size);
            var levels = data.levels;
            var weights = data.weights;
            var terrains = Terrain.activeTerrains;
            var raw = Capture(centre, span, size);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var east = centre.x + ((x + 0.5f) / size - 0.5f) * span.x;
                    var north = centre.z + ((y + 0.5f) / size - 0.5f) * span.y;

                    raw[y * size + x] = Mathf.Max(raw[y * size + x], Soil(terrains, east, north));
                }
            }

            var hash = Hash128.Compute(raw);

            hash.Append(levels);
            hash.Append(weights);
            hash.Append(span.x);
            hash.Append(span.y);
            hash.Append(centre.y);

            if (data.grounded && s_Baked.TryGetValue(data, out var baked) && baked == hash)
                return;

            s_Baked[data] = hash;

            var surface = new float[count];

            for (var index = 0; index < count; index++)
                surface[index] = centre.y + (weights[index * 4 + 3] > 127 ? -Dry : levels[index]);

            var wide = Mathf.Clamp(Mathf.RoundToInt(Worn / texel.x), 1, WornMost);
            var tall = Mathf.Clamp(Mathf.RoundToInt(Worn / texel.y), 1, WornMost);
            var bed = Span(Span(raw, size, wide, tall, true), size, wide, tall, false);
            var calm = Slope(bed, surface, size, texel);
            var shore = Shore(bed, surface, size, texel);

            if (!data.Lay(bed, calm, shore))
                return;

            EditorUtility.SetDirty(data);

            if (EditorUtility.IsPersistent(data))
                AssetDatabase.SaveAssetIfDirty(data);
        }

        static float[] Capture(Vector3 centre, Vector2 span, int size)
        {
            var raw = new float[size * size];

            for (var index = 0; index < raw.Length; index++)
                raw[index] = Open;

            var shader = Shader.Find(ShaderName);

            if (shader == null)
                return raw;

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var area = new Bounds(centre, new Vector3(span.x, float.MaxValue, span.y));
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGBFloat,
                                                    RenderTextureReadWrite.Linear);
            var command = new CommandBuffer { name = "HiddenBull Style Ground Height" };

            command.SetRenderTarget(target);
            command.ClearRenderTarget(false, true, new Color(Open, Open, Open, 0f));
            command.SetGlobalVector(s_FrameId, new Vector4(centre.x, centre.z, 2f / span.x, 2f / span.y));

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !renderer.bounds.Intersects(area))
                    continue;

                var materials = renderer.sharedMaterials;

                for (var part = 0; part < materials.Length; part++)
                {
                    if (materials[part] != null && materials[part].renderQueue <= (int)RenderQueue.GeometryLast)
                        command.DrawRenderer(renderer, material, part, 0);
                }
            }

            Graphics.ExecuteCommandBuffer(command);
            command.Release();

            var read = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
            var active = RenderTexture.active;

            RenderTexture.active = target;
            read.ReadPixels(new Rect(0f, 0f, size, size), 0, 0, false);
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);

            var pixels = read.GetPixelData<float>(0);

            for (var index = 0; index < size * size; index++)
            {
                if (pixels[index * 4 + 3] < 0.5f)
                    continue;

                var column = Mathf.FloorToInt(((pixels[index * 4 + 1] - centre.x) / span.x + 0.5f) * size);
                var row = Mathf.FloorToInt(((pixels[index * 4 + 2] - centre.z) / span.y + 0.5f) * size);

                if (column < 0 || row < 0 || column >= size || row >= size)
                    continue;

                raw[row * size + column] = Mathf.Max(raw[row * size + column], pixels[index * 4]);
            }

            Object.DestroyImmediate(read);
            Object.DestroyImmediate(material);

            return raw;
        }

        static float Soil(Terrain[] terrains, float x, float z)
        {
            var ground = Open;

            foreach (var terrain in terrains)
            {
                var shape = terrain.terrainData;

                if (shape == null)
                    continue;

                var origin = terrain.transform.position;

                if (x < origin.x || z < origin.z || x > origin.x + shape.size.x || z > origin.z + shape.size.z)
                    continue;

                ground = Mathf.Max(ground, terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y);
            }

            return ground;
        }

        static float[] Span(float[] source, int size, int wide, int tall, bool lowest)
        {
            var across = new float[source.Length];
            var spanned = new float[source.Length];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var from = Mathf.Max(x - wide, 0);
                    var to = Mathf.Min(x + wide, size - 1);
                    var found = source[y * size + from];

                    for (var other = from + 1; other <= to; other++)
                    {
                        var height = source[y * size + other];

                        found = lowest ? Mathf.Min(found, height) : Mathf.Max(found, height);
                    }

                    across[y * size + x] = found;
                }
            }

            for (var y = 0; y < size; y++)
            {
                var from = Mathf.Max(y - tall, 0);
                var to = Mathf.Min(y + tall, size - 1);

                for (var x = 0; x < size; x++)
                {
                    var found = across[from * size + x];

                    for (var other = from + 1; other <= to; other++)
                    {
                        var height = across[other * size + x];

                        found = lowest ? Mathf.Min(found, height) : Mathf.Max(found, height);
                    }

                    spanned[y * size + x] = found;
                }
            }

            return spanned;
        }

        static float[] Slope(float[] bed, float[] surface, int size, Vector2 texel)
        {
            var from = (float[])bed.Clone();
            var to = new float[bed.Length];
            var reach = Mathf.NextPowerOfTwo(Mathf.CeilToInt(SlopeReach / Mathf.Min(texel.x, texel.y)));

            for (var step = Mathf.Min(reach, size); step >= 1; step /= 2)
            {
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var height = from[y * size + x];

                        for (var side = 0; side < 9; side++)
                        {
                            var column = Mathf.Clamp(x + (side % 3 - 1) * step, 0, size - 1);
                            var row = Mathf.Clamp(y + (side / 3 - 1) * step, 0, size - 1);
                            var other = row * size + column;
                            var away = new Vector2((column - x) * texel.x, (row - y) * texel.y).magnitude;

                            height = Mathf.Max(height, Mathf.Min(from[other], surface[other]) - away * SlopeRise);
                        }

                        to[y * size + x] = height;
                    }
                }

                (from, to) = (to, from);
            }

            return from;
        }

        static float[] Shore(float[] bed, float[] surface, int size, Vector2 texel)
        {
            var from = new Vector2[bed.Length];
            var to = new Vector2[bed.Length];
            var shore = new float[bed.Length * 3];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var here = bed[y * size + x] - surface[y * size + x];
                    var sum = Vector2.zero;
                    var seeds = 0;

                    foreach (var side in s_Around)
                    {
                        var column = x + side.x;
                        var row = y + side.y;

                        if (column < 0 || row < 0 || column >= size || row >= size)
                            continue;

                        var there = bed[row * size + column] - surface[row * size + column];

                        if (here > 0f == there > 0f)
                            continue;

                        sum += new Vector2(x, y) + new Vector2(side.x, side.y) * (here / (here - there));
                        seeds++;
                    }

                    from[y * size + x] = seeds > 0 ? sum / seeds : new Vector2(Unseeded, Unseeded);
                }
            }

            for (var step = size / 2; step >= 1; step /= 2)
            {
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var best = from[y * size + x];
                        var nearest = best.x > Unseeded * 0.5f ? Apart(best, x, y, texel) : float.MaxValue;

                        for (var side = 0; side < 9; side++)
                        {
                            var column = x + (side % 3 - 1) * step;
                            var row = y + (side / 3 - 1) * step;

                            if (column < 0 || row < 0 || column >= size || row >= size)
                                continue;

                            var seed = from[row * size + column];

                            if (seed.x <= Unseeded * 0.5f)
                                continue;

                            var away = Apart(seed, x, y, texel);

                            if (away >= nearest)
                                continue;

                            nearest = away;
                            best = seed;
                        }

                        to[y * size + x] = best;
                    }
                }

                (from, to) = (to, from);
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var seed = from[index];
                    var seeded = seed.x > Unseeded * 0.5f;
                    var toward = new Vector2((seed.x - x) * texel.x, (seed.y - y) * texel.y);
                    var away = seeded ? Mathf.Min(toward.magnitude, ShoreFar) : ShoreFar;
                    var direction = seeded && toward.sqrMagnitude > 1e-6f ? toward.normalized : Vector2.zero;

                    shore[index * 3] = bed[index] < surface[index] ? away : -away;
                    shore[index * 3 + 1] = direction.x;
                    shore[index * 3 + 2] = direction.y;
                }
            }

            return shore;
        }

        static float Apart(Vector2 seed, int x, int y, Vector2 texel)
        {
            var across = (seed.x - x) * texel.x;
            var up = (seed.y - y) * texel.y;

            return across * across + up * up;
        }
    }
}
