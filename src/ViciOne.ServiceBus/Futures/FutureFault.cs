using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a future fault implementation.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class FutureFault<TCommand, TFault, TInput> :
    ISpecification
    where TCommand : class
    where TFault : class
    where TInput : class
{
    static readonly object _defaultValues = new Default();
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault> _factory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public FutureFault()
    {
        _factory = new ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault>(DefaultFactoryAsync);
    }

    /// <summary>
    /// Gets or sets the factory value.
    /// </summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault> Factory
    {
        set => _factory = value;
    }

    /// <summary>
    /// Gets or sets the wait for pending value.
    /// </summary>
    public bool WaitForPending { get; set; }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Sets faulted.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SetFaultedAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
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


    class Default
    {
    }
}


/// <summary>
/// Provides a future fault implementation.
/// </summary>
/// <typeparam name="TFault">The t fault type.</typeparam>
public class FutureFault<TFault> :
    ISpecification
    where TFault : class
{
    static readonly object _defaultValues = new Default();
    ContextMessageFactory<BehaviorContext<FutureState>, TFault> _factory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public FutureFault()
    {
        _factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)DefaultFactoryAsync);
    }

    /// <summary>
    /// Gets or sets the factory value.
    /// </summary>
    public ContextMessageFactory<BehaviorContext<FutureState>, TFault> Factory
    {
        set => _factory = value;
    }

    /// <summary>
    /// Gets or sets the wait for pending value.
    /// </summary>
    public bool WaitForPending { get; set; }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Sets faulted.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SetFaultedAsync(BehaviorContext<FutureState> context, CancellationToken cancellationToken = default)
    {
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


    class Default
    {
    }
}
