namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes a consumed message in the context of its selected saga instance.</summary>
/// <remarks>An implementation that owns terminal message handling may intentionally not invoke its continuation.</remarks>
/// <typeparam name="TSaga">The saga instance type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public interface ISagaMessageFilter<TSaga, TMessage> :
    IFilter<SagaConsumeContext<TSaga, TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
}
