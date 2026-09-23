using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [VolumeParameterDrawer(typeof(ElevationGradientParameter))]
    sealed class ElevationGradientParameterDrawer : VolumeParameterDrawer
    {
        static readonly Color k_MarkerEdge = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color k_MarkerCore = new Color(0f, 0f, 0f, 0.9f);

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(rect, parameter.value, title);

            var sun = FindSun();
            if (sun == null)
                return true;

            var elevation = -sun.transform.forward.y;
            if (parameter.GetObjectRef<ElevationGradientParameter>() is { mirrored: true })
                elevation = -elevation;

            var field = rect;
            field.xMin += EditorGUIUtility.labelWidth;

            if (field.width <= 4f)
                return true;

            var x = field.x + field.width * Mathf.Clamp01(elevation * 0.5f + 0.5f);

            EditorGUI.DrawRect(new Rect(x - 1.5f, field.y, 3f, field.height), k_MarkerEdge);
            EditorGUI.DrawRect(new Rect(x - 0.5f, field.y, 1f, field.height), k_MarkerCore);

            return true;
        }

        static Light FindSun()
        {
            if (RenderSettings.sun != null && RenderSettings.sun.isActiveAndEnabled)
                return RenderSettings.sun;

            var brightest = (Light)null;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type != LightType.Directional || !light.isActiveAndEnabled)
                    continue;

                if (brightest == null || light.intensity > brightest.intensity)
                    brightest = light;
            }

            return brightest;
        }
    }
}
