using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Identifies a registered component and whether conventional endpoint configuration includes it.</summary>
public interface IRegistration
{
    /// <summary>Gets the registered component type.</summary>
    Type Type { get; }

    /// <summary>Gets or sets whether conventional endpoint configuration includes the component.</summary>
    bool IncludeInConfigureEndpoints { get; set; }
}
