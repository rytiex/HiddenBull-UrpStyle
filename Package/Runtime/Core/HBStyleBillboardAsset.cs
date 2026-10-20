using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStyleBillboardDraw
    {
        public Mesh mesh;
        public int submesh;
        public Material material;
        public Matrix4x4 local;
        public ShadowCastingMode shadows;
    }

    public sealed class HBStyleBillboardAsset : ScriptableObject
    {
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Distance from the camera, in metres, where the plant turns into its billboard, for a 60 degree " +
                 "field of view. Zooming in (a smaller field of view) pushes it further away.")]
        float m_SwitchDistance = 25f;

        [SerializeField]
        [Min(0.2f)]
        [Tooltip("Distance from the camera, in metres, where the billboard disappears, for a 60 degree field of " +
                 "view. Zooming in pushes it further away.")]
        float m_CullDistance = 300f;

        [SerializeField]
        [HideInInspector]
        HBStyleBillboardDraw[] m_Draws = Array.Empty<HBStyleBillboardDraw>();

        [SerializeField]
        [HideInInspector]
        Mesh m_BillboardMesh;

        [SerializeField]
        [HideInInspector]
        Material m_BillboardMaterial;

        [SerializeField]
        [HideInInspector]
        Vector4 m_Sphere;

        public float switchDistance
        {
            get => m_SwitchDistance;
            set => m_SwitchDistance = Mathf.Max(value, 0.1f);
        }

        public float cullDistance
        {
            get => Mathf.Max(m_CullDistance, m_SwitchDistance + 0.1f);
            set => m_CullDistance = Mathf.Max(value, 0.2f);
        }

        public HBStyleBillboardDraw[] draws => m_Draws;
        public Mesh billboardMesh => m_BillboardMesh;
        public Material billboardMaterial => m_BillboardMaterial;
        public Vector4 sphere => m_Sphere;

        public bool valid => m_Draws != null && m_Draws.Length > 0 && m_BillboardMesh != null &&
                             m_BillboardMaterial != null;

        public void Set(HBStyleBillboardDraw[] draws, Mesh billboardMesh, Material billboardMaterial, Vector4 sphere)
        {
            m_Draws = draws ?? Array.Empty<HBStyleBillboardDraw>();
            m_BillboardMesh = billboardMesh;
            m_BillboardMaterial = billboardMaterial;
            m_Sphere = sphere;
            StyleBillboardSystem.Rebuild(this);
        }

        void OnValidate()
        {
            StyleBillboardSystem.Rebuild(this);
        }
    }
}
