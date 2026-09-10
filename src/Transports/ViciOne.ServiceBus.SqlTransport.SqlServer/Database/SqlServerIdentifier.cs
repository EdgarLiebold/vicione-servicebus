using System;
using System.Linq;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Validates SQL Server identifiers before they are embedded in provisioning statements.</summary>
internal static class SqlServerIdentifier
{
    const int MaximumLength = 128;

    /// <summary>Validates a database name used inside a bracket-delimited SQL Server identifier.</summary>
    /// <param name="value">The configured database name.</param>
    /// <param name="parameterName">The configuration property reported when validation fails.</param>
    /// <returns>The validated database name.</returns>
    public static string ValidateDatabase(string? value, string parameterName) =>
        Validate(value, parameterName, static character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    /// <summary>Validates a schema or role name used as an unquoted SQL Server identifier.</summary>
    /// <param name="value">The configured schema or role name.</param>
    /// <param name="parameterName">The configuration property reported when validation fails.</param>
    /// <returns>The validated identifier.</returns>
    public static string ValidateRegular(string? value, string parameterName)
    {
        string identifier = Validate(value, parameterName, static character => char.IsAsciiLetterOrDigit(character) || character == '_');
        if (!char.IsAsciiLetter(identifier[0]) && identifier[0] != '_')
            throw new ArgumentException("SQL Server schema and role names must start with an ASCII letter or underscore.", parameterName);

        return identifier;
    }

    /// <summary>Validates a SQL Server principal name used by provisioning statements.</summary>
    /// <param name="value">The login or database-user name.</param>
    /// <param name="parameterName">The configuration property reported when validation fails.</param>
    /// <returns>The validated principal name.</returns>
    public static string ValidatePrincipal(string? value, string parameterName) =>
        Validate(value, parameterName,
            static character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.' or '\\' or '@' or '$');

    static string Validate(string? value, string parameterName, Func<char, bool> isAllowed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > MaximumLength)
            throw new ArgumentException($"SQL Server identifiers must not exceed {MaximumLength} characters.", parameterName);

        if (!value.All(isAllowed))
            throw new ArgumentException("The SQL Server identifier contains unsupported characters.", parameterName);

        return value;
    }
}
