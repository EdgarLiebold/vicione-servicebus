using System;
using System.Linq;

namespace ViciOne.ServiceBus.Batching.Runtime;

/// <summary>Captures the validated batch limits used by one connected runtime pipeline.</summary>
internal sealed class BatchRuntimeSettings
{
    /// <summary>Copies the effective values from a validated batch configuration.</summary>
    /// <param name="options">The configuration snapshot source.</param>
    public BatchRuntimeSettings(BatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidationResult[] failures = options.Validate()
            .Where(static result => result.Disposition == ValidationResultDisposition.Failure)
            .ToArray();
        if (failures.Length > 0)
        {
            throw new ArgumentException(
                $"Batch options must be valid before runtime use: {string.Join("; ", failures.Select(static failure => failure.ToString()))}",
                nameof(options));
        }

        MessageLimit = options.MessageLimit;
        ConcurrencyLimit = options.ConcurrencyLimit;
        TimeLimit = options.TimeLimit;
        TimeLimitStart = options.TimeLimitStart;
    }

    /// <summary>Gets the maximum number of messages in one batch.</summary>
    public int MessageLimit { get; }

    /// <summary>Gets the maximum number of completed batches delivered concurrently.</summary>
    public int ConcurrencyLimit { get; }

    /// <summary>Gets the maximum collection interval for a partial batch.</summary>
    public TimeSpan TimeLimit { get; }

    /// <summary>Gets the message arrival from which the collection interval is measured.</summary>
    public BatchTimeLimitStart TimeLimitStart { get; }
}
