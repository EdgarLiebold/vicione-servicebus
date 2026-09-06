using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes request id tee pipeline stages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class RequestIdTeeFilter<TMessage> :
    TeeFilter<ConsumeContext<TMessage>>,
    IRequestIdTeeFilter<TMessage>
    where TMessage : class
{
    readonly Lazy<IKeyPipeConnector<TMessage, Guid>> _keyConnections;

    /// <summary>Initializes a new instance.</summary>
    public RequestIdTeeFilter()
    {
        _keyConnections = new Lazy<IKeyPipeConnector<TMessage, Guid>>(ConnectKeyFilter);
    }

    /// <summary>Connects pipe.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
