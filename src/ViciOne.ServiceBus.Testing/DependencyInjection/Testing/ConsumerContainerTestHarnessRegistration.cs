using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

/// <summary>Registers consumer container test harness services.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerContainerTestHarnessRegistration<TConsumer> :
    IConsumerFactoryDecoratorRegistration<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    public ConsumerContainerTestHarnessRegistration(ITestHarness testHarness)
    {
        Consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)Consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>Gets the consumed.</summary>
    public ReceivedMessageList Consumed { get; }

    /// <summary>Decorates consumer factory.</summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <returns>The consumer factory produced by the operation.</returns>
    public IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory)
    {
        return new TestConsumerFactoryDecorator<TConsumer>(consumerFactory, Consumed);
    }
}
