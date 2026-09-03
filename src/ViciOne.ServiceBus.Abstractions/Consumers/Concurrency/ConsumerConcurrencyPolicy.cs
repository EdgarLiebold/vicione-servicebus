using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Consumer-local concurrency policy. It never changes endpoint transport QoS.
/// </summary>
public sealed record ConsumerConcurrencyPolicy
{
    public const int AbsoluteMaximumConcurrency = 1024;

    private ConsumerConcurrencyPolicy(ConsumerConcurrencyMode mode, int concurrency)
    {
        Mode = mode;
        Concurrency = concurrency;
    }

    public ConsumerConcurrencyMode Mode { get; }

    /// <summary>
    /// Gets the maximum parallel consumer invocations for <see cref="ConsumerConcurrencyMode.Parallel"/>, one for
    /// <see cref="ConsumerConcurrencyMode.Serial"/>, or the fixed partition count for
    /// <see cref="ConsumerConcurrencyMode.Partitioned"/>.
    /// </summary>
    public int Concurrency { get; }

    public static ConsumerConcurrencyPolicy Parallel(int maximumConcurrency)
        => new(ConsumerConcurrencyMode.Parallel, ValidatePositive(maximumConcurrency, nameof(maximumConcurrency)));

    public static ConsumerConcurrencyPolicy Serial { get; } = new(ConsumerConcurrencyMode.Serial, 1);

    public static ConsumerConcurrencyPolicy Partitioned(int partitionCount)
        => new(ConsumerConcurrencyMode.Partitioned, ValidatePositive(partitionCount, nameof(partitionCount)));

    private static int ValidatePositive(int value, string parameterName)
    {
        if (value is < 1 or > AbsoluteMaximumConcurrency)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Concurrency must be between 1 and {AbsoluteMaximumConcurrency}.");
        }

        return value;
    }
}
