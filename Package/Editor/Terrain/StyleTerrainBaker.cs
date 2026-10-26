using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainBaker
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/Terrain/StyleTerrainRules.compute";
        const string ControlName = "Control";

        static readonly int s_RowsId = Shader.PropertyToID("_Rows");
        static readonly int s_HeightsId = Shader.PropertyToID("_Heights");
        static readonly int s_HandId = Shader.PropertyToID("_Hand");
        static readonly int s_ResultId = Shader.PropertyToID("_Result");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_CountsId = Shader.PropertyToID("_Counts");

        [StructLayout(LayoutKind.Sequential)]
        struct Row
        {
            public Vector2 slope;
            public Vector2 height;
            public float slopeSoftness;
            public float heightSoftness;
            public float breakup;
            public float breakupSize;
            public float opacity;
            public float automatic;
            public float hand;
            public float unused;
        }

        static Texture2DArray s_NoHand;

        internal static string Signature(HBStyleTerrainData data)
        {
            var text = new StringBuilder();

            text.Append(data.version).Append(':').Append(data.controlResolution).Append(':').Append(data.layerCount);

            for (var index = 0; index < data.layerCount; index++)
            {
                var rule = data.layers[index];

                text.Append('|').Append(rule.automatic).Append(rule.slope).Append(rule.slopeSoftness)
                    .Append(rule.height).Append(rule.heightSoftness).Append(rule.opacity).Append(rule.breakup)
                    .Append(rule.breakupSize).Append(':')
                    .Append(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rule.layer)));
            }

            if (data.hand != null)
                text.Append("|hand:").Append(data.handVersion);

            return text.ToString();
        }

        internal static int Slices(HBStyleTerrainData data)
        {
            return (data.layerCount + 3) / 4;
        }

        internal static RenderTexture NewTarget(HBStyleTerrainData data)
        {
            var resolution = data.controlResolution;
            var target = new RenderTexture(resolution, resolution, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = Mathf.Max(Slices(data), 1),
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "HB Style Terrain Rules"
            };

            target.Create();
            return target;
        }

        internal static bool Fits(RenderTexture target, HBStyleTerrainData data)
        {
            return target != null && target.IsCreated() && target.width == data.controlResolution &&
                   target.volumeDepth == Mathf.Max(Slices(data), 1);
        }

        internal static bool Dispatch(HBStyleTerrainData data, RenderTexture target)
        {
            var terrain = data.terrainData;
            var layers = data.layerCount;
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

            if (terrain == null || layers == 0 || compute == null)
                return false;

            var resolution = data.controlResolution;
            var slices = Slices(data);
            var kernel = compute.FindKernel("Rules");
            var rows = new Row[layers];
            var hand = data.hand;
            var hasHand = hand != null;

            for (var index = 0; index < layers; index++)
            {
                var rule = data.layers[index];

                rows[index] = new Row
                {
                    slope = rule.slope,
                    height = rule.height,
                    slopeSoftness = rule.slopeSoftness,
                    heightSoftness = rule.heightSoftness,
                    breakup = rule.breakup,
                    breakupSize = rule.breakupSize,
                    opacity = rule.opacity,
                    automatic = rule.automatic ? 1f : 0f,
                    hand = hasHand ? data.HandChannel(rule.layer) : -1f
                };
            }

            var buffer = new ComputeBuffer(layers, Marshal.SizeOf<Row>());

            buffer.SetData(rows);
            compute.SetBuffer(kernel, s_RowsId, buffer);
            compute.SetTexture(kernel, s_HeightsId, terrain.heightmapTexture);
            compute.SetTexture(kernel, s_HandId, hasHand ? hand : NoHand());
            compute.SetTexture(kernel, s_ResultId, target);
            compute.SetVector(s_SizeId, new Vector4(terrain.heightmapResolution, terrain.size.x, terrain.size.y,
                                                    resolution));
            compute.SetInts(s_CountsId, layers, slices, hasHand ? 1 : 0, 0);
            compute.Dispatch(kernel, (resolution + 7) / 8, (resolution + 7) / 8, 1);
            buffer.Release();

            return true;
        }

        internal static void Bake(HBStyleTerrainData data)
        {
            var layers = data.layerCount;

            if (data.terrainData == null || layers == 0)
            {
                if (data.control != null || data.controlLayers > 0)
                {
                    data.SetControl(null, null);
                    EditorUtility.SetDirty(data);
                }

                return;
            }

            var resolution = data.controlResolution;
            var slices = Slices(data);
            var target = NewTarget(data);

            try
            {
                if (!Dispatch(data, target))
                    return;

                var control = data.control;

                if (control == null || control.width != resolution || control.depth != slices)
                {
                    Remove(data, ControlName);

                    control = new Texture2DArray(resolution, resolution, slices, TextureFormat.RGBA32, false, true)
                    {
                        name = ControlName,
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };

                    AssetDatabase.AddObjectToAsset(control, data);
                }

                for (var slice = 0; slice < slices; slice++)
                {
                    var request = AsyncGPUReadback.Request(target, 0, 0, resolution, 0, resolution, slice, 1);
                    request.WaitForCompletion();

                    if (!request.hasError)
                        control.SetPixelData(request.GetData<byte>(), 0, slice);
                }

                control.Apply(false, false);
                data.SetControl(control, Snapshot(data));
                EditorUtility.SetDirty(data);
            }
            finally
            {
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        internal static HBStyleTerrainLayer[] Snapshot(HBStyleTerrainData data)
        {
            var layers = new HBStyleTerrainLayer[data.layerCount];

            for (var index = 0; index < layers.Length; index++)
                layers[index] = data.layers[index].layer;

            return layers;
        }

        static Texture2DArray NoHand()
        {
            if (s_NoHand == null)
            {
                s_NoHand = new Texture2DArray(1, 1, 4, TextureFormat.RGBA32, false, true)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                s_NoHand.Apply(false, true);
            }

            return s_NoHand;
        }

        static Texture2DArray Find(HBStyleTerrainData data, string name)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(data)))
            {
                if (asset is Texture2DArray array && array.name == name)
                    return array;
            }

            return null;
        }

        internal static void Remove(HBStyleTerrainData data, string name)
        {
            var old = Find(data, name);

            if (old == null)
                return;

            AssetDatabase.RemoveObjectFromAsset(old);
            Object.DestroyImmediate(old, true);
        }
    }
}
