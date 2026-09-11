using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Monitoring.Health;

/// <summary>Adds one .NET health-check registration for every configured bus instance.</summary>
internal sealed class ConfigureBusHealthCheckServiceOptions :
    IConfigureOptions<HealthCheckServiceOptions>
{
    readonly IServiceProvider _provider;
    readonly string[] _tags;

    /// <summary>Creates the options contributor from the application service provider.</summary>
    /// <param name="provider">The service provider containing bus registrations and their health options.</param>
    public ConfigureBusHealthCheckServiceOptions(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _tags = ["ready", "vicione-servicebus"];
    }

    /// <summary>Adds the health-check registrations represented by the configured buses.</summary>
    /// <param name="options">The .NET health-check options to extend.</param>
    public void Configure(HealthCheckServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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

                if (healthCheckOptions.Tags.Count > 0)
                    tags = new HashSet<string>(healthCheckOptions.Tags, StringComparer.OrdinalIgnoreCase);
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
