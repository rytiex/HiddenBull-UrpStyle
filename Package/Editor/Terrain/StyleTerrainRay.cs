using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    static class StyleTerrainRay
    {
        const float RayLength = 100000f;
        const int MarchLimit = 20000;
        const int Refine = 12;

        internal static bool Hit(HBStyleTerrain terrain, Ray ray, out Vector3 point)
        {
            point = Vector3.zero;

            var data = terrain != null ? terrain.data : null;

            if (data == null || data.terrainData == null)
                return false;

            var origin = terrain.transform.position;

            if (terrain.TryGetComponent<TerrainCollider>(out var collider) && collider.enabled &&
                collider.Raycast(ray, out var hit, RayLength))
            {
                var size = data.size;
                var u = Mathf.Clamp01((hit.point.x - origin.x) / size.x);
                var v = Mathf.Clamp01((hit.point.z - origin.z) / size.z);

                point = new Vector3(hit.point.x, origin.y + data.terrainData.GetInterpolatedHeight(u, v), hit.point.z);
                return true;
            }

            var bounds = data.size;
            var box = new Bounds(origin + bounds * 0.5f, bounds + Vector3.one * 2f);

            if (!box.IntersectRay(ray, out var enter))
                return false;

            var step = Mathf.Max(bounds.x / (data.resolution - 1) * 0.5f, box.size.magnitude / MarchLimit);
            var end = enter + box.size.magnitude;
            var previous = Mathf.Max(enter, 0f);

            if (Below(data, origin, ray.GetPoint(previous)))
                return false;

            for (var distance = previous + step; distance <= end; distance += step)
            {
                if (!Below(data, origin, ray.GetPoint(distance)))
                {
                    previous = distance;
                    continue;
                }

                var above = previous;
                var below = distance;

                for (var i = 0; i < Refine; i++)
                {
                    var middle = (above + below) * 0.5f;

                    if (Below(data, origin, ray.GetPoint(middle)))
                        below = middle;
                    else
                        above = middle;
                }

                point = ray.GetPoint(below);
                return true;
            }

            return false;
        }

        internal static void RefreshCollider(HBStyleTerrain terrain)
        {
            if (terrain == null || !terrain.TryGetComponent<TerrainCollider>(out var collider))
                return;

            var data = collider.terrainData;

            collider.terrainData = null;
            collider.terrainData = data;
        }

        static bool Below(HBStyleTerrainData data, Vector3 origin, Vector3 point)
        {
            var size = data.size;
            var u = (point.x - origin.x) / size.x;
            var v = (point.z - origin.z) / size.z;

            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return false;

            return point.y <= origin.y + data.terrainData.GetInterpolatedHeight(u, v);
        }
    }
}
