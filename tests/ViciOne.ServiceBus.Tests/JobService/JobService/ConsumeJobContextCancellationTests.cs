using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class ConsumeJobContextCancellationTests
{
    [Theory]
    [InlineData(JobOperation.NotifyCanceled)]
    [InlineData(JobOperation.NotifyStarted)]
    [InlineData(JobOperation.NotifyCompleted)]
    [InlineData(JobOperation.NotifyProgress)]
    [InlineData(JobOperation.NotifyFaulted)]
    [InlineData(JobOperation.SaveState)]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "every-notification-forwards-the-operation-token")]
    public async Task NotificationOperation_ForwardsTheExactCancellationTokenAsync(JobOperation operation)
    {
        var endpoint = new RecordingSendEndpoint();
        var provider = new RecordingPublishEndpointProvider(endpoint);
        ConsumeContext<StartJob> consumeContext = CreateContext(provider);
        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            new JobOptions<TestJob>());
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await (operation switch
        {
            JobOperation.NotifyCanceled => context.NotifyCanceledAsync(source.Token),
            JobOperation.NotifyStarted => context.NotifyStartedAsync(source.Token),
            JobOperation.NotifyCompleted => context.NotifyCompletedAsync(source.Token),
            JobOperation.NotifyProgress => context.NotifyJobProgressAsync(
                new SetJobProgressCommand
                {
                    JobId = context.JobId,
                    AttemptId = context.AttemptId,
                    SequenceNumber = 1,
                    Value = 42,
                    Limit = 100,
                },
                source.Token),
            JobOperation.NotifyFaulted => context.NotifyFaultedAsync(new InvalidOperationException("expected"), null, source.Token),
            JobOperation.SaveState => context.SaveJobStateAsync<object>(null, source.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        });

        Assert.NotEmpty(provider.CancellationTokens);
        Assert.All(provider.CancellationTokens, token => Assert.Equal(source.Token, token));
        Assert.NotEmpty(endpoint.CancellationTokens);
        Assert.All(endpoint.CancellationTokens, token => Assert.Equal(source.Token, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "progress-enqueue-observes-the-operation-token")]
    public async Task SetJobProgress_ObservesAnAlreadyCanceledOperationTokenAsync()
    {
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        ConsumeContext<StartJob> consumeContext = CreateContext(provider);
        var options = new JobOptions<TestJob>();
        options.ProgressBuffer.UpdateLimit = 1;
        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            options);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.SetJobProgressAsync(42, 100, source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
    }

    private static ConsumeContext<StartJob> CreateContext(IPublishEndpointProvider publishEndpointProvider)
    {
        TestConsumeContext<StartJob> context = DispatchProxy.Create<TestConsumeContext<StartJob>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(new StartJobMessage(), publishEndpointProvider);
        return context;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private class ConsumeContextProxy : DispatchProxy
    {
        private object _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;

        public void Configure(object message, IPublishEndpointProvider publishEndpointProvider)
        {
            _message = message;
            _receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)_receiveContext).PublishEndpointProvider = publishEndpointProvider;
            _serializerContext = DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_Message":
                    return _message;
                case "get_ReceiveContext":
                    return _receiveContext;
                case "get_SerializerContext":
                    return _serializerContext;
                case "get_CancellationToken":
                    return TestContext.Current.CancellationToken;
                case "HasPayloadType":
                    return false;
                case "TryGetPayload":
                    args![0] = null;
                    return false;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                "get_CancellationToken" => TestContext.Current.CancellationToken,
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class RecordingPublishEndpointProvider(RecordingSendEndpoint endpoint) : IPublishEndpointProvider
    {
        public List<CancellationToken> CancellationTokens { get; } = [];

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            CancellationTokens.Add(cancellationToken);
            return Task.FromResult<ISendEndpoint>(endpoint);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSendEndpoint : ISendEndpoint
    {
        public List<CancellationToken> CancellationTokens { get; } = [];

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            CancellationTokens.Add(cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) =>
            throw new NotSupportedException();
    }

    private sealed class StartJobMessage : StartJob
    {
        public Guid JobId { get; } = Guid.NewGuid();
        public Guid AttemptId { get; } = Guid.NewGuid();
        public int RetryAttempt => 0;
        public Dictionary<string, object> Job { get; } = [];
        public Guid JobTypeId { get; } = Guid.NewGuid();
        public long? LastProgressValue => null;
        public long? LastProgressLimit => null;
        public Dictionary<string, object>? JobState => null;
        public Dictionary<string, object>? JobProperties { get; } = [];
    }

    private sealed record TestJob;

    public enum JobOperation
    {
        NotifyCanceled,
        NotifyStarted,
        NotifyCompleted,
        NotifyProgress,
        NotifyFaulted,
        SaveState,
    }
}
