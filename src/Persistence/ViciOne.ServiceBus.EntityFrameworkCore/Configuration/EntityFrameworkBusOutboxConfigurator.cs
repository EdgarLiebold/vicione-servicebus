using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers and configures an Entity Framework Core transactional outbox for a bus and DbContext.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <typeparam name="TDbContext">The db context type.</typeparam>
public class EntityFrameworkBusOutboxConfigurator<TBus, TDbContext> :
    IEntityFrameworkBusOutboxConfigurator
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly IServiceCollection _services;
    readonly EntityFrameworkOutboxConfigurator<TBus, TDbContext> _outboxConfigurator;
    bool _isDefault;
    bool _registerOutboxDeliveryService = true;

    /// <summary>Initializes the bus-specific outbox configurator over the shared EF Core outbox settings.</summary>
    /// <param name="services">The service collection owned by the bus configuration.</param>
    /// <param name="outboxConfigurator">The outbox configurator.</param>
    internal EntityFrameworkBusOutboxConfigurator(IServiceCollection services,
        EntityFrameworkOutboxConfigurator<TBus, TDbContext> outboxConfigurator)
    {
        _outboxConfigurator = outboxConfigurator;
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Gets or sets the maximum number of persisted messages sent from one outbox row per delivery pass.</summary>
    public int MessageDeliveryLimit { get; set; } = 100;
    /// <summary>Gets or sets the timeout applied to each individual transport send.</summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>Gets or sets the number of failed delivery attempts after which an outbox row is quarantined.</summary>
    public int MaximumDeliveryAttempts { get; set; } = 10;
    /// <summary>Gets or sets the delay before the first retry of a failed transport send.</summary>
    public TimeSpan InitialDeliveryRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>Gets or sets the upper bound for exponentially increasing delivery retry delays.</summary>
    public TimeSpan MaximumDeliveryRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Prevents registration of the hosted service that drains this outbox.</summary>
    public void DisableDeliveryService()
    {
        _registerOutboxDeliveryService = false;
    }

    /// <summary>Selects this DbContext for untyped scoped send and publish APIs when the bus has multiple EF outboxes.</summary>
    public void UseAsDefault()
    {
        _isDefault = true;
    }

    /// <summary>Applies the callback, validates all settings, and adds the outbox services to dependency injection.</summary>
    /// <param name="configure">An optional callback that customizes outbox delivery.</param>
    public void Configure(Action<IEntityFrameworkBusOutboxConfigurator>? configure)
    {
        configure?.Invoke(this);
        Validate();

        EnsureCompatibleScopedContextOwner();
        EnsureUniqueRegistration();

        TimeSpan queryDelay = _outboxConfigurator.QueryDelay;
        int queryMessageLimit = _outboxConfigurator.QueryMessageLimit;
        TimeSpan queryTimeout = _outboxConfigurator.QueryTimeout;
        int messageDeliveryLimit = MessageDeliveryLimit;
        TimeSpan messageDeliveryTimeout = MessageDeliveryTimeout;
        int maximumDeliveryAttempts = MaximumDeliveryAttempts;
        TimeSpan initialDeliveryRetryDelay = InitialDeliveryRetryDelay;
        TimeSpan maximumDeliveryRetryDelay = MaximumDeliveryRetryDelay;

        _services.TryAddScoped<EntityFrameworkBusOutboxSessionRegistry<TBus>>();
        _services.AddScoped<EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext>>(provider =>
            provider.GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<TBus>>().GetOrCreateTransactional<TDbContext>(provider));
        _services.AddSingleton<IEntityFrameworkScopedBusContextFactory<TBus>>(
            new EntityFrameworkTransactionalScopedBusContextFactory<TBus, TDbContext>(_isDefault));
        _services.ReplaceScoped<IScopedBusContextProvider<TBus>, EntityFrameworkScopedBusContextProvider<TBus>>();
        _services.AddScoped<IEntityFrameworkTransactionalOutbox<TBus, TDbContext>>(provider =>
            provider.GetRequiredService<EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext>>());
        _services.AddScoped<IEntityFrameworkOutboxOperations<TBus, TDbContext>, EntityFrameworkOutboxOperations<TBus, TDbContext>>();

        _services.AddSingleton<IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>,
            BusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>();

        _services.AddOptions<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>()
            .Configure(options =>
            {
                options.QueryDelay = queryDelay;
                options.QueryMessageLimit = queryMessageLimit;
                options.QueryTimeout = queryTimeout;
                options.MessageDeliveryLimit = messageDeliveryLimit;
                options.MessageDeliveryTimeout = messageDeliveryTimeout;
                options.MaximumDeliveryAttempts = maximumDeliveryAttempts;
                options.InitialDeliveryRetryDelay = initialDeliveryRetryDelay;
                options.MaximumDeliveryRetryDelay = maximumDeliveryRetryDelay;
            })
            .Validate(
                static options => options.QueryDelay > TimeSpan.Zero,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': QueryDelay must be greater than zero. Set a positive delay.")
            .Validate(
                static options => options.QueryMessageLimit > 0,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': QueryMessageLimit must be greater than zero. Set a positive bounded batch size.")
            .Validate(
                static options => options.QueryTimeout > TimeSpan.Zero,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': QueryTimeout must be greater than zero. Set a positive timeout.")
            .Validate(
                static options => options.MessageDeliveryLimit > 0,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': MessageDeliveryLimit must be greater than zero. Set a positive bounded delivery size.")
            .Validate(
                static options => options.MessageDeliveryTimeout > TimeSpan.Zero,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': MessageDeliveryTimeout must be greater than zero. Set a positive timeout.")
            .Validate(
                static options => options.MaximumDeliveryAttempts > 0,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': MaximumDeliveryAttempts must be greater than zero. Set a positive attempt count.")
            .Validate(
                static options => options.InitialDeliveryRetryDelay > TimeSpan.Zero,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': InitialDeliveryRetryDelay must be greater than zero. Set a positive delay.")
            .Validate(
                static options => options.MaximumDeliveryRetryDelay >= options.InitialDeliveryRetryDelay,
                $"Outbox delivery for bus '{typeof(TBus).FullName}': MaximumDeliveryRetryDelay must not be less than InitialDeliveryRetryDelay. Raise the maximum or lower the initial delay.")
            .ValidateOnStart();

        if (_registerOutboxDeliveryService)
        {
            _services.AddSingleton<IReliableDeliverySource<TBus>,
                EntityFrameworkTransactionalOutboxSource<TBus, TDbContext>>();
            _services.AddMetrics();
            _services.TryAddSingleton<V5ServiceBusInstrumentation<TBus>>();
            _services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, ReliableMessagingDeliveryService<TBus>>());
        }
    }

    void EnsureCompatibleScopedContextOwner()
    {
        Type serviceType = typeof(IScopedBusContextProvider<TBus>);
        Type defaultProvider = typeof(ScopedBusContextProvider<TBus>);
        Type ownProvider = typeof(EntityFrameworkScopedBusContextProvider<TBus>);

        ServiceDescriptor? conflict = _services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == serviceType
            && descriptor.ImplementationType != defaultProvider
            && descriptor.ImplementationType != ownProvider);

        if (conflict == null)
            return;

        string owner = conflict.ImplementationType?.Name ?? conflict.ServiceType.Name;
        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", $"The Entity Framework bus outbox cannot replace scoped context owner {owner} for {TypeCache<TBus>.ShortName}.", "Correct the named configuration before starting the host"));
    }

    void EnsureUniqueRegistration()
    {
        Type marker = typeof(EntityFrameworkBusOutboxRegistration<TBus, TDbContext>);
        if (_services.Any(x => x.ServiceType == marker))
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", $"The Entity Framework bus outbox for {TypeCache<TBus>.ShortName} and {TypeCache<TDbContext>.ShortName} is already configured.", "Correct the named configuration before starting the host"));

        if (_isDefault && _services.Any(x =>
                x.ServiceType == typeof(IEntityFrameworkScopedBusContextFactory<TBus>)
                && x.ImplementationInstance is IEntityFrameworkScopedBusContextFactory<TBus> { IsDefault: true }))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", $"A default Entity Framework bus outbox is already configured for {TypeCache<TBus>.ShortName}. Exactly one default is allowed.", "Correct the named configuration before starting the host"));
        }

        _services.AddSingleton(new EntityFrameworkBusOutboxRegistration<TBus, TDbContext>());
    }

    void Validate()
    {
        if (MessageDeliveryLimit <= 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", "MessageDeliveryLimit must be greater than zero.", "Correct the named configuration before starting the host"));
        if (MessageDeliveryTimeout <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", "MessageDeliveryTimeout must be greater than zero.", "Correct the named configuration before starting the host"));
        if (MaximumDeliveryAttempts <= 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", "MaximumDeliveryAttempts must be greater than zero.", "Correct the named configuration before starting the host"));
        if (InitialDeliveryRetryDelay <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", "InitialDeliveryRetryDelay must be greater than zero.", "Correct the named configuration before starting the host"));
        if (MaximumDeliveryRetryDelay < InitialDeliveryRetryDelay)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", "MaximumDeliveryRetryDelay must be greater than or equal to InitialDeliveryRetryDelay.", "Correct the named configuration before starting the host"));
    }
}
