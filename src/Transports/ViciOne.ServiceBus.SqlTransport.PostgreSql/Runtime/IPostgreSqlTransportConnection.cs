using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Exposes the PostgreSQL connection used by a SQL transport operation.</summary>
internal interface IPostgreSqlTransportConnection :
    ISqlTransportConnection
{
    /// <summary>Gets the underlying PostgreSQL connection.</summary>
    NpgsqlConnection Connection { get; }

    /// <summary>Creates a command associated with the underlying connection.</summary>
    /// <param name="commandText">The SQL command text.</param>
    /// <returns>A command whose connection is set to <see cref="Connection" />.</returns>
    NpgsqlCommand CreateCommand(string commandText);
}
