// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    public class SqliteLockStatementProvider :
        SqlLockStatementProvider
    {
        public SqliteLockStatementProvider(bool enableSchemaCaching = true)
            : base(new SqliteLockStatementFormatter(), enableSchemaCaching)
        {
        }

        public SqliteLockStatementProvider(string schemaName, bool enableSchemaCaching = true)
            : base(schemaName, new SqliteLockStatementFormatter(), enableSchemaCaching)
        {
        }
    }
}
