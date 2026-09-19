using System;
using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Adds a JSON or JSONB value to a Dapper command.</summary>
internal sealed class JsonParameter :
    SqlMapper.ICustomQueryParameter
{
    readonly string? _value;
    readonly NpgsqlDbType _type;

    /// <summary>Initializes a JSON parameter, using JSONB by default.</summary>
    /// <param name="value">The JSON text, or <see langword="null" /> for a database null.</param>
    /// <param name="type">The PostgreSQL JSON storage type.</param>
    public JsonParameter(string? value, NpgsqlDbType type = NpgsqlDbType.Jsonb)
    {
        _value = value;
        _type = type;
    }

    /// <summary>Adds the JSON parameter to a database command.</summary>
    /// <param name="command">The command that receives the parameter.</param>
    /// <param name="name">The command parameter name.</param>
    public void AddParameter(IDbCommand command, string name)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameter = new NpgsqlParameter(name, _type) { Value = _value != null ? _value : DBNull.Value };

        command.Parameters.Add(parameter);
    }
}
