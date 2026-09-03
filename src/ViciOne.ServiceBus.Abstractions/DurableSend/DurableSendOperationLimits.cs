using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Absolute process-safety bounds for durable-send storage operations. Runtime options may choose lower values but
/// provider SPIs must never materialize an arbitrarily large claim or operations page from caller input.
/// </summary>
public static class DurableSendOperationLimits
{
    public const int AbsoluteMaximumClaimCount = 1024;
    public const int AbsoluteMaximumQuarantinePageSize = 1000;

    public static int ValidateClaimCount(int value, string parameterName)
        => Validate(value, AbsoluteMaximumClaimCount, parameterName, "claim count");

    public static int ValidateQuarantinePageSize(int value, string parameterName)
        => Validate(value, AbsoluteMaximumQuarantinePageSize, parameterName, "quarantine page size");

    static int Validate(int value, int maximum, string parameterName, string description)
    {
        if (value is < 1 || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName, value,
                $"Durable-send {description} must be between 1 and {maximum}.");

        return value;
    }
}
