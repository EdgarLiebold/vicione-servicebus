using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Defines consumer-local delivery concurrency without changing endpoint transport QoS.</summary>
public sealed record ConsumerConcurrencyPolicy
{
    /// <summary>Gets the largest parallel or partition count accepted by a consumer policy.</summary>
    public const int AbsoluteMaximumConcurrency = 1024;

    private ConsumerConcurrencyPolicy(ConsumerConcurrencyMode mode, int concurrency)
    {
        Mode = mode;
        Concurrency = concurrency;
    }

    /// <summary>Gets the delivery-admission mode.</summary>
    public ConsumerConcurrencyMode Mode { get; }

    /// <summary>Gets the maximum number of concurrent deliveries or active partitions.</summary>
    public int Concurrency { get; }

    /// <summary>Creates a policy that admits independent deliveries concurrently.</summary>
    /// <param name="maximumConcurrency">The maximum number of concurrently admitted deliveries.</param>
    /// <returns>A parallel consumer-concurrency policy.</returns>
    public static ConsumerConcurrencyPolicy Parallel(int maximumConcurrency)
        => new(ConsumerConcurrencyMode.Parallel, ValidatePositive(maximumConcurrency, nameof(maximumConcurrency)));

    /// <summary>Gets the policy that admits one delivery at a time.</summary>
    public static ConsumerConcurrencyPolicy Serial { get; } = new(ConsumerConcurrencyMode.Serial, 1);

    /// <summary>Creates a policy that serializes deliveries within a fixed number of concurrent partitions.</summary>
    /// <param name="partitionCount">The number of partitions across which deliveries may run concurrently.</param>
    /// <returns>A partitioned consumer-concurrency policy.</returns>
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
