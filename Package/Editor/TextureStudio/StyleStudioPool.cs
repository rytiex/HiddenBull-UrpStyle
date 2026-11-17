using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    sealed class StyleStudioPool
    {
        const string LitShader = "HB URP Style/Lit";
        const float Inner = 4f;
        const float Outer = 20f;
        const float Depth = 2f;
        const float Rim = 0.3f;

        static readonly Vector3 s_Origin = new Vector3(-5000f, 0f, 5000f);

        GameObject m_Basin;
        GameObject m_Liquid;
        Mesh m_Mesh;
        Material m_Stone;
        HBStyleLiquidPool m_Pool;

        internal Vector3 centre => s_Origin;

        internal bool Build(PreviewRenderUtility utility, HBStyleLiquidLayer layer)
        {
            if (m_Pool != null)
            {
                m_Pool.layer = layer;
                return true;
            }

            var shader = Shader.Find(LitShader);

            if (shader == null)
                return false;

            var cullingLayer = FirstLayer(utility.camera.cullingMask);

            m_Stone = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m_Stone.SetColor("_BaseColor", new Color(0.55f, 0.5f, 0.42f, 1f));
            m_Stone.SetFloat("_Smoothness", 0.1f);
            m_Stone.SetFloat("_Cull", 0f);
            m_Stone.SetFloat("_BrushEnabled", 0f);
            m_Stone.SetFloat("_ReceiveWeather", 0f);
            m_Stone.DisableKeyword("_HB_BRUSH");
            m_Mesh = Basin();

            m_Basin = new GameObject("HB Studio Basin") { hideFlags = HideFlags.HideAndDontSave, layer = cullingLayer };
            m_Basin.SetActive(false);
            m_Basin.transform.position = s_Origin;
            m_Basin.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            m_Basin.AddComponent<MeshRenderer>().sharedMaterial = m_Stone;
            utility.AddSingleGO(m_Basin);
            m_Basin.SetActive(true);

            m_Liquid = new GameObject("HB Studio Pool") { hideFlags = HideFlags.HideAndDontSave, layer = cullingLayer };
            m_Liquid.SetActive(false);
            m_Liquid.transform.SetPositionAndRotation(s_Origin + Vector3.down * (Depth * 0.5f), Quaternion.identity);
            m_Liquid.transform.localScale = new Vector3(Inner * 2f, Depth, Inner * 2f);
            m_Pool = m_Liquid.AddComponent<HBStyleLiquidPool>();
            m_Pool.onlyCamera = utility.camera;
            m_Pool.layer = layer;
            utility.AddSingleGO(m_Liquid);
            m_Liquid.SetActive(true);
            return true;
        }

        static Mesh Basin()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            Quad(vertices, normals, triangles, new Vector3(-Inner, -Depth, -Inner), new Vector3(Inner, -Depth, -Inner),
                 new Vector3(Inner, -Depth, Inner), new Vector3(-Inner, -Depth, Inner), Vector3.up);
            Quad(vertices, normals, triangles, new Vector3(-Inner, -Depth, Inner), new Vector3(Inner, -Depth, Inner),
                 new Vector3(Inner, Rim, Inner), new Vector3(-Inner, Rim, Inner), Vector3.back);
            Quad(vertices, normals, triangles, new Vector3(Inner, -Depth, -Inner), new Vector3(-Inner, -Depth, -Inner),
                 new Vector3(-Inner, Rim, -Inner), new Vector3(Inner, Rim, -Inner), Vector3.forward);
            Quad(vertices, normals, triangles, new Vector3(Inner, -Depth, Inner), new Vector3(Inner, -Depth, -Inner),
                 new Vector3(Inner, Rim, -Inner), new Vector3(Inner, Rim, Inner), Vector3.left);
            Quad(vertices, normals, triangles, new Vector3(-Inner, -Depth, -Inner), new Vector3(-Inner, -Depth, Inner),
                 new Vector3(-Inner, Rim, Inner), new Vector3(-Inner, Rim, -Inner), Vector3.right);
            Quad(vertices, normals, triangles, new Vector3(-Outer, Rim, Inner), new Vector3(Outer, Rim, Inner),
                 new Vector3(Outer, Rim, Outer), new Vector3(-Outer, Rim, Outer), Vector3.up);
            Quad(vertices, normals, triangles, new Vector3(-Outer, Rim, -Outer), new Vector3(Outer, Rim, -Outer),
                 new Vector3(Outer, Rim, -Inner), new Vector3(-Outer, Rim, -Inner), Vector3.up);
            Quad(vertices, normals, triangles, new Vector3(Inner, Rim, -Inner), new Vector3(Outer, Rim, -Inner),
                 new Vector3(Outer, Rim, Inner), new Vector3(Inner, Rim, Inner), Vector3.up);
            Quad(vertices, normals, triangles, new Vector3(-Outer, Rim, -Inner), new Vector3(-Inner, Rim, -Inner),
                 new Vector3(-Inner, Rim, Inner), new Vector3(-Outer, Rim, Inner), Vector3.up);

            var mesh = new Mesh { name = "HB Studio Basin", hideFlags = HideFlags.HideAndDontSave };

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Quad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 a, Vector3 b,
                         Vector3 c, Vector3 d, Vector3 normal)
        {
            var start = vertices.Count;

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            for (var i = 0; i < 4; i++)
                normals.Add(normal);

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
        }

        static int FirstLayer(int mask)
        {
            for (var layer = 0; layer < 32; layer++)
            {
                if ((mask & (1 << layer)) != 0)
                    return layer;
            }

            return 0;
        }

        internal void Release()
        {
            if (m_Liquid != null)
                Object.DestroyImmediate(m_Liquid);

            if (m_Basin != null)
                Object.DestroyImmediate(m_Basin);

            if (m_Mesh != null)
                Object.DestroyImmediate(m_Mesh);

            if (m_Stone != null)
                Object.DestroyImmediate(m_Stone);

            m_Liquid = null;
            m_Basin = null;
            m_Mesh = null;
            m_Stone = null;
            m_Pool = null;
        }
    }
}
