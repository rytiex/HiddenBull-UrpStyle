using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class SkyLutBaker : IDisposable
    {
        public const int Width = 256;

        const int AmbientRow = 0;
        const int SkyRow = 1;
        const int SkyAwayRow = 2;

        static readonly bool s_LinearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;

        static int s_Revision;

        static readonly Gradient s_FallbackDaySky = StyleSky.DefaultDaySky();
        static readonly Gradient s_FallbackDuskSky = StyleSky.DefaultDuskSky();
        static readonly Gradient s_FallbackNightSky = StyleSky.DefaultNightSky();
        static readonly Gradient s_FallbackDayAmbient = StyleSky.DefaultDayAmbient();
        static readonly Gradient s_FallbackDuskAmbient = StyleSky.DefaultDuskAmbient();
        static readonly Gradient s_FallbackNightAmbient = StyleSky.DefaultNightAmbient();

        readonly Color[] m_Pixels = new Color[Width * 3];
        readonly Gradient[] m_Sources = new Gradient[6];

        Texture2D m_Texture;
        float m_Blend = float.NaN;
        float m_Away = float.NaN;
        float m_SkyIntensity = float.NaN;
        float m_AmbientIntensity = float.NaN;
        int m_Revision = -1;

        public static void Invalidate()
        {
            s_Revision++;
        }

        public Texture2D texture => m_Texture;

        public static Vector2 ScaleOffset => new Vector2((Width - 1f) / Width, 0.5f / Width);

        public Texture2D Bake(
            Gradient daySky, Gradient duskSky, Gradient nightSky,
            Gradient dayAmbient, Gradient duskAmbient, Gradient nightAmbient,
            float blend, float away, float skyIntensity, float ambientIntensity)
        {
            daySky ??= s_FallbackDaySky;
            duskSky ??= s_FallbackDuskSky;
            nightSky ??= s_FallbackNightSky;
            dayAmbient ??= s_FallbackDayAmbient;
            duskAmbient ??= s_FallbackDuskAmbient;
            nightAmbient ??= s_FallbackNightAmbient;

            if (!NeedsBake(daySky, duskSky, nightSky, dayAmbient, duskAmbient, nightAmbient,
                           blend, away, skyIntensity, ambientIntensity))
                return m_Texture;

            if (m_Texture == null)
            {
                m_Texture = new Texture2D(Width, 3, TextureFormat.RGBAHalf, false, true)
                {
                    name = "HB Sky LUT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            for (var i = 0; i < Width; i++)
            {
                var position = i / (Width - 1f);

                m_Pixels[AmbientRow * Width + i] = Resolve(
                    dayAmbient, duskAmbient, nightAmbient, position, blend, ambientIntensity);

                m_Pixels[SkyRow * Width + i] = Resolve(
                    daySky, duskSky, nightSky, position, blend, skyIntensity);

                m_Pixels[SkyAwayRow * Width + i] = Resolve(
                    daySky, duskSky, nightSky, position, away, skyIntensity);
            }

            m_Texture.SetPixels(m_Pixels);
            m_Texture.Apply(false, false);

            return m_Texture;
        }

        bool NeedsBake(
            Gradient daySky, Gradient duskSky, Gradient nightSky,
            Gradient dayAmbient, Gradient duskAmbient, Gradient nightAmbient,
            float blend, float away, float skyIntensity, float ambientIntensity)
        {
            var stale = m_Texture == null
                        || s_Revision != m_Revision
                        || blend != m_Blend
                        || away != m_Away
                        || skyIntensity != m_SkyIntensity
                        || ambientIntensity != m_AmbientIntensity
                        || !ReferenceEquals(daySky, m_Sources[0])
                        || !ReferenceEquals(duskSky, m_Sources[1])
                        || !ReferenceEquals(nightSky, m_Sources[2])
                        || !ReferenceEquals(dayAmbient, m_Sources[3])
                        || !ReferenceEquals(duskAmbient, m_Sources[4])
                        || !ReferenceEquals(nightAmbient, m_Sources[5]);

            m_Revision = s_Revision;
            m_Blend = blend;
            m_Away = away;
            m_SkyIntensity = skyIntensity;
            m_AmbientIntensity = ambientIntensity;
            m_Sources[0] = daySky;
            m_Sources[1] = duskSky;
            m_Sources[2] = nightSky;
            m_Sources[3] = dayAmbient;
            m_Sources[4] = duskAmbient;
            m_Sources[5] = nightAmbient;

            return stale || Application.isEditor;
        }

        static Color Resolve(Gradient day, Gradient dusk, Gradient night,
                             float position, float blend, float intensity)
        {
            var authored = blend < 0.5f
                ? Color.Lerp(day.Evaluate(position), dusk.Evaluate(position), blend * 2f)
                : Color.Lerp(dusk.Evaluate(position), night.Evaluate(position), (blend - 0.5f) * 2f);

            var color = s_LinearColorSpace ? authored.linear : authored;

            return new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Texture);
            m_Texture = null;
            m_Blend = float.NaN;
            Array.Clear(m_Sources, 0, m_Sources.Length);
        }
    }
}
