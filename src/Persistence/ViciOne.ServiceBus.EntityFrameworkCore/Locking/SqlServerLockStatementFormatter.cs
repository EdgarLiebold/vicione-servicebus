using System;
using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Builds SQL Server row-lock, outbox-claim, and application-lock SQL.</summary>
internal sealed class SqlServerLockStatementFormatter :
    ILockStatementFormatter
{
    readonly bool _serializable;

    /// <summary>Initializes SQL Server row-lock formatting with optional serializable isolation.</summary>
    /// <param name="serializable"><see langword="true"/> to add the <c>SERIALIZABLE</c> table hint to row locks.</param>
    public SqlServerLockStatementFormatter(bool serializable)
    {
        _serializable = serializable;
    }

    /// <summary>Starts a quoted row query with SQL Server update and row locks.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped SQL Server schema.</param>
    /// <param name="table">The mapped SQL Server table.</param>
    public void Create(StringBuilder sb, string schema, string table)
    {
        sb.AppendFormat("SELECT * FROM {0} WITH (UPDLOCK, ROWLOCK", FormatTableName(schema, table));
        if (_serializable)
            sb.Append(", SERIALIZABLE");
        sb.Append(") WHERE ");
    }

    /// <summary>Appends a quoted equality predicate for a positional EF Core parameter.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="index">The zero-based EF Core parameter index.</param>
    /// <param name="columnName">The mapped SQL Server column.</param>
    public void AppendColumn(StringBuilder sb, int index, string columnName)
    {
        if (index == 0)
            sb.AppendFormat("{0} = @p0", QuoteIdentifier(columnName));
        else
            sb.AppendFormat(" AND {0} = @p{1}", QuoteIdentifier(columnName), index);
    }

    /// <summary>Completes the row-lock statement; SQL Server lock hints are already present in the query prefix.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    public void Complete(StringBuilder sb)
    {
    }

    /// <summary>Builds a SQL Server query that claims one due outbox row with <c>READPAST</c>.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped SQL Server schema.</param>
    /// <param name="table">The mapped SQL Server outbox table.</param>
    /// <param name="createdColumn">The created column.</param>
    /// <param name="outboxIdColumn">The outbox id column.</param>
    /// <param name="busKeyColumn">The bus key column.</param>
    /// <param name="statusColumn">The status column.</param>
    /// <param name="nextDeliveryTimeColumn">The next delivery time column.</param>
    public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn)
    {
        sb.AppendFormat(
            "SELECT TOP 1 * FROM {0} WITH (UPDLOCK, ROWLOCK, READPAST) WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5}",
            FormatTableName(schema, table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
            QuoteIdentifier(createdColumn), QuoteIdentifier(outboxIdColumn));
    }

    /// <summary>Builds a non-blocking transaction-owned <c>sp_getapplock</c> query for inbox cleanup.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped SQL Server schema.</param>
    /// <param name="table">The mapped SQL Server inbox table.</param>
    public void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table)
    {
        string resource = $"ViciOne.ServiceBus:InboxCleanup:{schema}.{table}".Replace("'", "''", StringComparison.Ordinal);
        sb.Append("DECLARE @result int; EXEC @result = sys.sp_getapplock ")
            .Append("@Resource = N'").Append(resource).Append("', ")
            .Append("@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; ")
            .Append("SELECT CASE WHEN @result >= 0 THEN 1 ELSE 0 END");
    }

    static string FormatTableName(string schema, string table)
    {
        return string.IsNullOrEmpty(schema)
            ? QuoteIdentifier(table)
            : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";
    }

    static string QuoteIdentifier(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}
