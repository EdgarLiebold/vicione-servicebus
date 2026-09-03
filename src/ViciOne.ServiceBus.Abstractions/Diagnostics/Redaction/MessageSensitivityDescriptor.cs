namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Collections.Frozen;

/// <summary>Immutable diagnostic-sensitivity metadata for one runtime message type.</summary>
public sealed record MessageSensitivityDescriptor
{
    /// <summary>Creates one validated immutable descriptor.</summary>
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

        PayloadSensitivity = payloadSensitivity;
        SensitiveMembers = sensitiveMembers ?? throw new ArgumentNullException(nameof(sensitiveMembers));
    }

    /// <summary>Gets the whole-payload classification.</summary>
    public MessagePayloadSensitivity PayloadSensitivity { get; }

    /// <summary>Gets the exact case-sensitive set of sensitive CLR member names.</summary>
    public FrozenSet<string> SensitiveMembers { get; }

    /// <summary>Gets whether every payload member must be redacted.</summary>
    public bool IsSensitive => PayloadSensitivity == MessagePayloadSensitivity.Sensitive;

    /// <summary>Returns whether the named CLR member is sensitive.</summary>
    public bool IsMemberSensitive(string memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        return SensitiveMembers.Contains(memberName);
    }
}
