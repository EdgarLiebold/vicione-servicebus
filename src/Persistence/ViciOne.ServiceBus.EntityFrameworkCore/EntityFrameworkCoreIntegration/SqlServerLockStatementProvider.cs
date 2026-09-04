namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a sql server lock statement provider implementation.
/// </summary>
public class SqlServerLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serializable">The serializable value.</param>
    public SqlServerLockStatementProvider(bool serializable = false)
        : base(new SqlServerLockStatementFormatter(serializable))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schemaName">The schema name value.</param>
    /// <param name="serializable">The serializable value.</param>
    public SqlServerLockStatementProvider(string schemaName, bool serializable = false)
        : base(schemaName, new SqlServerLockStatementFormatter(serializable))
    {
    }
}
