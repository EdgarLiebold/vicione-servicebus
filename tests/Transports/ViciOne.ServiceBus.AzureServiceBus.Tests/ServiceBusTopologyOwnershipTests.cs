using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusTopologyOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "deployment-stages-await-all-and-concurrent-readiness-shares-one-setup")]
    public async Task ConfigureAsync_CoalescesConcurrentReadinessAndWaitsForEveryStageAsync()
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(caller.Token) { HoldFirstTopicAndQueueStages = true };
        Task<OneTimeContext<ConfigureTopologyContext<Marker>>>? first = null;
        Task<OneTimeContext<ConfigureTopologyContext<Marker>>>? second = null;
        try
        {
            first = fixture.Filter.ConfigureAsync(fixture.Context, caller.Token);
            await fixture.TwoEntered[0].Task.WaitAsync(Timeout, TestToken);
            second = fixture.Filter.ConfigureAsync(fixture.Context, caller.Token);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            Assert.Equal(2, fixture.Calls.Count);
            fixture.Held[0][0].TrySetResult();
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            Assert.Equal(2, fixture.Calls.Count);
            fixture.Held[0][1].TrySetResult();
            await fixture.TwoEntered[1].Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(4, fixture.Calls.Count);
            Assert.False(first.IsCompleted);
            fixture.Held[1][0].TrySetResult();
            Assert.False(first.IsCompleted);
            Assert.Equal(4, fixture.Calls.Count);
            fixture.Held[1][1].TrySetResult();
            OneTimeContext<ConfigureTopologyContext<Marker>> handle = await first.WaitAsync(Timeout, TestToken);
            Assert.Same(handle, await second.WaitAsync(Timeout, TestToken));
            Assert.False(fixture.AdvancedBeforePriorStageSettled);
            Assert.Equal(new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 }, fixture.Calls.Select(call => call.Stage));
            fixture.AssertExactInputs();
            Assert.True(fixture.Context.TryGetPayload<Marker>(out Marker? settings));
            Assert.Same(fixture.Settings, settings);
            Assert.Same(handle, await fixture.Filter.ConfigureAsync(fixture.Context, caller.Token).WaitAsync(Timeout, TestToken));
            Assert.Equal(10, fixture.Calls.Count);
        }
        finally
        {
            fixture.ReleaseAll();
            try
            {
                await fixture.JoinActualReturnedTasksAsync();
            }
            finally
            {
                try
                {
                    await ObserveOwnedOutcomeAsync(first);
                }
                finally
                {
                    try
                    {
                        await ObserveOwnedOutcomeAsync(second);
                    }
                    finally
                    {
                        await fixture.JoinActualReturnedTasksAsync();
                    }
                }
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "next-failure-is-joined-preserved-and-evicts-successful-setup")]
    public async Task SendAsync_JoinsNextAndEvictsSuccessfulSetupAfterItsExactFailureAsync()
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(caller.Token);
        var next = new NextPipe();
        var failure = new ExpectedNextFailure();
        Task? pending = null;
        Task? setup = null;
        Task? repeated = null;
        try
        {
            setup = fixture.Filter.ConfigureAsync(fixture.Context, caller.Token);
            await setup.WaitAsync(Timeout, TestToken);
            Assert.Equal(10, fixture.Calls.Count);
            pending = fixture.Filter.SendAsync(fixture.Context, next);
            await Task.WhenAny(next.Entered.Task, pending).WaitAsync(Timeout, TestToken);
            if (!next.Entered.Task.IsCompleted)
                await pending.WaitAsync(Timeout, TestToken);
            Assert.True(next.Entered.Task.IsCompletedSuccessfully);
            Assert.Same(fixture.Context, next.Context);
            Assert.Equal(1, next.Calls);
            Assert.Equal(10, fixture.Calls.Count);
            Assert.False(pending.IsCompleted);
            Assert.False(next.ActualReturnedTask!.IsCompleted);
            next.Release.TrySetException(failure);
            Assert.Same(failure, await Assert.ThrowsAsync<ExpectedNextFailure>(() => pending.WaitAsync(Timeout, TestToken)));
            Assert.True(pending.IsFaulted);
            repeated = fixture.Filter.ConfigureAsync(fixture.Context, caller.Token);
            await repeated.WaitAsync(Timeout, TestToken);
            Assert.Equal(20, fixture.Calls.Count);
            await fixture.Filter.ConfigureAsync(fixture.Context, caller.Token).WaitAsync(Timeout, TestToken);
            Assert.Equal(20, fixture.Calls.Count);
            Assert.Equal(1, next.Calls);
            fixture.AssertExactInputs();
        }
        finally
        {
            fixture.ReleaseAll();
            next.Release.TrySetResult();
            try
            {
                await fixture.JoinActualReturnedTasksAsync();
            }
            finally
            {
                try
                {
                    await ObserveOwnedOutcomeAsync(next.ActualReturnedTask, failure);
                }
                finally
                {
                    try
                    {
                        await ObserveOwnedOutcomeAsync(pending, failure);
                    }
                    finally
                    {
                        try
                        {
                            await ObserveOwnedOutcomeAsync(setup);
                        }
                        finally
                        {
                            try
                            {
                                await ObserveOwnedOutcomeAsync(repeated);
                            }
                            finally
                            {
                                await fixture.JoinActualReturnedTasksAsync();
                            }
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "sync-provider-failure-joins-the-already-admitted-task-in-every-stage")]
    public async Task ConfigureAsync_JoinsAnAdmittedTaskAfterTheNextProviderThrowsSynchronouslyAsync(int stage)
    {
        using var caller = new CancellationTokenSource();
        var failure = new ExpectedProviderFailure();
        var fixture = new Fixture(caller.Token) { SynchronousFailureStage = stage, ProviderFailure = failure };
        Task? pending = null;
        try
        {
            pending = fixture.Filter.ConfigureAsync(fixture.Context, caller.Token);
            await fixture.SecondFailureEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(stage, fixture.Calls.Last().Stage);
            Assert.Equal(2, fixture.Calls.Count(call => call.Stage == stage));
            Assert.NotNull(fixture.HeldActualReturnedTask);
            Assert.False(fixture.HeldActualReturnedTask.IsCompleted);
            // Finite OriginalRED: old lazy Task.WhenAll enumeration is already faulted while its first accepted task is held.
            Assert.False(pending.IsCompleted);
            fixture.Held[stage][0].TrySetResult();
            Assert.Same(failure, await Assert.ThrowsAsync<ExpectedProviderFailure>(() => pending.WaitAsync(Timeout, TestToken)));
            Assert.True(pending.IsFaulted);
            Assert.True(fixture.HeldActualReturnedTask.IsCompletedSuccessfully);
            Assert.All(fixture.Calls, call => Assert.True(call.Stage <= stage));
            fixture.AssertExactInputs();
        }
        finally
        {
            fixture.ReleaseAll();
            try
            {
                await fixture.JoinActualReturnedTasksAsync();
            }
            finally
            {
                try
                {
                    await ObserveOwnedOutcomeAsync(pending, failure);
                }
                finally
                {
                    await fixture.JoinActualReturnedTasksAsync();
                }
            }
        }
    }

    private static async Task ObserveOwnedOutcomeAsync(Task? task, Exception? expected = null)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(Timeout, CancellationToken.None);
        }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, expected))
        {
        }
        Assert.True(task.IsCompleted);
    }

    private sealed class Marker;
    private sealed class ExpectedNextFailure : Exception;
    private sealed class ExpectedProviderFailure : Exception;
    private sealed record ProviderCall(int Stage, object Options, object? Rule, object? Filter, CancellationToken Token);

    private sealed class Fixture
    {
        private readonly int[] _stageCounts = new int[5];
        private readonly CancellationToken _caller;
        private readonly BrokerTopology _topology;
        private readonly ConcurrentQueue<Task> _actualReturnedTasks = new();
        public Fixture(CancellationToken caller)
        {
            _caller = caller;
            var builder = new BrokerTopologyBuilder();
            var topicA = builder.CreateTopic(NewTopic("ownership-topic-a"));
            var topicB = builder.CreateTopic(NewTopic("ownership-topic-b"));
            var queueA = builder.CreateQueue(NewQueue("ownership-queue-a"));
            var queueB = builder.CreateQueue(NewQueue("ownership-queue-b"));
            builder.CreateSubscription(topicA, NewSubscription("ownership-topic-a", "consumer-a"), NewRule("consumer-rule"), null);
            builder.CreateSubscription(topicB, NewSubscription("ownership-topic-b", "consumer-b"), null, NewFilter("consumer-b"));
            builder.CreateQueueSubscription(topicA, queueA, NewSubscription("ownership-topic-a", "queue-a"), NewRule("queue-rule"), null);
            builder.CreateQueueSubscription(topicB, queueB, NewSubscription("ownership-topic-b", "queue-b"), null, NewFilter("queue-b"));
            builder.CreateTopicSubscription(topicA, topicB, NewSubscription("ownership-topic-a", "topic-a"));
            builder.CreateTopicSubscription(topicB, topicA, NewSubscription("ownership-topic-b", "topic-b"));
            _topology = builder.BuildBrokerTopology();
            ConnectionContext connection = DispatchProxy.Create<ConnectionContext, RecordingConnection>();
            ((RecordingConnection)connection).Handler = InvokeAsync;
            Context = new Client(connection, caller);
            Filter = new ConfigureServiceBusTopologyFilter<Marker>(Settings, _topology);
        }
        public Marker Settings { get; } = new();
        public Client Context { get; }
        public ConfigureServiceBusTopologyFilter<Marker> Filter { get; }
        public ConcurrentQueue<ProviderCall> Calls { get; } = new();
        public TaskCompletionSource[][] Held { get; } = Enumerable.Range(0, 5).Select(_ => new[] { NewSignal(), NewSignal() }).ToArray();
        public TaskCompletionSource[] TwoEntered { get; } = Enumerable.Range(0, 5).Select(_ => NewSignal()).ToArray();
        public TaskCompletionSource SecondFailureEntered { get; } = NewSignal();
        public bool HoldFirstTopicAndQueueStages { get; init; }
        public int SynchronousFailureStage { get; init; } = -1;
        public ExpectedProviderFailure? ProviderFailure { get; init; }
        public Task? HeldActualReturnedTask { get; private set; }
        public bool AdvancedBeforePriorStageSettled { get; private set; }

        private Task InvokeAsync(MethodInfo method, object?[] args)
        {
            int stage = method.Name switch
            {
                nameof(ConnectionContext.CreateTopicAsync) => 0,
                nameof(ConnectionContext.CreateQueueAsync) => 1,
                nameof(ConnectionContext.CreateTopicSubscriptionAsync) => ((CreateSubscriptionOptions)args[0]!).SubscriptionName.Split('-')[0] switch
                {
                    "consumer" => 2,
                    "queue" => 3,
                    "topic" => 4,
                    _ => throw new InvalidOperationException("Unknown subscription stage.")
                },
                _ => throw new NotSupportedException(method.Name)
            };
            int count = Interlocked.Increment(ref _stageCounts[stage]);
            if (HoldFirstTopicAndQueueStages && stage > 0 && Held[0].Any(gate => !gate.Task.IsCompleted)
                || HoldFirstTopicAndQueueStages && stage > 1 && Held[1].Any(gate => !gate.Task.IsCompleted))
                AdvancedBeforePriorStageSettled = true;
            Calls.Enqueue(new ProviderCall(stage, args[0]!, stage > 1 ? args[1] : null, stage > 1 ? args[2] : null,
                (CancellationToken)args[^1]!));
            if (count == 2)
                TwoEntered[stage].TrySetResult();
            if (stage == SynchronousFailureStage && count == 2)
            {
                SecondFailureEntered.TrySetResult();
                throw ProviderFailure ?? throw new InvalidOperationException("Missing test provider failure.");
            }
            Task gate = (stage == SynchronousFailureStage && count == 1 || HoldFirstTopicAndQueueStages && stage < 2 && count <= 2)
                ? Held[stage][count - 1].Task : Task.CompletedTask;
            // Return the actual generic SPI task, not the non-generic synchronization gate.
            Task result = stage switch
            {
                0 => CompletePropertiesAsync(gate, CreateTopicProperties(((CreateTopicOptions)args[0]!).Name)),
                1 => CompletePropertiesAsync(gate, CreateQueueProperties(((CreateQueueOptions)args[0]!).Name)),
                _ => CompletePropertiesAsync(gate, CreateSubscriptionProperties((CreateSubscriptionOptions)args[0]!))
            };
            Assert.True(method.ReturnType.IsInstanceOfType(result));
            if (stage == SynchronousFailureStage && count == 1)
                HeldActualReturnedTask = result;
            _actualReturnedTasks.Enqueue(result);
            return result;
        }
        public void AssertExactInputs()
        {
            foreach (ProviderCall call in Calls)
            {
                Assert.Equal(_caller, call.Token);
                if (call.Stage == 0)
                {
                    var actual = Assert.IsType<CreateTopicOptions>(call.Options);
                    var declared = Assert.Single(_topology.Topics, item => item.CreateTopicOptions.Name == actual.Name);
                    AssertOptions(declared.CreateTopicOptions, actual);
                }
                else if (call.Stage == 1)
                {
                    var actual = Assert.IsType<CreateQueueOptions>(call.Options);
                    var declared = Assert.Single(_topology.Queues, item => item.CreateQueueOptions.Name == actual.Name);
                    AssertOptions(declared.CreateQueueOptions, actual);
                }
                else
                {
                    var subscriptions = call.Stage switch
                    {
                        2 => _topology.Subscriptions,
                        3 => _topology.QueueSubscriptions.Select(item => item.Subscription).ToArray(),
                        _ => _topology.TopicSubscriptions.Select(item => item.Subscription).ToArray()
                    };
                    var actual = Assert.IsType<CreateSubscriptionOptions>(call.Options);
                    var declared = Assert.Single(subscriptions, item => item.CreateSubscriptionOptions.TopicName == actual.TopicName
                        && item.CreateSubscriptionOptions.SubscriptionName == actual.SubscriptionName);
                    AssertOptions(declared.CreateSubscriptionOptions, actual);
                    AssertRule(declared.Rule, call.Rule);
                    AssertFilter(declared.Filter, call.Filter);
                }
            }
        }
        public void ReleaseAll()
        {
            foreach (TaskCompletionSource gate in Held.SelectMany(stage => stage))
                gate.TrySetResult();
        }
        public async Task JoinActualReturnedTasksAsync()
        {
            Task[] actualTasks = _actualReturnedTasks.ToArray();
            await Task.WhenAll(actualTasks).WaitAsync(Timeout, CancellationToken.None);
            Assert.All(actualTasks, task => Assert.True(task.IsCompletedSuccessfully));
        }
    }

    public class RecordingConnection : DispatchProxy
    {
        public Func<MethodInfo, object?[], Task> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Missing proxy method."), args ?? []);
    }

    private sealed class Client(ConnectionContext connection, CancellationToken token) : BasePipeContext(token), ClientContext
    {
        public ConnectionContext ConnectionContext => connection;
        public Uri InputAddress => new("sb://unit.servicebus.invalid/topology");
        public string EntityPath => "topology";
        public bool IsClosedOrClosing => false;
        public void ConfigureMessageProcessor(Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
            Func<ProcessErrorEventArgs, Task> exceptionHandler) => throw new NotSupportedException();
        public void ConfigureSessionProcessor(Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task> callback,
            Func<ProcessErrorEventArgs, Task> exceptionHandler) => throw new NotSupportedException();
        public Task StartAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ShutdownAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CloseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task NotifyFaultedAsync(Exception exception, string entityPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class NextPipe : IPipe<ClientContext>
    {
        public TaskCompletionSource Entered { get; } = NewSignal();
        public TaskCompletionSource Release { get; } = NewSignal();
        public ClientContext? Context { get; private set; }
        public Task? ActualReturnedTask { get; private set; }
        public int Calls { get; private set; }
        public Task SendAsync(ClientContext context)
        {
            Context = context;
            Calls++;
            ActualReturnedTask = Release.Task;
            Entered.TrySetResult();
            return ActualReturnedTask;
        }
        public void Probe(ProbeContext context) { }
    }
    private static async Task<T> CompletePropertiesAsync<T>(Task gate, T properties)
    {
        await gate.ConfigureAwait(false);
        return properties;
    }

    // Exact pinned SDK 7.20.2 model factory overloads already used by SharedAdministrationLeaseTests.
    private static QueueProperties CreateQueueProperties(string name) => ServiceBusModelFactory.QueueProperties(
        name, lockDuration: TimeSpan.FromMinutes(1), maxSizeInMegabytes: 1024,
        requiresDuplicateDetection: false, requiresSession: false,
        defaultMessageTimeToLive: TimeSpan.MaxValue, autoDeleteOnIdle: TimeSpan.MaxValue,
        deadLetteringOnMessageExpiration: false, duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
        maxDeliveryCount: 10, enableBatchedOperations: true, status: EntityStatus.Active,
        forwardTo: string.Empty, forwardDeadLetteredMessagesTo: string.Empty, userMetadata: string.Empty,
        enablePartitioning: false);

    private static TopicProperties CreateTopicProperties(string name) => ServiceBusModelFactory.TopicProperties(
        name, maxSizeInMegabytes: 1024, requiresDuplicateDetection: false,
        defaultMessageTimeToLive: TimeSpan.MaxValue, autoDeleteOnIdle: TimeSpan.MaxValue,
        duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
        enableBatchedOperations: true, status: EntityStatus.Active, enablePartitioning: false);

    private static SubscriptionProperties CreateSubscriptionProperties(CreateSubscriptionOptions options) =>
        ServiceBusModelFactory.SubscriptionProperties(options.TopicName, options.SubscriptionName,
            options.LockDuration, options.RequiresSession, options.DefaultMessageTimeToLive,
            options.AutoDeleteOnIdle, options.DeadLetteringOnMessageExpiration, options.MaxDeliveryCount,
            options.EnableBatchedOperations, EntityStatus.Active, options.ForwardTo ?? string.Empty,
            options.ForwardDeadLetteredMessagesTo ?? string.Empty, options.UserMetadata ?? string.Empty);

    private static CreateTopicOptions NewTopic(string name) => new(name)
    {
        AutoDeleteOnIdle = TimeSpan.FromMinutes(11),
        DefaultMessageTimeToLive = TimeSpan.FromHours(3),
        EnableBatchedOperations = false,
        UserMetadata = "topic-metadata:" + name,
    };

    private static CreateQueueOptions NewQueue(string name) => new(name)
    {
        AutoDeleteOnIdle = TimeSpan.FromMinutes(13),
        DefaultMessageTimeToLive = TimeSpan.FromHours(2),
        LockDuration = TimeSpan.FromMinutes(2),
        MaxDeliveryCount = 17,
        DeadLetteringOnMessageExpiration = true,
        EnableBatchedOperations = false,
        UserMetadata = "queue-metadata:" + name,
    };

    private static CreateSubscriptionOptions NewSubscription(string topic, string name) => new(topic, name)
    {
        DefaultMessageTimeToLive = TimeSpan.FromMinutes(45),
        LockDuration = TimeSpan.FromMinutes(2),
        MaxDeliveryCount = 27,
        DeadLetteringOnMessageExpiration = true,
        EnableDeadLetteringOnFilterEvaluationExceptions = true,
        EnableBatchedOperations = false,
        UserMetadata = "subscription-metadata:" + name,
    };

    private static SqlRuleFilter NewFilter(string value)
    {
        var filter = new SqlRuleFilter("kind = @kind");
        filter.Parameters.Add("kind", value);
        return filter;
    }

    private static CreateRuleOptions NewRule(string name)
    {
        var action = new SqlRuleAction("SET route = @route");
        action.Parameters.Add("route", name);
        return new CreateRuleOptions(name, NewFilter(name)) { Action = action };
    }

    private static void AssertOptions(CreateTopicOptions expected, CreateTopicOptions actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.AutoDeleteOnIdle, actual.AutoDeleteOnIdle);
        Assert.Equal(expected.DefaultMessageTimeToLive, actual.DefaultMessageTimeToLive);
        Assert.Equal(expected.EnableBatchedOperations, actual.EnableBatchedOperations);
        Assert.Equal(expected.EnablePartitioning, actual.EnablePartitioning);
        Assert.Equal(expected.RequiresDuplicateDetection, actual.RequiresDuplicateDetection);
        Assert.Equal(expected.SupportOrdering, actual.SupportOrdering);
        Assert.Equal(expected.UserMetadata, actual.UserMetadata);
    }

    private static void AssertOptions(CreateQueueOptions expected, CreateQueueOptions actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.AutoDeleteOnIdle, actual.AutoDeleteOnIdle);
        Assert.Equal(expected.DefaultMessageTimeToLive, actual.DefaultMessageTimeToLive);
        Assert.Equal(expected.LockDuration, actual.LockDuration);
        Assert.Equal(expected.MaxDeliveryCount, actual.MaxDeliveryCount);
        Assert.Equal(expected.DeadLetteringOnMessageExpiration, actual.DeadLetteringOnMessageExpiration);
        Assert.Equal(expected.EnableBatchedOperations, actual.EnableBatchedOperations);
        Assert.Equal(expected.RequiresSession, actual.RequiresSession);
        Assert.Equal(expected.EnablePartitioning, actual.EnablePartitioning);
        Assert.Equal(expected.ForwardTo, actual.ForwardTo);
        Assert.Equal(expected.ForwardDeadLetteredMessagesTo, actual.ForwardDeadLetteredMessagesTo);
        Assert.Equal(expected.UserMetadata, actual.UserMetadata);
    }

    private static void AssertOptions(CreateSubscriptionOptions expected, CreateSubscriptionOptions actual)
    {
        Assert.Equal(expected.TopicName, actual.TopicName);
        Assert.Equal(expected.SubscriptionName, actual.SubscriptionName);
        Assert.Equal(expected.AutoDeleteOnIdle, actual.AutoDeleteOnIdle);
        Assert.Equal(expected.DefaultMessageTimeToLive, actual.DefaultMessageTimeToLive);
        Assert.Equal(expected.LockDuration, actual.LockDuration);
        Assert.Equal(expected.MaxDeliveryCount, actual.MaxDeliveryCount);
        Assert.Equal(expected.DeadLetteringOnMessageExpiration, actual.DeadLetteringOnMessageExpiration);
        Assert.Equal(expected.EnableDeadLetteringOnFilterEvaluationExceptions, actual.EnableDeadLetteringOnFilterEvaluationExceptions);
        Assert.Equal(expected.EnableBatchedOperations, actual.EnableBatchedOperations);
        Assert.Equal(expected.RequiresSession, actual.RequiresSession);
        Assert.Equal(expected.ForwardTo, actual.ForwardTo);
        Assert.Equal(expected.ForwardDeadLetteredMessagesTo, actual.ForwardDeadLetteredMessagesTo);
        Assert.Equal(expected.UserMetadata, actual.UserMetadata);
    }

    private static void AssertRule(CreateRuleOptions? expected, object? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }
        var rule = Assert.IsType<CreateRuleOptions>(actual);
        Assert.Equal(expected.Name, rule.Name);
        AssertFilter(expected.Filter, rule.Filter);
        if (expected.Action is null)
            Assert.Null(rule.Action);
        else
        {
            var expectedAction = Assert.IsType<SqlRuleAction>(expected.Action);
            var actualAction = Assert.IsType<SqlRuleAction>(rule.Action);
            Assert.Equal(expectedAction.SqlExpression, actualAction.SqlExpression);
            Assert.Equal(expectedAction.Parameters.Count, actualAction.Parameters.Count);
            foreach (var parameter in expectedAction.Parameters)
                Assert.Equal(parameter.Value, actualAction.Parameters[parameter.Key]);
        }
    }

    private static void AssertFilter(RuleFilter? expected, object? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }
        var expectedFilter = Assert.IsType<SqlRuleFilter>(expected);
        var actualFilter = Assert.IsType<SqlRuleFilter>(actual);
        Assert.Equal(expectedFilter.SqlExpression, actualFilter.SqlExpression);
        Assert.Equal(expectedFilter.Parameters.Count, actualFilter.Parameters.Count);
        foreach (var parameter in expectedFilter.Parameters)
            Assert.Equal(parameter.Value, actualFilter.Parameters[parameter.Key]);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
