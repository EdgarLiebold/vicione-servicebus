namespace ViciOne.ServiceBus.Diagnostics;

/// <summary>Controls whether ServiceBus diagnostics may render individual safe values.</summary>
public enum MessagePayloadSensitivity
{
    /// <summary>Only explicitly sensitive members are redacted.</summary>
    Normal = 0,

    /// <summary>Every payload member is redacted.</summary>
    Sensitive = 1,
}
