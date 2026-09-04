using System;

namespace ViciOne.ServiceBus;

[Flags]
public enum ConnectPipeOptions
{
    ConfigureConsumeTopology = 1,

    All = ConfigureConsumeTopology
}
