using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.RetryPolicies;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

/// <summary>
/// Removes expired inbox entries. Only one process owns cleanup for a given physical inbox table at a time.
/// The ownership lock is transaction-bound so process death releases it automatically.
/// </summary>
public sealed class InboxCleanupService<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    readonly IsolationLevel _isolationLevel;
    readonly ILockStatementProvider _lockStatementProvider;
    readonly ILogger<InboxCleanupService<TDbContext>> _logger;
    readonly InboxCleanupServiceOptions<TDbContext> _options;
    readonly IServiceProvider _provider;
    readonly IRetryPolicy _retryPolicy;
    readonly TimeProvider _timeProvider;
    string? _cleanupLockStatement;

    public InboxCleanupService(IOptions<InboxCleanupServiceOptions<TDbContext>> options,
        IOptions<EntityFrameworkOutboxOptions<TDbContext>> outboxOptions,
        ILogger<InboxCleanupService<TDbContext>> logger,
        IServiceProvider provider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(outboxOptions);
        _options = options.Value;
        _isolationLevel = outboxOptions.Value.IsolationLevel;
        _lockStatementProvider = outboxOptions.Value.LockStatementProvider;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _retryPolicy = Retry.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var removed = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (removed == 0)
                    await Task.Delay(_options.QueryDelay, _timeProvider, stoppingToken).ConfigureAwait(false);
                else
                    removed = 0;

                removed = await _retryPolicy.RetryAsync(() => CleanUpInboxStateAsync(stoppingToken), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Inbox cleanup faulted for {DbContext}", typeof(TDbContext).Name);
            }
        }
    }

    async Task<int> CleanUpInboxStateAsync(CancellationToken cancellationToken)
    {
        await using var scope = _provider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        using var queryTimeout = new CancellationTokenSource(_options.QueryTimeout, _timeProvider);
        using var queryToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, queryTimeout.Token);

        _cleanupLockStatement ??= _lockStatementProvider.GetInboxCleanupLockStatement(dbContext);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await EntityFrameworkExecutionStrategy.ExecuteAsync(dbContext, strategy, ExecuteAttemptAsync, queryToken.Token).ConfigureAwait(false);

        async Task<int> ExecuteAttemptAsync()
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(_isolationLevel, queryToken.Token).ConfigureAwait(false);
            try
            {
                if (!await AcquireCleanupLockAsync(dbContext, transaction, _cleanupLockStatement, queryToken.Token).ConfigureAwait(false))
                {
                    await transaction.RollbackAsync(queryToken.Token).ConfigureAwait(false);
                    return 0;
                }

                DateTime removeTimestamp = _timeProvider.GetUtcNow().UtcDateTime - _options.DuplicateDetectionWindow;
                int count = await dbContext.Set<InboxState>()
                    .Where(x => x.Delivered != null && x.Delivered.Value < removeTimestamp)
                    .OrderBy(x => x.Delivered)
                    .Take(_options.QueryMessageLimit)
                    .ExecuteDeleteAsync(queryToken.Token)
                    .ConfigureAwait(false);

                await transaction.CommitAsync(queryToken.Token).ConfigureAwait(false);

                if (count > 0)
                    _logger.LogDebug("Removed {Count} expired inbox messages", count);

                return count;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // The primary cleanup failure remains authoritative; transaction disposal follows immediately.
                }

                throw;
            }
        }
    }

    static async Task<bool> AcquireCleanupLockAsync(TDbContext dbContext, IDbContextTransaction transaction, string statement,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = statement;
        command.Transaction = transaction.GetDbTransaction();

        object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
    }
}
