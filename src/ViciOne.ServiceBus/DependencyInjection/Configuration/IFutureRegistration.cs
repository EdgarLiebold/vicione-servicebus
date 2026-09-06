namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by future registration.</summary>
public interface IFutureRegistration :
    IRegistration
{
    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Gets definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    IFutureDefinition GetDefinition(IRegistrationContext context);
}
