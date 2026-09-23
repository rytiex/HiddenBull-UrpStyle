using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleRendererFeature("HiddenBull Style")]
    public sealed class HiddenBullStyleFeature : ScriptableRendererFeature
    {
        [SerializeField]
        BrushGlobalSettings m_Brush = new BrushGlobalSettings();

        StyleGlobalsPass m_GlobalsPass;

        public BrushGlobalSettings brush => m_Brush;

        public override void Create()
        {
            m_Brush ??= new BrushGlobalSettings();
            m_GlobalsPass = new StyleGlobalsPass();

            StyleGlobalDefaults.Apply();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_GlobalsPass.Setup(m_Brush);
            renderer.EnqueuePass(m_GlobalsPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_GlobalsPass?.Dispose();
            m_GlobalsPass = null;
        }
    }
}
