using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleLiquidWaves
    {
        internal const string ComputePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidWaves.compute";

        internal const int Bands = 3;
        internal const int Count = 12;
        internal const float Chop = 0.9f;

        const int Resolution = 256;
        const float Steepness = 0.1f;
        const float Spread = 1.1f;
        const float Gravity = 9.81f;
        const float Shortest = 2f;
        const float Longest = 6.5f;
        const int Group = 8;

        static readonly float[] s_Patch = { 96f, 24f, 6f };
        static readonly Vector4[] s_Set = new Vector4[Bands * Count];
        static readonly double[] s_Speeds = new double[Bands * Count];
        static readonly double[] s_Starts = new double[Bands * Count];

        static readonly int s_DisplacementOutId = Shader.PropertyToID("_HB_WaveDisplacementOut");
        static readonly int s_DerivativeOutId = Shader.PropertyToID("_HB_WaveDerivativeOut");
        static readonly int s_SetId = Shader.PropertyToID("_HB_WaveSet");
        static readonly int s_PatchId = Shader.PropertyToID("_HB_WavePatch");
        static readonly int s_BakeId = Shader.PropertyToID("_HB_WaveBake");
        static readonly int s_DisplacementId = Shader.PropertyToID("_HB_WaveDisplacement");
        static readonly int s_DerivativeId = Shader.PropertyToID("_HB_WaveDerivative");
        static readonly int s_ScaleId = Shader.PropertyToID("_HB_WaveScale");
        static readonly int s_TexelId = Shader.PropertyToID("_HB_WaveTexel");
        static readonly int s_FrameId = Shader.PropertyToID("_HB_WaveFrame");

        readonly Vector4[] m_Waves = new Vector4[Bands * Count];

        ComputeShader m_Compute;
        int m_Kernel = -1;
        RenderTexture m_Displacement;
        RenderTexture m_Derivative;
        CommandBuffer m_Commands;
        double m_Baked = double.NaN;

        static StyleLiquidWaves()
        {
            var taken = new HashSet<Vector2Int>();

            for (var band = 0; band < Bands; band++)
            {
                taken.Clear();

                for (var index = 0; index < Count; index++)
                {
                    var slot = band * Count + index;
                    var reach = Mathf.Lerp(Shortest, Longest, index / (Count - 1f));
                    var angle = (Hash(slot, 17) - 0.5f) * 2f * Spread;
                    var cell = new Vector2Int(Mathf.RoundToInt(reach * Mathf.Cos(angle)),
                                              Mathf.RoundToInt(reach * Mathf.Sin(angle)));

                    if (cell == Vector2Int.zero)
                        cell.x = 1;

                    while (!taken.Add(cell))
                        cell.y++;

                    var number = new Vector2(cell.x, cell.y) * (2f * Mathf.PI / s_Patch[band]);
                    var length = number.magnitude;

                    s_Set[slot] = new Vector4(number.x, number.y, Steepness / length, 0f);
                    s_Speeds[slot] = Math.Sqrt(Gravity * length);
                    s_Starts[slot] = Hash(slot, 53) * 2.0 * Math.PI;
                }
            }
        }

        internal static double time => Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;

        internal static float Patch(int band) => s_Patch[band];

        internal void Bind(ScriptableRenderContext context, ComputeShader compute, MaterialPropertyBlock block)
        {
            if (!Ready(compute))
                return;

            var now = time;

            if (now != m_Baked)
            {
                Bake(context, now);
                m_Baked = now;
            }

            var heading = StyleEnvironment.windHeading * Mathf.Deg2Rad;

            block.SetTexture(s_DisplacementId, m_Displacement);
            block.SetTexture(s_DerivativeId, m_Derivative);
            block.SetVector(s_ScaleId, new Vector4(1f / s_Patch[0], 1f / s_Patch[1], 1f / s_Patch[2], 0f));
            block.SetVector(s_TexelId, new Vector4(s_Patch[0] / Resolution, s_Patch[1] / Resolution,
                                                   s_Patch[2] / Resolution, 0f));
            block.SetVector(s_FrameId, new Vector4(Mathf.Sin(heading), Mathf.Cos(heading), 1f,
                                                   StyleEnvironment.weather.z));
        }

        internal void Dispose()
        {
            Release(ref m_Displacement);
            Release(ref m_Derivative);
            m_Commands?.Release();
            m_Commands = null;
            m_Compute = null;
            m_Kernel = -1;
            m_Baked = double.NaN;
        }

        bool Ready(ComputeShader compute)
        {
            if (compute == null || !SystemInfo.supportsComputeShaders)
                return false;

            if (compute != m_Compute)
            {
                m_Compute = compute;
                m_Kernel = compute.HasKernel("Bake") ? compute.FindKernel("Bake") : -1;
                m_Baked = double.NaN;
            }

            if (m_Kernel < 0)
                return false;

            if (m_Displacement == null || !m_Displacement.IsCreated())
            {
                Release(ref m_Displacement);
                m_Displacement = Allocate("HB Style Wave Displacement");
                m_Baked = double.NaN;
            }

            if (m_Derivative == null || !m_Derivative.IsCreated())
            {
                Release(ref m_Derivative);
                m_Derivative = Allocate("HB Style Wave Derivative");
                m_Baked = double.NaN;
            }

            return true;
        }

        void Bake(ScriptableRenderContext context, double now)
        {
            for (var slot = 0; slot < m_Waves.Length; slot++)
            {
                var wave = s_Set[slot];

                wave.w = (float)(s_Starts[slot] - s_Speeds[slot] * now % (2.0 * Math.PI));
                m_Waves[slot] = wave;
            }

            var cmd = m_Commands ??= new CommandBuffer { name = "HB Style Waves" };
            var groups = (Resolution + Group - 1) / Group;

            cmd.Clear();
            cmd.SetComputeTextureParam(m_Compute, m_Kernel, s_DisplacementOutId, m_Displacement);
            cmd.SetComputeTextureParam(m_Compute, m_Kernel, s_DerivativeOutId, m_Derivative);
            cmd.SetComputeVectorArrayParam(m_Compute, s_SetId, m_Waves);
            cmd.SetComputeVectorParam(m_Compute, s_PatchId, new Vector4(s_Patch[0], s_Patch[1], s_Patch[2], 0f));
            cmd.SetComputeVectorParam(m_Compute, s_BakeId, new Vector4(Resolution, Chop, 0f, 0f));
            cmd.DispatchCompute(m_Compute, m_Kernel, groups, groups, Bands);
            cmd.GenerateMips(m_Displacement);
            cmd.GenerateMips(m_Derivative);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        static RenderTexture Allocate(string name)
        {
            var texture = new RenderTexture(Resolution, Resolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = Bands,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            texture.Create();
            return texture;
        }

        static void Release(ref RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                CoreUtils.Destroy(texture);
            }

            texture = null;
        }

        static float Hash(int index, int salt)
        {
            var value = (uint)(index * 73856093) ^ (uint)(salt * 19349663);

            value ^= value >> 15;
            value *= 0x2C1B3C6Du;
            value ^= value >> 12;
            value *= 0x297A2D39u;
            value ^= value >> 15;

            return value / 4294967296f;
        }
    }
}
