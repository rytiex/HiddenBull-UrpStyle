using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleLiquidBakeLight
    {
        const string RootName = "HB Liquid Bake Light";
        const string Folder = "Assets/" + RootName;
        const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/BakeProxy.shader";
        const int Fewest = 128;
        const int Most = 1024;
        const int GroundMost = 1024;
        const float Boost = 1f;
        const float LightmapScale = 0.1f;
        const int Slots = HBStyleTerrainData.LiquidSlots;

        static readonly int s_EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int s_BaseColourId = Shader.PropertyToID("_BaseColor");
        static readonly Color s_Ground = new Color(0.3f, 0.3f, 0.3f, 1f);

        static readonly List<Vector3>[] s_Points = Lists<Vector3>();
        static readonly List<Vector3>[] s_Normals = Lists<Vector3>();
        static readonly List<Vector2>[] s_Coords = Lists<Vector2>();
        static readonly List<int>[] s_Corners = Lists<int>();
        static readonly HBStyleLiquidLayer[] s_Layers = new HBStyleLiquidLayer[Slots];

        static GameObject s_Root;

        internal static bool built => s_Root != null;

        static List<T>[] Lists<T>()
        {
            var lists = new List<T>[Slots];

            for (var slot = 0; slot < Slots; slot++)
                lists[slot] = new List<T>();

            return lists;
        }

        internal static void Create()
        {
            if (s_Root != null)
                return;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            if (shader == null)
                return;

            foreach (var terrain in HBStyleTerrain.active)
            {
                if (terrain != null && terrain.isActiveAndEnabled)
                {
                    AddGround(terrain, shader);
                    Add(terrain, shader);
                }
            }

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool != null && pool.isActiveAndEnabled)
                    Add(pool, shader);
            }
        }

        internal static void Clear()
        {
            if (s_Root == null)
                s_Root = GameObject.Find(RootName);

            if (s_Root != null)
                Object.DestroyImmediate(s_Root);

            s_Root = null;

            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
        }

        static float Glow(HBStyleLiquidLayer layer)
        {
            return layer != null ? layer.glow : 0f;
        }

        static void AddGround(HBStyleTerrain terrain, Shader shader)
        {
            var data = terrain.data;

            if (data == null || data.terrainData == null)
                return;

            var heights = data.terrainData;
            var cells = Mathf.Clamp(data.resolution - 1, Fewest, GroundMost);
            var row = cells + 1;
            var origin = terrain.transform.position;
            var size = data.size;
            var points = new List<Vector3>(row * row);
            var normals = new List<Vector3>(row * row);
            var coords = new List<Vector2>(row * row);
            var corners = new List<int>(cells * cells * 6);

            for (var z = 0; z < row; z++)
            {
                for (var x = 0; x < row; x++)
                {
                    var u = x / (float)cells;
                    var v = z / (float)cells;

                    points.Add(new Vector3(origin.x + u * size.x, origin.y + heights.GetInterpolatedHeight(u, v),
                                           origin.z + v * size.z));
                    normals.Add(heights.GetInterpolatedNormal(u, v));
                    coords.Add(new Vector2(u, v));
                }
            }

            for (var z = 0; z < cells; z++)
            {
                for (var x = 0; x < cells; x++)
                {
                    var first = z * row + x;

                    corners.Add(first);
                    corners.Add(first + row);
                    corners.Add(first + 1);
                    corners.Add(first + 1);
                    corners.Add(first + row);
                    corners.Add(first + row + 1);
                }
            }

            Emit(terrain.name + " Ground", shader, s_Ground, Color.black, ReceiveGI.LightProbes, true, points,
                 normals, coords, corners);
        }

        static void Add(HBStyleTerrain terrain, Shader shader)
        {
            var data = terrain.data;

            if (data == null || !data.liquidOn || data.terrainData == null)
                return;

            var count = data.liquidCount;
            var glowing = false;

            for (var slot = 0; slot < Slots; slot++)
            {
                s_Layers[slot] = slot < count ? data.Liquid(slot) : null;
                glowing |= Glow(s_Layers[slot]) > 0f;
                s_Points[slot].Clear();
                s_Normals[slot].Clear();
                s_Coords[slot].Clear();
                s_Corners[slot].Clear();
            }

            if (!glowing)
                return;

            var cells = Mathf.Clamp(data.resolution, Fewest, Most);
            var origin = terrain.transform.position;
            var size = data.size;
            var start = new Vector2(origin.x, origin.z);
            var step = new Vector2(size.x, size.z) / cells;

            for (var z = 0; z < cells; z++)
            {
                for (var x = 0; x < cells; x++)
                {
                    var low = start + new Vector2(x * step.x, z * step.y);

                    if (!HBStyleLiquid.TryGetStill(terrain, low + step * 0.5f, out _, out var mix))
                        continue;

                    var slot = 0;

                    for (var other = 1; other < Slots; other++)
                    {
                        if (mix[other] > mix[slot])
                            slot = other;
                    }

                    if (Glow(s_Layers[slot]) <= 0f)
                        continue;

                    var first = s_Points[slot].Count;

                    for (var corner = 0; corner < 4; corner++)
                    {
                        var at = low + new Vector2((corner & 1) * step.x, (corner >> 1) * step.y);

                        HBStyleLiquid.TryGetStill(terrain, at, out var level, out _);
                        s_Points[slot].Add(new Vector3(at.x, level, at.y));
                        s_Normals[slot].Add(Vector3.up);
                        s_Coords[slot].Add(new Vector2((x + (corner & 1)) / (float)cells,
                                                       (z + (corner >> 1)) / (float)cells));
                    }

                    s_Corners[slot].Add(first);
                    s_Corners[slot].Add(first + 2);
                    s_Corners[slot].Add(first + 1);
                    s_Corners[slot].Add(first + 1);
                    s_Corners[slot].Add(first + 2);
                    s_Corners[slot].Add(first + 3);
                }
            }

            for (var slot = 0; slot < Slots; slot++)
            {
                if (s_Corners[slot].Count > 0)
                    Emit(terrain.name, shader, Color.black, Emission(s_Layers[slot]), ReceiveGI.Lightmaps, false,
                         s_Points[slot], s_Normals[slot], s_Coords[slot], s_Corners[slot]);
            }
        }

        static void Add(HBStyleLiquidPool pool, Shader shader)
        {
            var layer = pool.layer;

            if (Glow(layer) <= 0f)
                return;

            var half = pool.half;
            var turn = pool.turn;
            var centre = pool.transform.position;
            var across = new Vector3(turn.y, 0f, -turn.x) * half.x;
            var along = new Vector3(turn.x, 0f, turn.y) * half.y;
            var middle = new Vector3(centre.x, pool.level, centre.z);

            var points = new List<Vector3>
            {
                middle - across - along, middle + across - along, middle - across + along, middle + across + along
            };

            var normals = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            var coords = new List<Vector2> { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            var corners = new List<int> { 0, 2, 1, 1, 2, 3 };

            Emit(pool.name, shader, Color.black, Emission(layer), ReceiveGI.Lightmaps, false, points, normals,
                 coords, corners);
        }

        static Color Emission(HBStyleLiquidLayer layer)
        {
            var colour = layer.shallowColor;
            var top = Mathf.Max(colour.maxColorComponent, 1f);

            return colour / top * (layer.glow * Boost);
        }

        static void Emit(string name, Shader shader, Color albedo, Color emission, ReceiveGI receive,
                         bool ground, List<Vector3> points, List<Vector3> normals, List<Vector2> coords,
                         List<int> corners)
        {
            if (s_Root == null)
                s_Root = new GameObject(RootName);

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", RootName);

            var path = Folder + "/" + s_Root.transform.childCount;

            var mesh = new Mesh
            {
                name = RootName,
                indexFormat = IndexFormat.UInt32
            };

            mesh.SetVertices(points);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, coords);
            mesh.SetUVs(1, coords);
            mesh.SetTriangles(corners, 0);
            mesh.RecalculateBounds();

            var material = new Material(shader)
            {
                name = RootName,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive,
                doubleSidedGI = !ground
            };

            material.SetColor(s_BaseColourId, albedo);
            material.SetColor(s_EmissionId, emission);

            AssetDatabase.CreateAsset(mesh, path + ".asset");
            AssetDatabase.CreateAsset(material, path + ".mat");

            var proxy = new GameObject(name);

            proxy.transform.SetParent(s_Root.transform, false);
            proxy.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = proxy.AddComponent<MeshRenderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ground ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.receiveGI = receive;
            renderer.scaleInLightmap = LightmapScale;

            GameObjectUtility.SetStaticEditorFlags(proxy, StaticEditorFlags.ContributeGI);
        }
    }
}
