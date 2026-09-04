using System;
using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides a json parameter implementation.
/// </summary>
public class JsonParameter :
    SqlMapper.ICustomQueryParameter
{
    readonly string? _value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    public JsonParameter(string? value)
    {
        _value = value;
    }

    /// <summary>
    /// Adds parameter to the configuration.
    /// </summary>
    /// <param name="command">The command value.</param>
    /// <param name="name">The name value.</param>
    public void AddParameter(IDbCommand command, string name)
    {
        var parameter = new NpgsqlParameter(name, NpgsqlDbType.Jsonb) { Value = _value != null ? _value : DBNull.Value };

        command.Parameters.Add(parameter);
    }
}
