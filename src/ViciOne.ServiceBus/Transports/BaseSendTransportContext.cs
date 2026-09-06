using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Carries state for base send transport operations.</summary>
public abstract class BaseSendTransportContext :
    BasePipeContext,
    SendTransportContext
{
    readonly Lazy<string> _activityName;
    readonly Lazy<string> _destination;
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="serialization">The serialization.</param>
    protected BaseSendTransportContext(IHostConfiguration hostConfiguration, ISerialization serialization)
    {
        _hostConfiguration = hostConfiguration;

        SendObservers = new SendObservable();
        Serialization = serialization;

        _destination = new Lazy<string>(() =>
        {
            var endpointName = EntityName;

            if (endpointName.Contains("_bus_"))
                endpointName = "bus";
            else if (endpointName.Contains("_endpoint_"))
                endpointName = "endpoint";
            else if (endpointName.Contains("_signalr_"))
                endpointName = "signalr";
            else if (endpointName.StartsWith("Instance_"))
                endpointName = "instance";

            return endpointName;
        });

        _activityName = new Lazy<string>(() => $"{_destination.Value} send");
    }

    /// <summary>Gets the entity name.</summary>
    public abstract string EntityName { get; }

    /// <summary>Gets the log context.</summary>
    public ILogContext LogContext => _hostConfiguration.SendLogContext ?? throw new InvalidOperationException("SendLogContext should not be null");

    /// <summary>Gets the activity name.</summary>
    public string ActivityName => _activityName.Value;
    /// <summary>Gets the activity destination.</summary>
    public string ActivityDestination => _destination.Value;
    /// <summary>Gets the activity system.</summary>
    public abstract string ActivitySystem { get; }

    /// <summary>Gets the send observers.</summary>
    public SendObservable SendObservers { get; }

    /// <summary>Gets the serialization.</summary>
    public ISerialization Serialization { get; }

    internal void ApplyPayloadAdmission<T>(SendContext<T> context)
        where T : class
    {
        PayloadAdmissionTransportBoundary.Apply(_hostConfiguration, context);
    }

    /// <summary>Creates send context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return SendObservers.Connect(observer);
    }

    /// <summary>Gets agent handles.</summary>
    /// <returns>The agent handles.</returns>
    public virtual IEnumerable<IAgent> GetAgentHandles()
    {
        return [];
    }
}
