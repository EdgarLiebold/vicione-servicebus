// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.Conventions
{
    public interface IMessageInputInitializerConvention<in TMessage>
        where TMessage : class
    {
    }


    public interface IMessageInitializerConvention
    {
    }
}
