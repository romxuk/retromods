using NUnit.Framework;
using Ryujinx.HLE.Loaders.Mods;

namespace Ryujinx.Tests.HLE
{
    public class RetroModsRomFsTests
    {
        [TestCase(0x0100277011F1A000UL, "1.0.2", "1.0.2\n", true)]
        [TestCase(0x0100277011F1A000UL, "1.0.1", "1.0.2", false)]
        [TestCase(0x010040600C5CE000UL, "1.0.2", "1.0.2", false)]
        [TestCase(0x0100277011F1A000UL, "1.0.2", null, false)]
        public void BundledPatchRequiresMatchingTitleAndVersion(ulong title, string gameVersion, string patchVersion, bool expected)
        {
            Assert.That(RetroModsRomFs.Matches(title, gameVersion, patchVersion), Is.EqualTo(expected));
        }
    }
}
