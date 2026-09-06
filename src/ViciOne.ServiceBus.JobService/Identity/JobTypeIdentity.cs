using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Creates the stable identity of a job type at a specific endpoint.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TJob">The job type.</typeparam>
internal static class JobTypeIdentity<TConsumer, TJob>
    where TConsumer : class
    where TJob : class
{
    /// <summary>Creates the deterministic job-type identifier.</summary>
    /// <param name="queueName">The receive endpoint name that owns the job consumer.</param>
    /// <returns>The deterministic identifier.</returns>
    public static Guid CreateId(string queueName)
    {
        var key = CreateName(queueName);

        return JobIdentity.CreateDeterministicId(key);
    }

    /// <summary>Creates the diagnostic job-type name.</summary>
    /// <param name="queueName">The receive endpoint name that owns the job consumer.</param>
    /// <returns>The namespace-qualified consumer, job, and endpoint identity.</returns>
    public static string CreateName(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var consumerTypeName = TypeCache<TConsumer>.ShortName;
        var jobTypeName = TypeCache<TJob>.ShortName;

        return $"{consumerTypeName}:{jobTypeName}:{queueName}";
    }
}
