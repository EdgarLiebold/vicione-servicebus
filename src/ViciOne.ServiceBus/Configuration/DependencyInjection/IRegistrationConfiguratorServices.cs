using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Exposes the dependency-injection collection to advanced registration extensions.
/// Application configuration should use the typed registration methods instead.
/// </summary>
public interface IRegistrationConfiguratorServices
{
    /// <summary>Gets the dependency-injection collection owned by this configurator.</summary>
    IServiceCollection Services { get; }

    /// <summary>Gets the bus contract owned by this registration configurator.</summary>
    Type BusType { get; }
}
