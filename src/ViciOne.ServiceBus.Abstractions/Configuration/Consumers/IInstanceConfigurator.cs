// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IInstanceConfigurator :
        IConsumeConfigurator
    {
    }


    public interface IInstanceConfigurator<TInstance> :
        IConsumerConfigurator<TInstance>,
        IInstanceConfigurator
        where TInstance : class, IConsumer
    {
    }
}
