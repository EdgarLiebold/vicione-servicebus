using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for registration.
/// </summary>
public interface IRegistration
{
    /// <summary>
    /// Gets the type value.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets or sets the include in configure endpoints value.
    /// </summary>
    bool IncludeInConfigureEndpoints { get; set; }
}
