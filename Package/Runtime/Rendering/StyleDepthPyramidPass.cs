using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleDepthPyramidPass : ScriptableRenderPass, IDisposable
    {
        internal const string ComputePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleDepthPyramid.compute";

        const double FreshSeconds = 0.5;
        const int LeastSide = 16;
        const int GroupSize = 8;

        sealed class Map
        {
            internal RTHandle atlas;
            internal int width;
            internal int height;
            internal int levels;
            internal Matrix4x4 view;
            internal Vector4 lens;
            internal Vector3 eye;
            internal double lastUsed;
        }

        class PassData
        {
            internal ComputeShader compute;
            internal int baseKernel;
            internal int downKernel;
            internal TextureHandle depth;
            internal TextureHandle atlas;
            internal Vector2 screen;
            internal Vector2 eye;
            internal int width;
            internal int height;
            internal int levels;
        }

        static readonly int s_DepthId = Shader.PropertyToID("_HB_PyramidDepth");
        static readonly int s_AtlasId = Shader.PropertyToID("_HB_PyramidAtlas");
        static readonly int s_SizesId = Shader.PropertyToID("_HB_PyramidSizes");
        static readonly int s_EyeId = Shader.PropertyToID("_HB_PyramidEye");
        static readonly int s_StepId = Shader.PropertyToID("_HB_PyramidStep");
        static readonly int s_OcclusionDepthId = Shader.PropertyToID("_HB_OcclusionDepth");
        static readonly int s_OcclusionViewId = Shader.PropertyToID("_HB_OcclusionView");
        static readonly int s_OcclusionLensId = Shader.PropertyToID("_HB_OcclusionLens");
        static readonly int s_OcclusionSizeId = Shader.PropertyToID("_HB_OcclusionSize");

        readonly Dictionary<Camera, Map> m_Maps = new Dictionary<Camera, Map>();
        readonly List<Camera> m_Stale = new List<Camera>();

        ComputeShader m_Compute;
        int m_BaseKernel = -1;
        int m_DownKernel = -1;

        internal StyleDepthPyramidPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            profilingSampler = new ProfilingSampler("HB Style Depth Pyramid");
        }

        internal void Setup(ComputeShader compute)
        {
            if (m_Compute != compute)
            {
                m_Compute = compute;
                m_BaseKernel = compute != null && compute.HasKernel("Base") ? compute.FindKernel("Base") : -1;
                m_DownKernel = compute != null && compute.HasKernel("Down") ? compute.FindKernel("Down") : -1;
            }

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        internal static void Bind(StyleDepthPyramidPass pass, CommandBuffer cmd, ComputeShader compute, int kernel,
                                  Camera camera, Camera viewer, Vector3 eye)
        {
            if (pass == null || !pass.TryGetMap(camera, viewer, out var map))
            {
                cmd.SetComputeTextureParam(compute, kernel, s_OcclusionDepthId, Texture2D.blackTexture);
                cmd.SetComputeVectorParam(compute, s_OcclusionSizeId, Vector4.zero);
                return;
            }

            var lens = map.lens;

            lens.w = (eye - map.eye).magnitude;

            cmd.SetComputeTextureParam(compute, kernel, s_OcclusionDepthId, map.atlas);
            cmd.SetComputeMatrixParam(compute, s_OcclusionViewId, map.view);
            cmd.SetComputeVectorParam(compute, s_OcclusionLensId, lens);
            cmd.SetComputeVectorParam(compute, s_OcclusionSizeId, new Vector4(map.width, map.height, map.levels, 1f));
        }

        bool TryGetMap(Camera camera, Camera viewer, out Map map)
        {
            map = null;

            if (viewer == null || viewer.orthographic || !m_Maps.TryGetValue(viewer, out map) ||
                map.atlas == null)
                return false;

            if (camera != viewer)
                map.lastUsed = StyleCameraStates.clock;

            return StyleCameraStates.clock - map.lastUsed <= FreshSeconds;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var camera = cameraData.camera;
            var depth = resourceData.cameraDepthTexture;

            if (m_BaseKernel < 0 || m_DownKernel < 0 || camera.orthographic || !depth.IsValid())
                return;

            var target = cameraData.cameraTargetDescriptor;
            var width = PreviousPower(target.width / 2);
            var height = PreviousPower(target.height / 2);

            if (Mathf.Min(width, height) < LeastSide)
                return;

            var map = Acquire(camera, width, height);
            var projection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                       !resourceData.isActiveTargetBackBuffer);

            map.view = cameraData.GetViewMatrix();
            map.lens = new Vector4(projection.m00, SystemInfo.graphicsUVStartsAtTop ? -projection.m11 : projection.m11,
                                   camera.nearClipPlane, 0f);
            map.eye = camera.transform.position;

            using var builder = renderGraph.AddComputePass<PassData>("HB Style Depth Pyramid", out var data,
                                                                     profilingSampler);

            data.compute = m_Compute;
            data.baseKernel = m_BaseKernel;
            data.downKernel = m_DownKernel;
            data.depth = depth;
            data.atlas = renderGraph.ImportTexture(map.atlas);
            data.screen = new Vector2(target.width, target.height);
            data.eye = EyeParams(camera);
            data.width = width;
            data.height = height;
            data.levels = map.levels;

            builder.UseTexture(data.depth, AccessFlags.Read);
            builder.UseTexture(data.atlas, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, ComputeGraphContext context) => Build(d, context.cmd));
        }

        static void Build(PassData data, ComputeCommandBuffer cmd)
        {
            var compute = data.compute;

            cmd.SetComputeTextureParam(compute, data.baseKernel, s_DepthId, data.depth);
            cmd.SetComputeTextureParam(compute, data.baseKernel, s_AtlasId, data.atlas);
            cmd.SetComputeTextureParam(compute, data.downKernel, s_AtlasId, data.atlas);
            cmd.SetComputeVectorParam(compute, s_EyeId, data.eye);
            cmd.SetComputeVectorParam(compute, s_SizesId, new Vector4(data.screen.x, data.screen.y, data.width,
                                                                      data.height));
            cmd.DispatchCompute(compute, data.baseKernel, Groups(data.width), Groups(data.height), 1);

            for (var level = 1; level < data.levels; level++)
            {
                var from = Origin(level - 1, data.width, data.height);
                var to = Origin(level, data.width, data.height);
                var width = data.width >> level;
                var height = data.height >> level;

                cmd.SetComputeVectorParam(compute, s_SizesId, new Vector4(0f, 0f, width, height));
                cmd.SetComputeVectorParam(compute, s_StepId, new Vector4(from.x, from.y, to.x, to.y));
                cmd.DispatchCompute(compute, data.downKernel, Groups(width), Groups(height), 1);
            }
        }

        static Vector2Int Origin(int level, int width, int height)
        {
            return level == 0 ? Vector2Int.zero : new Vector2Int(width, height - (height >> (level - 1)));
        }

        static int Groups(int size)
        {
            return (size + GroupSize - 1) / GroupSize;
        }

        static int PreviousPower(int value)
        {
            return Mathf.NextPowerOfTwo(value + 1) / 2;
        }

        static Vector2 EyeParams(Camera camera)
        {
            var near = camera.nearClipPlane;
            var far = camera.farClipPlane;
            var reversed = SystemInfo.usesReversedZBuffer;
            var x = reversed ? far / near - 1f : 1f - far / near;
            var y = reversed ? 1f : far / near;

            return new Vector2(x / far, y / far);
        }

        Map Acquire(Camera camera, int width, int height)
        {
            var now = StyleCameraStates.clock;

            m_Stale.Clear();

            foreach (var pair in m_Maps)
            {
                var unused = now - pair.Value.lastUsed > StyleCameraStates.PruneSeconds;

                if (pair.Key == null || (pair.Key != camera && unused))
                    m_Stale.Add(pair.Key);
            }

            foreach (var stale in m_Stale)
            {
                m_Maps[stale].atlas?.Release();
                m_Maps.Remove(stale);
            }

            if (!m_Maps.TryGetValue(camera, out var map))
            {
                map = new Map();
                m_Maps.Add(camera, map);
            }

            if (map.atlas == null || map.width != width || map.height != height)
            {
                map.atlas?.Release();
                map.width = width;
                map.height = height;
                map.levels = Mathf.FloorToInt(Mathf.Log(Mathf.Min(width, height), 2f)) + 1;
                map.atlas = RTHandles.Alloc(width + width / 2, height, colorFormat: GraphicsFormat.R32_SFloat,
                                            filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                                            enableRandomWrite: true, name: "_HB_DepthPyramid");
            }

            map.lastUsed = now;

            return map;
        }

        public void Dispose()
        {
            foreach (var map in m_Maps.Values)
                map.atlas?.Release();

            m_Maps.Clear();
        }
    }
}
