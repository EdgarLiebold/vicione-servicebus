using ViciOne.ServiceBus.DependencyInjection.Testing;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a registration consumer test harness implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class RegistrationConsumerTestHarness<TConsumer> :
    IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly ReceivedMessageList _consumed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    public RegistrationConsumerTestHarness(ConsumerContainerTestHarnessRegistration<TConsumer> registration)
    {
        _consumed = registration.Consumed;
    }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReceivedMessageList Consumed => _consumed;
}
