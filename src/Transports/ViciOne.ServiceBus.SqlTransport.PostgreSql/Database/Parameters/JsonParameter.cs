using System;
using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Adds a JSONB value to a Dapper command.</summary>
internal sealed class JsonParameter :
    SqlMapper.ICustomQueryParameter
{
    readonly string? _value;

    /// <summary>Initializes a JSONB parameter.</summary>
    /// <param name="value">The JSON text, or <see langword="null" /> for a database null.</param>
    public JsonParameter(string? value)
    {
        _value = value;
    }

    /// <summary>Adds the JSONB parameter to a database command.</summary>
    /// <param name="command">The command that receives the parameter.</param>
    /// <param name="name">The command parameter name.</param>
    public void AddParameter(IDbCommand command, string name)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameter = new NpgsqlParameter(name, NpgsqlDbType.Jsonb) { Value = _value != null ? _value : DBNull.Value };

        command.Parameters.Add(parameter);
    }
}
