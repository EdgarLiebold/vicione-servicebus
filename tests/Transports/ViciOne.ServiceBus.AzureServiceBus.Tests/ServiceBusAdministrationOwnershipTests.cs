using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusAdministrationOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "admin-get-or-create-joins-existing-create-and-race-reread-stages")]
    public async Task GetOrCreate_JoinsTheExactExistingCreateOrRaceRereadAlgorithmAsync(bool topic, int route)
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(topic, caller.Token);
        Task<object>? pending = null;
        try
        {
            pending = fixture.StartAsync();
            int stages = route + 1;
            for (int stage = 0; stage < stages; stage++)
            {
                await fixture.WaitForStageAsync(stage, pending);
                Assert.False(pending.IsCompleted);
                Assert.False(fixture.Calls[stage].Task.IsCompleted);
                fixture.AssertCallsThrough(stage);
                if (route > 0 && stage == 0)
                    fixture.ReleaseFault(stage, fixture.NotFound);
                else if (route == 2 && stage == 1)
                    fixture.ReleaseFault(stage, fixture.AlreadyExists);
                else
                    fixture.ReleaseSuccess(stage);
            }

            object result = await pending.WaitAsync(Timeout, TestToken);
            Assert.Same(fixture.ExpectedResult, result);
            Assert.True(pending.IsCompletedSuccessfully);
            Assert.Equal(stages, fixture.Calls.Count);
            fixture.AssertCallsThrough(stages - 1);
            Assert.True(fixture.Calls.All(call => call.Task.IsCompleted));
        }
        finally
        {
            await fixture.DrainAndDisposeAsync(pending);
        }
        Assert.Equal(1, fixture.Client.DisposeCalls);
    }

    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 0, true)]
    [InlineData(false, 1, false)]
    [InlineData(false, 1, true)]
    [InlineData(false, 2, false)]
    [InlineData(false, 2, true)]
    [InlineData(true, 0, false)]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, false)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, false)]
    [InlineData(true, 2, true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "admin-get-create-reread-preserves-exact-fault-and-cancellation")]
    public async Task GetOrCreate_PreservesTheExactFaultOrCancellationOfEachSdkStageAsync(bool topic, int failedStage, bool canceled)
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(topic, caller.Token);
        Task<object>? pending = null;
        try
        {
            pending = fixture.StartAsync();
            for (int stage = 0; stage <= failedStage; stage++)
            {
                await fixture.WaitForStageAsync(stage, pending);
                Assert.False(pending.IsCompleted);
                Assert.False(fixture.Calls[stage].Task.IsCompleted);
                fixture.AssertCallsThrough(stage);
                if (stage == failedStage)
                    break;
                fixture.ReleaseFault(stage, stage == 0 ? fixture.NotFound : fixture.AlreadyExists);
            }

            if (canceled)
            {
                caller.Cancel();
                fixture.ReleaseCanceled(failedStage);
                OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    pending.WaitAsync(Timeout, TestToken));
                Assert.Equal(caller.Token, observed.CancellationToken);
                Assert.True(pending.IsCanceled);
                Assert.True(fixture.Calls[failedStage].Task.IsCanceled);
            }
            else
            {
                fixture.ReleaseFault(failedStage, fixture.Failure);
                Assert.Same(fixture.Failure, await Assert.ThrowsAsync<ServiceBusException>(() =>
                    pending.WaitAsync(Timeout, TestToken)));
                Assert.True(pending.IsFaulted);
                Assert.True(fixture.Calls[failedStage].Task.IsFaulted);
            }
            Assert.Equal(failedStage + 1, fixture.Calls.Count);
            fixture.AssertCallsThrough(failedStage);
        }
        finally
        {
            await fixture.DrainAndDisposeAsync(pending);
        }
        Assert.Equal(1, fixture.Client.DisposeCalls);
    }

    private sealed record Call(string Operation, object Input, CancellationToken Token, Task Task);

    private sealed class Fixture
    {
        private readonly bool _topic;
        private readonly CancellationToken _caller;
        private readonly Stage<QueueProperties>[] _queueStages;
        private readonly Stage<TopicProperties>[] _topicStages;
        private readonly ServiceBusConnectionContext _context;
        private readonly CreateQueueOptions _queue = new("exact-admin-queue") { MaxDeliveryCount = 23 };
        private readonly CreateTopicOptions _topicOptions = new("exact-admin-topic") { MaxSizeInMegabytes = 2048 };

        public Fixture(bool topic, CancellationToken caller)
        {
            _topic = topic;
            _caller = caller;
            var queue = ServiceBusModelFactory.QueueProperties(
                _queue.Name, lockDuration: TimeSpan.FromMinutes(1), maxSizeInMegabytes: 1024,
                requiresDuplicateDetection: false, requiresSession: false,
                defaultMessageTimeToLive: TimeSpan.MaxValue, autoDeleteOnIdle: TimeSpan.MaxValue,
                deadLetteringOnMessageExpiration: false, duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
                maxDeliveryCount: 7, enableBatchedOperations: true, status: EntityStatus.Active,
                forwardTo: string.Empty, forwardDeadLetteredMessagesTo: string.Empty, userMetadata: "queue-result",
                enablePartitioning: false);
            var topicResult = ServiceBusModelFactory.TopicProperties(
                _topicOptions.Name, maxSizeInMegabytes: 1024, requiresDuplicateDetection: false,
                defaultMessageTimeToLive: TimeSpan.MaxValue, autoDeleteOnIdle: TimeSpan.MaxValue,
                duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
                enableBatchedOperations: true, status: EntityStatus.Active, enablePartitioning: false);
            ExpectedResult = topic ? topicResult : queue;
            _queueStages = [new(queue), new(queue), new(queue)];
            _topicStages = [new(topicResult), new(topicResult), new(topicResult)];
            Client = new RecordingClient();
            _context = new ServiceBusConnectionContext(Client, new RecordingAdministration(this), CancellationToken.None);
            string entity = topic ? _topicOptions.Name : _queue.Name;
            NotFound = new ServiceBusException(false, "first entity lookup absent", entity,
                ServiceBusFailureReason.MessagingEntityNotFound, null);
            AlreadyExists = new ServiceBusException(false, "other creator won", entity,
                ServiceBusFailureReason.MessagingEntityAlreadyExists, null);
        }

        public RecordingClient Client { get; }
        public object ExpectedResult { get; }
        public ServiceBusException Failure { get; } = new(false, "unique terminal administration failure",
            "exact-admin-entity", ServiceBusFailureReason.MessagingEntityDisabled, null);
        public ServiceBusException NotFound { get; }
        public ServiceBusException AlreadyExists { get; }
        public List<Call> Calls { get; } = [];

        public async Task<object> StartAsync() => _topic
            ? await _context.CreateTopicAsync(_topicOptions, _caller).ConfigureAwait(false)
            : await _context.CreateQueueAsync(_queue, _caller).ConfigureAwait(false);

        public async Task WaitForStageAsync(int stage, Task pending)
        {
            Task entered = _topic ? _topicStages[stage].Entered.Task : _queueStages[stage].Entered.Task;
            await Task.WhenAny(entered, pending).WaitAsync(Timeout, TestToken);
            if (!entered.IsCompleted)
                await pending.WaitAsync(Timeout, TestToken);
            Assert.True(entered.IsCompletedSuccessfully);
        }

        public void AssertCallsThrough(int stage)
        {
            Assert.Equal(stage + 1, Calls.Count);
            string name = _topic ? _topicOptions.Name : _queue.Name;
            for (int i = 0; i <= stage; i++)
            {
                Call call = Calls[i];
                Assert.Equal(i == 1 ? "create" : "get", call.Operation);
                Assert.Equal(_caller, call.Token);
                if (i == 1)
                {
                    Assert.Same(_topic ? (object)_topicOptions : _queue, call.Input);
                    if (_topic)
                        Assert.Equal(2048, Assert.IsType<CreateTopicOptions>(call.Input).MaxSizeInMegabytes);
                    else
                        Assert.Equal(23, Assert.IsType<CreateQueueOptions>(call.Input).MaxDeliveryCount);
                }
                else
                    Assert.Equal(name, Assert.IsType<string>(call.Input));
            }
        }

        public void ReleaseSuccess(int stage)
        {
            if (_topic) _topicStages[stage].ReleaseSuccess();
            else _queueStages[stage].ReleaseSuccess();
        }
        public void ReleaseFault(int stage, Exception failure)
        {
            if (_topic) _topicStages[stage].Completion.TrySetException(failure);
            else _queueStages[stage].Completion.TrySetException(failure);
        }
        public void ReleaseCanceled(int stage)
        {
            if (_topic) _topicStages[stage].Completion.TrySetCanceled(_caller);
            else _queueStages[stage].Completion.TrySetCanceled(_caller);
        }

        private Task<global::Azure.Response<T>> RecordAsync<T>(string operation, object input, CancellationToken token, Stage<T>[] stages)
        {
            int index = Calls.Count;
            if (index >= stages.Length)
                throw new InvalidOperationException("Unexpected fourth administration call.");
            Task<global::Azure.Response<T>> actual = stages[index].Completion.Task;
            Calls.Add(new Call(operation, input, token, actual));
            stages[index].Entered.TrySetResult();
            return actual;
        }

        public async Task DrainAndDisposeAsync(Task? pending)
        {
            for (int i = 0; i < 3; i++) ReleaseSuccess(i);
            try
            {
                await ObserveKnownOutcomesAsync(pending ?? Task.CompletedTask);
            }
            finally
            {
                try
                {
                    // The public wrapper may admit later SDK calls after the gates are released.
                    // Snapshot and join the actual returned tasks after that wrapper is terminal.
                    await ObserveKnownOutcomesAsync(Task.WhenAll(Calls.Select(call => call.Task)));
                }
                finally
                {
                    await _context.DisposeAsync();
                }
            }
        }
        private async Task ObserveKnownOutcomesAsync(Task task)
        {
            try
            {
                await task.WaitAsync(Timeout, CancellationToken.None);
            }
            catch (Exception) when (task.IsFaulted && task.Exception!.Flatten().InnerExceptions.All(IsExpected))
            {
            }
            catch (OperationCanceledException) when (task.IsCanceled)
            {
            }
        }
        private bool IsExpected(Exception error) => ReferenceEquals(error, Failure)
            || ReferenceEquals(error, NotFound) || ReferenceEquals(error, AlreadyExists);

        private sealed class RecordingAdministration(Fixture owner) : ServiceBusAdministrationClient
        {
            public override Task<global::Azure.Response<QueueProperties>> GetQueueAsync(string name, CancellationToken cancellationToken = default) =>
                owner.RecordAsync("get", name, cancellationToken, owner._queueStages);
            public override Task<global::Azure.Response<QueueProperties>> CreateQueueAsync(CreateQueueOptions options, CancellationToken cancellationToken = default) =>
                owner.RecordAsync("create", options, cancellationToken, owner._queueStages);
            public override Task<global::Azure.Response<TopicProperties>> GetTopicAsync(string name, CancellationToken cancellationToken = default) =>
                owner.RecordAsync("get", name, cancellationToken, owner._topicStages);
            public override Task<global::Azure.Response<TopicProperties>> CreateTopicAsync(CreateTopicOptions options, CancellationToken cancellationToken = default) =>
                owner.RecordAsync("create", options, cancellationToken, owner._topicStages);
        }
    }

    private sealed class Stage<T>(T result)
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<global::Azure.Response<T>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseSuccess() => Completion.TrySetResult(global::Azure.Response.FromValue(result, new StubResponse()));
    }
    private sealed class RecordingClient : ServiceBusClient
    {
        public override string FullyQualifiedNamespace => "admin-ownership.servicebus.invalid";
        public int DisposeCalls { get; private set; }
        public override ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;
        public override string ReasonPhrase => "OK";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "admin-ownership";
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
        protected override bool TryGetHeader(string name, out string value) { value = null!; return false; }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) { values = null!; return false; }
    }
}
