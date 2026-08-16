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
