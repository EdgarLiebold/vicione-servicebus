using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Attempts to add one message-property mapping to an initializer builder.</summary>
internal interface IPropertyInitializerInspector<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention);
}
