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
        const string ShaderName = "Hidden/HB URP Style/Bake Proxy";
        const int Fewest = 128;
        const int Most = 1024;
        const float Boost = 4f;
        const int Slots = 4;

        static readonly int s_GlowId = Shader.PropertyToID("_Glow");
        static readonly int s_ToneId = Shader.PropertyToID("_DepthColor0");
        static readonly int s_EmissionId = Shader.PropertyToID("_EmissionColor");

        static readonly List<Vector3>[] s_Points = Lists<Vector3>();
        static readonly List<Vector3>[] s_Normals = Lists<Vector3>();
        static readonly List<Vector2>[] s_Coords = Lists<Vector2>();
        static readonly List<int>[] s_Corners = Lists<int>();

        static GameObject s_Root;

        static List<T>[] Lists<T>()
        {
            var lists = new List<T>[Slots];

            for (var slot = 0; slot < Slots; slot++)
                lists[slot] = new List<T>();

            return lists;
        }

        public static void Create()
        {
            if (s_Root != null)
                return;

            var shader = Shader.Find(ShaderName);

            if (shader == null)
                return;

            foreach (var terrain in HBStyleLiquidTerrain.terrains)
            {
                if (terrain != null && terrain.isActiveAndEnabled)
                    Add(terrain, shader);
            }

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool != null && pool.isActiveAndEnabled)
                    Add(pool, shader);
            }
        }

        public static void Clear()
        {
            if (s_Root == null)
                s_Root = GameObject.Find(RootName);

            if (s_Root != null)
                Object.DestroyImmediate(s_Root);

            s_Root = null;

            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
        }

        static float Glow(Material material)
        {
            return material != null && material.HasFloat(s_GlowId) ? material.GetFloat(s_GlowId) : 0f;
        }

        static void Add(HBStyleLiquidTerrain terrain, Shader shader)
        {
            var layers = terrain.layers;
            var count = Mathf.Min(layers.Count, Slots);
            var glowing = false;

            for (var slot = 0; slot < count; slot++)
                glowing |= Glow(layers[slot]) > 0f;

            if (!glowing)
                return;

            for (var slot = 0; slot < Slots; slot++)
            {
                s_Points[slot].Clear();
                s_Normals[slot].Clear();
                s_Coords[slot].Clear();
                s_Corners[slot].Clear();
            }

            var data = terrain.data;
            var cells = data != null ? Mathf.Clamp(data.resolution, Fewest, Most) : Fewest;
            var area = terrain.area;
            var size = new Vector2(1f / area.z, 1f / area.w);
            var start = new Vector2(area.x, area.y) - size * 0.5f;
            var step = size / cells;

            for (var z = 0; z < cells; z++)
            {
                for (var x = 0; x < cells; x++)
                {
                    var low = start + new Vector2(x * step.x, z * step.y);
                    var middle = low + step * 0.5f;

                    if (terrain.Masked(middle))
                        continue;

                    if (terrain.GroundAt(middle, out var bed, out _) && bed > terrain.LevelAt(middle))
                        continue;

                    var painted = terrain.PaintedAt(middle);
                    var slot = 0;
                    var most = 1f - painted.x - painted.y - painted.z;

                    for (var other = 1; other < count; other++)
                    {
                        if (painted[other - 1] <= most)
                            continue;

                        most = painted[other - 1];
                        slot = other;
                    }

                    if (Glow(layers[slot]) <= 0f)
                        continue;

                    var first = s_Points[slot].Count;

                    for (var corner = 0; corner < 4; corner++)
                    {
                        var at = low + new Vector2((corner & 1) * step.x, (corner >> 1) * step.y);

                        s_Points[slot].Add(new Vector3(at.x, terrain.LevelAt(at), at.y));
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

            for (var slot = 0; slot < count; slot++)
            {
                if (s_Corners[slot].Count > 0)
                    Emit(terrain.name, layers[slot], shader, s_Points[slot], s_Normals[slot], s_Coords[slot],
                         s_Corners[slot]);
            }
        }

        static void Add(HBStyleLiquidPool pool, Shader shader)
        {
            var material = pool.material;

            if (Glow(material) <= 0f)
                return;

            var box = pool.box;
            var centre = pool.transform.position;
            var across = new Vector3(box.w, 0f, -box.z) * box.x;
            var along = new Vector3(box.z, 0f, box.w) * box.y;
            var middle = new Vector3(centre.x, pool.level, centre.z);

            var points = new List<Vector3>
            {
                middle - across - along, middle + across - along, middle - across + along, middle + across + along
            };

            var normals = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            var coords = new List<Vector2> { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            var corners = new List<int> { 0, 2, 1, 1, 2, 3 };

            Emit(pool.name, material, shader, points, normals, coords, corners);
        }

        static void Emit(string name, Material source, Shader shader, List<Vector3> points, List<Vector3> normals,
                         List<Vector2> coords, List<int> corners)
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
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive
            };

            var tone = source.HasColor(s_ToneId) ? source.GetColor(s_ToneId) : Color.white;

            material.SetColor(s_EmissionId, tone * (Glow(source) * Boost));

            AssetDatabase.CreateAsset(mesh, path + ".asset");
            AssetDatabase.CreateAsset(material, path + ".mat");

            var proxy = new GameObject(name);

            proxy.transform.SetParent(s_Root.transform, false);
            proxy.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = proxy.AddComponent<MeshRenderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.receiveGI = ReceiveGI.LightProbes;

            GameObjectUtility.SetStaticEditorFlags(proxy, StaticEditorFlags.ContributeGI);
        }
    }
}
