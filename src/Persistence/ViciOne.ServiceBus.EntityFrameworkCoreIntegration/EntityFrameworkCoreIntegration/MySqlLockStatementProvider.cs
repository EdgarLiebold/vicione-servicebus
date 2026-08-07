// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    public class MySqlLockStatementProvider :
        SqlLockStatementProvider
    {
        public MySqlLockStatementProvider(bool enableSchemaCaching = true)
            : base(new MySqlLockStatementFormatter(), enableSchemaCaching)
        {
        }
    }
}
