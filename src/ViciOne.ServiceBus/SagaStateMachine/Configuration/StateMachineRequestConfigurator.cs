using System;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a state machine request configurator implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class StateMachineRequestConfigurator<TInstance, TRequest, TResponse> :
    IRequestConfigurator<TInstance, TRequest, TResponse>,
    RequestSettings<TInstance, TRequest, TResponse>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public StateMachineRequestConfigurator()
    {
        Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public RequestSettings<TInstance, TRequest, TResponse> Settings => this;

    /// <summary>
    /// Gets or sets the service address value.
    /// </summary>
    public Uri ServiceAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the timeout value.
    /// </summary>
    public TimeSpan Timeout { get; set; }
    /// <summary>
    /// Gets or sets the clear request id on faulted value.
    /// </summary>
    public bool ClearRequestIdOnFaulted { get; set; }
    /// <summary>
    /// Gets or sets the time to live value.
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }

    /// <summary>
    /// Gets or sets the completed value.
    /// </summary>
    public Action<IEventCorrelationConfigurator<TInstance, TResponse>> Completed { get; set; } = null!;
    /// <summary>
    /// Gets or sets the faulted value.
    /// </summary>
    public Action<IEventCorrelationConfigurator<TInstance, Fault<TRequest>>> Faulted { get; set; } = null!;
    /// <summary>
    /// Gets or sets the timeout expired value.
    /// </summary>
    public Action<IEventCorrelationConfigurator<TInstance, RequestTimeoutExpired<TRequest>>> TimeoutExpired { get; set; } = null!;
}


/// <summary>
/// Provides a state machine request configurator implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
/// <typeparam name="TResponse2">The t response2 type.</typeparam>
public class StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2> :
    StateMachineRequestConfigurator<TInstance, TRequest, TResponse>,
    IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>,
    RequestSettings<TInstance, TRequest, TResponse, TResponse2>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public StateMachineRequestConfigurator()
    {
        Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public new RequestSettings<TInstance, TRequest, TResponse, TResponse2> Settings => this;

    /// <summary>
    /// Gets or sets the completed2 value.
    /// </summary>
    public Action<IEventCorrelationConfigurator<TInstance, TResponse2>> Completed2 { get; set; } = null!;
}


/// <summary>
/// Provides a state machine request configurator implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
/// <typeparam name="TResponse2">The t response2 type.</typeparam>
/// <typeparam name="TResponse3">The t response3 type.</typeparam>
public class StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3> :
    StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>,
    IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3>,
    RequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TResponse2 : class
    where TResponse3 : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public StateMachineRequestConfigurator()
    {
        Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public new RequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> Settings => this;

    /// <summary>
    /// Gets or sets the completed3 value.
    /// </summary>
    public Action<IEventCorrelationConfigurator<TInstance, TResponse3>> Completed3 { get; set; } = null!;
}
