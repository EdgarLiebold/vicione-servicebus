using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Hosts consumer-kind endpoints that require a service-instance endpoint.</summary>
public interface IConsumerKindHost : IRegistration
{
    /// <summary>Gets the definition of the service-instance endpoint.</summary>
    IEndpointDefinition EndpointDefinition { get; }

    /// <summary>Configures the service instance used by the contributed endpoints.</summary>
    /// <param name="configurator">The service instance to configure.</param>
    /// <param name="context">The active registration context.</param>
    void Configure(IServiceInstanceConfigurator configurator, IRegistrationContext context);
}
