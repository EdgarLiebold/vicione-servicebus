using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a base send transport context implementation.
/// </summary>
public abstract class BaseSendTransportContext :
    BasePipeContext,
    SendTransportContext
{
    readonly Lazy<string> _activityName;
    readonly Lazy<string> _destination;
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="serialization">The serialization value.</param>
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

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public abstract string EntityName { get; }

    /// <summary>
    /// Gets the log context value.
    /// </summary>
    public ILogContext LogContext => _hostConfiguration.SendLogContext ?? throw new InvalidOperationException("SendLogContext should not be null");

    /// <summary>
    /// Gets the activity name value.
    /// </summary>
    public string ActivityName => _activityName.Value;
    /// <summary>
    /// Gets the activity destination value.
    /// </summary>
    public string ActivityDestination => _destination.Value;
    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    public abstract string ActivitySystem { get; }

    /// <summary>
    /// Gets the send observers value.
    /// </summary>
    public SendObservable SendObservers { get; }

    /// <summary>
    /// Gets the serialization value.
    /// </summary>
    public ISerialization Serialization { get; }

    internal void ApplyPayloadAdmission<T>(SendContext<T> context)
        where T : class
    {
        PayloadAdmissionTransportBoundary.Apply(_hostConfiguration, context);
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return SendObservers.Connect(observer);
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<IAgent> GetAgentHandles()
    {
        return [];
    }
}
