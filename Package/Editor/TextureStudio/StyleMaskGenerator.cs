using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    enum StyleMaskSource
    {
        Constant,
        Texture,
        Auto
    }

    enum StyleMaskPick
    {
        Red,
        Green,
        Blue,
        Alpha,
        Gray
    }

    enum StyleMaskMap
    {
        Metallic,
        Occlusion,
        Height,
        Smoothness
    }

    [Serializable]
    sealed class StyleMaskChannel
    {
        public StyleMaskSource source;
        public Texture2D texture;
        public StyleMaskPick pick;
        public bool invert;
        public float value;

        public StyleMaskChannel()
        {
        }

        internal StyleMaskChannel(StyleMaskSource source, float value)
        {
            Set(source, value);
        }

        internal void Set(StyleMaskSource picked, float constant)
        {
            source = picked;
            texture = null;
            pick = StyleMaskPick.Gray;
            invert = false;
            value = constant;
        }
    }

    [Serializable]
    sealed class StyleMaskSettings
    {
        internal const float DefaultMetallic = 0f;
        internal const float DefaultOcclusion = 1f;
        internal const float DefaultHeight = 0.5f;
        internal const float DefaultSmoothness = 0.3f;

        public string name = "Mask";
        public int resolution;

        public Texture2D albedo;
        public Texture2D normal;

        public StyleMaskChannel metallic = new StyleMaskChannel(StyleMaskSource.Constant, DefaultMetallic);
        public StyleMaskChannel occlusion = new StyleMaskChannel(StyleMaskSource.Auto, DefaultOcclusion);
        public StyleMaskChannel height = new StyleMaskChannel(StyleMaskSource.Auto, DefaultHeight);
        public StyleMaskChannel smoothness = new StyleMaskChannel(StyleMaskSource.Auto, DefaultSmoothness);

        public float detailSize = 0.015f;
        public float heightContrast = 4f;
        public float occlusionStrength = 1f;
        public float occlusionRadius = 0.008f;
        public float cavitySmoothness = 0.15f;
        public float topSmoothness = 0.45f;

        internal StyleMaskChannel this[StyleMaskMap map]
        {
            get
            {
                switch (map)
                {
                    case StyleMaskMap.Metallic:
                        return metallic;
                    case StyleMaskMap.Occlusion:
                        return occlusion;
                    case StyleMaskMap.Height:
                        return height;
                    default:
                        return smoothness;
                }
            }
        }

        internal bool usesAuto => occlusion.source == StyleMaskSource.Auto ||
                                  height.source == StyleMaskSource.Auto ||
                                  smoothness.source == StyleMaskSource.Auto;

        internal int sourceSize
        {
            get
            {
                Texture2D[] sources =
                    { albedo, metallic.texture, occlusion.texture, height.texture, smoothness.texture, normal };

                foreach (var source in sources)
                {
                    if (source != null)
                        return Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(source.width, source.height)), 64, 4096);
                }

                return 1024;
            }
        }

        internal void ResetChannel(StyleMaskMap map)
        {
            switch (map)
            {
                case StyleMaskMap.Metallic:
                    metallic.Set(StyleMaskSource.Constant, DefaultMetallic);
                    break;
                case StyleMaskMap.Occlusion:
                    occlusion.Set(StyleMaskSource.Auto, DefaultOcclusion);
                    break;
                case StyleMaskMap.Height:
                    height.Set(StyleMaskSource.Auto, DefaultHeight);
                    break;
                default:
                    smoothness.Set(StyleMaskSource.Auto, DefaultSmoothness);
                    break;
            }
        }

        internal void ResetEstimate()
        {
            detailSize = 0.015f;
            heightContrast = 4f;
            occlusionStrength = 1f;
            occlusionRadius = 0.008f;
            cavitySmoothness = 0.15f;
            topSmoothness = 0.45f;
        }
    }

    sealed class StyleMaskGenerator
    {
        internal const int PreviewSize = 1024;

        const string ComputePath = "Packages/com.hiddenbull.urpstyle/Editor/TextureStudio/StyleMaskStudio.compute";
        const int MaxTaps = 128;
        const int Channels = 4;

        static readonly int s_AlbedoId = Shader.PropertyToID("_Albedo");
        static readonly int s_NormalId = Shader.PropertyToID("_Normal");
        static readonly int s_LumaInId = Shader.PropertyToID("_LumaIn");
        static readonly int s_BlurInId = Shader.PropertyToID("_BlurIn");
        static readonly int s_MaskInId = Shader.PropertyToID("_MaskIn");
        static readonly int s_LumaId = Shader.PropertyToID("_Luma");
        static readonly int s_BlurOutId = Shader.PropertyToID("_BlurOut");
        static readonly int s_MaskId = Shader.PropertyToID("_Mask");
        static readonly int s_DisplayId = Shader.PropertyToID("_Display");
        static readonly int s_SizeId = Shader.PropertyToID("_Size");
        static readonly int s_EstimateId = Shader.PropertyToID("_Estimate");
        static readonly int s_SmoothId = Shader.PropertyToID("_Smooth");
        static readonly int s_ModesId = Shader.PropertyToID("_Modes");
        static readonly int s_PicksId = Shader.PropertyToID("_Picks");
        static readonly int s_ShowId = Shader.PropertyToID("_Show");

        static readonly int[] s_SourceIds =
        {
            Shader.PropertyToID("_Source0"), Shader.PropertyToID("_Source1"), Shader.PropertyToID("_Source2"),
            Shader.PropertyToID("_Source3")
        };

        static readonly Vector4[] s_PickWeights =
        {
            new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f),
            new Vector4(0f, 0f, 0f, 1f), new Vector4(0.2126f, 0.7152f, 0.0722f, 0f)
        };

        readonly Vector4[] m_Modes = new Vector4[Channels];
        readonly Vector4[] m_Picks = new Vector4[Channels];

        ComputeShader m_Compute;
        RenderTexture m_Luma;
        RenderTexture m_Blurred;
        RenderTexture m_Temp;
        int m_LumaKernel;
        int m_BlurX;
        int m_BlurY;
        int m_Build;
        int m_Show;

        internal RenderTexture mask { get; private set; }

        internal RenderTexture display { get; private set; }

        internal bool ready => mask != null;

        internal bool Generate(StyleMaskSettings settings, int resolution)
        {
            if (!Prepare())
                return false;

            resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(resolution), 64, 4096);

            if (mask == null || mask.width != resolution)
            {
                Release();
                Prepare();
                m_Luma = NewTarget(resolution, RenderTextureFormat.RFloat, false, "Luma");
                m_Blurred = NewTarget(resolution, RenderTextureFormat.RFloat, false, "Blurred");
                m_Temp = NewTarget(resolution, RenderTextureFormat.RFloat, false, "Blur Pass");
                mask = NewTarget(resolution, RenderTextureFormat.ARGBHalf, true, "Mask");
                display = NewTarget(resolution, RenderTextureFormat.ARGBHalf, false, "Display");
            }

            var radius = Mathf.Clamp(Mathf.RoundToInt(settings.detailSize * resolution), 1,
                                     Mathf.Min(MaxTaps, resolution / 2));
            var albedo = settings.albedo;
            var normal = settings.normal;

            m_Compute.SetVector(s_SizeId, new Vector4(resolution, 1f / resolution, radius, normal != null ? 1f : 0f));
            m_Compute.SetVector(s_EstimateId, new Vector4(settings.heightContrast, settings.occlusionStrength,
                                                          settings.occlusionRadius, 0f));
            m_Compute.SetVector(s_SmoothId, new Vector4(settings.cavitySmoothness, settings.topSmoothness,
                                                        Gamma(albedo) ? 1f : 0f, 0f));

            for (var channel = 0; channel < Channels; channel++)
            {
                var picked = settings[(StyleMaskMap)channel];
                var source = Source(picked);

                m_Modes[channel] = new Vector4((float)source, Gamma(picked.texture) ? 1f : 0f,
                                               picked.invert ? 1f : 0f, picked.value);
                m_Picks[channel] = s_PickWeights[(int)picked.pick];
                m_Compute.SetTexture(m_Build, s_SourceIds[channel],
                                     picked.texture != null ? picked.texture : Texture2D.whiteTexture);
            }

            m_Compute.SetVectorArray(s_ModesId, m_Modes);
            m_Compute.SetVectorArray(s_PicksId, m_Picks);

            m_Compute.SetTexture(m_LumaKernel, s_AlbedoId, albedo != null ? albedo : Texture2D.grayTexture);
            m_Compute.SetTexture(m_LumaKernel, s_LumaId, m_Luma);
            Dispatch(m_LumaKernel, resolution);

            m_Compute.SetTexture(m_BlurX, s_BlurInId, m_Luma);
            m_Compute.SetTexture(m_BlurX, s_BlurOutId, m_Temp);
            Dispatch(m_BlurX, resolution);
            m_Compute.SetTexture(m_BlurY, s_BlurInId, m_Temp);
            m_Compute.SetTexture(m_BlurY, s_BlurOutId, m_Blurred);
            Dispatch(m_BlurY, resolution);

            m_Compute.SetTexture(m_Build, s_LumaInId, m_Luma);
            m_Compute.SetTexture(m_Build, s_BlurInId, m_Blurred);
            m_Compute.SetTexture(m_Build, s_NormalId, normal != null ? normal : Texture2D.grayTexture);
            m_Compute.SetTexture(m_Build, s_MaskId, mask);
            Dispatch(m_Build, resolution);
            mask.GenerateMips();
            return true;
        }

        internal void Show(StyleMaskMap map)
        {
            if (!ready)
                return;

            var weights = Vector4.zero;
            weights[(int)map] = 1f;

            m_Compute.SetVector(s_SizeId, new Vector4(mask.width, 1f / mask.width, 1f, 0f));
            m_Compute.SetVector(s_ShowId, weights);
            m_Compute.SetTexture(m_Show, s_MaskInId, mask);
            m_Compute.SetTexture(m_Show, s_DisplayId, display);
            Dispatch(m_Show, mask.width);
        }

        static StyleMaskSource Source(StyleMaskChannel channel)
        {
            return channel.source == StyleMaskSource.Texture && channel.texture == null
                ? StyleMaskSource.Constant
                : channel.source;
        }

        static bool Gamma(Texture2D texture)
        {
            return texture != null && GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat);
        }

        bool Prepare()
        {
            if (m_Compute == null)
            {
                m_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

                if (m_Compute == null || !SystemInfo.supportsComputeShaders)
                    return false;

                m_LumaKernel = m_Compute.FindKernel("Luma");
                m_BlurX = m_Compute.FindKernel("BlurX");
                m_BlurY = m_Compute.FindKernel("BlurY");
                m_Build = m_Compute.FindKernel("Build");
                m_Show = m_Compute.FindKernel("Show");
            }

            return true;
        }

        void Dispatch(int kernel, int size)
        {
            var groups = (size + 7) / 8;

            m_Compute.Dispatch(kernel, groups, groups, 1);
        }

        static RenderTexture NewTarget(int size, RenderTextureFormat format, bool mips, string name)
        {
            var target = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "HB Texture Studio Mask " + name,
                enableRandomWrite = true,
                useMipMap = mips,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            target.Create();
            return target;
        }

        static void Destroy(ref RenderTexture target)
        {
            if (target == null)
                return;

            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            target = null;
        }

        internal void Release()
        {
            var shown = mask;
            var screen = display;

            Destroy(ref shown);
            Destroy(ref screen);
            mask = null;
            display = null;
            Destroy(ref m_Luma);
            Destroy(ref m_Blurred);
            Destroy(ref m_Temp);
        }
    }
}
