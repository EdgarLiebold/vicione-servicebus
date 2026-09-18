using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for redeliver request state machine.</summary>
public class RedeliverRequestStateMachineSpecification :
    IRequestStateMachineMissingInstanceConfigurator
{
    readonly Action<IMissingInstanceRedeliveryConfigurator> _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public RedeliverRequestStateMachineSpecification(Action<IMissingInstanceRedeliveryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configure = configure;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The pipe produced by the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Apply<TInstance, TMessage>(IMissingInstanceConfigurator<TInstance, TMessage> configurator)
        where TInstance : ISagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator.Redeliver(r =>
        {
            r.OnRedeliveryLimitReached(x => x.Fault());

            _configure(r);
        });
    }
}
