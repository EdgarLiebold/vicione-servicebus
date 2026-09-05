using System;
using System.Data;
using Dapper;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Maps database text values to non-null URI instances.
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
    /// <returns>A relative or absolute URI preserving the stored representation.</returns>
    /// <exception cref="ArgumentNullException">The database value is <see langword="null" />.</exception>
    /// <exception cref="InvalidCastException">The database value is not text.</exception>
    /// <exception cref="UriFormatException">The database text is empty or is not a valid URI.</exception>
    public override Uri Parse(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is not string text)
            throw new InvalidCastException($"Database URI values must be strings, but the supplied value is '{value.GetType().FullName}'.");

        if (string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri? uri))
            throw new UriFormatException("The database URI value is empty or malformed.");

        return uri;
    }
}
