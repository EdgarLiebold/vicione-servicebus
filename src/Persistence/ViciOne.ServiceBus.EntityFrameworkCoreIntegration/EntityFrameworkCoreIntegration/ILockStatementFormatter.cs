// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System.Text;


    public interface ILockStatementFormatter
    {
        void Create(StringBuilder sb, string schema, string table);
        void AppendColumn(StringBuilder sb, int index, string columnName);
        void Complete(StringBuilder sb);

        void CreateOutboxStatement(StringBuilder sb, string schema, string table, string columnName);
    }
}
