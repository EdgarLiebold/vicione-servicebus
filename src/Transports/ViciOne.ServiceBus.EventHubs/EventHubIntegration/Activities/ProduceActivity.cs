using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.EventHubs.Activities;

/// <summary>Produces a message to a context-selected Event Hub during a saga behavior.</summary>
/// <typeparam name="TSaga">The saga instance type.</typeparam>
/// <typeparam name="TMessage">The produced message type.</typeparam>
public class ProduceActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly ContextMessageFactory<BehaviorContext<TSaga>, TMessage> _messageFactory;
    readonly EventHubNameProvider<TSaga> _nameProvider;

    /// <summary>Creates the activity from destination and message factories.</summary>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="messageFactory">Creates the outbound message and initializer pipe.</param>
    public ProduceActivity(EventHubNameProvider<TSaga> nameProvider, ContextMessageFactory<BehaviorContext<TSaga>, TMessage> messageFactory)
    {
        _nameProvider = nameProvider;
        _messageFactory = messageFactory;
    }

    /// <summary>Reports this activity to a state-machine visitor.</summary>
    /// <param name="inspector">The visitor receiving this activity.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to which the activity scope is added.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("produce");
    }

    /// <summary>Produces the message and then continues the behavior.</summary>
    /// <param name="context">The current saga behavior context.</param>
    /// <param name="next">The next behavior stage.</param>
    /// <returns>A task that completes after production and the remaining behavior finish.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Produces the message from a data-bearing context and then continues the behavior.</summary>
    /// <typeparam name="T">The current behavior data type.</typeparam>
    /// <param name="context">The current saga behavior context and data.</param>
    /// <param name="next">The next behavior stage.</param>
    /// <returns>A task that completes after production and the remaining behavior finish.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Passes a saga behavior fault to the next stage without producing a message.</summary>
    /// <typeparam name="TException">The propagated exception type.</typeparam>
    /// <param name="context">The current exception behavior context.</param>
    /// <param name="next">The next fault-handling stage.</param>
    /// <returns>The task returned by the next fault-handling stage.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>Passes a data-bearing behavior fault to the next stage without producing a message.</summary>
    /// <typeparam name="T">The current behavior data type.</typeparam>
    /// <typeparam name="TException">The propagated exception type.</typeparam>
    /// <param name="context">The current exception behavior context and data.</param>
    /// <param name="next">The next fault-handling stage.</param>
    /// <returns>The task returned by the next fault-handling stage.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return _messageFactory.UseAsync(context, async (ctx, s) =>
        {
            var producer = await ctx.GetProducerAsync(ctx, _nameProvider(ctx)).ConfigureAwait(false);

            await producer.ProduceAsync(s.Message, s.Pipe, ctx.CancellationToken).ConfigureAwait(false);
        });
    }
}


/// <summary>Produces a message from a data-bearing saga behavior to a context-selected Event Hub.</summary>
/// <typeparam name="TSaga">The saga instance type.</typeparam>
/// <typeparam name="TMessage">The behavior message type.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
public class ProduceActivity<TSaga, TMessage, T> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class
{
    readonly ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> _messageFactory;
    readonly EventHubNameProvider<TSaga, TMessage> _nameProvider;

    /// <summary>Creates the activity from destination and message factories.</summary>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="messageFactory">Creates the outbound message and initializer pipe.</param>
    public ProduceActivity(EventHubNameProvider<TSaga, TMessage> nameProvider, ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> messageFactory)
    {
        _nameProvider = nameProvider;
        _messageFactory = messageFactory;
    }

    /// <summary>Reports this activity to a state-machine visitor.</summary>
    /// <param name="inspector">The visitor receiving this activity.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to which the activity scope is added.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("produce");
    }

    /// <summary>Produces the message and then continues the behavior.</summary>
    /// <param name="context">The current saga behavior context and message.</param>
    /// <param name="next">The next behavior stage.</param>
    /// <returns>A task that completes after production and the remaining behavior finish.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await _messageFactory.UseAsync(context, async (ctx, s) =>
        {
            var producer = await ctx.GetProducerAsync(ctx, _nameProvider(ctx)).ConfigureAwait(false);

            await producer.ProduceAsync(s.Message, s.Pipe, ctx.CancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Passes a behavior fault to the next stage without producing a message.</summary>
    /// <typeparam name="TException">The propagated exception type.</typeparam>
    /// <param name="context">The current exception behavior context and message.</param>
    /// <param name="next">The next fault-handling stage.</param>
    /// <returns>The task returned by the next fault-handling stage.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
