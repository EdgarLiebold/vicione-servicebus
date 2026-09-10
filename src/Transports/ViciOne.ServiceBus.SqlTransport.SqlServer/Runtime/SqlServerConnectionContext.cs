using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Coordinates SQL Server connections, transactions, retries, and transport maintenance.</summary>
internal sealed class SqlServerConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly TaskExecutor _executor;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly SqlServerHostSettings _hostSettings;
    readonly IRetryPolicy _retryPolicy;

    static SqlServerConnectionContext()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new UriTypeHandler());
    }

    /// <summary>Initializes a SQL Server connection context and registers its maintenance agent when enabled.</summary>
    /// <param name="hostConfiguration">The SQL host configuration.</param>
    /// <param name="supervisor">The supervisor that owns the maintenance agent.</param>
    public SqlServerConnectionContext(ISqlHostConfiguration hostConfiguration, ITransportSupervisor<ConnectionContext> supervisor)
        : base(GetStoppedToken(supervisor))
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        _hostConfiguration = hostConfiguration;

        _hostSettings = hostConfiguration.Settings as SqlServerHostSettings
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host settings were not of the expected type", "Correct the named configuration before starting the host"));

        _retryPolicy = Retry.CreatePolicy(x => x.Immediate(10).Handle<SqlException>(ex => IsTransient(ex)));

        Topology = hostConfiguration.Topology;

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

    /// <summary>Gets the SQL Server schema containing the transport infrastructure.</summary>
    public string? Schema => _hostSettings.Schema;

    /// <summary>Creates a SQL Server client context with the specified lifetime token.</summary>
    /// <param name="cancellationToken">The token that controls the client context lifetime.</param>
    /// <returns>The new SQL Server client context.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new SqlServerClientContext(this, cancellationToken);
    }

    async Task<ISqlTransportConnection> ConnectionContext.CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes a callback in a retried SQL Server transaction and commits its result.</summary>
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

    Task ConnectionContext.DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        return DelayUntilMessageReadyAsync(queueId, timeout, timeProvider, cancellationToken);
    }

    internal static Task DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _ = queueId;
        return Task.Delay(timeout, timeProvider, cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        TransportLogMessages.DisconnectedHost(_hostConfiguration.HostAddress.ToString());
        return ValueTask.CompletedTask;
    }

    /// <summary>Creates and opens a SQL Server transport connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The open SQL Server transport connection.</returns>
    public async Task<ISqlServerTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlServerTransportConnection(_hostSettings.GetConnectionString());

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    static bool IsTransient(SqlException exception)
    {
        return IsTransientErrorNumber(exception.Number);
    }

    internal static bool IsTransientErrorNumber(int errorNumber)
    {
        return errorNumber switch
        {
            -2 => true,
            20 => true,
            64 => true,
            233 => true,
            1205 => true,
            10053 => true,
            10054 => true,
            10060 => true,
            10928 => true,
            10929 => true,
            40197 => true,
            40143 => true,
            40501 => true,
            40613 => true,
            _ => false
        };
    }

    static CancellationToken GetStoppedToken(ITransportSupervisor<ConnectionContext> supervisor)
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        return supervisor.Stopped;
    }

    sealed class MaintenanceAgent :
        Agent
    {
        readonly SqlServerConnectionContext _context;
        readonly ISqlHostConfiguration _hostConfiguration;
        readonly ILogContext? _logContext;

        public MaintenanceAgent(SqlServerConnectionContext context, ISqlHostConfiguration hostConfiguration)
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

            var processMetricsSql = $"{_context.Schema}.ProcessMetrics";
            var purgeTopologySql = $"{_context.Schema}.PurgeTopology";
            var removeOrphanedMessagesSql = $"{_context.Schema}.RemoveOrphanedMessages";

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
                    catch (OperationCanceledException) when (Stopping.IsCancellationRequested)
                    {
                        await ExecuteAsync<long>(processMetricsSql, new
                        {
                            rowLimit = _hostConfiguration.Settings.MaintenanceBatchSize,
                        }, CancellationToken.None);

                        if (lastCleanup == null)
                            await ExecuteAsync<long>(purgeTopologySql, new { }, CancellationToken.None);
                    }

                    await _hostConfiguration.RetryAsync(async () =>
                    {
                        await ExecuteAsync<long>(processMetricsSql, new
                        {
                            rowLimit = _hostConfiguration.Settings.MaintenanceBatchSize,
                        }, Stopping);

                        var utcNow = _context.GetTimeProvider().GetUtcNow().UtcDateTime;

                        if (lastCleanup == null || lastCleanup < utcNow - cleanupInterval)
                        {
                            await ExecuteAsync<long>(purgeTopologySql, new { }, CancellationToken.None);

                            lastCleanup = utcNow;
                            cleanupInterval = AddJitter(_hostConfiguration.Settings.QueueCleanupInterval, random);

                            await _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(removeOrphanedMessagesSql,
                                new { RowLimit = _hostConfiguration.Settings.MaintenanceBatchSize }, t,
                                commandType: CommandType.StoredProcedure), Stopping);
                        }
                    }, Stopping, Stopping);
                }
                catch (OperationCanceledException) when (Stopping.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    LogContext.Debug?.Log(exception, "SQL Server Maintenance Faulted");
                }
            }
        }

        Task<T?> ExecuteAsync<T>(string functionName, object values, CancellationToken cancellationToken)
            where T : struct
        {
            return _context.QueryAsync((connection, transaction) => connection
                .ExecuteScalarAsync<T?>(functionName, values, transaction, commandType: CommandType.StoredProcedure), cancellationToken);
        }

        static TimeSpan AddJitter(TimeSpan interval, Random random)
        {
            int jitterSeconds = (int)(interval.TotalSeconds / 10);
            return jitterSeconds > 0
                ? interval + TimeSpan.FromSeconds(random.Next(0, jitterSeconds))
                : interval;
        }
    }
}
