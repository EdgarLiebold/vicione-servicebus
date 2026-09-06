using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Monitoring;

/// <summary>Evaluates the health of bus health.</summary>
public class BusHealthCheck :
    IHealthCheck
{
    readonly IBusInstance _busInstance;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="busInstance">The bus instance.</param>
    public BusHealthCheck(IBusInstance busInstance)
    {
        _busInstance = busInstance;
    }

    /// <summary>Checks health.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the check health outcome.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult>(cancellationToken); var result = _busInstance.BusControl.CheckHealth();

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


    class EndpointDictionary :
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


    class Endpoint
    {
        public Endpoint(string? status, string? description)
        {
            Status = status;
            Description = description;
        }

        public string? Status { get; set; }
        public string? Description { get; set; }
        public override string ToString()
        {
            return $"{Status} - {Description}";
        }
    }
}
