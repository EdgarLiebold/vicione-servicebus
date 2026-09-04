using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Specifies the available validation result disposition values.
/// </summary>
[Serializable]
public enum ValidationResultDisposition
{
    /// <summary>
    /// Indicates success.
    /// </summary>
    Success,
    /// <summary>
    /// Indicates warning.
    /// </summary>
    Warning,
    /// <summary>
    /// Indicates failure.
    /// </summary>
    Failure,
}
