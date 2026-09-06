using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns registered bus instances.</summary>
public class BusDepot :
    IBusDepot
{
    readonly IDictionary<Type, IBusInstance> _instances;
    readonly ILogger<BusDepot> _logger;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="instances">The instances.</param>
    /// <param name="logger">The logger.</param>
    public BusDepot(IEnumerable<IBusInstance> instances, ILogger<BusDepot> logger)
    {
        _logger = logger;
        _instances = instances.ToDictionary(x => x.InstanceType);
    }

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_instances.Count == 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Depot", "unknown", "No bus instances were found. Ensure that AddViciOneServiceBus() is used to configure the transport.", "Correct the named configuration before starting the host"));

        _logger.LogDebug("Starting bus instances: {Instances}", string.Join(", ", _instances.Keys.Select(x => x.Name)));

        return Task.WhenAll(_instances.Values.Select(x => x.BusControl.StartAsync(cancellationToken)));
    }

    /// <summary>Stops the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_instances.Count == 0)
            return Task.CompletedTask;

        _logger.LogDebug("Stopping bus instances: {Instances}", string.Join(", ", _instances.Keys.Select(x => x.Name)));

        return Task.WhenAll(_instances.Values.Select(x => x.BusControl.StopAsync(cancellationToken)));
    }
}
