using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [CustomEditor(typeof(HBStyleWeather))]
    sealed class StyleWeatherEditor : StyleVolumeEditor
    {
        readonly struct Preset
        {
            public readonly GUIContent label;
            public readonly float rain;
            public readonly float snow;
            public readonly float wind;

            public Preset(string name, string tooltip, float rain, float snow, float wind)
            {
                label = new GUIContent(name, tooltip);
                this.rain = rain;
                this.snow = snow;
                this.wind = wind;
            }
        }

        static readonly Preset[] s_Presets =
        {
            new Preset("Clear", "No rain or snow, a light breeze.", 0f, 0f, 0.1f),
            new Preset("Drizzle", "Light rain, barely any wind.", 0.15f, 0f, 0.1f),
            new Preset("Rain", "Steady rain.", 0.5f, 0f, 0.2f),
            new Preset("Storm", "Heavy rain driven by a strong wind.", 1f, 0f, 0.8f),
            new Preset("Light Snow", "A few flakes drifting down.", 0f, 0.3f, 0.1f),
            new Preset("Blizzard", "Heavy snow in a strong wind.", 0f, 1f, 0.9f),
            new Preset("Sleet", "Rain and snow together.", 0.4f, 0.4f, 0.3f)
        };

        const int PresetsPerRow = 4;

        static readonly GUIContent s_PresetsLabel = new GUIContent(
            "Presets",
            "Sets Rain, Snow and Wind in one go. Everything else, and the wind direction, is left alone.");

        public override void OnInspectorGUI()
        {
            DrawPresets();
            DrawStatus();

            Section("Precipitation", "rain", "snow", "wind", "windHeading", "rainColor");
            Section("Surfaces", "instant", "speed", "wetTime", "dryTime", "settleTime", "meltTime");
            Section("Shelter", "coverTime", "uncoverTime");
        }

        void DrawPresets()
        {
            EditorGUILayout.LabelField(s_PresetsLabel, EditorStyles.miniBoldLabel);

            for (var start = 0; start < s_Presets.Length; start += PresetsPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var i = start; i < Mathf.Min(start + PresetsPerRow, s_Presets.Length); i++)
                    {
                        var preset = s_Presets[i];

                        if (!GUILayout.Button(preset.label, EditorStyles.miniButton))
                            continue;

                        SetValue("rain", preset.rain);
                        SetValue("snow", preset.snow);
                        SetValue("wind", preset.wind);
                    }
                }
            }
        }

        void DrawStatus()
        {
            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space(2f);

            var wet = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(wet, StyleWeatherState.wetness, $"Surfaces wet  {StyleWeatherState.wetness:P0}");

            var snow = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(snow, StyleWeatherState.snow, $"Snow settled  {StyleWeatherState.snow:P0}");

            m_Inspector?.Repaint();
        }
    }
}
