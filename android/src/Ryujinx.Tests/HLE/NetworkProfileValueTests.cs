using NUnit.Framework;
using Ryujinx.HLE.HOS.Services.Nifm.StaticService.Types;
using System;

namespace Ryujinx.Tests.HLE
{
    public class NetworkProfileValueTests
    {
        [TestCase("IPv4 mask")]
        [TestCase("gateway")]
        [TestCase("DNS addresses")]
        public void UnsupportedMetadataDoesNotEscapeIntoGuestThread(string field)
        {
            Assert.That(NetworkProfileValue.Read<int>(
                () => throw new PlatformNotSupportedException(), 0, field), Is.Zero);
        }

        [Test]
        public void SupportedMetadataIsPreserved()
        {
            Assert.That(NetworkProfileValue.Read(() => 24, 0, "IPv4 mask"), Is.EqualTo(24));
        }

        [Test]
        public void UnrelatedFailuresAreNotHidden()
        {
            Assert.Throws<InvalidOperationException>(() =>
                NetworkProfileValue.Read<int>(() => throw new InvalidOperationException(), 0, "gateway"));
        }
    }
}
