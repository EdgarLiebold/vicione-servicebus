using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a request id tee filter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RequestIdTeeFilter<TMessage> :
    TeeFilter<ConsumeContext<TMessage>>,
    IRequestIdTeeFilter<TMessage>
    where TMessage : class
{
    readonly Lazy<IKeyPipeConnector<TMessage, Guid>> _keyConnections;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RequestIdTeeFilter()
    {
        _keyConnections = new Lazy<IKeyPipeConnector<TMessage, Guid>>(ConnectKeyFilter);
    }

    /// <summary>
    /// Connects pipe.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPipe(Guid key, IPipe<ConsumeContext<TMessage>> pipe)
    {
        return _keyConnections.Value.ConnectPipe(key, pipe);
    }

    RequestIdFilter<TMessage> ConnectKeyFilter()
    {
        var filter = new RequestIdFilter<TMessage>();

        ConnectPipe(filter.ToPipe());

        return filter;
    }
}
