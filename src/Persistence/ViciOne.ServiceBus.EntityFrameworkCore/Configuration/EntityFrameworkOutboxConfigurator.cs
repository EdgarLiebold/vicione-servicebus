using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an entity framework outbox configurator implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
/// <typeparam name="TDbContext">The t db context type.</typeparam>
public class EntityFrameworkOutboxConfigurator<TBus, TDbContext> :
    IEntityFrameworkOutboxConfigurator
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly IServiceCollection _services;
    Action<IEntityFrameworkBusOutboxConfigurator>? _configureBusOutbox;
    IsolationLevel _isolationLevel;
    ILockStatementProvider? _lockStatementProvider;
    bool _registerInboxCleanupService;
    bool _useBusOutbox;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="services">The service collection owned by the bus configuration.</param>
    internal EntityFrameworkOutboxConfigurator(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _isolationLevel = IsolationLevel.RepeatableRead;
        _registerInboxCleanupService = true;
    }

    /// <summary>
    /// Gets or sets the duplicate detection window value.
    /// </summary>
    public TimeSpan DuplicateDetectionWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }

    /// <summary>
    /// Gets or sets the lock statement provider value.
    /// </summary>
    public ILockStatementProvider LockStatementProvider
    {
        set => _lockStatementProvider = value ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "LockStatementProvider must not be null", "Correct the named configuration before starting the host"));
    }

    /// <summary>
    /// Gets or sets the query delay value.
    /// </summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the query message limit value.
    /// </summary>
    public int QueryMessageLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the query timeout value.
    /// </summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Performs the disable inbox cleanup service operation.
    /// </summary>
    public void DisableInboxCleanupService()
    {
        _registerInboxCleanupService = false;
    }

    /// <summary>
    /// Configures bus outbox for the current pipeline.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public virtual void EnableTransactionalOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null)
    {
        if (_useBusOutbox)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "The Entity Framework bus outbox may only be configured once.", "Correct the named configuration before starting the host"));

        _useBusOutbox = true;
        _configureBusOutbox = configure;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public virtual void Configure(Action<IEntityFrameworkOutboxConfigurator>? configure)
    {
        configure?.Invoke(this);

        ValidateSettings();

        _services.TryAddSingleton(TimeProvider.System);

        IsolationLevel isolationLevel = _isolationLevel;
        ILockStatementProvider lockStatementProvider = _lockStatementProvider!;
        TimeSpan duplicateDetectionWindow = DuplicateDetectionWindow;
        int queryMessageLimit = QueryMessageLimit;
        TimeSpan queryDelay = QueryDelay;
        TimeSpan queryTimeout = QueryTimeout;

        _services.TryAddScoped<IOutboxContextFactory<TDbContext>, EntityFrameworkOutboxContextFactory<TDbContext>>();
        _services.AddOptions<EntityFrameworkOutboxOptions<TDbContext>>().Configure(options =>
        {
            options.IsolationLevel = isolationLevel;
            options.LockStatementProvider = lockStatementProvider;
        })
            .Validate(
                static options => Enum.IsDefined(options.IsolationLevel),
                $"Entity Framework outbox for bus '{typeof(TBus).FullName}': IsolationLevel is not defined. Select a valid isolation level.")
            .Validate(
                static options => options.LockStatementProvider is not null,
                $"Entity Framework outbox for bus '{typeof(TBus).FullName}': LockStatementProvider is not declared. Select exactly one relational provider.")
            .ValidateOnStart();

        if (_registerInboxCleanupService)
        {
            _services.AddHostedService<InboxCleanupService<TDbContext>>();
            _services.AddOptions<InboxCleanupServiceOptions<TDbContext>>().Configure(options =>
            {
                options.DuplicateDetectionWindow = duplicateDetectionWindow;
                options.QueryMessageLimit = queryMessageLimit;
                options.QueryDelay = queryDelay;
                options.QueryTimeout = queryTimeout;
            })
                .Validate(
                    static options => options.DuplicateDetectionWindow > TimeSpan.Zero,
                    $"Inbox cleanup for bus '{typeof(TBus).FullName}': DuplicateDetectionWindow must be greater than zero. Set a positive duration.")
                .Validate(
                    static options => options.QueryMessageLimit > 0,
                    $"Inbox cleanup for bus '{typeof(TBus).FullName}': QueryMessageLimit must be greater than zero. Set a positive bounded batch size.")
                .Validate(
                    static options => options.QueryDelay > TimeSpan.Zero,
                    $"Inbox cleanup for bus '{typeof(TBus).FullName}': QueryDelay must be greater than zero. Set a positive delay.")
                .Validate(
                    static options => options.QueryTimeout > TimeSpan.Zero,
                    $"Inbox cleanup for bus '{typeof(TBus).FullName}': QueryTimeout must be greater than zero. Set a positive timeout.")
                .ValidateOnStart();
        }

        if (_useBusOutbox)
        {
            var busOutboxConfigurator = new EntityFrameworkBusOutboxConfigurator<TBus, TDbContext>(_services, this);
            busOutboxConfigurator.Configure(_configureBusOutbox);
        }
    }

    void ValidateSettings()
    {
        if (_lockStatementProvider == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "A relational provider must be selected explicitly for the Entity Framework outbox.", "Correct the named configuration before starting the host"));
        if (DuplicateDetectionWindow <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "DuplicateDetectionWindow must be greater than zero.", "Correct the named configuration before starting the host"));
        if (QueryDelay <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "QueryDelay must be greater than zero.", "Correct the named configuration before starting the host"));
        if (QueryMessageLimit <= 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "QueryMessageLimit must be greater than zero.", "Correct the named configuration before starting the host"));
        if (QueryTimeout <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "QueryTimeout must be greater than zero.", "Correct the named configuration before starting the host"));
    }
}
