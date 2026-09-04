using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Observers;

public sealed class MessageObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVER", "configured-filter-and-response")]
    public async Task ConfiguredObserver_ReceivesTheFilteredContextAndCompletesTheRequestResponseAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var marker = new ObserverMarker();
        var observer = new RecordingMessageObserver();
        using var harness = CreateHarness(timeout);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Observer(observer, observerConfigurator =>
                observerConfigurator.UseExecute(context => context.GetOrAddPayload(() => marker)));

        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<ObservedRequest> client = await harness.ConnectRequestClientAsync<ObservedRequest>(TestContext.Current.CancellationToken);
            var request = new ObservedRequest(NewId.NextGuid(), "request");

            Response<ObservedResponse> response = await client.GetResponseAsync<ObservedResponse>(request, cancellationToken);
            ConsumeContext<ObservedRequest> observed = await observer.Observed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new ObservedResponse(request.CorrelationId, "reply:request"), response.Message);
            Assert.Equal(request, observed.Message);
            Assert.Equal(request.CorrelationId, observed.CorrelationId);
            Assert.Equal(response.RequestId, observed.RequestId);
            Assert.True(observed.TryGetPayload(out ObserverMarker? observedMarker));
            Assert.Same(marker, observedMarker);
            Assert.Equal(1, observer.OnNextCount);
            Assert.False(observer.Faulted.IsCompleted);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVER", "on-next-failure-isolated-from-consumers")]
    public async Task OnNextFailure_IsReportedToOnErrorWithoutSuppressingIndependentConsumersAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("observer failed");
        var observer = new RecordingMessageObserver(expected);
        using var harness = CreateHarness(timeout);
        var followingPipeCalls = 0;
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Observer(observer);
            configurator.Handler<ObservedRequest>(_ =>
            {
                Interlocked.Increment(ref followingPipeCalls);
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            var request = new ObservedRequest(NewId.NextGuid(), "fault");

            await harness.InputQueueSendEndpoint.SendAsync(request, cancellationToken);

            Exception reported = await observer.Faulted.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<Fault<ObservedRequest>> fault = await harness.Published
                .SelectAsync<Fault<ObservedRequest>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Same(expected, reported);
            Assert.Equal(1, observer.OnNextCount);
            Assert.Equal(1, observer.OnErrorCount);
            Assert.Equal(1, Volatile.Read(ref followingPipeCalls));
            Assert.Equal(request, fault.Context.Message.Message);
            Assert.Contains(fault.Context.Message.Exceptions,
                exception => exception.ExceptionType == typeof(InvalidOperationException).FullName
                    && exception.Message == expected.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"message-observer-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed class RecordingMessageObserver(Exception? failure = null) : IObserver<ConsumeContext<ObservedRequest>>
    {
        private readonly TaskCompletionSource<Exception> _faulted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ConsumeContext<ObservedRequest>> _observed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _onErrorCount;
        private int _onNextCount;

        public Task<Exception> Faulted => _faulted.Task;

        public int OnErrorCount => Volatile.Read(ref _onErrorCount);

        public int OnNextCount => Volatile.Read(ref _onNextCount);

        public Task<ConsumeContext<ObservedRequest>> Observed => _observed.Task;

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            Interlocked.Increment(ref _onErrorCount);
            _faulted.TrySetResult(error);
        }

        public void OnNext(ConsumeContext<ObservedRequest> context)
        {
            Interlocked.Increment(ref _onNextCount);
            _observed.TrySetResult(context);

            if (failure is not null)
                throw failure;

            context.DeferResponse(new ObservedResponse(context.Message.CorrelationId, $"reply:{context.Message.Value}"));
        }
    }

    private sealed class ObserverMarker;

    private sealed record ObservedRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record ObservedResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
}
