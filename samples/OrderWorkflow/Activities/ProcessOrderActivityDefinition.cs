namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Activities;

using Contracts;

public sealed class ProcessOrderActivityDefinition :
    ActivityDefinition<ProcessOrderActivity, ProcessOrderArguments, ProcessOrderLog>
{
    public ProcessOrderActivityDefinition()
    {
        ExecuteEndpoint(endpoint => endpoint.PrefetchCount = 16);
        CompensateEndpoint(endpoint => endpoint.PrefetchCount = 4);
    }

    protected override void ConfigureExecuteActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<ProcessOrderActivity, ProcessOrderArguments> executeActivityConfigurator,
        IRegistrationContext context)
    {
    }

    protected override void ConfigureCompensateActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        ICompensateActivityConfigurator<ProcessOrderActivity, ProcessOrderLog> compensateActivityConfigurator,
        IRegistrationContext context)
    {
    }
}
