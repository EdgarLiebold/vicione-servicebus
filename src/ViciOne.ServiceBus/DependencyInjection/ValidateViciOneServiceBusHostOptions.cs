using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public class ValidateViciOneServiceBusHostOptions :
    IValidateOptions<ViciOneServiceBusHostOptions>
{
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
