using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Monitoring;

/// <summary>Projects one bus instance's aggregate transport health into the .NET health-check model.</summary>
internal sealed class BusHealthCheck :
    IHealthCheck
{
    readonly IBusInstance _busInstance;

    /// <summary>Creates a health check for one configured bus instance.</summary>
    /// <param name="busInstance">The bus instance whose control surface supplies health snapshots.</param>
    public BusHealthCheck(IBusInstance busInstance)
    {
        _busInstance = busInstance ?? throw new ArgumentNullException(nameof(busInstance));
    }

    /// <summary>Captures the current bus snapshot and maps it to a .NET health-check result.</summary>
    /// <param name="context">The registration context that defines the minimum reported failure status.</param>
    /// <param name="cancellationToken">Cancels the health observation before the snapshot is captured.</param>
    /// <returns>The completed health-check result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<HealthCheckResult>(cancellationToken);

        BusHealthResult result = _busInstance.BusControl.CheckHealth();

        var data = new Dictionary<string, object>
        {
            ["Endpoints"] = new EndpointDictionary(result.Endpoints.ToDictionary(x => x.Key,
                x => new Endpoint(Enum.GetName(typeof(BusHealthStatus), x.Value.Status), x.Value.Description)
            ))
        };

        var minimalHealthcheckLevel = context.Registration.FailureStatus switch
        {
            HealthStatus.Healthy => BusHealthStatus.Healthy,
            HealthStatus.Degraded => BusHealthStatus.Degraded,
            _ => BusHealthStatus.Unhealthy
        };

        var usedHealthcheckResult = result.Status < minimalHealthcheckLevel ? minimalHealthcheckLevel : result.Status;

        return Task.FromResult(usedHealthcheckResult switch
        {
            BusHealthStatus.Healthy => HealthCheckResult.Healthy(result.Description, data),
            BusHealthStatus.Degraded => HealthCheckResult.Degraded(result.Description, result.Exception, data),
            _ => HealthCheckResult.Unhealthy(result.Description, result.Exception, data)
        });
    }


    sealed class EndpointDictionary :
        Dictionary<string, Endpoint>
    {
        public EndpointDictionary(IDictionary<string, Endpoint> dictionary)
            : base(dictionary, StringComparer.OrdinalIgnoreCase)
        {
        }

        public override string ToString()
        {
            return string.Join(", ", this.Select(x => $"{x.Key}: {x.Value}"));
        }
    }


    sealed class Endpoint
    {
        public Endpoint(string? status, string? description)
        {
            Status = status;
            Description = description;
        }

        public string? Status { get; }
        public string? Description { get; }

        public override string ToString()
        {
            return $"{Status} - {Description}";
        }
    }
}
