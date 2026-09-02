namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Configuration;
using Dapper;
using Logging;
using ViciOne.ServiceBus.Middleware;
using Microsoft.Data.SqlClient;
using RetryPolicies;
using Transports;
using Util;


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

    public SqlServerDbConnectionContext(ISqlHostConfiguration hostConfiguration, ITransportSupervisor<ConnectionContext> supervisor)
        : base(supervisor.Stopped)
    {
        _hostConfiguration = hostConfiguration;

        _hostSettings = hostConfiguration.Settings as SqlServerSqlHostSettings
            ?? throw new ConfigurationException("The host settings were not of the expected type");

        _retryPolicy = Retry.CreatePolicy(x => x.Immediate(10).Handle<SqlException>(ex => IsTransient(ex)));

        Topology = hostConfiguration.Topology;

        if (_hostSettings.MaintenanceEnabled)
            supervisor.AddConsumeAgent(new MaintenanceAgent(this, hostConfiguration));

        _executor = new TaskExecutor(hostConfiguration.Settings.ConnectionLimit);
    }

    public ISqlBusTopology Topology { get; }

    public IsolationLevel IsolationLevel => _hostSettings.IsolationLevel;

    public Uri HostAddress => _hostConfiguration.HostAddress;

    public string? Schema => _hostSettings.Schema;

    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new SqlServerClientContext(this, cancellationToken);
    }

    async Task<ISqlTransportConnection> ConnectionContext.CreateConnection(CancellationToken cancellationToken)
    {
        return await CreateConnection(cancellationToken).ConfigureAwait(false);
    }

    public Task<T> Query<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken)
    {
        return _executor.ExecuteAsync(() =>
        {
            return _retryPolicy.Retry(async () =>
            {
                await using var connection = await CreateConnection(cancellationToken).ConfigureAwait(false);

                await using var transaction = await connection.Connection.BeginTransactionAsync(_hostSettings.IsolationLevel, cancellationToken)
                    .ConfigureAwait(false);

                var result = await callback(connection.Connection, transaction).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return result;
            }, false, cancellationToken);
        }, cancellationToken);
    }

    public Task DelayUntilMessageReady(long queueId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return DelayUntilMessageReady(queueId, timeout, TimeProvider.System, cancellationToken);
    }

    internal static Task DelayUntilMessageReady(
        long queueId,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _ = queueId;
        return Task.Delay(timeout, timeProvider, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        TransportLogMessages.DisconnectedHost(_hostConfiguration.HostAddress.ToString());
    }

    public async Task<ISqlServerSqlTransportConnection> CreateConnection(CancellationToken cancellationToken)
    {
        var connection = new SqlServerSqlTransportConnection(_hostSettings.GetConnectionString());

        await connection.Open(cancellationToken).ConfigureAwait(false);

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

            var runTask = Task.Run(() => PerformMaintenance(), Stopping);

            SetReady(runTask);

            SetCompleted(runTask);
        }

        async Task PerformMaintenance()
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
                        await Task.Delay(maintenanceInterval, Stopping);
                    }
                    catch (OperationCanceledException)
                    {
                        await Execute<long>(processMetricsSql, new
                        {
                            rowLimit = _hostConfiguration.Settings.MaintenanceBatchSize,
                        }, CancellationToken.None);

                        if (lastCleanup == null)
                            await Execute<long>(purgeTopologySql, new { }, CancellationToken.None);
                    }

                    await _hostConfiguration.Retry(async () =>
                    {
                        await Execute<long>(processMetricsSql, new
                        {
                            rowLimit = _hostConfiguration.Settings.MaintenanceBatchSize,
                        }, Stopping);

                        if (lastCleanup == null || lastCleanup < DateTime.UtcNow - cleanupInterval)
                        {
                            await Execute<long>(purgeTopologySql, new { }, CancellationToken.None);

                            lastCleanup = DateTime.UtcNow;
                            cleanupInterval = AddJitter(_hostConfiguration.Settings.QueueCleanupInterval, random);

                            await _context.Query((x, t) => x.ExecuteScalarAsync<long?>(removeOrphanedMessagesSql,
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

        Task<T?> Execute<T>(string functionName, object values, CancellationToken cancellationToken)
            where T : struct
        {
            return _context.Query((connection, transaction) => connection
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
