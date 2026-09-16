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

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Removes expired inbox entries. Only one process owns cleanup for a given physical inbox table at a time.
/// The ownership lock is transaction-bound so process death releases it automatically.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type containing the inbox table.</typeparam>
internal sealed class InboxCleanupService<TDbContext> : BackgroundService
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

    /// <summary>Initializes the cleanup worker for one DbContext type.</summary>
    /// <param name="options">The duplicate-detection window, batch size, poll delay, and query timeout.</param>
    /// <param name="outboxOptions">The transaction isolation and relational lock provider.</param>
    /// <param name="logger">The destination for cleanup failures and removal counts.</param>
    /// <param name="provider">The root provider used to create a scope for each cleanup attempt.</param>
    /// <param name="timeProvider">The source for retention cutoffs and delays.</param>
    public InboxCleanupService(IOptions<InboxCleanupServiceOptions<TDbContext>> options,
        IOptions<EntityFrameworkOutboxOptions<TDbContext>> outboxOptions,
        ILogger<InboxCleanupService<TDbContext>> logger,
        IServiceProvider provider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(outboxOptions);
        _options = options.Value;
        EntityFrameworkOutboxOptions<TDbContext> persistenceOptions = outboxOptions.Value;
        _lockStatementProvider = persistenceOptions.LockStatementProvider
            ?? throw new ArgumentException("A lock-statement provider is required.", nameof(outboxOptions));
        _isolationLevel = Enum.IsDefined(persistenceOptions.IsolationLevel)
            ? persistenceOptions.IsolationLevel
            : throw new ArgumentOutOfRangeException(
                nameof(outboxOptions),
                persistenceOptions.IsolationLevel,
                "The transaction isolation level is undefined.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _retryPolicy = Retry.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
    }

    /// <summary>Polls for expired inbox rows and removes bounded batches while the host is running.</summary>
    /// <param name="stoppingToken">The host-shutdown token.</param>
    /// <returns>A task that completes when host shutdown stops the cleanup loop.</returns>
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

    internal async Task<int> CleanUpInboxStateAsync(CancellationToken cancellationToken)
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
                IQueryable<InboxState> deliveredQuery = dbContext.Set<InboxState>()
                    .AsNoTracking()
                    .Where(x => x.Delivered != null);

                // SQLite stores the UTC-normalized value as canonical text and cannot compare or order
                // DateTimeOffset expressions. Ordering that representation keeps the candidate query bounded.
                IOrderedQueryable<InboxState> orderedQuery = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                    ? deliveredQuery.OrderBy(x => x.Delivered.ToString()).ThenBy(x => x.Id)
                    : deliveredQuery.OrderBy(x => x.Delivered).ThenBy(x => x.Id);

                var candidates = await orderedQuery
                    .Take(_options.QueryMessageLimit)
                    .Select(x => new { x.Id, x.Delivered })
                    .ToListAsync(queryToken.Token)
                    .ConfigureAwait(false);
                long[] expiredIds = candidates
                    .Where(x => x.Delivered!.Value < removeTimestamp)
                    .Select(x => x.Id)
                    .ToArray();
                int count = expiredIds.Length == 0
                    ? 0
                    : await dbContext.Set<InboxState>()
                        .Where(x => expiredIds.Contains(x.Id))
                        .ExecuteDeleteAsync(queryToken.Token)
                        .ConfigureAwait(false);

                await transaction.CommitAsync(queryToken.Token).ConfigureAwait(false);

                if (count > 0)
                    _logger.LogDebug("Removed {Count} expired inbox messages", count);

                return count;
            }
            catch
            {
                await RollbackTransactionAsync(transaction).ConfigureAwait(false);
                throw;
            }
        }
    }

    internal static async Task RollbackTransactionAsync(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The primary cleanup failure remains authoritative; transaction disposal follows immediately.
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
