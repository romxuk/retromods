using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.HLE.HOS.Services.Nifm.StaticService.Types
{
    internal static class NetworkProfileValue
    {
        internal static T Read<T>(Func<T> read, T unavailable, string field)
        {
            try
            {
                return read();
            }
            catch (PlatformNotSupportedException)
            {
                // Keep the actual interface address. Optional host metadata must not
                // terminate the guest thread on platforms that do not expose it.
                Logger.Warning?.Print(LogClass.ServiceNifm,
                    $"Network profile {field} is unavailable on this platform; returning unspecified metadata.");
                return unavailable;
            }
        }
    }
}
