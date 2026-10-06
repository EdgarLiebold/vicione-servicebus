using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public class AmazonSqsClientOwnedDiagnosticsTests
{
    static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, true, false)]
    [InlineData(0, false, true)]
    [InlineData(1, false, false)]
    [InlineData(1, true, false)]
    [InlineData(1, false, true)]
    [InlineData(2, false, false)]
    [InlineData(2, true, false)]
    [InlineData(2, false, true)]
    [InlineData(3, false, false)]
    [InlineData(3, true, false)]
    [InlineData(3, false, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-owned-diagnostics-preserve-sdk-success-and-business-failures")]
    public async Task ClientOperation_OwnDiagnosticDoesNotReplaceProviderOutcomeAsync(int route, bool hostileLogger, bool providerFails)
    {
        using var lifetime = new CancellationTokenSource();
        var previousContext = LogContext.Current;
        var fixture = new Fixture(route, lifetime.Token, hostileLogger);
        Task? operation = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(fixture.Logger);
            operation = route switch
            {
                0 => fixture.Context.CreateQueueSubscriptionAsync(fixture.Topic, fixture.Queue, lifetime.Token),
                1 or 2 => fixture.Context.DeleteQueueAsync(fixture.Queue, lifetime.Token),
                3 => fixture.Context.DeleteTopicAsync(fixture.Topic, lifetime.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(route))
            };

            // A pre-admission diagnostic may terminate the public operation before any SDK call.
            var ready = await Task.WhenAny(operation, fixture.PrimaryEntered.Task)
                .WaitAsync(WaitBound, CancellationToken.None);
            var admitted = fixture.PrimaryEntered.Task.IsCompletedSuccessfully;
            if (admitted)
            {
                Assert.Same(fixture.PrimaryEntered.Task, ready);
                Assert.False(fixture.PrimaryTask.IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Empty(fixture.Removals);
                fixture.ReleasePrimary(providerFails);
            }
            else
            {
                Assert.Same(operation, ready);
                Assert.True(operation.IsCompleted);
                Assert.True(hostileLogger);
                Assert.NotEqual(0, route);
                Assert.DoesNotContain(fixture.SdkCalls, call => call.Name is "UnsubscribeAsync" or "DeleteQueueAsync" or "DeleteTopicAsync");
            }

            var observed = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(operation.IsCompleted);
            Assert.Equal(route == 0 ? 2 : 1, fixture.Resolutions.Count);
            Assert.All(fixture.Resolutions, call => Assert.Equal(lifetime.Token, call.Token));
            Assert.All(fixture.SdkCalls, call => Assert.Equal(lifetime.Token, call.Token));
            if (route == 0)
            {
                var subscribe = Assert.IsType<SubscribeRequest>(Assert.Single(fixture.SdkCalls, call => call.Name == "SubscribeAsync").Argument0);
                Assert.Equal(Fixture.TopicArn, subscribe.TopicArn);
                Assert.Equal(Fixture.QueueArn, subscribe.Endpoint);
                Assert.Equal("sqs", subscribe.Protocol);
                Assert.Equal("true", subscribe.Attributes["RawMessageDelivery"]);
                Assert.Equal(Fixture.TopicArn, Assert.Single(fixture.SdkCalls, call => call.Name == "ListSubscriptionsByTopicAsync").Argument0);
                Assert.Equal(Fixture.SubscriptionArn, Assert.Single(fixture.SdkCalls, call => call.Name == "GetSubscriptionAttributesAsync").Argument0);
            }
            if (route != 3)
                Assert.Contains(fixture.Resolutions, call => ReferenceEquals(call.Entity, fixture.Queue));
            if (route is 0 or 3)
                Assert.Contains(fixture.Resolutions, call => ReferenceEquals(call.Entity, fixture.Topic));
            if (admitted)
            {
                var primary = Assert.Single(fixture.SdkCalls, call => call.Name == fixture.PrimaryName);
                Assert.Equal(lifetime.Token, primary.Token);
                fixture.AssertPrimaryRequest(primary);
                if (providerFails)
                {
                    Assert.True(fixture.PrimaryTask.IsFaulted);
                    Assert.Same(fixture.ProviderFailure, Assert.Single(fixture.PrimaryTask.Exception!.InnerExceptions));
                }
                else
                    Assert.True(fixture.PrimaryTask.IsCompletedSuccessfully);
            }
            var expectedEmissions = route == 0 && providerFails ? 0 : 1;
            Assert.Equal(expectedEmissions, fixture.Logger.Emissions.Count);
            Assert.Equal(hostileLogger ? 1 : 0, fixture.Logger.ThrowCount);
            if (expectedEmissions == 1)
                fixture.AssertSelectedEmission(Assert.Single(fixture.Logger.Emissions));
            if (observed is not null)
                Assert.Same(providerFails ? fixture.ProviderFailure : fixture.LoggerFailure, observed);

            if (providerFails)
            {
                Assert.Same(fixture.ProviderFailure, observed);
                Assert.Empty(fixture.Removals);
                if (route == 0)
                {
                    Assert.Empty(fixture.QueueInfo.SubscriptionArns);
                    Assert.DoesNotContain(fixture.SdkCalls, call => call.Name == "SetQueueAttributesAsync");
                }
                else if (route is 1 or 2)
                    Assert.DoesNotContain(fixture.SdkCalls, call => call.Name == "DeleteQueueAsync");
            }
            else
            {
                Assert.Null(observed);
                Assert.True(admitted);
                Assert.All(fixture.SdkCalls, call => Assert.Equal(lifetime.Token, call.Token));
                if (route == 0)
                {
                    Assert.True(await (Task<bool>)operation);
                    Assert.Equal(Fixture.SubscriptionArn, Assert.Single(fixture.QueueInfo.SubscriptionArns));
                    var policy = Assert.Single(fixture.SdkCalls, call => call.Name == "SetQueueAttributesAsync");
                    Assert.Equal(Fixture.QueueUrl, policy.Argument0);
                    var attributes = Assert.IsAssignableFrom<IDictionary<string, string>>(policy.Argument1);
                    var text = attributes[QueueAttributeName.Policy];
                    Assert.Contains(Fixture.TopicArn, text);
                    Assert.Contains(Fixture.QueueArn, text);
                    Assert.Contains("sqs:SendMessage", text);
                    Assert.Equal(text, fixture.QueueInfo.Attributes[QueueAttributeName.Policy]);
                    Assert.Empty(fixture.Removals);
                }
                else
                {
                    var removal = Assert.Single(fixture.Removals);
                    Assert.Equal(route == 3 ? Fixture.TopicName : Fixture.QueueName, removal.Name);
                    Assert.Equal(lifetime.Token, removal.Token);
                    var events = fixture.Events.ToArray();
                    Assert.True(Array.IndexOf(events, "primary-admitted") < Array.IndexOf(events, "remove"));
                    if (route is 1 or 2)
                    {
                        var deleted = Assert.Single(fixture.SdkCalls, call => call.Name == "DeleteQueueAsync");
                        Assert.Equal(Fixture.QueueUrl, deleted.Argument0);
                        Assert.True(Array.IndexOf(events, "unsubscribe") < Array.IndexOf(events, "delete-queue"));
                        Assert.True(Array.IndexOf(events, "delete-queue") < Array.IndexOf(events, "remove"));
                    }
                }
            }
        }
        finally
        {
            fixture.ReleaseRemaining();
            var failures = new List<Exception>();
            try
            {
                foreach (var raw in fixture.RawTasks)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(raw), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(operation), failures);
                foreach (var raw in fixture.RawTasks)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(raw), failures);
                await CaptureCleanupAsync(() => fixture.QueueInfo.DisposeAsync().AsTask().WaitAsync(WaitBound, CancellationToken.None), failures);
                await CaptureCleanupAsync(() => fixture.TopicInfo.DisposeAsync().AsTask().WaitAsync(WaitBound, CancellationToken.None), failures);
                if (failures.Count != 0)
                    throw new AggregateException("Owned fixture cleanup did not complete successfully.", failures);
            }
            finally
            {
                LogContext.Current = previousContext;
            }
        }
    }

    static async Task CaptureCleanupAsync(Func<Task> cleanup, List<Exception> failures)
    {
        try { await cleanup(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveTerminalAsync(Task? task)
    {
        if (task is null)
            return;
        try { await task.WaitAsync(WaitBound, CancellationToken.None); }
        catch (Exception exception) when (exception is not TimeoutException
            && ((task.IsCanceled && exception is OperationCanceledException)
                || (task.IsFaulted && task.Exception!.InnerExceptions.Any(cause => ReferenceEquals(cause, exception)))))
        {
        }
    }

    sealed record SdkCall(string Name, object? Argument0, object? Argument1, CancellationToken Token);
    sealed record Resolution(object Entity, CancellationToken Token);
    sealed record Removal(string Name, CancellationToken Token);

    sealed class Fixture
    {
        public const string TopicName = "owned-diagnostic-topic";
        public const string QueueName = "owned-diagnostic-queue";
        public const string TopicArn = "arn:aws:sns:us-east-1:123456789012:owned-diagnostic-topic";
        public const string QueueArn = "arn:aws:sqs:us-east-1:123456789012:owned-diagnostic-queue";
        public const string SubscriptionArn = TopicArn + ":subscription";
        public const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/owned-diagnostic-queue";
        readonly int _route;
        readonly TaskCompletionSource<SetSubscriptionAttributesResponse> _attribute = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<UnsubscribeResponse> _unsubscribe = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<DeleteQueueResponse> _queueDelete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<DeleteTopicResponse> _topicDelete = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Fixture(int route, CancellationToken token, bool hostile)
        {
            _route = route;
            Logger = new SelectedLogger(route, hostile, LoggerFailure);
            var sqs = InterfaceProxy<IAmazonSQS>.Create(HandleSqs);
            var sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create(HandleSns);
            QueueInfo = new QueueInfo(QueueName, QueueUrl, new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }, sqs, token, true);
            TopicInfo = new TopicInfo(TopicName, TopicArn, sns, token, true);
            if (route is 1 or 2)
                QueueInfo.SubscriptionArns.Add(SubscriptionArn);
            Queue = new QueueEntity(1, QueueName, true, false);
            Topic = new TopicEntity(2, TopicName, true, false);
            var connection = InterfaceProxy<ConnectionContext>.Create((method, args) =>
            {
                if (method.Name is nameof(ConnectionContext.GetTopicAsync) or nameof(ConnectionContext.GetQueueAsync))
                {
                    Resolutions.Enqueue(new Resolution(args![0]!, (CancellationToken)args[1]!));
                    return Track(method.Name == nameof(ConnectionContext.GetTopicAsync) ? (Task)Task.FromResult(TopicInfo) : Task.FromResult(QueueInfo));
                }
                if (method.Name is nameof(ConnectionContext.RemoveQueueByNameAsync) or nameof(ConnectionContext.RemoveTopicByNameAsync))
                {
                    Removals.Enqueue(new Removal((string)args![0]!, (CancellationToken)args[1]!));
                    Events.Enqueue("remove");
                    // Metadata is retained for the fixture's independent owner-finally disposal.
                    return Track(Task.FromResult(true));
                }
                throw new NotSupportedException(method.Name);
            });
            Context = new AmazonSqsClientContext(connection, sqs, sns, token);
            _queueDelete.TrySetResult(new DeleteQueueResponse { HttpStatusCode = HttpStatusCode.OK });
        }

        public ApplicationException LoggerFailure { get; } = new("unique client diagnostic failure");
        public IOException ProviderFailure { get; } = new("unique admitted provider failure");
        public SelectedLogger Logger { get; }
        public QueueInfo QueueInfo { get; }
        public TopicInfo TopicInfo { get; }
        public QueueEntity Queue { get; }
        public TopicEntity Topic { get; }
        public AmazonSqsClientContext Context { get; }
        public TaskCompletionSource PrimaryEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<SdkCall> SdkCalls { get; } = new();
        public ConcurrentQueue<Resolution> Resolutions { get; } = new();
        public ConcurrentQueue<Removal> Removals { get; } = new();
        public ConcurrentQueue<string> Events { get; } = new();
        public Task PrimaryTask => _route == 0 ? _attribute.Task : _route == 3 ? _topicDelete.Task : _unsubscribe.Task;
        public string PrimaryName => _route == 0 ? "SetSubscriptionAttributesAsync" : _route == 3 ? "DeleteTopicAsync" : "UnsubscribeAsync";
        readonly ConcurrentQueue<Task> _sdkTasks = new();
        public Task[] RawTasks => new Task[] { _attribute.Task, _unsubscribe.Task, _queueDelete.Task, _topicDelete.Task }
            .Concat(_sdkTasks).Distinct().ToArray();

        object Track(Task task)
        {
            _sdkTasks.Enqueue(task);
            return task;
        }

        object HandleSqs(MethodInfo method, object?[]? args)
        {
            Record(method, args);
            if (method.Name == nameof(IAmazonSQS.DeleteQueueAsync))
            {
                Events.Enqueue("delete-queue");
                return Track(_queueDelete.Task);
            }
            if (method.Name == nameof(IAmazonSQS.SetQueueAttributesAsync))
                return Track(Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK }));
            throw new NotSupportedException(method.Name);
        }

        object HandleSns(MethodInfo method, object?[]? args)
        {
            Record(method, args);
            switch (method.Name)
            {
                case nameof(IAmazonSimpleNotificationService.SubscribeAsync):
                    return Track(Task.FromException<SubscribeResponse>(new InvalidParameterException("subscription already exists")));
                case nameof(IAmazonSimpleNotificationService.ListSubscriptionsByTopicAsync):
                    return Track(Task.FromResult(new ListSubscriptionsByTopicResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        Subscriptions = [new Subscription { TopicArn = TopicArn, Endpoint = QueueArn, Protocol = "sqs", SubscriptionArn = SubscriptionArn }]
                    }));
                case nameof(IAmazonSimpleNotificationService.GetSubscriptionAttributesAsync):
                    return Track(Task.FromResult(new GetSubscriptionAttributesResponse { HttpStatusCode = HttpStatusCode.OK, Attributes = new Dictionary<string, string> { ["RawMessageDelivery"] = "false" } }));
                case nameof(IAmazonSimpleNotificationService.SetSubscriptionAttributesAsync):
                    AdmitPrimary();
                    return Track(_attribute.Task);
                case nameof(IAmazonSimpleNotificationService.UnsubscribeAsync):
                    Events.Enqueue("unsubscribe");
                    AdmitPrimary();
                    return Track(_unsubscribe.Task);
                case nameof(IAmazonSimpleNotificationService.DeleteTopicAsync):
                    AdmitPrimary();
                    return Track(_topicDelete.Task);
                default: throw new NotSupportedException(method.Name);
            }
        }

        void Record(MethodInfo method, object?[]? args) => SdkCalls.Enqueue(new SdkCall(method.Name, args![0], args.Length == 3 ? args[1] : null, (CancellationToken)args[^1]!));
        void AdmitPrimary()
        {
            Events.Enqueue("primary-admitted");
            PrimaryEntered.TrySetResult();
        }
        public void ReleasePrimary(bool fail)
        {
            if (_route == 0)
            {
                if (fail) _attribute.TrySetException(ProviderFailure);
                else _attribute.TrySetResult(new SetSubscriptionAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
            }
            else if (_route == 3)
            {
                if (fail) _topicDelete.TrySetException(ProviderFailure);
                else _topicDelete.TrySetResult(new DeleteTopicResponse { HttpStatusCode = HttpStatusCode.OK });
            }
            else
            {
                if (fail) _unsubscribe.TrySetException(ProviderFailure);
                else _unsubscribe.TrySetResult(new UnsubscribeResponse { HttpStatusCode = HttpStatusCode.OK });
            }
        }
        public void ReleaseRemaining()
        {
            _attribute.TrySetResult(new SetSubscriptionAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
            _unsubscribe.TrySetResult(new UnsubscribeResponse { HttpStatusCode = HttpStatusCode.OK });
            _queueDelete.TrySetResult(new DeleteQueueResponse { HttpStatusCode = HttpStatusCode.OK });
            _topicDelete.TrySetResult(new DeleteTopicResponse { HttpStatusCode = HttpStatusCode.OK });
        }
        public void AssertPrimaryRequest(SdkCall call)
        {
            if (_route == 0)
            {
                var request = Assert.IsType<SetSubscriptionAttributesRequest>(call.Argument0);
                Assert.Equal(SubscriptionArn, request.SubscriptionArn);
                Assert.Equal("RawMessageDelivery", request.AttributeName);
                Assert.Equal("true", request.AttributeValue);
            }
            else if (_route == 3)
                Assert.Equal(TopicArn, call.Argument0);
            else
                Assert.Equal(SubscriptionArn, Assert.IsType<UnsubscribeRequest>(call.Argument0).SubscriptionArn);
        }
        public void AssertSelectedEmission(Dictionary<string, object?> values)
        {
            Assert.Equal(Logger.Template, values["{OriginalFormat}"]);
            if (_route == 0)
            {
                Assert.Equal(SubscriptionArn, values["SubscriptionArn"]);
                Assert.Equal("RawMessageDelivery", values["Name"]);
                Assert.Equal("true", values["Value"]);
            }
            else if (_route == 3)
                Assert.Equal(TopicArn, values["Topic"]);
            else
            {
                Assert.Equal(QueueUrl, values["Queue"]);
                if (_route == 2) Assert.Equal(SubscriptionArn, values["Subscription"]);
            }
        }
    }

    sealed class SelectedLogger(int route, bool hostile, Exception failure) : ILogger
    {
        int _throwCount;
        public string Template => route switch { 0 => "Updated subscription attribute: {SubscriptionArn} {Name}={Value}", 1 => "Delete queue: {Queue}", 2 => "Delete subscription: {Queue} {Subscription}", 3 => "Delete topic: {Topic}", _ => throw new ArgumentOutOfRangeException(nameof(route)) };
        public ConcurrentQueue<Dictionary<string, object?>> Emissions { get; } = new();
        public int ThrowCount => Volatile.Read(ref _throwCount);
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields) return;
            var values = fields.ToDictionary(x => x.Key, x => x.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var value) || !Equals(value, Template)) return;
            Emissions.Enqueue(values);
            if (!hostile) return;
            Interlocked.Increment(ref _throwCount);
            throw failure;
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
    }
}
