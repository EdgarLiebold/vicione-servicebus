using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Providers.Persistence;


namespace ViciOne.ServiceBus.Configuration;

internal sealed class ReliableMessagingConfigurator<TBus> :
    IReliableMessagingConfigurator<TBus>,
    IReliableMessagingProviderConfigurator
    where TBus : class, IBus
{
    bool _deliveryConfigured;
    bool _retentionConfigured;
    bool _schedulerAdapterConfigured;
    bool _storeLimitsConfigured;
    readonly ReliableSchedulerSelection<TBus> _schedulerSelection;

    public ReliableMessagingConfigurator(
        IBusRegistrationConfigurator registrationConfigurator,
        ReliableSchedulerSelection<TBus> schedulerSelection)
    {
        RegistrationConfigurator = registrationConfigurator
            ?? throw new ArgumentNullException(nameof(registrationConfigurator));
        _schedulerSelection = schedulerSelection ?? throw new ArgumentNullException(nameof(schedulerSelection));
    }

    public IServiceCollection Services => RegistrationConfigurator.Services;

    public IBusRegistrationConfigurator RegistrationConfigurator { get; }

    public Type BusType => typeof(TBus);

    public void Store(ReliableStoreLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (_storeLimitsConfigured)
            throw Duplicate("store limits");
        _storeLimitsConfigured = true;
        Services.Configure<ReliableMessagingOptions<TBus>>(options =>
        {
            options.MaximumStoredCount = limits.MaximumStoredCount;
            options.MaximumStoredBytes = limits.MaximumStoredBytes;
            options.StoreLimitsConfigured = true;
        });
    }

    public void Delivery(Action<IReliableDeliveryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (_deliveryConfigured)
            throw Duplicate("delivery policy");
        _deliveryConfigured = true;
        var delivery = new ReliableDeliveryConfigurator();
        configure(delivery);
        Services.Configure<ReliableMessagingOptions<TBus>>(options =>
        {
            options.MaximumConcurrentDeliveries = delivery.MaximumConcurrentDeliveries;
            options.MaximumDeliveryAttempts = delivery.MaximumAttempts;
            options.InitialRetryDelay = delivery.InitialRetryDelay;
            options.MaximumRetryDelay = delivery.MaximumRetryDelay;
            options.RetryJitterFraction = delivery.RetryJitterFraction;
            options.LeaseDuration = delivery.LeaseDuration;
            options.ConsumerCompletionTimeout = delivery.ConsumerCompletionTimeout;
            options.PollInterval = delivery.PollInterval;
            options.TelemetrySnapshotInterval = delivery.TelemetrySnapshotInterval;
            options.HealthDegradedAfter = delivery.HealthDegradedAfter;
            options.DeliveryConfigured = true;
        });
    }

    public void Retention(TimeSpan duration)
    {
        if (_retentionConfigured)
            throw Duplicate("retention policy");
        _retentionConfigured = true;
        Services.Configure<ReliableMessagingOptions<TBus>>(options =>
        {
            options.Retention = duration;
            options.RetentionConfigured = true;
        });
    }

    public void AddMessageContract<TMessage>(string name, int majorVersion = 1)
        where TMessage : class
        => BusFeatureConfigurationExtensions.RegisterContracts<TBus>(
            Services,
            catalog => catalog.Register<TMessage>(name, majorVersion));

    public void AddMessageContract<TMessage>()
        where TMessage : class
        => BusFeatureConfigurationExtensions.RegisterContracts<TBus>(Services, catalog => catalog.Register<TMessage>());

    public void UseStore(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        EnsureSingleOwner<IOutboxStore<TBus>>("persistence store");
        EnsureSingleOwner<IInboxStore<TBus>>("inbox store");
        EnsureSingleOwner<IScheduleStore<TBus>>("schedule store");
        Type outboxType = typeof(IOutboxStore<TBus>);
        Type inboxType = typeof(IInboxStore<TBus>);
        Type scheduleType = typeof(IScheduleStore<TBus>);
        if (!outboxType.IsAssignableFrom(implementationType)
            || !inboxType.IsAssignableFrom(implementationType)
            || !scheduleType.IsAssignableFrom(implementationType))
        {
            throw new ArgumentException(
                $"'{implementationType}' must implement '{outboxType}', '{inboxType}' and '{scheduleType}'.",
                nameof(implementationType));
        }

        Services.AddSingleton(implementationType);
        Services.AddSingleton(outboxType, provider => provider.GetRequiredService(implementationType));
        Services.AddSingleton(inboxType, provider => provider.GetRequiredService(implementationType));
        Services.AddSingleton(scheduleType, provider => provider.GetRequiredService(implementationType));
    }

    public void UseDispatcher(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        EnsureSingleOwner<IDurableSendDispatcher<TBus>>("transport dispatcher");
        Type serviceType = typeof(IDurableSendDispatcher<TBus>);
        if (!serviceType.IsAssignableFrom(implementationType))
            throw new ArgumentException($"'{implementationType}' does not implement '{serviceType}'.", nameof(implementationType));

        Services.AddSingleton(serviceType, implementationType);
    }

    public void UseTransportSchedulerAdapter()
    {
        EnsureSingleSchedulerAdapter();
        _schedulerSelection.SelectTransport();
        ReliableSchedulerRegistration.ReplaceWithTransport<TBus>(Services);
    }

    public void UseEndpointSchedulerAdapter(Uri endpointAddress)
    {
        ArgumentNullException.ThrowIfNull(endpointAddress);
        if (!endpointAddress.IsAbsoluteUri)
            throw new ArgumentException("The scheduler endpoint address must be absolute.", nameof(endpointAddress));

        EnsureSingleSchedulerAdapter();
        _schedulerSelection.SelectEndpoint(endpointAddress);
        ReliableSchedulerRegistration.ReplaceWithEndpoint<TBus>(Services, endpointAddress);
    }

    void EnsureSingleSchedulerAdapter()
    {
        if (_schedulerAdapterConfigured)
            throw Duplicate("scheduler adapter");
        _schedulerAdapterConfigured = true;
    }

    private void EnsureSingleOwner<TService>(string component)
    {
        if (Services.Any(static descriptor => descriptor.ServiceType == typeof(TService)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"Durable Sender for bus '{typeof(TBus)}' already has a {component}. Exactly one owner is allowed.", "Correct the named configuration before starting the host"));
        }
    }

    private static ConfigurationException Duplicate(string component) => new(
        global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
            "Reliable messaging",
            "unknown",
            $"The {component} was configured more than once for bus '{typeof(TBus)}'.",
            "Configure each reliable-messaging component exactly once"));
}

internal sealed class ReliableMessagingRegistration<TBus>
    where TBus : class, IBus;

internal enum ReliableSchedulerAdapterKind
{
    /// <summary>Indicates store.</summary>
    Store,
    /// <summary>Indicates transport.</summary>
    Transport,
    /// <summary>Indicates endpoint.</summary>
    Endpoint,
}

internal sealed class ReliableSchedulerSelection<TBus>
    where TBus : class, IBus
{
    public ReliableSchedulerAdapterKind Kind { get; private set; } = ReliableSchedulerAdapterKind.Store;

    public Uri? EndpointAddress { get; private set; }

    public void SelectTransport()
    {
        Kind = ReliableSchedulerAdapterKind.Transport;
        EndpointAddress = null;
    }

    public void SelectEndpoint(Uri endpointAddress)
    {
        EndpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
        Kind = ReliableSchedulerAdapterKind.Endpoint;
    }
}
