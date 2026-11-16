using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    enum StylePlantKind
    {
        Card,
        Flower
    }

    enum StylePlantPartKind
    {
        Blades,
        PetalHead
    }

    enum StylePetalShape
    {
        Round,
        Pointed,
        Heart
    }

    [Serializable]
    sealed class StylePlantPart
    {
        public StylePlantPartKind kind;
        public string label = "Blades";
        public bool visible = true;

        public int blades = 16;
        public float height = 0.95f;
        public float heightVariation = 0.4f;
        public float width = 0.035f;
        public float fan = 0.4f;
        public float spread = 0.5f;

        public StylePetalShape shape = StylePetalShape.Round;
        public int petalsMin = 5;
        public int petalsMax = 7;
        public int rings = 2;
        public float petalLength = 0.44f;
        public float petalWidth = 0.55f;
        public float irregularity = 0.25f;
        public float centreSize = 0.11f;
        public float petalVariation = 0.2f;

        public Color petal = new Color(0.86f, 0.24f, 0.28f);
        public Color centre = new Color(0.95f, 0.7f, 0.15f);
        public float shading = 0.45f;

        public Vector2 offset;
        public float scale = 1f;
        public int seed = 1;

        internal const int MostBlades = 64;
        internal const int MostPetals = 12;
        internal const int MostRings = 3;

        internal static StylePlantPart Create(StylePlantPartKind kind)
        {
            var part = new StylePlantPart
            {
                kind = kind,
                label = kind == StylePlantPartKind.PetalHead ? "Flower" : "Blades"
            };

            part.ResetShape();
            part.ResetColour();
            part.ResetPlacement();
            return part;
        }

        internal static StylePlantPart RandomFlower(System.Random random)
        {
            var part = Create(StylePlantPartKind.PetalHead);
            var low = random.Next(4, 9);

            part.shape = (StylePetalShape)random.Next(3);
            part.petalsMin = low;
            part.petalsMax = Mathf.Min(low + random.Next(3), MostPetals);
            part.rings = 1 + random.Next(MostRings);
            part.petalLength = Between(random, 0.3f, 0.48f);
            part.petalWidth = Between(random, 0.35f, 1f);
            part.irregularity = Between(random, 0.05f, 0.4f);
            part.centreSize = Between(random, 0.06f, 0.16f);
            part.petalVariation = Between(random, 0.1f, 0.35f);
            part.petal = Color.HSVToRGB(Between(random, 0f, 1f), Between(random, 0.35f, 0.85f),
                                        Between(random, 0.75f, 1f));
            var dark = random.Next(4) == 0;

            part.centre = Color.HSVToRGB(dark ? Between(random, 0.05f, 0.1f) : Between(random, 0.08f, 0.16f),
                                         dark ? Between(random, 0.5f, 0.8f) : Between(random, 0.6f, 0.95f),
                                         dark ? Between(random, 0.2f, 0.35f) : Between(random, 0.8f, 1f));
            part.shading = Between(random, 0.3f, 0.6f);
            part.seed = random.Next(1, 1000);
            return part;
        }

        static float Between(System.Random random, float low, float high)
        {
            return Mathf.Lerp(low, high, (float)random.NextDouble());
        }

        internal void ResetShape()
        {
            blades = 16;
            height = 0.95f;
            heightVariation = 0.4f;
            width = 0.035f;
            fan = 0.4f;
            spread = 0.5f;
            shape = StylePetalShape.Round;
            petalsMin = 5;
            petalsMax = 7;
            rings = 2;
            petalLength = 0.44f;
            petalWidth = 0.55f;
            irregularity = 0.25f;
            centreSize = 0.11f;
            petalVariation = 0.2f;
        }

        internal void ResetColour()
        {
            petal = new Color(0.86f, 0.24f, 0.28f);
            centre = new Color(0.95f, 0.7f, 0.15f);
            shading = 0.45f;
        }

        internal void ResetPlacement()
        {
            offset = Vector2.zero;
            scale = 1f;
            seed = 1;
        }

        internal StylePlantPart Clone()
        {
            return JsonUtility.FromJson<StylePlantPart>(JsonUtility.ToJson(this));
        }
    }

    [Serializable]
    sealed class StylePlant
    {
        public string name = "Clump Grass";
        public StylePlantKind kind;
        public List<StylePlantPart> parts = new List<StylePlantPart>();

        internal const int MostVariants = 8;

        internal StylePlantPartKind partKind =>
            kind == StylePlantKind.Flower ? StylePlantPartKind.PetalHead : StylePlantPartKind.Blades;

        internal int variantCount => Mathf.Clamp(Drawn().Count, 1, MostVariants);

        internal static StylePlant Create(StylePlantKind kind)
        {
            var plant = new StylePlant
            {
                kind = kind,
                name = kind == StylePlantKind.Flower ? "Flower Heads" : "Clump Grass"
            };

            plant.parts.Add(StylePlantPart.Create(plant.partKind));
            return plant;
        }

        internal StylePlantPart Card()
        {
            foreach (var part in parts)
            {
                if (part.kind == StylePlantPartKind.Blades)
                    return part;
            }

            var created = StylePlantPart.Create(StylePlantPartKind.Blades);

            parts.Insert(0, created);
            return created;
        }

        internal List<StylePlantPart> Drawn()
        {
            var drawn = new List<StylePlantPart>();

            if (kind == StylePlantKind.Card)
            {
                drawn.Add(Card());
                return drawn;
            }

            foreach (var part in parts)
            {
                if (part.visible && part.kind == StylePlantPartKind.PetalHead && drawn.Count < MostVariants)
                    drawn.Add(part);
            }

            return drawn;
        }
    }

    [Serializable]
    sealed class StyleGrassDesign
    {
        public StylePlant card = StylePlant.Create(StylePlantKind.Card);
        public StylePlant flower = StylePlant.Create(StylePlantKind.Flower);
    }

    sealed class StylePlantGenerator
    {
        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleGrassStudio.compute";
        const int ThumbnailSize = 64;
        const float InnerRing = 0.62f;
        const float InnerLift = 1.12f;

        internal const int Cell = 512;

        static readonly int s_PlantId = Shader.PropertyToID("_Plant");
        static readonly int s_PetalsId = Shader.PropertyToID("_Petals");
        static readonly int s_AreaId = Shader.PropertyToID("_Area");
        static readonly int s_FillId = Shader.PropertyToID("_Fill");
        static readonly int s_PlaceId = Shader.PropertyToID("_Place");
        static readonly int s_BladeShapeId = Shader.PropertyToID("_BladeShape");
        static readonly int s_BladeSpreadId = Shader.PropertyToID("_BladeSpread");
        static readonly int s_PetalId = Shader.PropertyToID("_Petal");
        static readonly int s_PetalColourId = Shader.PropertyToID("_PetalColour");
        static readonly int s_CentreColourId = Shader.PropertyToID("_CentreColour");
        static readonly int s_SourceId = Shader.PropertyToID("_Source");
        static readonly int s_EncodedId = Shader.PropertyToID("_Encoded");

        readonly List<RenderTexture> m_Thumbnails = new List<RenderTexture>();
        readonly Vector4[] m_PetalData = new Vector4[StylePlantPart.MostPetals * StylePlantPart.MostRings];

        ComputeShader m_Compute;
        ComputeBuffer m_Petals;
        int m_Clear;
        int m_Blades;
        int m_PetalKernel;
        int m_Encode;
        RenderTexture m_Encoded;

        internal RenderTexture output { get; private set; }

        internal bool ready => output != null;

        internal Texture Thumbnail(int index)
        {
            return index >= 0 && index < m_Thumbnails.Count ? m_Thumbnails[index] : null;
        }

        internal void Generate(StylePlant plant)
        {
            if (!Prepare())
                return;

            var drawn = plant.Drawn();
            var cells = Mathf.Max(drawn.Count, 1);
            var width = Cell * cells;

            if (output == null || output.width != width)
            {
                Destroy(output);
                output = NewTarget(width, Cell, true, "HB Texture Studio Plant");
            }

            for (var cell = 0; cell < cells; cell++)
            {
                var part = cell < drawn.Count ? drawn[cell] : null;

                Clear(output, Cell, cell * Cell, FillOf(part));

                if (part != null)
                    DrawPart(output, Cell, cell * Cell, part);
            }

            output.GenerateMips();

            while (m_Thumbnails.Count < plant.parts.Count)
                m_Thumbnails.Add(NewTarget(ThumbnailSize, ThumbnailSize, false, "HB Texture Studio Plant Part"));

            for (var i = 0; i < plant.parts.Count; i++)
            {
                var part = plant.parts[i];

                Clear(m_Thumbnails[i], ThumbnailSize, 0, FillOf(part));
                DrawPart(m_Thumbnails[i], ThumbnailSize, 0, part);
            }
        }

        internal RenderTexture Export()
        {
            if (!ready || !Prepare())
                return null;

            if (m_Encoded == null || m_Encoded.width != output.width)
            {
                Destroy(m_Encoded);
                m_Encoded = NewTarget(output.width, output.height, false, "HB Texture Studio Plant Export");
            }

            m_Compute.SetVector(s_AreaId, new Vector4(output.width, output.height, 0f, 0f));
            m_Compute.SetTexture(m_Encode, s_SourceId, output);
            m_Compute.SetTexture(m_Encode, s_EncodedId, m_Encoded);
            m_Compute.Dispatch(m_Encode, (output.width + 7) / 8, (output.height + 7) / 8, 1);
            return m_Encoded;
        }

        bool Prepare()
        {
            if (m_Compute == null)
            {
                m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

                if (m_Compute == null || !SystemInfo.supportsComputeShaders)
                    return false;

                m_Clear = m_Compute.FindKernel("Clear");
                m_Blades = m_Compute.FindKernel("Blades");
                m_PetalKernel = m_Compute.FindKernel("Petals");
                m_Encode = m_Compute.FindKernel("Encode");
            }

            m_Petals ??= new ComputeBuffer(m_PetalData.Length, 16);
            return true;
        }

        static Color FillOf(StylePlantPart part)
        {
            return part != null && part.kind == StylePlantPartKind.PetalHead ? part.petal : Color.white;
        }

        void Clear(RenderTexture target, int size, int origin, Color fill)
        {
            var linear = fill.linear;

            m_Compute.SetVector(s_AreaId, new Vector4(size, origin, 0f, 0f));
            m_Compute.SetVector(s_FillId, new Vector4(linear.r, linear.g, linear.b, 0f));
            Dispatch(m_Clear, target, size);
        }

        void DrawPart(RenderTexture target, int size, int origin, StylePlantPart part)
        {
            m_Compute.SetVector(s_AreaId, new Vector4(size, origin, 0f, 0f));
            m_Compute.SetVector(s_PlaceId, new Vector4(part.offset.x, part.offset.y, part.scale, 0f));

            if (part.kind == StylePlantPartKind.Blades)
            {
                var blades = Mathf.Clamp(part.blades, 1, StylePlantPart.MostBlades);

                m_Compute.SetVector(s_BladeShapeId, new Vector4(blades, part.height, part.heightVariation,
                                                                part.width));
                m_Compute.SetVector(s_BladeSpreadId, new Vector4(part.fan, part.spread, Mathf.Max(part.seed, 0), 0f));
                Dispatch(m_Blades, target, size);
                return;
            }

            var count = FillPetals(part, new System.Random(part.seed * 7919 + 17));
            var colour = part.petal.linear;
            var centre = part.centre.linear;

            m_Petals.SetData(m_PetalData, 0, 0, count);
            m_Compute.SetBuffer(m_PetalKernel, s_PetalsId, m_Petals);
            m_Compute.SetVector(s_PetalId, new Vector4((int)part.shape + 1, count, part.centreSize, part.shading));
            m_Compute.SetVector(s_PetalColourId, new Vector4(colour.r, colour.g, colour.b, 1f));
            m_Compute.SetVector(s_CentreColourId, new Vector4(centre.r, centre.g, centre.b, 1f));
            Dispatch(m_PetalKernel, target, size);
        }

        void Dispatch(int kernel, RenderTexture target, int size)
        {
            var groups = (size + 7) / 8;

            m_Compute.SetTexture(kernel, s_PlantId, target);
            m_Compute.Dispatch(kernel, groups, groups, 1);
        }

        int FillPetals(StylePlantPart part, System.Random random)
        {
            var low = Mathf.Clamp(Mathf.Min(part.petalsMin, part.petalsMax), 1, StylePlantPart.MostPetals);
            var high = Mathf.Clamp(Mathf.Max(part.petalsMin, part.petalsMax), 1, StylePlantPart.MostPetals);
            var count = random.Next(low, high + 1);
            var rings = Mathf.Clamp(part.rings, 1, StylePlantPart.MostRings);
            var irregularity = part.irregularity;
            var index = 0;

            for (var ring = 0; ring < rings; ring++)
            {
                var shrink = Mathf.Pow(InnerRing, ring);
                var turn = Next(random) * Mathf.PI * 2f;
                var step = Mathf.PI * 2f / count;

                for (var i = 0; i < count; i++)
                {
                    m_PetalData[index++] = new Vector4(
                        turn + (i + (Next(random) - 0.5f) * irregularity) * step,
                        part.petalLength * shrink * (1f - irregularity * 0.5f * Next(random)),
                        part.petalWidth * (1f - irregularity * 0.3f * Next(random)),
                        Mathf.Pow(InnerLift, ring) * (1f - part.petalVariation * Next(random)));
                }
            }

            return index;
        }

        static float Next(System.Random random)
        {
            return (float)random.NextDouble();
        }

        static RenderTexture NewTarget(int width, int height, bool mips, string name)
        {
            var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf,
                                           RenderTextureReadWrite.Linear)
            {
                name = name,
                enableRandomWrite = true,
                useMipMap = mips,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            target.Create();
            return target;
        }

        static void Destroy(RenderTexture target)
        {
            if (target == null)
                return;

            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        internal void Release()
        {
            Destroy(output);
            output = null;
            Destroy(m_Encoded);
            m_Encoded = null;

            foreach (var thumbnail in m_Thumbnails)
                Destroy(thumbnail);

            m_Thumbnails.Clear();
            m_Petals?.Release();
            m_Petals = null;
        }
    }
}
