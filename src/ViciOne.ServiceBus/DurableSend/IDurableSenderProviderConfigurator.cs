using System;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Extension surface used by persistence and transport provider packages.</summary>
public interface IDurableSenderProviderConfigurator
{
    /// <summary>
    /// Gets the bus type value.
    /// </summary>
    Type BusType { get; }

    /// <summary>
    /// Gets the services value.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Configures store for the current pipeline.
    /// </summary>
    /// <param name="implementationType">The implementation type value.</param>
    void UseStore(Type implementationType);

    /// <summary>
    /// Configures dispatcher for the current pipeline.
    /// </summary>
    /// <param name="implementationType">The implementation type value.</param>
    void UseDispatcher(Type implementationType);
}
