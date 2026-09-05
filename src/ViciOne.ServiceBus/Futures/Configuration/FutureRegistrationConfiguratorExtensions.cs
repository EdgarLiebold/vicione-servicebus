using System;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides the registration surface contributed by the Futures capability package.
/// </summary>
public static class FutureRegistrationConfiguratorExtensions
{
    /// <summary>
    /// Adds a future with an optional definition.
    /// </summary>
    public static IFutureRegistrationConfigurator<TFuture> AddFuture<TFuture>(this IRegistrationConfigurator configurator,
        Type? futureDefinitionType = null)
        where TFuture : class, SagaStateMachine<FutureState>
    {
        ArgumentNullException.ThrowIfNull(configurator);
        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        IFutureRegistration registration = configurator.Services.RegisterFuture<TFuture>(advanced.Registrar, futureDefinitionType);
        SagaRegistrationCompletionParticipant.RequireRepository<FutureState>(configurator);
        return new FutureRegistrationConfigurator<TFuture>(configurator, registration);
    }
}
