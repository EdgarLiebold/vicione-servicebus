namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Defines the contract for message data transform configuration.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    void Apply(ITransformConfigurator<TInput> configurator);
}
