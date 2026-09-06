using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the request activity.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class RequestActivity<TInstance, TRequest, TResponse> :
    RequestActivityImpl<TInstance, TRequest, TResponse>,
    IStateMachineActivity<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<BehaviorContext<TInstance>, TRequest> _messageFactory;
    readonly ServiceAddressProvider<TInstance> _serviceAddressProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="messageFactory">The message factory.</param>
    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ContextMessageFactory<BehaviorContext<TInstance>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance> serviceAddressProvider,
        ContextMessageFactory<BehaviorContext<TInstance>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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


/// <summary>Executes the request activity.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="messageFactory">The message factory.</param>
    public RequestActivity(Request<TInstance, TRequest, TResponse> request,
        ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance, TData> serviceAddressProvider,
        ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TInstance, TData> context, IBehavior<TInstance, TData> next)
    {
        var serviceAddress = _serviceAddressProvider(context);

        await _messageFactory.UseAsync(context, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TData, TException> context, IBehavior<TInstance, TData> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
