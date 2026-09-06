using System;
using System.Collections.Frozen;

namespace ViciOne.ServiceBus;
/// <summary>Immutable diagnostic-sensitivity metadata for one runtime message type.</summary>
public sealed record MessageSensitivityDescriptor
{
    /// <summary>Creates one validated immutable descriptor.</summary>
    /// <param name="payloadSensitivity">The payload sensitivity.</param>
    /// <param name="sensitiveMembers">The sensitive members.</param>
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

    /// <summary>Gets the payload sensitivity.</summary>
    public MessagePayloadSensitivity PayloadSensitivity { get; }

    /// <summary>Gets the sensitive members.</summary>
    public FrozenSet<string> SensitiveMembers { get; }

    /// <summary>Gets whether every payload member must be redacted.</summary>
    public bool IsSensitive => PayloadSensitivity == MessagePayloadSensitivity.Sensitive;

    /// <summary>Returns whether the named CLR member is sensitive.</summary>
    /// <param name="memberName">The member name.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsMemberSensitive(string memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        return SensitiveMembers.Contains(memberName);
    }
}
