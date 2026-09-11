using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches serialized messages through one configured receive endpoint.</summary>
public interface IReceiveEndpointDispatcher :
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IDispatchMetrics,
    IReceiveObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    IProbeSite
{
    /// <summary>Gets the address represented by the dispatcher.</summary>
    Uri InputAddress { get; }

    /// <summary>Dispatches one serialized message through the configured receive pipeline.</summary>
    /// <param name="body">The message body.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="payloads">One or more payloads to add to the receive context.</param>
    /// <param name="cancellationToken">The token that cancels dispatch and lock settlement.</param>
    /// <returns>A task that completes after the message has been dispatched and settled.</returns>
    Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default);

}


/// <summary>Identifies a receive endpoint dispatcher by its registration type.</summary>
/// <typeparam name="T">The registered consumer, saga, or activity type.</typeparam>
public interface IReceiveEndpointDispatcher<T> :
    IReceiveEndpointDispatcher
    where T : class
{
}
