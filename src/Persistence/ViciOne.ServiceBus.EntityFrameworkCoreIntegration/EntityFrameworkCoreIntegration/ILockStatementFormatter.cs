using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public interface ILockStatementFormatter
{
    void Create(StringBuilder sb, string schema, string table);
    void AppendColumn(StringBuilder sb, int index, string columnName);
    void Complete(StringBuilder sb);

    void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
        string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn);
    void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table);
}
