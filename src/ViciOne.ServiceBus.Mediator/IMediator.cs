using System;

namespace ViciOne.ServiceBus.Mediator;

/// <summary>Dispatches commands, events, and requests to handlers within the current process.</summary>
public interface IMediator :
    ISendEndpoint,
    IPublishEndpoint,
    IPublishEndpointProvider,
    IClientFactory,
    IConsumePipeConnector,
    IRequestPipeConnector,
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IAsyncDisposable
{
}
