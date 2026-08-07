// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

public static class SqlServerSqlTransportOptionsExtensions
{
    public static string? FormatDataSource(this SqlTransportOptions options)
    {
        return options.Port.HasValue ? $"{options.Host},{options.Port}" : options.Host;
    }
}
