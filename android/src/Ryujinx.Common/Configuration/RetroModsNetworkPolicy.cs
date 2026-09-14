namespace Ryujinx.Common.Configuration
{
    public static class RetroModsNetworkPolicy
    {
        public static bool RequiresGuestInternet(ulong applicationId) => applicationId is
            0x0100277011F1A000 or // Super Mario Bros. 35
            0x010040600C5CE000 or // Tetris 99
            0x0100AD9012510000;   // PAC-MAN 99

        public static bool EffectiveGuestInternet(bool configured, ulong applicationId) =>
            configured || RequiresGuestInternet(applicationId);
    }
}
