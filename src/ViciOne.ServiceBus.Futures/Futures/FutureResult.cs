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
    ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TResult>? _factory;

    /// <summary>Sets the factory that creates the successful result from the triggering event.</summary>
    public ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TResult> Factory
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
            yield return this.Failure("Result", "Factory", "SetResultFactory or SetResultInitializer must be configured");
    }

    /// <summary>Completes the future, sends the result to subscribers, and stores the serialized result.</summary>
    /// <param name="context">The future event context that supplies state and result data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetResultAsync(IBehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TResult> factory = _factory
            ?? throw new InvalidOperationException("The future result factory has not been configured.");
        DateTimeOffset? previousCompleted = context.Saga.Completed;
        bool wasPending = context.Saga.Pending.Contains(context.Saga.CorrelationId);
        context.SetCompleted(context.Saga.CorrelationId);

        try
        {
            var result = await context.SendMessageToSubscriptionsAsync(factory,
                context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

            context.SetResult(context.Saga.CorrelationId, result);
        }
        catch
        {
            context.Saga.Completed = previousCompleted;
            if (wasPending)
                context.Saga.Pending.Add(context.Saga.CorrelationId);

            throw;
        }
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
    ContextMessageFactory<IBehaviorContext<FutureState>, TResult>? _factory;

    /// <summary>Sets the factory that creates the successful result from future state.</summary>
    public ContextMessageFactory<IBehaviorContext<FutureState>, TResult> Factory
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
            yield return this.Failure("Result", "Factory", "SetResultFactory or SetResultInitializer must be configured");
    }

    /// <summary>Completes the future, sends the result to subscribers, and stores the serialized result.</summary>
    /// <param name="context">The future state context used to create the result.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetResultAsync(IBehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ContextMessageFactory<IBehaviorContext<FutureState>, TResult> factory = _factory
            ?? throw new InvalidOperationException("The future result factory has not been configured.");
        DateTimeOffset? previousCompleted = context.Saga.Completed;
        bool wasPending = context.Saga.Pending.Contains(context.Saga.CorrelationId);
        context.SetCompleted(context.Saga.CorrelationId);

        try
        {
            var result = await context.SendMessageToSubscriptionsAsync(factory,
                context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

            context.SetResult(context.Saga.CorrelationId, result);
        }
        catch
        {
            context.Saga.Completed = previousCompleted;
            if (wasPending)
                context.Saga.Pending.Add(context.Saga.CorrelationId);

            throw;
        }
    }
}
