using NUnit.Framework;
using UnityEngine;

namespace HiddenBull.UrpStyle.Tests.Editor
{
    public class StyleSettingsTests
    {
        [Test]
        public void ResolutionScale_MapsToTheExpectedDivisor()
        {
            Assert.AreEqual(1, StyleResolutionScale.Full.ToDivisor());
            Assert.AreEqual(2, StyleResolutionScale.Half.ToDivisor());
            Assert.AreEqual(4, StyleResolutionScale.Quarter.ToDivisor());
        }

        [Test]
        public void Feature_AlwaysHasSettings()
        {
            // A renderer feature serialized before the settings block existed deserializes with a
            // null field, and every pass from Phase 2 on will read it. Create() must cover that.
            var feature = ScriptableObject.CreateInstance<HiddenBullStyleFeature>();

            try
            {
                feature.Create();
                Assert.IsNotNull(feature.settings);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }
    }
}
