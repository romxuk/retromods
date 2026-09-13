using System;
using System.Net;

namespace Ryujinx.HLE.HOS.Services.Sockets
{
    // Routing only: NEX authentication and the later Eagle handoff remain game/server code.
    internal static class EagleBootstrap
    {
        public const string ServerHost = "mario35.retromods.co.uk";
        public const int ServerPort = 20000;

        // TEST-NET address used only inside the guest. Never connect to this address on the host.
        public static readonly IPAddress GuestAddress = IPAddress.Parse("192.0.2.35");

        public static ulong GetApplicationTitleId(ServiceCtx context)
        {
            // ServiceCtx.Process is the emulated service, not its caller (ServerBase).
            // Prefer an explicit application PID; legacy requests can omit it.
            ulong? callerTitleId = context.Device.Processes.TryGetProcess(context.ClientProcessId, out var process)
                ? process.ProgramId
                : null;
            return SelectApplicationTitleId(callerTitleId, context.Device.Processes.ActiveApplication?.ProgramId);
        }

        internal static ulong SelectApplicationTitleId(ulong? callerTitleId, ulong? activeTitleId)
        {
            return callerTitleId ?? activeTitleId ?? 0;
        }

        public static string GetBootstrapHost(ulong titleId)
        {
            return titleId switch
            {
                0x0100277011F1A000 => "g21f12900-lp1.s.n.srv.nintendo.net", // Super Mario Bros. 35
                0x010040600C5CE000 => "g23bda200-lp1.s.n.srv.nintendo.net", // Tetris 99
                0x0100AD9012510000 => "g2e471600-lp1.s.n.srv.nintendo.net", // PAC-MAN 99
                _ => null,
            };
        }

        public static bool Matches(ulong titleId, string host)
        {
            string expected = GetBootstrapHost(titleId);
            return expected != null && host != null &&
                string.Equals(expected, host.TrimEnd('.'), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBootstrapEndpoint(ulong titleId, IPEndPoint endpoint)
        {
            return GetBootstrapHost(titleId) != null && endpoint.Address.Equals(GuestAddress);
        }

        public static IPHostEntry CreateHostEntry(string host)
        {
            return new IPHostEntry
            {
                HostName = host,
                Aliases = Array.Empty<string>(),
                AddressList = new[] { GuestAddress },
            };
        }
    }
}
