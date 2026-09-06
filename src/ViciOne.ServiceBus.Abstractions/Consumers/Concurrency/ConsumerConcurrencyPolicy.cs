using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Consumer-local concurrency policy. It never changes endpoint transport QoS.</summary>
public sealed record ConsumerConcurrencyPolicy
{
    /// <summary>Exposes the absolute maximum concurrency used by the containing type.</summary>
    public const int AbsoluteMaximumConcurrency = 1024;

    private ConsumerConcurrencyPolicy(ConsumerConcurrencyMode mode, int concurrency)
    {
        Mode = mode;
        Concurrency = concurrency;
    }

    /// <summary>Gets the mode.</summary>
    public ConsumerConcurrencyMode Mode { get; }

    /// <summary>Gets the concurrency.</summary>
    public int Concurrency { get; }

    /// <summary>Executes the configured branches concurrently.</summary>
    /// <param name="maximumConcurrency">The maximum concurrency.</param>
    /// <returns>The consumer concurrency policy produced by the operation.</returns>
    public static ConsumerConcurrencyPolicy Parallel(int maximumConcurrency)
        => new(ConsumerConcurrencyMode.Parallel, ValidatePositive(maximumConcurrency, nameof(maximumConcurrency)));

    /// <summary>Gets the serial.</summary>
    public static ConsumerConcurrencyPolicy Serial { get; } = new(ConsumerConcurrencyMode.Serial, 1);

    /// <summary>Executes the configured operation within its partition.</summary>
    /// <param name="partitionCount">The partition count.</param>
    /// <returns>The consumer concurrency policy produced by the operation.</returns>
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
