using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IReceiveEndpointDispatcher :
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IDispatchMetrics,
    IReceiveObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    IProbeSite
{
    Uri InputAddress { get; }

    /// <summary>
    /// Handles the message based upon the endpoint configuration
    /// </summary>
    /// <param name="body">The message body</param>
    /// <param name="headers">The message headers</param>
    /// <param name="cancellationToken"></param>
    /// <param name="payloads">One or more payloads to add to the receive context</param>
    /// <returns></returns>
    Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default);

    // TODO convert this to use the MessageBody type for nicer integration, also MessageContext
}


public interface IReceiveEndpointDispatcher<T> :
    IReceiveEndpointDispatcher
    where T : class
{
}
