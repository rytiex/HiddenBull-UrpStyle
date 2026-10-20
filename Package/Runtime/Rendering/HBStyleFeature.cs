using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleRendererFeature("HiddenBull Style")]
    public sealed class HBStyleFeature : ScriptableRendererFeature
    {
        [SerializeField]
        HBStyleBrushGlobalSettings m_Brush = new HBStyleBrushGlobalSettings();

        [SerializeField]
        HBStyleCloudGlobalSettings m_Clouds = new HBStyleCloudGlobalSettings();

        [SerializeField]
        HBStyleShadowQualitySettings m_Shadows = new HBStyleShadowQualitySettings();

        [SerializeField]
        HBStyleGrassQualitySettings m_Grass = new HBStyleGrassQualitySettings();

        [SerializeField]
        HBStylePrecipitationQualitySettings m_Precipitation = new HBStylePrecipitationQualitySettings();

        [SerializeField]
        [Tooltip("How much work goes into every liquid. Low draws a coarser ocean, drops soft caustic " +
                 "shadows, crest trails and underwater light shafts. Medium keeps them at half cost. High " +
                 "is the full look.")]
        HBStyleLiquidQuality m_LiquidQuality = HBStyleLiquidQuality.High;

        [SerializeField]
        [HideInInspector]
        Shader m_FogShader;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_FogVolumeCompute;

        [SerializeField]
        [HideInInspector]
        Shader m_TransparencyShader;

        [SerializeField]
        [HideInInspector]
        Shader m_PrecipitationShader;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_ShelterCompute;

        [SerializeField]
        [HideInInspector]
        Shader m_GrassShader;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_GrassCompute;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_WavesCompute;

        [SerializeField]
        [HideInInspector]
        ComputeShader m_BillboardCompute;

        [SerializeField]
        [HideInInspector]
        Shader m_UnderwaterShader;

        [SerializeField]
        [Tooltip("Replaces the image with one term of the fog calculation, so a fog artefact can be " +
                 "traced to the step that made it instead of guessed at.\n\n" +
                 "Amount is how much fog lies in front of each point, Sun how much of that air the " +
                 "sun reaches, Sky how much of it has open sky above rather than a roof, and " +
                 "Scattering the sun glow on its own.")]
        HBStyleFogDebug m_FogDebug = HBStyleFogDebug.Off;

        [SerializeField]
        [Tooltip("Replaces the colour of every surface that receives weather with one term of the weather " +
                 "calculation, so a dry or snowed-over patch can be traced to the step that made it.\n\n" +
                 "Sky Exposure is white under open sky and black under cover. Wetness and Snow are how wet " +
                 "and how snowed-over the surface is before its own limits. Limits shows the surface's " +
                 "Wetness in red and Snow Cover in green. Soak shows the scene-wide state: wet in red, snow " +
                 "in green, rain in blue. Magenta means the weather is not reaching that surface at all.")]
        HBStyleWeatherDebug m_WeatherDebug = HBStyleWeatherDebug.Off;

        StyleGlobalsPass m_GlobalsPass;
        StyleFogPass m_FogPass;
        StyleSkyOcclusion m_SkyOcclusion;
        StyleTransparencyPass m_TransparencyPass;
        StyleTransparentShadows m_TransparentShadows;
        StylePrecipitationPass m_PrecipitationPass;
        StyleGrassPass m_GrassPass;
        StyleWavesPass m_WavesPass;
        StyleUnderwaterPass m_UnderwaterPass;

        public HBStyleBrushGlobalSettings brush => m_Brush;

        public HBStyleCloudGlobalSettings clouds => m_Clouds;

        public HBStyleShadowQualitySettings shadows => m_Shadows;

        public HBStyleGrassQualitySettings grass => m_Grass;

        public HBStylePrecipitationQualitySettings precipitation => m_Precipitation;

        public HBStyleLiquidQuality liquidQuality
        {
            get => m_LiquidQuality;
            set => m_LiquidQuality = value;
        }

        public override void Create()
        {
            m_Brush ??= new HBStyleBrushGlobalSettings();
            m_Clouds ??= new HBStyleCloudGlobalSettings();
            m_Shadows ??= new HBStyleShadowQualitySettings();
            m_Grass ??= new HBStyleGrassQualitySettings();
            m_Precipitation ??= new HBStylePrecipitationQualitySettings();

            Dispose(true);

            m_GlobalsPass = new StyleGlobalsPass();
            m_FogPass = new StyleFogPass();
            m_SkyOcclusion = new StyleSkyOcclusion();
            m_TransparencyPass = new StyleTransparencyPass();
            m_TransparentShadows = new StyleTransparentShadows();
            m_PrecipitationPass = new StylePrecipitationPass();
            m_GrassPass = new StyleGrassPass();
            m_WavesPass = new StyleWavesPass();
            m_UnderwaterPass = new StyleUnderwaterPass();

#if UNITY_EDITOR
            if (m_FogShader == null)
                m_FogShader = Shader.Find(StyleFogPass.ShaderName);

            if (m_FogVolumeCompute == null)
                m_FogVolumeCompute = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleFogPass.ComputePath);

            if (m_TransparencyShader == null)
                m_TransparencyShader = Shader.Find(StyleTransparencyPass.ShaderName);

            if (m_PrecipitationShader == null)
                m_PrecipitationShader = Shader.Find(StylePrecipitationPass.ShaderName);

            if (m_ShelterCompute == null)
                m_ShelterCompute = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleSkyOcclusion.ShelterComputePath);

            if (m_GrassShader == null)
                m_GrassShader = Shader.Find(StyleGrassPass.ShaderName);

            if (m_GrassCompute == null)
                m_GrassCompute = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleGrassPass.ComputePath);

            if (m_UnderwaterShader == null)
                m_UnderwaterShader = Shader.Find(StyleUnderwaterPass.ShaderName);

            if (m_WavesCompute == null)
                m_WavesCompute = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleWavesPass.ComputePath);

            if (m_BillboardCompute == null)
                m_BillboardCompute =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleBillboardSystem.ComputePath);
#endif

            StyleBillboardSystem.Setup(m_BillboardCompute);
            m_Shadows.ApplyDeferred();

#if UNITY_EDITOR
            ForceDepthPriming();
#endif

            HBStyleGlobalDefaults.Apply();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_GlobalsPass.Setup(m_Brush, m_Clouds, m_Shadows, m_WeatherDebug);
            renderer.EnqueuePass(m_GlobalsPass);

#if UNITY_EDITOR
            if (m_WavesCompute == null)
                m_WavesCompute = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(StyleWavesPass.ComputePath);
#endif

            m_WavesPass.Setup(m_WavesCompute, m_SkyOcclusion, m_LiquidQuality);

            if (m_WavesPass.isReady)
                renderer.EnqueuePass(m_WavesPass);

            renderer.EnqueuePass(m_TransparentShadows);
            m_SkyOcclusion.Setup(m_ShelterCompute);
            renderer.EnqueuePass(m_SkyOcclusion);

            m_GrassPass.Setup(m_GrassShader, m_GrassCompute, m_Grass);

            if (m_GrassPass.isReady)
                renderer.EnqueuePass(m_GrassPass);

            m_FogPass.Setup(m_FogShader, m_FogVolumeCompute, m_FogDebug);

            if (m_FogPass.isReady)
                renderer.EnqueuePass(m_FogPass);

            m_PrecipitationPass.Setup(m_PrecipitationShader, m_Precipitation);

            m_TransparencyPass.Setup(m_TransparencyShader, m_PrecipitationPass.isReady ? m_PrecipitationPass : null);

            if (m_TransparencyPass.isReady)
                renderer.EnqueuePass(m_TransparencyPass);

            if (m_PrecipitationPass.isReady)
                renderer.EnqueuePass(m_PrecipitationPass);

            m_UnderwaterPass.Setup(m_UnderwaterShader);

            if (m_UnderwaterPass.isReady && m_WavesPass.isReady)
                renderer.EnqueuePass(m_UnderwaterPass);
        }

#if UNITY_EDITOR
        void ForceDepthPriming()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
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

            m_FogPass?.Dispose();
            m_FogPass = null;

            m_SkyOcclusion?.Dispose();
            m_SkyOcclusion = null;

            m_TransparencyPass?.Dispose();
            m_TransparencyPass = null;

            m_TransparentShadows?.Dispose();
            m_TransparentShadows = null;

            m_WavesPass?.Dispose();
            m_WavesPass = null;

            m_GrassPass?.Dispose();
            m_GrassPass = null;

            m_PrecipitationPass?.Dispose();
            m_PrecipitationPass = null;

            m_UnderwaterPass?.Dispose();
            m_UnderwaterPass = null;
        }
    }
}
