using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class GradientSkyShaderGUI : ShaderGUI
    {
        const string k_Explanation =
            "This material has no settings, and that is deliberate.\n\n" +
            "The sky, the ambient lighting and the fog all read one set of gradients, so they come " +
            "from one place and cannot disagree with each other. Assign this material once and " +
            "author everything on a Volume:\n\n" +
            "•  HiddenBull > Style Sky  —  the gradients for day, dusk and night, and the light they cast\n" +
            "•  HiddenBull > Style Celestial  —  sun, moon and stars\n" +
            "•  HiddenBull > Style Clouds  —  the cloud bank\n" +
            "•  HiddenBull > Style Fog  —  haze, which also fades the sky toward the horizon";

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.HelpBox(k_Explanation, MessageType.Info);
        }
    }
}
