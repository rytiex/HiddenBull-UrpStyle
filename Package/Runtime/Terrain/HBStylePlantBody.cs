using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [AddComponentMenu("")]
    public sealed class HBStylePlantBody : MonoBehaviour
    {
        public HBStyleTerrain terrain { get; internal set; }
        public GameObject prefab { get; internal set; }
        public uint id { get; internal set; }

        public bool Harvest()
        {
            return terrain != null && terrain.Harvest(id);
        }
    }
}
