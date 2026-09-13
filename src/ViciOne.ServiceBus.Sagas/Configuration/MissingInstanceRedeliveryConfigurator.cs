using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class MissingInstanceRedeliveryConfigurator<TSaga, TMessage> :
    ExceptionSpecification,
    IMissingInstanceRedeliveryConfigurator<TSaga, TMessage>,
    ISpecification
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    readonly IMissingInstanceConfigurator<TSaga, TMessage> _configurator;
    readonly RetryObservable _observers;
    IPipe<ConsumeContext<TMessage>> _finalPipe;
    RetryPolicyFactory _policyFactory = null!;

    public MissingInstanceRedeliveryConfigurator(IMissingInstanceConfigurator<TSaga, TMessage> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _configurator = configurator;
        _observers = new RetryObservable();
        _finalPipe = configurator.Discard();
    }

    public void SetRetryPolicy(RetryPolicyFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _policyFactory = factory;
    }

    public void OnRedeliveryLimitReached(Func<IMissingInstanceConfigurator<TSaga, TMessage>, IPipe<ConsumeContext<TMessage>>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _finalPipe = configure(_configurator)
            ?? throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Missing instance redelivery",
                    TypeCache<TSaga>.ShortName,
                    "The redelivery-limit callback returned no terminal pipe.",
                    "Return a discard, fault, or executable pipe from the redelivery-limit callback"));
    }

    public ConnectHandle ConnectRetryObserver(IRetryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _observers.Connect(observer);
    }

    /// <summary>Gets or sets whether each redelivery receives a new message identifier.</summary>
    public bool ReplaceMessageId { get; set; } = true;
    /// <summary>Gets or sets whether redelivery uses the configured scheduler instead of transport delay.</summary>
    public bool ConfigureMessageScheduler { get; set; } = true;

    /// <summary>Reports a missing retry policy before the consume pipeline is built.</summary>
    /// <returns>The configuration failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_policyFactory == null)
            yield return this.Failure("RetryPolicy", "must not be null");
    }

    public IPipe<ConsumeContext<TMessage>> Build()
    {
        var retryPolicy = _policyFactory(Filter);

        var options = ReplaceMessageId ? RedeliveryOptions.ReplaceMessageId : RedeliveryOptions.None;
        if (ConfigureMessageScheduler)
            options |= RedeliveryOptions.ConfigureMessageScheduler;

        return new MissingInstanceRedeliveryPipe<TSaga, TMessage>(retryPolicy, _observers, _finalPipe, options);
    }
}
