using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleMaterialShape
    {
        Fill,
        Cracks,
        Scratches,
        Streaks,
        WoodGrain,
        Chips,
        Clumps,
        Leaves
    }

    enum StyleModuleHeight
    {
        None,
        Cut,
        Raise,
        Flatten
    }

    enum StyleModuleArea
    {
        Everywhere,
        Pieces,
        Joints
    }

    enum StyleMaskKind
    {
        Edges,
        Cavities,
        Height,
        Noise,
        RandomPieces
    }

    enum StyleMaskBlend
    {
        Multiply,
        Add,
        Subtract
    }

    [Serializable]
    sealed class StyleModuleMask
    {
        public StyleMaskKind kind;
        public StyleMaskBlend blend;
        public float threshold = 0.5f;
        public float softness = 0.2f;
        public bool invert;
        public int scale = 4;
        public float warp;
    }

    [Serializable]
    sealed class StyleMaterialModule
    {
        public StyleMaterialShape shape;
        public bool on = true;

        public Color colour = new Color(0.3f, 0.25f, 0.2f, 1f);
        public float variation = 0.2f;

        public float amount = 0.5f;
        public int scale = 8;
        public float size = 0.5f;
        public int seed = 1;
        public float extra = 0.3f;

        public StyleModuleHeight height;
        public float depth = 0.1f;

        public float smoothness = 0.3f;
        public float metallic;

        public StyleModuleArea area;
        public List<StyleModuleMask> masks = new List<StyleModuleMask>();

        internal const int MaxMasks = 3;

        internal static readonly StyleMaterialShape[] Shapes =
            (StyleMaterialShape[])Enum.GetValues(typeof(StyleMaterialShape));

        internal static StyleMaterialModule Create(StyleMaterialShape shape)
        {
            var module = new StyleMaterialModule { shape = shape };

            module.ApplyShapeDefaults();
            return module;
        }

        internal StyleMaterialModule Clone()
        {
            return JsonUtility.FromJson<StyleMaterialModule>(JsonUtility.ToJson(this));
        }

        internal void Reset()
        {
            var keptShape = shape;
            var keptOn = on;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new StyleMaterialModule()), this);
            shape = keptShape;
            on = keptOn;
            ApplyShapeDefaults();
        }

        void ApplyShapeDefaults()
        {
            switch (shape)
            {
                case StyleMaterialShape.Fill:
                    amount = 1f;
                    break;
                case StyleMaterialShape.Cracks:
                    scale = 6;
                    height = StyleModuleHeight.Cut;
                    break;
                case StyleMaterialShape.Scratches:
                    scale = 32;
                    height = StyleModuleHeight.Cut;
                    depth = 0.05f;
                    break;
                case StyleMaterialShape.Streaks:
                    scale = 24;
                    break;
                case StyleMaterialShape.WoodGrain:
                    colour = new Color(0.25f, 0.15f, 0.08f, 1f);
                    variation = 0f;
                    amount = 0.6f;
                    scale = 8;
                    height = StyleModuleHeight.Cut;
                    depth = 0.05f;
                    area = StyleModuleArea.Pieces;
                    break;
                case StyleMaterialShape.Chips:
                    colour = new Color(0.7f, 0.68f, 0.64f, 1f);
                    amount = 0.4f;
                    scale = 24;
                    size = 0.4f;
                    height = StyleModuleHeight.Cut;
                    depth = 0.4f;
                    area = StyleModuleArea.Pieces;
                    break;
                case StyleMaterialShape.Clumps:
                    colour = new Color(0.45f, 0.42f, 0.38f, 1f);
                    variation = 0.3f;
                    amount = 0.6f;
                    scale = 32;
                    extra = 0f;
                    height = StyleModuleHeight.Raise;
                    depth = 0.15f;
                    area = StyleModuleArea.Joints;
                    break;
                case StyleMaterialShape.Leaves:
                    colour = new Color(0.55f, 0.3f, 0.1f, 1f);
                    variation = 0.4f;
                    amount = 0.4f;
                    scale = 16;
                    extra = 0.15f;
                    height = StyleModuleHeight.Raise;
                    depth = 0.05f;
                    smoothness = 0.25f;
                    break;

            }
        }
    }
}
