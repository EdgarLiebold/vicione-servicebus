using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes test receive endpoint events.</summary>
public class TestReceiveEndpointObserver :
    IReceiveEndpointObserver
{
    readonly IPublishObserver _publishObserver;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishObserver">The publish observer.</param>
    public TestReceiveEndpointObserver(IPublishObserver publishObserver)
    {
        _publishObserver = publishObserver ?? throw new ArgumentNullException(nameof(publishObserver));
    }

    /// <summary>Reports that the component is ready.</summary>
    /// <param name="ready">The ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        ready.ReceiveEndpoint.ConnectPublishObserver(_publishObserver);

        return Task.CompletedTask;
    }

    /// <summary>Stops ping.</summary>
    /// <param name="stopping">The stopping.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return Task.CompletedTask;
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="completed">The completed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return Task.CompletedTask;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="faulted">The faulted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return Task.CompletedTask;
    }
}
