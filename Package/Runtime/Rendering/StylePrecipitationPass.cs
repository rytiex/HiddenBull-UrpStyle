using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HiddenBull.UrpStyle
{
    sealed class StylePrecipitationPass : ScriptableRenderPass, IDisposable
    {
        public const string ShaderName = "Hidden/HiddenBull/Style Precipitation";

        const int RainPass = 0;
        const int SnowPass = 1;
        const int WindPass = 2;
        const int WindSegments = 20;
        const int WindRibbons = 48;
        const float WindBox = 180f;
        const float WindInnerStart = 1.5f;
        const float WindInnerEnd = 5f;
        const float WindSlow = 12f;
        const float WindFast = 30f;

        readonly struct Layer
        {
            public readonly int pass;
            public readonly float box;
            public readonly float innerStart;
            public readonly float innerEnd;
            public readonly float sizeScale;
            public readonly int maxCount;
            public readonly float seed;
            public readonly bool far;

            public Layer(int pass, float box, float innerStart, float innerEnd, float sizeScale, int maxCount,
                         float seed, bool far)
            {
                this.far = far;
                this.pass = pass;
                this.box = box;
                this.innerStart = innerStart;
                this.innerEnd = innerEnd;
                this.sizeScale = sizeScale;
                this.maxCount = maxCount;
                this.seed = seed;
            }
        }

        static readonly Layer[] s_Layers =
        {
            new Layer(RainPass, 24f, 0.3f, 1f, 1f, 16000, 0f, false),
            new Layer(RainPass, 80f, 10f, 12f, 4f, 20000, 104729f, true),
            new Layer(SnowPass, 16f, 0.3f, 1f, 1f, 12000, 7919f, false),
            new Layer(SnowPass, 60f, 7f, 8f, 3f, 16000, 130363f, true)
        };

        const float RainSpeed = 9f;
        const float RainLean = 0.8f;
        const float SnowSpeed = 1.1f;
        const float SnowLean = 2.5f;

        static readonly int s_SkyOcclusionId = Shader.PropertyToID("_HB_SkyOcclusion");
        static readonly int s_SkyViewProjId = Shader.PropertyToID("_HB_SkyOcclusionViewProj");
        static readonly int s_ParamsId = Shader.PropertyToID("_HB_WeatherParams");
        static readonly int s_RainVelocityId = Shader.PropertyToID("_HB_RainVelocity");
        static readonly int s_BoxId = Shader.PropertyToID("_HB_DropBox");
        static readonly int s_OffsetId = Shader.PropertyToID("_HB_DropOffset");
        static readonly int s_SplitId = Shader.PropertyToID("_HB_PrecipitationSplit");
        static readonly int s_RainColorId = Shader.PropertyToID("_HB_RainColor");
        static readonly int s_WindStreakId = Shader.PropertyToID("_HB_WindStreak");
        static readonly int s_MasksId = Shader.PropertyToID("_HB_WeatherMasks");
        static readonly int s_MaskCountId = Shader.PropertyToID("_HB_WeatherMaskCount");

        const float SplitAll = 0f;
        const float SplitBehind = 1f;
        const float SplitFront = 2f;

        Material m_Material;
        HBStylePrecipitationQualitySettings m_Settings;
        Camera m_SplitCamera;

        readonly float[] m_MaskDistances = new float[HBStyleWeatherMask.Limit];

        public StylePrecipitationPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler("HiddenBull Style Precipitation");

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(Shader shader, HBStylePrecipitationQualitySettings settings)
        {
            m_Settings = settings;

            if (shader == null)
            {
                Dispose();
                return;
            }

            if (m_Material == null || m_Material.shader != shader)
            {
                Dispose();

                m_Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        public bool isReady => m_Material != null;

        class PassData
        {
            public Material material;
            public TextureHandle sky;
            public Matrix4x4 skyViewProjection;
            public Vector4 parameters;
            public Vector4 rainVelocity;
            public Vector4 split;
            public Color rainColor;
            public Vector4 windStreak;
            public int ribbons;
            public int maskCount;
            public readonly Vector4[] masks = new Vector4[HBStyleWeatherMask.Limit];
            public readonly Vector4[] boxes = new Vector4[4];
            public readonly Vector4[] offsets = new Vector4[4];
            public readonly int[] counts = new int[4];
        }

        static float Wrap(double value, float box)
        {
            var wrapped = value - Math.Floor(value / box) * box;

            return (float)wrapped;
        }

        static Vector4 Offset(Vector3 velocity, double time, float box, float seed)
        {
            return new Vector4(
                Wrap(velocity.x * time, box),
                Wrap(velocity.y * time, box),
                Wrap(velocity.z * time, box),
                seed);
        }

        public void RecordBehind(RenderGraph renderGraph, ContextContainer frameData, TextureHandle target,
                                 TextureHandle b0, TextureHandle moments, TextureHandle behindMoments,
                                 TextureHandle info)
        {
            if (Record(renderGraph, frameData, target, SplitBehind, b0, moments, behindMoments, info))
                m_SplitCamera = frameData.Get<UniversalCameraData>().camera;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var split = m_SplitCamera != null && m_SplitCamera == frameData.Get<UniversalCameraData>().camera;

            m_SplitCamera = null;

            Record(renderGraph, frameData, frameData.Get<UniversalResourceData>().activeColorTexture,
                   split ? SplitFront : SplitAll, TextureHandle.nullHandle, TextureHandle.nullHandle,
                   TextureHandle.nullHandle, TextureHandle.nullHandle);
        }

        bool Record(RenderGraph renderGraph, ContextContainer frameData, TextureHandle target, float split,
                    TextureHandle b0, TextureHandle moments, TextureHandle behindMoments, TextureHandle info)
        {
            if (m_Material == null)
                return false;

            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                return false;

            var weather = VolumeManager.instance.stack?.GetComponent<HBStyleWeather>();

            if (weather == null || !weather.isVisible)
                return false;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (!resourceData.cameraDepthTexture.IsValid())
                return false;

            var hasSky = StyleSkyOcclusionData.TryGet(frameData, cameraData.camera, out var occlusion);

            var time = Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
            var heading = weather.windHeading.value * Mathf.Deg2Rad;
            var wind = new Vector2(Mathf.Sin(heading), Mathf.Cos(heading)) * weather.wind.value;

            var rainVelocity = new Vector3(wind.x * RainLean * RainSpeed, -RainSpeed, wind.y * RainLean * RainSpeed);
            var snowVelocity = new Vector3(wind.x * SnowLean * SnowSpeed, -SnowSpeed, wind.y * SnowLean * SnowSpeed);

            using var builder = renderGraph.AddRasterRenderPass<PassData>(
                "HiddenBull Style Precipitation", out var passData, profilingSampler);

            passData.material = m_Material;
            passData.sky = hasSky ? occlusion.texture : TextureHandle.nullHandle;
            passData.skyViewProjection = hasSky ? occlusion.viewProjection : Matrix4x4.identity;
            passData.parameters = new Vector4(weather.rain.value, weather.snow.value,
                                              hasSky ? 1f / StyleSkyOcclusion.Resolution : 0f,
                                              (float)(time % 1000.0));
            passData.rainVelocity = rainVelocity;
            passData.split = new Vector4(split, StyleSkyOcclusion.Depth, 0f, 0f);
            passData.rainColor = weather.rainColor.value;
            passData.maskCount = HBStyleWeatherMask.Collect(cameraData.camera.transform.position, WindBox * 0.5f,
                                                          passData.masks, m_MaskDistances, true);
            passData.windStreak = new Vector4(Mathf.Sin(heading), Mathf.Cos(heading),
                                              Mathf.Lerp(WindSlow, WindFast, weather.streaks), weather.streaks);
            passData.ribbons = hasSky ? Mathf.RoundToInt(weather.streaks * WindRibbons) : 0;

            var level = new HBStylePrecipitationLevel(1f, true);
            var drawn = m_Settings == null || m_Settings.TryGetLevel(out level);

            for (var i = 0; i < s_Layers.Length; i++)
            {
                var layer = s_Layers[i];
                var rain = layer.pass == RainPass;
                var amount = rain ? weather.rain.value : weather.snow.value;

                if (!drawn || (layer.far && !level.far))
                    amount = 0f;
                else
                    amount *= level.density;

                passData.boxes[i] = new Vector4(layer.box, layer.innerStart, layer.innerEnd, layer.sizeScale);
                passData.offsets[i] = Offset(rain ? rainVelocity : snowVelocity, time, layer.box, layer.seed);
                passData.counts[i] = Mathf.RoundToInt(amount * layer.maxCount);
            }

            if (hasSky)
                builder.UseTexture(occlusion.texture, AccessFlags.Read);

            if (b0.IsValid())
            {
                builder.UseTexture(b0, AccessFlags.Read);
                builder.UseTexture(moments, AccessFlags.Read);
                builder.UseTexture(behindMoments, AccessFlags.Read);
                builder.UseTexture(info, AccessFlags.Read);
            }

            builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.SetRenderAttachment(target, 0, AccessFlags.ReadWrite);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                var cmd = context.cmd;

                if (data.sky.IsValid())
                    cmd.SetGlobalTexture(s_SkyOcclusionId, data.sky);

                cmd.SetGlobalMatrix(s_SkyViewProjId, data.skyViewProjection);
                cmd.SetGlobalVector(s_ParamsId, data.parameters);
                cmd.SetGlobalVector(s_RainVelocityId, data.rainVelocity);
                cmd.SetGlobalVector(s_SplitId, data.split);
                cmd.SetGlobalColor(s_RainColorId, data.rainColor);
                cmd.SetGlobalVector(s_WindStreakId, data.windStreak);
                cmd.SetGlobalFloat(s_MaskCountId, data.maskCount);
                cmd.SetGlobalVectorArray(s_MasksId, data.masks);

                for (var i = 0; i < s_Layers.Length; i++)
                {
                    if (data.counts[i] <= 0)
                        continue;

                    cmd.SetGlobalVector(s_BoxId, data.boxes[i]);
                    cmd.SetGlobalVector(s_OffsetId, data.offsets[i]);
                    cmd.DrawProcedural(Matrix4x4.identity, data.material, s_Layers[i].pass, MeshTopology.Triangles,
                                       data.counts[i] * 6);
                }

                if (data.ribbons > 0)
                {
                    cmd.SetGlobalVector(s_BoxId, new Vector4(WindBox, WindInnerStart, WindInnerEnd, 1f));
                    cmd.SetGlobalVector(s_OffsetId, Vector4.zero);
                    cmd.DrawProcedural(Matrix4x4.identity, data.material, WindPass, MeshTopology.Triangles,
                                       data.ribbons * WindSegments * 6);
                }
            });

            return true;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);

            m_Material = null;
        }
    }
}
