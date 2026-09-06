using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by receive endpoint dispatcher.</summary>
public interface IReceiveEndpointDispatcher :
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IDispatchMetrics,
    IReceiveObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    IProbeSite
{
    /// <summary>Gets the input address.</summary>
    Uri InputAddress { get; }

    /// <summary>Handles the message based upon the endpoint configuration.</summary>
    /// <param name="body">The message body.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="payloads">One or more payloads to add to the receive context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default);

}


/// <summary>Defines the operations required by receive endpoint dispatcher.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReceiveEndpointDispatcher<T> :
    IReceiveEndpointDispatcher
    where T : class
{
}
