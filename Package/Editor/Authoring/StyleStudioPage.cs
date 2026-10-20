using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public abstract class StyleStudioPage : ScriptableObject
    {
        internal EditorWindow host;

        public abstract string title { get; }

        public abstract void OnGUI();

        protected void Repaint()
        {
            if (host != null)
                host.Repaint();
        }
    }
}
