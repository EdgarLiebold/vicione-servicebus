namespace ViciOne.ServiceBus.Futures.DependencyInjection;

/// <summary>Applies the default retry and outbox policies to a future endpoint.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
internal sealed class DefaultFutureDefinition<TFuture> :
    FutureDefinition<TFuture>
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>Applies the standard delayed-redelivery, retry, and volatile-outbox policies.</summary>
    /// <param name="endpointConfigurator">The future's receive endpoint.</param>
    /// <param name="sagaConfigurator">The future-state saga configurator.</param>
    /// <param name="context">The registration context used to configure the outbox.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalDelayedRedelivery();
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseVolatileOutbox(context);
    }
}
