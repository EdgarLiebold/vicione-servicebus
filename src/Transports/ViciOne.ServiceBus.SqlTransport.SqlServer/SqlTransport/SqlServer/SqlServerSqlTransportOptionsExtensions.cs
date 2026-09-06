namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Provides SQL Server-specific formatting for SQL transport options.</summary>
public static class SqlServerSqlTransportOptionsExtensions
{
    /// <summary>Formats the configured host and optional port as a SQL Server data source.</summary>
    /// <param name="options">The SQL transport options containing the host and port.</param>
    /// <returns>The host followed by a comma and port when a port is configured; otherwise, the host.</returns>
    public static string? FormatDataSource(this SqlTransportOptions options)
    {
        return options.Port.HasValue ? $"{options.Host},{options.Port}" : options.Host;
    }
}
