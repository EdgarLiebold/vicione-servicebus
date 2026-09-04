using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

public interface ISqlServerSqlTransportConnection :
    ISqlTransportConnection
{
    SqlConnection Connection { get; }
}
