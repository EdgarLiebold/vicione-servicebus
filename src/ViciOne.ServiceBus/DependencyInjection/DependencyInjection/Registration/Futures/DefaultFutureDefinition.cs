namespace ViciOne.ServiceBus.DependencyInjection.Registration
{
    public class DefaultFutureDefinition<TFuture> :
        FutureDefinition<TFuture>
        where TFuture : class, SagaStateMachine<FutureState>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseTechnicalDelayedRedelivery();
            endpointConfigurator.UseTechnicalMessageRetry();
            endpointConfigurator.UseInMemoryOutbox(context);
        }
    }
}
