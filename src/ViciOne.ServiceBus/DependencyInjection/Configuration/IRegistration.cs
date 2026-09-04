using System;

namespace ViciOne.ServiceBus.Configuration;

public interface IRegistration
{
    Type Type { get; }

    bool IncludeInConfigureEndpoints { get; set; }
}
