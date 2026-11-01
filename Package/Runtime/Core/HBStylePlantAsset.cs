using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    public struct HBStylePlantDraw
    {
        public Mesh mesh;
        public int submesh;
        public Material material;
        public Matrix4x4 local;
        public ShadowCastingMode shadows;
    }

    public sealed class HBStylePlantAsset : ScriptableObject
    {
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Distance from the camera, in metres, where the plant turns into its billboard, for a 60 degree " +
                 "field of view. Zooming in (a smaller field of view) pushes it further away.")]
        float m_SwitchDistance = 25f;

        [SerializeField]
        [Min(0.2f)]
        [Tooltip("Distance from the camera, in metres, where the plant disappears, for a 60 degree field of view. " +
                 "Zooming in pushes it further away.")]
        float m_CullDistance = 300f;

        [SerializeField]
        [HideInInspector]
        HBStylePlantDraw[] m_Draws = Array.Empty<HBStylePlantDraw>();

        [SerializeField]
        [HideInInspector]
        Mesh m_BillboardMesh;

        [SerializeField]
        [HideInInspector]
        Material m_BillboardMaterial;

        [SerializeField]
        [HideInInspector]
        Vector4 m_Sphere;

        public float switchDistance => billboarded ? m_SwitchDistance : cullDistance;
        public float cullDistance => Mathf.Max(m_CullDistance, m_SwitchDistance + 0.1f);

        public HBStylePlantDraw[] draws => m_Draws;
        public Mesh billboardMesh => m_BillboardMesh;
        public Material billboardMaterial => m_BillboardMaterial;
        public Vector4 sphere => m_Sphere;

        public bool billboarded => m_BillboardMesh != null && m_BillboardMaterial != null;
        public bool valid => m_Draws != null && m_Draws.Length > 0;

        public void Set(HBStylePlantDraw[] draws, Mesh billboardMesh, Material billboardMaterial, Vector4 sphere)
        {
            m_Draws = draws ?? Array.Empty<HBStylePlantDraw>();
            m_BillboardMesh = billboardMesh;
            m_BillboardMaterial = billboardMaterial;
            m_Sphere = sphere;
            StylePlantSystem.Rebuild(this);
        }

        void OnValidate()
        {
            StylePlantSystem.Rebuild(this);
        }
    }
}
