using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Defines the contract for sql server sql transport connection.
/// </summary>
public interface ISqlServerSqlTransportConnection :
    ISqlTransportConnection
{
    /// <summary>
    /// Gets the connection value.
    /// </summary>
    SqlConnection Connection { get; }
}
