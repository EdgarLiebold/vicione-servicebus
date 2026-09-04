using System;
using System.Data;
using Dapper;
using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides an enum parameter implementation.
/// </summary>
public class EnumParameter :
    SqlMapper.ICustomQueryParameter
{
    readonly string _dataTypeName;
    readonly string? _value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="dataTypeName">The data type name value.</param>
    public EnumParameter(string? value, string dataTypeName)
    {
        _value = value;
        _dataTypeName = dataTypeName;
    }

    /// <summary>
    /// Adds parameter to the configuration.
    /// </summary>
    /// <param name="command">The command value.</param>
    /// <param name="name">The name value.</param>
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
