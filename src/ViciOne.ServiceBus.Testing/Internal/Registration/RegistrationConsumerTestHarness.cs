namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Exposes deliveries recorded for a dependency-injection registered consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
internal sealed class RegistrationConsumerTestHarness<TConsumer> :
    IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly ConsumedMessageList _consumed;

    /// <summary>Creates a harness over the consumer registration's recorded deliveries.</summary>
    /// <param name="registration">The consumer test registration.</param>
    public RegistrationConsumerTestHarness(ConsumerContainerTestHarnessRegistration<TConsumer> registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        _consumed = registration.Consumed;
    }

    /// <inheritdoc />
    public IConsumedMessageList Consumed => _consumed;
}
