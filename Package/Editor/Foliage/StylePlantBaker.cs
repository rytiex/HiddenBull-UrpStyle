using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StylePlantBaker
    {
        const string FoliageShader = "HB URP Style/Foliage";
        const string BodyShader = "HB URP Style/Foliage Body";

        const int MostSamples = 4000;
        const int MostSeeds = 64;
        const int ShiftRounds = 12;
        const float ShiftReach = 0.55f;
        const float LeastCluster = 0.05f;
        const float CanopyCentre = 0.6f;
        const float BakeMark = 1f;
        const float Epsilon = 1e-3f;
        const int PlantDataSize = 3;

        readonly struct Egg
        {
            readonly Vector3 m_Centre;
            readonly Vector3 m_Radius;
            readonly float m_Above;
            readonly float m_Below;

            public Egg(Bounds bounds)
            {
                var size = bounds.size;

                m_Centre = new Vector3(bounds.center.x, bounds.min.y + CanopyCentre * size.y, bounds.center.z);
                m_Radius = new Vector3(Mathf.Max(0.5f * size.x, Epsilon), 0f, Mathf.Max(0.5f * size.z, Epsilon));
                m_Above = Mathf.Max((1f - CanopyCentre) * size.y, Epsilon);
                m_Below = Mathf.Max(Mathf.Max(CanopyCentre * size.y, 0.5f * Mathf.Max(size.x, size.z)), Epsilon);
            }

            public float Depth(Vector3 point, out Vector3 normal)
            {
                var offset = point - m_Centre;
                var radius = m_Radius;

                radius.y = offset.y > 0f ? m_Above : m_Below;

                var scaled = new Vector3(offset.x / radius.x, offset.y / radius.y, offset.z / radius.z);

                normal = new Vector3(scaled.x / radius.x, scaled.y / radius.y, scaled.z / radius.z);

                return scaled.magnitude;
            }
        }

        internal static bool IsBaked(Mesh mesh)
        {
            return mesh.HasVertexAttribute(VertexAttribute.TexCoord3) &&
                   mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord3) == PlantDataSize;
        }

        internal static int CountMeshes(HBStylePlant plant, out int baked)
        {
            var filters = FoliageFilters(plant);

            baked = 0;

            foreach (var filter in filters)
            {
                if (IsBaked(filter.sharedMesh))
                    baked++;
            }

            return filters.Count;
        }

        internal static bool SwayChanged(HBStylePlant plant)
        {
            var plantData = new List<Vector3>();

            foreach (var filter in FoliageFilters(plant))
            {
                if (!IsBaked(filter.sharedMesh) || !filter.sharedMesh.isReadable)
                    continue;

                filter.sharedMesh.GetUVs(3, plantData);

                foreach (var data in plantData)
                {
                    if (data.z > 0.5f * BakeMark)
                        return !Mathf.Approximately(data.y, plant.sway);
                }
            }

            return false;
        }

        internal static void Bake(HBStylePlant plant)
        {
            var target = Target(plant);
            var filters = FoliageFilters(target);

            if (filters.Count == 0)
                return;

            var root = target.transform;
            var eggs = FitCanopy(filters, root, out var lowest, out var highest);
            var folder = OutputFolder(target, filters[0].sharedMesh);
            var plantName = string.Join("_", target.name.Split(Path.GetInvalidFileNameChars()));

            Undo.RecordObject(target, "Bake Plant");

            for (var i = 0; i < filters.Count; i++)
            {
                var mesh = BakeMesh(filters[i], root, eggs, lowest, highest, plant.sway);
                var original = OriginalMesh(target, filters[i]);

                target.bakedMeshes.RemoveAll(entry => entry.filter == null || entry.filter == filters[i]);

                if (original != null)
                    target.bakedMeshes.Add(new HBStylePlant.BakedMesh { filter = filters[i], source = original });

                SetMesh(filters[i], Store(mesh, $"{folder}/MESH_Plant_{plantName}_{i}.asset"), "Bake Plant");
            }

            target.asset = WriteAsset(target, filters, $"{folder}/DATA_Plant_{plantName}.asset", plant.sway);

            AssetDatabase.SaveAssets();
            SavePlant(target);

            if (plant != target)
                StylePlantSystem.Add(plant);
        }

        internal static bool CanClear(HBStylePlant plant)
        {
            var target = Target(plant);

            return FoliageFilters(target).Exists(filter => IsBaked(filter.sharedMesh) &&
                                                           OriginalMesh(target, filter) != null);
        }

        internal static void Clear(HBStylePlant plant)
        {
            var target = Target(plant);

            Undo.RecordObject(target, "Clear Plant Bake");

            foreach (var filter in FoliageFilters(target))
            {
                var original = IsBaked(filter.sharedMesh) ? OriginalMesh(target, filter) : null;

                if (original != null)
                    SetMesh(filter, original, "Clear Plant Bake");
            }

            target.bakedMeshes.Clear();
            target.asset = null;
            SavePlant(target);

            if (plant != target)
                StylePlantSystem.Add(plant);
        }

        static HBStylePlantAsset WriteAsset(HBStylePlant target, List<MeshFilter> filters, string path, float sway)
        {
            var root = target.transform;
            var draws = new List<HBStylePlantDraw>();
            var bounds = new Bounds();

            foreach (var filter in filters)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                var materials = renderer.sharedMaterials;

                if (!Shown(renderer, root) || !System.Array.TrueForAll(materials, IsPlant))
                    continue;

                var local = ToRoot(root, filter.transform);

                for (var subMesh = 0; subMesh < filter.sharedMesh.subMeshCount; subMesh++)
                {
                    var material = SubMeshMaterial(materials, subMesh);

                    material.enableInstancing = true;
                    EditorUtility.SetDirty(material);

                    draws.Add(new HBStylePlantDraw
                    {
                        mesh = filter.sharedMesh,
                        submesh = subMesh,
                        material = material,
                        local = local,
                        shadows = renderer.shadowCastingMode
                    });
                }

                var mesh = filter.sharedMesh.bounds;

                for (var corner = 0; corner < 8; corner++)
                {
                    var point = mesh.center + Vector3.Scale(mesh.extents, Corner(corner));

                    if (draws.Count == filter.sharedMesh.subMeshCount && corner == 0)
                        bounds = new Bounds(local.MultiplyPoint3x4(point), Vector3.zero);
                    else
                        bounds.Encapsulate(local.MultiplyPoint3x4(point));
                }
            }

            var asset = AssetDatabase.LoadAssetAtPath<HBStylePlantAsset>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HBStylePlantAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var centre = bounds.center;
            var sphere = new Vector4(centre.x, centre.y, centre.z, bounds.extents.magnitude + sway);

            asset.Set(draws.ToArray(), asset.billboardMesh, asset.billboardMaterial, sphere);
            EditorUtility.SetDirty(asset);

            return asset;
        }

        static Vector3 Corner(int corner)
        {
            return new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                               (corner & 4) == 0 ? -1f : 1f);
        }

        static bool Shown(MeshRenderer renderer, Transform root)
        {
            if (!renderer.enabled)
                return false;

            for (var parent = renderer.transform; parent != null; parent = parent.parent)
            {
                if (!parent.gameObject.activeSelf)
                    return false;

                if (parent == root)
                    break;
            }

            return true;
        }

        static HBStylePlant Target(HBStylePlant plant)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(plant);

            return source != null ? source : plant;
        }

        static Mesh OriginalMesh(HBStylePlant target, MeshFilter filter)
        {
            if (!IsBaked(filter.sharedMesh))
                return filter.sharedMesh;

            var entry = target.bakedMeshes.Find(item => item.filter == filter);

            if (entry.source != null)
                return entry.source;

            var source = PrefabUtility.GetCorrespondingObjectFromSource(filter);

            return source != null && source.sharedMesh != null && !IsBaked(source.sharedMesh)
                ? source.sharedMesh
                : null;
        }

        static void SetMesh(MeshFilter filter, Mesh mesh, string undoName)
        {
            Undo.RecordObject(filter, undoName);
            filter.sharedMesh = mesh;

            if (PrefabUtility.IsPartOfPrefabInstance(filter))
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);

            EditorUtility.SetDirty(filter);
        }

        static void SavePlant(HBStylePlant target)
        {
            EditorUtility.SetDirty(target);

            if (EditorUtility.IsPersistent(target))
                PrefabUtility.SavePrefabAsset(target.transform.root.gameObject);
            else if (PrefabStageUtility.GetPrefabStage(target.gameObject) is { } stage)
                EditorSceneManager.MarkSceneDirty(stage.scene);
        }

        static List<MeshFilter> FoliageFilters(HBStylePlant plant)
        {
            var filters = new List<MeshFilter>();

            foreach (var filter in plant.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.TryGetComponent<MeshRenderer>(out var renderer))
                    continue;

                foreach (var material in renderer.sharedMaterials)
                {
                    if (IsPlant(material))
                    {
                        filters.Add(filter);
                        break;
                    }
                }
            }

            return filters;
        }

        static bool IsFoliage(Material material)
        {
            return material != null && material.shader != null && material.shader.name == FoliageShader;
        }

        static bool IsPlant(Material material)
        {
            return IsFoliage(material) || (material != null && material.shader != null &&
                                           material.shader.name == BodyShader);
        }

        static Material SubMeshMaterial(Material[] materials, int subMesh)
        {
            return materials.Length == 0 ? null : materials[Mathf.Min(subMesh, materials.Length - 1)];
        }

        static bool[] MarkVertices(Mesh mesh, Material[] materials, System.Func<Material, bool> wanted)
        {
            var marks = new bool[mesh.vertexCount];

            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (!wanted(SubMeshMaterial(materials, subMesh)))
                    continue;

                foreach (var index in mesh.GetIndices(subMesh))
                    marks[index] = true;
            }

            return marks;
        }

        static Matrix4x4 ToRoot(Transform root, Transform child)
        {
            return root.worldToLocalMatrix * child.localToWorldMatrix;
        }

        static List<Egg> FitCanopy(List<MeshFilter> filters, Transform root, out float lowest, out float highest)
        {
            var points = new List<Vector3>();

            lowest = float.MaxValue;
            highest = float.MinValue;

            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                var toRoot = ToRoot(root, filter.transform);
                var materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
                var plantVertices = MarkVertices(mesh, materials, IsPlant);
                var leaves = MarkVertices(mesh, materials, IsFoliage);
                var vertices = mesh.vertices;

                for (var i = 0; i < vertices.Length; i++)
                {
                    if (!plantVertices[i])
                        continue;

                    var point = toRoot.MultiplyPoint3x4(vertices[i]);

                    lowest = Mathf.Min(lowest, point.y);
                    highest = Mathf.Max(highest, point.y);

                    if (leaves[i])
                        points.Add(point);
                }
            }

            return points.Count == 0 ? new List<Egg>() : FitClusters(Thin(points, MostSamples));
        }

        static List<Vector3> Thin(List<Vector3> points, int most)
        {
            if (points.Count <= most)
                return points;

            var thinned = new List<Vector3>(most);

            for (var i = 0; i < most; i++)
                thinned.Add(points[(int)((long)i * points.Count / most)]);

            return thinned;
        }

        static Bounds BoundsOf(List<Vector3> points)
        {
            var bounds = new Bounds(points[0], Vector3.zero);

            foreach (var point in points)
                bounds.Encapsulate(point);

            return bounds;
        }

        static List<Egg> FitClusters(List<Vector3> points)
        {
            var size = BoundsOf(points).size;
            var reach = Mathf.Max(ShiftReach * Mathf.Max(size.y, 0.5f * Mathf.Max(size.x, size.z)), Epsilon);
            var modes = new List<Vector3>();

            foreach (var seed in Thin(points, MostSeeds))
            {
                var mode = Climb(seed, points, reach);

                if (!modes.Exists(other => (other - mode).sqrMagnitude < reach * reach))
                    modes.Add(mode);
            }

            var members = Assign(points, modes);

            if (modes.Count > 1)
            {
                var least = points.Count * LeastCluster;

                for (var i = modes.Count - 1; i >= 0 && modes.Count > 1; i--)
                {
                    if (members[i].Count < least)
                        modes.RemoveAt(i);
                }

                members = Assign(points, modes);
            }

            var eggs = new List<Egg>(modes.Count);

            foreach (var cluster in members)
            {
                if (cluster.Count > 0)
                    eggs.Add(new Egg(BoundsOf(cluster)));
            }

            return eggs;
        }

        static Vector3 Climb(Vector3 start, List<Vector3> points, float reach)
        {
            var mode = start;
            var reach2 = reach * reach;

            for (var round = 0; round < ShiftRounds; round++)
            {
                var sum = Vector3.zero;
                var count = 0;

                foreach (var point in points)
                {
                    if ((point - mode).sqrMagnitude <= reach2)
                    {
                        sum += point;
                        count++;
                    }
                }

                if (count == 0)
                    break;

                var moved = sum / count;
                var settled = (moved - mode).sqrMagnitude < reach2 * Epsilon;

                mode = moved;

                if (settled)
                    break;
            }

            return mode;
        }

        static List<List<Vector3>> Assign(List<Vector3> points, List<Vector3> modes)
        {
            var members = new List<List<Vector3>>(modes.Count);

            foreach (var _ in modes)
                members.Add(new List<Vector3>());

            foreach (var point in points)
            {
                var nearest = 0;
                var best = float.MaxValue;

                for (var i = 0; i < modes.Count; i++)
                {
                    var gap = (point - modes[i]).sqrMagnitude;

                    if (gap < best)
                    {
                        best = gap;
                        nearest = i;
                    }
                }

                members[nearest].Add(point);
            }

            return members;
        }

        static Vector3 ReadCanopy(List<Egg> eggs, Vector3 point)
        {
            var sum = Vector3.zero;

            foreach (var egg in eggs)
            {
                var depth = egg.Depth(point, out var eggNormal);
                var depth2 = depth * depth;
                var weight = 1f / (depth2 * depth2 + Epsilon);

                sum += eggNormal.normalized * weight;
            }

            return sum.normalized;
        }

        static Mesh BakeMesh(MeshFilter filter, Transform root, List<Egg> eggs, float lowest, float highest,
                             float sway)
        {
            var mesh = filter.sharedMesh;
            var toRoot = ToRoot(root, filter.transform);
            var toObject = toRoot.transpose;
            var materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            var plantVertices = MarkVertices(mesh, materials, IsPlant);
            var leaves = MarkVertices(mesh, materials, IsFoliage);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var plantData = new Vector3[vertices.Length];
            var reach = Mathf.Max(highest - lowest, Epsilon);
            var baked = Object.Instantiate(mesh);

            if (mesh.tangents.Length != vertices.Length)
                baked.RecalculateTangents();

            var tangents = baked.tangents;

            for (var i = 0; i < vertices.Length; i++)
            {
                if (!plantVertices[i])
                    continue;

                var point = toRoot.MultiplyPoint3x4(vertices[i]);
                plantData[i] = new Vector3(Mathf.Clamp01((point.y - lowest) / reach), sway, BakeMark);

                if (!leaves[i])
                    continue;

                var canopy = i < normals.Length ? normals[i] : Vector3.up;

                if (eggs.Count > 0)
                    canopy = toObject.MultiplyVector(ReadCanopy(eggs, point)).normalized;

                tangents[i] = new Vector4(canopy.x, canopy.y, canopy.z, 0f);
            }

            baked.SetTangents(tangents);
            baked.SetUVs(3, plantData);

            return baked;
        }

        static Mesh Store(Mesh mesh, string path)
        {
            mesh.name = Path.GetFileNameWithoutExtension(path);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            var plantData = new List<Vector3>();

            mesh.GetUVs(3, plantData);
            EditorUtility.CopySerialized(mesh, existing);
            existing.SetTangents(mesh.tangents);
            existing.SetUVs(3, plantData);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);

            return existing;
        }

        static string OutputFolder(HBStylePlant plant, Mesh mesh)
        {
            var path = AssetDatabase.GetAssetPath(plant);

            if (string.IsNullOrEmpty(path) && PrefabStageUtility.GetPrefabStage(plant.gameObject) is { } stage)
                path = stage.assetPath;

            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(mesh);

            return path.StartsWith("Assets/") ? Path.GetDirectoryName(path).Replace('\\', '/') : "Assets";
        }
    }
}
