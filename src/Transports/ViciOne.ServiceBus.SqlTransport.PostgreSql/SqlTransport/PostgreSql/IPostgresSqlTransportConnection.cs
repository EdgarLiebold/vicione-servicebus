using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

public interface IPostgresSqlTransportConnection :
    ISqlTransportConnection
{
    NpgsqlConnection Connection { get; }

    NpgsqlCommand CreateCommand(string commandText);
}
