using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class TestReceiveEndpointObserver :
    IReceiveEndpointObserver
{
    readonly IPublishObserver _publishObserver;

    public TestReceiveEndpointObserver(IPublishObserver publishObserver)
    {
        _publishObserver = publishObserver ?? throw new ArgumentNullException(nameof(publishObserver));
    }

    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        ready.ReceiveEndpoint.ConnectPublishObserver(_publishObserver);

        return Task.CompletedTask;
    }

    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return Task.CompletedTask;
    }

    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return Task.CompletedTask;
    }

    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return Task.CompletedTask;
    }
}
