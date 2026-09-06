using System;
using System.Security.Cryptography;
using System.Text;

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

        var name = $"{consumerTypeName}:{jobTypeName}:{queueName}";

        return name;
    }
}


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

        var name = $"{jobTypeName}:{jobName}";

        return name;
    }
}


internal static class JobIdentity
{
    /// <summary>Creates a deterministic identifier from the first 128 bits of a SHA-256 digest.</summary>
    /// <param name="key">The stable, namespace-qualified identity key.</param>
    /// <returns>The deterministic identifier encoded from the digest.</returns>
    public static Guid CreateDeterministicId(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }
}
