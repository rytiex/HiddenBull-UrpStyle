using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HB Style/Foliage/HB Style Plant")]
    public sealed class HBStylePlant : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)]
        [Tooltip("How far the top of the plant moves in full wind, in metres. The root stays put. " +
                 "Leaves and body share it, so the leaves stay on the branches. Press Bake Plant after changing it.")]
        float m_Sway = 0.3f;

        [SerializeField]
        HBStylePlantAsset m_Asset;

        readonly List<Renderer> m_Hidden = new List<Renderer>();
        readonly List<MeshRenderer> m_Found = new List<MeshRenderer>();

        internal int slot = -1;
        internal HBStylePlantAsset registered;
        internal bool editing;

        public float sway => m_Sway;

        public HBStylePlantAsset asset
        {
            get => m_Asset;
            set
            {
                m_Asset = value;

                if (isActiveAndEnabled && !Previewed())
                    StylePlantSystem.Add(this);
            }
        }

#if UNITY_EDITOR
        [Serializable]
        internal struct BakedMesh
        {
            public MeshFilter filter;
            public Mesh source;
        }

        [SerializeField]
        internal List<BakedMesh> bakedMeshes = new List<BakedMesh>();
#endif

        void OnEnable()
        {
            if (!Previewed())
                StylePlantSystem.Add(this);
        }

        void OnDisable()
        {
            StylePlantSystem.Remove(this);
        }

        void OnValidate()
        {
            if (isActiveAndEnabled && !Previewed())
                StylePlantSystem.Add(this);
        }

        internal void Hide(bool hide)
        {
            foreach (var renderer in m_Hidden)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }

            m_Hidden.Clear();

            if (!hide || m_Asset == null)
                return;

            GetComponentsInChildren(true, m_Found);

            foreach (var renderer in m_Found)
            {
                if (!Drawn(renderer))
                    continue;

                renderer.forceRenderingOff = true;
                m_Hidden.Add(renderer);
            }

            m_Found.Clear();
        }

        bool Drawn(Renderer renderer)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (!Drawn(material))
                    return false;
            }

            return true;
        }

        bool Drawn(Material material)
        {
            foreach (var draw in m_Asset.draws)
            {
                if (draw.material == material)
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
