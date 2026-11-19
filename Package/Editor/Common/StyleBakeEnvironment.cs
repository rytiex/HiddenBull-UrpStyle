using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleBakeEnvironment
    {
        const int StartUpdates = 600;
        const int PushSize = 16;

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

            Push();
            SceneView.RepaintAll();
        }

        static void Push()
        {
            var holder = new GameObject("HB Style Bake Environment") { hideFlags = HideFlags.HideAndDontSave };
            var target = RenderTexture.GetTemporary(PushSize, PushSize, 16);

            try
            {
                var camera = holder.AddComponent<Camera>();
                var data = holder.AddComponent<UniversalAdditionalCameraData>();
                var view = SceneView.lastActiveSceneView;

                camera.enabled = false;
                camera.targetTexture = target;
                data.requiresDepthTexture = false;
                data.requiresColorTexture = false;

                if (view != null && view.camera != null)
                    holder.transform.SetPositionAndRotation(view.camera.transform.position,
                                                            view.camera.transform.rotation);

                camera.Render();
            }
            finally
            {
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(holder);
            }
        }
    }
}
