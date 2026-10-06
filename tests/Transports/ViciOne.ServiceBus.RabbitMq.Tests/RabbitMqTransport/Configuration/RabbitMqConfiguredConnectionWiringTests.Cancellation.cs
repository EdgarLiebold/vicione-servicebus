using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed partial class RabbitMqConfiguredConnectionWiringTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONFIGURED-CONNECTION-WIRING",
        "late-created-channel-cancellation-and-independent-same-token-causes")]
    public async Task ConfigureConnection_LateUnpublishedChannelPreservesCancellationCausesAsync(bool independentPipelineFailure)
    {
        ILogContext? previous = LogContext.Current;
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var pipelineCause = new OperationCanceledException("Unique independent configured-pipeline cancellation", CancellationToken.None);
        var disposalCause = new OperationCanceledException("Unique independent late-channel-disposal cancellation", CancellationToken.None);
        Task completion = independentPipelineFailure ? Task.FromException(disposalCause) : Task.FromCanceled(canceled.Token);
        var filter = new CancellationBoundaryFilter(independentPipelineFailure, pipelineCause);
        RecordingConnectionContext? raw = null;
        GenerationBorrowedConnectionSupervisor? parent = null;
        IChannelContextSupervisor? channels = null;
        var delivery = new HeldPublicChannelPipe();
        Task? operation = null;
        Task? parentOperation = null;
        Task? stop = null;
        bool operationObserved = false;
        bool parentObserved = false;
        bool admittedObserved = false;
        Exception? operationFailure = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            var bus = new RabbitMqBusConfiguration(new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology()));
            IRabbitMqHostConfiguration actualHost = bus.HostConfiguration;
            actualHost.LogContext = LogContext.Current;
            raw = new RecordingConnectionContext(actualHost);
            raw.ChannelDisposal.Completion = () => new ValueTask(completion);
            parent = new GenerationBorrowedConnectionSupervisor(new ConnectionFixtureFactory(raw, actualHost));
            IRabbitMqHostConfiguration host = DispatchProxy.Create<IRabbitMqHostConfiguration, HostConfigurationProxy>();
            var proxy = (HostConfigurationProxy)(object)host;
            proxy.Inner = actualHost;
            proxy.Supervisor = parent;
            IRabbitMqEndpointConfiguration child = bus.CreateEndpointConfiguration(false);
            child.AutoStart = false;
            var settings = new RabbitMqReceiveSettings(child, "public-late-channel-cancellation", ExchangeType.Fanout, true, false);
            var endpoint = new RabbitMqReceiveEndpointConfiguration(host, settings, child);
            endpoint.ConfigureConnection(c => c.UseFilter(filter));
            RabbitMqReceiveEndpointContext runtime = Assert.IsAssignableFrom<RabbitMqReceiveEndpointContext>(endpoint.CreateReceiveEndpointContext());
            channels = runtime.ChannelContextSupervisor;
            operation = channels.SendAsync(delivery, TestContext.Current.CancellationToken);
            await raw.CreateEntered.Task.WaitAsync(bound.Token);
            parentOperation = await parent.GetRecordedAsync(0).WaitAsync(bound.Token);
            Assert.Equal(1, filter.Calls);
            Assert.Equal(1, parent.SendCalls);
            Assert.Equal(1, raw.CreateCalls);
            Assert.Equal(0, delivery.Calls);
            Assert.Equal(0, raw.ChannelDisposal.DisposeCalls);
            Assert.False(raw.CreationTask!.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.False(parentOperation.IsCompleted);

            stop = channels.StopAsync("stop unpublished owned channel agent", CancellationToken.None);
            await stop.WaitAsync(bound.Token);
            await channels.Completed.WaitAsync(bound.Token);
            Exception? callerCancellation = await Record.ExceptionAsync(() => operation.WaitAsync(bound.Token));
            operationObserved = operation.IsCompleted;
            Assert.True(operationObserved);
            Assert.IsAssignableFrom<OperationCanceledException>(callerCancellation);
            Assert.Equal(0, delivery.Calls);
            Assert.True(raw.CreateToken.IsCancellationRequested);
            Assert.False(raw.CreationTask.IsCompleted);
            Assert.False(parentOperation.IsCompleted);

            raw.CreateRelease.TrySetResult();
            await raw.ChannelDisposal.Entered.Task.WaitAsync(bound.Token);
            Assert.Equal(1, raw.ChannelDisposal.DisposeCalls);
            Assert.Equal(0, delivery.Calls);
            Assert.False(parentOperation.IsCompleted);
            raw.ChannelDisposal.Release.TrySetResult();
            Exception? parentFailure = await Record.ExceptionAsync(() => parentOperation.WaitAsync(bound.Token));
            parentObserved = parentOperation.IsCompleted;
            Assert.True(parentObserved);
            Assert.NotNull(filter.Admitted);
            Exception? nextFailure = await Record.ExceptionAsync(() => filter.Admitted!.WaitAsync(bound.Token));
            admittedObserved = filter.Admitted!.IsCompleted;
            Assert.True(admittedObserved);
            Assert.True(filter.Admitted.IsCanceled);
            if (independentPipelineFailure)
            {
                Assert.True(completion.IsFaulted);
                AggregateException combined = Assert.IsType<AggregateException>(parentFailure);
                Assert.Collection(combined.InnerExceptions,
                    first => Assert.Same(pipelineCause, first),
                    second => Assert.Same(disposalCause, second));
                Assert.Same(disposalCause, nextFailure);
                Assert.Equal(pipelineCause.CancellationToken, disposalCause.CancellationToken);
            }
            else
            {
                Assert.True(completion.IsCanceled);
                Assert.True(parentOperation.IsCanceled);
                Assert.Same(filter.ObservedCancellation, parentFailure);
                Assert.Same(nextFailure, parentFailure);
                Assert.Equal(canceled.Token, Assert.IsAssignableFrom<OperationCanceledException>(parentFailure).CancellationToken);
            }
            Assert.True(raw.CreationTask.IsCompletedSuccessfully);
            Assert.Equal(1, raw.ChannelDisposal.DisposeCalls);
            Assert.Equal(1, raw.CreateCalls);
            Assert.Equal(0, delivery.Calls);
        }
        catch (Exception failure)
        {
            operationFailure = failure;
            throw;
        }
        finally
        {
            raw?.CreateRelease.TrySetResult();
            raw?.ChannelDisposal.Release.TrySetResult();
            delivery.Release.TrySetResult();
            var failures = new List<Exception>();
            async Task ObserveAsync(Func<Task> action)
            {
                using var fresh = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try { await action().WaitAsync(fresh.Token); }
                catch (Exception failure) { failures.Add(failure); }
            }
            try
            {
                if (channels is not null)
                {
                    await ObserveAsync(() => stop ?? channels.StopAsync("late fixture final channel drain", CancellationToken.None));
                    await ObserveAsync(() => channels.Completed);
                }
                if (operation is not null && !operationObserved) await ObserveAsync(() => operation);
                if (parent is not null)
                {
                    await ObserveAsync(() => parent.StopAsync("late fixture parent owner drain", CancellationToken.None));
                    await ObserveAsync(() => parent.Completed);
                    foreach (Task owned in parent.SendTasks)
                        if (!parentObserved || !ReferenceEquals(owned, parentOperation)) await ObserveAsync(() => owned);
                }
                if (raw?.CreationTask is { } creation) await ObserveAsync(() => creation);
                if (raw?.CreationAgent is { } agent) await ObserveAsync(() => agent.Completed);
                if (filter.Admitted is { } admitted && !admittedObserved) await ObserveAsync(() => admitted);
                if (raw is not null)
                    await ObserveAsync(() =>
                    {
                        Assert.Equal(1, raw.DisposeCalls);
                        return Task.CompletedTask;
                    });
            }
            finally { LogContext.Current = previous; }
            if (failures.Count > 0)
            {
                if (operationFailure is not null) failures.Insert(0, operationFailure);
                if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
                throw new AggregateException("Public late-creation test or owned cleanup failed.", failures);
            }
        }
    }

    private sealed class CancellationBoundaryFilter(bool independentFailure, OperationCanceledException primary)
        : IFilter<ConnectionContext>
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public Task? Admitted { get; private set; }
        public OperationCanceledException? ObservedCancellation { get; private set; }
        public async Task SendAsync(ConnectionContext context, IPipe<ConnectionContext> next)
        {
            Interlocked.Increment(ref _calls);
            Admitted = next.SendAsync(context);
            if (independentFailure) throw primary;
            try { await Admitted; }
            catch (OperationCanceledException canceled)
            {
                ObservedCancellation = canceled;
                throw;
            }
        }
        public void Probe(ProbeContext context) => context.CreateFilterScope("publicLateChannelCancellation");
    }
}
