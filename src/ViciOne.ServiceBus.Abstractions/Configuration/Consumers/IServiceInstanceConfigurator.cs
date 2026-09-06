using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures service instance.</summary>
public interface IServiceInstanceConfigurator :
    IReceiveConfigurator,
    IOptionsSet
{
    /// <summary>Gets the endpoint name formatter.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>If the InstanceEndpoint is enabled, the address of the instance endpoint.</summary>
    Uri InstanceAddress { get; }

    /// <summary>Gets the bus configurator.</summary>
    IReceiveConfigurator BusConfigurator { get; }
    /// <summary>Gets the instance endpoint configurator.</summary>
    IReceiveEndpointConfigurator InstanceEndpointConfigurator { get; }

    /// <summary>Add a specification for validation.</summary>
    /// <param name="specification">The specification.</param>
    void AddSpecification(ISpecification specification);
}


/// <summary>Configures service instance.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public interface IServiceInstanceConfigurator<out TEndpointConfigurator> :
    IServiceInstanceConfigurator,
    IReceiveConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Gets the bus configurator.</summary>
    new IReceiveConfigurator<TEndpointConfigurator> BusConfigurator { get; }
    /// <summary>Gets the instance endpoint configurator.</summary>
    new TEndpointConfigurator InstanceEndpointConfigurator { get; }
}
