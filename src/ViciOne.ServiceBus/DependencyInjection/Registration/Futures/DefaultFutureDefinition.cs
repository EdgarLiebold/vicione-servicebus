namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Defines configuration for default future.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public class DefaultFutureDefinition<TFuture> :
    FutureDefinition<TFuture>
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>Configures saga.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="sagaConfigurator">The saga configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalDelayedRedelivery();
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseVolatileOutbox(context);
    }
}
