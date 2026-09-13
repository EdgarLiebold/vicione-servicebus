using System;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures request.</summary>
public interface IRequestConfigurator
{
    /// <summary>Sets the service address of the request handler.</summary>
    Uri ServiceAddress { set; }

    /// <summary>Sets the request timeout.</summary>
    TimeSpan Timeout { set; }

    /// <summary>
    /// Set the time to live of the request message sent by the saga. If not specified, and the timeout
    /// is > TimeSpan.Zero, the <see cref="Timeout" /> value is used.
    /// </summary>
    TimeSpan? TimeToLive { set; }

    /// <summary>By default, the RequestId is not cleared when the request is Faulted. Set to true to clear the requestId.</summary>
    bool ClearRequestIdOnFaulted { set; }
}


/// <summary>Configures request.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestConfigurator<TInstance, TRequest, TResponse> :
    IRequestConfigurator
    where TInstance : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    /// <summary>Sets the correlation configuration applied to the completed response event.</summary>
    Action<IEventCorrelationConfigurator<TInstance, TResponse>> Completed { set; }

    /// <summary>Sets the correlation configuration applied to the request fault event.</summary>
    Action<IEventCorrelationConfigurator<TInstance, Fault<TRequest>>> Faulted { set; }

    /// <summary>Sets the correlation configuration applied to the request-timeout event.</summary>
    Action<IEventCorrelationConfigurator<TInstance, IRequestTimeoutExpired<TRequest>>> TimeoutExpired { set; }
}


/// <summary>Configures request.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TResponse2">The response2 type.</typeparam>
public interface IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2> :
    IRequestConfigurator<TInstance, TRequest, TResponse>
    where TInstance : class, ISagaStateMachineInstance
    where TResponse : class
    where TResponse2 : class
    where TRequest : class
{
    /// <summary>Sets the correlation configuration applied to the second completed response event.</summary>
    Action<IEventCorrelationConfigurator<TInstance, TResponse2>> Completed2 { set; }
}


/// <summary>Configures request.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TResponse2">The response2 type.</typeparam>
/// <typeparam name="TResponse3">The response3 type.</typeparam>
public interface IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3> :
    IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>
    where TInstance : class, ISagaStateMachineInstance
    where TResponse : class
    where TResponse2 : class
    where TResponse3 : class
    where TRequest : class
{
    /// <summary>Sets the correlation configuration applied to the third completed response event.</summary>
    Action<IEventCorrelationConfigurator<TInstance, TResponse3>> Completed3 { set; }
}
