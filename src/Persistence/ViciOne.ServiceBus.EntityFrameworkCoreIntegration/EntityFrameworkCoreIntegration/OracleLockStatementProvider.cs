// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    public class OracleLockStatementProvider :
        SqlLockStatementProvider
    {
        public OracleLockStatementProvider(bool enableSchemaCaching = true)
            : base(new OracleLockStatementFormatter(), enableSchemaCaching)
        {
        }

        public OracleLockStatementProvider(string schemaName, bool enableSchemaCaching = true)
            : base(schemaName, new OracleLockStatementFormatter(), enableSchemaCaching)
        {
        }
    }
}
