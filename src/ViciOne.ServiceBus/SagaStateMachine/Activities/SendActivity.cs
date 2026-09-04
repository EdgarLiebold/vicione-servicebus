using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a send activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SendActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly DestinationAddressProvider<TSaga> _destinationAddressProvider;
    readonly ContextMessageFactory<BehaviorContext<TSaga>, TMessage> _messageFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="destinationAddressProvider">The destination address provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public SendActivity(DestinationAddressProvider<TSaga> destinationAddressProvider,
        ContextMessageFactory<BehaviorContext<TSaga>, TMessage> messageFactory)
    {
        _destinationAddressProvider = destinationAddressProvider;
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
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("send");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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


/// <summary>
/// Provides a send activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SendActivity<TSaga, TData, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, SagaStateMachineInstance
    where TData : class
    where TMessage : class
{
    readonly DestinationAddressProvider<TSaga, TData> _destinationAddressProvider;
    readonly ContextMessageFactory<BehaviorContext<TSaga, TData>, TMessage> _messageFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="destinationAddressProvider">The destination address provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public SendActivity(DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        ContextMessageFactory<BehaviorContext<TSaga, TData>, TMessage> messageFactory)
    {
        _destinationAddressProvider = destinationAddressProvider;
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
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("send");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        var destinationAddress = _destinationAddressProvider(context);

        var endpoint = await context.GetSendEndpointAsync(destinationAddress).ConfigureAwait(false);

        await _messageFactory.UseAsync(context, (ctx, s) => endpoint.SendAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TData, TException> context, IBehavior<TSaga, TData> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
