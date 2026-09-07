using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures missing instance redelivery.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public MissingInstanceRedeliveryConfigurator(IMissingInstanceConfigurator<TSaga, TMessage> configurator)
    {
        _configurator = configurator;

        _finalPipe = configurator.Discard();
    }

    /// <summary>Sets retry policy.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        _policyFactory = factory;
    }

    /// <summary>Handles the notification for redelivery limit reached.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void OnRedeliveryLimitReached(Func<IMissingInstanceConfigurator<TSaga, TMessage>, IPipe<ConsumeContext<TMessage>>> configure)
    {
        _finalPipe = configure(_configurator) ?? _configurator.Discard();
    }

    /// <summary>Connects retry observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        return new EmptyConnectHandle();
    }

    /// <summary>Gets or sets the replace message id.</summary>
    public bool ReplaceMessageId { get; set; } = true;
    /// <summary>Gets or sets the configure message scheduler.</summary>
    public bool ConfigureMessageScheduler { get; set; } = true;

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_policyFactory == null)
            yield return this.Failure("RetryPolicy", "must not be null");
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public IPipe<ConsumeContext<TMessage>> Build()
    {
        var retryPolicy = _policyFactory(Filter);

        var options = ReplaceMessageId ? RedeliveryOptions.ReplaceMessageId : RedeliveryOptions.None;
        if (ConfigureMessageScheduler)
            options |= RedeliveryOptions.ConfigureMessageScheduler;

        return new MissingInstanceRedeliveryPipe<TSaga, TMessage>(retryPolicy, _finalPipe, options);
    }
}
