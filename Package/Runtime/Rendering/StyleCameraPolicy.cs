using UnityEngine;
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
    }
}
