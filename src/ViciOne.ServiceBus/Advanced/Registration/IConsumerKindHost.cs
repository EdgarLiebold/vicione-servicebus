using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Hosts consumer-kind endpoints that require a service-instance endpoint.</summary>
public interface IConsumerKindHost : IRegistration
{
    /// <summary>Gets the definition for the service-instance endpoint.</summary>
    IEndpointDefinition EndpointDefinition { get; }

    /// <summary>Configures the service instance used by the contributed endpoints.</summary>
    void Configure(IServiceInstanceConfigurator configurator, IRegistrationContext context);
}
