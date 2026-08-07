// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    using DependencyInjection.Registration;


    public class RegistrationConsumerTestHarness<TConsumer> :
        IConsumerTestHarness<TConsumer>
        where TConsumer : class, IConsumer
    {
        readonly ReceivedMessageList _consumed;

        public RegistrationConsumerTestHarness(IConsumerFactoryDecoratorRegistration<TConsumer> registration)
        {
            _consumed = registration.Consumed;
        }

        public IReceivedMessageList Consumed => _consumed;
    }
}
