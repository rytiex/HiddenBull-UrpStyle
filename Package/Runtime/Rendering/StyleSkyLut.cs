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
        readonly Color[] m_FogClear = new Color[Count];
        readonly Color[] m_FogRain = new Color[Count];
        readonly Color[] m_FogSnow = new Color[Count];
        readonly Color[] m_Ground = new Color[Rows];

        Texture2DArray m_Texture;
        bool m_Dirty = true;

        HBStyleEnvironmentProfile m_Profile;
        int m_Version = -1;
        HBStyleAtmosphereProfile m_FogProfile;
        int m_FogVersion = -1;
        Vector3 m_FogWeights = -Vector3.one;
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
            var fog = profile.atmosphere;
            var fogVersion = fog != null ? fog.version : 0;
            var weather = StyleEnvironment.weather;
            var weights = Quantise(HBStyleAtmosphereProfile.Weights(weather.x, weather.y));
            var changed = false;

            if (profile != m_Profile || profile.version != m_Version)
            {
                m_Profile = profile;
                m_Version = profile.version;

                var sky = profile.sky;

                Write(m_Ambient, 0, sky.dayAmbient ?? HBStyleSkySettings.DefaultDayAmbient());
                Write(m_Ambient, 1, sky.duskAmbient ?? HBStyleSkySettings.DefaultDuskAmbient());
                Write(m_Ambient, 2, sky.nightAmbient ?? HBStyleSkySettings.DefaultNightAmbient());
                Write(m_Sky, 0, sky.daySky ?? HBStyleSkySettings.DefaultDaySky());
                Write(m_Sky, 1, sky.duskSky ?? HBStyleSkySettings.DefaultDuskSky());
                Write(m_Sky, 2, sky.nightSky ?? HBStyleSkySettings.DefaultNightSky());

                for (var row = 0; row < Rows; row++)
                    m_Ground[row] = m_Ambient[row * Width];

                changed = true;
            }

            if (changed || fog != m_FogProfile || fogVersion != m_FogVersion)
            {
                m_FogProfile = fog;
                m_FogVersion = fogVersion;

                WriteFog(m_FogClear, fog != null ? fog.clear : null);
                WriteFog(m_FogRain, fog != null ? fog.rain : null);
                WriteFog(m_FogSnow, fog != null ? fog.snow : null);

                changed = true;
            }

            if (changed || weights != m_FogWeights)
            {
                m_FogWeights = weights;

                for (var i = 0; i < Count; i++)
                    m_Fog[i] = m_FogClear[i] * weights.x + m_FogRain[i] * weights.y + m_FogSnow[i] * weights.z;

                changed = true;
            }

            if (!changed)
                return;

            m_Bakes++;
            m_Dirty = true;
        }

        static Vector3 Quantise(Vector3 weights)
        {
            return new Vector3(Mathf.Round(weights.x * 128f), Mathf.Round(weights.y * 128f),
                               Mathf.Round(weights.z * 128f)) / 128f;
        }

        static void WriteFog(Color[] pixels, HBStyleFogState state)
        {
            Write(pixels, 0, state?.dayFog ?? HBStyleSkySettings.DefaultDayAmbient());
            Write(pixels, 1, state?.duskFog ?? HBStyleSkySettings.DefaultDuskAmbient());
            Write(pixels, 2, state?.nightFog ?? HBStyleFogState.DefaultNightFog());
        }

        internal void CopyFrom(StyleSkyLut source)
        {
            Array.Copy(source.m_Ambient, m_Ambient, Count);
            Array.Copy(source.m_Sky, m_Sky, Count);
            Array.Copy(source.m_Fog, m_Fog, Count);
            Array.Copy(source.m_Ground, m_Ground, Rows);

            m_Profile = null;
            m_FogProfile = null;
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

        internal Color Ambient(float blend, float up)
        {
            var scaled = Mathf.Clamp01(blend) * (Rows - 1);
            var lower = Mathf.FloorToInt(scaled);
            var upper = Mathf.Min(lower + 1, Rows - 1);
            var column = Mathf.RoundToInt(Mathf.Clamp01(up * 0.5f + 0.5f) * (Width - 1));

            return Color.Lerp(m_Ambient[lower * Width + column], m_Ambient[upper * Width + column], scaled - lower);
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
            m_FogProfile = null;
        }
    }
}
