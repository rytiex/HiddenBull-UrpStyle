using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle
{
    sealed class StyleSkyLut : IDisposable
    {
        internal const int Width = 256;

        const int Rows = 3;
        const int Slices = 3;
        const int Count = Width * Rows;

        readonly Color[] m_Ambient = new Color[Count];
        readonly Color[] m_Sky = new Color[Count];
        readonly Color[] m_Fog = new Color[Count];
        readonly Color[] m_Ground = new Color[Rows];

        Texture2DArray m_Texture;
        bool m_Dirty = true;

        HBStyleEnvironmentProfile m_Profile;
        int m_Version = -1;
        int m_Bakes;

        internal int mixKey = int.MinValue;

        internal int version => m_Bakes;

        internal Texture texture
        {
            get
            {
                if (m_Texture == null)
                {
                    m_Texture = new Texture2DArray(Width, Rows, Slices, TextureFormat.RGBAHalf, false, true)
                    {
                        name = "HB Style Sky LUT",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    m_Dirty = true;
                }

                if (m_Dirty)
                {
                    m_Texture.SetPixels(m_Ambient, 0);
                    m_Texture.SetPixels(m_Sky, 1);
                    m_Texture.SetPixels(m_Fog, 2);
                    m_Texture.Apply(false, false);
                    m_Dirty = false;
                }

                return m_Texture;
            }
        }

        internal void Bake(HBStyleEnvironmentProfile profile)
        {
            if (profile == m_Profile && profile.version == m_Version)
                return;

            m_Profile = profile;
            m_Version = profile.version;

            var sky = profile.sky;
            var atmosphere = profile.atmosphere;

            Write(m_Ambient, 0, sky.dayAmbient ?? HBStyleSkySettings.DefaultDayAmbient());
            Write(m_Ambient, 1, sky.duskAmbient ?? HBStyleSkySettings.DefaultDuskAmbient());
            Write(m_Ambient, 2, sky.nightAmbient ?? HBStyleSkySettings.DefaultNightAmbient());
            Write(m_Sky, 0, sky.daySky ?? HBStyleSkySettings.DefaultDaySky());
            Write(m_Sky, 1, sky.duskSky ?? HBStyleSkySettings.DefaultDuskSky());
            Write(m_Sky, 2, sky.nightSky ?? HBStyleSkySettings.DefaultNightSky());
            Write(m_Fog, 0, atmosphere.dayFog ?? HBStyleSkySettings.DefaultDayAmbient());
            Write(m_Fog, 1, atmosphere.duskFog ?? HBStyleSkySettings.DefaultDuskAmbient());
            Write(m_Fog, 2, atmosphere.nightFog ?? HBStyleAtmosphereSettings.DefaultNightFog());

            for (var row = 0; row < Rows; row++)
                m_Ground[row] = m_Ambient[row * Width];

            m_Bakes++;
            m_Dirty = true;
        }

        internal void CopyFrom(StyleSkyLut source)
        {
            Array.Copy(source.m_Ambient, m_Ambient, Count);
            Array.Copy(source.m_Sky, m_Sky, Count);
            Array.Copy(source.m_Fog, m_Fog, Count);
            Array.Copy(source.m_Ground, m_Ground, Rows);

            m_Profile = null;
            m_Dirty = true;
        }

        internal void BlendToward(StyleSkyLut target, float sky, float fog)
        {
            if (sky > 0f)
            {
                Lerp(m_Ambient, target.m_Ambient, sky);
                Lerp(m_Sky, target.m_Sky, sky);
                Lerp(m_Ground, target.m_Ground, sky);
            }

            if (fog > 0f)
                Lerp(m_Fog, target.m_Fog, fog);

            m_Dirty |= sky > 0f || fog > 0f;
        }

        internal static float RowCoordinate(float blend)
        {
            return (Mathf.Clamp01(blend) * (Rows - 1) + 0.5f) / Rows;
        }

        internal Color Ground(float blend)
        {
            var scaled = Mathf.Clamp01(blend) * (Rows - 1);
            var lower = Mathf.FloorToInt(scaled);
            var upper = Mathf.Min(lower + 1, Rows - 1);

            return Color.Lerp(m_Ground[lower], m_Ground[upper], scaled - lower);
        }

        static void Lerp(Color[] current, Color[] target, float weight)
        {
            for (var i = 0; i < current.Length; i++)
                current[i] = Color.LerpUnclamped(current[i], target[i], weight);
        }

        static void Write(Color[] pixels, int row, Gradient gradient)
        {
            for (var i = 0; i < Width; i++)
            {
                var colour = StyleKeyLight.Linear(gradient.Evaluate(i / (Width - 1f)));
                pixels[row * Width + i] = new Color(colour.r, colour.g, colour.b, 1f);
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Texture);
            m_Texture = null;
            m_Profile = null;
        }
    }
}
