using System;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Caches job metadata data.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TJob">The job type.</typeparam>
public static class JobMetadataCache<TConsumer, TJob>
    where TConsumer : class
    where TJob : class
{
    /// <summary>Generates job type id.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The guid produced by the operation.</returns>
    public static Guid GenerateJobTypeId(string? queueName)
    {
        var key = GenerateJobTypeName(queueName);

        return JobMetadataCache.GenerateHashGuid(key);
    }

    /// <summary>Generates job type name.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The string produced by the operation.</returns>
    public static string GenerateJobTypeName(string? queueName)
    {
        var consumerTypeName = TypeCache<TConsumer>.ShortName;
        var jobTypeName = TypeCache<TJob>.ShortName;

        var name = $"{consumerTypeName}:{jobTypeName}:{queueName}";

        return name;
    }
}


/// <summary>Caches job metadata data.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
public static class JobMetadataCache<TJob>
    where TJob : class
{
    /// <summary>Generates recurring job id.</summary>
    /// <param name="jobName">The job name.</param>
    /// <returns>The guid produced by the operation.</returns>
    public static Guid GenerateRecurringJobId(string jobName)
    {
        var key = GenerateJobTypeName(jobName);

        return JobMetadataCache.GenerateHashGuid(key);
    }

    /// <summary>Generates job type name.</summary>
    /// <param name="jobName">The job name.</param>
    /// <returns>The string produced by the operation.</returns>
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
