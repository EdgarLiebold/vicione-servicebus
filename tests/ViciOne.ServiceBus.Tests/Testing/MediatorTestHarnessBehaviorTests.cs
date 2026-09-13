using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class MediatorTestHarnessBehaviorTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MEDIATOR", "request-response-and-observations")]
    public async Task RequestResponse_RecordsTheExactRequestAndResponseOnTheConfiguredClockAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var harness = new MediatorTestHarness(timeProvider)
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.MediatorConfiguring += configurator => configurator.Handler<MediatorRequest>(context =>
            context.RespondAsync(new MediatorResponse($"response:{context.Message.Value}")));

        await harness.StartAsync(TestContext.Current.CancellationToken);
        IRequestClient<MediatorRequest> client = harness.CreateRequestClient<MediatorRequest>();
        Response<MediatorResponse> response = await client.GetResponseAsync<MediatorResponse>(
            new MediatorRequest("expected"),
            cancellationToken);

        IConsumedMessage<MediatorRequest> consumed = await harness.Consumed
            .SelectAsync<MediatorRequest>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        ISentMessage<MediatorResponse> sent = await harness.Sent
            .SelectAsync<MediatorResponse>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(timeProvider, harness.TimeProvider);
        Assert.Equal("expected", consumed.Context.Message.Value);
        Assert.Null(consumed.Exception);
        Assert.Equal("response:expected", response.Message.Value);
        Assert.Equal("response:expected", sent.Context.Message.Value);
        Assert.Equal(response.RequestId, sent.Context.RequestId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MEDIATOR", "exact-failure-propagation")]
    public async Task HandlerFailure_IsPropagatedAndRecordedWithoutWrappingAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected mediator failure");
        await using var harness = new MediatorTestHarness
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.MediatorConfiguring += configurator => configurator.Handler<MediatorFailureMessage>(_ => Task.FromException(expected));

        await harness.StartAsync(TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Mediator.SendAsync(new MediatorFailureMessage(), cancellationToken));
        IConsumedMessage<MediatorFailureMessage> observed = await harness.Consumed
            .SelectAsync<MediatorFailureMessage>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Same(expected, observed.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MEDIATOR", "async-owned-resource-cleanup")]
    public async Task DisposeAsync_ReleasesTheOwnedMediatorAndIsIdempotentAsync()
    {
        var harness = new MediatorTestHarness
        {
            TestTimeout = OperationTimeout(),
            TestInactivityTimeout = OperationTimeout(),
        };
        await harness.StartAsync(TestContext.Current.CancellationToken);
        IMediator mediator = harness.Mediator;

        await harness.DisposeAsync();
        await harness.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = mediator.CreateRequestClient<MediatorRequest>(new RequestTimeout(OperationTimeout()));
        });
        Assert.Throws<ObjectDisposedException>(harness.BeginTestScope);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MEDIATOR", "public-observer-connectors-and-published-history")]
    public async Task ObserverConnectors_ExposeConsumePublishAndSendActivityThroughThePublicHarnessContractAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using var harness = new MediatorTestHarness
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.MediatorConfiguring += configurator => configurator.Handler<MediatorRequest>(async context =>
        {
            await context.Advanced().PublishAsync(new MediatorPublished(context.Message.Value), context.CancellationToken);
            await context.RespondAsync(new MediatorResponse($"response:{context.Message.Value}"));
        });

        await harness.StartAsync(TestContext.Current.CancellationToken);
        var observer = new RecordingObserver();
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => harness.ConnectConsumeObserver(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => harness.ConnectPublishObserver(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => harness.ConnectSendObserver(null!)).ParamName);
        using ConnectHandle consumeConnection = harness.ConnectConsumeObserver(observer);
        using ConnectHandle publishConnection = harness.ConnectPublishObserver(observer);
        using ConnectHandle sendConnection = harness.ConnectSendObserver(observer);

        Response<MediatorResponse> response = await harness.CreateRequestClient<MediatorRequest>().GetResponseAsync<MediatorResponse>(
            new MediatorRequest("observed"),
            TestContext.Current.CancellationToken);
        IPublishedMessage<MediatorPublished> published = await harness.Published
            .SelectAsync<MediatorPublished>(TestContext.Current.CancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(harness.TestCancellationToken, harness.CancellationToken);
        Assert.Equal("response:observed", response.Message.Value);
        Assert.Equal("observed", published.Context.Message.Value);
        Assert.True(observer.ConsumeCount >= 2);
        Assert.True(observer.PublishCount >= 2);
        Assert.True(observer.SendCount >= 2);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MediatorRequest(string Value);

    private sealed record MediatorResponse(string Value);

    private sealed record MediatorPublished(string Value);

    private sealed record MediatorFailureMessage;

    private sealed class RecordingObserver : IConsumeObserver, IPublishObserver, ISendObserver
    {
        private int _consumeCount;
        private int _publishCount;
        private int _sendCount;

        public int ConsumeCount => Volatile.Read(ref _consumeCount);

        public int PublishCount => Volatile.Read(ref _publishCount);

        public int SendCount => Volatile.Read(ref _sendCount);

        public Task PreConsumeAsync<T>(ConsumeContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _consumeCount);
            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _consumeCount);
            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _publishCount);
            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _publishCount);
            return Task.CompletedTask;
        }

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _sendCount);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _sendCount);
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }
}
