using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class SkyLutBaker : IDisposable
    {
        public const int Width = 256;

        const int Rows = 3;
        const int Slices = 3;

        const int AmbientSlice = 0;
        const int SkySlice = 1;
        const int FogSlice = 2;

        const int DayRow = 0;
        const int DuskRow = 1;
        const int NightRow = 2;

        static readonly bool s_LinearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;

        static int s_Revision;

        static readonly Gradient s_FallbackDaySky = StyleSky.DefaultDaySky();
        static readonly Gradient s_FallbackDuskSky = StyleSky.DefaultDuskSky();
        static readonly Gradient s_FallbackNightSky = StyleSky.DefaultNightSky();
        static readonly Gradient s_FallbackDayAmbient = StyleSky.DefaultDayAmbient();
        static readonly Gradient s_FallbackDuskAmbient = StyleSky.DefaultDuskAmbient();
        static readonly Gradient s_FallbackNightAmbient = StyleSky.DefaultNightAmbient();
        static readonly Gradient s_FallbackDayFog = StyleFog.DefaultDayFog();
        static readonly Gradient s_FallbackDuskFog = StyleFog.DefaultDuskFog();
        static readonly Gradient s_FallbackNightFog = StyleFog.DefaultNightFog();

        readonly Color[] m_Slice = new Color[Width * Rows];
        readonly Gradient[] m_Sources = new Gradient[9];
        readonly Color[] m_Ground = new Color[Rows];
        readonly Color[] m_Horizon = new Color[Rows];

        Texture2DArray m_Texture;
        int m_Revision = -1;

        public static void Invalidate()
        {
            s_Revision++;
        }

        public Texture2DArray texture => m_Texture;

        public static Vector2 ScaleOffset => new Vector2((Width - 1f) / Width, 0.5f / Width);

        public static float TimeCoordinate(float blend)
        {
            return (Mathf.Clamp01(blend) * (Rows - 1) + 0.5f) / Rows;
        }

        public Color GroundAmbient(float blend, float intensity)
        {
            var scaled = Mathf.Clamp01(blend) * (Rows - 1);
            var lower = Mathf.FloorToInt(scaled);
            var upper = Mathf.Min(lower + 1, Rows - 1);

            return Color.Lerp(m_Ground[lower], m_Ground[upper], scaled - lower) * intensity;
        }

        public Color HorizonAmbient(float blend, float intensity)
        {
            var scaled = Mathf.Clamp01(blend) * (Rows - 1);
            var lower = Mathf.FloorToInt(scaled);
            var upper = Mathf.Min(lower + 1, Rows - 1);

            return Color.Lerp(m_Horizon[lower], m_Horizon[upper], scaled - lower) * intensity;
        }

        public Texture2DArray Bake(
            Gradient daySky, Gradient duskSky, Gradient nightSky,
            Gradient dayAmbient, Gradient duskAmbient, Gradient nightAmbient,
            Gradient dayFog, Gradient duskFog, Gradient nightFog)
        {
            daySky ??= s_FallbackDaySky;
            duskSky ??= s_FallbackDuskSky;
            nightSky ??= s_FallbackNightSky;
            dayAmbient ??= s_FallbackDayAmbient;
            duskAmbient ??= s_FallbackDuskAmbient;
            nightAmbient ??= s_FallbackNightAmbient;
            dayFog ??= s_FallbackDayFog;
            duskFog ??= s_FallbackDuskFog;
            nightFog ??= s_FallbackNightFog;

            if (!NeedsBake(daySky, duskSky, nightSky, dayAmbient, duskAmbient, nightAmbient,
                           dayFog, duskFog, nightFog))
                return m_Texture;

            if (m_Texture == null)
            {
                m_Texture = new Texture2DArray(Width, Rows, Slices, TextureFormat.RGBAHalf, false, true)
                {
                    name = "HB Sky LUT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            WriteSlice(AmbientSlice, dayAmbient, duskAmbient, nightAmbient);
            WriteSlice(SkySlice, daySky, duskSky, nightSky);
            WriteSlice(FogSlice, dayFog, duskFog, nightFog);

            m_Ground[DayRow] = Convert(dayAmbient.Evaluate(0f));
            m_Ground[DuskRow] = Convert(duskAmbient.Evaluate(0f));
            m_Ground[NightRow] = Convert(nightAmbient.Evaluate(0f));

            m_Horizon[DayRow] = Convert(dayAmbient.Evaluate(0.5f));
            m_Horizon[DuskRow] = Convert(duskAmbient.Evaluate(0.5f));
            m_Horizon[NightRow] = Convert(nightAmbient.Evaluate(0.5f));

            m_Texture.Apply(false, false);

            return m_Texture;
        }

        void WriteSlice(int slice, Gradient day, Gradient dusk, Gradient night)
        {
            for (var i = 0; i < Width; i++)
            {
                var position = i / (Width - 1f);

                m_Slice[DayRow * Width + i] = Convert(day.Evaluate(position));
                m_Slice[DuskRow * Width + i] = Convert(dusk.Evaluate(position));
                m_Slice[NightRow * Width + i] = Convert(night.Evaluate(position));
            }

            m_Texture.SetPixels(m_Slice, slice);
        }

        bool NeedsBake(
            Gradient daySky, Gradient duskSky, Gradient nightSky,
            Gradient dayAmbient, Gradient duskAmbient, Gradient nightAmbient,
            Gradient dayFog, Gradient duskFog, Gradient nightFog)
        {
            var stale = m_Texture == null
                        || s_Revision != m_Revision
                        || !ReferenceEquals(daySky, m_Sources[0])
                        || !ReferenceEquals(duskSky, m_Sources[1])
                        || !ReferenceEquals(nightSky, m_Sources[2])
                        || !ReferenceEquals(dayAmbient, m_Sources[3])
                        || !ReferenceEquals(duskAmbient, m_Sources[4])
                        || !ReferenceEquals(nightAmbient, m_Sources[5])
                        || !ReferenceEquals(dayFog, m_Sources[6])
                        || !ReferenceEquals(duskFog, m_Sources[7])
                        || !ReferenceEquals(nightFog, m_Sources[8]);

            m_Revision = s_Revision;
            m_Sources[0] = daySky;
            m_Sources[1] = duskSky;
            m_Sources[2] = nightSky;
            m_Sources[3] = dayAmbient;
            m_Sources[4] = duskAmbient;
            m_Sources[5] = nightAmbient;
            m_Sources[6] = dayFog;
            m_Sources[7] = duskFog;
            m_Sources[8] = nightFog;

            return stale || (Application.isEditor && !Application.isPlaying);
        }

        static Color Convert(Color authored)
        {
            var color = s_LinearColorSpace ? authored.linear : authored;

            return new Color(color.r, color.g, color.b, 1f);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Texture);
            m_Texture = null;
            Array.Clear(m_Sources, 0, m_Sources.Length);
        }
    }
}
