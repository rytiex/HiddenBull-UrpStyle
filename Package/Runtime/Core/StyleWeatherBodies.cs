using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    static class StyleWeatherBodies
    {
        const double ReadInterval = 0.2;
        const float CoverBias = 0.1f;
        const float CoverRange = 0.6f;
        const float UnderSoft = 0.05f;
        const float None = -1e5f;

        static readonly List<HBStyleWeatherBody> s_Bodies = new List<HBStyleWeatherBody>();

        static RTHandle s_Heights;
        static Vector4 s_WrittenArea;
        static bool s_Written;
        static bool s_Pending;
        static double s_LastRequest = double.NegativeInfinity;
        static double s_LastTick = double.NegativeInfinity;

        static float[] s_Map;
        static Vector4 s_MapArea;
        static bool s_MapValid;
        static Camera s_Camera;
        static double s_LastRead = double.NegativeInfinity;

        internal static bool wanted => Application.isPlaying && s_Bodies.Count > 0;

        internal static string status
        {
            get
            {
                if (s_Camera == null)
                    return "Waiting for a Game camera";

                if (!s_MapValid)
                    return $"Waiting for {s_Camera.name}";

                return $"{s_Camera.name}, read {Time.timeAsDouble - s_LastRead:0.0} s ago";
            }
        }

        internal static void Register(HBStyleWeatherBody body)
        {
            s_Bodies.Remove(body);
            s_Bodies.Add(body);
        }

        internal static void Unregister(HBStyleWeatherBody body)
        {
            s_Bodies.Remove(body);
        }

        internal static RTHandle Target(int size)
        {
            if (s_Heights == null || s_Heights.rt.width != size)
            {
                s_Heights?.Release();
                s_Heights = RTHandles.Alloc(size, size, colorFormat: GraphicsFormat.R32G32_SFloat,
                                            filterMode: FilterMode.Point, enableRandomWrite: true,
                                            name: "HB Style Weather Body Heights");
            }

            return s_Heights;
        }

        internal static void Written(Vector4 area, Camera camera)
        {
            s_WrittenArea = area;
            s_Camera = camera;
            s_Written = true;
        }

        internal static void Tick()
        {
            var now = Time.timeAsDouble;

            if (now == s_LastTick)
                return;

            s_LastTick = now;

            if (!s_Written || s_Pending || s_Heights == null || now - s_LastRequest < ReadInterval ||
                !SystemInfo.supportsAsyncGPUReadback)
                return;

            var area = s_WrittenArea;

            s_Pending = true;
            s_LastRequest = now;

            AsyncGPUReadback.Request(s_Heights.rt, 0, request => OnRead(request, area));
        }

        static void OnRead(AsyncGPUReadbackRequest request, Vector4 area)
        {
            s_Pending = false;

            if (request.hasError)
                return;

            var data = request.GetData<float>();

            if (s_Map == null || s_Map.Length != data.Length)
                s_Map = new float[data.Length];

            NativeArray<float>.Copy(data, s_Map, data.Length);

            s_MapArea = area;
            s_MapValid = true;
            s_LastRead = Time.timeAsDouble;
        }

        internal static float Exposure(Vector3 point)
        {
            return s_MapValid ? Open(point.x, point.z, point.y) : 1f;
        }

        static float Open(float x, float z, float y)
        {
            var size = (int)s_MapArea.w;
            var texel = s_MapArea.z / size;
            var i = Mathf.FloorToInt((x - s_MapArea.x) / texel + size * 0.5f);
            var j = Mathf.FloorToInt((z - s_MapArea.y) / texel + size * 0.5f);

            if (i < 0 || j < 0 || i >= size || j >= size)
                return 1f;

            var index = (j * size + i) * 2;
            var roof = s_Map[index];
            var under = s_Map[index + 1];
            var covered = Mathf.Clamp01((roof - y - CoverBias) / CoverRange);

            if (under > None)
                covered = Mathf.Max(covered, Mathf.Clamp01((under - y) / UnderSoft));

            return 1f - covered;
        }

        static void Reset()
        {
            s_Heights?.Release();
            s_Heights = null;
            s_Written = false;
            s_Pending = false;
            s_MapValid = false;
            s_Map = null;
            s_Camera = null;
            s_LastRead = double.NegativeInfinity;
            s_LastRequest = double.NegativeInfinity;
            s_LastTick = double.NegativeInfinity;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            s_Bodies.Clear();
            Reset();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                Reset();
        }
#endif
    }
}
