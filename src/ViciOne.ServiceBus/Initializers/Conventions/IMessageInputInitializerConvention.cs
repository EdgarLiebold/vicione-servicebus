namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Identifies an initializer convention for a specific message contract.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public interface IMessageInputInitializerConvention<in TMessage>
    where TMessage : class
{
}
