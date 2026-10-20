using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Vegetation/HB Style Billboard")]
    public sealed class HBStyleBillboard : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Baked data of this plant: the meshes drawn up close, the billboard drawn far away and the " +
                 "distances between them. Made by Tools > HiddenBull > URP Style > Billboard Baker.")]
        HBStyleBillboardAsset m_Asset;

        readonly List<Renderer> m_Hidden = new List<Renderer>();
        readonly List<MeshRenderer> m_Found = new List<MeshRenderer>();

        internal int slot = -1;
        internal HBStyleBillboardAsset registered;

        public HBStyleBillboardAsset asset
        {
            get => m_Asset;
            set
            {
                m_Asset = value;

                if (isActiveAndEnabled)
                    StyleBillboardSystem.Add(this);
            }
        }

        void OnEnable()
        {
            if (Previewed())
                return;

            StyleBillboardSystem.Add(this);
        }

        void OnDisable()
        {
            StyleBillboardSystem.Remove(this);
        }

        void OnValidate()
        {
            if (isActiveAndEnabled && !Previewed())
                StyleBillboardSystem.Add(this);
        }

        internal void Hide(bool hide)
        {
            if (hide)
            {
                if (m_Hidden.Count > 0 || m_Asset == null)
                    return;

                GetComponentsInChildren(true, m_Found);

                foreach (var renderer in m_Found)
                {
                    if (!renderer.TryGetComponent<MeshFilter>(out var filter) || !Drawn(filter.sharedMesh))
                        continue;

                    renderer.forceRenderingOff = true;
                    m_Hidden.Add(renderer);
                }

                m_Found.Clear();
                return;
            }

            foreach (var renderer in m_Hidden)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }

            m_Hidden.Clear();
        }

        bool Drawn(Mesh mesh)
        {
            if (mesh == null)
                return false;

            if (mesh == m_Asset.billboardMesh)
                return true;

            foreach (var draw in m_Asset.draws)
            {
                if (draw.mesh == mesh)
                    return true;
            }

            return false;
        }

        bool Previewed()
        {
#if UNITY_EDITOR
            return UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene);
#else
            return false;
#endif
        }
    }
}
