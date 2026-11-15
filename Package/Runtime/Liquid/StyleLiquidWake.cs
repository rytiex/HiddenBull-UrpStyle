using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    static class StyleLiquidWake
    {
        const int MapSize = 256;
        const float Reach = 32f;
        const float MostStep = 0.05f;
        const double SameFrame = 0.001;
        const double Linger = 35.0;
        const double PruneSeconds = 10.0;

        static readonly int s_StateId = Shader.PropertyToID("_HB_WakeState");
        static readonly int s_StateOutId = Shader.PropertyToID("_HB_WakeStateOut");
        static readonly int s_LookOutId = Shader.PropertyToID("_HB_WakeLookOut");
        static readonly int s_StampsId = Shader.PropertyToID("_HB_WakeStamps");
        static readonly int s_FrameId = Shader.PropertyToID("_HB_WakeFrame");
        static readonly int s_FromId = Shader.PropertyToID("_HB_WakeFrom");
        static readonly int s_MapId = Shader.PropertyToID("_HB_LiquidWake");
        static readonly int s_MapStateId = Shader.PropertyToID("_HB_LiquidWakeState");

        sealed class View
        {
            internal RenderTexture state;
            internal RenderTexture next;
            internal RenderTexture look;
            internal Vector4 from;
            internal double stamp;
            internal double until;
            internal double lastUsed;
            internal bool ready;

            internal void Release()
            {
                Free(state);
                Free(next);
                Free(look);
            }
        }

        static readonly Dictionary<Camera, View> s_Views = new Dictionary<Camera, View>();
        static readonly List<Camera> s_Stale = new List<Camera>();
        static readonly Vector4[] s_Stamps = new Vector4[HBStyleLiquidTrail.Limit * HBStyleLiquidTrail.StampSize];

        static GraphicsBuffer s_StampBuffer;
        static CommandBuffer s_Commands;

        static void Free(RenderTexture map)
        {
            if (map == null)
                return;

            map.Release();
            CoreUtils.Destroy(map);
        }

        static RenderTexture NewMap(FilterMode filter)
        {
            var map = new RenderTexture(MapSize, MapSize, 0, RenderTextureFormat.ARGBHalf,
                                        RenderTextureReadWrite.Linear)
            {
                name = "HB Style Liquid Wake",
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = filter,
                hideFlags = HideFlags.HideAndDontSave
            };

            map.Create();
            return map;
        }

        internal static void Run(ScriptableRenderContext context, Camera camera, ComputeShader compute)
        {
            var cmd = s_Commands ??= new CommandBuffer { name = "HB Style Liquid Wake" };
            var now = Time.realtimeSinceStartupAsDouble;
            var kernel = compute != null && SystemInfo.supportsComputeShaders && compute.HasKernel("Wake")
                ? compute.FindKernel("Wake")
                : -1;

            Prune(camera, now);
            s_Views.TryGetValue(camera, out var view);

            if (view != null && now - view.stamp < SameFrame)
                return;

            var position = camera.transform.position;
            var stamps = kernel >= 0 && Application.isPlaying && (view != null || HBStyleLiquidTrail.any)
                ? HBStyleLiquidTrail.Collect(position, Reach, s_Stamps, now)
                : 0;

            if (view == null && stamps > 0)
            {
                view = new View
                {
                    state = NewMap(FilterMode.Point),
                    next = NewMap(FilterMode.Point),
                    look = NewMap(FilterMode.Bilinear),
                    stamp = now
                };
                s_Views[camera] = view;
            }

            if (view != null)
            {
                view.lastUsed = now;

                if (stamps > 0)
                    view.until = now + Linger;

                if (kernel < 0 || !Application.isPlaying || now > view.until)
                {
                    view.Release();
                    s_Views.Remove(camera);
                    view = null;
                }
            }

            cmd.Clear();

            if (view == null)
            {
                cmd.SetGlobalVector(s_MapStateId, Vector4.zero);
                cmd.SetGlobalTexture(s_MapId, Texture2D.blackTexture);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                return;
            }

            var step = Mathf.Min((float)(now - view.stamp), MostStep);

            s_StampBuffer ??= new GraphicsBuffer(GraphicsBuffer.Target.Structured, s_Stamps.Length, sizeof(float) * 4);
            view.stamp = now;

            if (stamps > 0)
                cmd.SetBufferData(s_StampBuffer, s_Stamps, 0, 0, stamps * HBStyleLiquidTrail.StampSize);

            cmd.SetComputeVectorParam(compute, s_FrameId, new Vector4(position.x, position.z, step,
                                                                      view.ready ? 0f : 1f));
            cmd.SetComputeVectorParam(compute, s_FromId, new Vector4(view.from.x, view.from.y, stamps, 0f));
            cmd.SetComputeBufferParam(compute, kernel, s_StampsId, s_StampBuffer);
            cmd.SetComputeTextureParam(compute, kernel, s_StateId, view.state);
            cmd.SetComputeTextureParam(compute, kernel, s_StateOutId, view.next);
            cmd.SetComputeTextureParam(compute, kernel, s_LookOutId, view.look);
            cmd.DispatchCompute(compute, kernel, MapSize / 8, MapSize / 8, 1);
            cmd.SetGlobalTexture(s_MapId, view.look);
            cmd.SetGlobalVector(s_MapStateId, new Vector4(1f, position.x, position.z, 0f));
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            (view.state, view.next) = (view.next, view.state);
            view.from = new Vector4(position.x, position.z, 0f, 0f);
            view.ready = true;
        }

        static void Prune(Camera camera, double now)
        {
            s_Stale.Clear();

            foreach (var pair in s_Views)
            {
                if (pair.Key == null || (pair.Key != camera && now - pair.Value.lastUsed > PruneSeconds))
                    s_Stale.Add(pair.Key);
            }

            foreach (var stale in s_Stale)
            {
                s_Views[stale].Release();
                s_Views.Remove(stale);
            }
        }

        static void Reset()
        {
            foreach (var view in s_Views.Values)
                view.Release();

            s_Views.Clear();
            s_StampBuffer?.Release();
            s_StampBuffer = null;
            s_Commands?.Release();
            s_Commands = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            Reset();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Reset;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Reset;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                Reset();
        }
#endif
    }
}
