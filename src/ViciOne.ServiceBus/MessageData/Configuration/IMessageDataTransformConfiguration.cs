namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Defines message data transform configuration.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void Apply(ITransformConfigurator<TInput> configurator);
}
