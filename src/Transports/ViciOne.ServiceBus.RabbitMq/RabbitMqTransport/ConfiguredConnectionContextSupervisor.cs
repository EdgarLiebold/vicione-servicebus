using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Applies one endpoint's connection middleware while borrowing its host supervisor.</summary>
internal sealed class ConfiguredConnectionContextSupervisor : IConnectionContextSupervisor
{
    readonly IConnectionContextSupervisor _parent;
    readonly Func<IPipe<ConnectionContext>, IPipe<ConnectionContext>> _connectionPipe;

    public ConfiguredConnectionContextSupervisor(IConnectionContextSupervisor parent,
        Func<IPipe<ConnectionContext>, IPipe<ConnectionContext>> connectionPipe)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _connectionPipe = connectionPipe ?? throw new ArgumentNullException(nameof(connectionPipe));
    }

    public Task Ready => _parent.Ready;
    public Task Completed => _parent.Completed;
    public CancellationToken Stopping => _parent.Stopping;
    public CancellationToken Stopped => _parent.Stopped;
    public CancellationToken ConsumeStopping => _parent.ConsumeStopping;
    public CancellationToken SendStopping => _parent.SendStopping;
    public int PeakActiveCount => _parent.PeakActiveCount;
    public long TotalCount => _parent.TotalCount;

    public void Add(IAgent agent) => _parent.Add(agent);
    public void AddConsumeAgent<TAgent>(TAgent agent) where TAgent : IAgent => _parent.AddConsumeAgent(agent);
    public void AddSendAgent<TAgent>(TAgent agent) where TAgent : IAgent => _parent.AddSendAgent(agent);
    public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        => _parent.StopAsync(context, cancellationToken);
    public void Probe(ProbeContext context) => _parent.Probe(context);
    public Uri NormalizeAddress(Uri address) => _parent.NormalizeAddress(address);

    public Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext context,
        IChannelContextSupervisor channelSupervisor, Uri address, CancellationToken cancellationToken = default)
        => _parent.CreateSendTransportAsync(context, channelSupervisor, address, cancellationToken);

    public Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext context,
        IChannelContextSupervisor channelSupervisor, CancellationToken cancellationToken = default) where T : class
        => _parent.CreatePublishTransportAsync<T>(context, channelSupervisor, cancellationToken);

    public Task SendAsync(IPipe<ConnectionContext> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _parent.SendAsync(new InvocationPipe(_connectionPipe, pipe), cancellationToken);
    }

    sealed class InvocationPipe(Func<IPipe<ConnectionContext>, IPipe<ConnectionContext>> compose, IPipe<ConnectionContext> next)
        : IPipe<ConnectionContext>
    {
        public async Task SendAsync(ConnectionContext context)
        {
            var terminal = new Continuation(next);
            Exception? pipelineFailure = null;
            try
            {
                await compose(terminal).SendAsync(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                pipelineFailure = exception;
            }

            bool entered = terminal.CloseAndWasEntered();
            Exception? continuationFailure = null;
            Task? admitted = null;
            if (entered)
            {
                admitted = await terminal.Admitted.ConfigureAwait(false);
                try
                {
                    // Keep the borrowed connection alive even when custom middleware does not await next.
                    await admitted.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    continuationFailure = exception;
                }
            }

            if (pipelineFailure is not null)
            {
                bool sameCancellation = admitted is { IsCanceled: true }
                    && pipelineFailure is TaskCanceledException pipelineCancellation
                    && continuationFailure is TaskCanceledException continuationCancellation
                    && ReferenceEquals(pipelineCancellation.Task, admitted)
                    && ReferenceEquals(continuationCancellation.Task, admitted);
                if (continuationFailure is not null && !ReferenceEquals(pipelineFailure, continuationFailure) && !sameCancellation)
                    throw new AggregateException(pipelineFailure, continuationFailure);

                ExceptionDispatchInfo.Capture(pipelineFailure).Throw();
            }

            if (continuationFailure is not null)
                ExceptionDispatchInfo.Capture(continuationFailure).Throw();

            if (!entered)
                throw new InvalidOperationException("The configured RabbitMQ connection pipeline did not invoke its continuation.");
        }

        public void Probe(ProbeContext context) => next.Probe(context);
    }

    sealed class Continuation(IPipe<ConnectionContext> next) : IPipe<ConnectionContext>
    {
        readonly TaskCompletionSource<Task> _admitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _entered;

        public Task<Task> Admitted => _admitted.Task;

        public bool CloseAndWasEntered() => Interlocked.CompareExchange(ref _entered, 2, 0) == 1;

        public Task SendAsync(ConnectionContext context)
        {
            int previous = Interlocked.CompareExchange(ref _entered, 1, 0);
            if (previous != 0)
                return Task.FromException(new InvalidOperationException(previous == 2
                    ? "The configured RabbitMQ connection pipeline invoked its continuation after completing."
                    : "The configured RabbitMQ connection pipeline invoked its continuation more than once."));

            Task operation;
            try
            {
                operation = next.SendAsync(context)
                    ?? Task.FromException(new InvalidOperationException("The RabbitMQ connection continuation returned a null task."));
            }
            catch (Exception exception)
            {
                operation = Task.FromException(exception);
            }

            _admitted.TrySetResult(operation);
            return operation;
        }

        public void Probe(ProbeContext context) => next.Probe(context);
    }
}
