using System;

namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Absolute process-safety bounds for durable-send storage operations. Runtime options may choose lower values but
/// provider SPIs must never materialize an arbitrarily large claim or operations page from caller input.
/// </summary>
public static class DurableSendOperationLimits
{
    /// <summary>Gets the process-wide maximum number of records in one delivery claim.</summary>
    public const int AbsoluteMaximumClaimCount = 1024;

    /// <summary>Gets the process-wide maximum number of entries in one quarantine page.</summary>
    public const int AbsoluteMaximumQuarantinePageSize = 1000;

    /// <summary>Validates a requested delivery-claim count.</summary>
    /// <param name="value">The requested count.</param>
    /// <param name="parameterName">The public parameter name reported by a validation failure.</param>
    /// <returns>The validated count.</returns>
    /// <exception cref="ArgumentException"><paramref name="parameterName" /> is empty or contains only white-space characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value" /> is outside the published finite range.</exception>
    public static int ValidateClaimCount(int value, string parameterName)
        => Validate(value, AbsoluteMaximumClaimCount, parameterName, "claim count");

    /// <summary>Validates a requested quarantine page size.</summary>
    /// <param name="value">The requested page size.</param>
    /// <param name="parameterName">The public parameter name reported by a validation failure.</param>
    /// <returns>The validated page size.</returns>
    /// <exception cref="ArgumentException"><paramref name="parameterName" /> is empty or contains only white-space characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value" /> is outside the published finite range.</exception>
    public static int ValidateQuarantinePageSize(int value, string parameterName)
        => Validate(value, AbsoluteMaximumQuarantinePageSize, parameterName, "quarantine page size");

    static int Validate(int value, int maximum, string parameterName, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        if (value is < 1 || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName, value,
                $"Durable-send {description} must be between 1 and {maximum}.");

        return value;
    }
}
