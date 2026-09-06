using System;
using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Builds SQLite selection SQL used while the caller holds a serializable transaction.</summary>
public class SqliteLockStatementFormatter :
    ILockStatementFormatter
{
    /// <summary>Starts a quoted SQLite row query.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">Ignored because SQLite does not qualify table names with schemas.</param>
    /// <param name="table">The mapped SQLite table.</param>
    public void Create(StringBuilder sb, string schema, string table)
    {
        sb.Append($"SELECT * FROM {QuoteIdentifier(table)} WHERE ");
    }

    /// <summary>Appends a quoted equality predicate for a positional EF Core parameter.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="index">The zero-based EF Core parameter index.</param>
    /// <param name="columnName">The mapped SQLite column.</param>
    public void AppendColumn(StringBuilder sb, int index, string columnName)
    {
        sb.Append(index == 0
            ? $"{QuoteIdentifier(columnName)} = @p0"
            : $" AND {QuoteIdentifier(columnName)} = @p{index}");
    }

    /// <summary>Completes the statement; SQLite obtains exclusion from the surrounding transaction.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    public void Complete(StringBuilder sb)
    {
    }

    /// <summary>Builds a SQLite query that selects the next due outbox row.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">Ignored because SQLite does not qualify table names with schemas.</param>
    /// <param name="table">The mapped SQLite outbox table.</param>
    /// <param name="createdColumn">The created column.</param>
    /// <param name="outboxIdColumn">The outbox id column.</param>
    /// <param name="busKeyColumn">The bus key column.</param>
    /// <param name="statusColumn">The status column.</param>
    /// <param name="nextDeliveryTimeColumn">The next delivery time column.</param>
    public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn)
    {
        sb.AppendFormat(
            "SELECT * FROM {0} WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5} LIMIT 1",
            QuoteIdentifier(table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
            QuoteIdentifier(createdColumn), QuoteIdentifier(outboxIdColumn));
    }

    /// <summary>Builds a scalar ownership query whose exclusion comes from the surrounding serializable transaction.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">Ignored because SQLite does not qualify table names with schemas.</param>
    /// <param name="table">The mapped SQLite inbox table.</param>
    public void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table)
    {
        // The surrounding Serializable SQLite transaction is the cleanup ownership boundary.
        sb.Append("SELECT 1");
    }

    static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
