using System;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleStudioUndoState : ScriptableObject
    {
        [SerializeField]
        string m_Json;

        object m_Target;
        Action m_Restored;

        internal static StyleStudioUndoState Create(object target, Action restored)
        {
            var state = CreateInstance<StyleStudioUndoState>();

            state.hideFlags = HideFlags.HideAndDontSave;
            state.m_Target = target;
            state.m_Restored = restored;
            state.m_Json = JsonUtility.ToJson(target);
            Undo.undoRedoPerformed += state.OnUndoRedo;
            return state;
        }

        internal void Commit(string name)
        {
            var json = JsonUtility.ToJson(m_Target);

            if (json == m_Json)
                return;

            Undo.RecordObject(this, name);
            m_Json = json;
        }

        void OnUndoRedo()
        {
            if (m_Target == null || m_Json == JsonUtility.ToJson(m_Target))
                return;

            JsonUtility.FromJsonOverwrite(m_Json, m_Target);
            m_Restored?.Invoke();
        }

        internal void Release()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.ClearUndo(this);
            DestroyImmediate(this);
        }
    }
}
