using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    public enum HBStyleFogDebug
    {
        Off,
        Amount,
        Sun,
        Scattering,
        Sky
    }

    sealed class StyleFogPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/Style Fog";
        public const string ComputePath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Fog/StyleFogVolume.compute";

        const int TileDepthPass = 0;
        const int InjectPass = 1;
        const int CompositePass = 2;

        const int Tile = 8;
        const int Slices = 64;
        const int TilesPerRow = 8;
        const float FarScale = 1.5f;
        const float StaleSeconds = 10f;
        const float PruneInterval = 1f;

        static readonly int s_HistoryId = Shader.PropertyToID("_HB_FogVolumeHistory");
        static readonly int s_VolumeId = Shader.PropertyToID("_HB_FogVolume");
        static readonly int s_SizeId = Shader.PropertyToID("_HB_FogVolumeSize");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_FogVolumeParams");
        static readonly int s_FrameId = Shader.PropertyToID("_HB_FogVolumeFrame");
        static readonly int s_PreviousId = Shader.PropertyToID("_HB_FogVolumePrevious");
        static readonly int s_PreviousViewProjId = Shader.PropertyToID("_HB_FogVolumePrevViewProj");
        static readonly int s_InjectId = Shader.PropertyToID("_HB_FogInject");
        static readonly int s_IntegratedId = Shader.PropertyToID("_HB_FogIntegrated");
        static readonly int s_DebugId = Shader.PropertyToID("_HB_FogDebug");
        static readonly int s_SkyOcclusionId = Shader.PropertyToID("_HB_SkyOcclusion");
        static readonly int s_TileDepthId = Shader.PropertyToID("_HB_FogTileDepth");
        static readonly int s_SkyViewProjId = Shader.PropertyToID("_HB_SkyOcclusionViewProj");
        static readonly int s_SkyParamsId = Shader.PropertyToID("_HB_SkyOcclusionParams");

        const float SkyRadius = 1f;
        const float SkyBias = 0.25f;

        sealed class History : IDisposable
        {
            public readonly RTHandle[] atlas = new RTHandle[2];
            public int index;
            public int width;
            public int height;
            public int frame;
            public float lastSeen;
            public bool valid;
            public Matrix4x4 viewProjection;
            public Vector4 depth;

            public void Allocate(int volumeWidth, int volumeHeight)
            {
                Dispose();

                width = volumeWidth;
                height = volumeHeight;
                valid = false;

                for (var i = 0; i < atlas.Length; i++)
                {
                    atlas[i] = RTHandles.Alloc(
                        width * TilesPerRow, height * (Slices / TilesPerRow),
                        colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                        filterMode: FilterMode.Bilinear,
                        wrapMode: TextureWrapMode.Clamp,
                        name: "_HB_FogVolumeAtlas");
                }
            }

            public void Dispose()
            {
                for (var i = 0; i < atlas.Length; i++)
                {
                    atlas[i]?.Release();
                    atlas[i] = null;
                }
            }
        }

        readonly Dictionary<Camera, History> m_Histories = new Dictionary<Camera, History>();
        readonly List<Camera> m_Stale = new List<Camera>();

        Material m_Material;
        ComputeShader m_Compute;
        int m_Kernel;
        HBStyleFogDebug m_Debug;
        float m_LastPrune;

        public StyleFogPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            profilingSampler = new ProfilingSampler("HiddenBull Style Fog");

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(Shader shader, ComputeShader compute, HBStyleFogDebug debug)
        {
            m_Debug = debug;
            m_Compute = compute;

            if (m_Compute != null)
                m_Kernel = m_Compute.FindKernel("Integrate");

            if (shader == null)
            {
                DisposeMaterial();
                return;
            }

            if (m_Material == null || m_Material.shader != shader)
            {
                DisposeMaterial();

                m_Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        public bool isReady => m_Material != null && m_Compute != null;

        class TileDepthData
        {
            public Material material;
            public Vector4 size;
        }

        class InjectData
        {
            public Material material;
            public TextureHandle history;
            public TextureHandle tileDepth;
            public TextureHandle sky;
            public Matrix4x4 skyViewProjection;
            public Vector4 skyParams;
            public Vector4 size;
            public Vector4 parameters;
            public Vector4 frame;
            public Vector4 previous;
            public Matrix4x4 previousViewProjection;
        }

        class IntegrateData
        {
            public ComputeShader compute;
            public int kernel;
            public TextureHandle inject;
            public TextureHandle integrated;
            public Vector4 size;
            public Vector4 parameters;
            public int groupsX;
            public int groupsY;
        }

        class CompositeData
        {
            public Material material;
            public float debug;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!isReady)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType == CameraType.Preview)
                return;

            var fog = VolumeManager.instance.stack.GetComponent<HBStyleFog>();

            if (fog == null || fog.endDistance.value <= fog.startDistance.value || fog.maxOpacity.value <= 0f)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var camera = cameraData.camera;

            var target = cameraData.cameraTargetDescriptor;
            var width = Mathf.Max(1, (target.width + Tile - 1) / Tile);
            var height = Mathf.Max(1, (target.height + Tile - 1) / Tile);

            var history = Acquire(camera, width, height);

            var near = Mathf.Max(camera.nearClipPlane, 0.05f);
            var far = Mathf.Max(Mathf.Min(fog.endDistance.value * FarScale, camera.farClipPlane), near * 1.1f);
            var logNear = Mathf.Log(near);
            var parameters = new Vector4(logNear, 1f / (Mathf.Log(far) - logNear), Slices, 1f);

            var viewProjection = GL.GetGPUProjectionMatrix(
                cameraData.GetProjectionMatrix(), !resourceData.isActiveTargetBackBuffer)
                * cameraData.GetViewMatrix();

            var write = renderGraph.ImportTexture(history.atlas[history.index]);
            var read = renderGraph.ImportTexture(history.atlas[1 - history.index]);

            var size = new Vector4(width, height, Slices, 0f);

            var sky = TextureHandle.nullHandle;
            var skyViewProjection = Matrix4x4.identity;

            if (StyleSkyOcclusionData.TryGet(frameData, camera, out var occlusion))
            {
                sky = occlusion.texture;
                skyViewProjection = occlusion.viewProjection;
            }

            var tileDepth = renderGraph.CreateTexture(new TextureDesc(width, height)
            {
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "_HB_FogTileDepth"
            });

            using (var builder = renderGraph.AddRasterRenderPass<TileDepthData>(
                       "HiddenBull Style Fog Tile Depth", out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.size = size;

                builder.SetRenderAttachment(tileDepth, 0, AccessFlags.WriteAll);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (TileDepthData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(s_SizeId, data.size);

                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, TileDepthPass);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<InjectData>(
                       "HiddenBull Style Fog Inject", out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.history = read;
                passData.tileDepth = tileDepth;

                builder.UseTexture(tileDepth, AccessFlags.Read);
                passData.sky = sky;
                passData.skyViewProjection = skyViewProjection;
                passData.skyParams = new Vector4(SkyRadius, SkyBias, 0f, sky.IsValid() ? 1f : 0f);

                if (sky.IsValid())
                    builder.UseTexture(sky, AccessFlags.Read);
                passData.size = size;
                passData.parameters = parameters;
                passData.frame = new Vector4(
                    history.frame % 64,
                    Frac(history.frame * 0.61803399f),
                    Frac(history.frame * 0.75487767f),
                    0f);
                passData.previous = new Vector4(
                    history.depth.x, history.depth.y, 0f, history.valid ? 1f : 0f);
                passData.previousViewProjection = history.viewProjection;

                builder.SetRenderAttachment(write, 0, AccessFlags.WriteAll);
                builder.UseTexture(read, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (InjectData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;

                    cmd.SetGlobalTexture(s_HistoryId, data.history);
                    cmd.SetGlobalTexture(s_TileDepthId, data.tileDepth);
                    cmd.SetGlobalVector(s_SkyParamsId, data.skyParams);
                    cmd.SetGlobalMatrix(s_SkyViewProjId, data.skyViewProjection);

                    if (data.sky.IsValid())
                        cmd.SetGlobalTexture(s_SkyOcclusionId, data.sky);
                    cmd.SetGlobalVector(s_SizeId, data.size);
                    cmd.SetGlobalVector(s_ParamsId, data.parameters);
                    cmd.SetGlobalVector(s_FrameId, data.frame);
                    cmd.SetGlobalVector(s_PreviousId, data.previous);
                    cmd.SetGlobalMatrix(s_PreviousViewProjId, data.previousViewProjection);

                    Blitter.BlitTexture(cmd, Vector2.one, data.material, InjectPass);
                });
            }

            var integrated = renderGraph.CreateTexture(new TextureDesc(width, height)
            {
                dimension = TextureDimension.Tex3D,
                slices = Slices,
                format = GraphicsFormat.R16G16B16A16_SFloat,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "_HB_FogVolume"
            });

            using (var builder = renderGraph.AddComputePass<IntegrateData>(
                       "HiddenBull Style Fog Integrate", out var passData, profilingSampler))
            {
                passData.compute = m_Compute;
                passData.kernel = m_Kernel;
                passData.inject = write;
                passData.integrated = integrated;
                passData.size = size;
                passData.parameters = parameters;
                passData.groupsX = (width + 7) / 8;
                passData.groupsY = (height + 7) / 8;

                builder.UseTexture(write, AccessFlags.Read);
                builder.UseTexture(integrated, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(integrated, s_VolumeId);

                builder.SetRenderFunc(static (IntegrateData data, ComputeGraphContext context) =>
                {
                    var cmd = context.cmd;

                    cmd.SetComputeVectorParam(data.compute, s_SizeId, data.size);
                    cmd.SetComputeVectorParam(data.compute, s_ParamsId, data.parameters);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_InjectId, data.inject);
                    cmd.SetComputeTextureParam(data.compute, data.kernel, s_IntegratedId, data.integrated);
                    cmd.DispatchCompute(data.compute, data.kernel, data.groupsX, data.groupsY, 1);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>(
                       "HiddenBull Style Fog Composite", out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.debug = (int)m_Debug;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.UseTexture(integrated, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalFloat(s_DebugId, data.debug);

                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, CompositePass);
                });
            }

            history.viewProjection = viewProjection;
            history.depth = parameters;
            history.valid = true;
            history.index = 1 - history.index;
            history.frame++;
        }

        History Acquire(Camera camera, int width, int height)
        {
            var now = Time.realtimeSinceStartup;

            if (now - m_LastPrune > PruneInterval)
            {
                m_LastPrune = now;
                Prune(now);
            }

            if (!m_Histories.TryGetValue(camera, out var history))
            {
                history = new History();
                m_Histories.Add(camera, history);
            }

            if (history.width != width || history.height != height || history.atlas[0] == null)
                history.Allocate(width, height);

            history.lastSeen = now;

            return history;
        }

        void Prune(float now)
        {
            m_Stale.Clear();

            foreach (var pair in m_Histories)
            {
                if (pair.Key == null || now - pair.Value.lastSeen > StaleSeconds)
                    m_Stale.Add(pair.Key);
            }

            foreach (var camera in m_Stale)
            {
                m_Histories[camera].Dispose();
                m_Histories.Remove(camera);
            }
        }

        static float Frac(float value) => value - Mathf.Floor(value);

        void DisposeMaterial()
        {
            CoreUtils.Destroy(m_Material);

            m_Material = null;
        }

        public void Dispose()
        {
            DisposeMaterial();

            foreach (var history in m_Histories.Values)
                history.Dispose();

            m_Histories.Clear();
        }
    }
}
