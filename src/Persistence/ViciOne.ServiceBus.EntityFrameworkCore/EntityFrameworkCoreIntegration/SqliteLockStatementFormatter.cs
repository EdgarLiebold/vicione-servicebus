using System;
using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a sqlite lock statement formatter implementation.
/// </summary>
public class SqliteLockStatementFormatter :
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
        sb.Append($"SELECT * FROM {QuoteIdentifier(table)} WHERE ");
    }

    /// <summary>
    /// Performs the append column operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="index">The index value.</param>
    /// <param name="columnName">The column name value.</param>
    public void AppendColumn(StringBuilder sb, int index, string columnName)
    {
        sb.Append(index == 0
            ? $"{QuoteIdentifier(columnName)} = @p0"
            : $" AND {QuoteIdentifier(columnName)} = @p{index}");
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    public void Complete(StringBuilder sb)
    {
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
            "SELECT * FROM {0} WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5} LIMIT 1",
            QuoteIdentifier(table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
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
        // The surrounding Serializable SQLite transaction is the cleanup ownership boundary.
        sb.Append("SELECT 1");
    }

    static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
