using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for lock statement formatter.
/// </summary>
public interface ILockStatementFormatter
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="schema">The schema value.</param>
    /// <param name="table">The table value.</param>
    void Create(StringBuilder sb, string schema, string table);
    /// <summary>
    /// Performs the append column operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="index">The index value.</param>
    /// <param name="columnName">The column name value.</param>
    void AppendColumn(StringBuilder sb, int index, string columnName);
    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    void Complete(StringBuilder sb);

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
    void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn);
    /// <summary>
    /// Creates inbox cleanup lock statement.
    /// </summary>
    /// <param name="sb">The sb value.</param>
    /// <param name="schema">The schema value.</param>
    /// <param name="table">The table value.</param>
    void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table);
}
