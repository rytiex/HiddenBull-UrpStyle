using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [InitializeOnLoad]
    static class StyleGraphMapKeywords
    {
        const string MapsGraph = "299793584f0a05067fd4192d63e2a76a";
        const string SubsurfaceGraph = "7b3b3d0ce65a118cf7e1a312ea6f61bd";
        const string BrushGraph = "c4b2e8d61f0a4e3b9d7c5a1e2f3b4c6d";

        static readonly (string graph, string input, string keyword)[] s_Maps =
        {
            (MapsGraph, "Base Map", "_HB_BASE_MAP"),
            (MapsGraph, "Normal Map", "_HB_NORMAL_MAP"),
            (MapsGraph, "Mask Map", "_HB_MASK_MAP"),
            (SubsurfaceGraph, "Translucency Map", "_HB_THICKNESS_MAP"),
            (BrushGraph, "Brush Mask", "_HB_BRUSH_MASK"),
        };

        static readonly Regex s_ObjectId = new Regex("\"m_ObjectId\": \"(\\w+)\"");
        static readonly Regex s_Type = new Regex("\"m_Type\": \"([^\"]+)\"");
        static readonly Regex s_SlotList = new Regex("\"m_Slots\": \\[(.*?)\\]", RegexOptions.Singleline);
        static readonly Regex s_Reference = new Regex("\"m_Id\": \"(\\w+)\"");
        static readonly Regex s_SlotId = new Regex("\"m_Id\": (-?\\d+),");
        static readonly Regex s_Display = new Regex("\"m_DisplayName\": \"([^\"]*)\"");
        static readonly Regex s_Texture = new Regex("\"m_SerializedTexture\": \"([^\\n]*)\",");
        static readonly Regex s_SubGraph = new Regex("\\\\\"guid\\\\\": \\\\\"(\\w+)\\\\\"");
        static readonly Regex s_Property = new Regex("\"m_Property\": \\{\\s*\"m_Id\": \"(\\w+)\"");
        static readonly Regex s_Override = new Regex("\"m_OverrideReferenceName\": \"([^\"]*)\"");
        static readonly Regex s_Default = new Regex("\"m_DefaultReferenceName\": \"([^\"]*)\"");
        static readonly Regex s_Edge = new Regex(
            "\"m_OutputSlot\": \\{\\s*\"m_Node\": \\{\\s*\"m_Id\": \"(\\w+)\"\\s*\\},\\s*\"m_SlotId\": (-?\\d+)\\s*\\},\\s*" +
            "\"m_InputSlot\": \\{\\s*\"m_Node\": \\{\\s*\"m_Id\": \"(\\w+)\"\\s*\\},\\s*\"m_SlotId\": (-?\\d+)");

        sealed class MapUse
        {
            public bool always;
            public readonly List<string> properties = new List<string>();
        }

        sealed class GraphUse
        {
            public long stamp;
            public readonly Dictionary<string, MapUse> maps = new Dictionary<string, MapUse>();
        }

        static readonly Dictionary<string, GraphUse> s_Graphs = new Dictionary<string, GraphUse>();

        static StyleGraphMapKeywords()
        {
            ObjectChangeEvents.changesPublished += OnChanges;
        }

        static void OnChanges(ref ObjectChangeEventStream stream)
        {
            for (var i = 0; i < stream.length; i++)
            {
                Object target = null;

                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i, out var changed);
                        target = EditorUtility.EntityIdToObject(changed.entityId);
                        break;
                    case ObjectChangeKind.CreateAssetObject:
                        stream.GetCreateAssetObjectEvent(i, out var created);
                        target = EditorUtility.EntityIdToObject(created.entityId);
                        break;
                }

                if (target is Material material)
                    Sync(material);
            }
        }

        internal static bool IsGraph(Shader shader)
        {
            return shader != null && AssetDatabase.GetAssetPath(shader).EndsWith(".shadergraph");
        }

        internal static void Sync(Material material)
        {
            var shader = material.shader;

            if (!IsGraph(shader) || ShaderUtil.ShaderHasError(shader))
                return;

            var graph = Read(AssetDatabase.GetAssetPath(shader));

            if (graph == null)
                return;

            var declared = new HashSet<string>(shader.keywordSpace.keywordNames);
            var dirty = false;

            foreach (var (_, _, keyword) in s_Maps)
            {
                var used = graph.maps.TryGetValue(keyword, out var use) && Used(material, use);

                dirty |= Apply(material, declared, keyword, used);
            }

            if (dirty)
                EditorUtility.SetDirty(material);
        }

        static bool Apply(Material material, HashSet<string> declared, string keyword, bool on)
        {
            if (!declared.Contains(keyword) || System.Array.IndexOf(material.shaderKeywords, keyword) >= 0 == on)
                return false;

            if (on)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);

            return true;
        }

        static bool Used(Material material, MapUse use)
        {
            if (use.always)
                return true;

            foreach (var property in use.properties)
            {
                if (material.HasProperty(property) && material.GetTexture(property) != null)
                    return true;
            }

            return false;
        }

        static GraphUse Read(string path)
        {
            if (!File.Exists(path))
                return null;

            var stamp = File.GetLastWriteTimeUtc(path).Ticks;

            if (s_Graphs.TryGetValue(path, out var cached) && cached.stamp == stamp)
                return cached;

            var text = File.ReadAllText(path).Replace("\r", "");
            var objects = new Dictionary<string, string>();

            foreach (var part in Regex.Split(text, "\n\n(?=\\{)"))
            {
                var id = s_ObjectId.Match(part);

                if (id.Success)
                    objects[id.Groups[1].Value] = part;
            }

            var sources = new Dictionary<(string node, int slot), string>();

            foreach (Match edge in s_Edge.Matches(text))
                sources[(edge.Groups[3].Value, int.Parse(edge.Groups[4].Value))] = edge.Groups[1].Value;

            var graph = new GraphUse { stamp = stamp };

            foreach (var node in objects)
            {
                if (TypeOf(node.Value) != "UnityEditor.ShaderGraph.SubGraphNode")
                    continue;

                var subGraph = s_SubGraph.Match(node.Value);
                var slots = s_SlotList.Match(node.Value);

                if (!subGraph.Success || !slots.Success)
                    continue;

                foreach (Match reference in s_Reference.Matches(slots.Groups[1].Value))
                {
                    if (!objects.TryGetValue(reference.Groups[1].Value, out var slot))
                        continue;

                    var display = s_Display.Match(slot).Groups[1].Value;

                    foreach (var (owner, input, keyword) in s_Maps)
                    {
                        if (owner != subGraph.Groups[1].Value || input != display)
                            continue;

                        if (!graph.maps.TryGetValue(keyword, out var use))
                            graph.maps[keyword] = use = new MapUse();

                        var slotId = int.Parse(s_SlotId.Match(slot).Groups[1].Value);

                        if (sources.TryGetValue((node.Key, slotId), out var source))
                            Connect(use, objects, source);
                        else if (s_Texture.Match(slot).Groups[1].Value.Contains("guid"))
                            use.always = true;
                    }
                }
            }

            s_Graphs[path] = graph;
            return graph;
        }

        static void Connect(MapUse use, Dictionary<string, string> objects, string source)
        {
            if (!objects.TryGetValue(source, out var node))
                return;

            var property = s_Property.Match(node);

            if (TypeOf(node) != "UnityEditor.ShaderGraph.PropertyNode" || !property.Success ||
                !objects.TryGetValue(property.Groups[1].Value, out var declaration))
            {
                use.always = true;
                return;
            }

            var reference = s_Override.Match(declaration).Groups[1].Value;

            if (string.IsNullOrEmpty(reference))
                reference = s_Default.Match(declaration).Groups[1].Value;

            use.properties.Add(reference);
        }

        static string TypeOf(string json)
        {
            return s_Type.Match(json).Groups[1].Value;
        }
    }

    sealed class StyleGraphMapKeywordsPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var paths = new List<string>();

            foreach (var path in imported)
            {
                if (path.EndsWith(".mat") || path.EndsWith(".shadergraph"))
                    paths.Add(path);
            }

            if (paths.Count == 0)
                return;

            EditorApplication.delayCall += () => SyncLater(paths);
        }

        static void SyncLater(List<string> imported)
        {
            var graphs = new HashSet<Shader>();

            foreach (var path in imported)
            {
                if (path.EndsWith(".mat"))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                    if (material != null)
                        StyleGraphMapKeywords.Sync(material);
                }
                else if (path.EndsWith(".shadergraph"))
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);

                    if (shader != null)
                        graphs.Add(shader);
                }
            }

            if (graphs.Count == 0)
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));

                if (material != null && graphs.Contains(material.shader))
                    StyleGraphMapKeywords.Sync(material);
            }
        }
    }
}
