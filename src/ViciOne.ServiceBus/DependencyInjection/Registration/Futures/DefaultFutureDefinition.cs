namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a default future definition implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public class DefaultFutureDefinition<TFuture> :
    FutureDefinition<TFuture>
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>
    /// Configures saga.
    /// </summary>
    /// <param name="endpointConfigurator">The endpoint configurator value.</param>
    /// <param name="sagaConfigurator">The saga configurator value.</param>
    /// <param name="context">The operation context.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalDelayedRedelivery();
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseVolatileOutbox(context);
    }
}
