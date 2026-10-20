using System;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    public sealed class StyleTextureStudio : EditorWindow
    {
        static readonly Type[] s_PageTypes =
        {
            typeof(TerrainTextureGeneratorPage),
            typeof(GrassTextureGeneratorPage),
            typeof(LiquidTextureGeneratorPage),
            typeof(BrushAtlasGeneratorPage),
            typeof(CloudAtlasGeneratorPage),
            typeof(MaskMapGeneratorPage)
        };

        [SerializeField] int m_Page;
        [SerializeField] StyleStudioPage[] m_Pages;

        GUIContent[] m_Titles;
        Vector2 m_Scroll;

        [MenuItem("Tools/HiddenBull/URP Style/Texture Studio")]
        static void Open()
        {
            var window = GetWindow<StyleTextureStudio>();
            window.titleContent = new GUIContent("Texture Studio");
            window.minSize = new Vector2(420f, 640f);
            window.Show();
        }

        void OnEnable()
        {
            if (m_Pages == null || m_Pages.Length != s_PageTypes.Length)
                Array.Resize(ref m_Pages, s_PageTypes.Length);

            m_Titles = new GUIContent[s_PageTypes.Length];

            for (var index = 0; index < s_PageTypes.Length; index++)
            {
                if (m_Pages[index] == null || m_Pages[index].GetType() != s_PageTypes[index])
                {
                    m_Pages[index] = (StyleStudioPage)CreateInstance(s_PageTypes[index]);
                    m_Pages[index].hideFlags = HideFlags.DontSave;
                }

                m_Pages[index].host = this;
                m_Titles[index] = new GUIContent(m_Pages[index].title);
            }

            m_Page = Mathf.Clamp(m_Page, 0, s_PageTypes.Length - 1);
        }

        void OnDestroy()
        {
            if (m_Pages == null)
                return;

            foreach (var page in m_Pages)
            {
                if (page != null)
                    DestroyImmediate(page);
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(4f);

            var page = GUILayout.Toolbar(m_Page, m_Titles, GUILayout.Height(24f));

            if (page != m_Page)
            {
                m_Page = page;
                m_Scroll = Vector2.zero;
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(6f);

            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;
                m_Pages[m_Page].OnGUI();
            }
        }
    }
}
