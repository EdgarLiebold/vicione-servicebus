using System;
using System.Data;
using Dapper;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Provides an uri type handler implementation.
/// </summary>
public class UriTypeHandler : SqlMapper.TypeHandler<Uri>
{
    /// <summary>
    /// Sets value.
    /// </summary>
    /// <param name="parameter">The parameter value.</param>
    /// <param name="value">The value.</param>
    public override void SetValue(IDbDataParameter parameter, Uri? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value != null ? value.ToString() : DBNull.Value;
    }

    /// <summary>
    /// Parses the supplied representation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public override Uri Parse(object value)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
            return new Uri(text);

        return null!;
    }
}
