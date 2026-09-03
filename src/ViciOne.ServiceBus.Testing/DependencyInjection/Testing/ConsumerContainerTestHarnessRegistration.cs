namespace ViciOne.ServiceBus.DependencyInjection.Testing
{
    using ViciOne.ServiceBus.Testing;
    using ViciOne.ServiceBus.Testing.Implementations;
    using Registration;


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
}
