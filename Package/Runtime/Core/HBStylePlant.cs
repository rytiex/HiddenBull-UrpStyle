using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HB Style/Foliage/HB Style Plant")]
    public sealed class HBStylePlant : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)]
        [Tooltip("How far the top of the plant moves in full wind, in metres. The root stays put. " +
                 "Leaves and body share it, so the leaves stay on the branches. Press Bake Plant after changing it.")]
        float m_Sway = 0.3f;

        public float sway => m_Sway;

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
    }
}
