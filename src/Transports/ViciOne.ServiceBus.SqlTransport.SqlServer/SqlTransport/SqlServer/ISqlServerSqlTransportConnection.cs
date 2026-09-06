using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Exposes the SQL Server connection used by a SQL transport operation.</summary>
public interface ISqlServerSqlTransportConnection :
    ISqlTransportConnection
{
    /// <summary>Gets the underlying SQL Server connection.</summary>
    SqlConnection Connection { get; }
}
