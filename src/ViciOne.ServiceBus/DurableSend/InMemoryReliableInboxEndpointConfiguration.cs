using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class InMemoryReliableInboxEndpointConfiguration<TBus>(
    IRegistrationContext context,
    int maximumDeliveryAttempts) :
    IConfigureReceiveEndpoint
    where TBus : class, IBus
{
    readonly IRegistrationContext _context = context ?? throw new ArgumentNullException(nameof(context));
    readonly int _maximumDeliveryAttempts = maximumDeliveryAttempts > 0
        ? maximumDeliveryAttempts
        : throw new ArgumentOutOfRangeException(nameof(maximumDeliveryAttempts));

    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (_maximumDeliveryAttempts > 1)
        {
            _ = new MessageRetryConfigurationObserver(
                configurator,
                CancellationToken.None,
                retry =>
                {
                    // Consumer fault notifications precede the retained inbox-retry signal. Handling the original
                    // exception here defers Fault<T> publication while the inbox row owns retry and quarantine state.
                    retry.Handle<Exception>();
                    retry.Immediate(_maximumDeliveryAttempts - 1);
                });
        }

        var observer = new OutboxConsumePipeSpecificationObserver<InMemoryReliableInboxScope<TBus>>(
            configurator,
            _context);
        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }
}
