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
        ComputeShader m_PlantCompute;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_PyramidCompute;

        [SerializeField]
        [HideInInspector]
        Shader m_UndersideShader;

        [SerializeField]
        [HideInInspector]
        Shader m_PrecipitationShader;

        [SerializeField]
        [HideInInspector]
        Shader m_AtmosphereShader;

        [SerializeField]
        [HideInInspector]
        Shader m_AntiAliasingShader;

        [SerializeField]
        [HideInInspector]
        Shader m_RefractionBlurShader;

        [SerializeField]
        [HideInInspector]
        Shader m_LiquidCutShader;

        [SerializeField]
        [HideInInspector]
        Shader m_UnderwaterShader;

        [SerializeField]
        [HideInInspector]
        Shader m_ReflectDepthShader;

        [SerializeField]
        [HideInInspector]
        Shader m_SkyMapShader;

        [SerializeField]
        [HideInInspector]
        Material m_TerrainVariants;

        StyleCameraStates m_Cameras;
        StyleGlobalsPass m_GlobalsPass;
        StyleShelterPass m_ShelterPass;
        StyleDepthPyramidPass m_PyramidPass;
        StyleAtmospherePass m_AtmospherePass;
        StylePrecipitationPass m_PrecipitationPass;
        StyleAntiAliasingPass m_AntiAliasingPass;
        StyleTransparencyPass m_TransparencyPass;
        StyleTransparentShadowPass m_TransparentShadowPass;
        StyleOverlayShadowPass m_OverlayShadowPass;
        StyleUnderwaterPass m_UnderwaterPass;
        StyleUnderwaterHeightPass m_UnderwaterHeightPass;
        StyleReflectionHistoryPass m_ReflectionHistoryPass;
        StyleSkyMapPass m_SkyMapPass;
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
            m_PyramidPass = new StyleDepthPyramidPass();
            m_AtmospherePass = new StyleAtmospherePass();
            m_PrecipitationPass = new StylePrecipitationPass();
            m_AntiAliasingPass = new StyleAntiAliasingPass();
            m_TransparencyPass = new StyleTransparencyPass();
            m_TransparentShadowPass = new StyleTransparentShadowPass();
            m_OverlayShadowPass = new StyleOverlayShadowPass();
            m_UnderwaterPass = new StyleUnderwaterPass();
            m_UnderwaterHeightPass = new StyleUnderwaterHeightPass();
            m_ReflectionHistoryPass = new StyleReflectionHistoryPass();
            m_SkyMapPass = new StyleSkyMapPass();

#if UNITY_EDITOR
            m_ShelterCompute = Load(m_ShelterCompute, StyleShelterPass.ComputePath);
            m_PlantCompute = Load(m_PlantCompute, StylePlantSystem.ComputePath);
            m_PyramidCompute = Load(m_PyramidCompute, StyleDepthPyramidPass.ComputePath);
            m_UndersideShader = Load(m_UndersideShader, StyleShelterPass.UndersidePath);
            m_PrecipitationShader = Load(m_PrecipitationShader, StylePrecipitationPass.ShaderPath);
            m_AtmosphereShader = Load(m_AtmosphereShader, StyleAtmospherePass.ShaderPath);
            m_AntiAliasingShader = Load(m_AntiAliasingShader, StyleAntiAliasingPass.ShaderPath);
            m_RefractionBlurShader = Load(m_RefractionBlurShader, StyleTransparencyPass.BlurShaderPath);
            m_LiquidCutShader = Load(m_LiquidCutShader, StyleTransparencyPass.CutShaderPath);
            m_UnderwaterShader = Load(m_UnderwaterShader, StyleUnderwaterPass.ShaderPath);
            m_ReflectDepthShader = Load(m_ReflectDepthShader, StyleReflectionHistoryPass.DepthShaderPath);
            m_SkyMapShader = Load(m_SkyMapShader, StyleSkyMapPass.ShaderPath);
            m_TerrainVariants = Load(m_TerrainVariants, HBStyleTerrain.VariantsPath);

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
            StyleTerrains.grassQuality = quality.grass;
            StyleTerrains.shelter = m_ShelterPass;
            StyleTerrains.pyramid = m_PyramidPass;
            StylePlantSystem.Setup(m_PlantCompute, m_PyramidPass);
            renderer.EnqueuePass(m_GlobalsPass);

            var kind = StyleCameraPolicy.Classify(cameraData.cameraType, cameraData.renderType);

            m_PrecipitationPass.Setup(m_PrecipitationShader, quality.precipitation);
            var baseCamera = kind == StyleCameraKind.Base;

            m_TransparencyPass.Setup(baseCamera ? m_PrecipitationPass : null, quality.transparency,
                                     quality.reflections, baseCamera ? m_ReflectionHistoryPass : null,
                                     m_RefractionBlurShader, m_LiquidCutShader);
            m_ReflectionHistoryPass.Setup(quality.reflections, m_ReflectDepthShader);
            m_ReflectionHistoryPass.binder.Setup(baseCamera);
            renderer.EnqueuePass(m_ReflectionHistoryPass.binder);
            m_TransparentShadowPass.Setup(quality.transparency);

            if (kind == StyleCameraKind.Base)
                renderer.EnqueuePass(m_UnderwaterHeightPass);

            renderer.EnqueuePass(m_TransparencyPass);
            renderer.EnqueuePass(m_TransparentShadowPass);

            if (kind == StyleCameraKind.Overlay ||
                kind == StyleCameraKind.Base && StyleOverlayShadowPass.Feeds(cameraData.camera))
            {
                m_OverlayShadowPass.Setup(shadows.level, m_TransparentShadowPass);
                renderer.EnqueuePass(m_OverlayShadowPass);
            }

            if (kind == StyleCameraKind.Overlay)
            {
                m_UnderwaterPass.Setup(m_UnderwaterShader, m_TransparencyPass, m_UnderwaterHeightPass, true,
                                       quality.transparency.level);
                renderer.EnqueuePass(m_UnderwaterPass);
            }

            var antiAliasing = quality.antiAliasing;

            if (kind != StyleCameraKind.Isolated && cameraData.resolveFinalTarget &&
                antiAliasing.level != HBStyleQualityLevel.Off)
            {
                m_AntiAliasingPass.Setup(m_AntiAliasingShader, antiAliasing);
                renderer.EnqueuePass(m_AntiAliasingPass);
            }

            if (kind != StyleCameraKind.Base)
                return;

            m_Cameras.Touch(cameraData.camera);

            m_ShelterPass.Setup(m_ShelterCompute, m_UndersideShader, m_LiquidCutShader, quality.weather);
            renderer.EnqueuePass(m_ShelterPass);

            if (StylePlantSystem.active || StyleTerrains.growing)
            {
                m_PyramidPass.Setup(m_PyramidCompute);
                renderer.EnqueuePass(m_PyramidPass);
            }

            m_AtmospherePass.Setup(m_AtmosphereShader, quality.atmosphere);
            renderer.EnqueuePass(m_AtmospherePass);

            renderer.EnqueuePass(m_ReflectionHistoryPass);

            m_SkyMapPass.Setup(m_SkyMapShader, quality.reflections.skyMapSize);
            renderer.EnqueuePass(m_SkyMapPass);

            m_UnderwaterPass.Setup(m_UnderwaterShader, m_TransparencyPass, m_UnderwaterHeightPass, false,
                                   quality.transparency.level);
            renderer.EnqueuePass(m_UnderwaterPass);

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (pipeline != null)
                pipeline.msaaSampleCount = 1;

            StyleShadowSettings.Apply(pipeline, shadows);
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
            StyleTerrains.shelter = null;
            StyleTerrains.pyramid = null;

            m_PyramidPass?.Dispose();
            m_PyramidPass = null;

            m_AtmospherePass?.Dispose();
            m_AtmospherePass = null;

            m_PrecipitationPass?.Dispose();
            m_PrecipitationPass = null;

            m_AntiAliasingPass?.Dispose();
            m_AntiAliasingPass = null;

            m_TransparencyPass?.Dispose();
            m_TransparencyPass = null;

            m_TransparentShadowPass?.Dispose();
            m_TransparentShadowPass = null;

            m_OverlayShadowPass?.Dispose();
            m_OverlayShadowPass = null;

            m_UnderwaterPass?.Dispose();
            m_UnderwaterPass = null;

            m_UnderwaterHeightPass?.Dispose();
            m_UnderwaterHeightPass = null;
            m_ReflectionHistoryPass?.Dispose();
            m_ReflectionHistoryPass = null;

            m_SkyMapPass?.Dispose();
            m_SkyMapPass = null;

            m_Cameras?.Dispose();
            m_Cameras = null;

            CoreUtils.Destroy(m_DefaultQuality);
            m_DefaultQuality = null;
        }
    }
}
