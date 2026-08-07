// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.Configuration
{
    public interface IMessageDataTransformConfiguration<TInput>
        where TInput : class
    {
        void Apply(ITransformConfigurator<TInput> configurator);
    }
}
