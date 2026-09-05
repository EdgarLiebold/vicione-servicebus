#nullable enable

using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Strict built-in commit-durability preflight. It intentionally rejects ambiguous or unknown provider modes rather
/// than allowing the provider-level durable admission boundary to overstate restart safety.
/// </summary>
internal sealed class EntityFrameworkDurableSendCommitDurabilityValidator<TBus>
    : IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>
    where TBus : class, IBus
{
    public async Task ValidateAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        string providerName;
        try
        {
            providerName = dbContext.Database.ProviderName
                ?? throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "The EF Core provider name is unavailable; Durable Sender commit durability cannot be verified.", "Correct the named configuration before starting the host"));
        }
        catch (InvalidOperationException exception)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "The EF Core provider name is unavailable; Durable Sender commit durability cannot be verified.", "Correct the named configuration before starting the host"),
                exception);
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            switch (providerName)
            {
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    await ValidateSqlServerAsync(connection, cancellationToken).ConfigureAwait(false);
                    break;

                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    await ValidatePostgreSqlAsync(connection, cancellationToken).ConfigureAwait(false);
                    break;

                case "Microsoft.EntityFrameworkCore.Sqlite":
                    await ValidateSqliteAsync(connection, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new ConfigurationException(
                        global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"EF provider '{providerName}' has no built-in Durable Sender synchronous-commit validator. "
                        + $"Register a provider-certified {nameof(IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>)} before using the durable store.", "Correct the named configuration before starting the host"));
            }
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    static async Task ValidateSqlServerAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        string? delayedDurability = await ReadScalarTextAsync(
            connection,
            "SELECT delayed_durability_desc FROM sys.databases WHERE database_id = DB_ID()",
            cancellationToken).ConfigureAwait(false);

        EnsureSqlServerDurability(delayedDurability);
    }

    internal static void EnsureSqlServerDurability(string? delayedDurability)
    {
        bool fullyDurableByDefault = string.Equals(delayedDurability, "DISABLED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(delayedDurability, "ALLOWED", StringComparison.OrdinalIgnoreCase);
        if (!fullyDurableByDefault)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "Durable Sender requires SQL Server commits to remain fully durable. "
                + "Database DELAYED_DURABILITY may be DISABLED or ALLOWED, but must not be FORCED. "
                + $"Actual mode is '{delayedDurability ?? "<unknown>"}'.", "Correct the named configuration before starting the host"));
        }
    }

    static async Task ValidatePostgreSqlAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        string? synchronousCommit = await ReadScalarTextAsync(connection, "SHOW synchronous_commit", cancellationToken)
            .ConfigureAwait(false);
        string? fsync = await ReadScalarTextAsync(connection, "SHOW fsync", cancellationToken).ConfigureAwait(false);

        EnsurePostgreSqlDurability(synchronousCommit, fsync);
    }

    internal static void EnsurePostgreSqlDurability(string? synchronousCommit, string? fsync)
    {
        bool localWalFlush = string.Equals(synchronousCommit, "on", StringComparison.OrdinalIgnoreCase)
            || string.Equals(synchronousCommit, "local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(synchronousCommit, "remote_write", StringComparison.OrdinalIgnoreCase)
            || string.Equals(synchronousCommit, "remote_apply", StringComparison.OrdinalIgnoreCase);
        if (!localWalFlush || !string.Equals(fsync, "on", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "Durable Sender requires PostgreSQL fsync=on and a synchronous_commit mode that waits for local WAL flush. "
                + "synchronous_commit=off is not restart-safe for a returned admission. "
                + $"Actual: synchronous_commit='{synchronousCommit ?? "<unknown>"}', fsync='{fsync ?? "<unknown>"}'.", "Correct the named configuration before starting the host"));
        }
    }

    static async Task ValidateSqliteAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        object? synchronousValue = await ReadScalarAsync(connection, "PRAGMA synchronous", cancellationToken).ConfigureAwait(false);
        int synchronous = synchronousValue is null or DBNull
            ? -1
            : Convert.ToInt32(synchronousValue, CultureInfo.InvariantCulture);
        string? journalMode = await ReadScalarTextAsync(connection, "PRAGMA journal_mode", cancellationToken).ConfigureAwait(false);

        EnsureSqliteDurability(synchronous, journalMode);
    }

    internal static void EnsureSqliteDurability(int synchronous, string? journalMode)
    {
        bool walMode = string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase);
        bool persistentRollbackJournal = string.Equals(journalMode, "delete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(journalMode, "truncate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(journalMode, "persist", StringComparison.OrdinalIgnoreCase);
        bool durable = walMode
            ? synchronous >= 2
            : persistentRollbackJournal && synchronous >= 3;

        if (!durable)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "Durable Sender requires SQLite WAL with synchronous=FULL/EXTRA, or a persistent rollback journal "
                + "with synchronous=EXTRA for strict power-loss durability. "
                + $"Actual: synchronous={synchronous}, journal_mode='{journalMode ?? "<unknown>"}'.", "Correct the named configuration before starting the host"));
        }
    }

    static async Task<string?> ReadScalarTextAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        object? value = await ReadScalarAsync(connection, commandText, cancellationToken).ConfigureAwait(false);
        return value is null or DBNull ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    static async Task<object?> ReadScalarAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }
}
