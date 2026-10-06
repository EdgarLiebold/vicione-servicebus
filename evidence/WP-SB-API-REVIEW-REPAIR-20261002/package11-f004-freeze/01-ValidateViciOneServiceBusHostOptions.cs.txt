using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration options for validate vici one service bus host.</summary>
public sealed class ValidateViciOneServiceBusHostOptions :
    IValidateOptions<ViciOneServiceBusHostOptions>
{
    /// <summary>Validates the current configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The validation failures.</returns>
    public ValidateOptionsResult Validate(string? name, ViciOneServiceBusHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string bus = string.IsNullOrWhiteSpace(name) ? "all" : name;
        List<string> failures = [];
        AddPositiveTimeoutFailure(failures, options.StartTimeout, nameof(options.StartTimeout), bus);
        AddPositiveTimeoutFailure(failures, options.StopTimeout, nameof(options.StopTimeout), bus);
        AddPositiveTimeoutFailure(failures, options.ConsumerStopTimeout, nameof(options.ConsumerStopTimeout), bus);

        if (options.StopTimeout.HasValue
            && options.ConsumerStopTimeout.HasValue
            && options.ConsumerStopTimeout > options.StopTimeout)
        {
            failures.Add(
                $"Host lifecycle for bus '{bus}': {nameof(options.ConsumerStopTimeout)} ({options.ConsumerStopTimeout:c}) "
                + $"must be less than or equal to {nameof(options.StopTimeout)} ({options.StopTimeout:c}). "
                + $"Set {nameof(options.ConsumerStopTimeout)} to no more than {nameof(options.StopTimeout)}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    static void AddPositiveTimeoutFailure(List<string> failures, TimeSpan? value, string optionName, string bus)
    {
        if (value.HasValue && (long)value.Value.TotalMilliseconds > uint.MaxValue - 1L)
        {
            failures.Add(
                $"Host lifecycle for bus '{bus}': {optionName} ({value:c}) exceeds 4294967294 whole milliseconds. "
                + $"Set {optionName} to a positive duration supported by the .NET cancellation timer or leave it unset.");
        }
        else if (value <= TimeSpan.Zero)
        {
            failures.Add(
                $"Host lifecycle for bus '{bus}': {optionName} ({value:c}) must be greater than {TimeSpan.Zero:c} when specified. "
                + $"Set {optionName} to a positive duration or leave it unset.");
        }
    }
}
