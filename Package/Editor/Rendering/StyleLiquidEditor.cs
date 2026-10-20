using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleLiquidMenu
    {
        const string Root = "GameObject/HiddenBull/Liquid/";
        const int Priority = 10;

        [MenuItem(Root + "Terrain", false, Priority)]
        static void CreateTerrain(MenuCommand command)
        {
            Create<HBStyleLiquidTerrain>(command, "Liquid Terrain");
        }

        [MenuItem(Root + "Pool", false, Priority)]
        static void CreatePool(MenuCommand command)
        {
            Create<HBStyleLiquidPool>(command, "Liquid Pool").transform.localScale = new Vector3(5f, 2f, 5f);
        }

        [MenuItem(Root + "Cutter", false, Priority + 11)]
        static void CreateCutter(MenuCommand command)
        {
            Create<HBStyleLiquidCutter>(command, "Liquid Cutter");
        }

        static GameObject Create<T>(MenuCommand command, string name) where T : Component
        {
            var created = new GameObject(name, typeof(T));

            GameObjectUtility.SetParentAndAlign(created, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            Selection.activeGameObject = created;

            return created;
        }
    }

    [CustomEditor(typeof(HBStyleLiquidCutter))]
    [CanEditMultipleObjects]
    sealed class StyleLiquidCutterEditor : UnityEditor.Editor
    {
        const int HeavyTriangles = 2000;
        const int CheckedIndices = 300000;
        const float Weld = 10000f;
        const int Channels = 4;

        static readonly GUIContent s_Fit = new GUIContent(
            "Fit To Parent",
            "Moves and scales this cutter so it wraps everything drawn under its parent object.");

        static readonly Dictionary<Mesh, bool> s_Closed = new Dictionary<Mesh, bool>();

        SerializedProperty m_Shape;
        SerializedProperty m_Mesh;
        SerializedProperty m_Mode;
        SerializedProperty m_Target;
        SerializedProperty m_KeepDry;
        SerializedProperty m_KeepGroundWet;
        SerializedProperty m_DryOffset;

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_Mode = serializedObject.FindProperty("m_Mode");
            m_Target = serializedObject.FindProperty("m_Target");
            m_KeepDry = serializedObject.FindProperty("m_KeepDry");
            m_KeepGroundWet = serializedObject.FindProperty("m_KeepGroundWet");
            m_DryOffset = serializedObject.FindProperty("m_DryOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Shape);

            var meshed = !m_Shape.hasMultipleDifferentValues
                      && m_Shape.enumValueIndex == (int)HBStyleLiquidCutShape.Mesh;

            using (new EditorGUI.DisabledScope(!meshed && !m_Shape.hasMultipleDifferentValues))
                EditorGUILayout.PropertyField(m_Mesh);

            if (meshed && !m_Mesh.hasMultipleDifferentValues)
                Warn(m_Mesh.objectReferenceValue as Mesh);

            EditorGUILayout.PropertyField(m_Mode);

            EditorGUILayout.PropertyField(m_Target);

            var keeps = !m_Mode.hasMultipleDifferentValues
                     && m_Mode.enumValueIndex == (int)HBStyleLiquidCutMode.KeepInside;

            using (new EditorGUI.DisabledScope(keeps))
                EditorGUILayout.PropertyField(m_KeepDry);

            var drains = !keeps && (m_KeepDry.hasMultipleDifferentValues || m_KeepDry.boolValue);

            using (new EditorGUI.DisabledScope(!drains))
                EditorGUILayout.PropertyField(m_KeepGroundWet);

            var dries = keeps || m_KeepDry.hasMultipleDifferentValues || m_KeepDry.boolValue;

            using (new EditorGUI.DisabledScope(!dries))
                EditorGUILayout.PropertyField(m_DryOffset);

            serializedObject.ApplyModifiedProperties();

            if (Reached() > Channels)
                EditorGUILayout.HelpBox("More than four liquids are cut at once. Every Liquid Terrain and Pool " +
                                        "touched by a cutter takes one of four cut channels, so a fifth one " +
                                        "can be cut in the wrong place where it overlaps another on screen.",
                                        MessageType.Warning);

            using (new EditorGUI.DisabledScope(meshed || !Fits()))
            {
                if (GUILayout.Button(s_Fit))
                {
                    foreach (var selected in targets)
                        Fit((HBStyleLiquidCutter)selected);
                }
            }
        }

        static int Reached()
        {
            var reached = 0;

            foreach (var terrain in HBStyleLiquidTerrain.terrains)
            {
                if (terrain != null && terrain.channel >= 0)
                    reached++;
            }

            foreach (var pool in HBStyleLiquidPool.pools)
            {
                if (pool != null && pool.channel >= 0)
                    reached++;
            }

            return reached;
        }

        bool Fits()
        {
            foreach (var selected in targets)
            {
                if (((HBStyleLiquidCutter)selected).transform.parent == null)
                    return false;
            }

            return true;
        }

        static void Fit(HBStyleLiquidCutter cutter)
        {
            var own = cutter.transform;
            var parent = own.parent;

            if (parent == null)
                return;

            var found = false;
            var bounds = new Bounds();

            foreach (var renderer in parent.GetComponentsInChildren<Renderer>())
            {
                if (renderer.transform.IsChildOf(own))
                    continue;

                var local = renderer.localBounds;
                var toParent = parent.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

                for (var corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                                             (corner & 4) == 0 ? -1f : 1f);
                    var point = toParent.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, offset));

                    if (found)
                        bounds.Encapsulate(point);
                    else
                        bounds = new Bounds(point, Vector3.zero);

                    found = true;
                }
            }

            if (!found)
                return;

            Undo.RecordObject(own, "Fit Liquid Cutter");

            own.localPosition = bounds.center;
            own.localRotation = Quaternion.identity;
            own.localScale = Vector3.Max(bounds.size, Vector3.one * 0.01f);
        }

        static void Warn(Mesh mesh)
        {
            if (mesh == null)
            {
                EditorGUILayout.HelpBox("Assign a mesh, or nothing is cut.", MessageType.Warning);
                return;
            }

            if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
            {
                EditorGUILayout.HelpBox("This mesh has no normals, so it cannot be used. Turn on normals in " +
                                        "its import settings.", MessageType.Error);
                return;
            }

            if (!Closed(mesh))
                EditorGUILayout.HelpBox("This mesh is not closed. Liquid leaks through the open edges and the " +
                                        "cut flickers as the camera moves. Close the holes in the model.",
                                        MessageType.Warning);

            var triangles = 0L;

            for (var part = 0; part < mesh.subMeshCount; part++)
                triangles += mesh.GetIndexCount(part) / 3;

            if (triangles > HeavyTriangles)
                EditorGUILayout.HelpBox("This mesh has " + triangles + " triangles and is drawn again for every " +
                                        "liquid each frame. A rough shape of a few hundred is enough.",
                                        MessageType.Info);

            if (!mesh.isReadable)
                EditorGUILayout.HelpBox("Read/Write is off on this mesh, so HBStyleLiquid height queries treat " +
                                        "the cutter as its bounding box. Turn on Read/Write in its import " +
                                        "settings for the exact shape.", MessageType.Info);
        }

        static bool Closed(Mesh mesh)
        {
            if (s_Closed.TryGetValue(mesh, out var closed))
                return closed;

            closed = true;

            try
            {
                using var all = Mesh.AcquireReadOnlyMeshData(mesh);
                var data = all[0];

                using var positions = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);

                data.GetVertices(positions);

                var welded = new Dictionary<Vector3Int, int>();
                var ids = new int[positions.Length];

                for (var index = 0; index < positions.Length; index++)
                {
                    var key = Vector3Int.RoundToInt(positions[index] * Weld);

                    if (!welded.TryGetValue(key, out var id))
                    {
                        id = welded.Count;
                        welded.Add(key, id);
                    }

                    ids[index] = id;
                }

                var edges = new Dictionary<long, int>();
                var partial = false;

                for (var part = 0; part < data.subMeshCount; part++)
                {
                    var sub = data.GetSubMesh(part);

                    if (sub.topology != MeshTopology.Triangles || sub.indexCount > CheckedIndices)
                    {
                        partial = true;
                        continue;
                    }

                    using var indices = new NativeArray<int>(sub.indexCount, Allocator.Temp);

                    data.GetIndices(indices, part);

                    for (var index = 0; index < indices.Length; index += 3)
                    {
                        for (var side = 0; side < 3; side++)
                        {
                            var from = ids[indices[index + side]];
                            var to = ids[indices[index + (side + 1) % 3]];

                            if (from == to)
                                continue;

                            var edge = from < to ? ((long)from << 32) | (uint)to : ((long)to << 32) | (uint)from;

                            edges.TryGetValue(edge, out var count);
                            edges[edge] = count + 1;
                        }
                    }
                }

                foreach (var count in edges.Values)
                {
                    if (count != 2 && !partial)
                    {
                        closed = false;
                        break;
                    }
                }
            }
            catch (System.Exception)
            {
                closed = true;
            }

            s_Closed[mesh] = closed;

            return closed;
        }
    }

    [CustomEditor(typeof(HBStyleLiquidFloater))]
    [CanEditMultipleObjects]
    sealed class StyleLiquidFloaterEditor : UnityEditor.Editor
    {
        static readonly GUIContent s_Fit = new GUIContent(
            "Fit To Renderers",
            "Sets Size and Offset so the footprint wraps everything drawn under this object.");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();

            if (!GUILayout.Button(s_Fit))
                return;

            foreach (var selected in targets)
                Fit((HBStyleLiquidFloater)selected);
        }

        static void Fit(HBStyleLiquidFloater floater)
        {
            var own = floater.transform;
            var toOwn = Matrix4x4.TRS(own.position, own.rotation, Vector3.one).inverse;
            var found = false;
            var bounds = new Bounds();

            foreach (var renderer in floater.GetComponentsInChildren<Renderer>())
            {
                var local = renderer.localBounds;
                var toFloater = toOwn * renderer.transform.localToWorldMatrix;

                for (var corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                                             (corner & 4) == 0 ? -1f : 1f);
                    var point = toFloater.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, offset));

                    if (found)
                        bounds.Encapsulate(point);
                    else
                        bounds = new Bounds(point, Vector3.zero);

                    found = true;
                }
            }

            if (!found)
                return;

            Undo.RecordObject(floater, "Fit Liquid Floater");

            floater.size = new Vector2(bounds.size.x, bounds.size.z);
            floater.offset = new Vector2(bounds.center.x, bounds.center.z);

            EditorUtility.SetDirty(floater);
        }
    }
}
