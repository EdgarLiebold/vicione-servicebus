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
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault> _factory;

    /// <summary>Creates a fault producer with the conventional fault-message mapping.</summary>
    public FutureFault()
    {
        _factory = new ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault>(DefaultFactoryAsync);
    }

    /// <summary>Sets the factory that creates the terminal fault from the triggering event.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault> Factory
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

    /// <summary>Faults the future when permitted, sends the fault to subscribers, and stores it.</summary>
    /// <param name="context">The future event context that supplies state and fault data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetFaultedAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!WaitForPending || !context.Saga.HasPending())
        {
            context.SetFaulted(context.Saga.CorrelationId);

            var fault = await context.SendMessageToSubscriptionsAsync(_factory,
                context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

            context.SetFault(context.Saga.CorrelationId, fault);
        }
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> DefaultFactoryAsync(BehaviorContext<FutureState, TInput> context)
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
    ContextMessageFactory<BehaviorContext<FutureState>, TFault> _factory;

    /// <summary>Creates a fault producer with an empty conventional initializer.</summary>
    public FutureFault()
    {
        _factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)DefaultFactoryAsync);
    }

    /// <summary>Sets the factory that creates the terminal fault from future state.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState>, TFault> Factory
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

    /// <summary>Faults the future when permitted, sends the fault to subscribers, and stores it.</summary>
    /// <param name="context">The future state context used to create the fault.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetFaultedAsync(BehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!WaitForPending || !context.Saga.HasPending())
        {
            context.SetFaulted(context.Saga.CorrelationId);

            var fault = await context.SendMessageToSubscriptionsAsync(_factory,
                context.Saga.HasSubscriptions() ? context.Saga.Subscriptions.ToArray() : [], cancellationToken: cancellationToken);

            context.SetFault(context.Saga.CorrelationId, fault);
        }
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> DefaultFactoryAsync(BehaviorContext<FutureState> context)
    {
        return context.InitAsync<TFault>(_defaultValues);
    }
}
