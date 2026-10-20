using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleTerrainAutoBake
    {
        const double Pause = 0.3;

        static readonly Dictionary<HBStyleTerrain, RectInt?> s_Pending = new Dictionary<HBStyleTerrain, RectInt?>();
        static double s_Last;

        public static int heightsVersion { get; private set; }

        static StyleTerrainAutoBake()
        {
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
            TerrainCallbacks.textureChanged += OnTextureChanged;
            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        static void OnUndoRedo()
        {
            EditorApplication.delayCall -= RefreshAll;
            EditorApplication.delayCall += RefreshAll;
        }

        static void RefreshAll()
        {
            foreach (var owner in HBStyleTerrain.active)
            {
                if (owner != null && !StyleTerrainPreview.IsOn(owner))
                    owner.Refresh();
            }
        }

        public static void Request(HBStyleTerrain owner)
        {
            if (owner == null)
                return;

            if (StyleTerrainPreview.IsOn(owner))
            {
                StyleTerrainPreview.MarkDirty(owner);
                return;
            }

            if (!owner.autoBake || !StyleTerrainBaker.HasBake(owner))
                return;

            s_Pending[owner] = null;
            s_Last = EditorApplication.timeSinceStartup;
        }

        static void OnTextureChanged(Terrain terrain, string name, RectInt texels, bool synched)
        {
            if (name != TerrainData.AlphamapTextureName || terrain == null ||
                !terrain.TryGetComponent(out HBStyleTerrain owner))
            {
                return;
            }

            owner.Refresh();
            StyleTerrainPreview.MarkDirty(owner, !StyleTerrainBaker.HasBake(owner));
        }

        static void OnHeightmapChanged(Terrain terrain, RectInt heights, bool synched)
        {
            heightsVersion++;

            if (terrain == null || !terrain.TryGetComponent(out HBStyleTerrain owner))
                return;

            if (StyleTerrainPreview.IsOn(owner))
            {
                StyleTerrainPreview.MarkDirty(owner);
                return;
            }

            if (!owner.autoBake || owner.profile == null || !StyleTerrainBaker.HasBake(owner))
                return;

            var area = StyleTerrainBaker.AlphamapRect(terrain.terrainData, heights);

            if (s_Pending.TryGetValue(owner, out var pending))
                s_Pending[owner] = pending.HasValue ? Union(pending.Value, area) : (RectInt?)null;
            else
                s_Pending[owner] = area;

            s_Last = EditorApplication.timeSinceStartup;
        }

        static void Update()
        {
            if (s_Pending.Count == 0 || EditorApplication.timeSinceStartup - s_Last < Pause)
                return;

            var pending = new List<KeyValuePair<HBStyleTerrain, RectInt?>>(s_Pending);
            s_Pending.Clear();

            foreach (var entry in pending)
            {
                if (entry.Key != null && !StyleTerrainPreview.IsOn(entry.Key))
                    StyleTerrainBaker.Bake(entry.Key, entry.Value, true, true);
            }
        }

        static RectInt Union(RectInt a, RectInt b)
        {
            var xMin = Mathf.Min(a.xMin, b.xMin);
            var yMin = Mathf.Min(a.yMin, b.yMin);
            var xMax = Mathf.Max(a.xMax, b.xMax);
            var yMax = Mathf.Max(a.yMax, b.yMax);

            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
