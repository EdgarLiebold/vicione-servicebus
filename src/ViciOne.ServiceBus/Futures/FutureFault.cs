using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

public class FutureFault<TCommand, TFault, TInput> :
    ISpecification
    where TCommand : class
    where TFault : class
    where TInput : class
{
    static readonly object _defaultValues = new Default();
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault> _factory;

    public FutureFault()
    {
        _factory = new ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault>(DefaultFactoryAsync);
    }

    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TFault> Factory
    {
        set => _factory = value;
    }

    public bool WaitForPending { get; set; }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

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

    static Task<SendTuple<TFault>> DefaultFactoryAsync(BehaviorContext<FutureState, TInput> context)
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


public class FutureFault<TFault> :
    ISpecification
    where TFault : class
{
    static readonly object _defaultValues = new Default();
    ContextMessageFactory<BehaviorContext<FutureState>, TFault> _factory;

    public FutureFault()
    {
        _factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState>, Task<SendTuple<TFault>>>)DefaultFactoryAsync);
    }

    public ContextMessageFactory<BehaviorContext<FutureState>, TFault> Factory
    {
        set => _factory = value;
    }

    public bool WaitForPending { get; set; }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

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

    static Task<SendTuple<TFault>> DefaultFactoryAsync(BehaviorContext<FutureState> context)
    {
        return context.InitAsync<TFault>(_defaultValues);
    }


    class Default
    {
    }
}
