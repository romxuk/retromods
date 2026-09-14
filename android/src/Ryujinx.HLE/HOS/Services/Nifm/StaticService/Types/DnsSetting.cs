using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nifm.StaticService.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 9)]
    struct DnsSetting
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool IsDynamicDnsEnabled;
        public IpV4Address PrimaryDns;
        public IpV4Address SecondaryDns;

        public DnsSetting(IPInterfaceProperties interfaceProperties)
        {
            IsDynamicDnsEnabled = OperatingSystem.IsWindows() && interfaceProperties.IsDynamicDnsEnabled;

            // Android may throw instead of returning an empty DNS list.
            // Zero means unavailable metadata; guest DNS still uses the host resolver.
            var addresses = NetworkProfileValue.Read(() => interfaceProperties.DnsAddresses,
                default(IPAddressCollection), "DNS addresses");
            PrimaryDns = default;
            SecondaryDns = default;
            if (addresses == null)
            {
                return;
            }

            bool foundPrimary = false;
            foreach (var address in addresses)
            {
                if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    continue;
                }

                if (!foundPrimary)
                {
                    PrimaryDns = new IpV4Address(address);
                    SecondaryDns = PrimaryDns;
                    foundPrimary = true;
                }
                else
                {
                    SecondaryDns = new IpV4Address(address);
                    break;
                }
            }
        }
    }
}
