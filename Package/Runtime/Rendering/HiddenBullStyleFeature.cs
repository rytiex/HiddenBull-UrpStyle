using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleRendererFeature("HiddenBull Style")]
    public sealed class HiddenBullStyleFeature : ScriptableRendererFeature
    {
        [SerializeField]
        BrushGlobalSettings m_Brush = new BrushGlobalSettings();

        [SerializeField]
        CloudGlobalSettings m_Clouds = new CloudGlobalSettings();

        [SerializeField]
        ShadowQualitySettings m_Shadows = new ShadowQualitySettings();

        StyleGlobalsPass m_GlobalsPass;

        public BrushGlobalSettings brush => m_Brush;

        public CloudGlobalSettings clouds => m_Clouds;

        public ShadowQualitySettings shadows => m_Shadows;

        public override void Create()
        {
            m_Brush ??= new BrushGlobalSettings();
            m_Clouds ??= new CloudGlobalSettings();
            m_Shadows ??= new ShadowQualitySettings();
            m_GlobalsPass = new StyleGlobalsPass();

            m_Shadows.ApplyDeferred();

            StyleGlobalDefaults.Apply();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_GlobalsPass.Setup(m_Brush, m_Clouds, m_Shadows);
            renderer.EnqueuePass(m_GlobalsPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_GlobalsPass?.Dispose();
            m_GlobalsPass = null;
        }
    }
}
