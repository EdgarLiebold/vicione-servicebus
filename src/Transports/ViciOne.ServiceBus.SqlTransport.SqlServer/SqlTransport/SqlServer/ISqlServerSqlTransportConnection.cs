// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

using Microsoft.Data.SqlClient;


public interface ISqlServerSqlTransportConnection :
    ISqlTransportConnection
{
    SqlConnection Connection { get; }
}
