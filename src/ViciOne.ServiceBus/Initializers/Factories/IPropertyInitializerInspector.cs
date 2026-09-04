using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Initializers.Factories;

public interface IPropertyInitializerInspector<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention);
}
