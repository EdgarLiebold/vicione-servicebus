using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the faulted request activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class FaultedRequestActivity<TSaga, TException, TRequest, TResponse> :
    RequestActivityImpl<TSaga, TRequest, TResponse>,
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<IBehaviorExceptionContext<TSaga, TException>, TRequest> _messageFactory;
    readonly ServiceAddressExceptionProvider<TSaga, TException> _serviceAddressProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="messageFactory">The message factory.</param>
    public FaultedRequestActivity(IRequest<TSaga, TRequest, TResponse> request,
        ContextMessageFactory<IBehaviorExceptionContext<TSaga, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    public FaultedRequestActivity(IRequest<TSaga, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TSaga, TException> serviceAddressProvider,
        ContextMessageFactory<IBehaviorExceptionContext<TSaga, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        if (context is IBehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            var serviceAddress = _serviceAddressProvider(exceptionContext);

            await _messageFactory.UseAsync(exceptionContext, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TOtherException">The other exception type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T, TOtherException>(IBehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        if (context is IBehaviorExceptionContext<TSaga, T, TException> exceptionContext)
        {
            var serviceAddress = _serviceAddressProvider(exceptionContext);

            await _messageFactory.UseAsync(exceptionContext, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}


/// <summary>Executes the faulted request activity.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse> :
    RequestActivityImpl<TInstance, TRequest, TResponse>,
    IStateMachineActivity<TInstance, TData>
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
    where TException : Exception
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<IBehaviorExceptionContext<TInstance, TData, TException>, TRequest> _messageFactory;
    readonly ServiceAddressExceptionProvider<TInstance, TData, TException> _serviceAddressProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="messageFactory">The message factory.</param>
    public FaultedRequestActivity(IRequest<TInstance, TRequest, TResponse> request,
        ContextMessageFactory<IBehaviorExceptionContext<TInstance, TData, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    public FaultedRequestActivity(IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TData, TException> serviceAddressProvider,
        ContextMessageFactory<IBehaviorExceptionContext<TInstance, TData, TException>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(IBehaviorContext<TInstance, TData> context, IBehavior<TInstance, TData> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TInstance, TData, T> context, IBehavior<TInstance, TData> next)
        where T : Exception
    {
        if (context is IBehaviorExceptionContext<TInstance, TData, TException> exceptionContext)
        {
            var serviceAddress = _serviceAddressProvider(exceptionContext);

            await _messageFactory.UseAsync(exceptionContext, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
