using System;
using Quartz;

namespace ViciOne.ServiceBus.QuartzIntegration;

internal readonly record struct QuartzHostedServiceSettings(TimeSpan? StartDelay, bool WaitForJobsToComplete)
{
    public static QuartzHostedServiceSettings From(QuartzHostedServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new QuartzHostedServiceSettings(options.StartDelay, options.WaitForJobsToComplete);
    }
}
