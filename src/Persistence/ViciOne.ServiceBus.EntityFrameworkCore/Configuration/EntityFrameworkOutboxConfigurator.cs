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
    readonly IBusRegistrationConfigurator _configurator;
    Action<IEntityFrameworkBusOutboxConfigurator>? _configureBusOutbox;
    IsolationLevel _isolationLevel;
    ILockStatementProvider? _lockStatementProvider;
    bool _registerInboxCleanupService;
    bool _useBusOutbox;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public EntityFrameworkOutboxConfigurator(IBusRegistrationConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

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
        set => _lockStatementProvider = value ?? throw new ConfigurationException("LockStatementProvider must not be null");
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
    public virtual void UseBusOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null)
    {
        if (_useBusOutbox)
            throw new ConfigurationException("The Entity Framework bus outbox may only be configured once.");

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

        _configurator.Services.TryAddSingleton(TimeProvider.System);

        IsolationLevel isolationLevel = _isolationLevel;
        ILockStatementProvider lockStatementProvider = _lockStatementProvider!;
        TimeSpan duplicateDetectionWindow = DuplicateDetectionWindow;
        int queryMessageLimit = QueryMessageLimit;
        TimeSpan queryDelay = QueryDelay;
        TimeSpan queryTimeout = QueryTimeout;

        _configurator.Services.TryAddScoped<IOutboxContextFactory<TDbContext>, EntityFrameworkOutboxContextFactory<TDbContext>>();
        _configurator.Services.AddOptions<EntityFrameworkOutboxOptions<TDbContext>>().Configure(options =>
        {
            options.IsolationLevel = isolationLevel;
            options.LockStatementProvider = lockStatementProvider;
        });

        if (_registerInboxCleanupService)
        {
            _configurator.Services.AddHostedService<InboxCleanupService<TDbContext>>();
            _configurator.Services.AddOptions<InboxCleanupServiceOptions<TDbContext>>().Configure(options =>
            {
                options.DuplicateDetectionWindow = duplicateDetectionWindow;
                options.QueryMessageLimit = queryMessageLimit;
                options.QueryDelay = queryDelay;
                options.QueryTimeout = queryTimeout;
            });
        }

        if (_useBusOutbox)
        {
            var busOutboxConfigurator = new EntityFrameworkBusOutboxConfigurator<TBus, TDbContext>(_configurator, this);
            busOutboxConfigurator.Configure(_configureBusOutbox);
        }
    }

    void ValidateSettings()
    {
        if (_lockStatementProvider == null)
            throw new ConfigurationException("A relational provider must be selected explicitly for the Entity Framework outbox.");
        if (DuplicateDetectionWindow <= TimeSpan.Zero)
            throw new ConfigurationException("DuplicateDetectionWindow must be greater than zero.");
        if (QueryDelay <= TimeSpan.Zero)
            throw new ConfigurationException("QueryDelay must be greater than zero.");
        if (QueryMessageLimit <= 0)
            throw new ConfigurationException("QueryMessageLimit must be greater than zero.");
        if (QueryTimeout <= TimeSpan.Zero)
            throw new ConfigurationException("QueryTimeout must be greater than zero.");
    }
}
