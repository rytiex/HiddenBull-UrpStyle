using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleShelterPass : ScriptableRenderPass, IDisposable
    {
        internal const string ComputePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute";

        internal const string UndersidePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUnderside.shader";

        internal const int Cascades = 3;
        internal const float Extent = 160f;
        const float Growth = 4f;
        const float Above = 100f;
        const float Depth = 1000f;
        const float Near = 0.1f;
        const double FreshSeconds = 0.5;
        const double RedrawSeconds = 0.25;
        const float PlantPass = 2f;
        const float NoHeight = -1e6f;
        const float Away = 1e9f;
        const int MaxReach = 12;
        const string LevelIdsPass = "LiquidRoofIds";
        const string LevelPass = "LiquidRoofLevel";

        static readonly float[] s_Steps = { 8f, 32f, 128f };

        sealed class Map
        {
            internal RTHandle top;
            internal RTHandle below;
            internal RTHandle raw;
            internal RTHandle packed;
            internal RTHandle drops;
            internal RTHandle coverState;
            internal RTHandle coverNext;
            internal int resolution;
            internal double lastUsed;
            internal bool coverKept;
            internal Vector2 coverCentre;
            internal readonly Vector4[] areas = new Vector4[Cascades];
            internal readonly Vector3[] centres = new Vector3[Cascades];
            internal readonly Vector3[] drawnAt = new Vector3[Cascades];
            internal readonly double[] drawnOn = new double[Cascades];
            internal readonly bool[] valid = new bool[Cascades];

            internal void Forget()
            {
                coverKept = false;

                for (var cascade = 0; cascade < Cascades; cascade++)
                {
                    valid[cascade] = false;
                    areas[cascade] = new Vector4(Away, Away, 1f, 0f);
                }
            }

            internal void Release()
            {
                top?.Release();
                below?.Release();
                raw?.Release();
                packed?.Release();
                drops?.Release();
                coverState?.Release();
                coverNext?.Release();
            }
        }

        internal struct Binding
        {
            internal RTHandle map;
            internal RTHandle drops;
            internal Vector4[] areas;
            internal Vector4 state;
        }

        struct View
        {
            internal int cascade;
            internal CullingResults culling;
            internal Matrix4x4 view;
            internal Matrix4x4 projection;
            internal Vector3 origin;
            internal Vector3 position;
        }

        class SkyData
        {
            internal RendererListHandle list;
            internal bool near;
            internal int mask;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class LevelData
        {
            internal Camera camera;
            internal bool cut;
            internal Vector4 area;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class LevelCutData
        {
            internal List<StyleTransparencyPass.CutDraw> cuts;
            internal Material material;
            internal MaterialPropertyBlock block;
            internal LevelData view;
        }

        class UnderData
        {
            internal RendererListHandle list;
            internal Material plants;
            internal int mask;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class BindData
        {
            internal Vector4[] areas;
            internal Vector4 state;
        }

        class UnpackData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle top;
            internal TextureHandle below;
            internal TextureHandle liquid;
            internal TextureHandle target;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Vector4 cell;
            internal int groups;
        }

        class DilateData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle source;
            internal TextureHandle carry;
            internal bool withCarry;
            internal TextureHandle target;
            internal int targetId;
            internal Vector4 height;
            internal Vector4 reach;
            internal int groups;
        }

        class FinishData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle raw;
            internal TextureHandle map;
            internal bool aged;
            internal TextureHandle from;
            internal TextureHandle state;
            internal TextureHandle drops;
            internal Vector4 height;
            internal Vector4 cell;
            internal Vector4 step;
            internal Vector4 count;
            internal Vector4[] masks;
            internal int groups;
        }

        class ReadData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle map;
            internal TextureHandle target;
            internal int targetId;
            internal Vector4 height;
            internal Vector4 area;
            internal int groups;
        }

        static readonly Matrix4x4 s_FlipZ = Matrix4x4.Scale(new Vector3(1f, 1f, -1f));
        static readonly Quaternion s_Down = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly ShaderTagId s_DepthOnlyTag = new ShaderTagId("DepthOnly");
        static readonly RenderQueueRange s_Queues = new RenderQueueRange(1850, 2500);

        static readonly int s_MapId = Shader.PropertyToID("_HB_Shelter");
        static readonly int s_DropsId = Shader.PropertyToID("_HB_ShelterDrops");
        static readonly int s_AreaId = Shader.PropertyToID("_HB_ShelterArea");
        static readonly int s_StateId = Shader.PropertyToID("_HB_ShelterState");
        static readonly int s_PassId = Shader.PropertyToID("_HB_ShelterPass");
        static readonly int s_ViewProjId = Shader.PropertyToID("_HB_ShelterViewProj");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_ShelterHeight");
        static readonly int s_CellId = Shader.PropertyToID("_HB_ShelterCell");
        static readonly int s_TopId = Shader.PropertyToID("_HB_ShelterTop");
        static readonly int s_BelowId = Shader.PropertyToID("_HB_ShelterBelow");
        static readonly int s_LiquidId = Shader.PropertyToID("_HB_ShelterLiquid");
        static readonly int s_RawId = Shader.PropertyToID("_HB_ShelterRaw");
        static readonly int s_RawTargetId = Shader.PropertyToID("_HB_ShelterRawTarget");
        static readonly int s_CarryId = Shader.PropertyToID("_HB_ShelterCarry");
        static readonly int s_CarryTargetId = Shader.PropertyToID("_HB_ShelterCarryTarget");
        static readonly int s_DilateId = Shader.PropertyToID("_HB_ShelterDilate");
        static readonly int s_MapTargetId = Shader.PropertyToID("_HB_ShelterMapTarget");
        static readonly int s_DropsTargetId = Shader.PropertyToID("_HB_ShelterDropsTarget");
        static readonly int s_MasksId = Shader.PropertyToID("_HB_ShelterMasks");
        static readonly int s_MaskCountId = Shader.PropertyToID("_HB_ShelterMaskCount");
        static readonly int s_CoverFromId = Shader.PropertyToID("_HB_ShelterCoverFrom");
        static readonly int s_CoverStateId = Shader.PropertyToID("_HB_ShelterCoverState");
        static readonly int s_CoverStepId = Shader.PropertyToID("_HB_ShelterCoverStep");
        static readonly int s_PackedId = Shader.PropertyToID("_HB_ShelterMap");
        static readonly int s_PreviewId = Shader.PropertyToID("_HB_ShelterPreview");
        static readonly int s_HeightsId = Shader.PropertyToID("_HB_ShelterHeights");
        static readonly int s_PreviewAreaId = Shader.PropertyToID("_HB_ShelterPreviewArea");
        static readonly int s_DrawAreaId = Shader.PropertyToID("_HB_ShelterDrawArea");
        static readonly int s_CutIdsId = Shader.PropertyToID("_HB_LiquidCutIds");
        static readonly int s_CutMaskId = Shader.PropertyToID("_HB_LiquidCutMask");
        static readonly int s_CutStateId = Shader.PropertyToID("_HB_LiquidCutState");
        static readonly int s_CutWriteId = Shader.PropertyToID("_HB_CutWrite");
        static readonly int s_CutTargetId = Shader.PropertyToID("_HB_CutTarget");
        static readonly Vector4 s_CutOn = new Vector4(1f, 0f, 0f, 0f);
        static readonly Color s_NoLiquid = new Color(NoHeight, 0f, 0f, 0f);

        static readonly Vector4[] s_Masks = new Vector4[StyleWeatherMasks.Limit];
        static readonly Vector4[] s_NoAreas = NoAreas();

        static Texture2DArray s_Empty;

        readonly Dictionary<Camera, View> m_Views = new Dictionary<Camera, View>();
        readonly HashSet<Camera> m_Wanted = new HashSet<Camera>();
        readonly Dictionary<Camera, Map> m_Maps = new Dictionary<Camera, Map>();
        readonly List<Camera> m_Stale = new List<Camera>();
        readonly List<StyleTransparencyPass.CutDraw> m_Cuts = new List<StyleTransparencyPass.CutDraw>();

        ComputeShader m_Compute;
        Shader m_UndersideShader;
        Material m_UndersideMaterial;
        Material m_CutMaterial;
        MaterialPropertyBlock m_CutBlock;
        int m_PreviewKernel = -1;
        int m_HeightsKernel = -1;
        int m_UnpackKernel = -1;
        int m_DilateXKernel = -1;
        int m_DilateZKernel = -1;
        int m_FinishKernel = -1;
        int m_FinishFarKernel = -1;
        int m_Resolution = 512;
        RTHandle m_Preview;

        internal StyleShelterPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Shelter");

            RenderPipelineManager.beginCameraRendering += Prepare;
        }

        internal static int mapId => s_MapId;

        internal static int dropsId => s_DropsId;

        internal static int areaId => s_AreaId;

        internal static int stateId => s_StateId;

        internal static Vector4[] noAreas => s_NoAreas;

        internal static Texture empty
        {
            get
            {
                if (s_Empty == null)
                {
                    s_Empty = new Texture2DArray(1, 1, Cascades, TextureFormat.RGBAFloat, false, true)
                    {
                        name = "HB Style Shelter Empty",
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    var open = new[] { new Color(NoHeight, NoHeight, NoHeight, 1f) };

                    for (var cascade = 0; cascade < Cascades; cascade++)
                        s_Empty.SetPixels(open, cascade);

                    s_Empty.Apply(false, true);
                }

                return s_Empty;
            }
        }

        static Vector4[] NoAreas()
        {
            var areas = new Vector4[Cascades];

            for (var cascade = 0; cascade < Cascades; cascade++)
                areas[cascade] = new Vector4(Away, Away, 1f, 0f);

            return areas;
        }

        internal bool TryGetMap(Camera camera, out Binding binding)
        {
            binding = default;

            if (camera == null || !m_Maps.TryGetValue(camera, out var map) || map.packed == null ||
                !map.valid[0] || StyleCameraStates.clock - map.lastUsed > FreshSeconds)
                return false;

            binding.map = map.packed;
            binding.drops = map.drops;
            binding.areas = map.areas;
            binding.state = State(map.resolution);
            return true;
        }

        internal static Vector4 State(int resolution) => new Vector4(1f, resolution, 0f, 0f);

        internal void Setup(ComputeShader compute, Shader underside, Shader cut, HBStyleWeatherQuality quality)
        {
            m_CutMaterial ??= cut != null ? CoreUtils.CreateEngineMaterial(cut) : null;
            m_CutBlock ??= new MaterialPropertyBlock();

            m_UndersideShader = underside;
            m_UndersideMaterial ??= underside != null ? CoreUtils.CreateEngineMaterial(underside) : null;
            m_Resolution = (int)quality.current.shelterResolution;
            m_Compute = compute;
            m_PreviewKernel = Kernel(compute, "Preview");
            m_HeightsKernel = Kernel(compute, "Heights");
            m_UnpackKernel = Kernel(compute, "Unpack");
            m_DilateXKernel = Kernel(compute, "DilateX");
            m_DilateZKernel = Kernel(compute, "DilateZ");
            m_FinishKernel = Kernel(compute, "Finish");
            m_FinishFarKernel = Kernel(compute, "FinishFar");
        }

        internal static bool Needed() => StyleWeatherState.active || StyleDebug.view == StyleDebugView.WeatherShelter ||
                                StyleDebug.previewShelter;

        static float Reach(int cascade)
        {
            return Extent * Mathf.Pow(Growth, cascade);
        }

        int Due(Map map, Vector3 position)
        {
            if (map == null || map.resolution != m_Resolution || map.packed == null)
                return 0;

            for (var cascade = 0; cascade < Cascades; cascade++)
            {
                if (!map.valid[cascade])
                    return cascade;
            }

            var now = StyleCameraStates.clock;

            for (var cascade = Cascades - 1; cascade >= 0; cascade--)
            {
                var step = s_Steps[cascade];

                if ((position - map.drawnAt[cascade]).sqrMagnitude >= step * step)
                    return cascade;
            }

            return !Application.isPlaying || now - map.drawnOn[0] >= RedrawSeconds ? 0 : -1;
        }

        void Prepare(ScriptableRenderContext context, Camera camera)
        {
            m_Views.Remove(camera);

            if (!m_Wanted.Contains(camera) || m_UnpackKernel < 0 || m_FinishKernel < 0 || m_FinishFarKernel < 0)
                return;

            m_Maps.TryGetValue(camera, out var map);

            var position = camera.transform.position;
            var sky = new View { cascade = Due(map, position), position = position };

            if (sky.cascade >= 0)
            {
                var extent = Reach(sky.cascade);

                sky.origin = Snap(position, extent / m_Resolution);

                if (!CullBox(context, camera, sky.origin, extent, out sky.culling, out sky.view, out sky.projection))
                    sky.cascade = -1;
            }

            m_Views[camera] = sky;
        }

        static Vector3 Snap(Vector3 position, float texel)
        {
            return new Vector3(Mathf.Floor(position.x / texel) * texel, position.y + Above,
                               Mathf.Floor(position.z / texel) * texel);
        }

        static bool CullBox(ScriptableRenderContext context, Camera camera, Vector3 origin, float extent,
                            out CullingResults culling, out Matrix4x4 view, out Matrix4x4 projection)
        {
            culling = default;
            view = s_FlipZ * Matrix4x4.TRS(origin, s_Down, Vector3.one).inverse;
            projection = Matrix4x4.Ortho(-extent * 0.5f, extent * 0.5f, -extent * 0.5f, extent * 0.5f, Near, Depth);

            if (!camera.TryGetCullingParameters(out var parameters))
                return false;

            var box = projection * view;
            var planes = GeometryUtility.CalculateFrustumPlanes(box);

            parameters.cullingMatrix = box;
            parameters.cullingPlaneCount = planes.Length;

            for (var i = 0; i < planes.Length; i++)
                parameters.SetCullingPlane(i, planes[i]);

            parameters.origin = origin;
            parameters.isOrthographic = true;
            parameters.cullingOptions = CullingOptions.None;
            parameters.shadowDistance = 0f;

            culling = context.Cull(ref parameters);
            return true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;

            m_Wanted.RemoveWhere(static stale => stale == null);

            if (Needed())
                m_Wanted.Add(camera);
            else
                m_Wanted.Remove(camera);

            if (!m_Views.TryGetValue(camera, out var sky))
                return;

            m_Views.Remove(camera);

            var resolution = m_Resolution;
            var map = AcquireMap(camera, resolution);

            if (sky.cascade > 0 && !map.valid[0])
                return;

            var packed = renderGraph.ImportTexture(map.packed);
            var raw = renderGraph.ImportTexture(map.raw);

            if (sky.cascade >= 0)
                RecordCascade(renderGraph, frameData, map, sky, packed, raw);

            if (!map.valid[0])
                return;

            var drops = renderGraph.ImportTexture(map.drops);

            RecordFinish(renderGraph, map, packed, raw, drops);

            using (var builder = renderGraph.AddUnsafePass<BindData>("HB Style Shelter Bind", out var data))
            {
                data.areas ??= new Vector4[Cascades];
                Array.Copy(map.areas, data.areas, Cascades);
                data.state = State(resolution);

                builder.UseTexture(packed, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(packed, s_MapId);
                builder.UseTexture(drops, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(drops, s_DropsId);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (BindData d, UnsafeGraphContext context) =>
                {
                    context.cmd.SetGlobalVectorArray(s_AreaId, d.areas);
                    context.cmd.SetGlobalVector(s_StateId, d.state);
                });
            }

            if (StyleDebug.previewShelter && m_PreviewKernel >= 0 && StyleDebug.IsPreviewCamera(camera))
            {
                var preview = PreviewTarget(resolution);
                var slice = Mathf.Clamp(StyleDebug.shelterCascade, 0, Cascades - 1);
                var area = new Vector4(slice, Mathf.Clamp(StyleDebug.shelterChannel, 0, 3),
                                       camera.transform.position.y, 0f);

                StyleDebug.shelterPreview = preview.rt;
                RecordRead(renderGraph, "HB Style Shelter Preview", m_PreviewKernel, s_PreviewId, preview, packed,
                           resolution, area);
            }

            if (m_HeightsKernel >= 0 && StyleWeatherBodies.wanted && camera == KeyCamera())
            {
                var centre = map.centres[0];

                RecordRead(renderGraph, "HB Style Shelter Heights", m_HeightsKernel, s_HeightsId,
                           StyleWeatherBodies.Target(resolution), packed, resolution, Vector4.zero);
                StyleWeatherBodies.Written(new Vector4(centre.x, centre.z, Extent, resolution), camera);
            }
        }

        void RecordCascade(RenderGraph renderGraph, ContextContainer frameData, Map map, View sky, TextureHandle packed,
                           TextureHandle raw)
        {
            var cascade = sky.cascade;
            var near = cascade == 0;
            var resolution = map.resolution;
            var extent = Reach(cascade);
            var skyProjection = GL.GetGPUProjectionMatrix(sky.projection, true);
            var viewProjection = skyProjection * sky.view;
            var height = HeightParams(sky.origin.y, resolution, cascade);
            var liquids = HBStyleLiquid.any;
            var top = renderGraph.ImportTexture(map.top);
            var below = TextureHandle.nullHandle;

            RecordTopDepth(renderGraph, frameData, sky, skyProjection, top, near);

            if (m_UndersideShader != null)
            {
                below = renderGraph.ImportTexture(map.below);
                RecordUnderside(renderGraph, frameData, sky, skyProjection, below, near);
            }

            var level = TextureHandle.nullHandle;

            if (liquids)
            {
                var area = new Vector4(sky.origin.x - extent * 0.5f, sky.origin.z - extent * 0.5f, extent,
                                       sky.origin.y - Depth * 0.5f);
                var mask = Cutters(sky.origin, extent)
                    ? RecordLevelCut(renderGraph, frameData, sky, skyProjection, resolution, area)
                    : TextureHandle.nullHandle;

                level = RecordLevel(renderGraph, frameData, sky, skyProjection, resolution, area, mask);
            }

            var settled = near
                ? raw
                : renderGraph.CreateTexture(RawDesc(resolution, "HB Style Shelter Heights"));
            var cell = new Vector4(sky.origin.x, sky.origin.z, extent,
                                   (level.IsValid() ? 1f : 0f) + (below.IsValid() ? 2f : 0f));
            var reach = Margin(StyleEnvironment.targetProfile.weather.roofMargin, extent / resolution, near);

            if (reach > 0 && below.IsValid() && m_DilateXKernel >= 0 && m_DilateZKernel >= 0)
            {
                var unpacked = renderGraph.CreateTexture(RawDesc(resolution, "HB Style Shelter Unpacked"));
                var carry = renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
                {
                    format = GraphicsFormat.R32G32_SFloat,
                    enableRandomWrite = true,
                    name = "HB Style Shelter Overhangs"
                });

                RecordUnpack(renderGraph, top, below, level, unpacked, viewProjection, height, cell);
                RecordDilate(renderGraph, "HB Style Shelter Margin X", m_DilateXKernel, unpacked,
                             TextureHandle.nullHandle, carry, s_CarryTargetId, height, reach);
                RecordDilate(renderGraph, "HB Style Shelter Margin Z", m_DilateZKernel, unpacked, carry, settled,
                             s_RawTargetId, height, reach);
            }
            else
            {
                RecordUnpack(renderGraph, top, below, level, settled, viewProjection, height, cell);
            }

            map.centres[cascade] = sky.origin;
            map.drawnAt[cascade] = sky.position;
            map.drawnOn[cascade] = StyleCameraStates.clock;
            map.valid[cascade] = true;
            map.areas[cascade] = new Vector4(sky.origin.x, sky.origin.z, 1f / extent, 0f);

            if (near)
                return;

            using var builder = renderGraph.AddComputePass<FinishData>("HB Style Shelter Finish Far", out var data,
                                                                       profilingSampler);

            data.compute = m_Compute;
            data.kernel = m_FinishFarKernel;
            data.raw = settled;
            data.map = packed;
            data.aged = false;
            data.height = height;
            data.cell = cell;
            data.groups = (resolution + 7) / 8;
            FillMasks(data, sky.origin, extent);

            builder.UseTexture(settled, AccessFlags.Read);
            builder.UseTexture(packed, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (FinishData d, ComputeGraphContext context) => Finish(d, context.cmd));
        }

        void RecordFinish(RenderGraph renderGraph, Map map, TextureHandle packed, TextureHandle raw,
                          TextureHandle drops)
        {
            var resolution = map.resolution;
            var centre = map.centres[0];
            var texel = Extent / resolution;
            var settings = StyleEnvironment.targetProfile.weather;
            var rate = Application.isPlaying ? Time.deltaTime * settings.speed : float.PositiveInfinity;
            var here = new Vector2(centre.x, centre.z);
            var moved = (here - map.coverCentre) / texel;
            var shift = map.coverKept
                ? new Vector2(Mathf.Round(moved.x), Mathf.Round(moved.y))
                : new Vector2(resolution, resolution);

            using var builder = renderGraph.AddComputePass<FinishData>("HB Style Shelter Finish", out var data,
                                                                       profilingSampler);

            data.compute = m_Compute;
            data.kernel = m_FinishKernel;
            data.raw = raw;
            data.map = packed;
            data.aged = true;
            data.from = renderGraph.ImportTexture(map.coverState);
            data.state = renderGraph.ImportTexture(map.coverNext);
            data.drops = drops;
            data.height = new Vector4(0f, 0f, resolution, 0f);
            data.cell = new Vector4(centre.x, centre.z, Extent, 0f);
            data.step = new Vector4(shift.x, shift.y, Mathf.Min(rate / settings.uncoverTime, 1f), 0f);
            data.groups = (resolution + 7) / 8;
            FillMasks(data, centre, Extent);

            builder.UseTexture(data.raw, AccessFlags.Read);
            builder.UseTexture(data.from, AccessFlags.Read);
            builder.UseTexture(data.state, AccessFlags.Write);
            builder.UseTexture(packed, AccessFlags.Write);
            builder.UseTexture(drops, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (FinishData d, ComputeGraphContext context) => Finish(d, context.cmd));

            (map.coverState, map.coverNext) = (map.coverNext, map.coverState);
            map.coverCentre = here;
            map.coverKept = true;
        }

        static void FillMasks(FinishData data, Vector3 centre, float extent)
        {
            var count = StyleWeatherMasks.Collect(new Vector2(centre.x, centre.z), extent * 0.5f, s_Masks,
                                                  out var blocking);

            data.masks ??= new Vector4[StyleWeatherMasks.Limit];
            data.count = new Vector4(count, blocking, 0f, 0f);
            Array.Copy(s_Masks, data.masks, StyleWeatherMasks.Limit);
        }

        static void Finish(FinishData d, ComputeCommandBuffer cmd)
        {
            cmd.SetComputeTextureParam(d.compute, d.kernel, s_RawId, d.raw);
            cmd.SetComputeTextureParam(d.compute, d.kernel, s_MapTargetId, d.map);

            if (d.aged)
            {
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_CoverFromId, d.from);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_CoverStateId, d.state);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_DropsTargetId, d.drops);
                cmd.SetComputeVectorParam(d.compute, s_CoverStepId, d.step);
            }

            cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
            cmd.SetComputeVectorParam(d.compute, s_CellId, d.cell);
            cmd.SetComputeVectorParam(d.compute, s_MaskCountId, d.count);
            cmd.SetComputeVectorArrayParam(d.compute, s_MasksId, d.masks);
            cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
        }

        void RecordUnpack(RenderGraph renderGraph, TextureHandle top, TextureHandle below, TextureHandle level,
                          TextureHandle target, Matrix4x4 viewProjection, Vector4 height, Vector4 cell)
        {
            using var builder = renderGraph.AddComputePass<UnpackData>("HB Style Shelter Unpack", out var data,
                                                                       profilingSampler);

            data.compute = m_Compute;
            data.kernel = m_UnpackKernel;
            data.top = top;
            data.below = below.IsValid() ? below : top;
            data.liquid = level.IsValid() ? level : top;
            data.target = target;
            data.viewProjection = viewProjection;
            data.height = height;
            data.cell = cell;
            data.groups = ((int)height.z + 7) / 8;

            builder.UseTexture(top, AccessFlags.Read);

            if (below.IsValid())
                builder.UseTexture(below, AccessFlags.Read);

            if (level.IsValid())
                builder.UseTexture(level, AccessFlags.Read);

            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (UnpackData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_TopId, d.top);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_BelowId, d.below);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_LiquidId, d.liquid);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_RawTargetId, d.target);
                cmd.SetComputeMatrixParam(d.compute, s_ViewProjId, d.viewProjection);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_CellId, d.cell);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        void RecordDilate(RenderGraph renderGraph, string name, int kernel, TextureHandle source, TextureHandle carry,
                          TextureHandle target, int targetId, Vector4 height, int reach)
        {
            using var builder = renderGraph.AddComputePass<DilateData>(name, out var data, profilingSampler);

            data.compute = m_Compute;
            data.kernel = kernel;
            data.source = source;
            data.carry = carry;
            data.withCarry = carry.IsValid();
            data.target = target;
            data.targetId = targetId;
            data.height = height;
            data.reach = new Vector4(reach, 0f, 0f, 0f);
            data.groups = ((int)height.z + 7) / 8;

            builder.UseTexture(source, AccessFlags.Read);

            if (data.withCarry)
                builder.UseTexture(carry, AccessFlags.Read);

            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (DilateData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_RawId, d.source);

                if (d.withCarry)
                    cmd.SetComputeTextureParam(d.compute, d.kernel, s_CarryId, d.carry);

                cmd.SetComputeTextureParam(d.compute, d.kernel, d.targetId, d.target);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_DilateId, d.reach);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        void RecordRead(RenderGraph renderGraph, string name, int kernel, int targetId, RTHandle output,
                        TextureHandle packed, int resolution, Vector4 area)
        {
            var target = renderGraph.ImportTexture(output);

            using var builder = renderGraph.AddComputePass<ReadData>(name, out var data, profilingSampler);

            data.compute = m_Compute;
            data.kernel = kernel;
            data.map = packed;
            data.target = target;
            data.targetId = targetId;
            data.height = new Vector4(0f, 0f, resolution, 0f);
            data.area = area;
            data.groups = (resolution + 7) / 8;

            builder.UseTexture(packed, AccessFlags.Read);
            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (ReadData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_PackedId, d.map);
                cmd.SetComputeTextureParam(d.compute, d.kernel, d.targetId, d.target);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_PreviewAreaId, d.area);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        static TextureDesc RawDesc(int resolution, string name)
        {
            return new TextureDesc(resolution, resolution)
            {
                format = GraphicsFormat.R32G32B32A32_SFloat,
                enableRandomWrite = true,
                name = name
            };
        }

        static int Margin(float margin, float texel, bool near)
        {
            if (margin <= 0f)
                return 0;

            var reach = Mathf.RoundToInt(margin / texel);

            return Mathf.Min(near ? Mathf.Max(1, reach) : reach, MaxReach);
        }

        Map AcquireMap(Camera camera, int resolution)
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
                m_Maps[stale].Release();
                m_Maps.Remove(stale);
            }

            if (!m_Maps.TryGetValue(camera, out var map))
            {
                map = new Map();
                map.Forget();
                m_Maps.Add(camera, map);
            }

            if (map.packed == null || map.resolution != resolution)
            {
                map.Release();
                map.Forget();
                map.resolution = resolution;

                var depth = new RenderTextureDescriptor(resolution, resolution, GraphicsFormat.None,
                                                        GraphicsFormat.D16_UNorm);

                map.top = RTHandles.Alloc(depth, FilterMode.Point, TextureWrapMode.Clamp, name: "_HB_ShelterTop");
                map.below = RTHandles.Alloc(depth, FilterMode.Point, TextureWrapMode.Clamp,
                                            name: "_HB_ShelterBelow");
                map.raw = Field(resolution, GraphicsFormat.R32G32B32A32_SFloat, "_HB_ShelterRaw");
                map.drops = Field(resolution, GraphicsFormat.R32_SFloat, "_HB_ShelterDrops");
                map.coverState = Field(resolution, GraphicsFormat.R32G32_SFloat, "_HB_ShelterCoverState");
                map.coverNext = Field(resolution, GraphicsFormat.R32G32_SFloat, "_HB_ShelterCoverNext");
                map.packed = RTHandles.Alloc(resolution, resolution, slices: Cascades,
                                             colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                                             filterMode: FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp,
                                             dimension: TextureDimension.Tex2DArray, enableRandomWrite: true,
                                             name: "_HB_Shelter");
            }

            map.lastUsed = now;

            return map;
        }

        static RTHandle Field(int resolution, GraphicsFormat format, string name)
        {
            return RTHandles.Alloc(resolution, resolution, colorFormat: format, filterMode: FilterMode.Point,
                                   wrapMode: TextureWrapMode.Clamp, enableRandomWrite: true, name: name);
        }

        bool Cutters(Vector3 origin, float extent)
        {
            m_Cuts.Clear();

            if (m_CutMaterial == null)
                return false;

            var area = new Bounds(new Vector3(origin.x, origin.y - Depth * 0.5f, origin.z),
                                  new Vector3(extent, Depth, extent));
            var keeping = false;

            foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
            {
                if (cutter == null || cutter.affects == 0)
                    continue;

                var keep = cutter.mode == HBStyleLiquidCutMode.KeepInside;

                keeping |= keep;

                if ((!keep && cutter.roof) || !cutter.Spans(out var bounds) || !bounds.Intersects(area))
                    continue;

                m_Cuts.Add(new StyleTransparencyPass.CutDraw
                {
                    mesh = cutter.volume,
                    matrix = cutter.transform.localToWorldMatrix,
                    write = StyleTransparencyPass.CutWrite(cutter.mode),
                    target = (int)cutter.affects
                });
            }

            return keeping || m_Cuts.Count > 0;
        }

        static void Level(LevelData data, ContextContainer frameData, View sky, Matrix4x4 skyProjection, Vector4 area,
                          bool cut)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            data.camera = cameraData.camera;
            data.cut = cut;
            data.area = area;
            data.skyView = sky.view;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);
        }

        static void DrawLevel(RasterCommandBuffer cmd, LevelData d, string pass)
        {
            RenderingUtils.SetViewAndProjectionMatrices(cmd, d.skyView, d.skyProjection, false);
            cmd.SetGlobalVector(s_CutStateId, d.cut ? s_CutOn : Vector4.zero);
            cmd.SetGlobalVector(s_DrawAreaId, d.area);
            HBStyleLiquid.DrawLevel(cmd, d.camera, pass);
            cmd.SetGlobalVector(s_CutStateId, Vector4.zero);
            RenderingUtils.SetViewAndProjectionMatrices(cmd, d.cameraView, d.cameraProjection, true);
        }

        TextureHandle RecordLevelCut(RenderGraph renderGraph, ContextContainer frameData, View sky,
                                     Matrix4x4 skyProjection, int resolution, Vector4 area)
        {
            var colour = new TextureDesc(resolution, resolution)
            {
                format = GraphicsFormat.R16_SFloat,
                filterMode = FilterMode.Point,
                clearBuffer = true,
                clearColor = Color.clear,
                name = "HB Style Shelter Liquid Ids"
            };

            var ids = renderGraph.CreateTexture(colour);

            colour.format = GraphicsFormat.R16G16_SFloat;
            colour.name = "HB Style Shelter Liquid Cut";

            var mask = renderGraph.CreateTexture(colour);
            var depth = renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
            {
                depthBufferBits = DepthBits.Depth32,
                clearBuffer = true,
                name = "HB Style Shelter Liquid Depth"
            });

            using (var builder = renderGraph.AddRasterRenderPass<LevelData>("HB Style Shelter Liquid Ids", out var data,
                                                                           profilingSampler))
            {
                Level(data, frameData, sky, skyProjection, area, false);

                builder.SetRenderAttachment(ids, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(ids, s_CutIdsId);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (LevelData d, RasterGraphContext context) =>
                    DrawLevel(context.cmd, d, LevelIdsPass));
            }

            using (var builder = renderGraph.AddRasterRenderPass<LevelCutData>("HB Style Shelter Liquid Cut",
                                                                              out var data, profilingSampler))
            {
                data.cuts = m_Cuts;
                data.material = m_CutMaterial;
                data.block = m_CutBlock;
                data.view ??= new LevelData();
                Level(data.view, frameData, sky, skyProjection, area, false);

                builder.UseTexture(ids, AccessFlags.Read);
                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(mask, s_CutMaskId);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (LevelCutData d, RasterGraphContext context) =>
                {
                    var view = d.view;

                    RenderingUtils.SetViewAndProjectionMatrices(context.cmd, view.skyView, view.skyProjection, false);

                    foreach (var cut in d.cuts)
                    {
                        if (cut.mesh == null)
                            continue;

                        d.block.SetVector(s_CutWriteId, cut.write);
                        d.block.SetFloat(s_CutTargetId, cut.target);

                        for (var part = 0; part < cut.mesh.subMeshCount; part++)
                            context.cmd.DrawMesh(cut.mesh, cut.matrix, d.material, part, 0, d.block);
                    }

                    RenderingUtils.SetViewAndProjectionMatrices(context.cmd, view.cameraView, view.cameraProjection,
                                                                true);
                });
            }

            return mask;
        }

        TextureHandle RecordLevel(RenderGraph renderGraph, ContextContainer frameData, View sky,
                                  Matrix4x4 skyProjection, int resolution, Vector4 area, TextureHandle mask)
        {
            var level = renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
            {
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                name = "HB Style Shelter Liquid Level"
            });

            using var builder = renderGraph.AddRasterRenderPass<LevelData>("HB Style Shelter Liquid Level",
                                                                          out var data, profilingSampler);

            Level(data, frameData, sky, skyProjection, area, mask.IsValid());

            if (mask.IsValid())
                builder.UseTexture(mask, AccessFlags.Read);

            builder.SetRenderAttachment(level, 0);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (LevelData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Color, s_NoLiquid, 1f, 0u);
                DrawLevel(context.cmd, d, LevelPass);
            });

            return level;
        }

        void RecordTopDepth(RenderGraph renderGraph, ContextContainer frameData, View sky, Matrix4x4 skyProjection,
                            TextureHandle target, bool near)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<SkyData>("HB Style Shelter Depth", out var data,
                                                                        profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_DepthOnlyTag, frameData.Get<UniversalRenderingData>(),
                                                               cameraData, frameData.Get<UniversalLightData>(),
                                                               SortingCriteria.CommonOpaque);

            var tags = new NativeArray<ShaderTagId>(1, Allocator.Temp);
            tags[0] = ShaderTagId.none;

            var states = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
            states[0] = new RenderStateBlock(RenderStateMask.Raster)
            {
                rasterState = new RasterState(CullMode.Off)
            };

            data.list = renderGraph.CreateRendererList(new RendererListParams(
                sky.culling, drawing, new FilteringSettings(s_Queues, cameraData.camera.cullingMask))
            {
                tagValues = tags,
                stateBlocks = states,
                isPassTagName = false
            });
            data.near = near;
            data.mask = cameraData.camera.cullingMask;
            data.skyView = sky.view;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachmentDepth(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (SkyData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1f, 0u);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.skyView, d.skyProjection, false);
                context.cmd.SetGlobalFloat(s_PassId, 1f);
                context.cmd.DrawRendererList(d.list);
                context.cmd.SetGlobalFloat(s_PassId, PlantPass);

                if (d.near)
                    StylePlantSystem.DrawShelter(context.cmd, d.mask, null);
                else
                    StylePlantSystem.DrawFarShelter(context.cmd, d.mask);

                context.cmd.SetGlobalFloat(s_PassId, 0f);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.cameraView, d.cameraProjection, true);
            });
        }

        void RecordUnderside(RenderGraph renderGraph, ContextContainer frameData, View sky, Matrix4x4 skyProjection,
                             TextureHandle under, bool near)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<UnderData>("HB Style Shelter Underside", out var data,
                                                                          profilingSampler);

            var drawing = RenderingUtils.CreateDrawingSettings(s_DepthOnlyTag, frameData.Get<UniversalRenderingData>(),
                                                               cameraData, frameData.Get<UniversalLightData>(),
                                                               SortingCriteria.None);

            drawing.overrideShader = m_UndersideShader;
            drawing.overrideShaderPassIndex = 0;

            data.list = renderGraph.CreateRendererList(new RendererListParams(
                sky.culling, drawing, new FilteringSettings(s_Queues, cameraData.camera.cullingMask)));
            data.plants = near ? m_UndersideMaterial : null;
            data.mask = cameraData.camera.cullingMask;
            data.skyView = sky.view;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachmentDepth(under, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (UnderData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 0f, 0u);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.skyView, d.skyProjection, false);
                context.cmd.DrawRendererList(d.list);

                if (d.plants != null)
                    StylePlantSystem.DrawShelter(context.cmd, d.mask, d.plants);

                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.cameraView, d.cameraProjection, true);
            });
        }

        static Camera KeyCamera()
        {
            var camera = Camera.main;

            return camera != null ? camera : StyleCameraStates.lastGameCamera;
        }

        RTHandle PreviewTarget(int resolution)
        {
            if (m_Preview == null || m_Preview.rt.width != resolution)
            {
                m_Preview?.Release();
                m_Preview = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                                            filterMode: FilterMode.Point, enableRandomWrite: true,
                                            name: "HB Style Shelter Preview");
            }

            return m_Preview;
        }

        static int Kernel(ComputeShader compute, string name)
        {
            return compute != null && compute.HasKernel(name) ? compute.FindKernel(name) : -1;
        }

        static Vector4 HeightParams(float originY, int resolution, int cascade)
        {
            var span = Depth - Near;
            var reversed = SystemInfo.usesReversedZBuffer;

            return new Vector4(reversed ? span : -span, reversed ? originY - Depth : originY - Near, resolution,
                               cascade);
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= Prepare;

            CoreUtils.Destroy(m_UndersideMaterial);
            m_UndersideMaterial = null;
            CoreUtils.Destroy(m_CutMaterial);
            m_CutMaterial = null;

            m_Views.Clear();
            m_Wanted.Clear();

            foreach (var map in m_Maps.Values)
                map.Release();

            m_Maps.Clear();
            m_Preview?.Release();
            m_Preview = null;
            StyleDebug.shelterPreview = null;
        }
    }
}
