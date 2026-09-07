using System;
using System.Globalization;


namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Conservative bounded rendering that never invokes arbitrary application <see cref="object.ToString"/> implementations.</summary>
public sealed class MessageDiagnosticRedactor : IMessageDiagnosticRedactor
{
    /// <summary>Gets the stable marker used in place of sensitive values.</summary>
    public const string Redacted = "[REDACTED]";

    /// <summary>The stable marker for values that diagnostics deliberately do not materialize.</summary>
    public const string ComplexValue = "[COMPLEX-VALUE]";

    private readonly IMessageSensitivityInspector _inspector;
    private readonly int _maximumStringLength;

    /// <summary>Creates a redactor with a bounded diagnostic string length.</summary>
    /// <param name="inspector">The sensitivity metadata source.</param>
    /// <param name="maximumStringLength">The maximum number of UTF-16 code units retained before adding an ellipsis.</param>
    public MessageDiagnosticRedactor(IMessageSensitivityInspector inspector, int maximumStringLength = 256)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        if (maximumStringLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumStringLength));

        _maximumStringLength = maximumStringLength;
    }

    /// <summary>Renders the supplied value for diagnostics.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="value">The value to render without invoking application-defined string conversion.</param>
    /// <returns>A bounded invariant representation, a redaction marker or a complex-value marker.</returns>
    public string RenderValue(Type messageType, string? memberName, object? value)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        MessageSensitivityDescriptor descriptor = _inspector.Inspect(messageType);
        if (descriptor.IsSensitive || memberName is not null && descriptor.IsMemberSensitive(memberName))
            return Redacted;

        if (value is null)
            return "<null>";

        string? rendered = value switch
        {
            string text => text,
            char character => char.IsControl(character) ? "�" : character.ToString(),
            bool boolean => boolean ? "true" : "false",
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            Guid guid => guid.ToString("D"),
            DateTime timestamp => timestamp.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset timestamp => timestamp.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan duration => duration.ToString("c", CultureInfo.InvariantCulture),
            Uri uri when value.GetType() == typeof(Uri)
                => uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString,
            Enum enumValue => enumValue.ToString(),
            _ => null,
        };

        return rendered is null ? ComplexValue : BoundAndSanitize(rendered);
    }

    private string BoundAndSanitize(string value)
    {
        int length = Math.Min(value.Length, _maximumStringLength);
        if (length < value.Length
            && length > 0
            && char.IsHighSurrogate(value[length - 1])
            && char.IsLowSurrogate(value[length]))
            length--;

        bool truncated = value.Length > length;
        int firstUnsafeCharacter = -1;
        for (int index = 0; index < length; index++)
        {
            char character = value[index];
            bool invalidSurrogate = char.IsHighSurrogate(character)
                ? index + 1 >= length || !char.IsLowSurrogate(value[index + 1])
                : char.IsLowSurrogate(character)
                    && (index == 0 || !char.IsHighSurrogate(value[index - 1]));
            if (char.IsControl(character) || invalidSurrogate)
            {
                firstUnsafeCharacter = index;
                break;
            }
        }

        if (firstUnsafeCharacter < 0)
            return truncated ? string.Concat(value.AsSpan(0, length), "…") : value;

        char[] sanitized = value.AsSpan(0, length).ToArray();
        for (int index = firstUnsafeCharacter; index < sanitized.Length; index++)
        {
            char character = sanitized[index];
            bool invalidSurrogate = char.IsHighSurrogate(character)
                ? index + 1 >= sanitized.Length || !char.IsLowSurrogate(sanitized[index + 1])
                : char.IsLowSurrogate(character)
                    && (index == 0 || !char.IsHighSurrogate(sanitized[index - 1]));
            if (char.IsControl(character) || invalidSurrogate)
                sanitized[index] = '�';
        }

        return truncated ? string.Concat(sanitized, "…") : new string(sanitized);
    }
}
