using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    static class StyleBillboardSystem
    {
        public const string ComputePath =
            "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Billboard/StyleBillboard.compute";

        const int ThreadGroup = 64;
        const int ArgumentStride = 5;
        const int MinCapacity = 64;
        const float FadeShare = 0.1f;
        const float MinFade = 0.5f;
        const float BoundsMargin = 1f;
        const float ReferenceView = 60f;

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

        sealed class Group
        {
            public HBStyleBillboardAsset asset;
            public readonly List<HBStyleBillboard> members = new List<HBStyleBillboard>();
            public InstanceData[] instances = new InstanceData[MinCapacity];
            public ProbeData[] probes = new ProbeData[MinCapacity];
            public Vector3[] positions = new Vector3[MinCapacity];
            public SphericalHarmonicsL2[] harmonics = new SphericalHarmonicsL2[MinCapacity];
            public Vector4[] occlusion = new Vector4[MinCapacity];
            public GraphicsBuffer instanceBuffer;
            public GraphicsBuffer probeBuffer;
            public GraphicsBuffer near;
            public GraphicsBuffer far;
            public GraphicsBuffer arguments;
            public MaterialPropertyBlock[] blocks;
            public int capacity;
            public int drawCount;
            public bool dirty = true;
            public bool shaped = true;
            public bool reading;
            public long meshTriangles;
            public long billboardTriangles;
            public Counts counts;
            public Bounds bounds;
            public int layer;
            public int treeCount;
            public readonly Dictionary<Terrain, List<Matrix4x4>> trees = new Dictionary<Terrain, List<Matrix4x4>>();

            public int total => members.Count + treeCount;
        }

        static readonly int s_InstancesId = Shader.PropertyToID("_HB_BillboardInstances");
        static readonly int s_ProbesId = Shader.PropertyToID("_HB_BillboardProbes");
        static readonly int s_VisibleId = Shader.PropertyToID("_HB_BillboardVisible");
        static readonly int s_NearId = Shader.PropertyToID("_HB_BillboardNear");
        static readonly int s_FarId = Shader.PropertyToID("_HB_BillboardFar");
        static readonly int s_LocalId = Shader.PropertyToID("_HB_BillboardLocal");
        static readonly int s_LocalInverseId = Shader.PropertyToID("_HB_BillboardLocalInverse");
        static readonly int s_BoundsCenterId = Shader.PropertyToID("_HB_BillboardBoundsCenter");
        static readonly int s_BoundsExtentId = Shader.PropertyToID("_HB_BillboardBoundsExtent");
        static readonly int s_PlanesId = Shader.PropertyToID("_HB_BillboardPlanes");
        static readonly int s_EyeId = Shader.PropertyToID("_HB_BillboardEye");
        static readonly int s_RangesId = Shader.PropertyToID("_HB_BillboardRanges");
        static readonly int s_SunId = Shader.PropertyToID("_HB_BillboardSun");
        static readonly int s_ViewId = Shader.PropertyToID("_HB_BillboardView");

        static readonly Dictionary<HBStyleBillboardAsset, Group> s_Groups =
            new Dictionary<HBStyleBillboardAsset, Group>();

        static readonly List<Group> s_Order = new List<Group>();
        static readonly HashSet<Terrain> s_Terrains = new HashSet<Terrain>();
        static readonly List<Terrain> s_HiddenTerrains = new List<Terrain>();
        static readonly List<float> s_HiddenDistances = new List<float>();
        static readonly Plane[] s_Planes = new Plane[6];
        static readonly Vector4[] s_PlaneVectors = new Vector4[6];

        static ComputeShader s_Compute;
        static CommandBuffer s_Commands;
        static int s_Kernel = -1;
        static bool s_Hooked;

        internal struct Counts
        {
            public int total;
            public int meshes;
            public int billboards;
            public long triangles;
            public bool valid;
        }

        static HBStyleBillboardAsset s_Watched;
        static double s_WatchUntil;

        static bool ready => s_Compute != null && s_Kernel >= 0 && SystemInfo.supportsComputeShaders;

        internal static IEnumerable<HBStyleBillboard> members
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

        internal static bool drawing => ready;

        public static void Setup(ComputeShader compute)
        {
            if (compute == s_Compute)
                return;

            s_Compute = compute;
            s_Kernel = compute != null && compute.HasKernel("Cull") ? compute.FindKernel("Cull") : -1;

            Hook();

            foreach (var group in s_Order)
                ApplyHidden(group);
        }

        public static void Add(HBStyleBillboard member)
        {
            Remove(member);

            var asset = member.asset;

            if (asset == null)
                return;

            Hook();

            var group = Acquire(asset, member.gameObject.layer);

            member.registered = asset;
            member.slot = group.members.Count;
            group.members.Add(member);
            group.dirty = true;

            member.Hide(ready && asset.valid);
        }

        public static void Remove(HBStyleBillboard member)
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

        internal static void SetTerrain(Terrain terrain)
        {
            ClearTerrain(terrain);

            var data = terrain != null ? terrain.terrainData : null;

            if (data == null)
                return;

            var prototypes = data.treePrototypes;
            var assets = new HBStyleBillboardAsset[prototypes.Length];
            var scales = new Vector3[prototypes.Length];
            var baked = false;

            for (var index = 0; index < prototypes.Length; index++)
            {
                var prefab = prototypes[index].prefab;

                if (prefab == null || !prefab.TryGetComponent<HBStyleBillboard>(out var billboard) ||
                    billboard.asset == null)
                    continue;

                assets[index] = billboard.asset;
                scales[index] = prefab.transform.localScale;
                baked = true;
            }

            if (!baked)
                return;

            Hook();
            s_Terrains.Add(terrain);

            var origin = terrain.GetPosition();
            var size = data.size;
            var layer = terrain.gameObject.layer;

            foreach (var tree in data.treeInstances)
            {
                var index = tree.prototypeIndex;

                if (index < 0 || index >= assets.Length || assets[index] == null)
                    continue;

                var group = Acquire(assets[index], layer);

                if (!group.trees.TryGetValue(terrain, out var list))
                {
                    list = new List<Matrix4x4>();
                    group.trees.Add(terrain, list);
                }

                var scale = Vector3.Scale(scales[index],
                                          new Vector3(tree.widthScale, tree.heightScale, tree.widthScale));

                list.Add(Matrix4x4.TRS(origin + Vector3.Scale(tree.position, size),
                                       Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f), scale));
                group.treeCount++;
                group.dirty = true;
            }
        }

        internal static void ClearTerrain(Terrain terrain)
        {
            s_Terrains.Remove(terrain);

            for (var index = s_Order.Count - 1; index >= 0; index--)
            {
                var group = s_Order[index];

                if (!group.trees.TryGetValue(terrain, out var list))
                    continue;

                group.treeCount -= list.Count;
                group.trees.Remove(terrain);
                group.dirty = true;
                Drop(group);
            }
        }

        internal static bool Owns(Terrain terrain) => s_Terrains.Contains(terrain);

        static Group Acquire(HBStyleBillboardAsset asset, int layer)
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
            if (group.total > 0)
                return;

            Release(group);
            s_Groups.Remove(group.asset);
            s_Order.Remove(group);
        }

        static void HideTrees()
        {
            foreach (var terrain in s_Terrains)
            {
                if (terrain == null || terrain.treeDistance <= 0f)
                    continue;

                s_HiddenTerrains.Add(terrain);
                s_HiddenDistances.Add(terrain.treeDistance);
                terrain.treeDistance = 0f;
            }
        }

        static void ShowTrees(ScriptableRenderContext context, Camera camera)
        {
            for (var index = 0; index < s_HiddenTerrains.Count; index++)
            {
                if (s_HiddenTerrains[index] != null)
                    s_HiddenTerrains[index].treeDistance = s_HiddenDistances[index];
            }

            s_HiddenTerrains.Clear();
            s_HiddenDistances.Clear();
        }

        public static void Rebuild(HBStyleBillboardAsset asset)
        {
            if (asset == null || !s_Groups.TryGetValue(asset, out var group))
                return;

            group.shaped = true;
            group.dirty = true;
            ApplyHidden(group);
        }

        static void ApplyHidden(Group group)
        {
            var hide = ready && group.asset != null && group.asset.valid;

            foreach (var member in group.members)
            {
                member.Hide(false);
                member.Hide(hide);
            }
        }

        static void Hook()
        {
            if (s_Hooked)
                return;

            s_Hooked = true;
            RenderPipelineManager.beginCameraRendering += Render;
            RenderPipelineManager.endCameraRendering += ShowTrees;
            LightProbes.tetrahedralizationCompleted += Relight;
            Application.quitting += ReleaseAll;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseAll;
#endif
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

            HideTrees();

            s_Commands ??= new CommandBuffer { name = "HiddenBull Style Billboards" };

            GeometryUtility.CalculateFrustumPlanes(camera, s_Planes);

            for (var side = 0; side < s_Planes.Length; side++)
                s_PlaneVectors[side] = new Vector4(s_Planes[side].normal.x, s_Planes[side].normal.y,
                                                   s_Planes[side].normal.z, s_Planes[side].distance);

            var eye = camera.transform.position;
            var sun = Sun();
            var asset = UniversalRenderPipeline.asset;
            var shadowDistance = asset != null && asset.supportsMainLightShadows ? asset.shadowDistance : 0f;
            var travel = sun != null ? sun.transform.forward : Vector3.down;

            s_Commands.SetComputeVectorArrayParam(s_Compute, s_PlanesId, s_PlaneVectors);
            s_Commands.SetComputeVectorParam(s_Compute, s_ViewId, new Vector4(Zoom(camera), 0f, 0f, 0f));
            s_Commands.SetComputeVectorParam(s_Compute, s_SunId, new Vector4(travel.x, travel.y, travel.z,
                                                                             sun != null ? shadowDistance : 0f));

            var drawn = false;

            foreach (var group in s_Order)
            {
                if (group.asset == null || !group.asset.valid || group.total == 0)
                    continue;

                Refresh(group);

                if (group.drawCount == 0)
                    continue;

                Cull(group, eye);

                if (camera.cameraType == CameraType.Game)
                    Read(group);
                drawn = true;
            }

            if (!drawn)
                return;

            context.ExecuteCommandBuffer(s_Commands);
            s_Commands.Clear();

            foreach (var group in s_Order)
            {
                if (group.drawCount > 0 && group.asset != null && group.asset.valid && group.total > 0)
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

            if (sun == null && HBStyleSun.active != null)
                sun = HBStyleSun.active.GetComponent<Light>();

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

                if (draw.mesh == null || draw.material == null || draw.submesh >= draw.mesh.subMeshCount)
                    continue;

                Arguments(arguments, index, draw.mesh, draw.submesh);
            }

            Arguments(arguments, draws.Length, asset.billboardMesh, 0);

            group.arguments?.Release();
            group.arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, count * ArgumentStride,
                                                 sizeof(uint));
            group.arguments.SetData(arguments);

            if (group.blocks == null || group.blocks.Length != count)
            {
                group.blocks = new MaterialPropertyBlock[count];

                for (var index = 0; index < count; index++)
                    group.blocks[index] = new MaterialPropertyBlock();
            }

            for (var index = 0; index < count; index++)
            {
                var billboard = index == draws.Length;
                var local = billboard ? Matrix4x4.identity : draws[index].local;
                var mesh = billboard ? asset.billboardMesh : draws[index].mesh;
                var bounds = mesh != null ? mesh.bounds : new Bounds();
                var block = group.blocks[index];

                block.SetMatrix(s_LocalId, local);
                block.SetMatrix(s_LocalInverseId, local.inverse);
                block.SetVector(s_BoundsCenterId, bounds.center);
                block.SetVector(s_BoundsExtentId, bounds.extents);
            }

            group.meshTriangles = 0;

            for (var index = 0; index < draws.Length; index++)
                group.meshTriangles += arguments[index * ArgumentStride] / 3;

            group.billboardTriangles = arguments[draws.Length * ArgumentStride] / 3;
            group.drawCount = count;
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

            var count = group.total;
            var sphere = group.asset.sphere;

            Reserve(group, count);

            var least = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var most = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var index = 0;

            foreach (var member in group.members)
            {
                var transform = member.transform;

                Place(group, index++, transform.localToWorldMatrix, transform.worldToLocalMatrix, sphere,
                      ref least, ref most);
            }

            foreach (var trees in group.trees.Values)
            {
                foreach (var world in trees)
                    Place(group, index++, world, world.inverse, sphere, ref least, ref most);
            }

            if (group.harmonics.Length != group.positions.Length)
            {
                group.harmonics = new SphericalHarmonicsL2[group.positions.Length];
                group.occlusion = new Vector4[group.positions.Length];
            }

            LightProbes.CalculateInterpolatedLightAndOcclusionProbes(group.positions, group.harmonics,
                                                                     group.occlusion);

            for (var slot = 0; slot < count; slot++)
                group.probes[slot] = Pack(group.harmonics[slot], group.occlusion[slot]);

            group.instanceBuffer.SetData(group.instances, 0, 0, count);
            group.probeBuffer.SetData(group.probes, 0, 0, count);
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

            group.instanceBuffer?.Release();
            group.probeBuffer?.Release();
            group.near?.Release();
            group.far?.Release();

            group.instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity,
                                                      Marshal.SizeOf<InstanceData>());
            group.probeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity,
                                                   Marshal.SizeOf<ProbeData>());
            group.near = new GraphicsBuffer(GraphicsBuffer.Target.Append, capacity, sizeof(uint) * 2);
            group.far = new GraphicsBuffer(GraphicsBuffer.Target.Append, capacity, sizeof(uint) * 2);
        }

        static void Cull(Group group, Vector3 eye)
        {
            var asset = group.asset;
            var near = asset.switchDistance;
            var far = asset.cullDistance;
            var count = group.total;

            s_Commands.SetBufferCounterValue(group.near, 0);
            s_Commands.SetBufferCounterValue(group.far, 0);
            s_Commands.SetComputeBufferParam(s_Compute, s_Kernel, s_InstancesId, group.instanceBuffer);
            s_Commands.SetComputeBufferParam(s_Compute, s_Kernel, s_NearId, group.near);
            s_Commands.SetComputeBufferParam(s_Compute, s_Kernel, s_FarId, group.far);
            s_Commands.SetComputeVectorParam(s_Compute, s_EyeId, new Vector4(eye.x, eye.y, eye.z, count));
            s_Commands.SetComputeVectorParam(s_Compute, s_RangesId, new Vector4(
                near, far, 1f / Mathf.Max(near * FadeShare, MinFade), 1f / Mathf.Max(far * FadeShare, MinFade)));
            s_Commands.DispatchCompute(s_Compute, s_Kernel, (count + ThreadGroup - 1) / ThreadGroup, 1, 1);

            var billboard = group.drawCount - 1;

            for (var index = 0; index < billboard; index++)
                s_Commands.CopyCounterValue(group.near, group.arguments,
                                            (uint)((index * ArgumentStride + 1) * sizeof(uint)));

            s_Commands.CopyCounterValue(group.far, group.arguments,
                                        (uint)((billboard * ArgumentStride + 1) * sizeof(uint)));
        }

        internal static Counts Watch(HBStyleBillboardAsset asset)
        {
            s_Watched = asset;
            s_WatchUntil = Time.realtimeSinceStartupAsDouble + 1.0;

            return asset != null && s_Groups.TryGetValue(asset, out var group) ? group.counts : default;
        }

        static void Read(Group group)
        {
            if (group.reading || group.asset != s_Watched || Time.realtimeSinceStartupAsDouble > s_WatchUntil ||
                !SystemInfo.supportsAsyncGPUReadback)
                return;

            var total = group.total;
            var billboard = group.drawCount - 1;

            group.reading = true;
            s_Commands.RequestAsyncReadback(group.arguments, request =>
            {
                group.reading = false;

                if (request.hasError)
                    return;

                var data = request.GetData<uint>();
                var meshes = (int)data[1];
                var billboards = (int)data[billboard * ArgumentStride + 1];

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
            var billboard = group.drawCount - 1;

            for (var index = 0; index < group.drawCount; index++)
            {
                var isBillboard = index == billboard;
                var mesh = isBillboard ? asset.billboardMesh : draws[index].mesh;
                var material = isBillboard ? asset.billboardMaterial : draws[index].material;

                if (mesh == null || material == null)
                    continue;

                if (!isBillboard && draws[index].submesh >= mesh.subMeshCount)
                    continue;

                var block = group.blocks[index];

                block.SetBuffer(s_InstancesId, group.instanceBuffer);
                block.SetBuffer(s_ProbesId, group.probeBuffer);
                block.SetBuffer(s_VisibleId, isBillboard ? group.far : group.near);

                var parameters = new RenderParams(material)
                {
                    camera = camera,
                    layer = group.layer,
                    worldBounds = group.bounds,
                    matProps = block,
                    receiveShadows = true,
                    shadowCastingMode = isBillboard ? ShadowCastingMode.On : draws[index].shadows,
                    lightProbeUsage = LightProbeUsage.BlendProbes,
                    motionVectorMode = MotionVectorGenerationMode.Camera
                };

                Graphics.RenderMeshIndirect(parameters, mesh, group.arguments, 1, index);
            }
        }

        static void Release(Group group)
        {
            group.instanceBuffer?.Release();
            group.probeBuffer?.Release();
            group.near?.Release();
            group.far?.Release();
            group.arguments?.Release();

            group.instanceBuffer = null;
            group.probeBuffer = null;
            group.near = null;
            group.far = null;
            group.arguments = null;
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
