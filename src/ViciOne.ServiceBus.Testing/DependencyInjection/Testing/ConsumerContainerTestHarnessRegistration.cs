using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

/// <summary>
/// Provides a consumer container test harness registration implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ConsumerContainerTestHarnessRegistration<TConsumer> :
    IConsumerFactoryDecoratorRegistration<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="testHarness">The test harness value.</param>
    public ConsumerContainerTestHarnessRegistration(ITestHarness testHarness)
    {
        Consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)Consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public ReceivedMessageList Consumed { get; }

    /// <summary>
    /// Performs the decorate consumer factory operation.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <returns>The result of the operation.</returns>
    public IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory)
    {
        return new TestConsumerFactoryDecorator<TConsumer>(consumerFactory, Consumed);
    }
}
