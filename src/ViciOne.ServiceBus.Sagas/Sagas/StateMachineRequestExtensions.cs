using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for state machine request.</summary>
public static class StateMachineRequestExtensions
{
    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Request<TInstance, TData, TRequest, TResponse>(this IEventActivityBinder<TInstance, TData> binder,
        IRequest<TInstance, TRequest, TResponse> request, EventMessageFactory<TInstance, TData, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TData, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Request<TInstance, TData, TRequest, TResponse>(this IEventActivityBinder<TInstance, TData> binder,
        IRequest<TInstance, TRequest, TResponse> request, AsyncEventMessageFactory<TInstance, TData, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TData, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Request<TInstance, TData, TRequest, TResponse>(this IEventActivityBinder<TInstance, TData> binder,
        IRequest<TInstance, TRequest, TResponse> request, Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TData, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">A provider for the address used for the request.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Request<TInstance, TData, TRequest, TResponse>(this IEventActivityBinder<TInstance, TData> binder,
        IRequest<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance, TData> serviceAddressProvider,
        EventMessageFactory<TInstance, TData, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TData, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">A provider for the address used for the request.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Request<TInstance, TData, TRequest, TResponse>(this IEventActivityBinder<TInstance, TData> binder,
        IRequest<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance, TData> serviceAddressProvider,
        AsyncEventMessageFactory<TInstance, TData, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TData, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">A provider for the address used for the request.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Request<TInstance, TData, TRequest, TResponse>(this IEventActivityBinder<TInstance, TData> binder,
        IRequest<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance, TData> serviceAddressProvider,
        Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TData, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Request<TInstance, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        EventExceptionMessageFactory<TInstance, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TException, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Request<TInstance, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        AsyncEventExceptionMessageFactory<TInstance, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TException, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Request<TInstance, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TException, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Request<TInstance, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TException> serviceAddressProvider,
        EventExceptionMessageFactory<TInstance, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TException, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Request<TInstance, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TException> serviceAddressProvider,
        AsyncEventExceptionMessageFactory<TInstance, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TException, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Request<TInstance, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TException> serviceAddressProvider,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TException, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Request<TInstance, TData, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TData, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        EventExceptionMessageFactory<TInstance, TData, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse>(request,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Request<TInstance, TData, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TData, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse>(request,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Request<TInstance, TData, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TData, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse>(request,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Request<TInstance, TData, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TData, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TData, TException> serviceAddressProvider,
        EventExceptionMessageFactory<TInstance, TData, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Request<TInstance, TData, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TData, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TData, TException> serviceAddressProvider,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Request<TInstance, TData, TException, TRequest, TResponse>(
        this IExceptionActivityBinder<TInstance, TData, TException> binder, IRequest<TInstance, TRequest, TResponse> request,
        ServiceAddressExceptionProvider<TInstance, TData, TException> serviceAddressProvider,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
        where TResponse : class
        where TException : Exception
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new FaultedRequestActivity<TInstance, TData, TException, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Request<TInstance, TRequest, TResponse>(this IEventActivityBinder<TInstance> binder,
        IRequest<TInstance, TRequest, TResponse> request, EventMessageFactory<TInstance, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Request<TInstance, TRequest, TResponse>(this IEventActivityBinder<TInstance> binder,
        IRequest<TInstance, TRequest, TResponse> request, AsyncEventMessageFactory<TInstance, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Request<TInstance, TRequest, TResponse>(this IEventActivityBinder<TInstance> binder,
        IRequest<TInstance, TRequest, TResponse> request, Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TRequest, TResponse>(request, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Request<TInstance, TRequest, TResponse>(this IEventActivityBinder<TInstance> binder,
        IRequest<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance> serviceAddressProvider,
        EventMessageFactory<TInstance, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity = new RequestActivity<TInstance, TRequest, TResponse>(request, serviceAddressProvider,
            MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Request<TInstance, TRequest, TResponse>(this IEventActivityBinder<TInstance> binder,
        IRequest<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance> serviceAddressProvider,
        AsyncEventMessageFactory<TInstance, TRequest> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity =
            new RequestActivity<TInstance, TRequest, TResponse>(request, serviceAddressProvider, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Send a request to the configured service endpoint, and setup the state machine to accept the response.</summary>
    /// <typeparam name="TInstance">The state instance type.</typeparam>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="request">The configured request to use.</param>
    /// <param name="serviceAddressProvider">The service address provider.</param>
    /// <param name="messageFactory">The request message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Request<TInstance, TRequest, TResponse>(this IEventActivityBinder<TInstance> binder,
        IRequest<TInstance, TRequest, TResponse> request, ServiceAddressProvider<TInstance> serviceAddressProvider,
        Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        ScheduleTokenId.UseTokenId<IRequestTimeoutExpired<TRequest>>(x => x.RequestId);
        var activity =
            new RequestActivity<TInstance, TRequest, TResponse>(request, serviceAddressProvider, MessageFactory<TRequest>.Create(messageFactory));

        return binder.Add(activity);
    }

    /// <summary>Adds an activity that cancels an active configured timeout and clears the stored request ID when completed or <c>ClearRequestIdOnFaulted</c> is enabled.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="request">The request.</param>
    /// <param name="completed">Whether to clear the request ID on completion; when <see langword="false" />, clearing depends on <c>ClearRequestIdOnFaulted</c>.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> CancelRequestTimeout<TInstance, TData, TRequest, TResponse>(
        this IEventActivityBinder<TInstance, TData> binder, IRequest<TInstance, TRequest, TResponse> request, bool completed = true)
        where TInstance : class, ISagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TData : class
    {
        var activity = new CancelRequestTimeoutActivity<TInstance, TData, TRequest, TResponse>(request, completed);

        return binder.Add(activity);
    }
}
