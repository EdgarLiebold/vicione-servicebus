using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by registration.</summary>
public interface IRegistration
{
    /// <summary>Gets the type.</summary>
    Type Type { get; }

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    bool IncludeInConfigureEndpoints { get; set; }
}
