using System;

namespace ViciOne.ServiceBus.Azure.Table.Infrastructure;

internal static class AzureTableNameValidator
{
    private const int MaximumCharacters = 63;
    private const int MinimumCharacters = 3;

    internal static string Validate(string tableName, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName, parameterName);

        if (tableName.Length is < MinimumCharacters or > MaximumCharacters
            || !IsAsciiLetter(tableName[0]))
        {
            throw InvalidTableName(parameterName);
        }

        for (var index = 1; index < tableName.Length; index++)
        {
            char character = tableName[index];
            if (!IsAsciiLetter(character) && !char.IsAsciiDigit(character))
                throw InvalidTableName(parameterName);
        }

        if (string.Equals(tableName, "tables", StringComparison.OrdinalIgnoreCase))
            throw InvalidTableName(parameterName);

        return tableName;
    }

    private static bool IsAsciiLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static ArgumentException InvalidTableName(string parameterName) =>
        new("An Azure Table name must contain 3 to 63 ASCII letters or digits, begin with a letter, and not use a reserved name.", parameterName);
}
