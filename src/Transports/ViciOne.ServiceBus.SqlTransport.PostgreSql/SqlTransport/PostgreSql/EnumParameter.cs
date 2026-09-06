using System;
using System.Data;
using Dapper;
using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Adds a PostgreSQL enum value to a Dapper command with an explicit database type name.</summary>
public class EnumParameter :
    SqlMapper.ICustomQueryParameter
{
    readonly string _dataTypeName;
    readonly string? _value;

    /// <summary>Initializes a PostgreSQL enum parameter.</summary>
    /// <param name="value">The enum label, or <see langword="null" /> for a database null.</param>
    /// <param name="dataTypeName">The PostgreSQL enum type name.</param>
    public EnumParameter(string? value, string dataTypeName)
    {
        _value = value;
        _dataTypeName = dataTypeName;
    }

    /// <summary>Adds the typed enum parameter to a database command.</summary>
    /// <param name="command">The command that receives the parameter.</param>
    /// <param name="name">The command parameter name.</param>
    public void AddParameter(IDbCommand command, string name)
    {
        var parameter = new NpgsqlParameter
        {
            ParameterName = name,
            Value = _value != null ? _value : DBNull.Value,
            DataTypeName = _dataTypeName
        };

        command.Parameters.Add(parameter);
    }
}
