using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a redeliver request state machine specification implementation.
/// </summary>
public class RedeliverRequestStateMachineSpecification :
    IRequestStateMachineMissingInstanceConfigurator
{
    readonly Action<IMissingInstanceRedeliveryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public RedeliverRequestStateMachineSpecification(Action<IMissingInstanceRedeliveryConfigurator> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Apply<TInstance, TMessage>(IMissingInstanceConfigurator<TInstance, TMessage> configurator)
        where TInstance : SagaStateMachineInstance
        where TMessage : class
    {
        return configurator.Redeliver(r =>
        {
            r.OnRedeliveryLimitReached(x => x.Fault());

            _configure?.Invoke(r);
        });
    }
}
