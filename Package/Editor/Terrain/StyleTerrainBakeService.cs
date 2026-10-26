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

            var baked = data.control != null && data.bakeSignature == StyleTerrainBaker.Signature(data) &&
                        data.textureSignature == StyleTerrainArrays.Signature(data);

            return baked ? StyleTerrainBakeState.UpToDate : StyleTerrainBakeState.NeedsBake;
        }

        internal static void Run(HBStyleTerrainData data)
        {
            if (data.terrainData == null)
                return;

            if (data.textureSignature != StyleTerrainArrays.Signature(data))
                StyleTerrainArrays.Build(data);

            var signature = StyleTerrainBaker.Signature(data);

            StyleTerrainBaker.Bake(data);
            data.bakeSignature = signature;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }
    }
}
