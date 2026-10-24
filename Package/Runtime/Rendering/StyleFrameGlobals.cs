using System.Runtime.InteropServices;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [StructLayout(LayoutKind.Sequential)]
    struct StyleFrameGlobals
    {
        internal const string BufferName = "HBStyleFrame";

        public Vector4 _HB_Environment;
        public Vector4 _HB_Weather;
        public Vector4 _HB_Debug;

        internal static StyleFrameGlobals Resolve()
        {
            var weather = StyleEnvironment.weather;

            return new StyleFrameGlobals
            {
                _HB_Environment = new Vector4(StyleEnvironment.timeOfDay, 0f, 0f, 0f),
                _HB_Weather = new Vector4(weather.x, weather.y, weather.z, 0f),
                _HB_Debug = new Vector4((int)StyleDebug.view, 0f, 0f, 0f)
            };
        }
    }
}
