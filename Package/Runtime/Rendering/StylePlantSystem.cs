using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    static class StylePlantSystem
    {
        internal const string ComputePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/StylePlant.compute";

        const int ThreadGroup = 64;
        const int ArgumentStride = 5;
        const int MinCapacity = 64;
        const int VisibleStride = sizeof(uint) + sizeof(float) * 2;
        const float FadeShare = 0.1f;
        const float MinFade = 0.5f;
        const float BoundsMargin = 1f;
        const float ReferenceView = 60f;
        const string ShelterPass = "DepthOnly";
        const int UndersidePlantPass = 1;
        const float MovingWeather = 2f;

        [StructLayout(LayoutKind.Sequential)]
        struct InstanceData
        {
            public Vector4 row0;
            public Vector4 row1;
            public Vector4 row2;
            public Vector4 inverse0;
            public Vector4 inverse1;
            public Vector4 inverse2;
            public Vector4 sphere;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ProbeData
        {
            public Vector4 shAr;
            public Vector4 shAg;
            public Vector4 shAb;
            public Vector4 shBr;
            public Vector4 shBg;
            public Vector4 shBb;
            public Vector4 shC;
            public Vector4 occlusion;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VisibleData
        {
            public uint index;
            public float fade;
            public float gust;
        }

        internal struct Counts
        {
            public int total;
            public int meshes;
            public int billboards;
            public long triangles;
            public bool valid;
        }

        sealed class Group
        {
            public HBStylePlantAsset asset;
            public readonly List<HBStylePlant> members = new List<HBStylePlant>();
            public InstanceData[] instances = new InstanceData[MinCapacity];
            public ProbeData[] probes = new ProbeData[MinCapacity];
            public Vector3[] positions = new Vector3[MinCapacity];
            public SphericalHarmonicsL2[] harmonics = new SphericalHarmonicsL2[MinCapacity];
            public Vector4[] occlusion = new Vector4[MinCapacity];
            public VisibleData[] entries = new VisibleData[MinCapacity];
            public readonly uint[] farData = new uint[ArgumentStride];
            public GraphicsBuffer instanceBuffer;
            public GraphicsBuffer probeBuffer;
            public GraphicsBuffer near;
            public GraphicsBuffer far;
            public GraphicsBuffer sky;
            public GraphicsBuffer everyone;
            public GraphicsBuffer arguments;
            public GraphicsBuffer skyArguments;
            public GraphicsBuffer farArguments;
            public MaterialPropertyBlock[] blocks;
            public MaterialPropertyBlock[] skyBlocks;
            public int[] shelterPasses;
            public int capacity;
            public int count;
            public int drawCount;
            public bool dirty = true;
            public bool shaped = true;
            public bool reading;
            public long meshTriangles;
            public long billboardTriangles;
            public Counts counts;
            public Bounds bounds;
            public int layer;

            public bool drawable => asset != null && asset.valid && drawCount > 0 && count > 0;
        }

        static readonly int s_InstancesId = Shader.PropertyToID("_HB_PlantInstances");
        static readonly int s_ProbesId = Shader.PropertyToID("_HB_PlantProbes");
        static readonly int s_VisibleId = Shader.PropertyToID("_HB_PlantVisible");
        static readonly int s_NearId = Shader.PropertyToID("_HB_PlantNear");
        static readonly int s_FarId = Shader.PropertyToID("_HB_PlantFar");
        static readonly int s_SkyId = Shader.PropertyToID("_HB_PlantSky");
        static readonly int s_LocalId = Shader.PropertyToID("_HB_PlantLocal");
        static readonly int s_LocalInverseId = Shader.PropertyToID("_HB_PlantLocalInverse");
        static readonly int s_BoundsCenterId = Shader.PropertyToID("_HB_PlantBoundsCenter");
        static readonly int s_BoundsExtentId = Shader.PropertyToID("_HB_PlantBoundsExtent");
        static readonly int s_PlanesId = Shader.PropertyToID("_HB_PlantPlanes");
        static readonly int s_EyeId = Shader.PropertyToID("_HB_PlantEye");
        static readonly int s_RangesId = Shader.PropertyToID("_HB_PlantRanges");
        static readonly int s_SunId = Shader.PropertyToID("_HB_PlantSun");
        static readonly int s_ViewId = Shader.PropertyToID("_HB_PlantView");
        static readonly int s_AirId = Shader.PropertyToID("_HB_PlantAir");
        static readonly int s_TimeId = Shader.PropertyToID("_Time");
        static readonly int s_ReceiveWeatherId = Shader.PropertyToID("_ReceiveWeather");

        static readonly Dictionary<HBStylePlantAsset, Group> s_Groups = new Dictionary<HBStylePlantAsset, Group>();
        static readonly List<Group> s_Order = new List<Group>();
        static readonly Plane[] s_Planes = new Plane[6];
        static readonly Vector4[] s_PlaneVectors = new Vector4[6];

        static ComputeShader s_Compute;
        static CommandBuffer s_Commands;
        static int s_CullKernel = -1;
        static int s_SkyKernel = -1;
        static bool s_Hooked;
        static HBStylePlantAsset s_Watched;
        static double s_WatchUntil;

        static bool ready => s_Compute != null && s_CullKernel >= 0 && SystemInfo.supportsComputeShaders;

        internal static bool drawing => ready;

        internal static IEnumerable<HBStylePlant> members
        {
            get
            {
                foreach (var group in s_Order)
                {
                    foreach (var member in group.members)
                        yield return member;
                }
            }
        }

        internal static void Setup(ComputeShader compute)
        {
            var changed = compute != s_Compute;

            s_Compute = compute;
            s_CullKernel = compute != null && compute.HasKernel("Cull") ? compute.FindKernel("Cull") : -1;
            s_SkyKernel = compute != null && compute.HasKernel("Sky") ? compute.FindKernel("Sky") : -1;

            Hook();

            if (!changed)
                return;

            foreach (var group in s_Order)
                ApplyHidden(group);
        }

        internal static void Add(HBStylePlant member)
        {
            Remove(member);

            var asset = member.asset;

            if (asset == null)
                return;

            Hook();

            var group = Acquire(asset, member.gameObject.layer);

            member.registered = asset;
            member.slot = group.members.Count;
            member.editing = Selected(member);
            group.members.Add(member);
            group.dirty = true;

            member.Hide(ready && asset.valid && !member.editing);
        }

        internal static void Remove(HBStylePlant member)
        {
            member.Hide(false);

            var asset = member.registered;

            member.registered = null;

            if (asset == null || !s_Groups.TryGetValue(asset, out var group))
                return;

            var slot = member.slot;
            var last = group.members.Count - 1;

            member.slot = -1;

            if (slot < 0 || slot > last || group.members[slot] != member)
                return;

            group.members[slot] = group.members[last];
            group.members[slot].slot = slot;
            group.members.RemoveAt(last);
            group.dirty = true;

            Drop(group);
        }

        internal static void Rebuild(HBStylePlantAsset asset)
        {
            if (asset == null || !s_Groups.TryGetValue(asset, out var group))
                return;

            group.shaped = true;
            group.dirty = true;
            ApplyHidden(group);
        }

        static Group Acquire(HBStylePlantAsset asset, int layer)
        {
            if (s_Groups.TryGetValue(asset, out var group))
                return group;

            group = new Group { asset = asset, layer = layer };
            s_Groups.Add(asset, group);
            s_Order.Add(group);

            return group;
        }

        static void Drop(Group group)
        {
            if (group.members.Count > 0)
                return;

            Release(group);
            s_Groups.Remove(group.asset);
            s_Order.Remove(group);
        }

        static void ApplyHidden(Group group)
        {
            var hide = ready && group.asset != null && group.asset.valid;

            foreach (var member in group.members)
                member.Hide(hide && !member.editing);
        }

        static void Hook()
        {
            if (s_Hooked)
                return;

            s_Hooked = true;
            RenderPipelineManager.beginCameraRendering += Render;
            LightProbes.tetrahedralizationCompleted += Relight;
            Application.quitting += ReleaseAll;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseAll;
            UnityEditor.Selection.selectionChanged += Reselect;
#endif
        }

#if UNITY_EDITOR
        static void Reselect()
        {
            foreach (var group in s_Order)
            {
                var hide = ready && group.asset != null && group.asset.valid;

                foreach (var member in group.members)
                {
                    var editing = Selected(member);

                    if (member.editing == editing)
                        continue;

                    member.editing = editing;
                    member.Hide(hide && !editing);
                    group.dirty = true;
                }
            }
        }
#endif

        static bool Selected(HBStylePlant member)
        {
#if UNITY_EDITOR
            foreach (var selected in UnityEditor.Selection.gameObjects)
            {
                if (selected.scene.IsValid() && (selected.transform.IsChildOf(member.transform) ||
                                                 member.transform.IsChildOf(selected.transform)))
                    return true;
            }
#endif
            return false;
        }

        static void Relight()
        {
            foreach (var group in s_Order)
                group.dirty = true;
        }

        static void Render(ScriptableRenderContext context, Camera camera)
        {
            if (!ready || s_Order.Count == 0)
                return;

            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;

#if UNITY_EDITOR
            if (camera.scene.IsValid() && UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(camera.scene))
                return;
#endif

            s_Commands ??= new CommandBuffer { name = "HB Style Plants" };

            GeometryUtility.CalculateFrustumPlanes(StyleDebug.Viewer(camera), s_Planes);

            for (var side = 0; side < s_Planes.Length; side++)
                s_PlaneVectors[side] = new Vector4(s_Planes[side].normal.x, s_Planes[side].normal.y,
                                                   s_Planes[side].normal.z, s_Planes[side].distance);

            var viewer = StyleDebug.Viewer(camera);
            var eye = viewer.transform.position;
            var sun = Sun();
            var pipeline = UniversalRenderPipeline.asset;
            var shadowDistance = pipeline != null && pipeline.supportsMainLightShadows ? pipeline.shadowDistance : 0f;
            var travel = sun != null ? sun.transform.forward : Vector3.down;

            s_Commands.SetComputeVectorArrayParam(s_Compute, s_PlanesId, s_PlaneVectors);
            s_Commands.SetComputeVectorParam(s_Compute, s_ViewId, new Vector4(Zoom(viewer),
                                                                             StyleShelterPass.Extent * 0.5f, 0f, 0f));
            s_Commands.SetComputeVectorParam(s_Compute, s_SunId, new Vector4(travel.x, travel.y, travel.z,
                                                                             sun != null ? shadowDistance : 0f));
            s_Commands.SetComputeVectorParam(s_Compute, s_AirId, new Vector4(
                StyleEnvironment.weather.z, StyleEnvironment.windHeading * Mathf.Deg2Rad,
                Shader.GetGlobalVector(s_TimeId).y, 0f));

            var drawn = false;

            foreach (var group in s_Order)
            {
                if (group.asset == null || !group.asset.valid || group.members.Count == 0)
                    continue;

                Refresh(group);

                if (!group.drawable)
                    continue;

                Cull(group, eye);

                if (camera.cameraType == CameraType.Game || !Application.isPlaying)
                    Read(group);

                drawn = true;
            }

            if (drawn)
                context.ExecuteCommandBuffer(s_Commands);

            s_Commands.Clear();

            if (!drawn)
                return;

            foreach (var group in s_Order)
            {
                if (group.drawable)
                    Draw(group, camera);
            }
        }

        static float Zoom(Camera camera)
        {
            if (camera.orthographic)
                return 1f;

            return Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) /
                   Mathf.Tan(ReferenceView * 0.5f * Mathf.Deg2Rad);
        }

        static Light Sun()
        {
            var sun = RenderSettings.sun;

            if (sun == null || !sun.isActiveAndEnabled || sun.type != LightType.Directional ||
                sun.shadows == LightShadows.None)
                return null;

            return sun;
        }

        static void Refresh(Group group)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var member in group.members)
                {
                    if (member != null && member.transform.hasChanged)
                    {
                        member.transform.hasChanged = false;
                        group.dirty = true;
                    }
                }
            }
#endif

            if (group.shaped)
                Shape(group);

            if (group.dirty)
                Upload(group);
        }

        static void Shape(Group group)
        {
            group.shaped = false;

            var asset = group.asset;
            var draws = asset.draws;
            var count = draws.Length + 1;
            var arguments = new uint[count * ArgumentStride];

            group.drawCount = 0;

            for (var index = 0; index < draws.Length; index++)
            {
                var draw = draws[index];

                if (Usable(draw))
                    Arguments(arguments, index, draw.mesh, draw.submesh);
            }

            if (asset.billboarded)
            {
                Arguments(arguments, draws.Length, asset.billboardMesh, 0);
                System.Array.Copy(arguments, draws.Length * ArgumentStride, group.farData, 0, ArgumentStride);
            }

            group.arguments?.Release();
            group.skyArguments?.Release();
            group.farArguments?.Release();
            group.farArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, ArgumentStride,
                                                    sizeof(uint));
            group.arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, count * ArgumentStride,
                                                 sizeof(uint));
            group.skyArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, count * ArgumentStride,
                                                    sizeof(uint));
            group.arguments.SetData(arguments);
            group.skyArguments.SetData(arguments);

            if (group.blocks == null || group.blocks.Length != count)
            {
                group.blocks = NewBlocks(count);
                group.skyBlocks = NewBlocks(count);
                group.shelterPasses = new int[count];
            }

            for (var index = 0; index < count; index++)
            {
                var billboard = index == draws.Length;
                var mesh = billboard ? asset.billboardMesh : draws[index].mesh;
                var local = billboard ? Matrix4x4.identity : draws[index].local;
                var bounds = mesh != null ? mesh.bounds : new Bounds();

                Describe(group.blocks[index], local, bounds);
                group.blocks[index].SetFloat(s_ReceiveWeatherId, MovingWeather);
                Describe(group.skyBlocks[index], local, bounds);

                if (billboard ? asset.billboarded : Usable(draws[index]))
                    group.shelterPasses[index] = (billboard ? asset.billboardMaterial : draws[index].material)
                        .FindPass(ShelterPass);
            }

            group.meshTriangles = 0;

            for (var index = 0; index < draws.Length; index++)
                group.meshTriangles += arguments[index * ArgumentStride] / 3;

            group.billboardTriangles = arguments[draws.Length * ArgumentStride] / 3;
            group.drawCount = count;
        }

        static bool Usable(HBStylePlantDraw draw)
        {
            return draw.mesh != null && draw.material != null && draw.submesh < draw.mesh.subMeshCount;
        }

        static MaterialPropertyBlock[] NewBlocks(int count)
        {
            var blocks = new MaterialPropertyBlock[count];

            for (var index = 0; index < count; index++)
                blocks[index] = new MaterialPropertyBlock();

            return blocks;
        }

        static void Describe(MaterialPropertyBlock block, Matrix4x4 local, Bounds bounds)
        {
            block.SetMatrix(s_LocalId, local);
            block.SetMatrix(s_LocalInverseId, local.inverse);
            block.SetVector(s_BoundsCenterId, bounds.center);
            block.SetVector(s_BoundsExtentId, bounds.extents);
        }

        static void Arguments(uint[] arguments, int index, Mesh mesh, int submesh)
        {
            var offset = index * ArgumentStride;

            arguments[offset] = mesh.GetIndexCount(submesh);
            arguments[offset + 1] = 0u;
            arguments[offset + 2] = mesh.GetIndexStart(submesh);
            arguments[offset + 3] = mesh.GetBaseVertex(submesh);
            arguments[offset + 4] = 0u;
        }

        static void Upload(Group group)
        {
            group.dirty = false;

            var sphere = group.asset.sphere;

            Reserve(group, group.members.Count);

            var least = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var most = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var count = 0;

            foreach (var member in group.members)
            {
                if (member.editing)
                    continue;

                var transform = member.transform;

                Place(group, count++, transform.localToWorldMatrix, transform.worldToLocalMatrix, sphere, ref least,
                      ref most);
            }

            group.count = count;

            if (count == 0)
                return;

            if (group.harmonics.Length != group.positions.Length)
            {
                group.harmonics = new SphericalHarmonicsL2[group.positions.Length];
                group.occlusion = new Vector4[group.positions.Length];
            }

            LightProbes.CalculateInterpolatedLightAndOcclusionProbes(group.positions, group.harmonics,
                                                                     group.occlusion);

            for (var slot = 0; slot < count; slot++)
            {
                group.probes[slot] = Pack(group.harmonics[slot], group.occlusion[slot]);
                group.entries[slot] = new VisibleData { index = (uint)slot, fade = 1f };
            }

            group.farData[1] = (uint)count;

            group.instanceBuffer.SetData(group.instances, 0, 0, count);
            group.probeBuffer.SetData(group.probes, 0, 0, count);
            group.everyone.SetData(group.entries, 0, 0, count);
            group.farArguments.SetData(group.farData);
            group.bounds = new Bounds((least + most) * 0.5f, most - least + Vector3.one * BoundsMargin);
        }

        static void Place(Group group, int index, Matrix4x4 world, Matrix4x4 inverse, Vector4 sphere,
                          ref Vector3 least, ref Vector3 most)
        {
            var scale = world.lossyScale;
            var centre = world.MultiplyPoint3x4(sphere);
            var radius = sphere.w * Mathf.Max(Mathf.Abs(scale.x),
                                              Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

            group.instances[index] = new InstanceData
            {
                row0 = world.GetRow(0),
                row1 = world.GetRow(1),
                row2 = world.GetRow(2),
                inverse0 = inverse.GetRow(0),
                inverse1 = inverse.GetRow(1),
                inverse2 = inverse.GetRow(2),
                sphere = new Vector4(centre.x, centre.y, centre.z, radius)
            };

            group.positions[index] = centre;
            least = Vector3.Min(least, centre - Vector3.one * radius);
            most = Vector3.Max(most, centre + Vector3.one * radius);
        }

        static ProbeData Pack(SphericalHarmonicsL2 sh, Vector4 occlusion)
        {
            return new ProbeData
            {
                shAr = new Vector4(sh[0, 3], sh[0, 1], sh[0, 2], sh[0, 0] - sh[0, 6]),
                shAg = new Vector4(sh[1, 3], sh[1, 1], sh[1, 2], sh[1, 0] - sh[1, 6]),
                shAb = new Vector4(sh[2, 3], sh[2, 1], sh[2, 2], sh[2, 0] - sh[2, 6]),
                shBr = new Vector4(sh[0, 4], sh[0, 5], sh[0, 6] * 3f, sh[0, 7]),
                shBg = new Vector4(sh[1, 4], sh[1, 5], sh[1, 6] * 3f, sh[1, 7]),
                shBb = new Vector4(sh[2, 4], sh[2, 5], sh[2, 6] * 3f, sh[2, 7]),
                shC = new Vector4(sh[0, 8], sh[1, 8], sh[2, 8], 1f),
                occlusion = occlusion
            };
        }

        static void Reserve(Group group, int count)
        {
            if (group.instanceBuffer != null && count <= group.capacity)
                return;

            var capacity = Mathf.Max(Mathf.NextPowerOfTwo(count), MinCapacity);

            group.capacity = capacity;
            group.instances = new InstanceData[capacity];
            group.probes = new ProbeData[capacity];
            group.positions = new Vector3[capacity];
            group.entries = new VisibleData[capacity];

            group.instanceBuffer?.Release();
            group.probeBuffer?.Release();
            group.near?.Release();
            group.far?.Release();
            group.sky?.Release();
            group.everyone?.Release();

            group.everyone = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, VisibleStride);
            group.instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity,
                                                      Marshal.SizeOf<InstanceData>());
            group.probeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity,
                                                   Marshal.SizeOf<ProbeData>());
            group.near = new GraphicsBuffer(GraphicsBuffer.Target.Append, capacity, VisibleStride);
            group.far = new GraphicsBuffer(GraphicsBuffer.Target.Append, capacity, VisibleStride);
            group.sky = new GraphicsBuffer(GraphicsBuffer.Target.Append, capacity, VisibleStride);
        }

        static void Cull(Group group, Vector3 eye)
        {
            var asset = group.asset;
            var near = asset.switchDistance;
            var far = asset.cullDistance;
            var groups = (group.count + ThreadGroup - 1) / ThreadGroup;

            s_Commands.SetBufferCounterValue(group.near, 0);
            s_Commands.SetBufferCounterValue(group.far, 0);

            s_Commands.SetComputeVectorParam(s_Compute, s_EyeId, new Vector4(eye.x, eye.y, eye.z, group.count));
            s_Commands.SetComputeVectorParam(s_Compute, s_RangesId, new Vector4(
                near, far, 1f / Mathf.Max(near * FadeShare, MinFade), 1f / Mathf.Max(far * FadeShare, MinFade)));

            s_Commands.SetComputeBufferParam(s_Compute, s_CullKernel, s_InstancesId, group.instanceBuffer);
            s_Commands.SetComputeBufferParam(s_Compute, s_CullKernel, s_NearId, group.near);
            s_Commands.SetComputeBufferParam(s_Compute, s_CullKernel, s_FarId, group.far);
            s_Commands.DispatchCompute(s_Compute, s_CullKernel, groups, 1, 1);

            var sheltered = StyleShelterPass.Needed();

            if (sheltered)
            {
                s_Commands.SetBufferCounterValue(group.sky, 0);
                s_Commands.SetComputeBufferParam(s_Compute, s_SkyKernel, s_InstancesId, group.instanceBuffer);
                s_Commands.SetComputeBufferParam(s_Compute, s_SkyKernel, s_SkyId, group.sky);
                s_Commands.DispatchCompute(s_Compute, s_SkyKernel, groups, 1, 1);
            }

            var billboard = group.drawCount - 1;

            for (var index = 0; index < billboard; index++)
            {
                var offset = (uint)((index * ArgumentStride + 1) * sizeof(uint));

                s_Commands.CopyCounterValue(group.near, group.arguments, offset);

                if (sheltered)
                    s_Commands.CopyCounterValue(group.sky, group.skyArguments, offset);
            }

            s_Commands.CopyCounterValue(group.far, group.arguments,
                                        (uint)((billboard * ArgumentStride + 1) * sizeof(uint)));
        }

        internal static Counts Watch(HBStylePlantAsset asset)
        {
            s_Watched = asset;
            s_WatchUntil = Time.realtimeSinceStartupAsDouble + 1.0;

            if (asset == null || !s_Groups.TryGetValue(asset, out var group))
                return default;

            return group.count > 0 ? group.counts : new Counts { valid = true };
        }

        static void Read(Group group)
        {
            if (group.reading || group.asset != s_Watched || Time.realtimeSinceStartupAsDouble > s_WatchUntil ||
                !SystemInfo.supportsAsyncGPUReadback)
                return;

            var total = group.count;
            var billboard = group.drawCount - 1;

            group.reading = true;
            s_Commands.RequestAsyncReadback(group.arguments, request =>
            {
                group.reading = false;

                if (request.hasError)
                    return;

                var data = request.GetData<uint>();
                var meshes = (int)data[1];
                var billboards = group.asset != null && group.asset.billboarded
                    ? (int)data[billboard * ArgumentStride + 1]
                    : 0;

                group.counts = new Counts
                {
                    total = total,
                    meshes = meshes,
                    billboards = billboards,
                    triangles = meshes * group.meshTriangles + billboards * group.billboardTriangles,
                    valid = true
                };
            });
        }

        static void Draw(Group group, Camera camera)
        {
            var asset = group.asset;
            var draws = asset.draws;

            for (var index = 0; index < draws.Length; index++)
            {
                var draw = draws[index];

                if (Usable(draw))
                    Draw(group, camera, draw.mesh, draw.material, draw.shadows, index, group.near);
            }

            if (asset.billboarded)
                Draw(group, camera, asset.billboardMesh, asset.billboardMaterial, ShadowCastingMode.On, draws.Length,
                     group.far);
        }

        static void Draw(Group group, Camera camera, Mesh mesh, Material material, ShadowCastingMode shadows, int index,
                         GraphicsBuffer visible)
        {
            var block = group.blocks[index];

            block.SetBuffer(s_InstancesId, group.instanceBuffer);
            block.SetBuffer(s_ProbesId, group.probeBuffer);
            block.SetBuffer(s_VisibleId, visible);

            var parameters = new RenderParams(material)
            {
                camera = camera,
                layer = group.layer,
                worldBounds = group.bounds,
                matProps = block,
                receiveShadows = true,
                shadowCastingMode = shadows,
                lightProbeUsage = LightProbeUsage.BlendProbes,
                motionVectorMode = MotionVectorGenerationMode.Camera
            };

            Graphics.RenderMeshIndirect(parameters, mesh, group.arguments, 1, index);
        }

        internal static void DrawShelter(RasterCommandBuffer cmd, int cullingMask, Material underside)
        {
            foreach (var group in s_Order)
            {
                if (!group.drawable || (cullingMask & (1 << group.layer)) == 0)
                    continue;

                var draws = group.asset.draws;

                for (var index = 0; index < draws.Length; index++)
                {
                    var draw = draws[index];

                    if (!Usable(draw))
                        continue;

                    var block = group.skyBlocks[index];

                    block.SetBuffer(s_InstancesId, group.instanceBuffer);
                    block.SetBuffer(s_ProbesId, group.probeBuffer);
                    block.SetBuffer(s_VisibleId, group.sky);

                    var material = underside != null ? underside : draw.material;
                    var pass = underside != null ? UndersidePlantPass : group.shelterPasses[index];

                    cmd.DrawMeshInstancedIndirect(draw.mesh, draw.submesh, material, pass, group.skyArguments,
                                                  index * ArgumentStride * sizeof(uint), block);
                }
            }
        }

        internal static void DrawFarShelter(RasterCommandBuffer cmd, int cullingMask)
        {
            foreach (var group in s_Order)
            {
                if (!group.drawable || !group.asset.billboarded || (cullingMask & (1 << group.layer)) == 0)
                    continue;

                var index = group.asset.draws.Length;
                var block = group.skyBlocks[index];

                block.SetBuffer(s_InstancesId, group.instanceBuffer);
                block.SetBuffer(s_ProbesId, group.probeBuffer);
                block.SetBuffer(s_VisibleId, group.everyone);

                cmd.DrawMeshInstancedIndirect(group.asset.billboardMesh, 0, group.asset.billboardMaterial,
                                              group.shelterPasses[index], group.farArguments, 0, block);
            }
        }

        static void Release(Group group)
        {
            group.instanceBuffer?.Release();
            group.probeBuffer?.Release();
            group.near?.Release();
            group.far?.Release();
            group.sky?.Release();
            group.everyone?.Release();
            group.arguments?.Release();
            group.skyArguments?.Release();
            group.farArguments?.Release();

            group.instanceBuffer = null;
            group.probeBuffer = null;
            group.near = null;
            group.far = null;
            group.sky = null;
            group.everyone = null;
            group.arguments = null;
            group.skyArguments = null;
            group.farArguments = null;
            group.capacity = 0;
            group.shaped = true;
            group.dirty = true;
        }

        static void ReleaseAll()
        {
            foreach (var group in s_Order)
                Release(group);

            s_Commands?.Release();
            s_Commands = null;
        }
    }
}
