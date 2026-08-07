// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    public class PostgresLockStatementProvider :
        SqlLockStatementProvider
    {
        public PostgresLockStatementProvider(bool enableSchemaCaching = true)
            : base(new PostgresLockStatementFormatter(), enableSchemaCaching)
        {
        }

        public PostgresLockStatementProvider(string schemaName, bool enableSchemaCaching = true)
            : base(schemaName, new PostgresLockStatementFormatter(), enableSchemaCaching)
        {
        }
    }
}
