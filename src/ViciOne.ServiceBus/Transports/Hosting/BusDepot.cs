using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Transports;

internal sealed class BusDepot :
    IBusDepot
{
    readonly IReadOnlyDictionary<Type, IBusInstance> _instances;
    readonly ILogger<BusDepot> _logger;

    public BusDepot(IEnumerable<IBusInstance> instances, ILogger<BusDepot> logger)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(logger);

        var instancesByType = new Dictionary<Type, IBusInstance>();
        foreach (IBusInstance? instance in instances)
        {
            if (instance is null)
                throw new ArgumentException("The bus instance collection cannot contain null values.", nameof(instances));

            if (!instancesByType.TryAdd(instance.InstanceType, instance))
            {
                throw new ArgumentException(
                    $"Only one bus instance may represent the contract type '{instance.InstanceType.FullName}'.",
                    nameof(instances));
            }
        }

        _instances = instancesByType;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_instances.Count == 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Depot", "unknown", "No bus instances were found. Ensure that AddViciOneServiceBus() is used to configure the transport.", "Correct the named configuration before starting the host"));

        _logger.LogDebug("Starting bus instances: {Instances}", string.Join(", ", _instances.Keys.Select(x => x.Name)));

        return Task.WhenAll(_instances.Values.Select(StartInstanceAsync));

        async Task StartInstanceAsync(IBusInstance instance)
        {
            IBusControl control = instance.BusControl
                ?? throw new InvalidOperationException($"The bus instance '{instance.InstanceType}' returned no lifecycle control.");
            Task starting = control.StartAsync(cancellationToken)
                ?? throw new InvalidOperationException($"The bus instance '{instance.InstanceType}' returned no start task.");
            await starting.ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_instances.Count == 0)
            return Task.CompletedTask;

        _logger.LogDebug("Stopping bus instances: {Instances}", string.Join(", ", _instances.Keys.Select(x => x.Name)));

        return Task.WhenAll(_instances.Values.Select(StopInstanceAsync));

        async Task StopInstanceAsync(IBusInstance instance)
        {
            IBusControl control = instance.BusControl
                ?? throw new InvalidOperationException($"The bus instance '{instance.InstanceType}' returned no lifecycle control.");
            Task stopping = control.StopAsync(cancellationToken)
                ?? throw new InvalidOperationException($"The bus instance '{instance.InstanceType}' returned no stop task.");
            await stopping.ConfigureAwait(false);
        }
    }
}
