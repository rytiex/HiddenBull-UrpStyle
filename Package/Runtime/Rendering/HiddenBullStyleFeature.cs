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

        [SerializeField]
        [HideInInspector]
        Shader m_FogShader;

        [SerializeField]
        [Tooltip("Replaces the image with one term of the fog calculation, so a fog artefact can be " +
                 "traced to the step that made it instead of guessed at.\n\n" +
                 "Reach is the light the air receives, gathered and upsampled. Reach Raw is the same " +
                 "before the depth-aware filter, which is where a half resolution artefact shows " +
                 "itself. Amount is the distance integral alone, Sunlit the shadow reading, and " +
                 "Scattering the sun glow on its own.")]
        StyleFogDebug m_FogDebug = StyleFogDebug.Off;

        StyleGlobalsPass m_GlobalsPass;
        StyleFogPass m_FogPass;

        public BrushGlobalSettings brush => m_Brush;

        public CloudGlobalSettings clouds => m_Clouds;

        public ShadowQualitySettings shadows => m_Shadows;

        public override void Create()
        {
            m_Brush ??= new BrushGlobalSettings();
            m_Clouds ??= new CloudGlobalSettings();
            m_Shadows ??= new ShadowQualitySettings();
            m_GlobalsPass = new StyleGlobalsPass();
            m_FogPass = new StyleFogPass();

#if UNITY_EDITOR
            if (m_FogShader == null)
                m_FogShader = Shader.Find(StyleFogPass.ShaderName);
#endif

            m_Shadows.ApplyDeferred();

            StyleGlobalDefaults.Apply();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_GlobalsPass.Setup(m_Brush, m_Clouds, m_Shadows);
            renderer.EnqueuePass(m_GlobalsPass);

            m_FogPass.Setup(m_FogShader, m_FogDebug);

            if (m_FogPass.isReady)
                renderer.EnqueuePass(m_FogPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_GlobalsPass?.Dispose();
            m_GlobalsPass = null;

            m_FogPass?.Dispose();
            m_FogPass = null;
        }
    }
}
