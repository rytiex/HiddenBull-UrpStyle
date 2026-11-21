using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [Serializable]
    sealed class StyleTerrainMap
    {
        const int Flat = 0;

        [SerializeField]
        string m_Name;

        [SerializeField]
        int m_Width;

        [SerializeField]
        int m_Height;

        [SerializeField]
        int m_Depth;

        [SerializeField]
        TextureFormat m_Format;

        [SerializeField]
        TextureWrapMode m_Wrap;

        [SerializeField]
        FilterMode m_Filter;

        [SerializeField]
        int m_Length;

        [SerializeField]
        byte[] m_Packed;

        [NonSerialized]
        internal bool stale;

        internal bool empty => m_Packed == null || m_Packed.Length == 0 || m_Length <= 0;

        internal void Clear()
        {
            m_Packed = null;
            m_Length = 0;
            stale = false;
        }

        internal void Pack(Texture texture)
        {
            if (texture == null)
            {
                Clear();
                return;
            }

            var slices = Slices(texture);
            var length = 0;

            using (var memory = new MemoryStream())
            {
                using (var stream = new DeflateStream(memory, System.IO.Compression.CompressionLevel.Optimal, true))
                {
                    foreach (var slice in slices)
                    {
                        stream.Write(slice, 0, slice.Length);
                        length += slice.Length;
                    }
                }

                m_Packed = memory.ToArray();
            }

            m_Name = texture.name;
            m_Width = texture.width;
            m_Height = texture.height;
            m_Depth = texture is Texture2DArray array ? array.depth : Flat;
            m_Format = texture is Texture2DArray layered ? layered.format : ((Texture2D)texture).format;
            m_Wrap = texture.wrapMode;
            m_Filter = texture.filterMode;
            m_Length = length;
            stale = false;
        }

        internal bool Matches(Texture texture)
        {
            if (empty || texture == null)
                return false;

            var raw = Raw();
            var offset = 0;

            foreach (var slice in Slices(texture))
            {
                if (offset + slice.Length > raw.Length)
                    return false;

                for (var index = 0; index < slice.Length; index++)
                {
                    if (raw[offset + index] != slice[index])
                        return false;
                }

                offset += slice.Length;
            }

            return offset == raw.Length;
        }

        internal Texture Unpack()
        {
            if (empty)
                return null;

            var raw = Raw();
            Texture texture;

            if (m_Depth == Flat)
            {
                var flat = new Texture2D(m_Width, m_Height, m_Format, false, true);

                flat.SetPixelData(raw, 0);
                flat.Apply(false, false);
                texture = flat;
            }
            else
            {
                var array = new Texture2DArray(m_Width, m_Height, m_Depth, m_Format, false, true);
                var size = raw.Length / m_Depth;

                for (var slice = 0; slice < m_Depth; slice++)
                    array.SetPixelData(raw, 0, slice, slice * size);

                array.Apply(false, false);
                texture = array;
            }

            texture.name = m_Name;
            texture.wrapMode = m_Wrap;
            texture.filterMode = m_Filter;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return texture;
        }

        byte[] Raw()
        {
            var raw = new byte[m_Length];
            var read = 0;

            using (var stream = new DeflateStream(new MemoryStream(m_Packed), CompressionMode.Decompress))
            {
                while (read < raw.Length)
                {
                    var count = stream.Read(raw, read, raw.Length - read);

                    if (count == 0)
                        break;

                    read += count;
                }
            }

            if (read != raw.Length)
                throw new InvalidDataException($"HB Style Terrain: the map {m_Name} is damaged.");

            return raw;
        }

        static byte[][] Slices(Texture texture)
        {
            if (texture is Texture2DArray array)
            {
                var slices = new byte[array.depth][];

                for (var slice = 0; slice < slices.Length; slice++)
                    slices[slice] = array.GetPixelData<byte>(0, slice).ToArray();

                return slices;
            }

            return new[] { ((Texture2D)texture).GetPixelData<byte>(0).ToArray() };
        }
    }
}
