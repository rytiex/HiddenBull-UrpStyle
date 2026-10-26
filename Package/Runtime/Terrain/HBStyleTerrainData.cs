using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Terrain Data", order = 0,
                     menuName = "HiddenBull/URP Style/Terrain/Terrain Data")]
    public sealed class HBStyleTerrainData : ScriptableObject
    {
        public const int MaxLayers = HBStyleTerrainLayerSet.MaxLayers;

        static readonly HBStyleTerrainLayerRule[] s_NoLayers = new HBStyleTerrainLayerRule[0];

        [SerializeField]
        [Tooltip("The ground layers and their rules. One set can serve many terrains; copy it to vary it.")]
        HBStyleTerrainLayerSet m_LayerSet;

        [SerializeField]
        [Tooltip("Size of the maps that hold where each layer lies. 2048 over a 4 km terrain is one value every " +
                 "2 m; the textures' heights sharpen the borders between those values.")]
        int m_ControlResolution = 2048;

        [SerializeField]
        [Tooltip("Size every layer texture is resampled to for the terrain shader.")]
        int m_TextureResolution = 1024;

        [SerializeField]
        HBStyleTerrainLook m_Look = new HBStyleTerrainLook();

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Control;

        [SerializeField]
        [HideInInspector]
        HBStyleTerrainLayer[] m_BakedLayers;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Albedo;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Normal;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Mask;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_Hand;

        [SerializeField]
        [HideInInspector]
        HBStyleTerrainLayer[] m_HandLayers;

        [SerializeField]
        [HideInInspector]
        int m_HandVersion;

        [SerializeField]
        [HideInInspector]
        string m_TextureSignature;

        [SerializeField]
        [HideInInspector]
        string m_BakeSignature;

        [SerializeField]
        [HideInInspector]
        TerrainData m_Terrain;

        [SerializeField]
        [HideInInspector]
        float[] m_PatchMin;

        [SerializeField]
        [HideInInspector]
        float[] m_PatchMax;

        [SerializeField]
        [HideInInspector]
        int m_Patches;

        [SerializeField]
        [HideInInspector]
        int m_Version;

        public TerrainData terrainData => m_Terrain;

        public Vector3 size => m_Terrain != null ? m_Terrain.size : Vector3.zero;

        public int resolution => m_Terrain != null ? m_Terrain.heightmapResolution : 0;

        internal int patches => m_Patches;

        internal float[] patchMin => m_PatchMin;

        internal float[] patchMax => m_PatchMax;

        internal int version => m_Version;

        internal bool valid => m_Terrain != null && m_Patches > 0 && m_PatchMin != null && m_PatchMax != null &&
                               m_PatchMin.Length == m_Patches * m_Patches && m_PatchMax.Length == m_PatchMin.Length &&
                               Mathf.Approximately(m_Terrain.size.x, m_Terrain.size.z);

        public HBStyleTerrainLayerSet layerSet
        {
            get => m_LayerSet;
            set => m_LayerSet = value;
        }

        public IReadOnlyList<HBStyleTerrainLayerRule> layers => m_LayerSet != null ? m_LayerSet.layers : s_NoLayers;

        public HBStyleTerrainLook look => m_Look ??= new HBStyleTerrainLook();

        public int controlResolution => m_ControlResolution;

        public int textureResolution => m_TextureResolution;

        internal int layerCount => m_LayerSet != null ? m_LayerSet.count : 0;

        internal Texture2DArray control => m_Control;

        internal int controlLayers => m_BakedLayers != null ? m_BakedLayers.Length : 0;

        internal Texture2DArray albedoArray => m_Albedo;

        internal Texture2DArray normalArray => m_Normal;

        internal Texture2DArray maskArray => m_Mask;

        internal Texture2DArray hand => m_Hand;

        internal int handChannels => m_HandLayers != null ? m_HandLayers.Length : 0;

        internal HBStyleTerrainLayer[] handLayers => m_HandLayers;

        internal int handVersion => m_HandVersion;

        internal string textureSignature => m_TextureSignature;

        internal string bakeSignature
        {
            get => m_BakeSignature;
            set => m_BakeSignature = value;
        }

        internal bool painted => m_Control != null && controlLayers > 0 && m_Albedo != null && m_Normal != null &&
                                 m_Mask != null && m_Albedo.depth >= controlLayers;

        internal HBStyleTerrainLayer Layer(int index)
        {
            return index >= 0 && index < controlLayers ? m_BakedLayers[index] : null;
        }

        internal void SetControl(Texture2DArray control, HBStyleTerrainLayer[] layers)
        {
            m_Control = control;
            m_BakedLayers = layers;
        }

        internal int HandChannel(HBStyleTerrainLayer layer)
        {
            return layer != null && m_HandLayers != null ? System.Array.IndexOf(m_HandLayers, layer) : -1;
        }

        internal void SetHand(Texture2DArray hand, HBStyleTerrainLayer[] layers)
        {
            m_Hand = hand;
            m_HandLayers = layers;
            m_HandVersion++;
        }

        internal void HandEdited()
        {
            m_HandVersion++;
        }

        internal void SetTextures(Texture2DArray albedo, Texture2DArray normal, Texture2DArray mask, string signature)
        {
            m_Albedo = albedo;
            m_Normal = normal;
            m_Mask = mask;
            m_TextureSignature = signature;
        }

        internal void Assign(TerrainData terrain)
        {
            m_Terrain = terrain;
            RebuildBounds();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_ControlResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(m_ControlResolution), 512, 4096);
            m_TextureResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(m_TextureResolution), 512, 2048);
        }
#endif

        internal void UpdateBounds(int x, int z, int width, int height)
        {
            if (!valid)
            {
                RebuildBounds();
                return;
            }

            var quads = StyleTerrainTree.PatchQuads;
            var resolution = m_Terrain.heightmapResolution;
            var firstX = Mathf.Max((x - 1) / quads, 0);
            var firstZ = Mathf.Max((z - 1) / quads, 0);
            var lastX = Mathf.Min((x + width) / quads, m_Patches - 1);
            var lastZ = Mathf.Min((z + height) / quads, m_Patches - 1);
            var startX = firstX * quads;
            var startZ = firstZ * quads;
            var spanX = Mathf.Min((lastX + 1) * quads, resolution - 1) - startX + 1;
            var spanZ = Mathf.Min((lastZ + 1) * quads, resolution - 1) - startZ + 1;
            var heights = m_Terrain.GetHeights(startX, startZ, spanX, spanZ);
            var scale = m_Terrain.size.y;

            for (var pz = firstZ; pz <= lastZ; pz++)
            {
                for (var px = firstX; px <= lastX; px++)
                {
                    var low = float.MaxValue;
                    var high = float.MinValue;

                    for (var sz = pz * quads; sz <= (pz + 1) * quads; sz++)
                    {
                        for (var sx = px * quads; sx <= (px + 1) * quads; sx++)
                        {
                            var sample = heights[sz - startZ, sx - startX];

                            low = Mathf.Min(low, sample);
                            high = Mathf.Max(high, sample);
                        }
                    }

                    m_PatchMin[pz * m_Patches + px] = low * scale;
                    m_PatchMax[pz * m_Patches + px] = high * scale;
                }
            }

            m_Version++;
        }

        internal void RebuildBounds()
        {
            m_Version++;
            m_Patches = 0;
            m_PatchMin = null;
            m_PatchMax = null;

            if (m_Terrain == null)
                return;

            var resolution = m_Terrain.heightmapResolution;
            var quads = StyleTerrainTree.PatchQuads;
            var patches = (resolution - 1) / quads;

            if (patches < 1)
                return;

            var heights = m_Terrain.GetHeights(0, 0, resolution, resolution);
            var scale = m_Terrain.size.y;

            m_PatchMin = new float[patches * patches];
            m_PatchMax = new float[patches * patches];

            for (var pz = 0; pz < patches; pz++)
            {
                for (var px = 0; px < patches; px++)
                {
                    var low = float.MaxValue;
                    var high = float.MinValue;

                    for (var z = pz * quads; z <= (pz + 1) * quads; z++)
                    {
                        for (var x = px * quads; x <= (px + 1) * quads; x++)
                        {
                            var height = heights[z, x];

                            low = Mathf.Min(low, height);
                            high = Mathf.Max(high, height);
                        }
                    }

                    m_PatchMin[pz * patches + px] = low * scale;
                    m_PatchMax[pz * patches + px] = high * scale;
                }
            }

            m_Patches = patches;
        }
    }
}
