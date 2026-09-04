using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

public class ConsumerContainerTestHarnessRegistration<TConsumer> :
    IConsumerFactoryDecoratorRegistration<TConsumer>
    where TConsumer : class, IConsumer
{
    public ConsumerContainerTestHarnessRegistration(ITestHarness testHarness)
    {
        Consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)Consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    public ReceivedMessageList Consumed { get; }

    public IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory)
    {
        return new TestConsumerFactoryDecorator<TConsumer>(consumerFactory, Consumed);
    }
}
