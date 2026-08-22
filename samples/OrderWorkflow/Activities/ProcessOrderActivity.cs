namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Activities;

using Contracts;

public sealed class ProcessOrderActivity :
    IActivity<ProcessOrderArguments, ProcessOrderLog>
{
    public Task<ExecutionResult> Execute(ExecuteContext<ProcessOrderArguments> context)
    {
        Guid shipmentId = NewId.NextGuid();
        return Task.FromResult(context.Completed<ProcessOrderLog>(new
        {
            context.Arguments.OrderId,
            ShipmentId = shipmentId,
        }));
    }

    public Task<CompensationResult> Compensate(CompensateContext<ProcessOrderLog> context) =>
        Task.FromResult(context.Compensated());
}
