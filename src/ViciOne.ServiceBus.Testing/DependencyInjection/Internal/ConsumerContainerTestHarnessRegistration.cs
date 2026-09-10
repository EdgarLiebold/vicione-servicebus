using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Owns the observations and factory decorator registered for one consumer type.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
internal sealed class ConsumerContainerTestHarnessRegistration<TConsumer> :
    IConsumerFactoryDecoratorRegistration<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Creates a registration using the owning harness's observation policy.</summary>
    /// <param name="testHarness">The harness that supplies timeouts, cancellation, and retention.</param>
    public ConsumerContainerTestHarnessRegistration(ITestHarness testHarness)
    {
        ArgumentNullException.ThrowIfNull(testHarness);

        Consumed = new ConsumedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)Consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>Gets the consumptions attributed to this consumer type.</summary>
    public ConsumedMessageList Consumed { get; }

    /// <summary>Wraps a consumer factory so its invocations are recorded.</summary>
    /// <param name="consumerFactory">The factory to decorate.</param>
    /// <returns>The recording factory decorator.</returns>
    public IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory)
    {
        ArgumentNullException.ThrowIfNull(consumerFactory);

        return new TestConsumerFactoryDecorator<TConsumer>(consumerFactory, Consumed);
    }
}
