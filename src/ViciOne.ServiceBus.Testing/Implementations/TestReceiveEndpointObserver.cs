using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a test receive endpoint observer implementation.
/// </summary>
public class TestReceiveEndpointObserver :
    IReceiveEndpointObserver
{
    readonly IPublishObserver _publishObserver;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishObserver">The publish observer value.</param>
    public TestReceiveEndpointObserver(IPublishObserver publishObserver)
    {
        _publishObserver = publishObserver ?? throw new ArgumentNullException(nameof(publishObserver));
    }

    /// <summary>
    /// Performs the ready operation.
    /// </summary>
    /// <param name="ready">The ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        ready.ReceiveEndpoint.ConnectPublishObserver(_publishObserver);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops ping.
    /// </summary>
    /// <param name="stopping">The stopping value.</param>
    /// <returns>The result of the operation.</returns>
    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <param name="completed">The completed value.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="faulted">The faulted value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return Task.CompletedTask;
    }
}
