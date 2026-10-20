using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public static class StyleTerrainGpu
    {
        const string ShaderPath = "Packages/com.hiddenbull.urpstyle/Editor/Terrain/StyleTerrainRules.compute";
        const int RowFloats = 13;
        const int Group = 8;

        static readonly int s_RowsId = Shader.PropertyToID("_Rows");
        static readonly int s_HeightsId = Shader.PropertyToID("_Heights");
        static readonly int s_CurrentId = Shader.PropertyToID("_Current");
        static readonly int s_WrittenId = Shader.PropertyToID("_Written");
        static readonly int s_HandId = Shader.PropertyToID("_Hand");
        static readonly int s_ResultId = Shader.PropertyToID("_Result");
        static readonly int s_HandOutId = Shader.PropertyToID("_HandOut");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_OriginId = Shader.PropertyToID("_Origin");
        static readonly int s_CountsId = Shader.PropertyToID("_Counts");
        static readonly int s_AreaId = Shader.PropertyToID("_Area");
        static readonly int s_FlagsId = Shader.PropertyToID("_Flags");

        static ComputeShader s_Shader;
        static Texture2DArray s_Empty;

        public static bool supported => SystemInfo.supportsComputeShaders && Rules != null;

        static ComputeShader Rules
        {
            get
            {
                if (s_Shader == null)
                    s_Shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ShaderPath);

                return s_Shader;
            }
        }

        static Texture2DArray Empty
        {
            get
            {
                if (s_Empty == null)
                {
                    s_Empty = new Texture2DArray(1, 1, 1, TextureFormat.RGBA32, false, true)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    s_Empty.SetPixels32(new[] { new Color32(0, 0, 0, 0) }, 0);
                    s_Empty.Apply(false);
                }

                return s_Empty;
            }
        }

        public static RenderTexture NewTarget(int resolution, int slices)
        {
            var descriptor = new RenderTextureDescriptor(resolution, resolution, GraphicsFormat.R8G8B8A8_UNorm, 0)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = Mathf.Max(slices, 1),
                enableRandomWrite = true,
                msaaSamples = 1
            };

            var target = new RenderTexture(descriptor)
            {
                name = "HB Terrain Rules",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            target.Create();
            return target;
        }

        public static void Release(Object texture)
        {
            if (texture is RenderTexture target)
                target.Release();

            if (texture != null)
                Object.DestroyImmediate(texture);
        }

        public static Texture2DArray Upload(IList<byte[]> channels, int resolution)
        {
            var slices = Mathf.Max((channels.Count + 3) / 4, 1);
            var array = new Texture2DArray(resolution, resolution, slices, TextureFormat.RGBA32, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var size = resolution * resolution;
            var bytes = new byte[size * 4];

            for (var slice = 0; slice < slices; slice++)
            {
                System.Array.Clear(bytes, 0, bytes.Length);

                for (var part = 0; part < 4; part++)
                {
                    var index = slice * 4 + part;
                    var channel = index < channels.Count ? channels[index] : null;

                    if (channel == null)
                        continue;

                    for (var texel = 0; texel < size; texel++)
                        bytes[texel * 4 + part] = channel[texel];
                }

                array.SetPixelData(bytes, 0, slice);
            }

            array.Apply(false);
            return array;
        }

        static float Parent(List<HBStyleTerrainProfileLayer> rows, int index)
        {
            var variantOf = rows[index].variantOf;

            if (index == 0 || variantOf == null)
                return -1f;

            var parent = rows.FindIndex(row => row.layer == variantOf);
            return parent == index ? -1f : parent;
        }

        public static void Run(Terrain terrain, Texture current, List<HBStyleTerrainProfileLayer> rows, int layers,
                               RectInt area, Texture written, Texture hand, bool known, RenderTexture result,
                               RenderTexture handOut)
        {
            var shader = Rules;
            var data = terrain.terrainData;
            var kernel = shader.FindKernel("Rules");
            var packed = new float[Mathf.Max(rows.Count, 1) * RowFloats];

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var at = index * RowFloats;

                packed[at] = row.slope.x;
                packed[at + 1] = row.slope.y;
                packed[at + 2] = row.height.x;
                packed[at + 3] = row.height.y;
                packed[at + 4] = row.slopeSoftness;
                packed[at + 5] = row.heightSoftness;
                packed[at + 6] = row.breakup;
                packed[at + 7] = Mathf.Max(row.breakupSize, 0.5f);
                packed[at + 8] = row.opacity;
                packed[at + 9] = row.automatic ? 1f : 0f;
                packed[at + 10] = Parent(rows, index);
                packed[at + 11] = row.variantAmount;
                packed[at + 12] = Mathf.Max(row.variantSize, 0.5f);
            }

            var buffer = new ComputeBuffer(Mathf.Max(rows.Count, 1), RowFloats * sizeof(float));
            buffer.SetData(packed);

            var size = data.size;
            var origin = terrain.GetPosition();

            shader.SetVector(s_SizeId, new Vector4(data.heightmapResolution, size.x, size.y, size.z));
            shader.SetVector(s_OriginId, origin);
            shader.SetInts(s_CountsId, data.alphamapResolution, layers, rows.Count, result.volumeDepth);

            shader.SetBuffer(kernel, s_RowsId, buffer);
            shader.SetTexture(kernel, s_HeightsId, data.heightmapTexture);
            shader.SetTexture(kernel, s_CurrentId, current != null ? current : Empty);
            shader.SetTexture(kernel, s_WrittenId, written != null ? written : Empty);
            shader.SetTexture(kernel, s_HandId, hand != null ? hand : Empty);
            shader.SetTexture(kernel, s_ResultId, result);
            shader.SetTexture(kernel, s_HandOutId, handOut);
            shader.SetInts(s_AreaId, area.x, area.y, area.width, area.height);
            shader.SetVector(s_FlagsId, new Vector4(known ? 1f : 0f, 0f, 0f, 0f));

            shader.Dispatch(kernel, (area.width + Group - 1) / Group, (area.height + Group - 1) / Group, 1);
            buffer.Release();
        }

        public static byte[] Read(RenderTexture target, RectInt area, int slice)
        {
            var request = AsyncGPUReadback.Request(target, 0, area.x, area.width, area.y, area.height, slice, 1);
            request.WaitForCompletion();

            if (request.hasError)
                return null;

            return request.GetData<byte>().ToArray();
        }
    }
}
