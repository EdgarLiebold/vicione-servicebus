namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for request state machine missing instance configurator.
/// </summary>
public interface IRequestStateMachineMissingInstanceConfigurator
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    IPipe<ConsumeContext<TMessage>> Apply<TInstance, TMessage>(IMissingInstanceConfigurator<TInstance, TMessage> configurator)
        where TInstance : SagaStateMachineInstance
        where TMessage : class;
}
