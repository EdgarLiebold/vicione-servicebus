namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Adds one discovered message-data member transformation to an initializer.</summary>
/// <typeparam name="TInput">The message type being transformed.</typeparam>
internal interface IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    /// <summary>Adds the member transformation to the supplied initializer configurator.</summary>
    /// <param name="configurator">The target initializer configurator.</param>
    void Apply(ITransformConfigurator<TInput> configurator);
}
