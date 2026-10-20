using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleTerrainLayerSync
    {
        static readonly HashSet<HBStyleTerrainProfile> s_Edited = new HashSet<HBStyleTerrainProfile>();
        static readonly HashSet<TerrainLayer> s_EditedLayers = new HashSet<TerrainLayer>();
        static readonly HashSet<HBStyleTerrain> s_Stale = new HashSet<HBStyleTerrain>();

        const double SettleTime = 0.5;

        static double s_LastLayerEdit;

        static StyleTerrainLayerSync()
        {
            EditorApplication.update += Update;
            HBStyleTerrainProfile.edited += profile => s_Edited.Add(profile);
            ObjectChangeEvents.changesPublished += OnChanges;
        }

        static void Update()
        {
            var owners = HBStyleTerrain.active;

            if (s_Edited.Count > 0)
                Push(owners);

            if (s_EditedLayers.Count > 0)
                Rebind(owners);

            if (s_Stale.Count > 0 && EditorApplication.timeSinceStartup - s_LastLayerEdit > SettleTime)
                Rebuild();

            for (var index = owners.Count - 1; index >= 0; index--)
            {
                var owner = owners[index];
                var terrain = owner != null ? owner.GetComponent<Terrain>() : null;
                var data = terrain != null ? terrain.terrainData : null;

                if (data == null)
                    continue;

                var current = data.terrainLayers;

                if (owner.syncedLayers == null)
                {
                    owner.syncedLayers = current;
                    continue;
                }

                if (Same(current, owner.syncedLayers))
                    continue;

                owner.syncedLayers = current;
                Adopt(owner, terrain, current);
            }
        }

        internal static void LayerEdited(TerrainLayer layer)
        {
            s_EditedLayers.Add(layer);
            s_LastLayerEdit = EditorApplication.timeSinceStartup;
        }

        static void OnChanges(ref ObjectChangeEventStream stream)
        {
            for (var index = 0; index < stream.length; index++)
            {
                if (stream.GetEventType(index) != ObjectChangeKind.ChangeAssetObjectProperties)
                    continue;

                stream.GetChangeAssetObjectPropertiesEvent(index, out var change);

                if (EditorUtility.EntityIdToObject(change.entityId) is TerrainLayer layer)
                    LayerEdited(layer);
            }
        }

        static void Rebind(IReadOnlyList<HBStyleTerrain> owners)
        {
            var edited = new List<TerrainLayer>(s_EditedLayers);
            s_EditedLayers.Clear();

            for (var index = owners.Count - 1; index >= 0; index--)
            {
                var owner = owners[index];
                var terrain = owner != null ? owner.GetComponent<Terrain>() : null;
                var data = terrain != null ? terrain.terrainData : null;

                if (data == null || !System.Array.Exists(data.terrainLayers, edited.Contains))
                    continue;

                if (StyleTerrainSetup.UsesLayerShader(terrain))
                    s_Stale.Add(owner);

                owner.Bind();
            }
        }

        static void Rebuild()
        {
            var stale = new List<HBStyleTerrain>(s_Stale);
            s_Stale.Clear();

            foreach (var owner in stale)
            {
                if (owner != null && owner.isActiveAndEnabled)
                    StyleTerrainArrays.Ensure(owner);
            }
        }

        static void Push(IReadOnlyList<HBStyleTerrain> owners)
        {
            var edited = new List<HBStyleTerrainProfile>(s_Edited);
            s_Edited.Clear();

            for (var index = owners.Count - 1; index >= 0; index--)
            {
                var owner = owners[index];

                if (owner == null || owner.profile == null || !edited.Contains(owner.profile))
                    continue;

                var terrain = owner.GetComponent<Terrain>();

                if (StyleTerrainBaker.SyncLayers(owner, false) && StyleTerrainSetup.UsesLayerShader(terrain))
                    StyleTerrainArrays.Ensure(owner);

                owner.Bind();
                StyleTerrainPreview.MarkDirty(owner);
            }
        }

        static void Adopt(HBStyleTerrain owner, Terrain terrain, TerrainLayer[] current)
        {
            var profile = owner.profile;

            if (profile == null)
                return;

            var rows = new List<HBStyleTerrainProfileLayer>();

            foreach (var layer in current)
            {
                if (layer == null || rows.Exists(row => row.layer == layer))
                    continue;

                var kept = profile.layers.Find(row => row != null && row.layer == layer);
                rows.Add(kept ?? new HBStyleTerrainProfileLayer { layer = layer, automatic = false });
            }

            foreach (var row in profile.layers)
            {
                if (row != null && row.layer == null)
                    rows.Add(row);
            }

            if (Same(rows, profile.layers))
                return;

            profile.layers = rows;
            EditorUtility.SetDirty(profile);
            s_Edited.Add(profile);

            if (StyleTerrainSetup.UsesLayerShader(terrain))
                StyleTerrainArrays.Ensure(owner);

            owner.Refresh();
            StyleTerrainPreview.MarkDirty(owner, true);

            foreach (var window in Resources.FindObjectsOfTypeAll<StyleTerrainWindow>())
                window.Repaint();
        }

        static bool Same(TerrainLayer[] a, TerrainLayer[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (var index = 0; index < a.Length; index++)
            {
                if (a[index] != b[index])
                    return false;
            }

            return true;
        }

        static bool Same(List<HBStyleTerrainProfileLayer> a, List<HBStyleTerrainProfileLayer> b)
        {
            if (a.Count != b.Count)
                return false;

            for (var index = 0; index < a.Count; index++)
            {
                if (a[index] != b[index])
                    return false;
            }

            return true;
        }
    }
}
