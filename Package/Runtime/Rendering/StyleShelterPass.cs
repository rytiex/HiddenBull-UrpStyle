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

        internal const float Extent = 160f;
        const float FarExtent = 1000f;
        const int FarResolution = 512;
        const float FarStep = 50f;
        const float Above = 100f;
        const float Depth = 1000f;
        const float Near = 0.1f;
        const double FreshSeconds = 0.5;
        const float PlantPass = 2f;
        const float RoofReach = 0.75f;
        const float NoLiquid = -1e6f;
        const string RoofPass = "LiquidRoof";
        const string RoofIdsPass = "LiquidRoofIds";
        const string RoofLevelPass = "LiquidRoofLevel";

        sealed class Map
        {
            internal RTHandle depth;
            internal RTHandle dilated;
            internal RTHandle under;
            internal RTHandle far;
            internal RTHandle masked;
            internal RTHandle drops;
            internal RTHandle bound;
            internal RTHandle boundUnder;
            internal RTHandle boundDrops;
            internal RTHandle coverState;
            internal RTHandle coverNext;
            internal RTHandle cover;
            internal Matrix4x4 coverViewProjection;
            internal double lastUsed;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Matrix4x4 farViewProjection;
            internal Vector4 farHeight;
            internal Vector3 farCentre;
            internal bool farValid;
        }

        internal struct Binding
        {
            internal RTHandle depth;
            internal RTHandle under;
            internal RTHandle far;
            internal RTHandle drops;
            internal RTHandle cover;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Matrix4x4 farViewProjection;
            internal Vector4 farHeight;
        }

        struct View
        {
            internal CullingResults culling;
            internal Matrix4x4 view;
            internal Matrix4x4 projection;
            internal Vector3 origin;
            internal bool farDue;
            internal CullingResults farCulling;
            internal Matrix4x4 farView;
            internal Matrix4x4 farProjection;
            internal Vector3 farOrigin;
            internal Vector3 farCentre;
        }

        class SkyData
        {
            internal RendererListHandle list;
            internal bool near;
            internal bool roof;
            internal bool cut;
            internal float reach;
            internal Camera camera;
            internal int mask;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class RoofData
        {
            internal Camera camera;
            internal float reach;
            internal bool cut;
            internal Matrix4x4 skyView;
            internal Matrix4x4 skyProjection;
            internal Matrix4x4 cameraView;
            internal Matrix4x4 cameraProjection;
        }

        class RoofCutData
        {
            internal List<StyleTransparencyPass.CutDraw> cuts;
            internal Material material;
            internal MaterialPropertyBlock block;
            internal RoofData view;
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
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Matrix4x4 farViewProjection;
            internal Vector4 farHeight;
            internal bool noUnderside;
            internal bool noCover;
            internal Texture noCoverTexture;
            internal bool hasFar;
        }

        class DilateData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle source;
            internal TextureHandle extra;
            internal TextureHandle target;
            internal int extraId;
            internal int targetId;
            internal Vector4 height;
            internal Vector4 reach;
            internal int groups;
        }

        class MaskData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle source;
            internal TextureHandle surface;
            internal TextureHandle drops;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Vector4 count;
            internal Vector4[] masks;
            internal int groups;
        }

        class UncoverData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle top;
            internal TextureHandle liquid;
            internal bool hasLiquid;
            internal TextureHandle from;
            internal TextureHandle state;
            internal TextureHandle cover;
            internal Vector4 height;
            internal Vector4 step;
            internal int groups;
        }

        class PreviewData
        {
            internal ComputeShader compute;
            internal int kernel;
            internal TextureHandle depth;
            internal TextureHandle under;
            internal bool withUnder;
            internal TextureHandle preview;
            internal int previewId;
            internal Matrix4x4 viewProjection;
            internal Vector4 height;
            internal Vector4 area;
            internal int groups;
        }

        static readonly Matrix4x4 s_FlipZ = Matrix4x4.Scale(new Vector3(1f, 1f, -1f));
        static readonly Quaternion s_Down = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly ShaderTagId s_DepthOnlyTag = new ShaderTagId("DepthOnly");
        static readonly RenderQueueRange s_Queues = new RenderQueueRange(1850, 2500);

        static readonly int s_DepthId = Shader.PropertyToID("_HB_ShelterDepth");
        static readonly int s_ViewProjId = Shader.PropertyToID("_HB_ShelterViewProj");
        static readonly int s_HeightId = Shader.PropertyToID("_HB_ShelterHeight");
        static readonly int s_StateId = Shader.PropertyToID("_HB_ShelterState");
        static readonly int s_PassId = Shader.PropertyToID("_HB_ShelterPass");
        static readonly int s_PreviewId = Shader.PropertyToID("_HB_ShelterPreview");
        static readonly int s_HeightsId = Shader.PropertyToID("_HB_ShelterHeights");
        static readonly int s_PreviewAreaId = Shader.PropertyToID("_HB_ShelterPreviewArea");
        static readonly int s_SourceId = Shader.PropertyToID("_HB_ShelterSource");
        static readonly int s_TargetId = Shader.PropertyToID("_HB_ShelterTarget");
        static readonly int s_UnderId = Shader.PropertyToID("_HB_ShelterUnder");
        static readonly int s_CarryId = Shader.PropertyToID("_HB_ShelterCarry");
        static readonly int s_CarryTargetId = Shader.PropertyToID("_HB_ShelterCarryTarget");
        static readonly int s_DilateId = Shader.PropertyToID("_HB_ShelterDilate");
        static readonly int s_FarDepthId = Shader.PropertyToID("_HB_ShelterFarDepth");
        static readonly int s_FarViewProjId = Shader.PropertyToID("_HB_ShelterFarViewProj");
        static readonly int s_FarHeightId = Shader.PropertyToID("_HB_ShelterFarHeight");
        static readonly int s_DropsId = Shader.PropertyToID("_HB_ShelterDrops");
        static readonly int s_DropsTargetId = Shader.PropertyToID("_HB_ShelterDropsTarget");
        static readonly int s_MasksId = Shader.PropertyToID("_HB_ShelterMasks");
        static readonly int s_MaskCountId = Shader.PropertyToID("_HB_ShelterMaskCount");
        static readonly int s_CoverId = Shader.PropertyToID("_HB_ShelterCover");
        static readonly int s_CoverFromId = Shader.PropertyToID("_HB_ShelterCoverFrom");
        static readonly int s_CoverStateId = Shader.PropertyToID("_HB_ShelterCoverState");
        static readonly int s_CoverStepId = Shader.PropertyToID("_HB_ShelterCoverStep");
        static readonly int s_LiquidId = Shader.PropertyToID("_HB_ShelterLiquid");
        static readonly int s_CutIdsId = Shader.PropertyToID("_HB_LiquidCutIds");
        static readonly int s_CutMaskId = Shader.PropertyToID("_HB_LiquidCutMask");
        static readonly int s_CutStateId = Shader.PropertyToID("_HB_LiquidCutState");
        static readonly int s_CutWriteId = Shader.PropertyToID("_HB_CutWrite");
        static readonly int s_CutTargetId = Shader.PropertyToID("_HB_CutTarget");
        static readonly Vector4 s_CutOn = new Vector4(1f, 0f, 0f, 0f);
        static readonly Color s_NoLiquid = new Color(NoLiquid, 0f, 0f, 0f);

        static readonly Vector4[] s_Masks = new Vector4[StyleWeatherMasks.Limit];

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
        Texture2D m_NoCover;
        int m_PreviewKernel = -1;
        int m_HeightsKernel = -1;
        int m_DilateXKernel = -1;
        int m_DilateZKernel = -1;
        int m_MasksKernel = -1;
        int m_UncoverKernel = -1;
        int m_Resolution = 512;
        RTHandle m_Preview;

        internal StyleShelterPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
            profilingSampler = new ProfilingSampler("HB Style Shelter");

            RenderPipelineManager.beginCameraRendering += Prepare;
        }

        internal static int stateId => s_StateId;

        internal static int depthId => s_DepthId;

        internal static int viewProjectionId => s_ViewProjId;

        internal static int heightId => s_HeightId;

        internal static int underId => s_UnderId;

        internal static int farDepthId => s_FarDepthId;

        internal static int farViewProjectionId => s_FarViewProjId;

        internal static int farHeightId => s_FarHeightId;

        internal static int dropsId => s_DropsId;

        internal static int coverId => s_CoverId;

        internal Texture noCover => m_NoCover;

        internal bool TryGetMap(Camera camera, out Binding binding)
        {
            binding = default;

            if (camera == null || !m_Maps.TryGetValue(camera, out var map) || map.bound == null ||
                StyleCameraStates.clock - map.lastUsed > FreshSeconds)
                return false;

            binding.depth = map.bound;
            binding.under = map.boundUnder;
            binding.far = map.farValid ? map.far : null;
            binding.drops = map.boundDrops;
            binding.cover = map.cover;
            binding.viewProjection = map.viewProjection;
            binding.height = map.height;
            binding.farViewProjection = map.farViewProjection;
            binding.farHeight = map.farHeight;
            return true;
        }

        internal static Vector4 State(bool far) => new Vector4(1f, far ? 1f : 0f, 0f, 0f);

        internal void Setup(ComputeShader compute, Shader underside, Shader cut, HBStyleWeatherQuality quality)
        {
            m_CutMaterial ??= cut != null ? CoreUtils.CreateEngineMaterial(cut) : null;
            m_CutBlock ??= new MaterialPropertyBlock();

            if (m_NoCover == null)
            {
                m_NoCover = new Texture2D(1, 1, TextureFormat.RGFloat, false, true)
                {
                    name = "HB Style Shelter No Cover",
                    hideFlags = HideFlags.HideAndDontSave
                };
                m_NoCover.SetPixel(0, 0, new Color(1f, NoLiquid, 0f, 0f));
                m_NoCover.Apply(false, true);
            }

            m_UndersideShader = underside;
            m_UndersideMaterial ??= underside != null ? CoreUtils.CreateEngineMaterial(underside) : null;
            m_Resolution = (int)quality.current.shelterResolution;
            m_Compute = compute;
            m_PreviewKernel = Kernel(compute, "Preview");
            m_HeightsKernel = Kernel(compute, "Heights");
            m_DilateXKernel = Kernel(compute, "DilateX");
            m_DilateZKernel = Kernel(compute, "DilateZ");
            m_MasksKernel = Kernel(compute, "Masks");
            m_UncoverKernel = Kernel(compute, "Uncover");
        }

        internal static bool Needed() => StyleWeatherState.active || StyleDebug.view == StyleDebugView.WeatherShelter ||
                                StyleDebug.previewShelter;

        void Prepare(ScriptableRenderContext context, Camera camera)
        {
            m_Views.Remove(camera);

            if (!m_Wanted.Contains(camera))
                return;

            var position = camera.transform.position;
            var origin = Snap(position, Extent / m_Resolution);

            if (!CullBox(context, camera, origin, Extent, out var culling, out var view, out var projection))
                return;

            var sky = new View { culling = culling, view = view, projection = projection, origin = origin };

            m_Maps.TryGetValue(camera, out var map);

            if (map == null || !map.farValid || (position - map.farCentre).sqrMagnitude > FarStep * FarStep)
            {
                var farOrigin = Snap(position, FarExtent / FarResolution);

                sky.farDue = CullBox(context, camera, farOrigin, FarExtent, out sky.farCulling, out sky.farView,
                                     out sky.farProjection);
                sky.farOrigin = farOrigin;
                sky.farCentre = position;
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
            var skyProjection = GL.GetGPUProjectionMatrix(sky.projection, true);
            var viewProjection = skyProjection * sky.view;
            var height = HeightParams(sky.origin.y, resolution);

            var map = AcquireMap(camera, resolution);
            var depth = renderGraph.ImportTexture(map.depth);

            map.viewProjection = viewProjection;
            map.height = height;

            var liquids = HBStyleLiquid.any;
            var mask = liquids && Cutters(sky.origin)
                ? RecordRoofCut(renderGraph, frameData, sky, skyProjection, resolution)
                : TextureHandle.nullHandle;

            RecordTopDepth(renderGraph, frameData, "HB Style Shelter Depth", sky.culling, sky.view, skyProjection,
                           depth, true, liquids, mask);

            var far = map.farValid ? renderGraph.ImportTexture(map.far) : TextureHandle.nullHandle;

            if (sky.farDue)
            {
                var farProjection = GL.GetGPUProjectionMatrix(sky.farProjection, true);

                far = renderGraph.ImportTexture(AcquireFar(map));

                RecordTopDepth(renderGraph, frameData, "HB Style Shelter Far", sky.farCulling, sky.farView,
                               farProjection, far, false, liquids, TextureHandle.nullHandle);

                map.farViewProjection = farProjection * sky.farView;
                map.farHeight = HeightParams(sky.farOrigin.y, FarResolution);
                map.farCentre = sky.farCentre;
                map.farValid = true;
            }

            var bound = depth;
            var reach = Reach(StyleEnvironment.targetProfile.weather.roofMargin, resolution);

            var under = TextureHandle.nullHandle;

            map.bound = map.depth;
            map.boundUnder = null;

            if (m_UndersideShader != null)
            {
                map.boundUnder = AcquireUnder(map, resolution);
                under = renderGraph.ImportTexture(map.boundUnder);

                RecordUnderside(renderGraph, frameData, sky, skyProjection, under);
            }

            if (reach > 0 && m_DilateXKernel >= 0 && m_DilateZKernel >= 0 && under.IsValid())
            {
                var carry = renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
                {
                    format = GraphicsFormat.R32G32_SFloat,
                    enableRandomWrite = true,
                    name = "HB Style Shelter Overhangs"
                });

                map.bound = AcquireDilated(map, resolution);
                bound = renderGraph.ImportTexture(map.bound);

                RecordDilate(renderGraph, "HB Style Shelter Margin X", m_DilateXKernel, depth, under, s_UnderId,
                             carry, s_CarryTargetId, height, reach);
                RecordDilate(renderGraph, "HB Style Shelter Margin Z", m_DilateZKernel, depth, carry, s_CarryId,
                             bound, s_TargetId, height, reach);
            }

            var level = liquids
                ? RecordRoofLevel(renderGraph, frameData, sky, skyProjection, resolution, mask)
                : TextureHandle.nullHandle;

            var cover = m_UncoverKernel >= 0
                ? RecordUncover(renderGraph, map, bound, level, viewProjection, height)
                : TextureHandle.nullHandle;

            var drops = bound;
            var blocking = 0;
            var masks = m_MasksKernel >= 0
                ? StyleWeatherMasks.Collect(new Vector2(sky.origin.x, sky.origin.z), Extent * 0.5f, s_Masks,
                                            out blocking)
                : 0;

            map.boundDrops = map.bound;

            if (masks > 0)
            {
                map.bound = AcquireMasked(ref map.masked, resolution, "_HB_ShelterMasked");
                map.boundDrops = AcquireMasked(ref map.drops, resolution, "_HB_ShelterMaskedDrops");

                var surface = renderGraph.ImportTexture(map.bound);

                drops = renderGraph.ImportTexture(map.boundDrops);

                RecordMasks(renderGraph, bound, surface, drops, viewProjection, height, masks, blocking);
                bound = surface;
            }

            using (var builder = renderGraph.AddUnsafePass<BindData>("HB Style Shelter Bind", out var data))
            {
                data.viewProjection = viewProjection;
                data.height = height;
                data.farViewProjection = map.farViewProjection;
                data.farHeight = map.farHeight;
                data.noUnderside = !under.IsValid();
                data.noCover = !cover.IsValid();
                data.noCoverTexture = m_NoCover;
                data.hasFar = far.IsValid();

                builder.UseTexture(bound, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(bound, s_DepthId);

                if (masks > 0)
                {
                    builder.UseTexture(drops, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(drops, s_DropsId);
                }
                else
                {
                    builder.SetGlobalTextureAfterPass(bound, s_DropsId);
                }

                if (under.IsValid())
                {
                    builder.UseTexture(under, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(under, s_UnderId);
                }

                if (cover.IsValid())
                {
                    builder.UseTexture(cover, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(cover, s_CoverId);
                }

                if (far.IsValid())
                {
                    builder.UseTexture(far, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(far, s_FarDepthId);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (BindData d, UnsafeGraphContext context) =>
                {
                    if (d.noUnderside)
                        context.cmd.SetGlobalTexture(s_UnderId, Texture2D.blackTexture);

                    if (d.noCover)
                        context.cmd.SetGlobalTexture(s_CoverId, d.noCoverTexture);

                    context.cmd.SetGlobalMatrix(s_ViewProjId, d.viewProjection);
                    context.cmd.SetGlobalVector(s_HeightId, d.height);
                    context.cmd.SetGlobalMatrix(s_FarViewProjId, d.farViewProjection);
                    context.cmd.SetGlobalVector(s_FarHeightId, d.farHeight);
                    context.cmd.SetGlobalVector(s_StateId, State(d.hasFar));
                });
            }

            if (StyleDebug.previewShelter && m_PreviewKernel >= 0 && StyleDebug.IsPreviewCamera(camera))
            {
                var preview = PreviewTarget(resolution);
                var area = new Vector4(sky.origin.x, sky.origin.z, Extent, camera.transform.position.y);

                StyleDebug.shelterPreview = preview.rt;
                RecordKernel(renderGraph, "HB Style Shelter Preview", m_PreviewKernel, s_PreviewId, preview, bound,
                             TextureHandle.nullHandle, viewProjection, height, area, false);
            }

            if (m_HeightsKernel >= 0 && StyleWeatherBodies.wanted && camera == KeyCamera())
            {
                var area = new Vector4(sky.origin.x, sky.origin.z, Extent, resolution);

                RecordKernel(renderGraph, "HB Style Shelter Heights", m_HeightsKernel, s_HeightsId,
                             StyleWeatherBodies.Target(resolution), bound, under, viewProjection, height, area,
                             true);
                StyleWeatherBodies.Written(area, camera);
            }
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
                m_Maps[stale].depth?.Release();
                m_Maps[stale].dilated?.Release();
                m_Maps[stale].under?.Release();
                m_Maps[stale].far?.Release();
                m_Maps[stale].masked?.Release();
                m_Maps[stale].drops?.Release();
                m_Maps[stale].coverState?.Release();
                m_Maps[stale].coverNext?.Release();
                m_Maps[stale].cover?.Release();
                m_Maps.Remove(stale);
            }

            if (!m_Maps.TryGetValue(camera, out var map))
            {
                map = new Map();
                m_Maps.Add(camera, map);
            }

            if (map.depth == null || map.depth.rt.width != resolution)
            {
                map.depth?.Release();

                var descriptor = new RenderTextureDescriptor(resolution, resolution, GraphicsFormat.None,
                                                             GraphicsFormat.D16_UNorm);

                map.depth = RTHandles.Alloc(descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                                            name: "_HB_ShelterDepth");
            }

            map.lastUsed = now;

            return map;
        }

        static RTHandle AcquireDilated(Map map, int resolution)
        {
            if (map.dilated == null || map.dilated.rt.width != resolution)
            {
                map.dilated?.Release();
                map.dilated = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R32_SFloat,
                                              filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                                              enableRandomWrite: true, name: "_HB_ShelterMargin");
            }

            return map.dilated;
        }

        static RTHandle AcquireMasked(ref RTHandle handle, int resolution, string name)
        {
            if (handle == null || handle.rt.width != resolution)
            {
                handle?.Release();
                handle = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R32_SFloat,
                                         filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                                         enableRandomWrite: true, name: name);
            }

            return handle;
        }

        static RTHandle AcquireCover(ref RTHandle handle, int resolution, GraphicsFormat format, FilterMode filter,
                                     string name)
        {
            if (handle == null || handle.rt.width != resolution)
            {
                handle?.Release();
                handle = RTHandles.Alloc(resolution, resolution, colorFormat: format, filterMode: filter,
                                         wrapMode: TextureWrapMode.Clamp, enableRandomWrite: true, name: name);
            }

            return handle;
        }

        static Vector2 CoverShift(Matrix4x4 from, Matrix4x4 to, int resolution)
        {
            var moved = (from.GetColumn(3) - to.GetColumn(3)) * 0.5f * resolution;

            return new Vector2(Mathf.Round(moved.x),
                               Mathf.Round(SystemInfo.graphicsUVStartsAtTop ? -moved.y : moved.y));
        }

        TextureHandle RecordUncover(RenderGraph renderGraph, Map map, TextureHandle top, TextureHandle liquid,
                                    Matrix4x4 viewProjection, Vector4 height)
        {
            var resolution = (int)height.z;
            var keep = map.coverState != null && map.coverState.rt.width == resolution;
            var settings = StyleEnvironment.targetProfile.weather;
            var rate = Application.isPlaying ? Time.deltaTime * settings.speed : float.PositiveInfinity;
            var shift = keep
                ? CoverShift(map.coverViewProjection, viewProjection, resolution)
                : new Vector2(resolution, resolution);

            AcquireCover(ref map.coverState, resolution, GraphicsFormat.R32G32_SFloat, FilterMode.Point,
                         "_HB_ShelterCoverState");
            AcquireCover(ref map.coverNext, resolution, GraphicsFormat.R32G32_SFloat, FilterMode.Point,
                         "_HB_ShelterCoverNext");
            AcquireCover(ref map.cover, resolution, GraphicsFormat.R32G32_SFloat, FilterMode.Bilinear,
                         "_HB_ShelterCover");

            var from = renderGraph.ImportTexture(map.coverState);
            var state = renderGraph.ImportTexture(map.coverNext);
            var cover = renderGraph.ImportTexture(map.cover);

            using var builder = renderGraph.AddComputePass<UncoverData>("HB Style Shelter Uncover", out var data,
                                                                        profilingSampler);

            data.compute = m_Compute;
            data.kernel = m_UncoverKernel;
            data.top = top;
            data.hasLiquid = liquid.IsValid();
            data.liquid = data.hasLiquid ? liquid : top;
            data.from = from;
            data.state = state;
            data.cover = cover;
            data.height = height;
            data.step = new Vector4(shift.x, shift.y, Mathf.Min(rate / settings.uncoverTime, 1f),
                                    data.hasLiquid ? 1f : 0f);
            data.groups = (resolution + 7) / 8;

            builder.UseTexture(data.top, AccessFlags.Read);

            if (data.hasLiquid)
                builder.UseTexture(data.liquid, AccessFlags.Read);

            builder.UseTexture(data.from, AccessFlags.Read);
            builder.UseTexture(data.state, AccessFlags.Write);
            builder.UseTexture(data.cover, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (UncoverData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_SourceId, d.top);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_LiquidId, d.liquid);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_CoverFromId, d.from);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_CoverStateId, d.state);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_CoverId, d.cover);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_CoverStepId, d.step);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });

            (map.coverState, map.coverNext) = (map.coverNext, map.coverState);
            map.coverViewProjection = viewProjection;

            return cover;
        }

        void RecordMasks(RenderGraph renderGraph, TextureHandle source, TextureHandle surface, TextureHandle drops,
                         Matrix4x4 viewProjection, Vector4 height, int count, int blocking)
        {
            using var builder = renderGraph.AddComputePass<MaskData>("HB Style Shelter Masks", out var data,
                                                                     profilingSampler);

            data.compute = m_Compute;
            data.kernel = m_MasksKernel;
            data.source = source;
            data.surface = surface;
            data.drops = drops;
            data.viewProjection = viewProjection;
            data.height = height;
            data.count = new Vector4(count, blocking, Extent / height.z, 0f);
            data.masks ??= new Vector4[StyleWeatherMasks.Limit];
            data.groups = ((int)height.z + 7) / 8;

            Array.Copy(s_Masks, data.masks, StyleWeatherMasks.Limit);

            builder.UseTexture(source, AccessFlags.Read);
            builder.UseTexture(surface, AccessFlags.Write);
            builder.UseTexture(drops, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (MaskData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_SourceId, d.source);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_TargetId, d.surface);
                cmd.SetComputeTextureParam(d.compute, d.kernel, s_DropsTargetId, d.drops);
                cmd.SetComputeMatrixParam(d.compute, s_ViewProjId, d.viewProjection);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_MaskCountId, d.count);
                cmd.SetComputeVectorArrayParam(d.compute, s_MasksId, d.masks);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        static int Reach(float margin, int resolution)
        {
            return margin > 0f ? Mathf.Max(1, Mathf.RoundToInt(margin / (Extent / resolution))) : 0;
        }

        bool Cutters(Vector3 origin)
        {
            m_Cuts.Clear();

            if (m_CutMaterial == null)
                return false;

            var area = new Bounds(new Vector3(origin.x, origin.y - Depth * 0.5f, origin.z),
                                  new Vector3(Extent, Depth, Extent));
            var keeping = false;

            foreach (var cutter in HBStyleLiquidCutter.enabledCutters)
            {
                if (cutter == null || cutter.affects == 0)
                    continue;

                keeping |= cutter.mode == HBStyleLiquidCutMode.KeepInside;

                if (!cutter.Spans(out var bounds) || !bounds.Intersects(area))
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

        void Roof(RoofData data, ContextContainer frameData, Matrix4x4 skyView, Matrix4x4 skyProjection, float reach,
                  bool cut)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            data.camera = cameraData.camera;
            data.reach = reach;
            data.cut = cut;
            data.skyView = skyView;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);
        }

        static void DrawRoof(RasterCommandBuffer cmd, Camera camera, string pass, float reach, bool cut)
        {
            cmd.SetGlobalVector(s_CutStateId, cut ? s_CutOn : Vector4.zero);
            HBStyleLiquid.DrawRoof(cmd, camera, pass, reach);
            cmd.SetGlobalVector(s_CutStateId, Vector4.zero);
        }

        static void DrawRoofView(RasterCommandBuffer cmd, RoofData d, string pass)
        {
            RenderingUtils.SetViewAndProjectionMatrices(cmd, d.skyView, d.skyProjection, false);
            DrawRoof(cmd, d.camera, pass, d.reach, d.cut);
            RenderingUtils.SetViewAndProjectionMatrices(cmd, d.cameraView, d.cameraProjection, true);
        }

        TextureHandle RecordRoofCut(RenderGraph renderGraph, ContextContainer frameData, View sky,
                                    Matrix4x4 skyProjection, int resolution)
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

            using (var builder = renderGraph.AddRasterRenderPass<RoofData>("HB Style Shelter Liquid Ids", out var data,
                                                                          profilingSampler))
            {
                Roof(data, frameData, sky.view, skyProjection, Extent * RoofReach, false);

                builder.SetRenderAttachment(ids, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(ids, s_CutIdsId);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (RoofData d, RasterGraphContext context) =>
                    DrawRoofView(context.cmd, d, RoofIdsPass));
            }

            using (var builder = renderGraph.AddRasterRenderPass<RoofCutData>("HB Style Shelter Liquid Cut",
                                                                             out var data, profilingSampler))
            {
                data.cuts = m_Cuts;
                data.material = m_CutMaterial;
                data.block = m_CutBlock;
                data.view ??= new RoofData();
                Roof(data.view, frameData, sky.view, skyProjection, Extent * RoofReach, false);

                builder.UseTexture(ids, AccessFlags.Read);
                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(mask, s_CutMaskId);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (RoofCutData d, RasterGraphContext context) =>
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

        TextureHandle RecordRoofLevel(RenderGraph renderGraph, ContextContainer frameData, View sky,
                                      Matrix4x4 skyProjection, int resolution, TextureHandle mask)
        {
            var level = renderGraph.CreateTexture(new TextureDesc(resolution, resolution)
            {
                format = GraphicsFormat.R32_SFloat,
                filterMode = FilterMode.Point,
                name = "HB Style Shelter Liquid Level"
            });

            using var builder = renderGraph.AddRasterRenderPass<RoofData>("HB Style Shelter Liquid Level", out var data,
                                                                         profilingSampler);

            Roof(data, frameData, sky.view, skyProjection, Extent * RoofReach, mask.IsValid());

            if (mask.IsValid())
                builder.UseTexture(mask, AccessFlags.Read);

            builder.SetRenderAttachment(level, 0);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (RoofData d, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.Color, s_NoLiquid, 1f, 0u);
                DrawRoofView(context.cmd, d, RoofLevelPass);
            });

            return level;
        }

        void RecordTopDepth(RenderGraph renderGraph, ContextContainer frameData, string name, CullingResults culling,
                            Matrix4x4 skyView, Matrix4x4 skyProjection, TextureHandle target, bool near, bool roof,
                            TextureHandle mask)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<SkyData>(name, out var data, profilingSampler);

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
                culling, drawing, new FilteringSettings(s_Queues, cameraData.camera.cullingMask))
            {
                tagValues = tags,
                stateBlocks = states,
                isPassTagName = false
            });
            data.near = near;
            data.roof = roof;
            data.cut = mask.IsValid();
            data.reach = (near ? Extent : FarExtent) * RoofReach;
            data.camera = cameraData.camera;
            data.mask = cameraData.camera.cullingMask;
            data.skyView = skyView;
            data.skyProjection = skyProjection;
            data.cameraView = cameraData.GetViewMatrix();
            data.cameraProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),
                                                              !resourceData.isActiveTargetBackBuffer);

            builder.UseRendererList(data.list);
            builder.SetRenderAttachmentDepth(target, AccessFlags.Write);

            if (mask.IsValid())
                builder.UseTexture(mask, AccessFlags.Read);

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

                if (d.roof)
                    DrawRoof(context.cmd, d.camera, RoofPass, d.reach, d.cut);

                context.cmd.SetGlobalFloat(s_PassId, 0f);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.cameraView, d.cameraProjection, true);
            });
        }

        static RTHandle AcquireFar(Map map)
        {
            if (map.far == null)
            {
                var descriptor = new RenderTextureDescriptor(FarResolution, FarResolution, GraphicsFormat.None,
                                                             GraphicsFormat.D16_UNorm);

                map.far = RTHandles.Alloc(descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                                          name: "_HB_ShelterFarDepth");
            }

            return map.far;
        }

        static RTHandle AcquireUnder(Map map, int resolution)
        {
            if (map.under == null || map.under.rt.width != resolution)
            {
                map.under?.Release();

                var descriptor = new RenderTextureDescriptor(resolution, resolution, GraphicsFormat.None,
                                                             GraphicsFormat.D16_UNorm);

                map.under = RTHandles.Alloc(descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                                            name: "_HB_ShelterUnder");
            }

            return map.under;
        }

        void RecordUnderside(RenderGraph renderGraph, ContextContainer frameData, View sky, Matrix4x4 skyProjection,
                             TextureHandle under)
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
            data.plants = m_UndersideMaterial;
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
                StylePlantSystem.DrawShelter(context.cmd, d.mask, d.plants);
                RenderingUtils.SetViewAndProjectionMatrices(context.cmd, d.cameraView, d.cameraProjection, true);
            });
        }

        void RecordDilate(RenderGraph renderGraph, string name, int kernel, TextureHandle source, TextureHandle extra,
                          int extraId, TextureHandle target, int targetId, Vector4 height, int reach)
        {
            using var builder = renderGraph.AddComputePass<DilateData>(name, out var data, profilingSampler);

            data.compute = m_Compute;
            data.kernel = kernel;
            data.source = source;
            data.extra = extra;
            data.extraId = extraId;
            data.target = target;
            data.targetId = targetId;
            data.height = height;
            data.reach = new Vector4(reach, 0f, 0f, 0f);
            data.groups = ((int)height.z + 7) / 8;

            builder.UseTexture(source, AccessFlags.Read);
            builder.UseTexture(extra, AccessFlags.Read);
            builder.UseTexture(target, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (DilateData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_SourceId, d.source);
                cmd.SetComputeTextureParam(d.compute, d.kernel, d.extraId, d.extra);
                cmd.SetComputeTextureParam(d.compute, d.kernel, d.targetId, d.target);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_DilateId, d.reach);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
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

        void RecordKernel(RenderGraph renderGraph, string name, int kernel, int outputId, RTHandle output,
                          TextureHandle depth, TextureHandle under, Matrix4x4 viewProjection, Vector4 height,
                          Vector4 area, bool withUnder)
        {
            var resolution = (int)height.z;
            var preview = renderGraph.ImportTexture(output);

            using var builder = renderGraph.AddComputePass<PreviewData>(name, out var data, profilingSampler);

            data.compute = m_Compute;
            data.kernel = kernel;
            data.depth = depth;
            data.under = under;
            data.withUnder = withUnder;
            data.preview = preview;
            data.previewId = outputId;
            data.viewProjection = viewProjection;
            data.height = height;
            data.area = area;
            data.groups = (resolution + 7) / 8;

            builder.UseTexture(depth, AccessFlags.Read);

            if (under.IsValid())
                builder.UseTexture(under, AccessFlags.Read);

            builder.UseTexture(preview, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PreviewData d, ComputeGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.SetComputeTextureParam(d.compute, d.kernel, s_DepthId, d.depth);

                if (d.withUnder)
                    cmd.SetComputeTextureParam(d.compute, d.kernel, s_UnderId, d.under.IsValid() ? d.under : d.depth);

                cmd.SetComputeTextureParam(d.compute, d.kernel, d.previewId, d.preview);
                cmd.SetComputeMatrixParam(d.compute, s_ViewProjId, d.viewProjection);
                cmd.SetComputeVectorParam(d.compute, s_HeightId, d.height);
                cmd.SetComputeVectorParam(d.compute, s_PreviewAreaId, d.area);
                cmd.DispatchCompute(d.compute, d.kernel, d.groups, d.groups, 1);
            });
        }

        static Vector4 HeightParams(float originY, int resolution)
        {
            var span = Depth - Near;
            var reversed = SystemInfo.usesReversedZBuffer;

            return new Vector4(reversed ? span : -span, reversed ? originY - Depth : originY - Near, resolution, 0f);
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= Prepare;

            CoreUtils.Destroy(m_UndersideMaterial);
            m_UndersideMaterial = null;
            CoreUtils.Destroy(m_CutMaterial);
            m_CutMaterial = null;
            CoreUtils.Destroy(m_NoCover);
            m_NoCover = null;

            m_Views.Clear();
            m_Wanted.Clear();

            foreach (var map in m_Maps.Values)
            {
                map.depth?.Release();
                map.dilated?.Release();
                map.under?.Release();
                map.far?.Release();
                map.masked?.Release();
                map.drops?.Release();
                map.coverState?.Release();
                map.coverNext?.Release();
                map.cover?.Release();
            }

            m_Maps.Clear();
            m_Preview?.Release();
            m_Preview = null;
            StyleDebug.shelterPreview = null;
        }
    }
}
