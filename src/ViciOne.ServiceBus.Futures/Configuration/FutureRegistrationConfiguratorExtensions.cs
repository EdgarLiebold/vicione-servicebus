using System;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Futures.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a future through the application registration surface.</summary>
public static class FutureRegistrationConfiguratorExtensions
{
    /// <summary>Adds a future with an optional definition.</summary>
    /// <typeparam name="TFuture">The future state-machine type.</typeparam>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="futureDefinitionType">The runtime future definition type, or <see langword="null" /> for the default.</param>
    /// <returns>A configurator for the registered future.</returns>
    public static IFutureRegistrationConfigurator<TFuture> AddFuture<TFuture>(this IRegistrationConfigurator configurator,
        Type? futureDefinitionType = null)
        where TFuture : class, ISagaStateMachine<FutureState>
    {
        ArgumentNullException.ThrowIfNull(configurator);
        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        IFutureRegistration registration = configurator.Services.RegisterFuture<TFuture>(advanced.Registrar, futureDefinitionType);
        SagaRegistrationCompletionParticipant.RequireRepository<FutureState>(configurator);
        return new FutureRegistrationConfigurator<TFuture>(configurator, registration);
    }
}
