namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class SqlServerLockStatementProvider :
    SqlLockStatementProvider
{
    public SqlServerLockStatementProvider(bool serializable = false)
        : base(new SqlServerLockStatementFormatter(serializable))
    {
    }

    public SqlServerLockStatementProvider(string schemaName, bool serializable = false)
        : base(schemaName, new SqlServerLockStatementFormatter(serializable))
    {
    }
}
