using System;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides dispatch, timeout, and response-correlation settings for a state-machine request.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TRequest">The outgoing request message type.</typeparam>
/// <typeparam name="TResponse">The accepted response message type.</typeparam>
public interface IRequestSettings<TSaga, TRequest, TResponse>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    /// <summary>Gets the endpoint address that handles the request.</summary>
    Uri ServiceAddress { get; }

    /// <summary>Gets the maximum interval to wait for a response.</summary>
    TimeSpan Timeout { get; }

    /// <summary>Gets whether the active request identifier is cleared when the fault event is raised.</summary>
    bool ClearRequestIdOnFaulted { get; }

    /// <summary>Gets the outgoing request message lifetime, or <see langword="null" /> to leave it unspecified.</summary>
    TimeSpan? TimeToLive { get; }

    /// <summary>Gets the correlation configuration applied to the completed response event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, TResponse>> Completed { get; }

    /// <summary>Gets the correlation configuration applied to the request fault event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, Fault<TRequest>>> Faulted { get; }

    /// <summary>Gets the correlation configuration applied to the request-timeout event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, IRequestTimeoutExpired<TRequest>>> TimeoutExpired { get; }
}


/// <summary>Adds correlation settings for a second accepted response type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TRequest">The outgoing request message type.</typeparam>
/// <typeparam name="TResponse">The first accepted response message type.</typeparam>
/// <typeparam name="TResponse2">The second accepted response message type.</typeparam>
public interface IRequestSettings<TSaga, TRequest, TResponse, TResponse2> :
    IRequestSettings<TSaga, TRequest, TResponse>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
{
    /// <summary>Gets the correlation configuration applied to the second completed response event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, TResponse2>> Completed2 { get; }
}


/// <summary>Adds correlation settings for a third accepted response type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TRequest">The outgoing request message type.</typeparam>
/// <typeparam name="TResponse">The first accepted response message type.</typeparam>
/// <typeparam name="TResponse2">The second accepted response message type.</typeparam>
/// <typeparam name="TResponse3">The third accepted response message type.</typeparam>
public interface IRequestSettings<TSaga, TRequest, TResponse, TResponse2, TResponse3> :
    IRequestSettings<TSaga, TRequest, TResponse, TResponse2>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
    where TResponse3 : class
{
    /// <summary>Gets the correlation configuration applied to the third completed response event.</summary>
    Action<IEventCorrelationConfigurator<TSaga, TResponse3>> Completed3 { get; }
}
