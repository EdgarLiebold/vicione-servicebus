using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

sealed class HealthResultReceiveEndpointObserver :
    IReceiveEndpointObserver
{
    readonly ReceiveEndpoint _endpoint;

    public HealthResultReceiveEndpointObserver(ReceiveEndpoint endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        ArgumentNullException.ThrowIfNull(ready);

        if (_endpoint.CurrentState is ReceiveEndpoint.State.Starting or ReceiveEndpoint.State.Faulted)
        {
            _endpoint.InputAddress = ready.InputAddress;
            _endpoint.Message = ready.IsStarted ? "ready" : "ready (not started)";
            _endpoint.HealthResult = EndpointHealthResult.Healthy(_endpoint, _endpoint.Message);

            LogContext.Debug?.Log("Endpoint Ready: {InputAddress}", _endpoint.InputAddress);

            _endpoint.CurrentState = ReceiveEndpoint.State.Ready;
        }

        return Task.CompletedTask;
    }

    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        ArgumentNullException.ThrowIfNull(stopping);

        if (_endpoint.CurrentState is ReceiveEndpoint.State.Starting
            or ReceiveEndpoint.State.Ready
            or ReceiveEndpoint.State.Paused
            or ReceiveEndpoint.State.Faulted)
        {
            _endpoint.Message = "stopping";
            _endpoint.HealthResult = EndpointHealthResult.Degraded(_endpoint, _endpoint.Message);
            _endpoint.CurrentState = ReceiveEndpoint.State.Stopping;

            LogContext.Debug?.Log("Endpoint Stopping: {InputAddress}", _endpoint.InputAddress);
        }

        return Task.CompletedTask;
    }

    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        ArgumentNullException.ThrowIfNull(completed);

        if (_endpoint.CurrentState is ReceiveEndpoint.State.Starting
            or ReceiveEndpoint.State.Ready
            or ReceiveEndpoint.State.Stopping
            or ReceiveEndpoint.State.Faulted)
        {
            _endpoint.InputAddress = completed.InputAddress;
            _endpoint.Message = $"stopped (delivered {completed.DeliveryCount} messages)";
            _endpoint.HealthResult = EndpointHealthResult.Degraded(_endpoint, _endpoint.Message);

            LogContext.Debug?.Log("Endpoint Completed: {InputAddress}", _endpoint.InputAddress);

            _endpoint.CurrentState = ReceiveEndpoint.State.Stopped;
        }

        return Task.CompletedTask;
    }

    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        ArgumentNullException.ThrowIfNull(faulted);

        if (_endpoint.CurrentState is ReceiveEndpoint.State.Starting
            or ReceiveEndpoint.State.Ready
            or ReceiveEndpoint.State.Stopping
            or ReceiveEndpoint.State.Stopped)
        {
            _endpoint.InputAddress = faulted.InputAddress;
            _endpoint.Message = $"faulted ({faulted.Exception.Message})";
            _endpoint.HealthResult = EndpointHealthResult.Unhealthy(_endpoint, _endpoint.Message, faulted.Exception);

            LogContext.Debug?.Log(faulted.Exception, "Endpoint Faulted: {InputAddress}", _endpoint.InputAddress);

            _endpoint.CurrentState = ReceiveEndpoint.State.Faulted;
        }

        return Task.CompletedTask;
    }
}
