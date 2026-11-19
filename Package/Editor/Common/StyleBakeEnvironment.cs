using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleBakeEnvironment
    {
        const int StartUpdates = 600;

        static int s_Waiting;
        static bool s_Seen;

        internal static bool Begin()
        {
            s_Seen = false;
            s_Waiting = 0;

            if (StyleEnvironment.baking)
                return false;

            StyleEnvironment.baking = true;
            Show();
            DynamicGI.UpdateEnvironment();

            return true;
        }

        internal static void Watch(bool running)
        {
            if (!StyleEnvironment.baking)
                return;

            if (running)
            {
                s_Seen = true;
                return;
            }

            if (!s_Seen && ++s_Waiting < StartUpdates)
                return;

            StyleEnvironment.baking = false;
            Show();
        }

        static void Show()
        {
            var controller = StyleEnvironment.controller;

            if (controller != null)
                controller.Refresh();

            var view = SceneView.lastActiveSceneView;

            if (view != null && view.camera != null)
                view.camera.Render();

            SceneView.RepaintAll();
        }
    }
}
