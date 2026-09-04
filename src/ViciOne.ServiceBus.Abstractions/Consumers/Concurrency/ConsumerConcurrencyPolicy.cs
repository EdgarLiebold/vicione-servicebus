using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Consumer-local concurrency policy. It never changes endpoint transport QoS.
/// </summary>
public sealed record ConsumerConcurrencyPolicy
{
    /// <summary>
    /// Defines the absolute maximum concurrency value.
    /// </summary>
    public const int AbsoluteMaximumConcurrency = 1024;

    private ConsumerConcurrencyPolicy(ConsumerConcurrencyMode mode, int concurrency)
    {
        Mode = mode;
        Concurrency = concurrency;
    }

    /// <summary>
    /// Gets the mode value.
    /// </summary>
    public ConsumerConcurrencyMode Mode { get; }

    /// <summary>
    /// Gets the maximum parallel consumer invocations for <see cref="ConsumerConcurrencyMode.Parallel"/>, one for
    /// <see cref="ConsumerConcurrencyMode.Serial"/>, or the fixed partition count for
    /// <see cref="ConsumerConcurrencyMode.Partitioned"/>.
    /// </summary>
    public int Concurrency { get; }

    /// <summary>
    /// Performs the parallel operation.
    /// </summary>
    /// <param name="maximumConcurrency">The maximum concurrency value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerConcurrencyPolicy Parallel(int maximumConcurrency)
        => new(ConsumerConcurrencyMode.Parallel, ValidatePositive(maximumConcurrency, nameof(maximumConcurrency)));

    /// <summary>
    /// Gets the serial value.
    /// </summary>
    public static ConsumerConcurrencyPolicy Serial { get; } = new(ConsumerConcurrencyMode.Serial, 1);

    /// <summary>
    /// Performs the partitioned operation.
    /// </summary>
    /// <param name="partitionCount">The partition count value.</param>
    /// <returns>The result of the operation.</returns>
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
