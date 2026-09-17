using System;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Describes a state-machine request and its completion, fault, timeout, and pending-state behavior.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TRequest">The outgoing request message type.</typeparam>
/// <typeparam name="TResponse">The accepted response message type.</typeparam>
public interface IRequest<TSaga, TRequest, TResponse>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    /// <summary>Gets the request declaration name.</summary>
    string Name { get; }

    /// <summary>Gets the address, timeout, message lifetime, fault-time request-identifier policy, and correlation settings for the request.</summary>
    IRequestSettings<TSaga, TRequest, TResponse> Settings { get; }

    /// <summary>Gets or sets the event raised when the response is received.</summary>
    IEvent<TResponse> Completed { get; set; }

    /// <summary>Gets or sets the event raised when request processing faults.</summary>
    IEvent<Fault<TRequest>> Faulted { get; set; }

    /// <summary>Gets or sets the event raised when the request timeout expires before a response arrives.</summary>
    IEvent<IRequestTimeoutExpired<TRequest>> TimeoutExpired { get; set; }

    /// <summary>Gets or sets the state entered while the response is pending.</summary>
    IState Pending { get; set; }

    /// <summary>Writes the active request identifier to the configured saga property.</summary>
    /// <param name="instance">The saga instance to update.</param>
    /// <param name="requestId">The request identifier, or <see langword="null" /> to clear it.</param>
    void SetRequestId(TSaga instance, Guid? requestId);

    /// <summary>Reads the active request identifier from the configured saga property, or the saga correlation identifier when no property is configured.</summary>
    /// <param name="instance">The saga instance to inspect.</param>
    /// <returns>The active request identifier, if present.</returns>
    Guid? GetRequestId(TSaga instance);

    /// <summary>Creates a request identifier, using a new identifier when a request property is configured and the saga correlation identifier otherwise.</summary>
    /// <param name="instance">The saga instance for which the request is created.</param>
    /// <returns>The identifier to assign to the outgoing request.</returns>
    Guid GenerateRequestId(TSaga instance);

    /// <summary>Applies the configured lifetime and accepted response types to an outgoing request.</summary>
    /// <param name="context">The outgoing request context to configure.</param>
    void SetSendContextHeaders(SendContext<TRequest> context);
}


/// <summary>Extends a state-machine request with a second accepted response type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TRequest">The outgoing request message type.</typeparam>
/// <typeparam name="TResponse">The first accepted response message type.</typeparam>
/// <typeparam name="TResponse2">The second accepted response message type.</typeparam>
public interface IRequest<TSaga, TRequest, TResponse, TResponse2> :
    IRequest<TSaga, TRequest, TResponse>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
{
    /// <summary>Gets the settings for both accepted response types.</summary>
    new IRequestSettings<TSaga, TRequest, TResponse, TResponse2> Settings { get; }

    /// <summary>Gets or sets the event raised when the second response type is received.</summary>
    IEvent<TResponse2> Completed2 { get; set; }
}


/// <summary>Extends a state-machine request with a third accepted response type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TRequest">The outgoing request message type.</typeparam>
/// <typeparam name="TResponse">The first accepted response message type.</typeparam>
/// <typeparam name="TResponse2">The second accepted response message type.</typeparam>
/// <typeparam name="TResponse3">The third accepted response message type.</typeparam>
public interface IRequest<TSaga, TRequest, TResponse, TResponse2, TResponse3> :
    IRequest<TSaga, TRequest, TResponse, TResponse2>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
    where TResponse3 : class
{
    /// <summary>Gets the settings for all three accepted response types.</summary>
    new IRequestSettings<TSaga, TRequest, TResponse, TResponse2, TResponse3> Settings { get; }

    /// <summary>Gets or sets the event raised when the third response type is received.</summary>
    IEvent<TResponse3> Completed3 { get; set; }
}
