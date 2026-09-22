using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    /// <summary>
    /// The single renderer feature the package adds to a URP renderer (Architecture D4).
    ///
    /// Later phases add passes that share resources — depth, normals, the occlusion and bent normal
    /// buffer, the stylized shadow mask. Those resources are owned here and declared once, so no
    /// feature re-derives a buffer another feature already produced. A style pass that cannot
    /// express its inputs as a dependency on this shared set does not belong as a separate feature;
    /// it belongs in here.
    ///
    /// Phase 1 uses it for one job: publishing the gradient ambient model to global constants.
    ///
    /// Quality scaling is Unity's: point each Quality Level at its own URP Asset and Renderer, and
    /// author <see cref="settings"/> per renderer (Architecture D7).
    /// </summary>
    [DisallowMultipleRendererFeature("HiddenBull Style")]
    public sealed class HiddenBullStyleFeature : ScriptableRendererFeature
    {
        [SerializeField]
        StyleSettings m_Settings = new StyleSettings();

        AmbientSetupPass m_AmbientSetupPass;

        /// <summary>Performance settings for this renderer.</summary>
        public StyleSettings settings => m_Settings;

        public override void Create()
        {
            m_Settings ??= new StyleSettings();
            m_AmbientSetupPass = new AmbientSetupPass();

            // Written eagerly so scene view and edit mode are lit before any camera renders,
            // where the RuntimeInitializeOnLoadMethod fallback does not run.
            AmbientDefaults.Apply();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_AmbientSetupPass);
        }
    }
}
