using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

public class FutureResult<TCommand, TResult, TInput> :
    ISpecification
    where TCommand : class
    where TResult : class
    where TInput : class
{
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> _factory = null!;

    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> Factory
    {
        set => _factory = value;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    public async Task SetResultAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(_factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}


public class FutureResult<TCommand, TResult> :
    ISpecification
    where TCommand : class
    where TResult : class
{
    ContextMessageFactory<BehaviorContext<FutureState>, TResult> _factory = null!;

    public ContextMessageFactory<BehaviorContext<FutureState>, TResult> Factory
    {
        set => _factory = value;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    public async Task SetResultAsync(BehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(_factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}
