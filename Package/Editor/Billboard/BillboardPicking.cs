using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class BillboardPicking
    {
        static BillboardPicking()
        {
            HandleUtility.pickGameObjectCustomPasses += Pick;
        }

        static GameObject Pick(Camera camera, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter,
                               out int materialIndex)
        {
            materialIndex = -1;

            if (!StyleBillboardSystem.drawing)
                return null;

            var ray = HandleUtility.GUIPointToWorldRay(position);
            var nearest = float.MaxValue;
            GameObject picked = null;

            foreach (var member in StyleBillboardSystem.members)
            {
                if (member == null || member.asset == null)
                    continue;

                var target = member.gameObject;

                if ((layers & (1 << target.layer)) == 0 || Excluded(target, ignore) ||
                    (filter != null && filter.Length > 0 && !Excluded(target, filter)))
                    continue;

                var sphere = member.asset.sphere;
                var transform = member.transform;
                var scale = transform.lossyScale;
                var centre = transform.TransformPoint(sphere);
                var radius = sphere.w * Mathf.Max(Mathf.Abs(scale.x),
                                                  Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                var along = Vector3.Dot(centre - ray.origin, ray.direction);
                var gap = (ray.origin + ray.direction * along - centre).sqrMagnitude;

                if (along <= 0f || gap > radius * radius || along >= nearest)
                    continue;

                nearest = along;
                picked = target;
            }

            return picked;
        }

        static bool Excluded(GameObject target, GameObject[] list)
        {
            if (list == null)
                return false;

            foreach (var entry in list)
            {
                if (entry == target)
                    return true;
            }

            return false;
        }
    }
}
