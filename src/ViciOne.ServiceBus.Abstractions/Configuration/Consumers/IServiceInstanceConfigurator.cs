using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for service instance configurator.
/// </summary>
public interface IServiceInstanceConfigurator :
    IReceiveConfigurator,
    IOptionsSet
{
    /// <summary>
    /// Gets the endpoint name formatter value.
    /// </summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>
    /// If the InstanceEndpoint is enabled, the address of the instance endpoint
    /// </summary>
    Uri InstanceAddress { get; }

    /// <summary>
    /// Gets the bus configurator value.
    /// </summary>
    IReceiveConfigurator BusConfigurator { get; }
    /// <summary>
    /// Gets the instance endpoint configurator value.
    /// </summary>
    IReceiveEndpointConfigurator InstanceEndpointConfigurator { get; }

    /// <summary>
    /// Add a specification for validation
    /// </summary>
    /// <param name="specification"></param>
    void AddSpecification(ISpecification specification);
}


/// <summary>
/// Defines the contract for service instance configurator.
/// </summary>
/// <typeparam name="TEndpointConfigurator">The t endpoint configurator type.</typeparam>
public interface IServiceInstanceConfigurator<out TEndpointConfigurator> :
    IServiceInstanceConfigurator,
    IReceiveConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>
    /// Gets the bus configurator value.
    /// </summary>
    new IReceiveConfigurator<TEndpointConfigurator> BusConfigurator { get; }
    /// <summary>
    /// Gets the instance endpoint configurator value.
    /// </summary>
    new TEndpointConfigurator InstanceEndpointConfigurator { get; }
}
