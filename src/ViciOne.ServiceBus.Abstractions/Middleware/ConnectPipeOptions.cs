// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Flags]
    public enum ConnectPipeOptions
    {
        ConfigureConsumeTopology = 1,

        All = ConfigureConsumeTopology
    }
}
