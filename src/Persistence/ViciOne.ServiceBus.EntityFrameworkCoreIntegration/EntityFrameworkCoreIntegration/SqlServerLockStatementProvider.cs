// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    public class SqlServerLockStatementProvider :
        SqlLockStatementProvider
    {
        public SqlServerLockStatementProvider(bool enableSchemaCaching = true, bool serializable = false)
            : base(new SqlServerLockStatementFormatter(serializable), enableSchemaCaching)
        {
        }

        public SqlServerLockStatementProvider(string schemaName, bool enableSchemaCaching = true, bool serializable = false)
            : base(schemaName, new SqlServerLockStatementFormatter(serializable), enableSchemaCaching)
        {
        }
    }
}
