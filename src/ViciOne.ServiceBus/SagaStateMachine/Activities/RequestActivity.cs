using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class RequestActivity<TInstance, TRequest, TResponse> :
    RequestActivityImpl<TInstance, TRequest, TResponse>,
    IStateMachineActivity<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    readonly ContextMessageFactory<BehaviorContext<TInstance>, TRequest> _messageFactory;
    readonly ServiceAddressProvider<TInstance> _serviceAddressProvider;

    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ContextMessageFactory<BehaviorContext<TInstance>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance> serviceAddressProvider,
        ContextMessageFactory<BehaviorContext<TInstance>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task ExecuteAsync(BehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public async Task ExecuteAsync<T>(BehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

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

    public RequestActivity(Request<TInstance, TRequest, TResponse> request,
        ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => request.Settings.ServiceAddress ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    public RequestActivity(Request<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance, TData> serviceAddressProvider,
        ContextMessageFactory<BehaviorContext<TInstance, TData>, TRequest> messageFactory)
        : base(request)
    {
        _messageFactory = messageFactory;
        _serviceAddressProvider = context => serviceAddressProvider(context) ?? request.Settings.ServiceAddress
            ?? EndpointConvention.GetDestinationAddress<TRequest>(context);
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task ExecuteAsync(BehaviorContext<TInstance, TData> context, IBehavior<TInstance, TData> next)
    {
        var serviceAddress = _serviceAddressProvider(context);

        await _messageFactory.UseAsync(context, (ctx, m) => SendRequestAsync(ctx, m, serviceAddress)).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TData, TException> context, IBehavior<TInstance, TData> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
