using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    abstract class StyleStudioPage
    {
        [NonSerialized]
        internal StyleTextureStudio host;

        internal abstract GUIContent title { get; }

        internal abstract void DrawSettings();

        internal abstract void DrawLayers();

        internal abstract void DrawPreview(Rect rect);

        internal virtual void DrawToolbar(Rect rect)
        {
        }

        internal abstract void Release();
    }
}
