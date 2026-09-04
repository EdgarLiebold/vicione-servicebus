namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class SqliteLockStatementProvider :
    SqlLockStatementProvider
{
    public SqliteLockStatementProvider()
        : base(new SqliteLockStatementFormatter())
    {
    }

    public SqliteLockStatementProvider(string schemaName)
        : base(schemaName, new SqliteLockStatementFormatter())
    {
    }
}
