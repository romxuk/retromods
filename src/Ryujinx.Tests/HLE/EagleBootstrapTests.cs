using NUnit.Framework;
using Ryujinx.HLE.HOS.Services.Sockets;
using System.Net;

namespace Ryujinx.Tests.HLE
{
    public class EagleBootstrapTests
    {
        private const ulong Mario35 = 0x0100277011F1A000;

        [Test]
        public void ServiceRequestsWithoutAnApplicationPidUseTheActiveGame()
        {
            ulong titleId = EagleBootstrap.SelectApplicationTitleId(null, Mario35);
            Assert.That(EagleBootstrap.Matches(titleId, "g21f12900-lp1.s.n.srv.nintendo.net"), Is.True);
            Assert.That(EagleBootstrap.IsBootstrapEndpoint(titleId,
                new IPEndPoint(EagleBootstrap.GuestAddress, 443)), Is.True);
        }

        [Test]
        public void KnownUnrelatedCallerDoesNotInheritActiveGameRouting()
        {
            ulong titleId = EagleBootstrap.SelectApplicationTitleId(0x0100000000010000, Mario35);
            Assert.That(EagleBootstrap.Matches(titleId, "g21f12900-lp1.s.n.srv.nintendo.net"), Is.False);
            Assert.That(EagleBootstrap.SelectApplicationTitleId(null, null), Is.Zero);
        }

        [TestCase(0x0100277011F1A000UL, "g21f12900-lp1.s.n.srv.nintendo.net")]
        [TestCase(0x010040600C5CE000UL, "g23bda200-lp1.s.n.srv.nintendo.net")]
        [TestCase(0x0100AD9012510000UL, "g2e471600-lp1.s.n.srv.nintendo.net")]
        public void MatchesOnlyTheTitlesOwnBootstrap(ulong titleId, string host)
        {
            Assert.That(EagleBootstrap.Matches(titleId, host), Is.True);
            Assert.That(EagleBootstrap.Matches(titleId, host.ToUpperInvariant() + "."), Is.True);
            Assert.That(EagleBootstrap.Matches(0, host), Is.False);
            Assert.That(EagleBootstrap.Matches(titleId, host + ".example.com"), Is.False);
        }

        [TestCase("accounts.nintendo.com")]
        [TestCase("g23bda200-lp1.s.n.srv.nintendo.net")]
        [TestCase("g21f12900-lp1.n.n.srv.nintendo.net")]
        [TestCase("d7d-ayam.g.lp1.e.srv.nintendo.net")]
        [TestCase("mario35.retromods.co.uk")]
        [TestCase(null)]
        public void LeavesOtherHostsAlone(string host)
        {
            Assert.That(EagleBootstrap.Matches(Mario35, host), Is.False);
        }

        [Test]
        public void RoutesOnlyTheSyntheticBootstrapAddress()
        {
            Assert.That(EagleBootstrap.IsBootstrapEndpoint(Mario35,
                new IPEndPoint(EagleBootstrap.GuestAddress, 443)), Is.True);
            Assert.That(EagleBootstrap.IsBootstrapEndpoint(0,
                new IPEndPoint(EagleBootstrap.GuestAddress, 443)), Is.False);
            Assert.That(EagleBootstrap.IsBootstrapEndpoint(Mario35,
                new IPEndPoint(IPAddress.Loopback, 443)), Is.False);
            Assert.That(EagleBootstrap.ServerHost, Is.EqualTo("mario35.retromods.co.uk"));
            Assert.That(EagleBootstrap.ServerPort, Is.EqualTo(20000));
        }
    }
}
