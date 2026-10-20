using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleBakePanel : EditorWindow
    {
        const string KeyPrefix = "HiddenBull.UrpStyle.BakePanel.";

        static class Styles
        {
            public static readonly GUIContent Lighting = new GUIContent("Lighting");
            public static readonly GUIContent Checks = new GUIContent("Checks");
            public static readonly GUIContent Bake = new GUIContent("Bake");

            public static readonly GUIContent Quality = new GUIContent(
                "Quality",
                "Fills the settings below in one go. Default is the tuned set: fast and clean. Ultra doubles " +
                "the resolution, atlas size, samples and bounces; Low halves them and Potato quarters them, " +
                "for quick previews. Change any value afterwards and this reads Custom.");

            public static readonly GUIContent[] Qualities =
            {
                new GUIContent("Potato"), new GUIContent("Low"), new GUIContent("Default"), new GUIContent("Ultra"),
                new GUIContent("Custom")
            };

            public static readonly GUIContent Lightmapper = new GUIContent("Lightmapper");
            public static readonly GUIContent DirectSamples = new GUIContent("Direct Samples");
            public static readonly GUIContent IndirectSamples = new GUIContent("Indirect Samples");
            public static readonly GUIContent EnvironmentSamples = new GUIContent("Environment Samples");
            public static readonly GUIContent ProbeSamples = new GUIContent("Light Probe Sample Multiplier");
            public static readonly GUIContent Bounces = new GUIContent("Max Bounces");
            public static readonly GUIContent Resolution = new GUIContent("Lightmap Resolution", "Texels per unit.");
            public static readonly GUIContent MaxSize = new GUIContent("Max Lightmap Size");
            public static readonly GUIContent Compression = new GUIContent("Lightmap Compression");
            public static readonly GUIContent Directional = new GUIContent("Directional Mode");
            public static readonly GUIContent MixedMode = new GUIContent("Mixed Lighting Mode");

            public static readonly GUIContent MipmapLimits = new GUIContent(
                "Use Mipmap Limits",
                "Off: after every bake the lightmaps lose their mipmaps, so distant and grazing surfaces read " +
                "the full-resolution light instead of a smaller copy where neighbouring charts bleed into " +
                "each other. On: lightmaps keep their mipmaps and follow the quality mipmap limits.");

            public static readonly GUIContent Packing = new GUIContent(
                "Packing Method",
                "How lightmap charts are packed into atlases. XAtlas packs tighter and keeps charts on 4x4 " +
                "texel blocks, so compression does not mix the light of neighbouring charts. Unity is the " +
                "older packer, kept for projects baked with earlier versions.");

            public static readonly GUIContent[] PackingNames = { new GUIContent("Unity"), new GUIContent("XAtlas") };
            public static readonly int[] PackingValues = { 0, 1 };

            public static readonly GUIContent[] SizeNames =
            {
                new GUIContent("32"), new GUIContent("64"), new GUIContent("128"), new GUIContent("256"),
                new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"), new GUIContent("4096")
            };
            public static readonly int[] Sizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
        }

        readonly struct Check
        {
            public readonly string label;
            public readonly bool ok;
            public readonly Action fix;

            public Check(string label, bool ok, Action fix)
            {
                this.label = label;
                this.ok = ok;
                this.fix = fix;
            }
        }

        Vector2 m_Scroll;
        List<Material> m_SingleSided;
        double m_SingleSidedTime;
        double m_BakeStart;
        string m_Report;

        [MenuItem("Tools/HiddenBull/URP Style/Bake Panel")]
        static void Open()
        {
            GetWindow<StyleBakePanel>("HB Bake");
        }

        void OnEnable()
        {
            Lightmapping.bakeStarted += OnBakeStarted;
            Lightmapping.bakeCompleted += OnBakeCompleted;
            EditorApplication.update += Tick;
        }

        void OnDisable()
        {
            Lightmapping.bakeStarted -= OnBakeStarted;
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            EditorApplication.update -= Tick;
        }

        void Tick()
        {
            if (Lightmapping.isRunning)
                Repaint();
        }

        void OnBakeStarted()
        {
            m_BakeStart = EditorApplication.timeSinceStartup;
        }

        void OnBakeCompleted()
        {
            var seconds = m_BakeStart > 0.0 ? EditorApplication.timeSinceStartup - m_BakeStart : 0.0;
            var lightmaps = LightmapSettings.lightmaps;
            long bytes = 0;

            foreach (var lightmap in lightmaps)
            {
                bytes += Size(lightmap.lightmapColor) + Size(lightmap.lightmapDir) + Size(lightmap.shadowMask);
            }

            m_Report = $"Baked in {TimeSpan.FromSeconds(seconds):mm\\:ss} — {lightmaps.Length} lightmap(s), " +
                       $"{bytes / (1024f * 1024f):0.0} MB.";
            m_BakeStart = 0.0;
            Repaint();
        }

        static long Size(Texture texture) => texture != null ? Profiler.GetRuntimeMemorySizeLong(texture) : 0;

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            Lightmapping.TryGetLightingSettings(out var settings);

            if (Section(Styles.Lighting, "Lighting"))
                DrawLighting(settings);

            if (Section(Styles.Checks, "Checks"))
                DrawChecks(settings);

            if (Section(Styles.Bake, "Bake"))
                DrawBake(settings);

            CoreEditorUtils.DrawSplitter();
            EditorGUILayout.EndScrollView();
        }

        static bool Section(GUIContent title, string key)
        {
            var expanded = EditorPrefs.GetBool(KeyPrefix + key, true);

            CoreEditorUtils.DrawSplitter();

            var next = CoreEditorUtils.DrawHeaderFoldout(title, expanded);

            if (next != expanded)
                EditorPrefs.SetBool(KeyPrefix + key, next);

            if (next)
                EditorGUILayout.Space(2f);

            return next;
        }

        void DrawLighting(LightingSettings settings)
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("This scene has no Lighting Settings asset yet.", MessageType.Info);

                if (GUILayout.Button("Create with HB Defaults"))
                    CreateSettings();

                return;
            }

            DrawQuality(settings);

            EditorGUILayout.Space(2f);
            EditorGUI.BeginChangeCheck();

            var lightmapper = (LightingSettings.Lightmapper)EditorGUILayout.EnumPopup(Styles.Lightmapper, settings.lightmapper);
            var direct = EditorGUILayout.IntSlider(Styles.DirectSamples, settings.directSampleCount, 1, 1024);
            var indirect = EditorGUILayout.IntSlider(Styles.IndirectSamples, settings.indirectSampleCount, 8, 2048);
            var environment = EditorGUILayout.IntSlider(Styles.EnvironmentSamples, settings.environmentSampleCount, 8, 2048);
            var probes = EditorGUILayout.FloatField(Styles.ProbeSamples, settings.lightProbeSampleCountMultiplier);
            var bounces = EditorGUILayout.IntSlider(Styles.Bounces, settings.maxBounces, 0, 10);
            var resolution = EditorGUILayout.FloatField(Styles.Resolution, settings.lightmapResolution);
            var maxSize = EditorGUILayout.IntPopup(Styles.MaxSize, settings.lightmapMaxSize, Styles.SizeNames, Styles.Sizes);
            var compression = (LightmapCompression)EditorGUILayout.EnumPopup(Styles.Compression, settings.lightmapCompression);
            var directional = (LightmapsMode)EditorGUILayout.EnumPopup(Styles.Directional, settings.directionalityMode);
            var mixed = (MixedLightingMode)EditorGUILayout.EnumPopup(Styles.MixedMode, settings.mixedBakeMode);

            var serialized = new SerializedObject(settings);
            var packingMode = serialized.FindProperty("m_LightmapPackingMode");
            var packingMethod = serialized.FindProperty("m_LightmapPackingMethod");
            var mipmapLimits = serialized.FindProperty("m_UseMipmapLimits");

            if (packingMode != null && packingMethod != null)
            {
                var method = EditorGUILayout.IntPopup(Styles.Packing, packingMethod.intValue, Styles.PackingNames,
                                                      Styles.PackingValues);

                if (method != packingMethod.intValue)
                {
                    packingMode.intValue = StyleBakeDefaults.CustomPacking;
                    packingMethod.intValue = method;
                }
            }

            if (mipmapLimits != null)
                EditorGUILayout.PropertyField(mipmapLimits, Styles.MipmapLimits);

            serialized.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Lighting Settings");

                settings.lightmapper = lightmapper;
                settings.directSampleCount = direct;
                settings.indirectSampleCount = indirect;
                settings.environmentSampleCount = environment;
                settings.lightProbeSampleCountMultiplier = Mathf.Max(probes, 0f);
                settings.maxBounces = bounces;
                settings.lightmapResolution = Mathf.Max(resolution, 0.1f);
                settings.lightmapMaxSize = maxSize;
                settings.lightmapCompression = compression;
                settings.directionalityMode = directional;
                settings.mixedBakeMode = mixed;

                EditorUtility.SetDirty(settings);
            }

            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Lighting Window", GUILayout.Width(160f)))
                    EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
            }
        }

        static void DrawQuality(LightingSettings settings)
        {
            var current = Styles.Qualities.Length - 1;

            for (var i = 0; i < Styles.Qualities.Length - 1; i++)
            {
                if (StyleBakeDefaults.Matches(settings, (StyleBakeQuality)i))
                {
                    current = i;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();

            var chosen = EditorGUILayout.Popup(Styles.Quality, current, Styles.Qualities);

            if (EditorGUI.EndChangeCheck() && chosen < Styles.Qualities.Length - 1)
            {
                Undo.RecordObject(settings, "Bake Quality");
                StyleBakeDefaults.Apply(settings, (StyleBakeQuality)chosen);
                EditorUtility.SetDirty(settings);
            }
        }

        static void CreateSettings()
        {
            var scene = SceneManager.GetActiveScene();
            var settings = new LightingSettings { name = scene.name + " Lighting" };

            StyleBakeDefaults.Apply(settings);

            var folder = string.IsNullOrEmpty(scene.path) ? "Assets" : Path.GetDirectoryName(scene.path);
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, settings.name + ".lighting")
                                                                 .Replace('\\', '/'));

            AssetDatabase.CreateAsset(settings, path);
            Lightmapping.lightingSettings = settings;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        void DrawChecks(LightingSettings settings)
        {
            var sun = StyleSunInstaller.FindSun();
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            var checks = new List<Check>
            {
                new Check("Scene has a Lighting Settings asset", settings != null, CreateSettings),
                new Check("Baked Global Illumination is on", settings != null && settings.bakedGI,
                          () => Change(settings, s => s.bakedGI = true)),
                new Check("Mixed Lighting Mode is Shadowmask",
                          settings != null && settings.mixedBakeMode == MixedLightingMode.Shadowmask,
                          () => Change(settings, s => s.mixedBakeMode = MixedLightingMode.Shadowmask)),
                new Check("URP asset supports Mixed Lighting", asset != null && asset.supportsMixedLighting,
                          () => EnableMixedLighting(asset)),
                new Check("Scene has a directional sun", sun != null, null),
                new Check("Sun has a Style Sun", sun != null && sun.TryGetComponent<HBStyleSun>(out _),
                          sun != null ? () => StyleSunInstaller.Add(sun) : null),
                new Check("Sun is Mixed", sun != null && sun.lightmapBakeType == LightmapBakeType.Mixed,
                          sun != null ? () => ChangeLight(sun, l => l.lightmapBakeType = LightmapBakeType.Mixed) : null),
                new Check("Sun casts shadows", sun != null && sun.shadows != LightShadows.None,
                          sun != null ? () => ChangeLight(sun, l => l.shadows = LightShadows.Soft) : null),
                new Check(SingleSidedLabel(), SingleSidedMaterials().Count == 0, EnableDoubleSidedGI)
            };

            foreach (var check in checks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var icon = EditorGUIUtility.IconContent(check.ok ? "TestPassed" : "console.warnicon.sml");
                    GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(18f));
                    EditorGUILayout.LabelField(check.label);

                    using (new EditorGUI.DisabledScope(check.ok || check.fix == null))
                    {
                        if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(50f)))
                            check.fix();
                    }
                }
            }
        }

        string SingleSidedLabel()
        {
            var count = SingleSidedMaterials().Count;

            return count == 0
                ? "Lightmapped materials use Double Sided GI"
                : $"{count} lightmapped material(s) without Double Sided GI (light leaks at joins)";
        }

        List<Material> SingleSidedMaterials()
        {
            var now = EditorApplication.timeSinceStartup;

            if (m_SingleSided != null && now - m_SingleSidedTime < 1.0)
                return m_SingleSided;

            m_SingleSidedTime = now;
            m_SingleSided = new List<Material>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if ((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) &
                             StaticEditorFlags.ContributeGI) == 0 || renderer.receiveGI != ReceiveGI.Lightmaps)
                            continue;

                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material != null && !material.doubleSidedGI && !m_SingleSided.Contains(material))
                                m_SingleSided.Add(material);
                        }
                    }
                }
            }

            return m_SingleSided;
        }

        void EnableDoubleSidedGI()
        {
            var materials = SingleSidedMaterials();

            Undo.RecordObjects(materials.ToArray(), "Double Sided GI");

            foreach (var material in materials)
            {
                material.doubleSidedGI = true;
                EditorUtility.SetDirty(material);
            }

            m_SingleSided = null;
        }

        static void Change(LightingSettings settings, Action<LightingSettings> change)
        {
            if (settings == null)
                return;

            Undo.RecordObject(settings, "Lighting Settings");
            change(settings);
            EditorUtility.SetDirty(settings);
        }

        static void ChangeLight(Light light, Action<Light> change)
        {
            Undo.RecordObject(light, "Sun");
            change(light);
            EditorUtility.SetDirty(light);
        }

        static void EnableMixedLighting(UniversalRenderPipelineAsset asset)
        {
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_MixedLightingSupported").boolValue = true;
            serialized.ApplyModifiedProperties();
        }

        void DrawBake(LightingSettings settings)
        {
            if (Lightmapping.isRunning)
            {
                var rect = EditorGUILayout.GetControlRect(false, 20f);
                EditorGUI.ProgressBar(rect, Lightmapping.buildProgress, $"Baking… {Lightmapping.buildProgress:P0}");

                if (GUILayout.Button("Cancel"))
                    Lightmapping.Cancel();
            }
            else
            {
                using (new EditorGUI.DisabledScope(settings == null))
                {
                    if (GUILayout.Button("Bake Lighting", GUILayout.Height(28f)))
                    {
                        StyleLiquidBakeLight.Create();
                        Lightmapping.BakeAsync();
                    }
                }

                using (new EditorGUI.DisabledScope(settings == null || StyleBakeHook.UsesMipmapLimits(settings) ||
                                                   LightmapSettings.lightmaps.Length == 0))
                {
                    if (GUILayout.Button("Remove Mipmaps From Current Lightmaps"))
                    {
                        StyleBakeHook.StripLightmapMipmaps();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (!string.IsNullOrEmpty(m_Report))
                EditorGUILayout.HelpBox(m_Report, MessageType.None);
        }
    }
}
