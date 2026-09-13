namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures request state machine missing instance.</summary>
public interface IRequestStateMachineMissingInstanceConfigurator
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The pipe produced by the operation.</returns>
    IPipe<ConsumeContext<TMessage>> Apply<TInstance, TMessage>(IMissingInstanceConfigurator<TInstance, TMessage> configurator)
        where TInstance : ISagaStateMachineInstance
        where TMessage : class;
}
