using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures EF Core inbox deduplication and optional transactional-outbox delivery for a bus.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type containing inbox and outbox entities.</typeparam>
internal sealed class EntityFrameworkOutboxConfigurator<TBus, TDbContext> :
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

    /// <summary>Initializes default EF Core outbox behavior for the supplied service collection.</summary>
    /// <param name="services">The service collection owned by the bus configuration.</param>
    internal EntityFrameworkOutboxConfigurator(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _isolationLevel = IsolationLevel.RepeatableRead;
        _registerInboxCleanupService = true;
    }

    /// <summary>Gets or sets how long delivered inbox rows remain available for duplicate detection.</summary>
    public TimeSpan DuplicateDetectionWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the isolation level used by inbox and outbox transactions.</summary>
    public IsolationLevel IsolationLevel
    {
        get => _isolationLevel;
        set => _isolationLevel = value;
    }

    /// <summary>Gets or sets the provider-specific SQL used to acquire inbox and outbox locks.</summary>
    public ILockStatementProvider LockStatementProvider
    {
        get => _lockStatementProvider
            ?? throw new InvalidOperationException("A relational lock-statement provider has not been selected.");
        set => _lockStatementProvider = value ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "LockStatementProvider must not be null", "Correct the named configuration before starting the host"));
    }

    /// <summary>Gets or sets the idle delay between inbox-cleanup and outbox-delivery polls.</summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets or sets the maximum number of rows processed by one cleanup or delivery polling cycle.</summary>
    public int QueryMessageLimit { get; set; } = 100;

    /// <summary>Gets or sets the timeout for each database polling operation.</summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Prevents registration of the hosted service that removes expired delivered inbox rows.</summary>
    public void DisableInboxCleanupService()
    {
        _registerInboxCleanupService = false;
    }

    /// <summary>Enables the transactional bus outbox for sends and publishes made outside a receive pipeline.</summary>
    /// <param name="configure">An optional callback that customizes delivery of persisted outbox messages.</param>
    public void EnableTransactionalOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null)
    {
        if (_useBusOutbox)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "The Entity Framework bus outbox may only be configured once.", "Correct the named configuration before starting the host"));

        _useBusOutbox = true;
        _configureBusOutbox = configure;
    }

    /// <summary>Applies the callback, validates the relational settings, and registers the configured services.</summary>
    /// <param name="configure">An optional callback that customizes inbox and outbox persistence.</param>
    public void Configure(Action<IEntityFrameworkOutboxConfigurator>? configure)
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
        if (!Enum.IsDefined(_isolationLevel))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Outbox", "unknown", "IsolationLevel must be a defined value.", "Correct the named configuration before starting the host"));
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
