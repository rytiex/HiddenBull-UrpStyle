using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StyleGrassPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/Style Grass";
        public const string ComputePath = "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrass.compute";
        public const int Layers = HBStyleTerrain.Limit;

        const string FlowerKeyword = "_HB_GRASS_FLOWERS";
        const string DistantKeyword = "_HB_GRASS_FAR";

        const int InstanceStride = 32;
        const int GrassIndices = 12;
        const int FlowerIndices = 12;
        const int DistantIndices = 6;
        const int DepthPass = 0;
        const int ColourPass = 1;
        const int MaxGrid = 3072;
        const int MaxVariants = 8;
        const int MinCapacity = 16384;
        const int MaxCapacity = 1048576;
        const float Spacing = 0.2f;
        const float CullMargin = 4f;
        const float FadeRange = 0.25f;
        const float NearShare = 0.3f;
        const float FullShare = 0.25f;
        const float ViewShare = 0.5f;
        const float GrassMargin = 1.5f;
        const float DistantShare = 0.6f;
        const float SplitShare = 0.18f * 1.15f;
        const float FlowersPerArea = 5f;
        const float FlowerMargin = 2f;
        const double LightmapRefresh = 1.0;
        const float EdgeLeast = 0.02f;

        const int Grass = 0;
        const int Flowers = 1;
        const int Distant = 2;
        const int Buffers = 3;

        static readonly uint[] s_Corners = { 0u, 1u, 2u, 0u, 2u, 3u, 4u, 5u, 6u, 4u, 6u, 7u };

        static readonly int[] s_InstanceIds =
        {
            Shader.PropertyToID("_HB_GrassBlades"),
            Shader.PropertyToID("_HB_GrassFlowers"),
            Shader.PropertyToID("_HB_GrassDistant")
        };

        static readonly int s_HeightmapId = Shader.PropertyToID("_HB_GrassHeightmap");
        static readonly int s_ControlId = Shader.PropertyToID("_HB_GrassControl");
        static readonly int s_HolesId = Shader.PropertyToID("_HB_GrassHoles");
        static readonly int s_ViewProjId = Shader.PropertyToID("_HB_GrassViewProj");
        static readonly int s_CameraId = Shader.PropertyToID("_HB_GrassCamera");
        static readonly int s_SidesId = Shader.PropertyToID("_HB_GrassSides");
        static readonly int s_GridId = Shader.PropertyToID("_HB_GrassGrid");
        static readonly int s_TerrainOriginId = Shader.PropertyToID("_HB_GrassTerrainOrigin");
        static readonly int s_TerrainSizeId = Shader.PropertyToID("_HB_GrassTerrainSize");
        static readonly int s_ResolutionId = Shader.PropertyToID("_HB_GrassResolution");
        static readonly int s_SettingsId = Shader.PropertyToID("_HB_GrassSettings");
        static readonly int s_RingsId = Shader.PropertyToID("_HB_GrassRings");
        static readonly int s_AlbedoId = Shader.PropertyToID("_HB_GrassAlbedo");
        static readonly int s_TilingId = Shader.PropertyToID("_HB_GrassTiling");
        static readonly int s_LookId = Shader.PropertyToID("_HB_GrassLook");
        static readonly int s_VaryId = Shader.PropertyToID("_HB_GrassVary");
        static readonly int s_VaryShapeId = Shader.PropertyToID("_HB_GrassVaryShape");
        static readonly int s_VaryMiscId = Shader.PropertyToID("_HB_GrassVaryMisc");
        static readonly int s_LayerId = Shader.PropertyToID("_HB_GrassLayer");
        static readonly int s_TipId = Shader.PropertyToID("_HB_GrassTip");
        static readonly int s_FlowerId = Shader.PropertyToID("_HB_GrassFlower");
        static readonly int s_WeatherId = Shader.PropertyToID("_HB_GrassWeather");

        static readonly int s_BufferId = Shader.PropertyToID("_HB_GrassBuffer");
        static readonly int s_TextureId = Shader.PropertyToID("_HB_GrassTexture");
        static readonly int s_DrawId = Shader.PropertyToID("_HB_GrassDraw");
        static readonly int s_StyleId = Shader.PropertyToID("_HB_GrassStyle");
        static readonly int s_BrushId = Shader.PropertyToID("_HB_GrassBrush");
        static readonly int s_BrushLookId = Shader.PropertyToID("_HB_GrassBrushLook");
        static readonly int s_GlowSunId = Shader.PropertyToID("_HB_GrassGlowSun");
        static readonly int s_GlowShadeId = Shader.PropertyToID("_HB_GrassGlowShade");
        static readonly int s_FarId = Shader.PropertyToID("_HB_GrassFar");
        static readonly int s_GroundId = Shader.PropertyToID("_HB_GrassGround");
        static readonly int s_LightmapSTId = Shader.PropertyToID("_HB_GrassLightmapST");
        static readonly int s_LightmapId = Shader.PropertyToID("_HB_GrassLightmap");
        static readonly int s_ShadowMaskId = Shader.PropertyToID("_HB_GrassShadowMask");
        static readonly int s_BendersId = Shader.PropertyToID("_HB_GrassBenders");
        static readonly int s_BenderLooksId = Shader.PropertyToID("_HB_GrassBenderLooks");

        static readonly int s_LayerStrokesId = Shader.PropertyToID("_LayerStrokes");
        static readonly int s_HeightTransitionId = Shader.PropertyToID("_HeightTransition");
        static readonly int s_ReceiveWeatherId = Shader.PropertyToID("_ReceiveWeather");
        static readonly int s_DiffuseWrapId = Shader.PropertyToID("_DiffuseWrap");
        static readonly int s_DiffuseSoftnessId = Shader.PropertyToID("_DiffuseSoftness");
        static readonly int s_HalfToneId = Shader.PropertyToID("_HalfTone");
        static readonly int s_BrushEnabledId = Shader.PropertyToID("_BrushEnabled");
        static readonly int s_BrushScaleId = Shader.PropertyToID("_BrushScale");
        static readonly int s_BrushReliefId = Shader.PropertyToID("_BrushRelief");
        static readonly int s_BrushShadingId = Shader.PropertyToID("_BrushShading");
        static readonly int s_BrushAmbientId = Shader.PropertyToID("_BrushAmbient");
        static readonly int s_BrushAlbedoId = Shader.PropertyToID("_BrushAlbedo");
        static readonly int s_BakedTonesId = Shader.PropertyToID("_BakedTones");
        static readonly int s_BakedSoftnessId = Shader.PropertyToID("_BakedSoftness");

        static readonly int s_GrassTipLiftId = Shader.PropertyToID("_GrassTipLift");
        static readonly int s_GrassGlowSunId = Shader.PropertyToID("_GrassGlowSun");
        static readonly int s_GrassGlowShadeId = Shader.PropertyToID("_GrassGlowShade");
        static readonly int s_GrassTextureId = Shader.PropertyToID("_GrassTexture");
        static readonly int s_FlowerTextureId = Shader.PropertyToID("_FlowerTexture");
        static readonly int s_FlowerVariantsId = Shader.PropertyToID("_FlowerVariants");


        readonly GraphicsBuffer[] m_Instances = new GraphicsBuffer[Buffers];
        readonly GraphicsBuffer[] m_Arguments = new GraphicsBuffer[Buffers];
        readonly Material[] m_Materials = new Material[Buffers];
        readonly int[] m_Capacity = new int[Buffers];

        GraphicsBuffer m_Corners;
        readonly Vector4[] m_Benders = new Vector4[HBStyleGrassBender.Limit];
        readonly Vector4[] m_BenderLooks = new Vector4[HBStyleGrassBender.Limit];

        ComputeShader m_Compute;
        HBStyleGrassQualitySettings m_Settings;
        LightmapData[] m_Lightmaps;
        double m_LightmapsRead;
        int m_BenderCount;
        int m_Kernel;
        bool m_Ready;

        public bool isReady => m_Ready && m_Materials[Grass] != null;

        public StyleGrassPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            profilingSampler = new ProfilingSampler("HiddenBull Style Grass");
        }

        public void Setup(Shader shader, ComputeShader compute, HBStyleGrassQualitySettings settings)
        {
            m_Settings = settings;

            if (shader == null || compute == null)
            {
                m_Ready = false;
                return;
            }

            if (m_Materials[Grass] == null || m_Materials[Grass].shader != shader)
            {
                CoreUtils.Destroy(m_Materials[Grass]);
                CoreUtils.Destroy(m_Materials[Flowers]);
                CoreUtils.Destroy(m_Materials[Distant]);

                m_Materials[Grass] = CoreUtils.CreateEngineMaterial(shader);
                m_Materials[Flowers] = CoreUtils.CreateEngineMaterial(shader);
                m_Materials[Flowers].EnableKeyword(FlowerKeyword);
                m_Materials[Distant] = CoreUtils.CreateEngineMaterial(shader);
                m_Materials[Distant].EnableKeyword(DistantKeyword);
            }

            if (compute == m_Compute && m_Ready)
                return;

            m_Compute = compute;
            m_Ready = compute.HasKernel("Place");

            if (m_Ready)
                m_Kernel = compute.FindKernel("Place");
        }

        class PlaceData
        {
            public ComputeShader compute;
            public int kernel;
            public Texture heightmap;
            public Texture noise;
            public Texture control;
            public Texture holes;
            public Matrix4x4 viewProjection;
            public readonly Vector4[] sides = new Vector4[4];
            public Vector4 camera;
            public Vector4 grid;
            public Vector4 terrainOrigin;
            public Vector4 terrainSize;
            public Vector4 resolution;
            public Vector4 settings;
            public Vector4 rings;
            public Vector2Int groups;
            public readonly GraphicsBuffer[] instances = new GraphicsBuffer[Buffers];
            public readonly GraphicsBuffer[] arguments = new GraphicsBuffer[Buffers];
            public Texture albedo;
            public Vector4[] tiling;
            public Vector4[] look;
            public Vector4[] vary;
            public Vector4[] varyShape;
            public Vector4[] varyMisc;
            public Vector4[] layer;
            public Vector4[] tip;
            public Vector4[] flower;
            public Vector4[] weather;
        }

        class DrawData
        {
            public Material material;
            public GraphicsBuffer corners;
            public GraphicsBuffer arguments;
            public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                return;

            if (m_Settings == null || !m_Settings.TryGetLevel(out var level))
                return;

            m_BenderCount = HBStyleGrassBender.Collect(cameraData.camera.transform.position, level.distance, m_Benders,
                                                     m_BenderLooks);

            var resourceData = frameData.Get<UniversalResourceData>();
            var position = cameraData.camera.transform.position;
            var viewProjection = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true)
                               * cameraData.GetViewMatrix();

            var stage = cameraData.camera.scene;

            foreach (var owner in HBStyleTerrain.active)
            {
                if (owner == null || !owner.ready)
                    continue;

                if (stage.IsValid() && owner.gameObject.scene != stage)
                    continue;

                if ((cameraData.camera.cullingMask & (1 << owner.gameObject.layer)) == 0)
                    continue;

                var terrain = owner.GetComponent<Terrain>();
                var material = terrain.materialTemplate;
                var data = terrain.terrainData;

                if (material == null || data == null || material.shader == null ||
                    material.shader.name != HBStyleTerrain.ShaderName)
                {
                    continue;
                }

                var grass = material.GetTexture(s_GrassTextureId);
                var blooms = material.GetTexture(s_FlowerTextureId);

                if (!Grows(owner, grass != null, blooms != null))
                    continue;

                var origin = terrain.GetPosition();
                var reach = data.size + Vector3.one * (level.distance * 2f);

                if (!new Bounds(origin + data.size * 0.5f, reach).Contains(position))
                    continue;

                Record(renderGraph, resourceData, owner, terrain, level, grass, blooms, cameraData.camera,
                       viewProjection);
            }
        }

        static bool Grows(HBStyleTerrain owner, bool grass, bool flowers)
        {
            for (var row = 0; row < owner.layerCount; row++)
            {
                if (grass && owner.grass[row].x > 0f)
                    return true;

                if (flowers && owner.flowers[row].x > 0f)
                    return true;
            }

            return false;
        }

        void Record(RenderGraph renderGraph, UniversalResourceData resourceData, HBStyleTerrain owner, Terrain terrain,
                    HBStyleGrassLevel level, Texture grass, Texture blooms, Camera camera,
                    Matrix4x4 viewProjection)
        {
            var position = camera.transform.position;
            var material = terrain.materialTemplate;
            var data = terrain.terrainData;
            var origin = terrain.GetPosition();
            var count = owner.layerCount;

            Allocate(level, grass != null);

            var spacing = Spacing / Mathf.Sqrt(level.density);
            var cells = Mathf.Min(Mathf.CeilToInt(level.distance * 2f / spacing / 8f) * 8, MaxGrid);
            var reach = Mathf.Min(level.distance, cells * spacing * 0.5f);
            var variants = blooms != null
                ? Mathf.Clamp(Mathf.RoundToInt(material.GetFloat(s_FlowerVariantsId)), 1, MaxVariants)
                : 0;
            var most = 0f;

            for (var row = 0; row < count; row++)
                most = Mathf.Max(most, owner.flowers[row].x);

            var low = new Vector2(Mathf.Floor(position.x / spacing) - cells / 2,
                                  Mathf.Floor(position.z / spacing) - cells / 2);
            var span = Seen(camera, reach, spacing, low, cells, out var first);

            using (var builder = renderGraph.AddUnsafePass<PlaceData>(
                       "HiddenBull Style Grass Place", out var passData, profilingSampler))
            {
                passData.compute = m_Compute;
                passData.kernel = m_Kernel;
                passData.heightmap = data.heightmapTexture;
                passData.noise = StyleTerrainNoise.texture;
                passData.control = owner.layerControl;
                passData.albedo = owner.arrays.albedo;
                passData.holes = data.holesTexture != null ? data.holesTexture : Texture2D.whiteTexture;
                passData.viewProjection = viewProjection;
                passData.camera = position;
                Sides(camera, passData.sides);
                passData.grid = new Vector4(first.x, first.y, spacing, cells);
                passData.terrainOrigin = origin;
                passData.terrainSize = data.size;
                passData.resolution = new Vector4(data.heightmapResolution, data.alphamapResolution, 0f, 0f);
                passData.settings = new Vector4(reach, material.GetFloat(s_GrassTipLiftId), count,
                                                material.GetFloat(s_LayerStrokesId));
                passData.rings = new Vector4(variants, most, (count + 3) / 4,
                                             Mathf.Max(material.GetFloat(s_HeightTransitionId), EdgeLeast));
                passData.groups = new Vector2Int((span.x + 7) / 8, (span.y + 7) / 8);
                passData.tiling = owner.tiling;
                passData.look = owner.look;
                passData.vary = owner.vary;
                passData.varyShape = owner.varyShape;
                passData.varyMisc = owner.varyExtra;
                passData.layer = owner.grass;
                passData.tip = owner.grassTip;
                passData.flower = owner.flowers;
                passData.weather = owner.weather;

                for (var buffer = 0; buffer < Buffers; buffer++)
                {
                    passData.instances[buffer] = m_Instances[buffer];
                    passData.arguments[buffer] = m_Arguments[buffer];
                }

                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PlaceData data, UnsafeGraphContext context) => Place(data, context.cmd));
            }

            if (grass != null)
            {
                Draw(renderGraph, resourceData, terrain, Grass, grass, 0, reach);
                Draw(renderGraph, resourceData, terrain, Distant, grass, 0, reach);
            }

            if (variants > 0 && most > 0f)
                Draw(renderGraph, resourceData, terrain, Flowers, blooms, variants, reach);
        }

        void Draw(RenderGraph renderGraph, UniversalResourceData resourceData, Terrain terrain, int buffer,
                  Texture texture, int variants, float reach)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawData>(
                "HiddenBull Style Grass", out var passData, profilingSampler);

            passData.material = m_Materials[buffer];
            passData.arguments = m_Arguments[buffer];
            passData.corners = m_Corners;

            Describe(passData.properties, terrain, buffer, texture, variants, reach);

            builder.UseAllGlobalTextures(true);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (DrawData data, RasterGraphContext context) =>
            {
                var cmd = context.cmd;

                cmd.DrawProceduralIndirect(data.corners, Matrix4x4.identity, data.material, DepthPass,
                                           MeshTopology.Triangles, data.arguments, 0, data.properties);
                cmd.DrawProceduralIndirect(data.corners, Matrix4x4.identity, data.material, ColourPass,
                                           MeshTopology.Triangles, data.arguments, 0, data.properties);
            });
        }

        static Vector2Int Seen(Camera camera, float reach, float spacing, Vector2 low, int cells, out Vector2 first)
        {
            first = low;

            if (camera.orthographic)
                return new Vector2Int(cells, cells);

            var projection = camera.projectionMatrix;
            var view = camera.transform;
            var eye = view.position;
            var across = (reach + CullMargin) / Mathf.Max(Mathf.Abs(projection.m00), 1e-4f);
            var upward = (reach + CullMargin) / Mathf.Max(Mathf.Abs(projection.m11), 1e-4f);
            var ahead = eye + view.forward * reach;
            var least = new Vector2(eye.x - view.forward.x * CullMargin, eye.z - view.forward.z * CullMargin);
            var most = least;

            for (var corner = 0; corner < 4; corner++)
            {
                var point = ahead + view.right * ((corner & 1) == 0 ? -across : across)
                                  + view.up * ((corner & 2) == 0 ? -upward : upward);

                least = Vector2.Min(least, new Vector2(point.x, point.z));
                most = Vector2.Max(most, new Vector2(point.x, point.z));
            }

            var from = Vector2.Max(low, new Vector2(Mathf.Floor(least.x / spacing) - 1f,
                                                    Mathf.Floor(least.y / spacing) - 1f));
            var to = Vector2.Min(low + Vector2.one * cells, new Vector2(Mathf.Ceil(most.x / spacing) + 1f,
                                                                        Mathf.Ceil(most.y / spacing) + 1f));

            first = from;

            return new Vector2Int(Mathf.Max((int)(to.x - from.x), 1), Mathf.Max((int)(to.y - from.y), 1));
        }

        static void Sides(Camera camera, Vector4[] sides)
        {
            if (camera.orthographic)
            {
                for (var side = 0; side < sides.Length; side++)
                    sides[side] = new Vector4(0f, 0f, 0f, 1f);

                return;
            }

            var projection = camera.projectionMatrix;
            var view = camera.transform;
            var across = view.right * Mathf.Abs(projection.m00);
            var upward = view.up * Mathf.Abs(projection.m11);

            Side(sides, 0, view.forward - across, view.position);
            Side(sides, 1, view.forward + across, view.position);
            Side(sides, 2, view.forward - upward, view.position);
            Side(sides, 3, view.forward + upward, view.position);
        }

        static void Side(Vector4[] sides, int side, Vector3 normal, Vector3 eye)
        {
            sides[side] = new Vector4(normal.x, normal.y, normal.z, CullMargin - Vector3.Dot(normal, eye));
        }

        static void Place(PlaceData data, UnsafeCommandBuffer cmd)
        {
            var compute = data.compute;
            var kernel = data.kernel;

            for (var buffer = 0; buffer < Buffers; buffer++)
            {
                cmd.SetBufferCounterValue(data.instances[buffer], 0);
                cmd.SetComputeBufferParam(compute, kernel, s_InstanceIds[buffer], data.instances[buffer]);
            }

            cmd.SetComputeTextureParam(compute, kernel, s_HeightmapId, data.heightmap);
            cmd.SetComputeTextureParam(compute, kernel, StyleTerrainNoise.MapId, data.noise);
            cmd.SetComputeTextureParam(compute, kernel, s_ControlId, data.control);
            cmd.SetComputeTextureParam(compute, kernel, s_HolesId, data.holes);
            cmd.SetComputeMatrixParam(compute, s_ViewProjId, data.viewProjection);
            cmd.SetComputeVectorParam(compute, s_CameraId, data.camera);
            cmd.SetComputeVectorArrayParam(compute, s_SidesId, data.sides);
            cmd.SetComputeVectorParam(compute, s_GridId, data.grid);
            cmd.SetComputeVectorParam(compute, s_TerrainOriginId, data.terrainOrigin);
            cmd.SetComputeVectorParam(compute, s_TerrainSizeId, data.terrainSize);
            cmd.SetComputeVectorParam(compute, s_ResolutionId, data.resolution);
            cmd.SetComputeVectorParam(compute, s_SettingsId, data.settings);
            cmd.SetComputeVectorParam(compute, s_RingsId, data.rings);
            cmd.SetComputeTextureParam(compute, kernel, s_AlbedoId, data.albedo);
            cmd.SetComputeVectorArrayParam(compute, s_TilingId, data.tiling);
            cmd.SetComputeVectorArrayParam(compute, s_LookId, data.look);
            cmd.SetComputeVectorArrayParam(compute, s_VaryId, data.vary);
            cmd.SetComputeVectorArrayParam(compute, s_VaryShapeId, data.varyShape);
            cmd.SetComputeVectorArrayParam(compute, s_VaryMiscId, data.varyMisc);
            cmd.SetComputeVectorArrayParam(compute, s_LayerId, data.layer);
            cmd.SetComputeVectorArrayParam(compute, s_TipId, data.tip);
            cmd.SetComputeVectorArrayParam(compute, s_FlowerId, data.flower);
            cmd.SetComputeVectorArrayParam(compute, s_WeatherId, data.weather);
            cmd.DispatchCompute(compute, kernel, data.groups.x, data.groups.y, 1);

            for (var buffer = 0; buffer < Buffers; buffer++)
                cmd.CopyCounterValue(data.instances[buffer], data.arguments[buffer], sizeof(uint));
        }

        void Describe(MaterialPropertyBlock properties, Terrain terrain, int buffer, Texture texture, int variants,
                      float reach)
        {
            var material = terrain.materialTemplate;
            var origin = terrain.GetPosition();
            var size = terrain.terrainData.size;
            var light = Baked(terrain);
            var baked = light != null && light.lightmapColor != null;

            properties.SetBuffer(s_BufferId, m_Instances[buffer]);
            properties.SetTexture(s_TextureId, texture);
            properties.SetVector(s_DrawId, new Vector4(
                m_Capacity[buffer],
                material.GetFloat(s_ReceiveWeatherId),
                variants,
                baked ? 1f : 0f));
            properties.SetVector(s_StyleId, new Vector4(
                material.GetFloat(s_DiffuseWrapId),
                material.GetFloat(s_DiffuseSoftnessId),
                material.GetFloat(s_HalfToneId),
                m_Settings.translucency ? 1f : 0f));
            properties.SetVector(s_BrushId, new Vector4(
                material.GetFloat(s_BrushScaleId),
                material.GetFloat(s_BrushReliefId),
                material.GetFloat(s_BrushShadingId),
                material.GetFloat(s_BrushAmbientId)));
            properties.SetVector(s_BrushLookId, new Vector4(
                material.GetFloat(s_BrushAlbedoId),
                material.GetFloat(s_BrushEnabledId),
                material.GetFloat(s_BakedTonesId),
                material.GetFloat(s_BakedSoftnessId)));
            properties.SetVector(s_GlowSunId, material.GetColor(s_GrassGlowSunId).linear);
            properties.SetVector(s_GlowShadeId, material.GetColor(s_GrassGlowShadeId).linear);
            properties.SetVector(s_FarId, new Vector4(reach, 1f / (reach * FadeRange), m_BenderCount, 0f));
            properties.SetVectorArray(s_BendersId, m_Benders);
            properties.SetVectorArray(s_BenderLooksId, m_BenderLooks);
            properties.SetVector(s_GroundId, new Vector4(origin.x, origin.z, 1f / size.x, 1f / size.z));
            properties.SetVector(s_LightmapSTId, terrain.lightmapScaleOffset);
            properties.SetTexture(s_LightmapId, baked ? light.lightmapColor : Texture2D.blackTexture);
            properties.SetTexture(s_ShadowMaskId, baked && light.shadowMask != null
                ? light.shadowMask
                : Texture2D.whiteTexture);
        }

        LightmapData Baked(Terrain terrain)
        {
            var now = Time.realtimeSinceStartupAsDouble;

            if (m_Lightmaps == null || now < m_LightmapsRead || now - m_LightmapsRead > LightmapRefresh)
            {
                m_Lightmaps = LightmapSettings.lightmaps;
                m_LightmapsRead = now;
            }

            var index = terrain.lightmapIndex;

            return index >= 0 && index < m_Lightmaps.Length ? m_Lightmaps[index] : null;
        }

        void Allocate(HBStyleGrassLevel level, bool grass)
        {
            var full = level.distance * FullShare;
            var split = level.distance * SplitShare;
            var inner = Mathf.PI * split * split * ViewShare;
            var band = Mathf.PI * Mathf.Max(full * full - split * split, 0f) * ViewShare * DistantShare;
            var outer = 2f * Mathf.PI * full * full * Mathf.Log(1f / NearShare) * ViewShare * DistantShare + band;
            var blades = grass ? level.density / (Spacing * Spacing) * GrassMargin : 0f;

            Resize(Grass, inner * blades);
            Resize(Distant, outer * blades);
            Resize(Flowers, (inner + outer / DistantShare) * FlowersPerArea * FlowerMargin);

            m_Arguments[Grass] ??= Arguments(GrassIndices);
            m_Arguments[Flowers] ??= Arguments(FlowerIndices);
            m_Arguments[Distant] ??= Arguments(DistantIndices);

            if (m_Corners == null)
            {
                m_Corners = new GraphicsBuffer(GraphicsBuffer.Target.Index, s_Corners.Length, sizeof(uint));
                m_Corners.SetData(s_Corners);
            }
        }

        void Resize(int buffer, float expected)
        {
            var capacity = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(expected)), MinCapacity, MaxCapacity);

            if (m_Instances[buffer] != null && m_Capacity[buffer] == capacity)
                return;

            m_Instances[buffer]?.Release();
            m_Instances[buffer] = new GraphicsBuffer(GraphicsBuffer.Target.Append, capacity, InstanceStride);
            m_Capacity[buffer] = capacity;
        }

        static GraphicsBuffer Arguments(int indices)
        {
            var arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);

            arguments.SetData(new[] { (uint)indices, 0u, 0u, 0u, 0u });

            return arguments;
        }

        public void Dispose()
        {
            for (var buffer = 0; buffer < Buffers; buffer++)
            {
                CoreUtils.Destroy(m_Materials[buffer]);
                m_Materials[buffer] = null;

                m_Instances[buffer]?.Release();
                m_Instances[buffer] = null;

                m_Arguments[buffer]?.Release();
                m_Arguments[buffer] = null;
            }

            m_Corners?.Release();
            m_Corners = null;
        }
    }
}
