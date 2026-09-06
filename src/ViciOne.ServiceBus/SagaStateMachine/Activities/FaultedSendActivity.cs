using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the faulted send activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class FaultedSendActivity<TSaga, TException, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TException : Exception
{
    readonly DestinationAddressProvider<TSaga> _destinationAddressProvider;
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> _messageFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    public FaultedSendActivity(DestinationAddressProvider<TSaga> destinationAddressProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> messageFactory)
    {
        _destinationAddressProvider = destinationAddressProvider;
        _messageFactory = messageFactory;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="inspector">The inspector.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("send-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
            await SendAsync(exceptionContext).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TOtherException">The other exception type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T, TOtherException>(BehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
            await SendAsync(exceptionContext).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    async Task SendAsync(BehaviorExceptionContext<TSaga, TException> exceptionContext)
    {
        var destinationAddress = _destinationAddressProvider(exceptionContext);

        var endpoint = await exceptionContext.GetSendEndpointAsync(destinationAddress).ConfigureAwait(false);

        await _messageFactory.UseAsync(exceptionContext, (ctx, s) => endpoint.SendAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);
    }
}


/// <summary>Executes the faulted send activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class FaultedSendActivity<TSaga, TData, TException, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, SagaStateMachineInstance
    where TData : class
    where TMessage : class
    where TException : Exception
{
    readonly DestinationAddressProvider<TSaga, TData> _destinationAddressProvider;
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> _messageFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    public FaultedSendActivity(DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> messageFactory)
    {
        _destinationAddressProvider = destinationAddressProvider;
        _messageFactory = messageFactory;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="inspector">The inspector.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("send-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TData, T> context, IBehavior<TSaga, TData> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TData, TException> exceptionContext)
        {
            var destinationAddress = _destinationAddressProvider(exceptionContext);

            var endpoint = await exceptionContext.GetSendEndpointAsync(destinationAddress).ConfigureAwait(false);

            await _messageFactory.UseAsync(exceptionContext, (ctx, s) => endpoint.SendAsync(s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
