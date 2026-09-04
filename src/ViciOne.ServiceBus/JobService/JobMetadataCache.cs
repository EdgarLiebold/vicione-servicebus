using System;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a job metadata cache implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TJob">The t job type.</typeparam>
public static class JobMetadataCache<TConsumer, TJob>
    where TConsumer : class
    where TJob : class
{
    /// <summary>
    /// Performs the generate job type id operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static Guid GenerateJobTypeId(string? queueName)
    {
        var key = GenerateJobTypeName(queueName);

        return JobMetadataCache.GenerateHashGuid(key);
    }

    /// <summary>
    /// Performs the generate job type name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static string GenerateJobTypeName(string? queueName)
    {
        var consumerTypeName = TypeCache<TConsumer>.ShortName;
        var jobTypeName = TypeCache<TJob>.ShortName;

        var name = $"{consumerTypeName}:{jobTypeName}:{queueName}";

        return name;
    }
}


/// <summary>
/// Provides a job metadata cache implementation.
/// </summary>
/// <typeparam name="TJob">The t job type.</typeparam>
public static class JobMetadataCache<TJob>
    where TJob : class
{
    /// <summary>
    /// Performs the generate recurring job id operation.
    /// </summary>
    /// <param name="jobName">The job name value.</param>
    /// <returns>The result of the operation.</returns>
    public static Guid GenerateRecurringJobId(string jobName)
    {
        var key = GenerateJobTypeName(jobName);

        return JobMetadataCache.GenerateHashGuid(key);
    }

    /// <summary>
    /// Performs the generate job type name operation.
    /// </summary>
    /// <param name="jobName">The job name value.</param>
    /// <returns>The result of the operation.</returns>
    public static string GenerateJobTypeName(string jobName)
    {
        var jobTypeName = TypeCache<TJob>.ShortName;

        var name = $"{jobTypeName}:{jobName}";

        return name;
    }
}


static class JobMetadataCache
{
    static bool? _fipsMode;

    public static bool IsFipsMode =>
        CryptoConfig.AllowOnlyFipsAlgorithms ||
        (_fipsMode ??= bool.TryParse(Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_FIPS_ENABLE"), out var fipsMode) && fipsMode);

    public static Guid GenerateHashGuid(string key)
    {
        if (IsFipsMode)
        {
            using var hasher = SHA256.Create();

            var data = hasher.ComputeHash(Encoding.UTF8.GetBytes(key));

            return new Guid(new ReadOnlySpan<byte>(data, 0, 16).ToArray());
        }
        else
        {
            using var hasher = MD5.Create();

            var data = hasher.ComputeHash(Encoding.UTF8.GetBytes(key));

            return new Guid(data);
        }
    }
}
