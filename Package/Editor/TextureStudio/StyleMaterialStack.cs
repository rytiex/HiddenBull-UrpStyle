using System;
using System.Collections.Generic;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    sealed class StyleMaterialStack
    {
        public string name = "New Material";
        public int resolution = 1024;

        public List<StyleMaterialSettings> layers = new List<StyleMaterialSettings>();

        internal StyleMaterialSettings Layer(int index)
        {
            if (layers.Count == 0)
                layers.Add(StyleMaterialSettings.Create(StyleMaterialPattern.Cells));

            return layers[Math.Clamp(index, 0, layers.Count - 1)];
        }
    }
}
