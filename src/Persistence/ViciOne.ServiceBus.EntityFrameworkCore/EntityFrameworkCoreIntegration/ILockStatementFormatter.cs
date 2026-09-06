using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Builds provider-specific SQL for saga rows, due outbox rows, and inbox-cleanup ownership.</summary>
public interface ILockStatementFormatter
{
    /// <summary>Starts a parameterized row-lock query for a mapped table.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped relational schema.</param>
    /// <param name="table">The mapped relational table.</param>
    void Create(StringBuilder sb, string schema, string table);
    /// <summary>Appends an equality predicate whose parameter index matches the property position.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="index">The zero-based parameter index.</param>
    /// <param name="columnName">The mapped column name.</param>
    void AppendColumn(StringBuilder sb, int index, string columnName);
    /// <summary>Completes the row-lock statement with provider-specific locking syntax.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    void Complete(StringBuilder sb);

    /// <summary>Builds a query that locks the next due outbox row for one bus.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped relational schema.</param>
    /// <param name="table">The mapped outbox table.</param>
    /// <param name="createdColumn">The created column.</param>
    /// <param name="outboxIdColumn">The outbox id column.</param>
    /// <param name="busKeyColumn">The bus key column.</param>
    /// <param name="statusColumn">The status column.</param>
    /// <param name="nextDeliveryTimeColumn">The next delivery time column.</param>
    void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn);
    /// <summary>Builds a transaction-scoped, non-blocking ownership query for inbox cleanup.</summary>
    /// <param name="sb">The destination SQL builder.</param>
    /// <param name="schema">The mapped relational schema.</param>
    /// <param name="table">The mapped inbox table.</param>
    void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table);
}
