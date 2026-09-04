using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

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

    public EntityFrameworkOutboxConfigurator(IBusRegistrationConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

        _isolationLevel = IsolationLevel.RepeatableRead;
        _registerInboxCleanupService = true;
    }

    public TimeSpan DuplicateDetectionWindow { get; set; } = TimeSpan.FromMinutes(30);

    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }

    public ILockStatementProvider LockStatementProvider
    {
        set => _lockStatementProvider = value ?? throw new ConfigurationException("LockStatementProvider must not be null");
    }

    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(10);

    public int QueryMessageLimit { get; set; } = 100;

    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public void DisableInboxCleanupService()
    {
        _registerInboxCleanupService = false;
    }

    public virtual void UseBusOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null)
    {
        if (_useBusOutbox)
            throw new ConfigurationException("The Entity Framework bus outbox may only be configured once.");

        _useBusOutbox = true;
        _configureBusOutbox = configure;
    }

    public virtual void Configure(Action<IEntityFrameworkOutboxConfigurator>? configure)
    {
        configure?.Invoke(this);

        ValidateSettings();

        _configurator.TryAddSingleton(TimeProvider.System);

        IsolationLevel isolationLevel = _isolationLevel;
        ILockStatementProvider lockStatementProvider = _lockStatementProvider!;
        TimeSpan duplicateDetectionWindow = DuplicateDetectionWindow;
        int queryMessageLimit = QueryMessageLimit;
        TimeSpan queryDelay = QueryDelay;
        TimeSpan queryTimeout = QueryTimeout;

        _configurator.TryAddScoped<IOutboxContextFactory<TDbContext>, EntityFrameworkOutboxContextFactory<TDbContext>>();
        _configurator.AddOptions<EntityFrameworkOutboxOptions<TDbContext>>().Configure(options =>
        {
            options.IsolationLevel = isolationLevel;
            options.LockStatementProvider = lockStatementProvider;
        });

        if (_registerInboxCleanupService)
        {
            _configurator.AddHostedService<InboxCleanupService<TDbContext>>();
            _configurator.AddOptions<InboxCleanupServiceOptions<TDbContext>>().Configure(options =>
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
