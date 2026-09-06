using ViciOne.ServiceBus.DependencyInjection.Testing;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Provides a test harness for registration consumer test.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class RegistrationConsumerTestHarness<TConsumer> :
    IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly ReceivedMessageList _consumed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="registration">The registration.</param>
    public RegistrationConsumerTestHarness(ConsumerContainerTestHarnessRegistration<TConsumer> registration)
    {
        _consumed = registration.Consumed;
    }

    /// <summary>Gets the consumed.</summary>
    public IReceivedMessageList Consumed => _consumed;
}
