using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleBillboardBaker
    {
        const string CaptureShader = "Hidden/HB URP Style/Foliage Capture";
        const string BillboardShader = "Hidden/HB URP Style/Foliage Billboard";
        const string LeafShader = "HB URP Style/Foliage";

        const int Frames = 8;
        const int TileSize = 512;
        const int TopSize = 128;
        const int ShadowFrames = 8;
        const int ShadowTile = 128;
        const int ShadowSample = ShadowTile * 2;
        const float ShadowSpread = 16f;
        const float ShadowDiagonal = 1.4142f;
        const int DilateSteps = 8;
        const int AlbedoPass = 0;
        const int NormalPass = 1;
        const int TopPass = 2;
        const float MinSize = 1e-3f;

        static readonly int s_TopId = Shader.PropertyToID("_HB_CaptureTop");
        static readonly int s_MinId = Shader.PropertyToID("_HB_CaptureMin");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_CaptureSize");

        static readonly string[] s_StyleProperties =
        {
            "_DiffuseWrap", "_DiffuseSoftness", "_HalfTone", "_BakedTones", "_BakedSoftness", "_Wetness", "_SnowCover"
        };

        internal readonly struct Result
        {
            public readonly Mesh mesh;
            public readonly Material material;

            public Result(Mesh mesh, Material material)
            {
                this.mesh = mesh;
                this.material = material;
            }
        }

        internal static Result Bake(IReadOnlyList<HBStylePlantDraw> draws, Bounds bounds, string folder, string name,
                                    float sway)
        {
            var captureShader = Shader.Find(CaptureShader);
            var billboardShader = Shader.Find(BillboardShader);

            if (captureShader == null || billboardShader == null || draws.Count == 0)
                return default;

            var size = Mathf.Max(new Vector2(bounds.size.x, bounds.size.z).magnitude, bounds.size.y);
            var albedo = new Texture2D(TileSize * Frames, TileSize, TextureFormat.RGBA32, false, false);
            var normal = new Texture2D(TileSize * Frames, TileSize, TextureFormat.RGBA32, false, true);
            var shadow = new byte[ShadowFrames * ShadowTile * ShadowFrames * ShadowTile];
            var shadowSize = Mathf.Max(bounds.size.magnitude, MinSize);
            var captures = new Dictionary<Material, Material>();

            try
            {
                foreach (var draw in draws)
                    CaptureMaterial(draw.material, captureShader, captures);

                Capture(draws, captures, bounds, size, shadowSize, albedo, normal, shadow);
                Dilate(albedo, normal);

                var albedoAsset = SaveTexture(albedo, $"{folder}/TEX_Billboard_{name}_Albedo.png", true);
                var normalAsset = SaveTexture(normal, $"{folder}/TEX_Billboard_{name}_Normal.png", false);
                var shadowAsset = SaveShadow(shadow, $"{folder}/TEX_Billboard_{name}_Shadow.png");
                var mesh = SaveMesh($"{folder}/MESH_Billboard_{name}.asset", bounds.center, size);
                var material = SaveMaterial($"{folder}/MAT_Billboard_{name}.mat", billboardShader, draws, albedoAsset,
                                            normalAsset, shadowAsset, bounds, size, shadowSize, sway);

                return new Result(mesh, material);
            }
            finally
            {
                foreach (var capture in captures.Values)
                    Object.DestroyImmediate(capture);

                Object.DestroyImmediate(albedo);
                Object.DestroyImmediate(normal);
            }
        }

        static void Capture(IReadOnlyList<HBStylePlantDraw> draws, Dictionary<Material, Material> captures,
                            Bounds bounds, float size, float shadowSize, Texture2D albedo, Texture2D normal,
                            byte[] shadow)
        {
            var albedoTarget = RenderTexture.GetTemporary(new RenderTextureDescriptor(TileSize, TileSize,
                RenderTextureFormat.ARGB32, 24) { sRGB = true });
            var normalTarget = RenderTexture.GetTemporary(new RenderTextureDescriptor(TileSize, TileSize,
                RenderTextureFormat.ARGB32, 24) { sRGB = false });
            var top = RenderTexture.GetTemporary(TopSize, TopSize, 0, RenderTextureFormat.RHalf,
                                                 RenderTextureReadWrite.Linear);

            var previous = RenderTexture.active;
            var distance = size * 2f;
            var projection = Matrix4x4.Ortho(-size * 0.5f, size * 0.5f, -size * 0.5f, size * 0.5f, 0.01f,
                                             distance * 2f);

            top.filterMode = FilterMode.Bilinear;
            top.wrapMode = TextureWrapMode.Clamp;

            try
            {
                CaptureTop(draws, captures, bounds, top);

                for (var frame = 0; frame < Frames; frame++)
                {
                    var angle = frame * Mathf.PI * 2f / Frames;
                    var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    var rotation = Quaternion.LookRotation(-direction, Vector3.up);
                    var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) *
                               Matrix4x4.TRS(bounds.center + direction * distance, rotation, Vector3.one).inverse;

                    Render(draws, captures, view, projection, albedoTarget, AlbedoPass);
                    Read(albedoTarget, albedo, frame);

                    Render(draws, captures, view, projection, normalTarget, NormalPass);
                    Read(normalTarget, normal, frame);
                }

                CaptureShadow(draws, captures, bounds, shadowSize, shadow);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(albedoTarget);
                RenderTexture.ReleaseTemporary(normalTarget);
                RenderTexture.ReleaseTemporary(top);
            }
        }

        static void CaptureShadow(IReadOnlyList<HBStylePlantDraw> draws, Dictionary<Material, Material> captures,
                                  Bounds bounds, float size, byte[] shadow)
        {
            var target = RenderTexture.GetTemporary(new RenderTextureDescriptor(ShadowSample, ShadowSample,
                RenderTextureFormat.ARGB32, 24));
            var read = new Texture2D(ShadowSample, ShadowSample, TextureFormat.RGBA32, false, true);
            var outside = new float[ShadowSample * ShadowSample];
            var inside = new float[ShadowSample * ShadowSample];
            var distance = size * 2f;
            var projection = Matrix4x4.Ortho(-size * 0.5f, size * 0.5f, -size * 0.5f, size * 0.5f, 0.01f,
                                             distance * 2f);

            try
            {
                for (var row = 0; row < ShadowFrames; row++)
                {
                    for (var column = 0; column < ShadowFrames; column++)
                    {
                        var direction = ShadowDirection(column, row);
                        var rotation = Quaternion.LookRotation(-direction, Vector3.up);
                        var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) *
                                   Matrix4x4.TRS(bounds.center + direction * distance, rotation, Vector3.one).inverse;

                        Render(draws, captures, view, projection, target, AlbedoPass);
                        RenderTexture.active = target;
                        read.ReadPixels(new Rect(0f, 0f, ShadowSample, ShadowSample), 0, 0, false);
                        StoreShadow(read.GetPixels32(), outside, inside, shadow, column, row);
                    }
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(read);
            }
        }

        static Vector3 ShadowDirection(int column, int row)
        {
            var x = column * 2f / (ShadowFrames - 1) - 1f;
            var y = row * 2f / (ShadowFrames - 1) - 1f;
            var spreadX = (x + y) * 0.5f;
            var spreadZ = (x - y) * 0.5f;

            return new Vector3(spreadX, 1f - Mathf.Abs(spreadX) - Mathf.Abs(spreadZ), spreadZ).normalized;
        }

        static void StoreShadow(Color32[] pixels, float[] outside, float[] inside, byte[] shadow, int column, int row)
        {
            for (var index = 0; index < pixels.Length; index++)
            {
                var solid = pixels[index].a > 127;

                outside[index] = solid ? 0f : ShadowSample;
                inside[index] = solid ? ShadowSample : 0f;
            }

            Sweep(outside, 1);
            Sweep(outside, -1);
            Sweep(inside, 1);
            Sweep(inside, -1);

            var side = ShadowFrames * ShadowTile;

            for (var y = 0; y < ShadowTile; y++)
            {
                for (var x = 0; x < ShadowTile; x++)
                {
                    var sample = y * 2 * ShadowSample + x * 2;
                    var away = outside[sample] - inside[sample];
                    var shape = Mathf.Clamp01(0.5f - away * 0.5f / ShadowSpread);

                    shadow[(row * ShadowTile + y) * side + column * ShadowTile + x] =
                        (byte)Mathf.RoundToInt(shape * 255f);
                }
            }
        }

        static void Sweep(float[] field, int sign)
        {
            var first = sign > 0 ? 0 : ShadowSample - 1;

            for (var line = 0; line < ShadowSample; line++)
            {
                var y = first + sign * line;
                var last = y - sign;

                for (var step = 0; step < ShadowSample; step++)
                {
                    var x = first + sign * step;
                    var back = x - sign;
                    var ahead = x + sign;
                    var index = y * ShadowSample + x;
                    var least = field[index];

                    if (Inside(back))
                        least = Mathf.Min(least, field[y * ShadowSample + back] + 1f);

                    if (Inside(last))
                    {
                        least = Mathf.Min(least, field[last * ShadowSample + x] + 1f);

                        if (Inside(back))
                            least = Mathf.Min(least, field[last * ShadowSample + back] + ShadowDiagonal);

                        if (Inside(ahead))
                            least = Mathf.Min(least, field[last * ShadowSample + ahead] + ShadowDiagonal);
                    }

                    field[index] = least;
                }
            }
        }

        static bool Inside(int position)
        {
            return position >= 0 && position < ShadowSample;
        }

        static void CaptureTop(IReadOnlyList<HBStylePlantDraw> draws, Dictionary<Material, Material> captures,
                               Bounds bounds, RenderTexture top)
        {
            var size = Vector3.Max(bounds.size, Vector3.one * MinSize);

            using var command = new CommandBuffer { name = "Bake Billboard Top" };

            command.SetGlobalVector(s_MinId, bounds.min);
            command.SetGlobalVector(s_SizeId, size);
            command.SetRenderTarget(top);
            command.ClearRenderTarget(false, true, Color.clear);

            foreach (var draw in draws)
                command.DrawMesh(draw.mesh, draw.local, captures[draw.material], draw.submesh, TopPass);

            command.SetGlobalTexture(s_TopId, top);
            Graphics.ExecuteCommandBuffer(command);
        }

        static void Render(IReadOnlyList<HBStylePlantDraw> draws, Dictionary<Material, Material> captures,
                           Matrix4x4 view, Matrix4x4 projection, RenderTexture target, int pass)
        {
            using var command = new CommandBuffer { name = "Bake Billboard" };

            command.SetRenderTarget(target);
            command.ClearRenderTarget(true, true, Color.clear);
            command.SetViewProjectionMatrices(view, projection);

            foreach (var draw in draws)
                command.DrawMesh(draw.mesh, draw.local, captures[draw.material], draw.submesh, pass);

            Graphics.ExecuteCommandBuffer(command);
        }

        static void CaptureMaterial(Material source, Shader shader, Dictionary<Material, Material> captures)
        {
            if (captures.ContainsKey(source))
                return;

            var capture = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            if (source.IsKeywordEnabled("_HB_BASE_MAP"))
            {
                capture.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                capture.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                capture.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
            }

            capture.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            capture.SetFloat("_Cutoff", source.IsKeywordEnabled("_ALPHATEST_ON") ? source.GetFloat("_Cutoff") : -1f);

            if (source.shader.name == LeafShader)
            {
                capture.SetFloat("_Leaf", 1f);
                capture.SetFloat("_Canopy", source.GetFloat("_CanopyShape"));
            }
            else if (source.IsKeywordEnabled("_NORMALMAP"))
            {
                capture.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                capture.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
            }

            captures.Add(source, capture);
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
            var filled = new bool[colours.Length];
            var next = new bool[colours.Length];

            for (var index = 0; index < colours.Length; index++)
                filled[index] = colours[index].a > 0;

            for (var step = 0; step < DilateSteps; step++)
            {
                System.Array.Copy(filled, next, filled.Length);

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (!filled[y * width + x])
                            Grow(x, y, width, height, filled, next, colours, normals);
                    }
                }

                (filled, next) = (next, filled);
            }

            albedo.SetPixels32(colours);
            albedo.Apply(false);
            normal.SetPixels32(normals);
            normal.Apply(false);
        }

        static void Grow(int x, int y, int width, int height, bool[] filled, bool[] next, Color32[] colours,
                         Color32[] normals)
        {
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
                return;

            var index = y * width + x;

            colours[index] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
            normals[index] = new Color32((byte)(nr / count), (byte)(ng / count), (byte)(nb / count),
                                         (byte)(na / count));
            next[index] = true;
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

        static Texture2D SaveShadow(byte[] shadow, string path)
        {
            var side = ShadowFrames * ShadowTile;
            var texture = new Texture2D(side, side, TextureFormat.R8, false, true);

            texture.SetPixelData(shadow, 0);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var settings = new TextureImporterSettings();

                importer.ReadTextureSettings(settings);
                settings.textureType = TextureImporterType.SingleChannel;
                settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                settings.sRGBTexture = false;
                settings.mipmapEnabled = false;
                settings.wrapMode = TextureWrapMode.Clamp;
                importer.SetTextureSettings(settings);
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

        static Material SaveMaterial(string path, Shader shader, IReadOnlyList<HBStylePlantDraw> draws,
                                     Texture2D albedo, Texture2D normal, Texture2D shadow, Bounds bounds, float size,
                                     float shadowSize, float sway)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var created = material == null;

            if (created)
                material = new Material(shader);

            var style = StyleSource(draws);

            foreach (var property in s_StyleProperties)
                material.SetFloat(property, style.GetFloat(property));

            material.shader = shader;
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_NormalAtlas", normal);
            material.SetTexture("_ShadowAtlas", shadow);
            material.SetFloat("_ShadowSize", shadowSize);
            material.SetFloat("_Frames", Frames);
            material.SetVector("_BillboardCenter", bounds.center);
            material.SetFloat("_BillboardSize", size);
            material.SetFloat("_BillboardTop", bounds.max.y);
            material.SetFloat("_BillboardRadius", 0.5f * Mathf.Max(bounds.size.x, bounds.size.z));
            material.SetFloat("_BillboardSway", sway);
            material.enableInstancing = true;

            if (created)
                AssetDatabase.CreateAsset(material, path);
            else
                EditorUtility.SetDirty(material);

            return material;
        }

        static Material StyleSource(IReadOnlyList<HBStylePlantDraw> draws)
        {
            foreach (var draw in draws)
            {
                if (draw.material.shader.name == LeafShader)
                    return draw.material;
            }

            return draws[0].material;
        }
    }
}
