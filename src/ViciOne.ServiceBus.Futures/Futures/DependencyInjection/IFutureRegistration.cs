namespace ViciOne.ServiceBus.Futures.DependencyInjection;

/// <summary>Connects a registered future to endpoint discovery and endpoint configuration.</summary>
internal interface IFutureRegistration :
    IRegistration
{
    /// <summary>Connects the registered future and its repository to a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint that hosts the future.</param>
    /// <param name="context">The registration context that resolves the state machine and repository.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Gets the resolved definition for the registered future.</summary>
    /// <param name="context">The registration context used to resolve the definition.</param>
    /// <returns>The explicit definition, or the default future definition.</returns>
    IFutureDefinition GetDefinition(IRegistrationContext context);
}
