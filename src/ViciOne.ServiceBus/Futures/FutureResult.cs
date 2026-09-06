using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Represents the outcome of future.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class FutureResult<TCommand, TResult, TInput> :
    ISpecification
    where TCommand : class
    where TResult : class
    where TInput : class
{
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> _factory = null!;

    /// <summary>Gets or sets the factory.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> Factory
    {
        set => _factory = value;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    /// <summary>Sets result.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetResultAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(_factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}


/// <summary>Represents the outcome of future.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public class FutureResult<TCommand, TResult> :
    ISpecification
    where TCommand : class
    where TResult : class
{
    ContextMessageFactory<BehaviorContext<FutureState>, TResult> _factory = null!;

    /// <summary>Gets or sets the factory.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState>, TResult> Factory
    {
        set => _factory = value;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    /// <summary>Sets result.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetResultAsync(BehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(_factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}
