using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a faulted request activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class FaultedRequestActivity<TSaga, TException, TRequest, TResponse> :
    RequestActivityImpl<TSaga, TRequest, TResponse>,
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TRequest> _messageFactory;
    readonly ServiceAddressExceptionProvider<TSaga, TException> _serviceAddressProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedRequestActivity(Request<TSaga, TRequest, TResponse> request,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="serviceAddressProvider">The service address provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedRequestActivity(Request<TSaga, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TSaga, TException> serviceAddressProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
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
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            var serviceAddress = _serviceAddressProvider(exceptionContext);

            await _messageFactory.UseAsync(exceptionContext, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);
        }

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
    public async Task FaultedAsync<T, TOtherException>(BehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, T, TException> exceptionContext)
        {
            var serviceAddress = _serviceAddressProvider(exceptionContext);

            await _messageFactory.UseAsync(exceptionContext, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}


/// <summary>
/// Provides a faulted request activity implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse> :
    RequestActivityImpl<TInstance, TRequest, TResponse>,
    IStateMachineActivity<TInstance, TData>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
    where TException : Exception
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TInstance, TData, TException>, TRequest> _messageFactory;
    readonly ServiceAddressExceptionProvider<TInstance, TData, TException> _serviceAddressProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedRequestActivity(Request<TInstance, TRequest, TResponse> request,
        ContextMessageFactory<BehaviorExceptionContext<TInstance, TData, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="serviceAddressProvider">The service address provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedRequestActivity(Request<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TData, TException> serviceAddressProvider,
        ContextMessageFactory<BehaviorExceptionContext<TInstance, TData, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TInstance, TData> context, IBehavior<TInstance, TData> next)
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
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TInstance, TData, T> context, IBehavior<TInstance, TData> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TInstance, TData, TException> exceptionContext)
        {
            var serviceAddress = _serviceAddressProvider(exceptionContext);

            await _messageFactory.UseAsync(exceptionContext, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
