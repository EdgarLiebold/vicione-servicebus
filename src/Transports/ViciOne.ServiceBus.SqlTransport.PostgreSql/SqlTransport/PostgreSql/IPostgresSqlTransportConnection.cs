using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Defines the contract for postgres sql transport connection.
/// </summary>
public interface IPostgresSqlTransportConnection :
    ISqlTransportConnection
{
    /// <summary>
    /// Gets the connection value.
    /// </summary>
    NpgsqlConnection Connection { get; }

    /// <summary>
    /// Creates command.
    /// </summary>
    /// <param name="commandText">The command text value.</param>
    /// <returns>The result of the operation.</returns>
    NpgsqlCommand CreateCommand(string commandText);
}
