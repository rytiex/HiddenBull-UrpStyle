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
                "Writes the rounded canopy, the wind reach and the Sway into copies of the plant's meshes, points " +
                "its renderers at them and hands the plant to the GPU. Press it again after changing the plant.");

            public static readonly GUIContent Clear = new GUIContent(
                "Clear Bake",
                "Points the plant's renderers back at their original meshes and takes the plant off the GPU, so it " +
                "stops moving in the wind and leaves light by their own normals. The baked files stay in the project.");

            public const string NoFoliage =
                "No renderer under this object uses HB URP Style/Foliage or Foliage Body, so there is nothing to bake.";

            public const string SwayChanged = "Sway differs from the baked value. Press Bake Plant to apply it.";

            public const string NotDrawn =
                "Not drawn by the GPU: the renderer has no HB Style feature, so the plant draws its own meshes.";

            public const string Waiting = "Waiting for a view to draw a frame.";

            public const string Alone =
                "A selected plant draws its own renderers. Select another object to hand this one back to the GPU.";
        }

        SerializedProperty m_Sway;
        SerializedObject m_Asset;

        void OnEnable()
        {
            m_Sway = serializedObject.FindProperty("m_Sway");
        }

        public override bool RequiresConstantRepaint() => ((HBStylePlant)target).asset != null;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Sway);
            serializedObject.ApplyModifiedProperties();

            var plant = (HBStylePlant)target;

            DrawDistances(plant.asset);

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

            DrawCounts(plant);
        }

        void DrawDistances(HBStylePlantAsset asset)
        {
            if (asset == null)
                return;

            if (m_Asset == null || m_Asset.targetObject != asset)
                m_Asset = new SerializedObject(asset);

            m_Asset.Update();

            using (new EditorGUI.DisabledScope(!asset.billboarded))
                EditorGUILayout.PropertyField(m_Asset.FindProperty("m_SwitchDistance"));

            EditorGUILayout.PropertyField(m_Asset.FindProperty("m_CullDistance"));

            using (new EditorGUI.DisabledScope(!asset.billboarded))
                EditorGUILayout.PropertyField(m_Asset.FindProperty("m_ShadowMeshDistance"));

            m_Asset.ApplyModifiedProperties();
        }

        static void DrawCounts(HBStylePlant plant)
        {
            if (plant.asset == null || !plant.asset.valid || plant.registered == null)
                return;

            if (!StylePlantSystem.drawing)
            {
                EditorGUILayout.HelpBox(Styles.NotDrawn, MessageType.Info);
                return;
            }

            var counts = StylePlantSystem.Watch(plant.asset);

            if (!counts.valid)
            {
                EditorGUILayout.HelpBox(Styles.Waiting, MessageType.None);
                return;
            }

            if (counts.total == 0)
            {
                EditorGUILayout.HelpBox(Styles.Alone, MessageType.None);
                return;
            }

            var hidden = Mathf.Max(counts.total - counts.meshes - counts.billboards, 0);

            EditorGUILayout.HelpBox(
                $"GPU copies of this plant ({counts.total}):\n" +
                $"Meshes: {counts.meshes}   Billboards: {counts.billboards}   Not drawn: {hidden}\n" +
                $"Triangles: {counts.triangles:N0} per pass (the Statistics window does not count GPU draws).\n" +
                $"Shadow meshes: {counts.shadowMeshes}   Shadow billboards: {counts.shadowBillboards}\n" +
                $"Shadow triangles: {counts.shadowTriangles:N0} per cascade.",
                MessageType.None);
        }
    }
}
