using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class PublishActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly ContextMessageFactory<BehaviorContext<TSaga>, TMessage> _messageFactory;

    public PublishActivity(ContextMessageFactory<BehaviorContext<TSaga>, TMessage> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("publish");
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await _messageFactory.UseAsync(context, (ctx, s) => ctx.PublishAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        await _messageFactory.UseAsync(context, (ctx, s) => ctx.PublishAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

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
}


public class PublishActivity<TSaga, TMessage, T> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class
{
    readonly ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> _messageFactory;

    public PublishActivity(ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("publish");
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await _messageFactory.UseAsync(context, (ctx, s) => ctx.PublishAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
