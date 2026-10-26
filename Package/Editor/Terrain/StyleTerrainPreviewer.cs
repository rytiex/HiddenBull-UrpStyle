using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleTerrainPreviewer
    {
        const double Interval = 0.1;
        const double TexturePause = 0.5;

        sealed class Entry
        {
            internal int users;
            internal RenderTexture target;
            internal string signature;
            internal string textures;
            internal double texturesSince;
        }

        static readonly Dictionary<HBStyleTerrainData, Entry> s_Entries = new Dictionary<HBStyleTerrainData, Entry>();
        static readonly List<HBStyleTerrainData> s_Stale = new List<HBStyleTerrainData>();
        static readonly HBStyleTerrainLayer[] s_NoLayers = new HBStyleTerrainLayer[0];

        static double s_LastCheck;

        static StyleTerrainPreviewer()
        {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseAll;
        }

        internal static bool IsPreviewing(HBStyleTerrainData data)
        {
            return data != null && s_Entries.ContainsKey(data);
        }

        internal static void Begin(HBStyleTerrainData data)
        {
            if (data == null)
                return;

            if (!s_Entries.TryGetValue(data, out var entry))
            {
                entry = new Entry();
                s_Entries.Add(data, entry);
            }

            entry.users++;
            Refresh(data, entry);
        }

        internal static void End(HBStyleTerrainData data)
        {
            if (data == null || !s_Entries.TryGetValue(data, out var entry) || --entry.users > 0)
                return;

            Release(data, entry);
            s_Entries.Remove(data);
            SceneView.RepaintAll();
        }

        internal static void Restart(HBStyleTerrainData data)
        {
            if (data == null || !s_Entries.TryGetValue(data, out var entry))
                return;

            entry.signature = null;
            Refresh(data, entry);
        }

        static void Update()
        {
            var now = EditorApplication.timeSinceStartup;

            if (s_Entries.Count == 0 || now - s_LastCheck < Interval)
                return;

            s_LastCheck = now;
            s_Stale.Clear();

            foreach (var pair in s_Entries)
            {
                if (pair.Key == null)
                    s_Stale.Add(pair.Key);
                else
                    Refresh(pair.Key, pair.Value);
            }

            foreach (var data in s_Stale)
            {
                Release(data, s_Entries[data]);
                s_Entries.Remove(data);
            }
        }

        static void Refresh(HBStyleTerrainData data, Entry entry)
        {
            var textures = StyleTerrainArrays.Signature(data);

            if (data.terrainData != null && textures != data.textureSignature)
            {
                var now = EditorApplication.timeSinceStartup;

                if (entry.textures != textures)
                {
                    entry.textures = textures;
                    entry.texturesSince = now;
                }
                else if (now - entry.texturesSince >= TexturePause)
                {
                    StyleTerrainArrays.Build(data);
                    entry.signature = null;
                }
            }

            var signature = StyleTerrainBaker.Signature(data);

            if (signature == entry.signature)
                return;

            entry.signature = signature;

            if (data.terrainData == null || data.layerCount == 0)
            {
                StyleTerrains.SetPreview(data, null, s_NoLayers);
            }
            else
            {
                if (!StyleTerrainBaker.Fits(entry.target, data))
                {
                    ReleaseTarget(entry);
                    entry.target = StyleTerrainBaker.NewTarget(data);
                }

                if (StyleTerrainBaker.Dispatch(data, entry.target))
                    StyleTerrains.SetPreview(data, entry.target, StyleTerrainBaker.Snapshot(data));
            }

            SceneView.RepaintAll();
        }

        static void Release(HBStyleTerrainData data, Entry entry)
        {
            StyleTerrains.ClearPreview(data);
            ReleaseTarget(entry);
        }

        static void ReleaseTarget(Entry entry)
        {
            if (entry.target == null)
                return;

            entry.target.Release();
            Object.DestroyImmediate(entry.target);
            entry.target = null;
        }

        static void ReleaseAll()
        {
            foreach (var pair in s_Entries)
                Release(pair.Key, pair.Value);

            s_Entries.Clear();
        }
    }
}
