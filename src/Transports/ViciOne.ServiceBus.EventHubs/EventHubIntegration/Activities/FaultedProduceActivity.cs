using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.EventHubs.Activities;

/// <summary>Produces a message to a context-selected Event Hub when a saga behavior faults with a matching exception.</summary>
/// <typeparam name="TSaga">The saga instance type.</typeparam>
/// <typeparam name="TException">The exception type that triggers production.</typeparam>
/// <typeparam name="TMessage">The produced message type.</typeparam>
public class FaultedProduceActivity<TSaga, TException, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TException : Exception
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> _messageFactory;
    readonly ExceptionEventHubNameProvider<TSaga, TException> _nameProvider;

    /// <summary>Creates the fault activity from destination and message factories.</summary>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="messageFactory">Creates the outbound message and initializer pipe.</param>
    public FaultedProduceActivity(ExceptionEventHubNameProvider<TSaga, TException> nameProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> messageFactory)
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

    /// <summary>Continues a successful behavior without producing a fault message.</summary>
    /// <param name="context">The current saga behavior context.</param>
    /// <param name="next">The next behavior stage.</param>
    /// <returns>The task returned by the next behavior stage.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Continues a successful data-bearing behavior without producing a fault message.</summary>
    /// <typeparam name="T">The current behavior data type.</typeparam>
    /// <param name="context">The current saga behavior context and data.</param>
    /// <param name="next">The next behavior stage.</param>
    /// <returns>The task returned by the next behavior stage.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Produces the configured fault message for a matching exception and then continues fault handling.</summary>
    /// <typeparam name="T">The actual exception type.</typeparam>
    /// <param name="context">The current exception behavior context.</param>
    /// <param name="next">The next fault-handling stage.</param>
    /// <returns>A task that completes after conditional production and downstream fault handling.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Conditionally produces a fault message from a data-bearing context and then continues fault handling.</summary>
    /// <typeparam name="T">The current behavior data type.</typeparam>
    /// <typeparam name="TOtherException">The actual exception type.</typeparam>
    /// <param name="context">The current exception behavior context and data.</param>
    /// <param name="next">The next fault-handling stage.</param>
    /// <returns>A task that completes after conditional production and downstream fault handling.</returns>
    public async Task FaultedAsync<T, TOtherException>(BehaviorExceptionContext<TSaga, T, TOtherException> context,
        IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to which the fault activity scope is added.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("produce-faulted");
    }

    Task FaultedAsync(BehaviorContext<TSaga> context)
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            return _messageFactory.UseAsync(exceptionContext, async (ctx, s) =>
            {
                var producer = await ctx.GetProducerAsync(ctx, _nameProvider(ctx)).ConfigureAwait(false);

                await producer.ProduceAsync(s.Message, s.Pipe, ctx.CancellationToken).ConfigureAwait(false);
            });
        }

        return Task.CompletedTask;
    }
}


/// <summary>Produces a message from a data-bearing behavior when it faults with a matching exception.</summary>
/// <typeparam name="TSaga">The saga instance type.</typeparam>
/// <typeparam name="TData">The behavior data type.</typeparam>
/// <typeparam name="TException">The exception type that triggers production.</typeparam>
/// <typeparam name="TMessage">The produced message type.</typeparam>
public class FaultedProduceActivity<TSaga, TData, TException, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, SagaStateMachineInstance
    where TData : class
    where TMessage : class
    where TException : Exception
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> _messageFactory;
    readonly ExceptionEventHubNameProvider<TSaga, TData, TException> _nameProvider;

    /// <summary>Creates the fault activity from destination and message factories.</summary>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="messageFactory">Creates the outbound message and initializer pipe.</param>
    public FaultedProduceActivity(ExceptionEventHubNameProvider<TSaga, TData, TException> nameProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> messageFactory)
    {
        _messageFactory = messageFactory;
        _nameProvider = nameProvider;
    }

    /// <summary>Reports this activity to a state-machine visitor.</summary>
    /// <param name="inspector">The visitor receiving this activity.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to which the fault activity scope is added.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("produce-faulted");
    }

    /// <summary>Continues a successful behavior without producing a fault message.</summary>
    /// <param name="context">The current saga behavior context and data.</param>
    /// <param name="next">The next behavior stage.</param>
    /// <returns>The task returned by the next behavior stage.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Produces the configured fault message for a matching exception and then continues fault handling.</summary>
    /// <typeparam name="T">The actual exception type.</typeparam>
    /// <param name="context">The current exception behavior context and data.</param>
    /// <param name="next">The next fault-handling stage.</param>
    /// <returns>A task that completes after conditional production and downstream fault handling.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TData, T> context,
        IBehavior<TSaga, TData> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TData, TException> exceptionContext)
        {
            await _messageFactory.UseAsync(exceptionContext, async (ctx, s) =>
            {
                var producer = await ctx.GetProducerAsync(ctx, _nameProvider(ctx)).ConfigureAwait(false);

                await producer.ProduceAsync(s.Message, s.Pipe, ctx.CancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
