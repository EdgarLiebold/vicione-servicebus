namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Sagas;

public sealed class OrderDeliverySagaDefinition :
    SagaDefinition<OrderDeliverySaga>
{
    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<OrderDeliverySaga> sagaConfigurator,
        IRegistrationContext context)
    {
    }
}
