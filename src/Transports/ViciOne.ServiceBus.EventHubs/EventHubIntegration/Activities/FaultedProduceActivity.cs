using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.EventHubs.Activities;

/// <summary>
/// Provides a faulted produce activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class FaultedProduceActivity<TSaga, TException, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TException : Exception
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> _messageFactory;
    readonly ExceptionEventHubNameProvider<TSaga, TException> _nameProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedProduceActivity(ExceptionEventHubNameProvider<TSaga, TException> nameProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> messageFactory)
    {
        _nameProvider = nameProvider;
        _messageFactory = messageFactory;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="inspector">The inspector value.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TOtherException">The t other exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T, TOtherException>(BehaviorExceptionContext<TSaga, T, TOtherException> context,
        IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
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


/// <summary>
/// Provides a faulted produce activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class FaultedProduceActivity<TSaga, TData, TException, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, SagaStateMachineInstance
    where TData : class
    where TMessage : class
    where TException : Exception
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> _messageFactory;
    readonly ExceptionEventHubNameProvider<TSaga, TData, TException> _nameProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedProduceActivity(ExceptionEventHubNameProvider<TSaga, TData, TException> nameProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> messageFactory)
    {
        _messageFactory = messageFactory;
        _nameProvider = nameProvider;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="inspector">The inspector value.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("produce-faulted");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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
