using System;
using System.Text;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Validates identifiers that are embedded in PostgreSQL data-definition statements.</summary>
internal static class PostgreSqlIdentifier
{
    static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>Returns a non-empty identifier that is safe for the migrator's quoted SQL templates.</summary>
    /// <param name="identifier">The database, schema, or role identifier.</param>
    /// <param name="parameterName">The configuration property reported when validation fails.</param>
    /// <returns>The validated identifier.</returns>
    /// <exception cref="ArgumentException">The identifier is empty, malformed, too long, or contains a quote or control character.</exception>
    public static string Validate(string? identifier, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, parameterName);

        int byteCount;
        try
        {
            byteCount = StrictUtf8.GetByteCount(identifier);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ArgumentException("PostgreSQL identifiers must contain valid Unicode text.", parameterName, exception);
        }

        if (byteCount > 63)
            throw new ArgumentException("PostgreSQL identifiers must not exceed 63 UTF-8 bytes.", parameterName);

        foreach (var character in identifier)
        {
            if (character is '\'' or '"' || char.IsControl(character))
                throw new ArgumentException("PostgreSQL identifiers must not contain quote or control characters.", parameterName);
        }

        return identifier;
    }
}
