using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    [CreateAssetMenu(fileName = "HB Style Terrain Data", order = 0,
                     menuName = "HiddenBull/URP Style/Terrain/Terrain Data")]
    public sealed class HBStyleTerrainData : ScriptableObject
    {
        const int GrassMarks = 64;

        public const int MaxLayers = HBStyleTerrainLayerSet.MaxLayers;

        public const int LiquidSlots = 4;
        internal const int WaterFormat = 3;

        static readonly HBStyleTerrainLayerRule[] s_NoLayers = new HBStyleTerrainLayerRule[0];

        static readonly Vector4[] s_AllSources = AllSources();

        [SerializeField]
        [Tooltip("The ground layers and their rules. One set can serve many terrains; copy it to vary it.")]
        HBStyleTerrainLayerSet m_LayerSet;

        [SerializeField]
        [Tooltip("Size of the maps that hold where each layer lies. 2048 over a 4 km terrain is one value every " +
                 "2 m; the textures' heights sharpen the borders between those values.")]
        int m_ControlResolution = 2048;

        [SerializeField]
        [Tooltip("Size the layer textures are used at. Square layer textures at least this big, with mipmaps and " +
                 "one compression format per map, are copied straight to the GPU; others are resampled and take " +
                 "about four times the memory.")]
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
        Texture2D m_Grass;

        [SerializeField]
        [HideInInspector]
        int m_GrassVersion;

        [SerializeField]
        [HideInInspector]
        Texture2DArray m_FlowerArray;

        [SerializeField]
        [HideInInspector]
        Texture2D[] m_FlowerTextures;

        [SerializeField]
        [HideInInspector]
        string m_GrassSignature;

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

        [SerializeField]
        [HideInInspector]
        HBStyleTerrainPlants m_Plants;

        [SerializeField]
        [Tooltip("The liquids of this terrain, up to four. The sea liquid fills every place below its level; the " +
                 "others are painted over it with the Liquid tools.")]
        HBStyleLiquidLayer[] m_Liquids = new HBStyleLiquidLayer[0];

        [SerializeField]
        [Tooltip("Turns the liquid of this terrain on. Off, nothing about the liquid is drawn, moved or edited; " +
                 "what was sculpted and painted is kept.")]
        bool m_LiquidOn;

        [SerializeField]
        [Tooltip("Height of the liquid wherever its level was not changed, in metres above the terrain's base. " +
                 "Put it below the ground for a terrain with only lakes and rivers.")]
        float m_SeaLevel;

        [SerializeField]
        [HideInInspector]
        float m_Sunk;

        [SerializeField]
        [Tooltip("The liquid that fills the sea and every place no other liquid was painted.")]
        int m_SeaSlot;

        [SerializeField]
        [Tooltip("Draws the sea past the terrain's edges out to the camera's far plane. Turn it off on maps where " +
                 "that far water is never seen; the liquid then ends at the terrain's edges.")]
        bool m_SeaBeyond = true;

        [SerializeField]
        [HideInInspector]
        Texture2D m_WaterLevel;

        [SerializeField]
        [HideInInspector]
        Texture2D m_WaterMix;

        [SerializeField]
        [HideInInspector]
        Texture2D m_WaterFlow;

        [SerializeField]
        [HideInInspector]
        int m_WaterFormat;

        [SerializeField]
        [HideInInspector]
        int m_WaterVersion;

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        StyleTerrainMap m_HandMap = new StyleTerrainMap();
#endif

        [SerializeField]
        [HideInInspector]
        StyleTerrainMap m_ControlMap = new StyleTerrainMap();

        [SerializeField]
        [HideInInspector]
        StyleTerrainMap m_GrassMap = new StyleTerrainMap();

        [SerializeField]
        [HideInInspector]
        StyleTerrainMap m_LevelMap = new StyleTerrainMap();

        [SerializeField]
        [HideInInspector]
        StyleTerrainMap m_MixMap = new StyleTerrainMap();

        [SerializeField]
        [HideInInspector]
        StyleTerrainMap m_FlowMap = new StyleTerrainMap();

#if UNITY_EDITOR
        [System.NonSerialized]
        Texture2DArray m_HandLive;
#endif

        [System.NonSerialized]
        Texture2DArray m_ControlLive;

        [System.NonSerialized]
        Texture2D m_GrassLive;

        [System.NonSerialized]
        Texture2D m_LevelLive;

        [System.NonSerialized]
        Texture2D m_MixLive;

        [System.NonSerialized]
        Texture2D m_FlowLive;

        [System.NonSerialized]
        StyleTerrainLayerArrays m_Arrays;

        [System.NonSerialized]
        RectInt m_WaterShown;

        [System.NonSerialized]
        bool m_WaterShownAny;

        [System.NonSerialized]
        int m_WaterStamp;

        [System.NonSerialized]
        RectInt m_HolesEdited;

        [System.NonSerialized]
        bool m_HolesEditedAny;

        public TerrainData terrainData => m_Terrain;

        public Vector3 size => m_Terrain != null ? m_Terrain.size : Vector3.zero;

        public int resolution => m_Terrain != null ? m_Terrain.heightmapResolution : 0;

        internal int patches => m_Patches;

        internal float[] patchMin => m_PatchMin;

        internal float[] patchMax => m_PatchMax;

        internal int version => m_Version;

        internal HBStyleTerrainPlants plants => m_Plants;

        public float seaLevel => m_SeaLevel;

        internal float sunk => m_Sunk;

        internal void Sink(float depth)
        {
            m_SeaLevel += depth;
            m_Sunk += depth;
        }

        public int liquidCount => m_Liquids != null ? Mathf.Min(m_Liquids.Length, LiquidSlots) : 0;

        public bool liquidOn => m_LiquidOn && liquidCount > 0;

        public bool seaBeyond
        {
            get => m_SeaBeyond;
            set => m_SeaBeyond = value;
        }

        public int seaSlot =>Mathf.Clamp(m_SeaSlot, 0, Mathf.Max(liquidCount - 1, 0));

        internal Texture2D waterLevel => Live(ref m_LevelLive, m_LevelMap, m_WaterLevel);

        internal Texture2D waterMix => Live(ref m_MixLive, m_MixMap, m_WaterMix);

        internal Texture2D waterFlow => Live(ref m_FlowLive, m_FlowMap, m_WaterFlow);

        internal int waterFormat => m_WaterFormat;

        internal int waterVersion => m_WaterVersion;

        public HBStyleLiquidLayer Liquid(int slot)
        {
            return m_Liquids != null && slot >= 0 && slot < m_Liquids.Length ? m_Liquids[slot] : null;
        }

        internal void SetWater(Texture2D level, Texture2D mix, Texture2D flow, int format)
        {
            m_LevelLive = Keep(level);
            m_MixLive = Keep(mix);
            m_FlowLive = Keep(flow);
            WaterTouched();
            m_WaterFormat = format;
            m_WaterVersion++;
        }

        internal void WaterEdited()
        {
            m_WaterVersion++;
            WaterTouched();
        }

        internal void WaterShown(RectInt area)
        {
            m_WaterStamp++;
            WaterTouched();

            if (m_WaterShownAny)
            {
                var xMin = Mathf.Min(m_WaterShown.xMin, area.xMin);
                var yMin = Mathf.Min(m_WaterShown.yMin, area.yMin);
                var xMax = Mathf.Max(m_WaterShown.xMax, area.xMax);
                var yMax = Mathf.Max(m_WaterShown.yMax, area.yMax);

                area = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            }

            m_WaterShown = area;
            m_WaterShownAny = true;
        }

        internal int waterStamp => m_WaterStamp;

        internal void FlowEdited()
        {
            m_WaterStamp++;
            WaterTouched();
        }

        internal bool TakeWaterShown(out RectInt area)
        {
            area = m_WaterShown;

            var any = m_WaterShownAny;

            m_WaterShownAny = false;
            return any;
        }

        internal void HolesEdited(RectInt cells)
        {
            if (m_HolesEditedAny)
            {
                var xMin = Mathf.Min(m_HolesEdited.xMin, cells.xMin);
                var yMin = Mathf.Min(m_HolesEdited.yMin, cells.yMin);
                var xMax = Mathf.Max(m_HolesEdited.xMax, cells.xMax);
                var yMax = Mathf.Max(m_HolesEdited.yMax, cells.yMax);

                cells = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            }

            m_HolesEdited = cells;
            m_HolesEditedAny = true;
        }

        internal bool TakeHolesEdited(out RectInt cells)
        {
            cells = m_HolesEdited;

            var any = m_HolesEditedAny;

            m_HolesEditedAny = false;
            return any;
        }

        internal void AddLiquid(HBStyleLiquidLayer liquid)
        {
            if (liquidCount >= LiquidSlots)
                return;

            var count = liquidCount;

            System.Array.Resize(ref m_Liquids, count + 1);
            m_Liquids[count] = liquid;
        }

        internal void RemoveLiquid(int slot)
        {
            var count = liquidCount;

            if (slot < 0 || slot >= count)
                return;

            var kept = new HBStyleLiquidLayer[count - 1];

            for (int from = 0, to = 0; from < count; from++)
            {
                if (from != slot)
                    kept[to++] = m_Liquids[from];
            }

            m_Liquids = kept;

            if (m_SeaSlot == slot)
                m_SeaSlot = 0;
            else if (m_SeaSlot > slot)
                m_SeaSlot--;
        }

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

        internal Texture2DArray control => Live(ref m_ControlLive, m_ControlMap, m_Control);

        internal int controlLayers => m_BakedLayers != null ? m_BakedLayers.Length : 0;

        internal Texture albedoArray => m_Albedo != null ? m_Albedo : m_Arrays?.albedo;

        internal Texture normalArray => m_Normal != null ? m_Normal : m_Arrays?.normal;

        internal Texture maskArray => m_Mask != null ? m_Mask : m_Arrays?.mask;

        internal int layerDepth => m_Albedo != null ? m_Albedo.depth : m_Arrays != null ? m_Arrays.depth : 0;

        internal Vector4[] layerSources => m_Albedo != null || m_Arrays == null ? s_AllSources : m_Arrays.sources;

        internal StyleTerrainLayerArrays layerArrays => m_Arrays;

        internal void PrepareLayers(HBStyleTerrainLayer[] layers, int count, Shader slice)
        {
            if (m_Albedo != null)
                return;

            m_Arrays ??= new StyleTerrainLayerArrays();
            m_Arrays.Refresh(layers, count, m_TextureResolution, slice);
        }

        #if UNITY_EDITOR
        internal Texture2DArray hand => Live(ref m_HandLive, m_HandMap, m_Hand);
#else
        internal Texture2DArray hand => m_Hand;
#endif

        internal int handChannels => m_HandLayers != null ? m_HandLayers.Length : 0;

        internal HBStyleTerrainLayer[] handLayers => m_HandLayers;

        internal int handVersion => m_HandVersion;

        internal Texture2D grass => Live(ref m_GrassLive, m_GrassMap, m_Grass);

        internal int grassVersion => m_GrassVersion;

        internal int controlVersion => m_ControlVersion;

        internal Texture2DArray flowerArray => m_FlowerArray;

        internal int flowerCount => m_FlowerTextures != null ? m_FlowerTextures.Length : 0;

        internal string grassSignature => m_GrassSignature;

        [System.NonSerialized]
        int m_ControlVersion;

        [System.NonSerialized]
        internal int grassStamp;

        [System.NonSerialized]
        internal int grassPaint;

        [System.NonSerialized]
        readonly Vector4[] m_GrassMarks = new Vector4[GrassMarks];

        internal int FlowerSlice(HBStyleTerrainLayer layer)
        {
            return layer != null ? HeadSlice(layer.flowerTexture) : -1;
        }

        internal int CardSlice(HBStyleTerrainLayer layer)
        {
            return layer != null ? HeadSlice(layer.grassCardTexture) : -1;
        }

        int HeadSlice(Texture2D texture)
        {
            if (texture == null || m_FlowerArray == null || m_FlowerTextures == null)
                return -1;

            var slice = System.Array.IndexOf(m_FlowerTextures, texture);

            return slice < m_FlowerArray.depth ? slice : -1;
        }

        internal void SetFlowerTextures(Texture2DArray array, Texture2D[] textures)
        {
            m_FlowerArray = array;
            m_FlowerTextures = textures;
        }

        internal void SetGrassSignature(string signature)
        {
            m_GrassSignature = signature;
        }

        internal string textureSignature => m_TextureSignature;

        internal string bakeSignature
        {
            get => m_BakeSignature;
            set => m_BakeSignature = value;
        }

        internal bool painted => controlLayers > 0 && control != null;

        internal HBStyleTerrainLayer Layer(int index)
        {
            return index >= 0 && index < controlLayers ? m_BakedLayers[index] : null;
        }

        internal void SetControl(Texture2DArray control, HBStyleTerrainLayer[] layers)
        {
            if (control == null)
            {
                Release(ref m_ControlLive);
                m_ControlMap.Clear();
                m_Control = null;
            }
            else
            {
                m_ControlLive = Keep(control);
                Touched(m_ControlMap);
            }

            m_BakedLayers = layers;
            m_ControlVersion++;
        }

        internal int HandChannel(HBStyleTerrainLayer layer)
        {
            return layer != null && m_HandLayers != null ? System.Array.IndexOf(m_HandLayers, layer) : -1;
        }

        internal void SetHand(Texture2DArray hand, HBStyleTerrainLayer[] layers)
        {
#if UNITY_EDITOR
            m_HandLive = Keep(hand);
            Touched(m_HandMap);
#else
            m_Hand = hand;
#endif
            m_HandLayers = layers;
            m_HandVersion++;
        }

        internal void HandEdited()
        {
            m_HandVersion++;
#if UNITY_EDITOR
            Touched(m_HandMap);
#endif
        }

        internal void SetGrass(Texture2D grass)
        {
            m_GrassLive = Keep(grass);
            Touched(m_GrassMap);
            m_GrassVersion++;
        }

        internal void GrassPainted(Vector4 area)
        {
            m_GrassMarks[grassPaint % GrassMarks] = area;
            grassPaint++;
            Touched(m_GrassMap);
        }

        internal bool GrassTouched(int seen, Vector4 area)
        {
            if (grassPaint - seen > GrassMarks)
                return true;

            for (var mark = seen; mark < grassPaint; mark++)
            {
                var painted = m_GrassMarks[mark % GrassMarks];

                if (painted.x <= area.z && painted.z >= area.x && painted.y <= area.w && painted.w >= area.y)
                    return true;
            }

            return false;
        }

        internal void SetPlants(HBStyleTerrainPlants plants)
        {
            m_Plants = plants;
        }

        internal void GrassEdited()
        {
            m_GrassVersion++;
            Touched(m_GrassMap);
        }

        internal void SetTextures(Texture2DArray albedo, Texture2DArray normal, Texture2DArray mask, string signature)
        {
            m_Albedo = albedo;
            m_Normal = normal;
            m_Mask = mask;
            m_TextureSignature = signature;
        }

        static T Live<T>(ref T live, StyleTerrainMap map, T legacy) where T : Texture
        {
            if (live == null && map != null && !map.empty)
                live = map.Unpack() as T;

            return live != null ? live : legacy;
        }

        static Vector4[] AllSources()
        {
            var sources = new Vector4[MaxLayers];

            for (var index = 0; index < sources.Length; index++)
                sources[index] = Vector4.one;

            return sources;
        }

        static T Keep<T>(T texture) where T : Texture
        {
            if (texture != null)
                texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return texture;
        }

        void WaterTouched()
        {
            Touched(m_LevelMap);
            Touched(m_MixMap);
            Touched(m_FlowMap);
        }

        void Touched(StyleTerrainMap map)
        {
            if (map != null)
                map.stale = true;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            PackMaps(false);
            Release(ref m_HandLive);
#endif
            Release(ref m_ControlLive);
            Release(ref m_GrassLive);
            Release(ref m_LevelLive);
            Release(ref m_MixLive);
            Release(ref m_FlowLive);
            m_Arrays?.Release();
        }

        static void Release<T>(ref T texture) where T : Texture
        {
#if UNITY_EDITOR
            if (texture != null && !UnityEditor.EditorUtility.IsPersistent(texture))
#else
            if (texture != null)
#endif
                CoreUtils.Destroy(texture);

            texture = null;
        }

#if UNITY_EDITOR
        internal bool legacyMaps => m_Hand != null || m_Control != null || m_Grass != null || m_WaterLevel != null ||
                                    m_WaterMix != null || m_WaterFlow != null || m_Albedo != null ||
                                    m_Normal != null || m_Mask != null;

        internal bool HoldsLegacy(Texture texture)
        {
            return texture == m_Hand || texture == m_Control || texture == m_Grass || texture == m_WaterLevel ||
                   texture == m_WaterMix || texture == m_WaterFlow || texture == m_Albedo || texture == m_Normal ||
                   texture == m_Mask;
        }

        internal bool PackMaps(bool all)
        {
            var packed = Pack(m_HandMap, m_HandLive, all);

            packed |= Pack(m_ControlMap, m_ControlLive, all);

            packed |= Pack(m_GrassMap, m_GrassLive, all);
            packed |= Pack(m_LevelMap, m_LevelLive, all);
            packed |= Pack(m_MixMap, m_MixLive, all);
            packed |= Pack(m_FlowMap, m_FlowLive, all);
            return packed;
        }

        internal void MapsTouched()
        {
            Stale(m_HandMap, m_HandLive);
            Stale(m_ControlMap, m_ControlLive);
            Stale(m_GrassMap, m_GrassLive);
            Stale(m_LevelMap, m_LevelLive);
            Stale(m_MixMap, m_MixLive);
            Stale(m_FlowMap, m_FlowLive);
        }

        internal void MoveLegacyMaps(List<Texture> moved, List<string> failed)
        {
            m_Hand = Move(m_HandMap, m_Hand, moved, failed);
            m_Control = Move(m_ControlMap, m_Control, moved, failed);
            m_ControlVersion++;
            m_Grass = Move(m_GrassMap, m_Grass, moved, failed);
            m_WaterLevel = Move(m_LevelMap, m_WaterLevel, moved, failed);
            m_WaterMix = Move(m_MixMap, m_WaterMix, moved, failed);
            m_WaterFlow = Move(m_FlowMap, m_WaterFlow, moved, failed);
            m_Albedo = Drop(m_Albedo, moved);
            m_Normal = Drop(m_Normal, moved);
            m_Mask = Drop(m_Mask, moved);
            m_GrassVersion++;
            m_WaterVersion++;
        }

        static T Move<T>(StyleTerrainMap map, T legacy, List<Texture> moved, List<string> failed) where T : Texture
        {
            if (legacy == null)
                return null;

            try
            {
                map.Pack(legacy);

                if (map.Matches(legacy))
                {
                    moved.Add(legacy);
                    return null;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }

            map.Clear();
            failed.Add(legacy.name);
            return legacy;
        }

        static Texture2DArray Drop(Texture2DArray copy, List<Texture> dropped)
        {
            if (copy != null)
                dropped.Add(copy);

            return null;
        }

        static bool Pack(StyleTerrainMap map, Texture live, bool all)
        {
            if (map == null || live == null || !(all || map.stale))
                return false;

            map.Pack(live);
            return true;
        }

        static void Stale(StyleTerrainMap map, Texture live)
        {
            if (map != null && live != null)
                map.stale = true;
        }
#endif

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

            if (m_Liquids == null)
                m_Liquids = new HBStyleLiquidLayer[0];
            else if (m_Liquids.Length > LiquidSlots)
                System.Array.Resize(ref m_Liquids, LiquidSlots);

            m_SeaSlot = seaSlot;
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
