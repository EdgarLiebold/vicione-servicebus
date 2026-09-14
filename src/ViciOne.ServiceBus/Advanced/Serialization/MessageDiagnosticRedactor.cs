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
    /// <exception cref="ArgumentNullException"><paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="memberName" /> is empty or contains only white-space characters.</exception>
    public string RenderValue(Type messageType, string? memberName, object? value)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        if (memberName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

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
        int length = GetBoundedLength(value);
        bool truncated = value.Length > length;
        int firstUnsafeCharacter = FindFirstUnsafeCharacter(value.AsSpan(0, length));

        if (firstUnsafeCharacter < 0)
            return truncated ? string.Concat(value.AsSpan(0, length), "…") : value;

        char[] sanitized = value.AsSpan(0, length).ToArray();
        Sanitize(sanitized.AsSpan(firstUnsafeCharacter));

        return truncated ? string.Concat(sanitized, "…") : new string(sanitized);
    }

    private int GetBoundedLength(string value)
    {
        int length = Math.Min(value.Length, _maximumStringLength);
        if (length < value.Length
            && length > 0
            && char.IsHighSurrogate(value[length - 1])
            && char.IsLowSurrogate(value[length]))
            return length - 1;

        return length;
    }

    private static int FindFirstUnsafeCharacter(ReadOnlySpan<char> value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (IsUnsafeCharacter(value, index))
                return index;
        }

        return -1;
    }

    private static void Sanitize(Span<char> value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (IsUnsafeCharacter(value, index))
                value[index] = '�';
        }
    }

    private static bool IsUnsafeCharacter(ReadOnlySpan<char> value, int index)
    {
        char character = value[index];
        return char.IsControl(character) || IsInvalidSurrogate(value, index, character);
    }

    private static bool IsInvalidSurrogate(ReadOnlySpan<char> value, int index, char character)
    {
        if (char.IsHighSurrogate(character))
            return index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]);

        return char.IsLowSurrogate(character)
            && (index == 0 || !char.IsHighSurrogate(value[index - 1]));
    }
}
