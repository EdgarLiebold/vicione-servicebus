using System;
using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class SqliteLockStatementFormatter :
    ILockStatementFormatter
{
    public void Create(StringBuilder sb, string schema, string table)
    {
        sb.Append($"SELECT * FROM {QuoteIdentifier(table)} WHERE ");
    }

    public void AppendColumn(StringBuilder sb, int index, string columnName)
    {
        sb.Append(index == 0
            ? $"{QuoteIdentifier(columnName)} = @p0"
            : $" AND {QuoteIdentifier(columnName)} = @p{index}");
    }

    public void Complete(StringBuilder sb)
    {
    }

    public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn)
    {
        sb.AppendFormat(
            "SELECT * FROM {0} WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5} LIMIT 1",
            QuoteIdentifier(table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
            QuoteIdentifier(createdColumn), QuoteIdentifier(outboxIdColumn));
    }

    public void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table)
    {
        // The surrounding Serializable SQLite transaction is the cleanup ownership boundary.
        sb.Append("SELECT 1");
    }

    static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
