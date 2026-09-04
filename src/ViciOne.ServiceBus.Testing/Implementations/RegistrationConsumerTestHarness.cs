using ViciOne.ServiceBus.DependencyInjection.Testing;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class RegistrationConsumerTestHarness<TConsumer> :
    IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly ReceivedMessageList _consumed;

    public RegistrationConsumerTestHarness(ConsumerContainerTestHarnessRegistration<TConsumer> registration)
    {
        _consumed = registration.Consumed;
    }

    public IReceivedMessageList Consumed => _consumed;
}
