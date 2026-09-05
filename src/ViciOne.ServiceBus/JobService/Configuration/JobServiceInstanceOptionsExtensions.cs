using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides service-instance options contributed by the Job Service capability package.
/// </summary>
public static class JobServiceInstanceOptionsExtensions
{
    /// <summary>
    /// Enables the job service endpoints for the service instance.
    /// </summary>
    public static ServiceInstanceOptions EnableJobServiceEndpoints(this ServiceInstanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Options<JobServiceOptions>();
        return options;
    }
}
