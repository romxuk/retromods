using Ryujinx.Common.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Ryujinx.HLE.HOS.Services.Ssl.SslService
{
    internal static class AndroidCertificateTrust
    {
        private static readonly Lazy<X509Certificate2Collection> Roots = new(() =>
            LoadRoots(new[] { "/apex/com.android.conscrypt/cacerts", "/system/etc/security/cacerts" }));

        internal static X509Certificate2Collection LoadRoots(string[] directories)
        {
            var roots = new X509Certificate2Collection();

            foreach (string directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(directory);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Logger.Warning?.Print(LogClass.ServiceSsl, $"Cannot read Android trust directory {directory}: {exception.Message}");
                    continue;
                }

                foreach (string file in files)
                {
                    try
                    {
                        // Android uses hash filenames (for example 00673b5b.0), not .pem extensions.
                        // LoadCertificate accepts both PEM and DER certificates.
                        roots.Add(X509CertificateLoader.LoadCertificate(File.ReadAllBytes(file)));
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
                    {
                        Logger.Warning?.Print(LogClass.ServiceSsl, $"Cannot load Android trust certificate {Path.GetFileName(file)}: {exception.Message}");
                    }
                }

                // Prefer the current Conscrypt store; use the legacy store only as a fallback.
                if (roots.Count > 0)
                {
                    Logger.Info?.Print(LogClass.ServiceSsl, $"Loaded {roots.Count} Android TLS trust certificates from {directory}.");
                    return roots;
                }
            }

            Logger.Warning?.Print(LogClass.ServiceSsl, "No Android TLS trust certificates could be loaded; retaining system validation.");
            return roots;
        }

        internal static X509ChainPolicy CreatePolicy()
        {
            return CreatePolicy(Roots.Value);
        }

        internal static X509ChainPolicy CreatePolicy(X509Certificate2Collection roots)
        {
            var policy = new X509ChainPolicy
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.NoFlag,
            };
            policy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1")); // TLS server authentication.

            if (roots.Count > 0)
            {
                policy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                policy.CustomTrustStore.AddRange(roots);
            }

            return policy;
        }
    }
}
