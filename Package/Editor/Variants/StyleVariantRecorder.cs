using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleVariantRecorder : IPreprocessBuildWithReport, IPreprocessShaders, IPostprocessBuildWithReport
    {
        internal const string DataPath = "Library/HBStyle/ShaderVariants.tsv";

        static readonly HashSet<string> s_Variants = new HashSet<string>();
        static string s_Target;

        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_Variants.Clear();
            s_Target = report.summary.platform.ToString();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            for (var i = 0; i < data.Count; i++)
            {
                var keywords = data[i].shaderKeywordSet.GetShaderKeywords()
                    .Select(keyword => keyword.name)
                    .OrderBy(name => name, StringComparer.Ordinal);
                var key = string.Join(" ", keywords);

                s_Variants.Add($"{shader.name}\t{snippet.passName}\t{snippet.shaderType}\t{key}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (s_Variants.Count == 0)
                return;

            var text = new StringBuilder();

            text.Append("# ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                .Append('\t').Append(s_Target).Append('\n');

            foreach (var variant in s_Variants)
                text.Append(variant).Append('\n');

            Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
            File.WriteAllText(DataPath, text.ToString());
            s_Variants.Clear();
            StyleVariantData.Invalidate();
        }
    }

    sealed class StyleVariantData
    {
        internal sealed class Group
        {
            public string shader;
            public string pass;
            public string stage;
            public readonly List<string[]> variants = new List<string[]>();
            public List<KeyValuePair<string, float>> multipliers;
        }

        internal sealed class ShaderTotal
        {
            public string shader;
            public int variants;
            public Group largest;
        }

        static StyleVariantData s_Cached;
        static DateTime s_CachedTime;

        public string built;
        public string target;
        public int total;
        public readonly List<Group> groups = new List<Group>();
        public readonly List<ShaderTotal> shaders = new List<ShaderTotal>();
        readonly Dictionary<string, float> m_Factors = new Dictionary<string, float>();

        internal static void Invalidate()
        {
            s_Cached = null;
        }

        internal static StyleVariantData Load()
        {
            if (!File.Exists(StyleVariantRecorder.DataPath))
                return null;

            var time = File.GetLastWriteTimeUtc(StyleVariantRecorder.DataPath);

            if (s_Cached != null && time == s_CachedTime)
                return s_Cached;

            s_Cached = Read();
            s_CachedTime = time;
            return s_Cached;
        }

        static StyleVariantData Read()
        {
            var data = new StyleVariantData();
            var byKey = new Dictionary<string, Group>();

            foreach (var line in File.ReadLines(StyleVariantRecorder.DataPath))
            {
                if (line.StartsWith("# "))
                {
                    var header = line.Substring(2).Split('\t');

                    data.built = header[0];
                    data.target = header.Length > 1 ? header[1] : string.Empty;
                    continue;
                }

                var parts = line.Split('\t');

                if (parts.Length < 4)
                    continue;

                var key = parts[0] + "\t" + parts[1] + "\t" + parts[2];

                if (!byKey.TryGetValue(key, out var group))
                {
                    group = new Group { shader = parts[0], pass = parts[1], stage = parts[2] };
                    byKey.Add(key, group);
                    data.groups.Add(group);
                }

                group.variants.Add(parts[3].Length == 0
                                       ? Array.Empty<string>()
                                       : parts[3].Split(' '));
                data.total++;
            }

            foreach (var shader in data.groups.GroupBy(group => group.shader))
            {
                data.shaders.Add(new ShaderTotal
                {
                    shader = shader.Key,
                    variants = shader.Sum(group => group.variants.Count),
                    largest = shader.OrderByDescending(group => group.variants.Count).First()
                });
            }

            data.shaders.Sort((a, b) => b.variants.CompareTo(a.variants));
            return data;
        }

        internal float Factor(string keyword)
        {
            if (m_Factors.TryGetValue(keyword, out var factor))
                return factor;

            var left = 0;

            foreach (var group in groups)
                left += Without(group, keyword);

            factor = left > 0 ? (float)total / left : 1f;
            m_Factors.Add(keyword, factor);
            return factor;
        }

        internal static float Factor(Group group, string keyword)
        {
            var left = Without(group, keyword);

            return left > 0 ? (float)group.variants.Count / left : 1f;
        }

        static int Without(Group group, string keyword)
        {
            var unique = new HashSet<string>();
            var found = false;

            foreach (var variant in group.variants)
            {
                if (Array.IndexOf(variant, keyword) >= 0)
                    found = true;

                unique.Add(string.Join(" ", variant.Where(name => name != keyword)));
            }

            return found ? unique.Count : group.variants.Count;
        }

        internal static List<KeyValuePair<string, float>> Multipliers(Group group)
        {
            if (group.multipliers != null)
                return group.multipliers;

            var names = new HashSet<string>();

            foreach (var variant in group.variants)
                names.UnionWith(variant);

            group.multipliers = names.Select(name => new KeyValuePair<string, float>(name, Factor(group, name)))
                .Where(pair => pair.Value > 1.01f)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .ToList();

            return group.multipliers;
        }
    }
}
