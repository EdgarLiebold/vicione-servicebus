using System;
using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Builds PostgreSQL row-lock, outbox-claim, and advisory-lock SQL.</summary>
internal sealed class PostgreSqlLockStatementFormatter :
    ILockStatementFormatter
{
    /// <summary>Starts a quoted PostgreSQL row query and includes <c>xmin</c> for concurrency tracking.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped PostgreSQL schema.</param>
    /// <param name="table">The mapped PostgreSQL table.</param>
    public void Create(StringBuilder sb, string schema, string table)
    {
        sb.AppendFormat("SELECT *, xmin FROM {0} WHERE ", FormatTableName(schema, table));
    }

    /// <summary>Appends a quoted equality predicate for a positional EF Core parameter.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="index">The zero-based EF Core parameter index.</param>
    /// <param name="columnName">The mapped PostgreSQL column.</param>
    public void AppendColumn(StringBuilder sb, int index, string columnName)
    {
        if (index == 0)
            sb.AppendFormat("{0} = @p0", QuoteIdentifier(columnName));
        else
            sb.AppendFormat(" AND {0} = @p{1}", QuoteIdentifier(columnName), index);
    }

    /// <summary>Completes the row query with a PostgreSQL <c>FOR UPDATE</c> lock.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    public void Complete(StringBuilder sb)
    {
        sb.Append(" FOR UPDATE");
    }

    /// <summary>Builds a PostgreSQL query that claims one due outbox row with <c>FOR UPDATE SKIP LOCKED</c>.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped PostgreSQL schema.</param>
    /// <param name="table">The mapped PostgreSQL outbox table.</param>
    /// <param name="createdColumn">The created column.</param>
    /// <param name="outboxIdColumn">The outbox id column.</param>
    /// <param name="busKeyColumn">The bus key column.</param>
    /// <param name="statusColumn">The status column.</param>
    /// <param name="nextDeliveryTimeColumn">The next delivery time column.</param>
    public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn)
    {
        sb.AppendFormat(
            "SELECT *, xmin FROM {0} WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5} LIMIT 1 FOR UPDATE SKIP LOCKED",
            FormatTableName(schema, table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
            QuoteIdentifier(createdColumn), QuoteIdentifier(outboxIdColumn));
    }

    /// <summary>Builds a transaction-scoped PostgreSQL advisory-lock query for the mapped inbox table.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped PostgreSQL schema.</param>
    /// <param name="table">The mapped PostgreSQL inbox table.</param>
    public void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table)
    {
        string resource = $"ViciOne.ServiceBus:InboxCleanup:{schema}.{table}".Replace("'", "''", StringComparison.Ordinal);
        sb.Append($"SELECT CASE WHEN pg_try_advisory_xact_lock(hashtext('{resource}'), 0) THEN 1 ELSE 0 END");
    }

    static string FormatTableName(string schema, string table)
    {
        return string.IsNullOrEmpty(schema)
            ? QuoteIdentifier(table)
            : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";
    }

    static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
