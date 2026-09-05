using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Monitoring;

/// <summary>
/// Defines configuration options for configure bus health check service.
/// </summary>
public sealed class ConfigureBusHealthCheckServiceOptions :
    IConfigureOptions<HealthCheckServiceOptions>
{
    readonly IServiceProvider _provider;
    readonly string[] _tags;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public ConfigureBusHealthCheckServiceOptions(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _tags = new[] { "ready", "vicione-servicebus" };
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="options">The options value.</param>
    public void Configure(HealthCheckServiceOptions options)
    {
        foreach (IBusCompositionRegistration busRegistration in _provider.GetServices<IBusCompositionRegistration>())
        {
            Type busType = busRegistration.BusType;
            var type = typeof(ViciOneServiceBusHealthCheckOptions<>).MakeGenericType(busType);
            var optionsType = typeof(IOptions<>).MakeGenericType(type);

            string name = HealthCheckName(busType);
            HealthStatus? minimalFailureStatus = HealthStatus.Unhealthy;
            var tags = new HashSet<string>(_tags, StringComparer.OrdinalIgnoreCase);

            var busOptions = _provider.GetService(optionsType);
            if (busOptions != null)
            {
                var healthCheckOptions = optionsType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)?.GetValue(busOptions, null)
                    as IHealthCheckOptions
                    ?? throw new InvalidOperationException($"Could not read health check options for bus instance '{name}'.");

                if (!string.IsNullOrWhiteSpace(healthCheckOptions.Name))
                    name = healthCheckOptions.Name;

                if (healthCheckOptions.MinimalFailureStatus.HasValue)
                    minimalFailureStatus = healthCheckOptions.MinimalFailureStatus.Value;

                if (healthCheckOptions.Tags.Any())
                    tags = healthCheckOptions.Tags;
            }

            options.Registrations.Add(new HealthCheckRegistration(
                name,
                provider => new BusHealthCheck(provider.GetServices<IBusInstance>()
                    .Single(instance => instance.InstanceType == busType)),
                minimalFailureStatus,
                tags));
        }
    }

    static string HealthCheckName(Type busType)
    {
        if (busType == typeof(IBus))
            return "vicione-servicebus-bus";

        string name = busType.Name;
        if (name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name[1..];
        return $"vicione-servicebus-{KebabCaseEndpointNameFormatter.Instance.SanitizeName(name)}";
    }
}
