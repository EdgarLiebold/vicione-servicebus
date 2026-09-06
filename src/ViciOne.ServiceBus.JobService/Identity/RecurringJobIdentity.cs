using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Creates the stable identity of a named recurring job.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal static class RecurringJobIdentity<TJob>
    where TJob : class
{
    /// <summary>Creates the deterministic recurring-job identifier.</summary>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <returns>The deterministic recurring-job identifier.</returns>
    public static Guid CreateId(string jobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        var key = CreateName(jobName);

        return JobIdentity.CreateDeterministicId(key);
    }

    /// <summary>Creates the recurring job's diagnostic type and name pair.</summary>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <returns>The namespace-qualified job type and application name.</returns>
    public static string CreateName(string jobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        var jobTypeName = TypeCache<TJob>.ShortName;

        return $"{jobTypeName}:{jobName}";
    }
}
