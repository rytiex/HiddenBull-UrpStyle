using NUnit.Framework;
using UnityEngine;

namespace HiddenBull.UrpStyle.Tests.Editor
{
    /// <summary>
    /// Guards the invariant every renderer feature relies on: a profile always hands back a
    /// usable settings object for any tier, however the asset was authored.
    /// </summary>
    public class StyleProfileTests
    {
        StyleProfile m_Profile;

        [SetUp]
        public void SetUp() => m_Profile = ScriptableObject.CreateInstance<StyleProfile>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Profile);

        [Test]
        public void GetSettings_ReturnsNonNull_ForEveryTier()
        {
            for (var i = 0; i < StyleQuality.TierCount; i++)
                Assert.IsNotNull(m_Profile.GetSettings((StyleQualityTier)i));
        }

        [Test]
        public void GetSettings_ReturnsDefaults_ForTierOutsideTheSerializedRange()
        {
            // An asset serialized before a tier was added, or a bad cast, must not throw.
            Assert.IsNotNull(m_Profile.GetSettings((StyleQualityTier)StyleQuality.TierCount));
        }

        [Test]
        public void ActiveTier_StartsAtDefaultTier()
        {
            Assert.AreEqual(m_Profile.defaultTier, m_Profile.activeTier);
        }

        [Test]
        public void CurrentSettings_FollowsActiveTier()
        {
            m_Profile.activeTier = StyleQualityTier.Low;
            Assert.AreSame(m_Profile.GetSettings(StyleQualityTier.Low), m_Profile.currentSettings);

            m_Profile.activeTier = StyleQualityTier.Ultra;
            Assert.AreSame(m_Profile.GetSettings(StyleQualityTier.Ultra), m_Profile.currentSettings);
        }

        [Test]
        public void LowTier_DisablesScreenSpaceFeatures()
        {
            var low = m_Profile.GetSettings(StyleQualityTier.Low);
            Assert.IsFalse(low.stylizedShadowMask, "Low tier must be able to run without the shadow mask pass.");
            Assert.IsFalse(low.directionalOcclusion, "Low tier must be able to run without the occlusion pass.");
        }
    }
}
