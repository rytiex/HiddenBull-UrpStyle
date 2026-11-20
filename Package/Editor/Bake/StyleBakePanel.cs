using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleBakePanel : EditorWindow
    {
        const string TabKey = "HiddenBull.UrpStyle.BakePanel.Tab";
        const string DilationPath = "settings.dilationSettings.enableDilation";
        const string DilationDistancePath = "settings.dilationSettings.dilationDistance";
        const float NearPlane = 1f;
        const string LightingTabType =
            "UnityEngine.Rendering.ProbeVolumeLightingTab, Unity.RenderPipelines.Core.Editor";
        const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags HiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;

        static class Styles
        {
            public static readonly string[] Tabs = { "Lighting", "Checks", "Bake" };

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

            public static readonly GUIContent BakingSet = new GUIContent(
                "Baking Set",
                "The Adaptive Probe Volume baking set of the open scenes. Create one in the Lighting window.");

            public static readonly GUIContent Dilation = new GUIContent(
                "Dilation",
                "Fills probes that sit inside geometry from their valid neighbours. Off leaves those probes black " +
                "and they bleed into the ground as dark lines along height contours.");

            public static readonly GUIContent DilationDistance = new GUIContent(
                "Dilation Distance",
                "How far, in metres, a probe looks for valid neighbours to fill itself from.");
        }

        enum Severity
        {
            Required,
            Advice
        }

        readonly struct Check
        {
            public readonly string label;
            public readonly bool ok;
            public readonly Severity severity;
            public readonly Action fix;

            public Check(string label, bool ok, Severity severity, Action fix)
            {
                this.label = label;
                this.ok = ok;
                this.severity = severity;
                this.fix = fix;
            }
        }

        readonly List<Check> m_Checks = new List<Check>();
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

        static bool Running => Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning;

        void Tick()
        {
            if (Running)
                Repaint();
        }

        void OnBakeStarted()
        {
            m_BakeStart = EditorApplication.timeSinceStartup;
        }

        void OnBakeCompleted()
        {
            var seconds = m_BakeStart > 0.0 ? EditorApplication.timeSinceStartup - m_BakeStart : 0.0;
            var set = BakingSet();
            var probes = set == null
                ? "no Adaptive Probe Volume baking set"
                : $"probes in \"{set.name}\", dilation {(HasDilation(set) ? "on" : "OFF")}";

            m_Report = $"Baked in {TimeSpan.FromSeconds(seconds):mm\\:ss}: {LightmapSettings.lightmaps.Length} " +
                       $"lightmap(s), {StyleBakeLightmaps.Bytes() / (1024f * 1024f):0.0} MB; {probes}. Fog and " +
                       "weather were held neutral during the bake, so the light is the same in any weather.";
            m_BakeStart = 0.0;
            Repaint();
        }

        void OnGUI()
        {
            Lightmapping.TryGetLightingSettings(out var settings);
            Gather(settings);

            var tab = Mathf.Clamp(SessionState.GetInt(TabKey, 0), 0, Styles.Tabs.Length - 1);
            var next = GUILayout.Toolbar(tab, Styles.Tabs);

            if (next != tab)
            {
                SessionState.SetInt(TabKey, next);
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(4f);
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            switch (next)
            {
                case 0:
                    DrawLighting(settings);
                    break;
                case 1:
                    DrawChecks();
                    break;
                default:
                    DrawBake(settings);
                    break;
            }

            EditorGUILayout.EndScrollView();
            DrawFooter(settings);
        }

        void DrawLighting(LightingSettings settings)
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("This scene has no Lighting Settings asset yet.", MessageType.Info);

                if (GUILayout.Button("Create with HB Defaults"))
                    CreateSettings();
            }
            else
            {
                DrawLightmapper(settings);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Adaptive Probe Volumes", EditorStyles.boldLabel);
            DrawProbeVolumes();

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Lighting Window", GUILayout.Width(160f)))
                    EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
            }
        }

        static void DrawLightmapper(LightingSettings settings)
        {
            EditorGUILayout.LabelField("Lightmaps", EditorStyles.boldLabel);
            DrawQuality(settings);

            EditorGUILayout.Space(2f);
            EditorGUI.BeginChangeCheck();

            var lightmapper = (LightingSettings.Lightmapper)EditorGUILayout.EnumPopup(Styles.Lightmapper,
                                                                                      settings.lightmapper);
            var direct = EditorGUILayout.IntSlider(Styles.DirectSamples, settings.directSampleCount, 1, 1024);
            var indirect = EditorGUILayout.IntSlider(Styles.IndirectSamples, settings.indirectSampleCount, 8, 2048);
            var environment = EditorGUILayout.IntSlider(Styles.EnvironmentSamples, settings.environmentSampleCount,
                                                        8, 2048);
            var probes = EditorGUILayout.FloatField(Styles.ProbeSamples, settings.lightProbeSampleCountMultiplier);
            var bounces = EditorGUILayout.IntSlider(Styles.Bounces, settings.maxBounces, 0, 10);
            var resolution = EditorGUILayout.FloatField(Styles.Resolution, settings.lightmapResolution);
            var maxSize = EditorGUILayout.IntPopup(Styles.MaxSize, settings.lightmapMaxSize, Styles.SizeNames,
                                                   Styles.Sizes);
            var compression = (LightmapCompression)EditorGUILayout.EnumPopup(Styles.Compression,
                                                                             settings.lightmapCompression);
            var directional = (LightmapsMode)EditorGUILayout.EnumPopup(Styles.Directional,
                                                                      settings.directionalityMode);
            var mixed = (MixedLightingMode)EditorGUILayout.EnumPopup(Styles.MixedMode, settings.mixedBakeMode);

            using var serialized = new SerializedObject(settings);
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

            if (!EditorGUI.EndChangeCheck())
                return;

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

            if (!EditorGUI.EndChangeCheck() || chosen >= Styles.Qualities.Length - 1)
                return;

            Undo.RecordObject(settings, "Bake Quality");
            StyleBakeDefaults.Apply(settings, (StyleBakeQuality)chosen);
            EditorUtility.SetDirty(settings);
        }

        static void DrawProbeVolumes()
        {
            if (!UsesProbeVolumes())
            {
                EditorGUILayout.HelpBox("The URP asset does not use Adaptive Probe Volumes. Set Light Probe System " +
                                        "to Adaptive Probe Volumes on the URP asset (see Checks).", MessageType.Info);
                return;
            }

            var set = BakingSet();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(Styles.BakingSet, set, typeof(ProbeVolumeBakingSet), false);

            if (set == null)
            {
                EditorGUILayout.HelpBox("The scene has no probe baking set yet. Use Fix in Checks to create one.",
                                        MessageType.Info);
                return;
            }

            using var serialized = new SerializedObject(set);
            var dilation = serialized.FindProperty(DilationPath);
            var distance = serialized.FindProperty(DilationDistancePath);

            if (dilation != null)
                EditorGUILayout.PropertyField(dilation, Styles.Dilation);

            if (distance != null)
            {
                using (new EditorGUI.DisabledScope(dilation != null && !dilation.boolValue))
                    EditorGUILayout.PropertyField(distance, Styles.DilationDistance);

                distance.floatValue = Mathf.Max(distance.floatValue, 0f);
            }

            serialized.ApplyModifiedProperties();
        }

        void DrawChecks()
        {
            foreach (var check in m_Checks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var icon = check.ok ? "TestPassed"
                        : check.severity == Severity.Required ? "console.erroricon.sml" : "console.warnicon.sml";

                    GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20f), GUILayout.Height(18f));
                    EditorGUILayout.LabelField(check.label);

                    using (new EditorGUI.DisabledScope(check.ok || check.fix == null))
                    {
                        if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            check.fix();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        void DrawBake(LightingSettings settings)
        {
            if (!string.IsNullOrEmpty(m_Report))
                EditorGUILayout.HelpBox(m_Report, MessageType.None);
            else
                EditorGUILayout.HelpBox("Bake bakes lightmaps and Adaptive Probe Volumes together. While it runs, " +
                                        "fog and weather are held neutral and clouds keep the profile's Coverage, " +
                                        "so the baked light is the same in any weather.", MessageType.None);

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(Running || settings == null ||
                                               StyleBakeLightmaps.UsesMipmapLimits(settings) ||
                                               LightmapSettings.lightmaps.Length == 0))
            {
                if (GUILayout.Button("Remove Mipmaps From Current Lightmaps"))
                {
                    StyleBakeLightmaps.StripMipmaps();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DrawFooter(LightingSettings settings)
        {
            var required = 0;
            var advice = 0;

            foreach (var check in m_Checks)
            {
                if (check.ok)
                    continue;

                if (check.severity == Severity.Required)
                    required++;
                else
                    advice++;
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var summary = required == 0 && advice == 0 ? "All checks pass."
                    : required > 0 ? $"{required} check(s) need fixing" + (advice > 0 ? $", {advice} advice." : ".")
                    : $"{advice} advice.";
                var icon = required > 0 ? "console.erroricon.sml"
                    : advice > 0 ? "console.warnicon.sml" : "TestPassed";

                GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20f), GUILayout.Height(18f));
                EditorGUILayout.LabelField(summary);

                if (required + advice > 0 && GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(50f)))
                    SessionState.SetInt(TabKey, 1);
            }

            if (Running)
            {
                var progress = Lightmapping.isRunning ? Lightmapping.buildProgress : 0f;
                var label = Lightmapping.isRunning ? $"Baking… {progress:P0}" : "Baking probes…";
                var rect = EditorGUILayout.GetControlRect(false, 20f);

                EditorGUI.ProgressBar(rect, progress, label);

                if (GUILayout.Button("Cancel", GUILayout.Height(24f)))
                {
                    Lightmapping.Cancel();
                    AdaptiveProbeVolumes.Cancel();
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(settings == null))
                {
                    if (GUILayout.Button("Bake Lighting", GUILayout.Height(28f)))
                        Lightmapping.BakeAsync();
                }
            }

            EditorGUILayout.Space(2f);
        }

        void Gather(LightingSettings settings)
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var sun = FindSun();
            var probeVolumes = UsesProbeVolumes();
            var set = probeVolumes ? BakingSet() : null;

            m_Checks.Clear();
            m_Checks.Add(new Check("Scene has a Lighting Settings asset", settings != null, Severity.Required,
                                   CreateSettings));
            m_Checks.Add(new Check("Baked Global Illumination is on", settings != null && settings.bakedGI,
                                   Severity.Required, () => Change(settings, s => s.bakedGI = true)));
            m_Checks.Add(new Check("Mixed Lighting Mode is Shadowmask",
                                   settings != null && settings.mixedBakeMode == MixedLightingMode.Shadowmask,
                                   Severity.Required,
                                   () => Change(settings, s => s.mixedBakeMode = MixedLightingMode.Shadowmask)));
            m_Checks.Add(new Check("URP asset supports Mixed Lighting", asset != null && asset.supportsMixedLighting,
                                   Severity.Required, () => SetAsset(asset, "m_MixedLightingSupported", 1)));
            m_Checks.Add(new Check("URP asset uses Adaptive Probe Volumes", probeVolumes, Severity.Required,
                                   () => SetAsset(asset, "m_LightProbeSystem", (int)LightProbeSystem.ProbeVolumes)));

            if (probeVolumes)
            {
                m_Checks.Add(new Check("Scene has a saved probe baking set",
                                       set != null && EditorUtility.IsPersistent(set), Severity.Required,
                                       CanSaveBakingSet() ? SaveBakingSet : null));
                m_Checks.Add(new Check("Probe Dilation is on (no dark contour lines on the ground)",
                                       set != null && HasDilation(set), Severity.Required,
                                       set != null ? () => EnableDilation(set) : null));
            }

            m_Checks.Add(new Check("Scene has a directional sun", sun != null, Severity.Required, null));
            m_Checks.Add(new Check("Sun has an HB Style Environment Controller",
                                   sun != null && sun.TryGetComponent<HBStyleEnvironmentController>(out _),
                                   Severity.Required, sun != null ? () => AddController(sun) : null));
            m_Checks.Add(new Check("Sun is Mixed", sun != null && sun.lightmapBakeType == LightmapBakeType.Mixed,
                                   Severity.Required, sun != null ? () => MakeMixed(sun) : null));
            m_Checks.Add(new Check("Sun casts shadows", sun != null && sun.shadows != LightShadows.None,
                                   Severity.Required,
                                   sun != null ? () => ChangeLight(sun, l => l.shadows = LightShadows.Soft) : null));
            m_Checks.Add(new Check($"Sun Shadow Near Plane is {NearPlane:0} or more (no shadow leaks on big faces)",
                                   sun != null && sun.shadowNearPlane >= NearPlane, Severity.Advice,
                                   sun != null ? () => ChangeLight(sun, l => l.shadowNearPlane = NearPlane) : null));
            m_Checks.Add(new Check(SingleSidedLabel(), SingleSidedMaterials().Count == 0, Severity.Required,
                                   EnableDoubleSidedGI));
        }

        static bool UsesProbeVolumes()
        {
            return GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset &&
                   asset.lightProbeSystem == LightProbeSystem.ProbeVolumes;
        }

        static ProbeVolumeBakingSet BakingSet()
        {
            var find = typeof(ProbeVolumeBakingSet).GetMethod("GetBakingSetForScene", HiddenStatic, null,
                                                              new[] { typeof(Scene) }, null);
            var set = find?.Invoke(null, new object[] { SceneManager.GetActiveScene() }) as ProbeVolumeBakingSet;

            return set != null ? set : ProbeReferenceVolume.instance.currentBakingSet;
        }

        static bool CanSaveBakingSet()
        {
            return !string.IsNullOrEmpty(SceneManager.GetActiveScene().path);
        }

        static void SaveBakingSet()
        {
            if (!SaveLightingTabSet())
                CreateBakingSet();
        }

        static bool SaveLightingTabSet()
        {
            var type = Type.GetType(LightingTabType);
            var tab = type?.GetField("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var temporary = type?.GetField("m_TempBakingSet", Hidden);
            var active = type?.GetProperty("activeSet", Hidden);
            var save = type?.GetMethod("SaveTempBakingSetIfNeeded", Hidden, null, Type.EmptyTypes, null);

            if (tab == null || temporary == null || active == null || save == null || !(bool)temporary.GetValue(tab))
                return false;

            if (active.GetValue(tab) is not ProbeVolumeBakingSet set || set == null || EditorUtility.IsPersistent(set))
                return false;

            save.Invoke(tab, null);

            return EditorUtility.IsPersistent(set);
        }

        static void CreateBakingSet()
        {
            var scene = SceneManager.GetActiveScene();
            var type = typeof(ProbeVolumeBakingSet);
            var folder = type.GetMethod("GetDirectory", HiddenStatic, null, new[] { typeof(string), typeof(string) },
                                        null);
            var defaults = type.GetMethod("SetDefaults", Hidden, null, Type.EmptyTypes, null);
            var addScene = type.GetMethod("AddScene", Hidden);
            var single = type.GetField("singleSceneMode", Hidden);
            var sync = type.GetMethod("SyncBakingSets", HiddenStatic, null, Type.EmptyTypes, null);

            if (folder == null || defaults == null || addScene == null || single == null || sync == null)
            {
                Debug.LogWarning("HB Style: this URP version cannot create a probe baking set from the Bake Panel. " +
                                 "Create one in the Lighting window under Adaptive Probe Volumes.");
                return;
            }

            var set = CreateInstance<ProbeVolumeBakingSet>();

            set.name = scene.name + " Baking Set";
            defaults.Invoke(set, null);
            single.SetValue(set, true);
            addScene.Invoke(set, new object[] { AssetDatabase.AssetPathToGUID(scene.path), null });

            var directory = (string)folder.Invoke(null, new object[] { scene.path, scene.name });
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, set.name + ".asset")
                                                                 .Replace('\\', '/'));

            AssetDatabase.CreateAsset(set, path);
            AssetDatabase.SaveAssets();
            sync.Invoke(null, null);
        }

        static bool HasDilation(ProbeVolumeBakingSet set)
        {
            using var serialized = new SerializedObject(set);
            var dilation = serialized.FindProperty(DilationPath);
            var distance = serialized.FindProperty(DilationDistancePath);

            return dilation != null && dilation.boolValue && (distance == null || distance.floatValue > 0f);
        }

        static void EnableDilation(ProbeVolumeBakingSet set)
        {
            using var serialized = new SerializedObject(set);
            var dilation = serialized.FindProperty(DilationPath);
            var distance = serialized.FindProperty(DilationDistancePath);

            if (dilation != null)
                dilation.boolValue = true;

            if (distance != null && distance.floatValue <= 0f)
                distance.floatValue = 1f;

            serialized.ApplyModifiedProperties();
        }

        static Light FindSun()
        {
            var sun = RenderSettings.sun;

            if (sun != null && sun.type == LightType.Directional)
                return sun;

            var controller = StyleEnvironment.controller;

            if (controller != null && controller.TryGetComponent<Light>(out var light))
                return light;

            foreach (var candidate in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (candidate.type == LightType.Directional)
                    return candidate;
            }

            return null;
        }

        static void MakeMixed(Light sun)
        {
            ChangeLight(sun, l => l.lightmapBakeType = LightmapBakeType.Mixed);
        }

        static void AddController(Light sun)
        {
            Undo.AddComponent<HBStyleEnvironmentController>(sun.gameObject);
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

        static void SetAsset(UniversalRenderPipelineAsset asset, string property, int value)
        {
            if (asset == null)
                return;

            using var serialized = new SerializedObject(asset);
            var field = serialized.FindProperty(property);

            if (field == null)
                return;

            if (field.propertyType == SerializedPropertyType.Boolean)
                field.boolValue = value != 0;
            else
                field.intValue = value;

            serialized.ApplyModifiedProperties();
        }
    }
}
