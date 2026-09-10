using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Connects the harness publish observer to each ready receive endpoint.</summary>
internal sealed class TestReceiveEndpointObserver :
    IReceiveEndpointObserver
{
    readonly IPublishObserver _publishObserver;

    /// <summary>Creates an endpoint observer.</summary>
    /// <param name="publishObserver">The observer connected to ready endpoints.</param>
    public TestReceiveEndpointObserver(IPublishObserver publishObserver)
    {
        _publishObserver = publishObserver ?? throw new ArgumentNullException(nameof(publishObserver));
    }

    /// <summary>Connects publish observation to a ready receive endpoint.</summary>
    /// <param name="ready">The ready endpoint notification.</param>
    /// <returns>A completed task after the observer has been connected.</returns>
    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        ArgumentNullException.ThrowIfNull(ready);
        ready.ReceiveEndpoint.ConnectPublishObserver(_publishObserver);

        return Task.CompletedTask;
    }

    /// <summary>Accepts the endpoint stopping notification.</summary>
    /// <param name="stopping">The stopping endpoint notification.</param>
    /// <returns>A completed task.</returns>
    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return Task.CompletedTask;
    }

    /// <summary>Accepts the endpoint completion notification.</summary>
    /// <param name="completed">The completed endpoint notification.</param>
    /// <returns>A completed task.</returns>
    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return Task.CompletedTask;
    }

    /// <summary>Accepts the endpoint fault notification.</summary>
    /// <param name="faulted">The faulted endpoint notification.</param>
    /// <returns>A completed task.</returns>
    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return Task.CompletedTask;
    }
}
