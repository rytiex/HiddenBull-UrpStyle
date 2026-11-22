using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    enum StyleCameraKind
    {
        Base,
        Overlay,
        Isolated
    }

    static class StyleCameraPolicy
    {
        internal static StyleCameraKind Classify(CameraType type, CameraRenderType renderType)
        {
            if (type != CameraType.Game && type != CameraType.SceneView)
                return StyleCameraKind.Isolated;

            return renderType == CameraRenderType.Overlay ? StyleCameraKind.Overlay : StyleCameraKind.Base;
        }

        internal static bool Filtered(Camera camera)
        {
#if UNITY_EDITOR
            return camera != null && CoreUtils.IsSceneFilteringEnabled() &&
                   camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
#else
            return false;
#endif
        }

        internal static bool Filtered(Camera camera, GameObject owner)
        {
            return Filtered(camera) && !(camera.scene.IsValid() && owner.scene == camera.scene);
        }

        internal static bool Preview(Camera camera)
        {
#if UNITY_EDITOR
            return camera != null && camera.scene.IsValid() &&
                   UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(camera.scene);
#else
            return false;
#endif
        }
    }
}
