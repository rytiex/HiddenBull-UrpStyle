using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    public static class StyleTerrainPreview
    {
        sealed class State
        {
            public RenderTexture result;
            public RenderTexture hand;
            public Texture2DArray written;
            public Texture2DArray held;
            public bool known;
            public bool dirty = true;
            public bool upload = true;
        }

        static readonly Dictionary<HBStyleTerrain, State> s_States = new Dictionary<HBStyleTerrain, State>();

        static StyleTerrainPreview()
        {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += StopAll;
            Undo.undoRedoPerformed += OnUndo;
        }

        public static bool IsOn(HBStyleTerrain owner)
        {
            return owner != null && s_States.ContainsKey(owner);
        }

        public static void Set(HBStyleTerrain owner, bool on)
        {
            if (owner == null || on == IsOn(owner))
                return;

            if (on)
            {
                s_States[owner] = new State();
                return;
            }

            Stop(owner);
        }

        public static void MarkDirty(HBStyleTerrain owner, bool reloadCache = false)
        {
            if (owner == null || !s_States.TryGetValue(owner, out var state))
                return;

            state.dirty = true;
            state.upload |= reloadCache;
        }

        static void OnUndo()
        {
            foreach (var pair in s_States)
            {
                var owner = pair.Key;

                if (owner == null)
                    continue;

                if (StyleTerrainSetup.UsesLayerShader(owner.GetComponent<Terrain>()))
                    StyleTerrainArrays.Ensure(owner);

                owner.Refresh();
                pair.Value.dirty = true;
                pair.Value.upload = true;
            }
        }

        static void StopAll()
        {
            foreach (var owner in new List<HBStyleTerrain>(s_States.Keys))
                Stop(owner);
        }

        static void Stop(HBStyleTerrain owner)
        {
            if (!s_States.TryGetValue(owner, out var state))
                return;

            s_States.Remove(owner);

            if (owner != null)
                owner.SetPreview(null);

            Release(state);
        }

        static void Release(State state)
        {
            StyleTerrainGpu.Release(state.result);
            StyleTerrainGpu.Release(state.hand);
            StyleTerrainGpu.Release(state.written);
            StyleTerrainGpu.Release(state.held);
            state.result = null;
            state.hand = null;
            state.written = null;
            state.held = null;
        }

        static void Update()
        {
            if (s_States.Count == 0)
                return;

            foreach (var owner in new List<HBStyleTerrain>(s_States.Keys))
            {
                if (owner == null || !owner.isActiveAndEnabled)
                {
                    Stop(owner);
                    continue;
                }

                var state = s_States[owner];
                if (state.dirty)
                    Run(owner, state);
            }
        }

        static void Run(HBStyleTerrain owner, State state)
        {
            state.dirty = false;

            var terrain = owner.GetComponent<Terrain>();
            var data = terrain != null ? terrain.terrainData : null;
            var rows = StyleTerrainBaker.Rows(owner.profile);

            if (data == null || rows.Count == 0 || !StyleTerrainGpu.supported || owner.control == null)
                return;

            var layers = data.terrainLayers;
            var resolution = data.alphamapResolution;
            var slices = (layers.Length + 3) / 4;

            if (state.result == null || state.result.width != resolution || state.result.volumeDepth != slices)
            {
                Release(state);
                state.result = StyleTerrainGpu.NewTarget(resolution, slices);
                state.hand = StyleTerrainGpu.NewTarget(resolution, slices);
                state.upload = true;
            }

            if (state.upload)
                Upload(data, rows, layers, resolution, state);

            StyleTerrainGpu.Run(terrain, owner.control, rows, layers.Length, new RectInt(0, 0, resolution, resolution),
                                state.written, state.held, state.known, state.result, state.hand);

            owner.SetPreview(state.result);
        }

        static void Upload(TerrainData data, List<HBStyleTerrainProfileLayer> rows, TerrainLayer[] layers, int resolution,
                           State state)
        {
            state.upload = false;

            StyleTerrainGpu.Release(state.written);
            StyleTerrainGpu.Release(state.held);
            state.written = null;
            state.held = null;

            var cache = StyleTerrainBakeCache.Load(data);
            state.known = true;

            if (cache == null || !cache.Matches(resolution))
            {
                state.written = StyleTerrainGpu.Upload(StyleTerrainBaker.Adopted(data, rows, layers), resolution);
                return;
            }

            var written = new List<byte[]>(layers.Length);
            var held = new List<byte[]>(layers.Length);

            foreach (var layer in layers)
            {
                var channel = cache.Find(layer);
                written.Add(channel?.written);
                held.Add(channel?.hand);
            }

            state.written = StyleTerrainGpu.Upload(written, resolution);
            state.held = StyleTerrainGpu.Upload(held, resolution);
        }
    }
}
