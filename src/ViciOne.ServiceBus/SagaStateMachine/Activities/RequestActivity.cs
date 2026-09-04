using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a request activity implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestActivity<TInstance, TRequest, TResponse> :
    RequestActivityImpl<TInstance, TRequest, TResponse>,
    IStateMachineActivity<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<BehaviorContext<TInstance>, TRequest> _messageFactory;
    readonly ServiceAddressProvider<TInstance> _serviceAddressProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ContextMessageFactory<BehaviorContext<TInstance>, TRequest> messageFactory)
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
    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance> serviceAddressProvider,
        ContextMessageFactory<BehaviorContext<TInstance>, TRequest> messageFactory)
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
    public async Task ExecuteAsync(BehaviorContext<TInstance> context, IBehavior<TInstance> next)
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
    public async Task ExecuteAsync<T>(BehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
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
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
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
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    Task ExecuteAsync(BehaviorContext<TInstance> context)
    {
        var serviceAddress = _serviceAddressProvider(context);

        return _messageFactory.UseAsync(context, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress));
    }
}


/// <summary>
/// Provides a request activity implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestActivity<TInstance, TData, TRequest, TResponse> :
    RequestActivityImpl<TInstance, TRequest, TResponse>,
    IStateMachineActivity<TInstance, TData>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> _messageFactory;
    readonly ServiceAddressProvider<TInstance, TData> _serviceAddressProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public RequestActivity(Request<TInstance, TRequest, TResponse> request,
        ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> messageFactory)
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
    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance, TData> serviceAddressProvider,
        ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> messageFactory)
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
    public async Task ExecuteAsync(BehaviorContext<TInstance, TData> context, IBehavior<TInstance, TData> next)
    {
        var serviceAddress = _serviceAddressProvider(context);

        await _messageFactory.UseAsync(context, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TData, TException> context, IBehavior<TInstance, TData> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
