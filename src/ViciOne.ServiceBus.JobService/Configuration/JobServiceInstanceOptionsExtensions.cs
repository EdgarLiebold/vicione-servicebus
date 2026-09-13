using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides service-instance options contributed by the Job Service capability package.</summary>
public static class JobServiceInstanceOptionsExtensions
{
    /// <summary>Enables job-consumer discovery and local execution endpoints for the service instance.</summary>
    /// <param name="options">The service-instance options to update.</param>
    /// <returns>The supplied options for fluent configuration.</returns>
    public static ServiceInstanceOptions EnableJobServiceEndpoints(this ServiceInstanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Options<JobServiceOptions>();
        return options;
    }
}
