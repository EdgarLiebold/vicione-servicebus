// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

using Npgsql;


public interface IPostgresSqlTransportConnection :
    ISqlTransportConnection
{
    NpgsqlConnection Connection { get; }

    NpgsqlCommand CreateCommand(string commandText);
}
