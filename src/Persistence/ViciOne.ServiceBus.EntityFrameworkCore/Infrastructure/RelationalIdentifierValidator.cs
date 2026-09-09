using System.Text;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Enforces the portable safety boundary for quoted relational table and schema identifiers.</summary>
internal static class RelationalIdentifierValidator
{
    internal const int MaximumUtf8Bytes = 63;

    public static string Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(char.IsControl))
            throw new ArgumentException("A relational identifier cannot contain control characters.", parameterName);

        int utf8Bytes = Encoding.UTF8.GetByteCount(value);
        if (utf8Bytes > MaximumUtf8Bytes)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"A relational identifier cannot exceed {MaximumUtf8Bytes} UTF-8 bytes.");
        }

        return value;
    }
}
