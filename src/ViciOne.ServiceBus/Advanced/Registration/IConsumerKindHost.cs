using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Hosts consumer-kind endpoints that require a service-instance endpoint.</summary>
public interface IConsumerKindHost : IRegistration
{
    /// <summary>Gets the endpoint definition.</summary>
    IEndpointDefinition EndpointDefinition { get; }

    /// <summary>Configures the service instance used by the contributed endpoints.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IServiceInstanceConfigurator configurator, IRegistrationContext context);
}
