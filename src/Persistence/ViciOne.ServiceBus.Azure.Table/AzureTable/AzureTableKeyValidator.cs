namespace ViciOne.ServiceBus.AzureTable;

using System;
using System.Linq;

internal static class AzureTableKeyValidator
{
    internal const int MaximumKeyCharacters = 1024;

    internal static string Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > MaximumKeyCharacters
            || value.Any(IsDisallowedCharacter))
        {
            throw new ArgumentException(
                $"An Azure Table key must contain at most {MaximumKeyCharacters} characters and no '/', '\\', '#', '?', or control characters.",
                parameterName);
        }

        return value;
    }

    internal static Guid ValidateCorrelationId(Guid correlationId, string parameterName)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("An Azure Table saga key requires a non-empty correlation identifier.", parameterName);

        return correlationId;
    }

    private static bool IsDisallowedCharacter(char value) =>
        value is '/' or '\\' or '#' or '?'
        || value is >= '\u0000' and <= '\u001f'
        || value is >= '\u007f' and <= '\u009f';
}
