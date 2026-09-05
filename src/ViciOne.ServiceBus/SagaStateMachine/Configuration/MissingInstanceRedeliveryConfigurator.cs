using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a missing instance redelivery configurator implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MissingInstanceRedeliveryConfigurator<TSaga, TMessage> :
    ExceptionSpecification,
    IMissingInstanceRedeliveryConfigurator<TSaga, TMessage>,
    ISpecification
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    readonly IMissingInstanceConfigurator<TSaga, TMessage> _configurator;
    IPipe<ConsumeContext<TMessage>> _finalPipe;
    RetryPolicyFactory _policyFactory = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public MissingInstanceRedeliveryConfigurator(IMissingInstanceConfigurator<TSaga, TMessage> configurator)
    {
        _configurator = configurator;

        _finalPipe = configurator.Discard();
    }

    /// <summary>
    /// Sets retry policy.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        _policyFactory = factory;
    }

    /// <summary>
    /// Performs the on redelivery limit reached operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void OnRedeliveryLimitReached(Func<IMissingInstanceConfigurator<TSaga, TMessage>, IPipe<ConsumeContext<TMessage>>> configure)
    {
        _finalPipe = configure(_configurator) ?? _configurator.Discard();
    }

    /// <summary>
    /// Connects retry observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        return new EmptyConnectHandle();
    }

    /// <summary>
    /// Gets or sets the replace message id value.
    /// </summary>
    public bool ReplaceMessageId { get; set; } = true;
    /// <summary>
    /// Gets or sets the use message scheduler value.
    /// </summary>
    public bool ConfigureMessageScheduler { get; set; } = true;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_policyFactory == null)
            yield return this.Failure("RetryPolicy", "must not be null");
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Build()
    {
        var retryPolicy = _policyFactory(Filter);

        var options = ReplaceMessageId ? RedeliveryOptions.ReplaceMessageId : RedeliveryOptions.None;
        if (ConfigureMessageScheduler)
            options |= RedeliveryOptions.ConfigureMessageScheduler;

        return new MissingInstanceRedeliveryPipe<TSaga, TMessage>(retryPolicy, _finalPipe, options);
    }
}
