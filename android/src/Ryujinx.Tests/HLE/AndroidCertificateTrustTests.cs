using NUnit.Framework;
using Ryujinx.HLE.HOS.Services.Ssl.SslService;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Tests.HLE
{
    public class AndroidCertificateTrustTests
    {
        [TestCase("mario35.retromods.co.uk", true, true)]
        [TestCase("g21f12900-lp1.s.n.srv.nintendo.net", true, false)]
        [TestCase("mario35.retromods.co.uk", false, false)]
        public async Task TlsValidatesTrustAndHostName(string host, bool trusted, bool expectedSuccess)
        {
            using RSA rootKey = RSA.Create(2048);
            var rootRequest = new CertificateRequest("CN=Test Root", rootKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
            using var root = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            using RSA leafKey = RSA.Create(2048);
            var leafRequest = new CertificateRequest("CN=mario35.retromods.co.uk", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("mario35.retromods.co.uk");
            leafRequest.CertificateExtensions.Add(san.Build());
            leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            leafRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));
            using var signedLeaf = leafRequest.Create(root, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1), RandomNumberGenerator.GetBytes(16));
            using var leaf = signedLeaf.CopyWithPrivateKey(leafKey);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Task server = Task.Run(async () =>
            {
                using var accepted = await listener.AcceptTcpClientAsync(timeout.Token);
                using var stream = new SslStream(accepted.GetStream());
                try
                {
                    await stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = leaf,
                        EnabledSslProtocols = SslProtocols.Tls12,
                    }, timeout.Token);
                }
                catch (AuthenticationException) { }
                catch (IOException) { }
            });
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port, timeout.Token);
            using var clientStream = new SslStream(client.GetStream());
            var roots = new X509Certificate2Collection();
            if (trusted)
            {
                roots.Add(root);
            }
            bool success;
            try
            {
                await clientStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateChainPolicy = AndroidCertificateTrust.CreatePolicy(roots),
                }, timeout.Token);
                success = true;
            }
            catch (AuthenticationException)
            {
                success = false;
            }
            await server;
            Assert.That(success, Is.EqualTo(expectedSuccess));
        }

        [Test]
        public void LoadsHashNamedPemAndDerFilesAndSkipsInvalidFiles()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            using RSA key = RSA.Create(2048);
            var request = new CertificateRequest("CN=Test Root", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            try
            {
                File.WriteAllText(Path.Combine(directory, "12345678.0"), certificate.ExportCertificatePem());
                File.WriteAllBytes(Path.Combine(directory, "12345678.1"), certificate.Export(X509ContentType.Cert));
                File.WriteAllText(Path.Combine(directory, "invalid.0"), "invalid certificate");
                var roots = AndroidCertificateTrust.LoadRoots(new[] { directory + "-missing", directory });
                try
                {
                    Assert.That(roots.Count, Is.EqualTo(2));
                    Assert.That(roots[0].Thumbprint, Is.EqualTo(certificate.Thumbprint));
                    Assert.That(roots[1].Thumbprint, Is.EqualTo(certificate.Thumbprint));
                }
                finally
                {
                    foreach (var root in roots)
                    {
                        root.Dispose();
                    }
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
