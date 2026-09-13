using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Creates, publishes, and persists a terminal future fault from an input event.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TFault">The terminal fault contract.</typeparam>
/// <typeparam name="TInput">The event contract that triggers the fault.</typeparam>
internal sealed class FutureFault<TCommand, TFault, TInput> :
    ISpecification
    where TCommand : class
    where TFault : class
    where TInput : class
{
    static readonly object _defaultValues = new { };
    ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TFault> _factory;

    /// <summary>Creates a fault producer with the conventional fault-message mapping.</summary>
    public FutureFault()
    {
        _factory = new ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TFault>(DefaultFactoryAsync);
    }

    /// <summary>Sets the factory that creates the terminal fault from the triggering event.</summary>
    public ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TFault> Factory
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _factory = value;
        }
    }

    /// <summary>Gets or sets whether terminal fault publication waits until no operation remains pending.</summary>
    public bool WaitForPending { get; set; }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Tries to fault the future, send the fault to subscribers, and store it.</summary>
    /// <param name="context">The future event context that supplies state and fault data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the terminal fault was emitted; otherwise, <see langword="false" /> while operations remain pending.</returns>
    public async Task<bool> TrySetFaultedAsync(IBehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (WaitForPending && context.Saga.HasPending())
            return false;

        DateTimeOffset? previousFaulted = context.Saga.Faulted;
        bool wasPending = context.Saga.Pending.Contains(context.Saga.CorrelationId);
        context.SetFaulted(context.Saga.CorrelationId);

        try
        {
            var fault = await context.SendMessageToSubscriptionsAsync(_factory,
                context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

            context.SetFault(context.Saga.CorrelationId, fault);
            return true;
        }
        catch
        {
            context.Saga.Faulted = previousFaulted;
            if (wasPending)
                context.Saga.Pending.Add(context.Saga.CorrelationId);

            throw;
        }
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> DefaultFactoryAsync(IBehaviorContext<FutureState, TInput> context)
    {
        if (context.Message is Fault fault)
        {
            var request = context.GetCommand<TCommand>();

            return context.InitAsync<TFault>(new
            {
                fault.FaultId,
                fault.FaultedMessageId,
                fault.Timestamp,
                fault.Exceptions,
                fault.Host,
                fault.FaultMessageTypes,
                Message = request
            });
        }

        return context.InitAsync<TFault>(_defaultValues);
    }
}


/// <summary>Creates, publishes, and persists a terminal future fault from future state.</summary>
/// <typeparam name="TFault">The terminal fault contract.</typeparam>
internal sealed class FutureFault<TFault> :
    ISpecification
    where TFault : class
{
    static readonly object _defaultValues = new { };
    ContextMessageFactory<IBehaviorContext<FutureState>, TFault> _factory;

    /// <summary>Creates a fault producer with an empty conventional initializer.</summary>
    public FutureFault()
    {
        _factory = MessageFactory<TFault>.Create((Func<IBehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)DefaultFactoryAsync);
    }

    /// <summary>Sets the factory that creates the terminal fault from future state.</summary>
    public ContextMessageFactory<IBehaviorContext<FutureState>, TFault> Factory
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _factory = value;
        }
    }

    /// <summary>Gets or sets whether terminal fault publication waits until no operation remains pending.</summary>
    public bool WaitForPending { get; set; }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Tries to fault the future, send the fault to subscribers, and store it.</summary>
    /// <param name="context">The future state context used to create the fault.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the terminal fault was emitted; otherwise, <see langword="false" /> while operations remain pending.</returns>
    public async Task<bool> TrySetFaultedAsync(IBehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (WaitForPending && context.Saga.HasPending())
            return false;

        DateTimeOffset? previousFaulted = context.Saga.Faulted;
        bool wasPending = context.Saga.Pending.Contains(context.Saga.CorrelationId);
        context.SetFaulted(context.Saga.CorrelationId);

        try
        {
            var fault = await context.SendMessageToSubscriptionsAsync(_factory,
                context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

            context.SetFault(context.Saga.CorrelationId, fault);
            return true;
        }
        catch
        {
            context.Saga.Faulted = previousFaulted;
            if (wasPending)
                context.Saga.Pending.Add(context.Saga.CorrelationId);

            throw;
        }
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> DefaultFactoryAsync(IBehaviorContext<FutureState> context)
    {
        return context.InitAsync<TFault>(_defaultValues);
    }
}
