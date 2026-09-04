using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Monitoring;

/// <summary>
/// Defines configuration options for configure bus health check service.
/// </summary>
public class ConfigureBusHealthCheckServiceOptions :
    IConfigureOptions<HealthCheckServiceOptions>
{
    readonly IEnumerable<IBusInstance> _busInstances;
    readonly IServiceProvider _provider;
    readonly string[] _tags;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busInstances">The bus instances value.</param>
    /// <param name="provider">The service provider.</param>
    public ConfigureBusHealthCheckServiceOptions(IEnumerable<IBusInstance> busInstances, IServiceProvider provider)
    {
        _busInstances = busInstances;
        _provider = provider;
        _tags = new[] { "ready", "vicione-servicebus" };
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="options">The options value.</param>
    public void Configure(HealthCheckServiceOptions options)
    {
        foreach (var busInstance in _busInstances)
        {
            var type = typeof(ViciOneServiceBusHealthCheckOptions<>).MakeGenericType(busInstance.InstanceType);
            var optionsType = typeof(IOptions<>).MakeGenericType(type);

            var name = busInstance.Name;
            HealthStatus? minimalFailureStatus = HealthStatus.Unhealthy;
            var tags = new HashSet<string>(_tags, StringComparer.OrdinalIgnoreCase);

            var busOptions = _provider.GetService(optionsType);
            if (busOptions != null)
            {
                var healthCheckOptions = optionsType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)?.GetValue(busOptions, null)
                    as IHealthCheckOptions
                    ?? throw new InvalidOperationException($"Could not read health check options for bus instance '{busInstance.Name}'.");

                if (!string.IsNullOrWhiteSpace(healthCheckOptions.Name))
                    name = healthCheckOptions.Name;

                if (healthCheckOptions.MinimalFailureStatus.HasValue)
                    minimalFailureStatus = healthCheckOptions.MinimalFailureStatus.Value;

                if (healthCheckOptions.Tags.Any())
                    tags = healthCheckOptions.Tags;
            }

            options.Registrations.Add(new HealthCheckRegistration(name, new BusHealthCheck(busInstance), minimalFailureStatus, tags));
        }
    }
}
