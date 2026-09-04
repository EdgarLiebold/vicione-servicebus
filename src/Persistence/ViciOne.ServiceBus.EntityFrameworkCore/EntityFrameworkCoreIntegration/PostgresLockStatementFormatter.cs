using System;
using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a postgres lock statement formatter implementation.
/// </summary>
public class PostgresLockStatementFormatter :
    ILockStatementFormatter
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="schema">The schema value.</param>
    /// <param name="table">The table value.</param>
    public void Create(StringBuilder sb, string schema, string table)
    {
        sb.AppendFormat("SELECT *, xmin FROM {0} WHERE ", FormatTableName(schema, table));
    }

    /// <summary>
    /// Performs the append column operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="index">The index value.</param>
    /// <param name="columnName">The column name value.</param>
    public void AppendColumn(StringBuilder sb, int index, string columnName)
    {
        if (index == 0)
            sb.AppendFormat("{0} = @p0", QuoteIdentifier(columnName));
        else
            sb.AppendFormat(" AND {0} = @p{1}", QuoteIdentifier(columnName), index);
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    public void Complete(StringBuilder sb)
    {
        sb.Append(" FOR UPDATE");
    }

    /// <summary>
    /// Creates outbox statement.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="schema">The schema value.</param>
    /// <param name="table">The table value.</param>
    /// <param name="createdColumn">The created column value.</param>
    /// <param name="outboxIdColumn">The outbox id column value.</param>
    /// <param name="busKeyColumn">The bus key column value.</param>
    /// <param name="statusColumn">The status column value.</param>
    /// <param name="nextDeliveryTimeColumn">The next delivery time column value.</param>
    public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn)
    {
        sb.AppendFormat(
            "SELECT *, xmin FROM {0} WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5} LIMIT 1 FOR UPDATE SKIP LOCKED",
            FormatTableName(schema, table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
            QuoteIdentifier(createdColumn), QuoteIdentifier(outboxIdColumn));
    }

    /// <summary>
    /// Creates inbox cleanup lock statement.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="schema">The schema value.</param>
    /// <param name="table">The table value.</param>
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
