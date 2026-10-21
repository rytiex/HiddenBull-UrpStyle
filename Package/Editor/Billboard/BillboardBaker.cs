using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class BillboardBaker
    {
        public sealed class Options
        {
            public float switchDistance;
            public float cullDistance;
            public string folder;
            public string name;
        }

        const int Frames = 8;
        const int TileSize = 512;
        const int DilateSteps = 8;
        const string CaptureShaderName = "Hidden/HB URP Style/Billboard Capture";
        const string BillboardShaderName = "Hidden/HiddenBull/Style Billboard";
        const string BillboardName = "Billboard";
        const string FoliageShaderName = "HB URP Style/Foliage";
        const float DefaultSwitch = 25f;
        const float DefaultCull = 300f;

        static readonly int s_WorldToRootId = Shader.PropertyToID("_HB_CaptureWorldToRoot");
        static readonly int s_BoundsMinId = Shader.PropertyToID("_HB_CaptureBoundsMin");
        static readonly int s_BoundsMaxId = Shader.PropertyToID("_HB_CaptureBoundsMax");

        static readonly string[] s_StyleProperties = { "_DiffuseWrap", "_DiffuseSoftness", "_HalfTone" };

        [MenuItem("CONTEXT/HBStyleBillboard/Bake Billboard")]
        static void BakeComponent(MenuCommand command)
        {
            if (command.context is HBStyleBillboard billboard)
                BillboardBakerWindow.Open(billboard.gameObject);
        }

        public static Options DefaultOptions(GameObject root)
        {
            var options = new Options
            {
                switchDistance = DefaultSwitch,
                cullDistance = DefaultCull,
                folder = OutputFolder(root),
                name = root.name.StartsWith("PREFAB_") ? root.name.Substring("PREFAB_".Length) : root.name
            };

            if (root.TryGetComponent<HBStyleBillboard>(out var billboard) && billboard.asset != null)
            {
                options.switchDistance = billboard.asset.switchDistance;
                options.cullDistance = billboard.asset.cullDistance;
            }

            return options;
        }

        public static void Bake(GameObject root, Options options)
        {
            if (!EditorUtility.IsPersistent(root))
            {
                BakeInto(root, options, true);
                RefreshTerrains();
                return;
            }

            var path = AssetDatabase.GetAssetPath(root);
            var contents = PrefabUtility.LoadPrefabContents(path);

            try
            {
                if (BakeInto(contents, options, false))
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            RefreshTerrains();
        }

        static void RefreshTerrains()
        {
            foreach (var owner in HBStyleTerrain.active)
                owner.RefreshTrees();
        }

        static bool BakeInto(GameObject root, Options options, bool record)
        {
            var lodGroup = root.GetComponent<LODGroup>();
            var existing = FindBillboard(root);
            var renderers = SourceRenderers(root, lodGroup, existing);

            if (renderers.Count == 0)
            {
                EditorUtility.DisplayDialog("Bake Billboard", $"{root.name} has no mesh renderers to capture.", "OK");
                return false;
            }

            var captureShader = Shader.Find(CaptureShaderName);
            var billboardShader = Shader.Find(BillboardShaderName);

            if (captureShader == null || billboardShader == null)
            {
                EditorUtility.DisplayDialog("Bake Billboard", "The billboard shaders are missing.", "OK");
                return false;
            }

            var hidden = renderers.Where(renderer => renderer.forceRenderingOff).ToList();

            foreach (var renderer in hidden)
                renderer.forceRenderingOff = false;

            var bounds = renderers[0].bounds;

            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            var transform = root.transform;
            var scale = transform.lossyScale;
            var horizontal = Mathf.Max(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)), 1e-4f);
            var vertical = Mathf.Max(Mathf.Abs(scale.y), 1e-4f);

            var sizeLocal = Mathf.Max(new Vector2(bounds.size.x, bounds.size.z).magnitude / horizontal,
                                      bounds.size.y / vertical);
            var centerLocal = transform.InverseTransformPoint(bounds.center);
            var extent = new Vector2(sizeLocal * horizontal, sizeLocal * vertical);

            Shader.SetGlobalVector(s_BoundsMinId, bounds.min);
            Shader.SetGlobalVector(s_BoundsMaxId, bounds.max);

            var albedo = new Texture2D(TileSize * Frames, TileSize, TextureFormat.RGBA32, false, false);
            var normal = new Texture2D(TileSize * Frames, TileSize, TextureFormat.RGBA32, false, true);
            var captures = new Dictionary<Material, Material>();

            try
            {
                try
                {
                    Capture(renderers, transform, bounds.center, extent, captureShader, captures, albedo, normal);
                }
                finally
                {
                    foreach (var renderer in hidden)
                        renderer.forceRenderingOff = true;
                }

                Dilate(albedo, normal);

                var folder = options.folder;
                var name = options.name;

                Directory.CreateDirectory(folder);

                var albedoAsset = SaveTexture(albedo, $"{folder}/TEX_Billboard_{name}_Albedo.png", true);
                var normalAsset = SaveTexture(normal, $"{folder}/TEX_Billboard_{name}_Normal.png", false);

                var mesh = SaveMesh($"{folder}/MESH_Billboard_{name}.asset", centerLocal, sizeLocal);
                var material = SaveMaterial($"{folder}/MAT_Billboard_{name}.mat", billboardShader, albedoAsset,
                                            normalAsset, centerLocal, sizeLocal, renderers);
                var data = SaveData($"{folder}/DATA_Billboard_{name}.asset", root, renderers, mesh, material,
                                    options);

                RemoveOldLods(lodGroup, existing, record);
                Attach(root, data, record);

                if (record)
                    EditorSceneManager.MarkSceneDirty(root.scene);
            }
            finally
            {
                foreach (var capture in captures.Values)
                    Object.DestroyImmediate(capture);

                Object.DestroyImmediate(albedo);
                Object.DestroyImmediate(normal);
            }

            return true;
        }

        static Renderer FindBillboard(GameObject root)
        {
            var child = root.transform.Find(BillboardName);

            if (child == null || !child.TryGetComponent<MeshRenderer>(out var renderer))
                return null;

            var material = renderer.sharedMaterial;

            return material != null && material.shader != null && material.shader.name == BillboardShaderName
                ? renderer
                : null;
        }

        static List<Renderer> SourceRenderers(GameObject root, LODGroup lodGroup, Renderer existing)
        {
            IEnumerable<Renderer> candidates = lodGroup != null && lodGroup.lodCount > 0
                ? lodGroup.GetLODs()[0].renderers
                : root.GetComponentsInChildren<Renderer>();

            return candidates.Where(renderer => renderer is MeshRenderer && renderer != existing && renderer.enabled &&
                                                renderer.TryGetComponent<MeshFilter>(out var filter) &&
                                                filter.sharedMesh != null)
                             .ToList();
        }

        static void Capture(List<Renderer> renderers, Transform root, Vector3 center, Vector2 extent, Shader shader,
                            Dictionary<Material, Material> captures, Texture2D albedo, Texture2D normal)
        {
            var albedoTarget = RenderTexture.GetTemporary(new RenderTextureDescriptor(TileSize, TileSize,
                RenderTextureFormat.ARGB32, 24) { sRGB = true });
            var normalTarget = RenderTexture.GetTemporary(new RenderTextureDescriptor(TileSize, TileSize,
                RenderTextureFormat.ARGB32, 24) { sRGB = false });

            var previous = RenderTexture.active;
            var distance = Mathf.Max(extent.x, extent.y) * 2f;
            var projection = Matrix4x4.Ortho(-extent.x * 0.5f, extent.x * 0.5f, -extent.y * 0.5f, extent.y * 0.5f,
                                             0.01f, distance * 2f);

            try
            {
                for (var frame = 0; frame < Frames; frame++)
                {
                    var angle = frame * Mathf.PI * 2f / Frames;
                    var direction = root.TransformDirection(new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)))
                                        .normalized;
                    var rotation = Quaternion.LookRotation(-direction, root.up);
                    var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) *
                               Matrix4x4.TRS(center + direction * distance, rotation, Vector3.one).inverse;

                    Render(renderers, root, view, projection, shader, captures, albedoTarget, 0);
                    Read(albedoTarget, albedo, frame);

                    Render(renderers, root, view, projection, shader, captures, normalTarget, 1);
                    Read(normalTarget, normal, frame);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(albedoTarget);
                RenderTexture.ReleaseTemporary(normalTarget);
            }
        }

        static void Render(List<Renderer> renderers, Transform root, Matrix4x4 view, Matrix4x4 projection,
                           Shader shader, Dictionary<Material, Material> captures, RenderTexture target, int pass)
        {
            using var command = new CommandBuffer { name = "Bake Billboard" };

            command.SetRenderTarget(target);
            command.ClearRenderTarget(true, true, Color.clear);
            command.SetViewProjectionMatrices(view, projection);
            command.SetGlobalMatrix(s_WorldToRootId, root.worldToLocalMatrix);

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;

                for (var submesh = 0; submesh < materials.Length; submesh++)
                {
                    if (materials[submesh] != null)
                        command.DrawRenderer(renderer, CaptureMaterial(materials[submesh], shader, captures), submesh,
                                             pass);
                }
            }

            Graphics.ExecuteCommandBuffer(command);
        }

        static Material CaptureMaterial(Material source, Shader shader, Dictionary<Material, Material> captures)
        {
            if (captures.TryGetValue(source, out var capture))
                return capture;

            capture = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            if (source.HasProperty("_BaseMap"))
            {
                capture.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                capture.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                capture.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
            }

            if (source.HasProperty("_BaseColor"))
                capture.SetColor("_BaseColor", source.GetColor("_BaseColor"));

            var foliage = source.shader != null && source.shader.name == FoliageShaderName;
            var canopy = foliage && source.HasProperty("_CanopyShape") ? source.GetFloat("_CanopyShape") : 1f;
            capture.SetFloat("_Canopy", foliage ? canopy : 0f);

            var clipped = source.IsKeywordEnabled("_ALPHATEST_ON") && source.HasProperty("_Cutoff");
            capture.SetFloat("_Cutoff", clipped ? source.GetFloat("_Cutoff") : -1f);

            if (source.HasProperty("_BumpMap") && source.IsKeywordEnabled("_NORMALMAP"))
            {
                capture.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                capture.SetFloat("_BumpScale", source.HasProperty("_BumpScale") ? source.GetFloat("_BumpScale") : 1f);
            }

            captures.Add(source, capture);

            return capture;
        }

        static void Read(RenderTexture source, Texture2D atlas, int frame)
        {
            RenderTexture.active = source;
            atlas.ReadPixels(new Rect(0f, 0f, TileSize, TileSize), frame * TileSize, 0, false);
        }

        static void Dilate(Texture2D albedo, Texture2D normal)
        {
            var width = albedo.width;
            var height = albedo.height;

            var colours = albedo.GetPixels32();
            var normals = normal.GetPixels32();
            var filled = colours.Select(colour => colour.a > 0).ToArray();
            var next = new bool[filled.Length];

            for (var step = 0; step < DilateSteps; step++)
            {
                System.Array.Copy(filled, next, filled.Length);

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var index = y * width + x;

                        if (filled[index])
                            continue;

                        int r = 0, g = 0, b = 0, nr = 0, ng = 0, nb = 0, na = 0, count = 0;
                        var tile = x / TileSize;

                        for (var dy = -1; dy <= 1; dy++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var sx = x + dx;
                                var sy = y + dy;

                                if (sx < 0 || sy < 0 || sx >= width || sy >= height || sx / TileSize != tile)
                                    continue;

                                var neighbour = sy * width + sx;

                                if (!filled[neighbour])
                                    continue;

                                r += colours[neighbour].r;
                                g += colours[neighbour].g;
                                b += colours[neighbour].b;
                                nr += normals[neighbour].r;
                                ng += normals[neighbour].g;
                                nb += normals[neighbour].b;
                                na += normals[neighbour].a;
                                count++;
                            }
                        }

                        if (count == 0)
                            continue;

                        colours[index] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
                        normals[index] = new Color32((byte)(nr / count), (byte)(ng / count), (byte)(nb / count),
                                                     (byte)(na / count));
                        next[index] = true;
                    }
                }

                (filled, next) = (next, filled);
            }

            albedo.SetPixels32(colours);
            albedo.Apply(false);
            normal.SetPixels32(normals);
            normal.Apply(false);
        }

        static string OutputFolder(GameObject root)
        {
            var prefab = EditorUtility.IsPersistent(root)
                ? AssetDatabase.GetAssetPath(root)
                : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            var source = string.IsNullOrEmpty(prefab) ? root.scene.path : prefab;
            var folder = string.IsNullOrEmpty(source) ? null : Path.GetDirectoryName(source);

            return string.IsNullOrEmpty(folder) ? "Assets" : folder.Replace('\\', '/');
        }

        static Texture2D SaveTexture(Texture2D texture, string path, bool colour)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = colour;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.mipmapEnabled = true;
                importer.mipMapsPreserveCoverage = colour;
                importer.alphaTestReferenceValue = 0.5f;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Mesh SaveMesh(string path, Vector3 center, float size)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var created = mesh == null;

            if (created)
                mesh = new Mesh();

            mesh.Clear();
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.vertices = new[] { center, center, center, center };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.bounds = new Bounds(center, Vector3.one * size);

            if (created)
                AssetDatabase.CreateAsset(mesh, path);
            else
                EditorUtility.SetDirty(mesh);

            return mesh;
        }

        static Material SaveMaterial(string path, Shader shader, Texture2D albedo, Texture2D normal, Vector3 center,
                                     float size, List<Renderer> sources)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var created = material == null;

            if (created)
            {
                material = new Material(shader);

                var style = sources.SelectMany(renderer => renderer.sharedMaterials)
                                   .FirstOrDefault(source => source != null && source.HasProperty("_DiffuseWrap"));

                if (style != null)
                {
                    foreach (var property in s_StyleProperties.Where(style.HasProperty))
                        material.SetFloat(property, style.GetFloat(property));
                }
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_NormalAtlas", normal);
            material.SetFloat("_Frames", Frames);
            material.SetVector("_BillboardCenter", center);
            material.SetFloat("_BillboardSize", size);
            material.enableInstancing = true;

            if (created)
                AssetDatabase.CreateAsset(material, path);
            else
                EditorUtility.SetDirty(material);

            AssetDatabase.SaveAssets();

            return material;
        }

        static HBStyleBillboardAsset SaveData(string path, GameObject root, List<Renderer> renderers, Mesh mesh,
                                            Material material, Options options)
        {
            var data = AssetDatabase.LoadAssetAtPath<HBStyleBillboardAsset>(path);
            var created = data == null;

            if (created)
                data = ScriptableObject.CreateInstance<HBStyleBillboardAsset>();

            var toRoot = root.transform.worldToLocalMatrix;
            var draws = new List<HBStyleBillboardDraw>();
            var least = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var most = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var renderer in renderers)
            {
                var source = renderer.GetComponent<MeshFilter>().sharedMesh;
                var materials = renderer.sharedMaterials;
                var local = toRoot * renderer.transform.localToWorldMatrix;

                for (var submesh = 0; submesh < Mathf.Min(materials.Length, source.subMeshCount); submesh++)
                {
                    if (materials[submesh] == null)
                        continue;

                    draws.Add(new HBStyleBillboardDraw
                    {
                        mesh = source,
                        submesh = submesh,
                        material = materials[submesh],
                        local = local,
                        shadows = renderer.shadowCastingMode
                    });
                }

                var bounds = source.bounds;

                for (var corner = 0; corner < 8; corner++)
                {
                    var point = local.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));

                    least = Vector3.Min(least, point);
                    most = Vector3.Max(most, point);
                }
            }

            var centre = (least + most) * 0.5f;

            data.switchDistance = options.switchDistance;
            data.cullDistance = options.cullDistance;
            data.Set(draws.ToArray(), mesh, material,
                     new Vector4(centre.x, centre.y, centre.z, (most - least).magnitude * 0.5f));

            if (created)
                AssetDatabase.CreateAsset(data, path);
            else
                EditorUtility.SetDirty(data);

            AssetDatabase.SaveAssets();

            return data;
        }

        static void RemoveOldLods(LODGroup lodGroup, Renderer existing, bool record)
        {
            if (existing == null)
                return;

            if (lodGroup != null && lodGroup.GetLODs().Any(lod => lod.renderers.Contains(existing)))
            {
                if (record)
                    Undo.DestroyObjectImmediate(lodGroup);
                else
                    Object.DestroyImmediate(lodGroup);
            }

            if (record)
                Undo.DestroyObjectImmediate(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        static void Attach(GameObject root, HBStyleBillboardAsset data, bool record)
        {
            if (!root.TryGetComponent<HBStyleBillboard>(out var billboard))
                billboard = record ? Undo.AddComponent<HBStyleBillboard>(root) : root.AddComponent<HBStyleBillboard>();
            else if (record)
                Undo.RecordObject(billboard, "Bake Billboard");

            billboard.asset = data;
            EditorUtility.SetDirty(billboard);
        }
    }
}
