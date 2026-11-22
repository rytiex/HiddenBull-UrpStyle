using UnityEditor;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleTerrainBakeState
    {
        NoHeights,
        NoLayers,
        UpToDate,
        NeedsBake
    }

    static class StyleTerrainBakeService
    {
        internal static StyleTerrainBakeState State(HBStyleTerrainData data)
        {
            if (data.terrainData == null)
                return StyleTerrainBakeState.NoHeights;

            if (data.layerCount == 0)
                return data.controlLayers > 0 ? StyleTerrainBakeState.NeedsBake : StyleTerrainBakeState.NoLayers;

            var baked = data.control != null && Same(data.bakeSignature, StyleTerrainBaker.Signature(data));

            return baked ? StyleTerrainBakeState.UpToDate : StyleTerrainBakeState.NeedsBake;
        }

        static bool Same(string baked, string current)
        {
            return baked == current || baked != null && baked.Replace(',', '.') == current.Replace(',', '.');
        }

        internal static string Key(HBStyleTerrainData data)
        {
            return StyleTerrainBaker.Signature(data);
        }

        internal static void Run(HBStyleTerrainData data)
        {
            if (data.terrainData == null)
                return;

            if (data.textureSignature != StyleTerrainArrays.Signature(data))
                StyleTerrainArrays.Build(data);

            if (data.grassSignature != StyleTerrainArrays.GrassSignature(data))
                StyleTerrainArrays.BuildGrass(data);

            var signature = StyleTerrainBaker.Signature(data);

            StyleTerrainBaker.Bake(data);
            data.bakeSignature = signature;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }
    }
}
