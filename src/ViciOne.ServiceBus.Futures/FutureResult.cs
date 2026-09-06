using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Creates, publishes, and persists a future result from an input event.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful result contract.</typeparam>
/// <typeparam name="TInput">The event contract that triggers completion.</typeparam>
internal sealed class FutureResult<TCommand, TResult, TInput> :
    ISpecification
    where TCommand : class
    where TResult : class
    where TInput : class
{
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult>? _factory;

    /// <summary>Sets the factory that creates the successful result from the triggering event.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> Factory
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _factory = value;
        }
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    /// <summary>Completes the future, sends the result to subscribers, and stores the serialized result.</summary>
    /// <param name="context">The future event context that supplies state and result data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetResultAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> factory = _factory
            ?? throw new InvalidOperationException("The future result factory has not been configured.");
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}


/// <summary>Creates, publishes, and persists a future result from future state.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful result contract.</typeparam>
internal sealed class FutureResult<TCommand, TResult> :
    ISpecification
    where TCommand : class
    where TResult : class
{
    ContextMessageFactory<BehaviorContext<FutureState>, TResult>? _factory;

    /// <summary>Sets the factory that creates the successful result from future state.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState>, TResult> Factory
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _factory = value;
        }
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    /// <summary>Completes the future, sends the result to subscribers, and stores the serialized result.</summary>
    /// <param name="context">The future state context used to create the result.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetResultAsync(BehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ContextMessageFactory<BehaviorContext<FutureState>, TResult> factory = _factory
            ?? throw new InvalidOperationException("The future result factory has not been configured.");
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}
