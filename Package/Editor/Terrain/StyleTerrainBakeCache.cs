using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [PreferBinarySerialization]
    public sealed class StyleTerrainBakeCache : ScriptableObject
    {
        const string Suffix = " HB Bake.asset";

        [Serializable]
        public sealed class Channel
        {
            public TerrainLayer layer;
            public byte[] written;
            public byte[] hand;
        }

        [SerializeField]
        [HideInInspector]
        int m_Resolution;

        [SerializeField]
        [HideInInspector]
        List<Channel> m_Channels = new List<Channel>();

        public static string PathFor(TerrainData data)
        {
            var path = data != null ? AssetDatabase.GetAssetPath(data) : null;

            if (string.IsNullOrEmpty(path))
                return null;

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            return $"{folder}/{Path.GetFileNameWithoutExtension(path)}{Suffix}";
        }

        public static StyleTerrainBakeCache Load(TerrainData data)
        {
            var path = PathFor(data);
            return path != null ? AssetDatabase.LoadAssetAtPath<StyleTerrainBakeCache>(path) : null;
        }

        public static StyleTerrainBakeCache LoadOrCreate(TerrainData data)
        {
            var cache = Load(data);

            if (cache != null)
                return cache;

            var path = PathFor(data);

            if (path == null)
                return null;

            cache = CreateInstance<StyleTerrainBakeCache>();
            AssetDatabase.CreateAsset(cache, path);
            return cache;
        }

        public bool Matches(int resolution)
        {
            return m_Resolution == resolution && m_Channels.Count > 0;
        }

        public void Prepare(int resolution, IList<TerrainLayer> layers)
        {
            if (m_Resolution != resolution)
            {
                m_Resolution = resolution;
                m_Channels.Clear();
            }

            var kept = new List<Channel>(layers.Count);

            foreach (var layer in layers)
            {
                var channel = m_Channels.Find(entry => entry.layer == layer);

                if (channel == null || !Fits(channel, resolution))
                {
                    channel = new Channel
                    {
                        layer = layer,
                        written = new byte[resolution * resolution],
                        hand = new byte[resolution * resolution]
                    };
                }

                kept.Add(channel);
            }

            m_Channels = kept;
        }

        public Channel Find(TerrainLayer layer)
        {
            return m_Channels.Find(entry => entry.layer == layer);
        }

        static bool Fits(Channel channel, int resolution)
        {
            var size = resolution * resolution;
            return channel.written != null && channel.written.Length == size &&
                   channel.hand != null && channel.hand.Length == size;
        }
    }
}
