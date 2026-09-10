using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Coordinates PostgreSQL connections, transactions, notifications, retries, and transport maintenance.</summary>
internal sealed class PostgreSqlDbConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly NotificationAgent _agent;
    readonly NpgsqlDataSource _dataSource;
    readonly TaskExecutor _executor;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly PostgreSqlHostSettings _hostSettings;
    readonly IRetryPolicy _retryPolicy;

    static PostgreSqlDbConnectionContext()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new UriTypeHandler());
    }

    /// <summary>Initializes a PostgreSQL connection context and registers its background agents.</summary>
    /// <param name="hostConfiguration">The SQL host configuration.</param>
    /// <param name="supervisor">The supervisor that owns the notification and maintenance agents.</param>
    public PostgreSqlDbConnectionContext(ISqlHostConfiguration hostConfiguration, ITransportSupervisor<ConnectionContext> supervisor)
        : base(supervisor.Stopped)
    {
        _hostConfiguration = hostConfiguration;

        _hostSettings = hostConfiguration.Settings as PostgreSqlHostSettings
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host settings were not of the expected type", "Correct the named configuration before starting the host"));

        _dataSource = _hostSettings.GetDataSource();

        _retryPolicy = Retry.CreatePolicy(x => x.Immediate(10).Handle<PostgresException>(ex => ex.IsTransient));

        Topology = hostConfiguration.Topology;

        _agent = new NotificationAgent(this, hostConfiguration);
        supervisor.AddConsumeAgent(_agent);

        if (_hostSettings.MaintenanceEnabled)
            supervisor.AddConsumeAgent(new MaintenanceAgent(this, hostConfiguration));

        _executor = new TaskExecutor(hostConfiguration.Settings.ConnectionLimit);
    }

    /// <summary>Gets the configured SQL bus topology.</summary>
    public ISqlBusTopology Topology { get; }

    /// <summary>Gets the transaction isolation level used for client operations.</summary>
    public IsolationLevel IsolationLevel => _hostSettings.IsolationLevel;

    /// <summary>Gets the logical transport host address.</summary>
    public Uri HostAddress => _hostConfiguration.HostAddress;

    /// <summary>Gets the PostgreSQL schema containing the transport infrastructure.</summary>
    public string? Schema => _hostSettings.Schema;

    /// <summary>Creates a PostgreSQL client context with the specified lifetime token.</summary>
    /// <param name="cancellationToken">The token that controls the client context lifetime.</param>
    /// <returns>The new PostgreSQL client context.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new PostgreSqlClientContext(this, cancellationToken);
    }

    async Task<ISqlTransportConnection> ConnectionContext.CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes a callback in a retried PostgreSQL transaction and commits its result.</summary>
    /// <typeparam name="T">The callback result type.</typeparam>
    /// <param name="callback">The operation to execute with the open connection and transaction.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value returned by <paramref name="callback" /> after the transaction commits.</returns>
    public Task<T> QueryAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return _executor.ExecuteAsync(() =>
        {
            return _retryPolicy.RetryAsync(async () =>
            {
                await using var connection = await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

                await using var transaction = await connection.Connection.BeginTransactionAsync(_hostSettings.IsolationLevel, cancellationToken)
                    .ConfigureAwait(false);

                var result = await callback(connection.Connection, transaction).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return result;
            }, false, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>Waits until PostgreSQL notifies the queue or the polling timeout elapses.</summary>
    /// <param name="queueId">The database identifier of the queue being observed.</param>
    /// <param name="timeout">The maximum time to wait before polling again.</param>
    /// <param name="timeProvider">The time source used for the polling delay.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var queueToken = _agent.GetCancellationTokenForQueue(queueId);

        try
        {
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, queueToken);
            await Task.Delay(timeout, timeProvider, linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (queueToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (queueToken.IsCancellationRequested)
                _agent.ResetCancellationTokenForQueue(queueId, queueToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_hostSettings.IsProvidedDataSource == false)
            await _dataSource.DisposeAsync().ConfigureAwait(false);

        TransportLogMessages.DisconnectedHost(_hostConfiguration.HostAddress.ToString());
    }

    async Task<IPostgreSqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new PostgreSqlTransportConnection(_dataSource.CreateConnection());

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }


    sealed class NotificationAgent :
        Agent
    {
        readonly PostgreSqlDbConnectionContext _context;
        readonly ISqlHostConfiguration _hostConfiguration;
        readonly object _listenTokenLock;
        readonly ILogContext? _logContext;
        readonly ConcurrentDictionary<long, CancellationTokenSource> _notificationTokens;
        CancellationTokenSource _listenTokenSource;

        public NotificationAgent(PostgreSqlDbConnectionContext context, ISqlHostConfiguration hostConfiguration)
        {
            _context = context;
            _hostConfiguration = hostConfiguration;
            _logContext = hostConfiguration.LogContext;

            _listenTokenLock = new object();
            _notificationTokens = new ConcurrentDictionary<long, CancellationTokenSource>();
            _listenTokenSource = new CancellationTokenSource();

            var runTask = Task.Run(() => ListenForNotificationsAsync(), Stopping);

            SetReady(runTask);

            SetCompleted(runTask);
        }

        public CancellationToken GetCancellationTokenForQueue(long queueId)
        {
            while (true)
            {
                if (_notificationTokens.TryGetValue(queueId, out var existing))
                {
                    if (!existing.IsCancellationRequested)
                        return existing.Token;

                    var replacement = new CancellationTokenSource();
                    if (_notificationTokens.TryUpdate(queueId, replacement, existing))
                    {
                        existing.Dispose();
                        return replacement.Token;
                    }

                    replacement.Dispose();
                    continue;
                }

                var created = new CancellationTokenSource();
                if (_notificationTokens.TryAdd(queueId, created))
                {
                    PulseListener();
                    return created.Token;
                }

                created.Dispose();
            }
        }

        public void ResetCancellationTokenForQueue(long queueId, CancellationToken observedToken)
        {
            if (!_notificationTokens.TryGetValue(queueId, out var existing)
                || existing.Token != observedToken
                || !existing.IsCancellationRequested)
                return;

            var replacement = new CancellationTokenSource();
            if (_notificationTokens.TryUpdate(queueId, replacement, existing))
                existing.Dispose();
            else
                replacement.Dispose();
        }

        async Task ListenForNotificationsAsync()
        {
            LogContext.SetCurrentIfNull(_logContext);

            while (!Stopping.IsCancellationRequested)
            {
                try
                {
                    await _hostConfiguration.RetryAsync(async () =>
                    {
                        await using var connection = await _context.CreateConnectionAsync(Stopping);

                        var queueIds = new HashSet<long>(_notificationTokens.Keys);
                        connection.Connection.Notification += OnConnectionOnNotification;

                        foreach (var queueId in queueIds)
                        {
                            string channelName = PostgreSqlNotificationChannel.CreateName(_context.Schema, queueId);
                            await connection.Connection.ExecuteScalarAsync<int>(new CommandDefinition(
                                    $"LISTEN \"{channelName}\"", cancellationToken: Stopping))
                                .ConfigureAwait(false);
                        }

                        while (!Stopping.IsCancellationRequested)
                        {
                            try
                            {
                                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(GetListenToken(), Stopping);

                                await connection.Connection.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                            }

                            ResetListenTokenIfCanceled();

                            foreach (var queueId in _notificationTokens.Keys)
                            {
                                if (queueIds.Contains(queueId))
                                    continue;

                                string channelName = PostgreSqlNotificationChannel.CreateName(_context.Schema, queueId);
                                await connection.Connection.ExecuteScalarAsync<int>(new CommandDefinition(
                                        $"LISTEN \"{channelName}\"", cancellationToken: Stopping))
                                    .ConfigureAwait(false);

                                queueIds.Add(queueId);
                            }
                        }
                    }, Stopping, Stopping);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    LogContext.Debug?.Log(exception, "PostgreSQL notification listener faulted");
                }
            }
        }

        void OnConnectionOnNotification(object sender, NpgsqlNotificationEventArgs args)
        {
            LogContext.SetCurrentIfNull(_logContext);

            var index = args.Channel.LastIndexOf('_');
            if (index > 0
                && long.TryParse(args.Channel.Substring(index + 1), out var queueId)
                && _notificationTokens.TryGetValue(queueId, out var source)
                && !source.IsCancellationRequested)
                source.Cancel();
        }

        CancellationToken GetListenToken()
        {
            lock (_listenTokenLock)
                return _listenTokenSource.Token;
        }

        void PulseListener()
        {
            lock (_listenTokenLock)
                _listenTokenSource.Cancel();
        }

        void ResetListenTokenIfCanceled()
        {
            CancellationTokenSource? previous = null;

            lock (_listenTokenLock)
            {
                if (_listenTokenSource.IsCancellationRequested)
                {
                    previous = _listenTokenSource;
                    _listenTokenSource = new CancellationTokenSource();
                }
            }

            previous?.Dispose();
        }
    }


    sealed class MaintenanceAgent :
        Agent
    {
        readonly PostgreSqlDbConnectionContext _context;
        readonly ISqlHostConfiguration _hostConfiguration;
        readonly ILogContext? _logContext;

        public MaintenanceAgent(PostgreSqlDbConnectionContext context, ISqlHostConfiguration hostConfiguration)
        {
            _context = context;
            _hostConfiguration = hostConfiguration;
            _logContext = hostConfiguration.LogContext;

            var runTask = Task.Run(() => PerformMaintenanceAsync(), Stopping);

            SetReady(runTask);

            SetCompleted(runTask);
        }

        async Task PerformMaintenanceAsync()
        {
            LogContext.SetCurrentIfNull(_logContext);

            var processMetricsSql = string.Format(PostgreSqlStatements.DbProcessMetricsSql, _context.Schema);
            var purgeTopologySql = string.Format(PostgreSqlStatements.DbPurgeTopologySql, _context.Schema);
            var removeOrphanedMessagesSql = string.Format(PostgreSqlStatements.DbRemoveOrphanedMessages, _context.Schema);

            var random = new Random();

            var cleanupInterval = AddJitter(_hostConfiguration.Settings.QueueCleanupInterval, random);

            DateTime? lastCleanup = null;

            while (!Stopping.IsCancellationRequested)
            {
                try
                {
                    var maintenanceInterval = AddJitter(_hostConfiguration.Settings.MaintenanceInterval, random);

                    try
                    {
                        await Task.Delay(maintenanceInterval, _context.GetTimeProvider(), Stopping);
                    }
                    catch (OperationCanceledException)
                    {
                        using var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(10), _context.GetTimeProvider());

                        try
                        {
                            await _context.QueryAsync(
                                (connection, transaction) => connection.ExecuteScalarAsync<long?>(new CommandDefinition(processMetricsSql,
                                    new { row_limit = _hostConfiguration.Settings.MaintenanceBatchSize }, transaction,
                                    cancellationToken: timeoutToken.Token)), timeoutToken.Token);

                            if (lastCleanup == null)
                            {
                                await _context.QueryAsync(
                                    (connection, transaction) => connection.ExecuteScalarAsync<long?>(new CommandDefinition(
                                        purgeTopologySql, transaction: transaction, cancellationToken: timeoutToken.Token)), timeoutToken.Token);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (TimeoutException)
                        {
                        }
                    }

                    await _hostConfiguration.RetryAsync(async () =>
                    {
                        await _context.QueryAsync(
                            (connection, transaction) => connection.ExecuteScalarAsync<long?>(new CommandDefinition(processMetricsSql,
                                new { row_limit = _hostConfiguration.Settings.MaintenanceBatchSize }, transaction, cancellationToken: Stopping)), Stopping);

                        var utcNow = _context.GetTimeProvider().GetUtcNow().UtcDateTime;

                        if (lastCleanup == null || lastCleanup < utcNow - cleanupInterval)
                        {
                            await _context.QueryAsync(
                                (connection, transaction) => connection.ExecuteScalarAsync<long?>(new CommandDefinition(
                                    purgeTopologySql, transaction: transaction, cancellationToken: Stopping)), Stopping);

                            lastCleanup = utcNow;
                            cleanupInterval = AddJitter(_hostConfiguration.Settings.QueueCleanupInterval, random);

                            await _context.QueryAsync(
                                (connection, transaction) => connection.ExecuteScalarAsync<long?>(new CommandDefinition(removeOrphanedMessagesSql,
                                    new { row_limit = _hostConfiguration.Settings.MaintenanceBatchSize }, transaction, cancellationToken: Stopping)), Stopping);
                        }
                    }, Stopping, Stopping);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    LogContext.Debug?.Log(exception, "PostgreSQL maintenance faulted");
                }
            }
        }

        static TimeSpan AddJitter(TimeSpan interval, Random random)
        {
            var maximumJitterSeconds = (int)(interval.TotalSeconds / 10);

            return maximumJitterSeconds > 0
                ? interval + TimeSpan.FromSeconds(random.Next(maximumJitterSeconds))
                : interval;
        }
    }
}
