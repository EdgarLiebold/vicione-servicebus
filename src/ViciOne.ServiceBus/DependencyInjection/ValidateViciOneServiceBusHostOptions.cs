using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines configuration options for validate vici one service bus host.
/// </summary>
public class ValidateViciOneServiceBusHostOptions :
    IValidateOptions<ViciOneServiceBusHostOptions>
{
    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public ValidateOptionsResult Validate(string? name, ViciOneServiceBusHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];
        AddPositiveTimeoutFailure(failures, options.StartTimeout, nameof(options.StartTimeout));
        AddPositiveTimeoutFailure(failures, options.StopTimeout, nameof(options.StopTimeout));
        AddPositiveTimeoutFailure(failures, options.ConsumerStopTimeout, nameof(options.ConsumerStopTimeout));

        if (options.StopTimeout.HasValue
            && options.ConsumerStopTimeout.HasValue
            && options.ConsumerStopTimeout > options.StopTimeout)
        {
            failures.Add(
                $"{nameof(options.ConsumerStopTimeout)} must be less than or equal to {nameof(options.StopTimeout)}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    static void AddPositiveTimeoutFailure(List<string> failures, TimeSpan? value, string optionName)
    {
        if (value <= TimeSpan.Zero)
            failures.Add($"{optionName} must be greater than {TimeSpan.Zero} when specified.");
    }
}
