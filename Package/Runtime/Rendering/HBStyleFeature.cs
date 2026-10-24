using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleRendererFeature("HB Style")]
    public sealed class HBStyleFeature : ScriptableRendererFeature
    {
        [SerializeField]
        [Tooltip("Quality levels of every HB Style module drawn by this renderer. Empty uses the defaults.")]
        HBStyleQualityProfile m_Quality;

        StyleCameraStates m_Cameras;
        StyleGlobalsPass m_GlobalsPass;

        public HBStyleQualityProfile quality
        {
            get => m_Quality;
            set => m_Quality = value;
        }

        internal StyleCameraStates cameras => m_Cameras;

        public override void Create()
        {
            Dispose(true);

            m_Cameras = new StyleCameraStates();
            m_GlobalsPass = new StyleGlobalsPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;

            renderer.EnqueuePass(m_GlobalsPass);

            if (StyleCameraPolicy.Classify(cameraData.cameraType, cameraData.renderType) != StyleCameraKind.Base)
                return;

            m_Cameras.Touch(cameraData.camera);
        }

        protected override void Dispose(bool disposing)
        {
            m_GlobalsPass?.Dispose();
            m_GlobalsPass = null;

            m_Cameras?.Dispose();
            m_Cameras = null;
        }
    }
}
