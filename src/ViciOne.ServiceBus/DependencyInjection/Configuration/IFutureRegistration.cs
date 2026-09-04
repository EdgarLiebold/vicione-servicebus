namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future registration.
/// </summary>
public interface IFutureRegistration :
    IRegistration
{
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IFutureDefinition GetDefinition(IRegistrationContext context);
}
