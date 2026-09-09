using System;
using System.Collections.Frozen;

namespace ViciOne.ServiceBus;

/// <summary>Immutable diagnostic-sensitivity metadata for one runtime message type.</summary>
public sealed record MessageSensitivityDescriptor
{
    /// <summary>Initializes the classification for a payload and its named members.</summary>
    /// <param name="payloadSensitivity">Whether the complete payload must be redacted.</param>
    /// <param name="sensitiveMembers">The case-sensitive CLR member names that must be redacted.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="payloadSensitivity" /> is not defined.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sensitiveMembers" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="sensitiveMembers" /> contains an empty or white-space member name.</exception>
    public MessageSensitivityDescriptor(
        MessagePayloadSensitivity payloadSensitivity,
        FrozenSet<string> sensitiveMembers)
    {
        if (payloadSensitivity is not (MessagePayloadSensitivity.Normal or MessagePayloadSensitivity.Sensitive))
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadSensitivity),
                payloadSensitivity,
                "Unknown message payload sensitivity value.");
        }

        ArgumentNullException.ThrowIfNull(sensitiveMembers);
        foreach (string memberName in sensitiveMembers)
        {
            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentException(
                    "Sensitive member names cannot be empty or contain only white-space characters.",
                    nameof(sensitiveMembers));
            }
        }

        PayloadSensitivity = payloadSensitivity;
        SensitiveMembers = sensitiveMembers.ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>Gets the classification applied to the complete payload.</summary>
    public MessagePayloadSensitivity PayloadSensitivity { get; }

    /// <summary>Gets the case-sensitive CLR member names that must be redacted.</summary>
    public FrozenSet<string> SensitiveMembers { get; }

    /// <summary>Gets whether every payload member must be redacted.</summary>
    public bool IsSensitive => PayloadSensitivity == MessagePayloadSensitivity.Sensitive;

    /// <summary>Determines whether a named CLR member is classified as sensitive.</summary>
    /// <param name="memberName">The case-sensitive CLR member name.</param>
    /// <returns><see langword="true" /> when the member must be redacted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="memberName" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="memberName" /> is empty or consists only of white-space characters.</exception>
    public bool IsMemberSensitive(string memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        return SensitiveMembers.Contains(memberName);
    }
}
