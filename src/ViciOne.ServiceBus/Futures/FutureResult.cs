using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a future result implementation.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class FutureResult<TCommand, TResult, TInput> :
    ISpecification
    where TCommand : class
    where TResult : class
    where TInput : class
{
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> _factory = null!;

    /// <summary>
    /// Gets or sets the factory value.
    /// </summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TResult> Factory
    {
        set => _factory = value;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SetResultAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(_factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}


/// <summary>
/// Provides a future result implementation.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
public class FutureResult<TCommand, TResult> :
    ISpecification
    where TCommand : class
    where TResult : class
{
    ContextMessageFactory<BehaviorContext<FutureState>, TResult> _factory = null!;

    /// <summary>
    /// Gets or sets the factory value.
    /// </summary>
    public ContextMessageFactory<BehaviorContext<FutureState>, TResult> Factory
    {
        set => _factory = value;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SetResultAsync(BehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        context.SetCompleted(context.Saga.CorrelationId);

        var result = await context.SendMessageToSubscriptionsAsync(_factory,
            context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

        context.SetResult(context.Saga.CorrelationId, result);
    }
}
