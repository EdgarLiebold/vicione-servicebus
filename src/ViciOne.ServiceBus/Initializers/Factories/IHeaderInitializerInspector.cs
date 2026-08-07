// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.Factories
{
    using Conventions;


    public interface IHeaderInitializerInspector<in TMessage, in TInput>
        where TMessage : class
        where TInput : class
    {
        bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention);
    }
}
