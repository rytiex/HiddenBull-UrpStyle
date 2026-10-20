using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleBillboard))]
    [CanEditMultipleObjects]
    sealed class StyleBillboardEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var billboard = (HBStyleBillboard)target;

            if (billboard.asset == null || targets.Length > 1)
                return;

            if (!StyleBillboardSystem.drawing)
            {
                EditorGUILayout.HelpBox("Not drawn by the GPU: the renderer has no HiddenBull Style feature, so " +
                                        "the plant draws its own meshes without distance switching.",
                                        MessageType.Info);
                return;
            }

            var counts = StyleBillboardSystem.Watch(billboard.asset);

            if (!counts.valid)
            {
                EditorGUILayout.HelpBox("Waiting for the Game view to draw a frame.", MessageType.None);
                return;
            }

            var hidden = UnityEngine.Mathf.Max(counts.total - counts.meshes - counts.billboards, 0);

            EditorGUILayout.HelpBox(
                $"Game view, all copies of this plant ({counts.total}):\n" +
                $"Meshes: {counts.meshes}   Billboards: {counts.billboards}   Not drawn: {hidden}\n" +
                $"Triangles: {counts.triangles:N0} per pass (the Statistics window does not count GPU draws).",
                MessageType.None);
        }
    }
}
