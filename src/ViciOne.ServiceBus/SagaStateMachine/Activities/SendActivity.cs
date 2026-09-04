using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class SendActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly DestinationAddressProvider<TSaga> _destinationAddressProvider;
    readonly ContextMessageFactory<BehaviorContext<TSaga>, TMessage> _messageFactory;

    public SendActivity(DestinationAddressProvider<TSaga> destinationAddressProvider,
        ContextMessageFactory<BehaviorContext<TSaga>, TMessage> messageFactory)
    {
        _destinationAddressProvider = destinationAddressProvider;
        _messageFactory = messageFactory;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("send");
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    async Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        var destinationAddress = _destinationAddressProvider(context);

        var endpoint = await context.GetSendEndpointAsync(destinationAddress).ConfigureAwait(false);

        await _messageFactory.UseAsync(context, (ctx, s) => endpoint.SendAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);
    }
}


public class SendActivity<TSaga, TData, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, SagaStateMachineInstance
    where TData : class
    where TMessage : class
{
    readonly DestinationAddressProvider<TSaga, TData> _destinationAddressProvider;
    readonly ContextMessageFactory<BehaviorContext<TSaga, TData>, TMessage> _messageFactory;

    public SendActivity(DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        ContextMessageFactory<BehaviorContext<TSaga, TData>, TMessage> messageFactory)
    {
        _destinationAddressProvider = destinationAddressProvider;
        _messageFactory = messageFactory;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("send");
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        var destinationAddress = _destinationAddressProvider(context);

        var endpoint = await context.GetSendEndpointAsync(destinationAddress).ConfigureAwait(false);

        await _messageFactory.UseAsync(context, (ctx, s) => endpoint.SendAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TData, TException> context, IBehavior<TSaga, TData> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
