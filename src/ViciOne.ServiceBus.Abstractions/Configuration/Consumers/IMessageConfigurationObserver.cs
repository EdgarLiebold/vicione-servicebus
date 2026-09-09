namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about message configuration events.</summary>
public interface IMessageConfigurationObserver
{
    /// <summary>Called when a message pipeline is configured for the first time.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The consume-pipeline configuration containing the message pipeline.</param>
    void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class;
}
