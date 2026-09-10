using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Exposes the SQL Server connection used by an internal transport operation.</summary>
internal interface ISqlServerTransportConnection :
    ISqlTransportConnection
{
    /// <summary>Gets the underlying SQL Server connection.</summary>
    SqlConnection Connection { get; }
}
