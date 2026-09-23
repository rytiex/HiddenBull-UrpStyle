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
        [HideInInspector]
        Shader m_BrushDistortionShader;

        StyleGlobalsPass m_GlobalsPass;

        BrushDistortionPass m_DistortionPass;

        public BrushGlobalSettings brush => m_Brush;

        public CloudGlobalSettings clouds => m_Clouds;

        public override void Create()
        {
            m_Brush ??= new BrushGlobalSettings();
            m_Clouds ??= new CloudGlobalSettings();
            m_GlobalsPass = new StyleGlobalsPass();
            m_DistortionPass = new BrushDistortionPass();

            ResolveShaders();

            StyleGlobalDefaults.Apply();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_GlobalsPass.Setup(m_Brush, m_Clouds);
            renderer.EnqueuePass(m_GlobalsPass);

            var cameraType = renderingData.cameraData.cameraType;

            if (cameraType != CameraType.Preview
                && cameraType != CameraType.Reflection
                && m_Brush.distortionEnabled
                && m_DistortionPass.Setup(m_BrushDistortionShader))
                renderer.EnqueuePass(m_DistortionPass);
        }

        void ResolveShaders()
        {
#if UNITY_EDITOR
            if (m_BrushDistortionShader != null)
                return;

            m_BrushDistortionShader = Shader.Find(BrushDistortionPass.ShaderName);

            if (m_BrushDistortionShader != null)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        protected override void Dispose(bool disposing)
        {
            m_GlobalsPass?.Dispose();
            m_GlobalsPass = null;

            m_DistortionPass?.Dispose();
            m_DistortionPass = null;
        }
    }
}
