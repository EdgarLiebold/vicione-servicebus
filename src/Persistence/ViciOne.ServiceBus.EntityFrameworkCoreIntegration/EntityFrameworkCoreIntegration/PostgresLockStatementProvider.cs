namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    public class PostgresLockStatementProvider :
        SqlLockStatementProvider
    {
        public PostgresLockStatementProvider()
            : base(new PostgresLockStatementFormatter())
        {
        }

        public PostgresLockStatementProvider(string schemaName)
            : base(schemaName, new PostgresLockStatementFormatter())
        {
        }
    }
}
