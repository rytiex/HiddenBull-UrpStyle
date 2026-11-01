using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStylePlant))]
    sealed class StylePlantEditor : UnityEditor.Editor
    {
        static class Styles
        {
            public static readonly GUIContent Bake = new GUIContent(
                "Bake Plant",
                "Writes the rounded canopy, the wind reach and the Sway into copies of the plant's meshes and points " +
                "its renderers at them. Press it again after changing the plant.");

            public static readonly GUIContent Clear = new GUIContent(
                "Clear Bake",
                "Points the plant's renderers back at their original meshes, so it stops moving in the wind and " +
                "leaves light by their own normals. The baked mesh files stay in the project.");

            public const string NoFoliage =
                "No renderer under this object uses HB URP Style/Foliage or Foliage Body, so there is nothing to bake.";

            public const string SwayChanged = "Sway differs from the baked value. Press Bake Plant to apply it.";
        }

        SerializedProperty m_Sway;

        void OnEnable()
        {
            m_Sway = serializedObject.FindProperty("m_Sway");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Sway);
            serializedObject.ApplyModifiedProperties();

            var plant = (HBStylePlant)target;
            var total = StylePlantBaker.CountMeshes(plant, out var baked);

            if (total == 0)
                EditorGUILayout.HelpBox(Styles.NoFoliage, MessageType.Warning);
            else
                EditorGUILayout.LabelField("Baked Meshes", $"{baked} of {total}");

            if (StylePlantBaker.SwayChanged(plant))
                EditorGUILayout.HelpBox(Styles.SwayChanged, MessageType.Warning);

            using (new EditorGUI.DisabledScope(total == 0 || EditorApplication.isPlaying))
            {
                if (GUILayout.Button(Styles.Bake))
                    StylePlantBaker.Bake(plant);
            }

            using (new EditorGUI.DisabledScope(!StylePlantBaker.CanClear(plant) || EditorApplication.isPlaying))
            {
                if (GUILayout.Button(Styles.Clear))
                    StylePlantBaker.Clear(plant);
            }
        }
    }
}
