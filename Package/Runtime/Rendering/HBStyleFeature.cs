using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleRendererFeature("HB Style")]
    public sealed class HBStyleFeature : ScriptableRendererFeature
    {
        [SerializeField]
        [Tooltip("Quality levels of every HB Style module drawn by this renderer. Empty uses the defaults.")]
        HBStyleQualityProfile m_Quality;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_ShelterCompute;

        [SerializeField]
        [HideInInspector]
        Shader m_UndersideShader;

        [SerializeField]
        [HideInInspector]
        Shader m_PrecipitationShader;

        [SerializeField]
        [HideInInspector]
        Shader m_AtmosphereShader;

        StyleCameraStates m_Cameras;
        StyleGlobalsPass m_GlobalsPass;
        StyleShelterPass m_ShelterPass;
        StyleAtmospherePass m_AtmospherePass;
        StylePrecipitationPass m_PrecipitationPass;
        HBStyleQualityProfile m_DefaultQuality;

        public HBStyleQualityProfile quality
        {
            get => m_Quality;
            set => m_Quality = value;
        }

        internal StyleCameraStates cameras => m_Cameras;

        HBStyleQualityProfile activeQuality
        {
            get
            {
                if (m_Quality != null)
                    return m_Quality;

                if (m_DefaultQuality == null)
                {
                    m_DefaultQuality = CreateInstance<HBStyleQualityProfile>();
                    m_DefaultQuality.name = "HB Style Default Quality";
                    m_DefaultQuality.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_DefaultQuality;
            }
        }

        public override void Create()
        {
            Dispose(true);

            m_Cameras = new StyleCameraStates();
            m_GlobalsPass = new StyleGlobalsPass();
            m_ShelterPass = new StyleShelterPass();
            m_AtmospherePass = new StyleAtmospherePass();
            m_PrecipitationPass = new StylePrecipitationPass();

#if UNITY_EDITOR
            m_ShelterCompute = Load(m_ShelterCompute, StyleShelterPass.ComputePath);
            m_UndersideShader = Load(m_UndersideShader, StyleShelterPass.UndersidePath);
            m_PrecipitationShader = Load(m_PrecipitationShader, StylePrecipitationPass.ShaderPath);
            m_AtmosphereShader = Load(m_AtmosphereShader, StyleAtmospherePass.ShaderPath);

            ForceDepthPriming();
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var quality = activeQuality;
            var shadows = quality.shadows;

            m_GlobalsPass.Setup(quality, m_Cameras, m_ShelterPass);
            StyleTerrains.quality = quality.terrain;
            renderer.EnqueuePass(m_GlobalsPass);

            if (StyleCameraPolicy.Classify(cameraData.cameraType, cameraData.renderType) != StyleCameraKind.Base)
                return;

            m_Cameras.Touch(cameraData.camera);

            m_ShelterPass.Setup(m_ShelterCompute, m_UndersideShader, quality.weather);
            renderer.EnqueuePass(m_ShelterPass);

            m_AtmospherePass.Setup(m_AtmosphereShader, quality.atmosphere);
            renderer.EnqueuePass(m_AtmospherePass);

            m_PrecipitationPass.Setup(m_PrecipitationShader, quality.precipitation);
            renderer.EnqueuePass(m_PrecipitationPass);

            StyleShadowSettings.Apply(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset, shadows);
        }

#if UNITY_EDITOR
        T Load<T>(T current, string path) where T : Object
        {
            if (current != null)
                return current;

            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

            if (loaded != null)
                UnityEditor.EditorUtility.SetDirty(this);

            return loaded;
        }

        bool m_PrimingPending;

        void ForceDepthPriming()
        {
            if (m_PrimingPending)
                return;

            m_PrimingPending = true;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                m_PrimingPending = false;

                if (this == null)
                    return;

                var path = UnityEditor.AssetDatabase.GetAssetPath(this);

                if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) is not UniversalRendererData data ||
                    data.depthPrimingMode == DepthPrimingMode.Forced)
                    return;

                data.depthPrimingMode = DepthPrimingMode.Forced;
                UnityEditor.EditorUtility.SetDirty(data);
            };
        }
#endif

        protected override void Dispose(bool disposing)
        {
            m_GlobalsPass?.Dispose();
            m_GlobalsPass = null;

            m_ShelterPass?.Dispose();
            m_ShelterPass = null;

            m_AtmospherePass?.Dispose();
            m_AtmospherePass = null;

            m_PrecipitationPass?.Dispose();
            m_PrecipitationPass = null;

            m_Cameras?.Dispose();
            m_Cameras = null;

            CoreUtils.Destroy(m_DefaultQuality);
            m_DefaultQuality = null;
        }
    }
}
