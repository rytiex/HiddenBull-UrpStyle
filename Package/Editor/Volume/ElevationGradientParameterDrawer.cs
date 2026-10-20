using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [VolumeParameterDrawer(typeof(HBStyleElevationGradientParameter))]
    sealed class ElevationGradientParameterDrawer : VolumeParameterDrawer
    {
        static readonly Color k_MarkerEdge = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color k_MarkerCore = new Color(0f, 0f, 0f, 0.9f);

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(rect, parameter.value, title);

            if (!TryGetElevation(out var elevation))
                return true;

            if (parameter.GetObjectRef<HBStyleElevationGradientParameter>() is { mirrored: true })
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

        static bool TryGetElevation(out float elevation)
        {
            if (HBStyleSun.active != null)
            {
                elevation = HBStyleSun.active.sunDirection.y;
                return true;
            }

            var sun = StyleSunInstaller.FindSun();

            elevation = sun != null ? -sun.transform.forward.y : 0f;

            return sun != null;
        }
    }
}
