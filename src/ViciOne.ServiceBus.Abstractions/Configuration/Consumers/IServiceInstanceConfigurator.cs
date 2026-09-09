using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the bus and dedicated receive endpoint for a service instance.</summary>
public interface IServiceInstanceConfigurator :
    IReceiveConfigurator,
    IOptionsSet
{
    /// <summary>Gets the convention used to derive endpoint names.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>Gets the address of the service instance's dedicated receive endpoint.</summary>
    Uri InstanceAddress { get; }

    /// <summary>Gets the configurator for shared bus receive endpoints.</summary>
    IReceiveConfigurator BusConfigurator { get; }
    /// <summary>Gets the configurator for the service instance's dedicated receive endpoint.</summary>
    IReceiveEndpointConfigurator InstanceEndpointConfigurator { get; }

    /// <summary>Adds a configuration specification to the validation set.</summary>
    /// <param name="specification">The specification to validate before the bus starts.</param>
    void AddSpecification(ISpecification specification);
}


/// <summary>Configures a service instance using a transport-specific endpoint configurator.</summary>
/// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
public interface IServiceInstanceConfigurator<out TEndpointConfigurator> :
    IServiceInstanceConfigurator,
    IReceiveConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Gets the transport-specific configurator for shared bus receive endpoints.</summary>
    new IReceiveConfigurator<TEndpointConfigurator> BusConfigurator { get; }
    /// <summary>Gets the transport-specific configurator for the service instance's dedicated receive endpoint.</summary>
    new TEndpointConfigurator InstanceEndpointConfigurator { get; }
}
