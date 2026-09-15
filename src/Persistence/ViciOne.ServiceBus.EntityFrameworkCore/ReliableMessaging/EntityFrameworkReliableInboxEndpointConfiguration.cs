using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkReliableInboxEndpointConfiguration<TBus, TDbContext>(
    IRegistrationContext context,
    int maximumDeliveryAttempts) :
    IConfigureReceiveEndpoint
    where TBus : class, IBus
    where TDbContext : DbContext
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
            MessageRetryConfigurationObserver.Attach(
                configurator,
                CancellationToken.None,
                retry =>
                {
                    // The persisted inbox state decides whether another invocation is due. Handling the original
                    // consumer notification here prevents an intermediate Fault<T> from escaping before that decision.
                    retry.Handle<Exception>();
                    retry.Immediate(_maximumDeliveryAttempts - 1);
                });
        }

        var observer = new OutboxConsumePipeSpecificationObserver<EntityFrameworkReliableInboxScope<TBus, TDbContext>>(
            configurator,
            _context);
        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }
}
