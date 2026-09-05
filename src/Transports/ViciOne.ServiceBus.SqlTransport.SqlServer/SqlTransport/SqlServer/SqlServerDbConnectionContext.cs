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

/// <summary>
/// Provides a sql server db connection context implementation.
/// </summary>
public class SqlServerDbConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly TaskExecutor _executor;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly SqlServerSqlHostSettings _hostSettings;
    readonly IRetryPolicy _retryPolicy;

    static SqlServerDbConnectionContext()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new UriTypeHandler());
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="supervisor">The supervisor value.</param>
    public SqlServerDbConnectionContext(ISqlHostConfiguration hostConfiguration, ITransportSupervisor<ConnectionContext> supervisor)
        : base(supervisor.Stopped)
    {
        _hostConfiguration = hostConfiguration;

        _hostSettings = hostConfiguration.Settings as SqlServerSqlHostSettings
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host settings were not of the expected type", "Correct the named configuration before starting the host"));

        _retryPolicy = Retry.CreatePolicy(x => x.Immediate(10).Handle<SqlException>(ex => IsTransient(ex)));

        Topology = hostConfiguration.Topology;

        if (_hostSettings.MaintenanceEnabled)
            supervisor.AddConsumeAgent(new MaintenanceAgent(this, hostConfiguration));

        _executor = new TaskExecutor(hostConfiguration.Settings.ConnectionLimit);
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public ISqlBusTopology Topology { get; }

    /// <summary>
    /// Gets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel => _hostSettings.IsolationLevel;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _hostConfiguration.HostAddress;

    /// <summary>
    /// Gets the schema value.
    /// </summary>
    public string? Schema => _hostSettings.Schema;

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new SqlServerClientContext(this, cancellationToken);
    }

    async Task<ISqlTransportConnection> ConnectionContext.CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the query operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="callback">The callback value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T> QueryAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken)
    {
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

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        TransportLogMessages.DisconnectedHost(_hostConfiguration.HostAddress.ToString());
    }

    /// <summary>
    /// Creates connection.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ISqlServerSqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlServerSqlTransportConnection(_hostSettings.GetConnectionString());

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


    class MaintenanceAgent :
        Agent
    {
        readonly SqlServerDbConnectionContext _context;
        readonly ISqlHostConfiguration _hostConfiguration;
        readonly ILogContext? _logContext;

        public MaintenanceAgent(SqlServerDbConnectionContext context, ISqlHostConfiguration hostConfiguration)
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
                    catch (OperationCanceledException)
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
                catch (OperationCanceledException)
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
