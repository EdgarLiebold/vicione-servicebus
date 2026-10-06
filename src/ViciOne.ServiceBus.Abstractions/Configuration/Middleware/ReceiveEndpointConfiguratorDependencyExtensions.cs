using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Links receive endpoints so startup and shutdown honor their dependency relationship.</summary>
public static class ReceiveEndpointConfiguratorDependencyExtensions
{
    /// <summary>Starts one endpoint only after another is ready and stops the dependency only after its dependent completes.</summary>
    /// <param name="connector">The endpoint that depends on another endpoint.</param>
    /// <param name="dependency">The endpoint that must become ready first and stop last.</param>
    public static void AddDependency(this IReceiveEndpointConfigurator connector, IReceiveEndpointConfigurator dependency)
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(dependency);

        var endpointDependency = new ReceiveEndpointDependency(dependency);
        try
        {
            connector.AddDependency(endpointDependency);
        }
        catch (Exception operationFailure)
        {
            try
            {
                endpointDependency.Disconnect();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Receive endpoint dependency admission and observer cleanup failed.", operationFailure, cleanupFailure);
            }
            throw;
        }
        dependency.AddDependent(new ReceiveEndpointDependent(connector));
    }


    sealed class ReceiveEndpointDependency :
        IReceiveEndpointDependency,
        IReceiveEndpointObserver
    {
        readonly ConnectHandle _handle;
        readonly TaskCompletionSource<ReceiveEndpointReady> _ready;

        public ReceiveEndpointDependency(IReceiveEndpointObserverConnector connector)
        {
            ArgumentNullException.ThrowIfNull(connector);

            _ready = new TaskCompletionSource<ReceiveEndpointReady>(TaskCreationOptions.RunContinuationsAsynchronously);

            _handle = connector.ConnectReceiveEndpointObserver(this)
                ?? throw new InvalidOperationException("The dependency endpoint returned no observer connection handle.");
        }

        public Task Ready => _ready.Task;

        public void Disconnect()
        {
            _handle.Disconnect();
        }

        Task IReceiveEndpointObserver.ReadyAsync(ReceiveEndpointReady ready)
        {
            ArgumentNullException.ThrowIfNull(ready);

            _handle.Disconnect();

            _ready.TrySetResult(ready);

            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.StoppingAsync(ReceiveEndpointStopping stopping)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.CompletedAsync(ReceiveEndpointCompleted completed)
        {
            ArgumentNullException.ThrowIfNull(completed);

            _handle.Disconnect();
            _ready.TrySetException(new InvalidOperationException("The dependency endpoint completed before it became ready."));

            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            ArgumentNullException.ThrowIfNull(faulted);

            if (faulted.IsTerminal)
            {
                _handle.Disconnect();
                _ready.TrySetException(faulted.Exception);
            }

            return Task.CompletedTask;
        }
    }


    sealed class ReceiveEndpointDependent :
        IReceiveEndpointDependent,
        IReceiveEndpointObserver
    {
        readonly TaskCompletionSource<ReceiveEndpointCompleted> _completed;
        readonly ConnectHandle _handle;

        public ReceiveEndpointDependent(IReceiveEndpointObserverConnector connector)
        {
            ArgumentNullException.ThrowIfNull(connector);

            _completed = new TaskCompletionSource<ReceiveEndpointCompleted>(TaskCreationOptions.RunContinuationsAsynchronously);

            _handle = connector.ConnectReceiveEndpointObserver(this)
                ?? throw new InvalidOperationException("The dependent endpoint returned no observer connection handle.");
        }

        public Task Completed => _completed.Task;

        Task IReceiveEndpointObserver.ReadyAsync(ReceiveEndpointReady ready)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.StoppingAsync(ReceiveEndpointStopping stopping)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.CompletedAsync(ReceiveEndpointCompleted completed)
        {
            ArgumentNullException.ThrowIfNull(completed);

            _handle.Disconnect();

            _completed.TrySetResult(completed);

            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            ArgumentNullException.ThrowIfNull(faulted);

            if (faulted.IsTerminal)
            {
                _handle.Disconnect();
                _completed.TrySetException(faulted.Exception);
            }

            return Task.CompletedTask;
        }
    }
}
