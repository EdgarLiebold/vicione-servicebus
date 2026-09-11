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

/// <summary>Provides serialization, diagnostics, observers, and payload admission for a send transport.</summary>
public abstract class BaseSendTransportContext :
    BasePipeContext,
    SendTransportContext
{
    readonly Lazy<string> _activityName;
    readonly Lazy<string> _destination;
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>Initializes the context from its host configuration and serialization registry.</summary>
    /// <param name="hostConfiguration">The configuration that owns transport diagnostics and admission limits.</param>
    /// <param name="serialization">The serializers available to the send transport.</param>
    protected BaseSendTransportContext(IHostConfiguration hostConfiguration, ISerialization serialization)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));

        SendObservers = new SendObservable();
        Serialization = serialization ?? throw new ArgumentNullException(nameof(serialization));

        _destination = new Lazy<string>(() =>
        {
            var endpointName = EntityName;
            if (string.IsNullOrWhiteSpace(endpointName))
                throw new InvalidOperationException("The send transport context returned an empty entity name.");

            if (endpointName.Contains("_bus_", StringComparison.Ordinal))
                endpointName = "bus";
            else if (endpointName.Contains("_endpoint_", StringComparison.Ordinal))
                endpointName = "endpoint";
            else if (endpointName.Contains("_signalr_", StringComparison.Ordinal))
                endpointName = "signalr";
            else if (endpointName.StartsWith("Instance_", StringComparison.Ordinal))
                endpointName = "instance";

            return endpointName;
        });

        _activityName = new Lazy<string>(() => $"{_destination.Value} send");
    }

    /// <summary>Gets the transport entity that receives sent messages.</summary>
    public abstract string EntityName { get; }

    /// <summary>Gets the diagnostic context dedicated to send operations.</summary>
    public ILogContext LogContext => _hostConfiguration.SendLogContext
        ?? throw new InvalidOperationException("The host configuration returned no send log context.");

    /// <summary>Gets the tracing activity name for this destination.</summary>
    public string ActivityName => _activityName.Value;
    /// <summary>Gets the normalized tracing destination name.</summary>
    public string ActivityDestination => _destination.Value;
    /// <summary>Gets the messaging-system identifier used by tracing.</summary>
    public abstract string ActivitySystem { get; }

    /// <summary>Gets the observers notified around physical sends.</summary>
    public SendObservable SendObservers { get; }

    /// <summary>Gets the serialization registry available to the transport.</summary>
    public ISerialization Serialization { get; }

    internal void ApplyPayloadAdmission<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        PayloadAdmissionTransportBoundary.Apply(_hostConfiguration, context);
    }

    /// <summary>Creates a transport-ready send context without dispatching the message.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message represented by the context.</param>
    /// <param name="pipe">The pipeline that configures the send context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public abstract Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Connects an observer to physical send operations.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return SendObservers.Connect(observer);
    }

    /// <summary>Gets transport agents whose lifetime is owned by the send transport.</summary>
    /// <returns>The owned transport agents, or an empty sequence.</returns>
    public virtual IEnumerable<IAgent> GetAgentHandles()
    {
        return [];
    }
}
