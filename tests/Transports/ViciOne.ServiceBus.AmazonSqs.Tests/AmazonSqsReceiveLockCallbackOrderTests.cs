using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsReceiveLockCallbackOrderTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "complete-callback-failure-observed-sdk-completion-order")]
    public Task Complete_RealSdkCancellationCallbackPreservesObservedWorkerOrderAsync(bool hostile)
        => RunAsync(true, hostile);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "faulted-callback-failure-observed-sdk-completion-order")]
    public Task Faulted_RealSdkCancellationCallbackPreservesObservedWorkerOrderAsync(bool hostile)
        => RunAsync(false, hostile);

    static async Task RunAsync(bool complete, bool hostile)
    {
        var previous = LogContext.Current;
        Fixture? fixture = null;
        AmazonSqsReceiveLockContext? context = null;
        Task? operation = null;
        Task<CompletionObservation>? observation = null;
        var publicTasks = new List<Task>();
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
            fixture = new Fixture(hostile);
            Task<QueueInfo> warm = fixture.Client.GetQueueInfoAsync(Fixture.QueueName, CancellationToken.None);
            publicTasks.Add(warm);
            QueueInfo queue = await warm.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(Fixture.QueueUrl, queue.Url);
            Assert.Equal(Fixture.QueueName, queue.EntityName);
            Assert.Equal(2, fixture.Calls.Count);
            SdkCall lookup = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.GetQueueUrlAsync));
            Assert.Equal(Fixture.QueueName, lookup.Arguments[0]);
            Assert.True(lookup.Raw.IsCompletedSuccessfully);
            SdkCall attributes = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.GetQueueAttributesAsync));
            Assert.Equal(Fixture.QueueUrl, attributes.Arguments[0]);
            Assert.Equal("All", Assert.Single(Assert.IsAssignableFrom<IEnumerable<string>>(attributes.Arguments[1])));
            Assert.True(attributes.Raw.IsCompletedSuccessfully);
            context = new AmazonSqsReceiveLockContext(Fixture.InputAddress,
                new Message { ReceiptHandle = Fixture.Receipt }, fixture.Settings, fixture.Client, fixture.Lifetime.Token);
            await fixture.FirstEntered.Task.WaitAsync(Bound, CancellationToken.None);
            SdkCall first = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.ChangeMessageVisibilityAsync));
            AssertVisibility(first, 60);
            Assert.False(first.Raw.IsCompleted);
            Assert.False(first.Token.IsCancellationRequested);
            Exception? invocationFailure = Record.Exception(() =>
            {
                operation = complete ? context.CompleteAsync(CancellationToken.None)
                    : context.FaultedAsync(fixture.ConsumerFailure, CancellationToken.None);
            });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            publicTasks.Add(operation);
            // This observes the actual public Task. It never asserts inside a callback or probes a private worker.
            observation = operation.ContinueWith(task =>
                new CompletionObservation(task.Status, first.Raw.IsCompleted, first.Raw.Status),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            await fixture.CallbackEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Task callbackRetired = fixture.DisposeCallbackAsync();
            publicTasks.Add(callbackRetired);
            await callbackRetired.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, fixture.CallbackCalls);
            Assert.True(first.Token.IsCancellationRequested);
            Assert.False(first.Raw.IsCompleted);
            // Callback retirement is not worker completion. Releasing the genuine SDK task enables either implementation to retire.
            fixture.ReleaseFirst();
            await first.Raw.WaitAsync(Bound, CancellationToken.None);
            Assert.True(first.Raw.IsCompletedSuccessfully);
            if (hostile)
            {
                Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
                Assert.True(operation.IsFaulted);
                Assert.Same(fixture.CallbackFailure, Assert.Single(Assert.IsType<AggregateException>(failure).Flatten().InnerExceptions));
                Assert.Equal(0, fixture.DeleteCalls);
                Assert.Equal(1, fixture.VisibilityCalls);
            }
            else
            {
                Task admitted = complete ? fixture.DeleteEntered.Task : fixture.SecondEntered.Task;
                await Task.WhenAny(operation, admitted).WaitAsync(Bound, CancellationToken.None);
                if (operation.IsCompleted && !admitted.IsCompleted)
                    await operation.WaitAsync(Bound, CancellationToken.None);
                Assert.True(admitted.IsCompletedSuccessfully);
                Assert.False(operation.IsCompleted);
                if (complete)
                {
                    SdkCall deletion = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.DeleteMessageBatchAsync));
                    var request = Assert.IsType<DeleteMessageBatchRequest>(deletion.Arguments[0]);
                    Assert.Equal(Fixture.QueueUrl, request.QueueUrl);
                    DeleteMessageBatchRequestEntry entry = Assert.Single(request.Entries);
                    Assert.Equal(Fixture.Receipt, entry.ReceiptHandle);
                    Assert.Equal("0", entry.Id);
                    Assert.True(deletion.Token.CanBeCanceled);
                    Assert.False(deletion.Token.IsCancellationRequested);
                    Assert.False(deletion.Raw.IsCompleted);
                    fixture.ReleaseDelete();
                    await deletion.Raw.WaitAsync(Bound, CancellationToken.None);
                }
                else
                {
                    SdkCall second = Assert.Single(fixture.Calls, call => ReferenceEquals(call.Raw, fixture.SecondRaw));
                    AssertVisibility(second, 1);
                    Assert.False(second.Token.IsCancellationRequested);
                    Assert.False(second.Raw.IsCompleted);
                    fixture.ReleaseSecond();
                    await second.Raw.WaitAsync(Bound, CancellationToken.None);
                }
                await operation.WaitAsync(Bound, CancellationToken.None);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.Equal(complete ? 1 : 2, fixture.VisibilityCalls);
                Assert.Equal(complete ? 1 : 0, fixture.DeleteCalls);
            }
            CompletionObservation order = await observation.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(hostile ? TaskStatus.Faulted : TaskStatus.RanToCompletion, order.PublicStatus);
            // FIRST completion-order oracle occurs before any fallback/retry. A late observer can miss a broken early exit.
            Assert.True(order.SdkTerminalAtPublicCompletion);
            Assert.Equal(TaskStatus.RanToCompletion, order.SdkStatusAtPublicCompletion);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                if (fixture is not null)
                {
                    fixture.Armed = false;
                    await CaptureAsync(() => { fixture.ReleaseAll(); return Task.CompletedTask; }, cleanup);
                }
                foreach (Task task in publicTasks)
                    await CaptureAsync(() => ObservePublicAsync(task, fixture), cleanup);
                if (observation is not null)
                    await CaptureAsync(() => observation.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (context is not null && fixture is not null)
                {
                    // A real public retry after release awaits the stored renewal worker; cleanup only, no idempotence assertion.
                    await CaptureAsync(async () =>
                    {
                        Task retry = complete ? context.CompleteAsync(CancellationToken.None)
                            : context.FaultedAsync(fixture.ConsumerFailure, CancellationToken.None);
                        publicTasks.Add(retry);
                        await retry.WaitAsync(Bound, CancellationToken.None);
                    }, cleanup);
                }
                if (fixture is not null)
                {
                    await CaptureAsync(() => fixture.DisposeCallbackAsync().WaitAsync(Bound, CancellationToken.None), cleanup);
                    // Observe public/retry work before taking the final snapshot of every actually recorded SDK task.
                    foreach (SdkCall call in fixture.Calls.ToArray())
                        await CaptureAsync(() => call.Raw.WaitAsync(Bound, CancellationToken.None), cleanup);
                    await fixture.RetireAsync(cleanup);
                }
            }
            catch (Exception exception) { cleanup.Add(exception); }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null)
                cleanup.Insert(0, primary);
            throw new AggregateException("Settlement callback characterization and actual fixture retirement failed.", cleanup);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    sealed record CompletionObservation(TaskStatus PublicStatus, bool SdkTerminalAtPublicCompletion, TaskStatus SdkStatusAtPublicCompletion);

    static void AssertVisibility(SdkCall call, int seconds)
    {
        var request = Assert.IsType<ChangeMessageVisibilityRequest>(call.Arguments[0]);
        Assert.Equal(Fixture.QueueUrl, request.QueueUrl);
        Assert.Equal(Fixture.Receipt, request.ReceiptHandle);
        Assert.Equal(seconds, request.VisibilityTimeout);
        Assert.True(call.Token.CanBeCanceled);
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObservePublicAsync(Task task, Fixture? fixture)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (AggregateException exception) when (task.IsFaulted && fixture is not null
            && exception.Flatten().InnerExceptions.Count == 1
            && ReferenceEquals(exception.Flatten().InnerExceptions[0], fixture.CallbackFailure)) { }
    }

    sealed record SdkCall(string Name, object?[] Arguments, CancellationToken Token, Task Raw);
    sealed class NameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => typeof(T).Name;
    }

    sealed class Fixture
    {
        public const string QueueName = "renewal-diagnostic-queue";
        public const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/renewal-diagnostic-queue";
        public const string Receipt = "unique-original-receipt";
        public static readonly Uri InputAddress = new("amazonsqs://us-east-1/renewal-diagnostic-queue");
        const string QueueArn = "arn:aws:sqs:us-east-1:123456789012:renewal-diagnostic-queue";
        readonly bool _throwCallback;
        readonly TaskCompletionSource<ChangeMessageVisibilityResponse> _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<ChangeMessageVisibilityResponse> _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<DeleteMessageBatchResponse> _delete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration _renewalCancellationRegistration;
        DeleteMessageBatchRequest? _deleteRequest;
        int _releaseAll;
        int _visibilityCalls;
        int _deleteCalls;
        int _sqsDisposals;
        int _snsDisposals;
        public readonly CancellationTokenSource Lifetime = new();
        public readonly ConcurrentQueue<SdkCall> Calls = new();
        public readonly TaskCompletionSource FirstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource SecondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource DeleteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource CallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly IOException CallbackFailure = new("unique real SDK token callback failure");
        public volatile bool Armed = true;
        int _callbackCalls;
        public int CallbackCalls => Volatile.Read(ref _callbackCalls);
        public readonly InvalidOperationException ConsumerFailure = new("unique original consumer failure");
        public readonly IAmazonSQS Sqs;
        public readonly IAmazonSimpleNotificationService Sns;
        public readonly AmazonSqsConnectionContext Connection;
        public readonly ClientContext Client;
        public readonly QueueReceiveSettings Settings;
        public int VisibilityCalls => Volatile.Read(ref _visibilityCalls);
        public int DeleteCalls => Volatile.Read(ref _deleteCalls);
        public Task FirstRaw => _first.Task;
        public Task SecondRaw => _second.Task;

        public Fixture(bool throwCallback)
        {
            _throwCallback = throwCallback;
            Sqs = InterfaceProxy<IAmazonSQS>.Create(HandleSqs);
            Sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) =>
            {
                if (method.Name != nameof(IDisposable.Dispose))
                    throw new NotSupportedException("Unexpected SNS operation: " + method);
                Interlocked.Increment(ref _snsDisposals);
                return null;
            });
            var host = new AmazonSqsHostConfigurator(new Uri("amazonsqs://us-east-1/"));
            host.ClientFactories(() => Sqs, () => Sns);
            var topology = new AmazonSqsTopologyConfiguration(new ViciOne.ServiceBus.Topology.MessageTopology(new NameFormatter()));
            var bus = new AmazonSqsBusConfiguration(topology);
            bus.HostConfiguration.Settings = host.Settings;
            bus.HostConfiguration.LogContext = LogContext.Current;
            Settings = new QueueReceiveSettings(bus.CreateEndpointConfiguration(false), QueueName, true, false)
            {
                QueueUrl = QueueUrl,
                VisibilityTimeout = 0,
                RedeliverVisibilityTimeout = 1,
                MaxVisibilityTimeoutRenewal = 60,
                MaxVisibilityTimeout = TimeSpan.FromHours(12)
            };
            Connection = new AmazonSqsConnectionContext(host.Settings.CreateConnection(), bus.HostConfiguration, Lifetime.Token);
            Client = Connection.CreateClientContext(Lifetime.Token);
        }

        object? HandleSqs(MethodInfo method, object?[]? arguments)
        {
            if (method.Name == nameof(IDisposable.Dispose))
            {
                Interlocked.Increment(ref _sqsDisposals);
                return null;
            }
            object?[] args = arguments ?? throw new InvalidOperationException("Missing SDK arguments.");
            if (args.Length == 0 || args[^1] is not CancellationToken token)
                throw new NotSupportedException("Unexpected SDK overload: " + method);
            Task raw;
            switch (method.Name)
            {
                case nameof(IAmazonSQS.GetQueueUrlAsync):
                    raw = Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
                    break;
                case nameof(IAmazonSQS.GetQueueAttributesAsync):
                    raw = Task.FromResult(new GetQueueAttributesResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn }
                    });
                    break;
                case nameof(IAmazonSQS.ChangeMessageVisibilityAsync):
                    int ordinal = Interlocked.Increment(ref _visibilityCalls);
                    if (ordinal == 1)
                    {
                        _renewalCancellationRegistration = token.Register(() =>
                        {
                            Interlocked.Increment(ref _callbackCalls);
                            CallbackEntered.TrySetResult();
                            if (Armed && _throwCallback)
                                throw CallbackFailure;
                        });
                        raw = _first.Task;
                    }
                    else if (Volatile.Read(ref _releaseAll) != 0)
                        raw = Task.FromResult(VisibilityResponse());
                    else if (ordinal == 2)
                        raw = _second.Task;
                    else
                        throw new NotSupportedException("Unexpected extra visibility request before retirement.");
                    if (Volatile.Read(ref _releaseAll) != 0)
                    {
                        _first.TrySetResult(VisibilityResponse());
                        _second.TrySetResult(VisibilityResponse());
                    }
                    break;
                case nameof(IAmazonSQS.DeleteMessageBatchAsync):
                    _deleteRequest = args[0] as DeleteMessageBatchRequest
                        ?? throw new NotSupportedException("Unexpected delete SDK request overload.");
                    Interlocked.Increment(ref _deleteCalls);
                    raw = Volatile.Read(ref _releaseAll) != 0
                        ? Task.FromResult(DeleteResponse(_deleteRequest)) : _delete.Task;
                    break;
                default:
                    throw new NotSupportedException("Unexpected SQS operation: " + method);
            }
            Calls.Enqueue(new SdkCall(method.Name, args.ToArray(), token, raw));
            if (method.Name == nameof(IAmazonSQS.ChangeMessageVisibilityAsync))
            {
                if (VisibilityCalls == 1)
                    FirstEntered.TrySetResult();
                else
                    SecondEntered.TrySetResult();
            }
            if (method.Name == nameof(IAmazonSQS.DeleteMessageBatchAsync))
                DeleteEntered.TrySetResult();
            return raw;
        }

        public void ReleaseFirst() => _first.TrySetResult(VisibilityResponse());
        public void ReleaseSecond() => _second.TrySetResult(VisibilityResponse());
        public Task DisposeCallbackAsync() => _renewalCancellationRegistration.DisposeAsync().AsTask();
        public void ReleaseDelete()
        {
            DeleteMessageBatchRequest? request = Volatile.Read(ref _deleteRequest);
            if (request is not null)
                _delete.TrySetResult(DeleteResponse(request));
        }
        static DeleteMessageBatchResponse DeleteResponse(DeleteMessageBatchRequest request) => new()
        {
            HttpStatusCode = HttpStatusCode.OK,
            Successful = request.Entries.Select(entry => new DeleteMessageBatchResultEntry { Id = entry.Id }).ToList(),
            Failed = []
        };
        public void ReleaseAll()
        {
            Interlocked.Exchange(ref _releaseAll, 1);
            _first.TrySetResult(VisibilityResponse());
            _second.TrySetResult(VisibilityResponse());
            ReleaseDelete();
        }
        static ChangeMessageVisibilityResponse VisibilityResponse() => new() { HttpStatusCode = HttpStatusCode.OK };

        public async Task RetireAsync(List<Exception> failures)
        {
            await CaptureAsync(() => Connection.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), failures);
            await CaptureAsync(() => DisposeCallbackAsync().WaitAsync(Bound, CancellationToken.None), failures);
            await CaptureAsync(() => { Lifetime.Dispose(); return Task.CompletedTask; }, failures);
            if (Volatile.Read(ref _sqsDisposals) == 0)
                await CaptureAsync(() => { Sqs.Dispose(); return Task.CompletedTask; }, failures);
            if (Volatile.Read(ref _snsDisposals) == 0)
                await CaptureAsync(() => { Sns.Dispose(); return Task.CompletedTask; }, failures);
        }
    }
}
