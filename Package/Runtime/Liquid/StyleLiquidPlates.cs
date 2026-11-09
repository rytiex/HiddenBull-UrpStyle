using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    static class StyleLiquidPlates
    {
        const int Size = 512;
        const int Group = 8;
        const int SpreadRounds = 384;
        const int FillRounds = 12;

        static readonly int s_NormalId = Shader.PropertyToID("_HB_PlateNormal");
        static readonly int s_LabelsId = Shader.PropertyToID("_HB_PlateLabels");
        static readonly int s_ShapeId = Shader.PropertyToID("_HB_PlateShape");
        static readonly int s_OutId = Shader.PropertyToID("_HB_PlateOut");
        static readonly int s_BakeId = Shader.PropertyToID("_HB_PlateBake");

        sealed class Entry
        {
            internal RenderTexture texture;
            internal uint version;
            internal bool baked;
            internal int users;
        }

        static readonly Dictionary<Texture2D, Entry> s_Entries = new Dictionary<Texture2D, Entry>();

        static ComputeShader s_Compute;
        static CommandBuffer s_Commands;

        internal static void Use(ComputeShader compute)
        {
            if (compute != null)
                s_Compute = compute;
        }

        internal static void Acquire(Texture2D map)
        {
            if (map == null)
                return;

            if (!s_Entries.TryGetValue(map, out var entry))
            {
                entry = new Entry();
                s_Entries.Add(map, entry);
            }

            entry.users++;
        }

        internal static void Release(Texture2D map)
        {
            if (map == null || !s_Entries.TryGetValue(map, out var entry))
                return;

            if (--entry.users > 0)
                return;

            Destroy(entry);
            s_Entries.Remove(map);
        }

        internal static Texture Get(Texture2D map)
        {
            if (map == null || !s_Entries.TryGetValue(map, out var entry))
                return Texture2D.grayTexture;

            var lost = entry.texture == null || !entry.texture.IsCreated();

            if ((lost || !entry.baked || entry.version != map.updateCount) && !Bake(map, entry))
                return Texture2D.grayTexture;

            return entry.texture;
        }

        static bool Bake(Texture2D map, Entry entry)
        {
            if (s_Compute == null || !SystemInfo.supportsComputeShaders || !s_Compute.HasKernel("PlateMask"))
                return false;

            var mask = s_Compute.FindKernel("PlateMask");
            var spread = s_Compute.FindKernel("PlateSpread");
            var fill = s_Compute.FindKernel("PlateFill");
            var tone = s_Compute.FindKernel("PlateTone");

            if (entry.texture == null || !entry.texture.IsCreated())
            {
                Destroy(entry);
                entry.texture = new RenderTexture(Size, Size, 0, GraphicsFormat.R16G16_SFloat)
                {
                    name = "HB Style Liquid Plates",
                    hideFlags = HideFlags.HideAndDontSave,
                    enableRandomWrite = true,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Repeat
                };
                entry.texture.Create();
            }

            var labels = new ComputeBuffer(Size * Size, sizeof(uint));
            var shape = new ComputeBuffer(Size * Size, sizeof(float));
            var cmd = s_Commands ??= new CommandBuffer { name = "HB Style Liquid Plates" };
            var groups = Size / Group;
            var lod = Mathf.Max(0f, Mathf.Log(Mathf.Max(map.width, map.height) / (float)Size, 2f));

            cmd.Clear();
            cmd.SetComputeVectorParam(s_Compute, s_BakeId, new Vector4(Size, lod, 0f, 0f));
            cmd.SetComputeTextureParam(s_Compute, mask, s_NormalId, map);
            cmd.SetComputeBufferParam(s_Compute, mask, s_LabelsId, labels);
            cmd.SetComputeBufferParam(s_Compute, mask, s_ShapeId, shape);
            cmd.DispatchCompute(s_Compute, mask, groups, groups, 1);
            cmd.SetComputeBufferParam(s_Compute, spread, s_LabelsId, labels);

            for (var round = 0; round < SpreadRounds; round++)
                cmd.DispatchCompute(s_Compute, spread, groups, groups, 1);

            cmd.SetComputeBufferParam(s_Compute, fill, s_LabelsId, labels);

            for (var round = 0; round < FillRounds; round++)
                cmd.DispatchCompute(s_Compute, fill, groups, groups, 1);

            cmd.SetComputeBufferParam(s_Compute, tone, s_LabelsId, labels);
            cmd.SetComputeBufferParam(s_Compute, tone, s_ShapeId, shape);
            cmd.SetComputeTextureParam(s_Compute, tone, s_OutId, entry.texture);
            cmd.DispatchCompute(s_Compute, tone, groups, groups, 1);
            cmd.GenerateMips(entry.texture);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            labels.Release();
            shape.Release();

            entry.version = map.updateCount;
            entry.baked = true;

            return true;
        }

        static void Destroy(Entry entry)
        {
            if (entry.texture != null)
            {
                entry.texture.Release();
                CoreUtils.Destroy(entry.texture);
            }

            entry.texture = null;
            entry.baked = false;
        }
    }
}
