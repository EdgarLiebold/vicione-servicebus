using ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Activities;

public sealed class ProcessOrderActivity :
    IActivity<ProcessOrderArguments, ProcessOrderLog>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ProcessOrderArguments> context)
    {
        Guid shipmentId = NewId.NextGuid();
        return Task.FromResult(context.Completed<ProcessOrderLog>(new
        {
            context.Arguments.OrderId,
            ShipmentId = shipmentId,
        }));
    }

    public Task<CompensationResult> CompensateAsync(CompensateContext<ProcessOrderLog> context) =>
        Task.FromResult(context.Compensated());
}
