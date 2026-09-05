using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Transactions;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an entity framework bus outbox configurator implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
/// <typeparam name="TDbContext">The t db context type.</typeparam>
public class EntityFrameworkBusOutboxConfigurator<TBus, TDbContext> :
    IEntityFrameworkBusOutboxConfigurator
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly EntityFrameworkOutboxConfigurator<TBus, TDbContext> _outboxConfigurator;
    bool _isDefault;
    bool _registerOutboxDeliveryService = true;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="outboxConfigurator">The outbox configurator value.</param>
    public EntityFrameworkBusOutboxConfigurator(IBusRegistrationConfigurator configurator,
        EntityFrameworkOutboxConfigurator<TBus, TDbContext> outboxConfigurator)
    {
        _outboxConfigurator = outboxConfigurator;
        _configurator = configurator;
    }

    /// <summary>
    /// Gets or sets the message delivery limit value.
    /// </summary>
    public int MessageDeliveryLimit { get; set; } = 100;
    /// <summary>
    /// Gets or sets the message delivery timeout value.
    /// </summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>
    /// Gets or sets the maximum delivery attempts value.
    /// </summary>
    public int MaximumDeliveryAttempts { get; set; } = 10;
    /// <summary>
    /// Gets or sets the initial delivery retry delay value.
    /// </summary>
    public TimeSpan InitialDeliveryRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>
    /// Gets or sets the maximum delivery retry delay value.
    /// </summary>
    public TimeSpan MaximumDeliveryRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Performs the disable delivery service operation.
    /// </summary>
    public void DisableDeliveryService()
    {
        _registerOutboxDeliveryService = false;
    }

    /// <summary>
    /// Configures as default for the current pipeline.
    /// </summary>
    public void UseAsDefault()
    {
        _isDefault = true;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

        _configurator.Services.TryAddScoped<EntityFrameworkBusOutboxSessionRegistry<TBus>>();
        _configurator.Services.AddScoped<EntityFrameworkScopedBusContext<TBus, TDbContext>>(provider =>
            provider.GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<TBus>>().GetOrCreate<TDbContext>(provider));
        _configurator.Services.AddSingleton<IEntityFrameworkScopedBusContextFactory<TBus>>(
            new EntityFrameworkScopedBusContextFactory<TBus, TDbContext>(_isDefault));
        _configurator.Services.ReplaceScoped<IScopedBusContextProvider<TBus>, EntityFrameworkScopedBusContextProvider<TBus>>();
        _configurator.Services.AddScoped<IEntityFrameworkTransactionalOutbox<TBus, TDbContext>>(provider =>
            provider.GetRequiredService<EntityFrameworkScopedBusContext<TBus, TDbContext>>());
        _configurator.Services.AddScoped<IEntityFrameworkOutboxOperations<TBus, TDbContext>, EntityFrameworkOutboxOperations<TBus, TDbContext>>();

        _configurator.Services.AddSingleton<IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>,
            BusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>();

        _configurator.Services.AddOptions<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>()
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
            _configurator.Services.AddHostedService<BusOutboxDeliveryService<TBus, TDbContext>>();
    }

    void EnsureCompatibleScopedContextOwner()
    {
        Type serviceType = typeof(IScopedBusContextProvider<TBus>);
        Type defaultProvider = typeof(ScopedBusContextProvider<TBus>);
        Type ownProvider = typeof(EntityFrameworkScopedBusContextProvider<TBus>);

        ServiceDescriptor? conflict = _configurator.Services.FirstOrDefault(descriptor =>
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
        if (_configurator.Services.Any(x => x.ServiceType == marker))
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", $"The Entity Framework bus outbox for {TypeCache<TBus>.ShortName} and {TypeCache<TDbContext>.ShortName} is already configured.", "Correct the named configuration before starting the host"));

        if (_isDefault && _configurator.Services.Any(x =>
                x.ServiceType == typeof(IEntityFrameworkScopedBusContextFactory<TBus>)
                && x.ImplementationInstance is IEntityFrameworkScopedBusContextFactory<TBus> { IsDefault: true }))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Bus Outbox", "unknown", $"A default Entity Framework bus outbox is already configured for {TypeCache<TBus>.ShortName}. Exactly one default is allowed.", "Correct the named configuration before starting the host"));
        }

        _configurator.Services.AddSingleton(new EntityFrameworkBusOutboxRegistration<TBus, TDbContext>());
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
