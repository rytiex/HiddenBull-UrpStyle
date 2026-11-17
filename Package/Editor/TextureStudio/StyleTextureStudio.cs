using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleTextureStudio : EditorWindow
    {
        const string SettingsKey = "HiddenBull.UrpStyle.TextureStudio.SettingsWidth";
        const string LayersKey = "HiddenBull.UrpStyle.TextureStudio.LayersWidth";
        const float Splitter = 5f;
        const float LeastSide = 200f;
        const float LeastPreview = 240f;

        [SerializeReference]
        StyleStudioPage[] m_Pages;

        [SerializeField]
        int m_Page;

        Vector2 m_SettingsScroll;
        Vector2 m_LayersScroll;
        GUIContent[] m_Titles;
        float m_SettingsWidth;
        float m_LayersWidth;
        int m_Dragging;

        [MenuItem("Tools/HiddenBull/URP Style/Texture Studio")]
        static void Open()
        {
            var window = GetWindow<StyleTextureStudio>();
            window.titleContent = new GUIContent("Texture Studio");
            window.minSize = new Vector2(LeastSide * 2f + LeastPreview + Splitter * 2f, 480f);
            window.Show();
        }

        void OnEnable()
        {
            var material = m_Pages != null && m_Pages.Length > 0 && m_Pages[0] is StyleMaterialPage kept
                ? kept
                : new StyleMaterialPage();
            var grass = m_Pages != null && m_Pages.Length > 1 && m_Pages[1] is StyleGrassPage keptGrass
                ? keptGrass
                : new StyleGrassPage();
            var brush = m_Pages != null && m_Pages.Length > 2 && m_Pages[2] is StyleBrushPage keptBrush
                ? keptBrush
                : new StyleBrushPage();
            var cloud = m_Pages != null && m_Pages.Length > 3 && m_Pages[3] is StyleCloudPage keptCloud
                ? keptCloud
                : new StyleCloudPage();
            var liquid = m_Pages != null && m_Pages.Length > 4 && m_Pages[4] is StyleLiquidPage keptLiquid
                ? keptLiquid
                : new StyleLiquidPage();

            m_Pages = new StyleStudioPage[] { material, grass, brush, cloud, liquid };

            foreach (var page in m_Pages)
                page.host = this;

            m_Titles = System.Array.ConvertAll(m_Pages, page => page.title);
            m_Page = Mathf.Clamp(m_Page, 0, m_Pages.Length - 1);
            m_SettingsWidth = EditorPrefs.GetFloat(SettingsKey, 380f);
            m_LayersWidth = EditorPrefs.GetFloat(LayersKey, 220f);
        }

        void OnDisable()
        {
            foreach (var page in m_Pages)
                page.Release();
        }

        void OnGUI()
        {
            var page = m_Pages[m_Page];

            var width = position.width;
            var top = EditorStyles.toolbar.fixedHeight > 0f ? EditorStyles.toolbar.fixedHeight : 21f;
            var bar = new Rect(0f, 0f, width, top);

            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbar.Draw(bar, false, false, false, false);

            var tabs = new Rect(0f, 0f, Mathf.Min(width, 90f * m_Titles.Length), top);
            m_Page = GUI.Toolbar(tabs, m_Page, m_Titles, EditorStyles.toolbarButton);
            page.DrawToolbar(new Rect(tabs.xMax + 8f, 0f, Mathf.Max(width - tabs.xMax - 12f, 0f), top));

            var height = position.height - top;
            var side = page.hasLayers;
            var most = width - LeastPreview - Splitter * (side ? 2f : 1f);
            var layersWidth = side ? m_LayersWidth : 0f;

            m_SettingsWidth = Mathf.Clamp(m_SettingsWidth, LeastSide, Mathf.Max(LeastSide, most - layersWidth));
            if (side)
                m_LayersWidth = Mathf.Clamp(m_LayersWidth, LeastSide, Mathf.Max(LeastSide, most - m_SettingsWidth));

            var settings = new Rect(0f, top, m_SettingsWidth, height);
            var leftSplit = new Rect(settings.xMax, top, Splitter, height);
            var layers = new Rect(width - layersWidth, top, layersWidth, height);
            var rightSplit = new Rect(layers.xMin - (side ? Splitter : 0f), top, side ? Splitter : 0f, height);
            var preview = Rect.MinMaxRect(leftSplit.xMax, top + 4f, rightSplit.xMin, position.height - 4f);

            DrawSplitter(leftSplit, 1);

            if (side)
                DrawSplitter(rightSplit, 2);

            GUILayout.BeginArea(settings);
            m_SettingsScroll = EditorGUILayout.BeginScrollView(m_SettingsScroll);
            EditorGUIUtility.labelWidth = Mathf.Clamp(m_SettingsWidth * 0.4f, 90f, 200f);
            page.DrawSettings();
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (side)
            {
                GUILayout.BeginArea(layers, EditorStyles.helpBox);
                m_LayersScroll = EditorGUILayout.BeginScrollView(m_LayersScroll);
                page.DrawLayers();
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            if (preview.width > 1f && preview.height > 1f)
                page.DrawPreview(preview);
        }

        void DrawSplitter(Rect rect, int which)
        {
            var current = Event.current;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

            if (current.type == EventType.Repaint)
            {
                var line = new Rect(rect.center.x - 0.5f, rect.y, 1f, rect.height);
                EditorGUI.DrawRect(line, new Color(0f, 0f, 0f, 0.35f));
            }

            switch (current.type)
            {
                case EventType.MouseDown when current.button == 0 && rect.Contains(current.mousePosition):
                    m_Dragging = which;
                    current.Use();
                    break;
                case EventType.MouseDrag when m_Dragging == which:
                    if (which == 1)
                        m_SettingsWidth += current.delta.x;
                    else
                        m_LayersWidth -= current.delta.x;

                    current.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when m_Dragging == which:
                    m_Dragging = 0;
                    EditorPrefs.SetFloat(SettingsKey, m_SettingsWidth);
                    EditorPrefs.SetFloat(LayersKey, m_LayersWidth);
                    current.Use();
                    break;
            }
        }
    }
}
