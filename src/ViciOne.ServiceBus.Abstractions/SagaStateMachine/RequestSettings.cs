using System;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// The request settings include the address of the request handler, as well as the timeout to use
/// for requests.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface RequestSettings<TSaga, TRequest, TResponse>
    where TSaga : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    /// <summary>The endpoint address of the service that handles the request.</summary>
    Uri ServiceAddress { get; }

    /// <summary>The timeout period before the request times out.</summary>
    TimeSpan Timeout { get; }

    /// <summary>If true, the requestId is cleared when Faulted is triggered.</summary>
    bool ClearRequestIdOnFaulted { get; }

    /// <summary>If specified, the TimeToLive is set on the outgoing request.</summary>
    TimeSpan? TimeToLive { get; }

    /// <summary>Gets the correlation configuration applied to the completed response event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, TResponse>> Completed { get; }

    /// <summary>Gets the correlation configuration applied to the request fault event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, Fault<TRequest>>> Faulted { get; }

    /// <summary>Gets the correlation configuration applied to the request-timeout event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, RequestTimeoutExpired<TRequest>>> TimeoutExpired { get; }
}


/// <summary>
/// The request settings include the address of the request handler, as well as the timeout to use
/// for requests.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TResponse2">The response2 type.</typeparam>
public interface RequestSettings<TSaga, TRequest, TResponse, TResponse2> :
    RequestSettings<TSaga, TRequest, TResponse>
    where TSaga : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
{
    /// <summary>Gets the correlation configuration applied to the second completed response event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, TResponse2>> Completed2 { get; }
}


/// <summary>
/// The request settings include the address of the request handler, as well as the timeout to use
/// for requests.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TResponse2">The response2 type.</typeparam>
/// <typeparam name="TResponse3">The response3 type.</typeparam>
public interface RequestSettings<TSaga, TRequest, TResponse, TResponse2, TResponse3> :
    RequestSettings<TSaga, TRequest, TResponse, TResponse2>
    where TSaga : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
    where TResponse3 : class
{
    /// <summary>Gets the correlation configuration applied to the third completed response event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, TResponse3>> Completed3 { get; }
}
