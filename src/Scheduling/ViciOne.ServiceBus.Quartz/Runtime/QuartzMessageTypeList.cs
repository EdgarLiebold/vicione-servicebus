using System;
using System.Text.Json;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Encodes and decodes persisted message-type identifiers as a JSON array.</summary>
internal static class QuartzMessageTypeList
{
    public static string Serialize(string[] messageTypes)
    {
        Validate(messageTypes);
        return JsonSerializer.Serialize(messageTypes);
    }

    public static void Validate(string[] messageTypes)
    {
        Validate(messageTypes, nameof(messageTypes));
    }

    public static void Validate(string[] messageTypes, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(messageTypes, parameterName);
        if (messageTypes.Length == 0)
            throw new ArgumentException("At least one message-type identifier is required.", parameterName);

        for (var index = 0; index < messageTypes.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(messageTypes[index]))
            {
                throw new ArgumentException(
                    $"The message-type identifier at index {index} is required.",
                    parameterName);
            }
        }
    }

    public static string[] Deserialize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("The persisted message-type list must be a JSON array.");

        string[]? messageTypes = JsonSerializer.Deserialize<string[]>(value);
        if (messageTypes is null || messageTypes.Length == 0)
            throw new FormatException("The persisted message-type list must contain at least one identifier.");

        for (var index = 0; index < messageTypes.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(messageTypes[index]))
                throw new FormatException($"The persisted message-type identifier at index {index} is required.");
        }

        return messageTypes;
    }
}
