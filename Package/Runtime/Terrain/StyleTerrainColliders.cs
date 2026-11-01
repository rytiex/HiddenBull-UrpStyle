using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleTerrainColliders
    {
        const string RootName = "HB Style Plant Colliders";

        struct Piece
        {
            internal Collider source;
            internal Matrix4x4 local;
        }

        readonly Dictionary<uint, HBStylePlantBody[]> m_Bodies = new Dictionary<uint, HBStylePlantBody[]>();
        readonly List<Piece> m_Pieces = new List<Piece>();

        GameObject m_Root;

        internal void Build(HBStyleTerrain terrain, IReadOnlyList<HBStyleTerrainPlantType> types,
                            IReadOnlyList<StylePlantSystem.Placed> placed)
        {
            Release();

            for (var type = 0; type < types.Count; type++)
            {
                var prefab = types[type].prefab;

                Read(prefab);

                if (m_Pieces.Count == 0 || placed[type].count == 0)
                    continue;

                if (m_Root == null)
                {
                    m_Root = new GameObject(RootName) { hideFlags = HideFlags.HideAndDontSave };
                    m_Root.transform.SetParent(terrain.transform, false);
                }

                for (var index = 0; index < placed[type].count; index++)
                    Add(terrain, prefab, placed[type], index);
            }
        }

        internal void Release()
        {
            CoreUtils.Destroy(m_Root);

            m_Root = null;
            m_Bodies.Clear();
        }

        internal void Show(uint id, bool shown)
        {
            if (!m_Bodies.TryGetValue(id, out var bodies))
                return;

            foreach (var body in bodies)
                body.gameObject.SetActive(shown);
        }

        void Read(GameObject prefab)
        {
            m_Pieces.Clear();

            if (prefab == null)
                return;

            var root = prefab.transform.localToWorldMatrix.inverse;

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.enabled && Supported(collider))
                    m_Pieces.Add(new Piece { source = collider, local = root * collider.transform.localToWorldMatrix });
            }
        }

        void Add(HBStyleTerrain terrain, GameObject prefab, StylePlantSystem.Placed placed, int index)
        {
            var bodies = new HBStylePlantBody[m_Pieces.Count];

            for (var slot = 0; slot < bodies.Length; slot++)
            {
                var piece = m_Pieces[slot];
                var matrix = placed.worlds[index] * piece.local;
                var owner = new GameObject(prefab.name)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    layer = piece.source.gameObject.layer
                };

                owner.SetActive(false);
                owner.transform.SetParent(m_Root.transform, false);
                owner.transform.SetPositionAndRotation(matrix.GetPosition(), matrix.rotation);
                owner.transform.localScale = matrix.lossyScale;
                Copy(piece.source, owner);

                var body = owner.AddComponent<HBStylePlantBody>();

                body.terrain = terrain;
                body.prefab = prefab;
                body.id = placed.ids[index];
                bodies[slot] = body;
                owner.SetActive(!placed.gone[index]);
            }

            m_Bodies[placed.ids[index]] = bodies;
        }

        static bool Supported(Collider collider)
        {
            return collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider ||
                   collider is MeshCollider;
        }

        static void Copy(Collider source, GameObject owner)
        {
            Collider copy;

            switch (source)
            {
                case BoxCollider box:
                    var boxCopy = owner.AddComponent<BoxCollider>();

                    boxCopy.center = box.center;
                    boxCopy.size = box.size;
                    copy = boxCopy;
                    break;

                case SphereCollider sphere:
                    var sphereCopy = owner.AddComponent<SphereCollider>();

                    sphereCopy.center = sphere.center;
                    sphereCopy.radius = sphere.radius;
                    copy = sphereCopy;
                    break;

                case CapsuleCollider capsule:
                    var capsuleCopy = owner.AddComponent<CapsuleCollider>();

                    capsuleCopy.center = capsule.center;
                    capsuleCopy.radius = capsule.radius;
                    capsuleCopy.height = capsule.height;
                    capsuleCopy.direction = capsule.direction;
                    copy = capsuleCopy;
                    break;

                default:
                    var mesh = (MeshCollider)source;
                    var meshCopy = owner.AddComponent<MeshCollider>();

                    meshCopy.cookingOptions = mesh.cookingOptions;
                    meshCopy.convex = mesh.convex;
                    meshCopy.sharedMesh = mesh.sharedMesh;
                    copy = meshCopy;
                    break;
            }

            copy.isTrigger = source.isTrigger;
            copy.sharedMaterial = source.sharedMaterial;
            copy.includeLayers = source.includeLayers;
            copy.excludeLayers = source.excludeLayers;
        }
    }
}
