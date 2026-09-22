using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Tests.Editor
{
    /// <summary>
    /// Guards the properties of the master shader that are expensive to notice by eye.
    ///
    /// A broken SRP Batcher layout or a missing DepthNormals pass does not produce an error — the
    /// frame just gets slower, or a screen-space feature two phases from now quietly has nothing to
    /// read. Both are caught here instead.
    /// </summary>
    public class LitShaderTests
    {
        const string k_ShaderName = "HiddenBull/URP Style/Lit";

        static readonly string[] k_RequiredLightModes =
        {
            "UniversalForward",
            "ShadowCaster",
            "DepthOnly",
            "DepthNormals",  // The shared resource set of Architecture D4 is built on this.
            "Meta"           // Required for the baked indirect light of Architecture D16.
        };

        Shader m_Shader;

        [SetUp]
        public void SetUp()
        {
            m_Shader = Shader.Find(k_ShaderName);
            Assert.IsNotNull(m_Shader, $"Could not find shader '{k_ShaderName}'.");
        }

        [Test]
        public void LitShader_Compiles()
        {
            Assert.IsTrue(m_Shader.isSupported, $"'{k_ShaderName}' failed to compile on this platform.");
        }

        [Test]
        public void LitShader_DeclaresEveryRequiredPass()
        {
            var lightModeTag = new ShaderTagId("LightMode");
            var found = new List<string>();

            for (var i = 0; i < m_Shader.passCount; i++)
            {
                var lightMode = m_Shader.FindPassTagValue(i, lightModeTag);
                if (!string.IsNullOrEmpty(lightMode.name))
                    found.Add(lightMode.name);
            }

            foreach (var required in k_RequiredLightModes)
                Assert.Contains(required, found, $"'{k_ShaderName}' is missing its {required} pass.");
        }

        [Test]
        public void LitShader_IsSRPBatcherCompatible()
        {
            // Accessed reflectively: the check is an editor utility whose accessibility has moved
            // between Unity versions, and a compile error here would take the whole test assembly
            // down with it.
            var method = typeof(UnityEditor.ShaderUtil).GetMethod(
                "GetSRPBatcherCompatibilityCode",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
                Assert.Ignore("ShaderUtil.GetSRPBatcherCompatibilityCode is unavailable in this Unity version.");

            var code = (int)method.Invoke(null, new object[] { m_Shader, 0 });

            Assert.AreEqual(0, code,
                $"'{k_ShaderName}' is not SRP Batcher compatible (code {code}). The usual cause is a " +
                "material property used in HLSL but missing from the UnityPerMaterial CBUFFER, or a " +
                "CBUFFER layout that differs between passes.");
        }
    }

    /// <summary>
    /// The ambient fallback is what stands between a missing renderer feature and an all-black
    /// scene, so it is worth a test of its own.
    /// </summary>
    public class AmbientDefaultsTests
    {
        [Test]
        public void Defaults_ProduceVisibleAmbient()
        {
            AmbientDefaults.Apply();

            var parameters = Shader.GetGlobalVector("_HB_AmbientParams");
            Assert.Greater(parameters.z, 0f, "Default ambient intensity must be above zero, or an " +
                                             "unconfigured project renders black.");
            Assert.Greater(parameters.x, 0f, "Sky falloff must be above zero to avoid a divide guard " +
                                             "collapsing the gradient.");
            Assert.Greater(parameters.y, 0f, "Ground falloff must be above zero for the same reason.");
            Assert.AreEqual(0f, parameters.w, "Baked ambient must default to off so a project with no " +
                                              "lightmaps is not blended toward black.");
        }
    }
}
