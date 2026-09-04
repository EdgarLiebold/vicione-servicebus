namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Provides extension methods for sql server sql transport options.
/// </summary>
public static class SqlServerSqlTransportOptionsExtensions
{
    /// <summary>
    /// Performs the format data source operation.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static string? FormatDataSource(this SqlTransportOptions options)
    {
        return options.Port.HasValue ? $"{options.Host},{options.Port}" : options.Host;
    }
}
