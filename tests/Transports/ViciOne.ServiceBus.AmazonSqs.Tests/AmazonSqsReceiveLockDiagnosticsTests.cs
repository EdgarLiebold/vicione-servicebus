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

public sealed class AmazonSqsReceiveLockDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "complete-own-renewal-error-preserves-real-worker-join-and-delete")]
    public Task Complete_OptionalRenewalDiagnosticPreservesActualDeleteAsync(bool hostile)
        => RunAsync(2, true, hostile);

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "faulted-own-renewal-redelivery-diagnostics-preserve-real-settlement")]
    public Task Faulted_OptionalDiagnosticsPreserveActualRenewalAndRedeliveryAsync(int site, bool hostile)
        => RunAsync(site, false, hostile);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "maximum-own-warning-preserves-real-batch-delete")]
    public async Task MaximumVisibility_OptionalDiagnosticsPreserveActualDeleteAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var maximum = TimeSpan.FromMilliseconds(500);
        var logger = new MaximumLogger(hostile, maximum);
        Fixture? fixture = null;
        AmazonSqsReceiveLockContext? context = null;
        Task? operation = null;
        var publicTasks = new List<Task>();
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture = new Fixture(2);
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
            fixture.Settings.MaxVisibilityTimeout = maximum;
            Assert.Equal(0, fixture.Settings.VisibilityTimeout);
            Assert.Equal(60, fixture.Settings.MaxVisibilityTimeoutRenewal);
            // A valid subsecond maximum reaches the public System-time branch without sleeping or a native renewal.
            context = new AmazonSqsReceiveLockContext(Fixture.InputAddress,
                new Message { ReceiptHandle = Fixture.Receipt }, fixture.Settings, fixture.Client, fixture.Lifetime.Token);
            Exception? invocationFailure = Record.Exception(() => { operation = context.CompleteAsync(CancellationToken.None); });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            publicTasks.Add(operation);
            await Task.WhenAny(operation, fixture.DeleteEntered.Task).WaitAsync(Bound, CancellationToken.None);
            logger.AssertEmissions();
            Assert.Equal(0, fixture.VisibilityCalls);
            Assert.DoesNotContain(fixture.Calls, call => call.Name == nameof(IAmazonSQS.ChangeMessageVisibilityAsync));
            if (operation.IsCompleted && !fixture.DeleteEntered.Task.IsCompleted)
            {
                Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
                Assert.True(operation.IsFaulted);
                Assert.Same(logger.Failure, observed);
            }
            // FIRST causal oracle precedes any fallback settlement or raw deletion gate release.
            Assert.Equal(1, fixture.DeleteCalls);
            Assert.False(operation.IsCompleted);
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
            await operation.WaitAsync(Bound, CancellationToken.None);
            await deletion.Raw.WaitAsync(Bound, CancellationToken.None);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.True(deletion.Raw.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.DeleteCalls);
            Assert.Equal(0, fixture.VisibilityCalls);
            Task? validation = null;
            Exception? lost = Record.Exception(() => { validation = context.ValidateLockStatusAsync(CancellationToken.None); });
            Assert.Contains(Fixture.Receipt, Assert.IsType<TransportException>(lost).Message, StringComparison.Ordinal);
            Assert.Null(validation);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                if (fixture is not null)
                    await CaptureAsync(() => { fixture.ReleaseAll(); return Task.CompletedTask; }, cleanup);
                if (context is not null && operation is null)
                    await CaptureAsync(() =>
                    {
                        publicTasks.Add(context.CompleteAsync(CancellationToken.None));
                        return Task.CompletedTask;
                    }, cleanup);
                foreach (Task task in publicTasks)
                    await CaptureAsync(() => ObservePublicAsync(task, logger.Failure), cleanup);
                if (fixture is not null)
                {
                    // No FirstEntered or renewal-cancellation signal is awaited: neither operation was admitted here.
                    foreach (SdkCall call in fixture.Calls.ToArray())
                        await CaptureAsync(() => ObserveRawAsync(call, fixture), cleanup);
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
            throw new AggregateException("Maximum visibility control and fixture retirement failed.", cleanup);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task RunAsync(int site, bool complete, bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger(site, hostile);
        Fixture? fixture = null;
        AmazonSqsReceiveLockContext? context = null;
        Task? operation = null;
        var publicTasks = new List<Task>();
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture = new Fixture(site);
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
            Assert.Empty(logger.Emissions);
            Exception? invocationFailure = Record.Exception(() =>
            {
                operation = complete ? context.CompleteAsync(CancellationToken.None)
                    : context.FaultedAsync(fixture.ConsumerFailure, CancellationToken.None);
            });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            publicTasks.Add(operation);
            await fixture.RenewalCancellationObserved.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.True(first.Token.IsCancellationRequested);
            Assert.False(first.Raw.IsCompleted);
            Assert.False(operation.IsCompleted);
            // Cancellation notification alone is not worker completion; the actual SDK task remains held.
            fixture.ReleaseFirst();
            await ObserveRawAsync(first, fixture);
            if (site == 3)
                Assert.True(first.Raw.IsCompletedSuccessfully);
            else
            {
                Assert.True(first.Raw.IsFaulted);
                Assert.Same(fixture.FirstFailure, Assert.Single(first.Raw.Exception!.InnerExceptions));
            }

            if (site == 3)
            {
                await fixture.SecondEntered.Task.WaitAsync(Bound, CancellationToken.None);
                SdkCall second = Assert.Single(fixture.Calls, call => ReferenceEquals(call.Raw, fixture.SecondRaw));
                AssertVisibility(second, 1);
                Assert.False(second.Raw.IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Empty(logger.Emissions);
                fixture.FaultSecond();
                await ObserveRawAsync(second, fixture);
                Assert.True(second.Raw.IsFaulted);
                Assert.Same(fixture.SecondFailure, Assert.Single(second.Raw.Exception!.InnerExceptions));
                Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
                logger.AssertEmission(fixture);
                Assert.Equal(2, fixture.VisibilityCalls);
                if (observed is not null)
                    Assert.Same(logger.Failure, observed);
                Assert.Null(observed);
            }
            else
            {
                Task continuation = complete ? fixture.DeleteEntered.Task : fixture.SecondEntered.Task;
                await Task.WhenAny(operation, continuation).WaitAsync(Bound, CancellationToken.None);
                logger.AssertEmission(fixture);
                if (operation.IsCompleted && !continuation.IsCompleted)
                {
                    Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
                    Assert.True(operation.IsFaulted);
                    Assert.Same(logger.Failure, observed);
                }
                // The causal admission count is checked before fixture fallback or gate release.
                Assert.Equal(complete ? 1 : 2, complete ? fixture.DeleteCalls : fixture.VisibilityCalls);
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
                    // Native batch deletion uses the queue owner's lifetime token, not the public caller token.
                    fixture.ReleaseDelete();
                }
                else
                {
                    SdkCall second = Assert.Single(fixture.Calls, call => ReferenceEquals(call.Raw, fixture.SecondRaw));
                    AssertVisibility(second, 1);
                    Assert.False(second.Token.IsCancellationRequested);
                    Assert.False(second.Raw.IsCompleted);
                    fixture.ReleaseSecond();
                }
                await operation.WaitAsync(Bound, CancellationToken.None);
            }
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(complete ? 1 : 2, fixture.VisibilityCalls);
            Assert.Equal(complete ? 1 : 0, fixture.DeleteCalls);
            Task? validation = null;
            Exception? lost = Record.Exception(() => { validation = context.ValidateLockStatusAsync(CancellationToken.None); });
            Assert.Contains(Fixture.Receipt, Assert.IsType<TransportException>(lost).Message, StringComparison.Ordinal);
            Assert.Null(validation);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                if (fixture is not null)
                    await CaptureAsync(() => { fixture.ReleaseAll(); return Task.CompletedTask; }, cleanup);
                if (context is not null && operation is null)
                    await CaptureAsync(() =>
                    {
                        publicTasks.Add(context.FaultedAsync(fixture!.ConsumerFailure, CancellationToken.None));
                        return Task.CompletedTask;
                    }, cleanup);
                // Join all actual public operations before enumerating every recorded raw SDK task.
                foreach (Task task in publicTasks)
                    await CaptureAsync(() => ObservePublicAsync(task, logger.Failure), cleanup);
                if (fixture is not null)
                {
                    foreach (SdkCall call in fixture.Calls.ToArray())
                        await CaptureAsync(() => ObserveRawAsync(call, fixture), cleanup);
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
            throw new AggregateException("Public settlement control and fixture retirement failed.", cleanup);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

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

    static async Task ObservePublicAsync(Task task, Exception known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, known)) { }
    }

    static async Task ObserveRawAsync(SdkCall call, Fixture fixture)
    {
        try { await call.Raw.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (call.Raw.IsFaulted
            && (ReferenceEquals(call.Raw, fixture.FirstRaw) && ReferenceEquals(exception, fixture.FirstFailure)
                || ReferenceEquals(call.Raw, fixture.SecondRaw) && ReferenceEquals(exception, fixture.SecondFailure))) { }
    }

    sealed record Emission(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class SelectedLogger(int site, bool hostile) : ILogger
    {
        public readonly IOException Failure = new("unique selected SQS receive-lock diagnostic failure");
        public readonly ConcurrentQueue<Emission> Emissions = new();
        public volatile bool Armed = true;
        int _throws;
        string Template => site switch
        {
            0 => "Message no longer in flight: {ReceiptHandle}",
            1 => "Message receipt handle is invalid: {ReceiptHandle}",
            2 => "Failed to extend message {ReceiptHandle} visibility ({ElapsedTime})",
            3 => "ChangeMessageVisibility failed: {ReceiptHandle}, Original Exception: {Exception}",
            _ => throw new ArgumentOutOfRangeException(nameof(site))
        };
        public bool IsEnabled(LogLevel level) => level is LogLevel.Warning or LogLevel.Error;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (level != (site < 2 ? LogLevel.Warning : LogLevel.Error)
                || !values.TryGetValue("{OriginalFormat}", out var template) || !Equals(template, Template))
                return;
            Emissions.Enqueue(new Emission(values, exception));
            if (Armed && hostile)
            {
                Interlocked.Increment(ref _throws);
                throw Failure;
            }
        }
        public void AssertEmission(Fixture fixture)
        {
            Emission emission = Assert.Single(Emissions);
            Assert.Equal(Template, emission.Fields["{OriginalFormat}"]);
            Assert.Equal(Fixture.Receipt, emission.Fields["ReceiptHandle"]);
            Assert.Same(site == 3 ? fixture.SecondFailure : fixture.FirstFailure, emission.Cause);
            if (site == 2)
                Assert.IsType<TimeSpan>(emission.Fields["ElapsedTime"]);
            if (site == 3)
                Assert.Same(fixture.ConsumerFailure, emission.Fields["Exception"]);
            Assert.Equal(hostile ? 1 : 0, Volatile.Read(ref _throws));
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
    }

    sealed class MaximumLogger(bool hostile, TimeSpan maximum) : ILogger
    {
        const string MaximumTemplate = "Maximum visibility timeout {MaxVisibilityTimeout} for message {ReceiptHandle} reached.";
        const string ErrorTemplate = "Failed to extend message {ReceiptHandle} visibility ({ElapsedTime})";
        public readonly IOException Failure = new("unique maximum visibility owning diagnostic failure");
        readonly ConcurrentQueue<Emission> _emissions = new();
        public volatile bool Armed = true;
        int _throws;
        public bool IsEnabled(LogLevel level) => level is LogLevel.Warning or LogLevel.Error;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var template)
                || !(level == LogLevel.Warning && Equals(template, MaximumTemplate)
                    || level == LogLevel.Error && Equals(template, ErrorTemplate)))
                return;
            _emissions.Enqueue(new Emission(values, exception));
            if (Armed && hostile)
            {
                Interlocked.Increment(ref _throws);
                throw Failure;
            }
        }
        public void AssertEmissions()
        {
            Emission[] emissions = _emissions.ToArray();
            Emission warning = Assert.Single(emissions, emission => Equals(emission.Fields["{OriginalFormat}"], MaximumTemplate));
            Assert.Equal(maximum, Assert.IsType<TimeSpan>(warning.Fields["MaxVisibilityTimeout"]));
            Assert.Equal(Fixture.Receipt, warning.Fields["ReceiptHandle"]);
            Assert.Null(warning.Cause);
            Emission[] errors = emissions.Where(emission => Equals(emission.Fields["{OriginalFormat}"], ErrorTemplate)).ToArray();
            if (errors.Length != 0)
            {
                Assert.True(hostile);
                Emission error = Assert.Single(errors);
                Assert.Same(Failure, error.Cause);
                Assert.Equal(Fixture.Receipt, error.Fields["ReceiptHandle"]);
                Assert.IsType<TimeSpan>(error.Fields["ElapsedTime"]);
            }
            Assert.Equal(1 + errors.Length, emissions.Length);
            Assert.Equal(hostile ? emissions.Length : 0, Volatile.Read(ref _throws));
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
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
        readonly int _site;
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
        public readonly TaskCompletionSource RenewalCancellationObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly Exception FirstFailure;
        public readonly IOException SecondFailure = new("unique raw SDK redelivery failure");
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

        public Fixture(int site)
        {
            _site = site;
            FirstFailure = site switch
            {
                0 => new MessageNotInflightException("unique raw SDK not-inflight failure"),
                1 => new ReceiptHandleIsInvalidException("unique raw SDK invalid-receipt failure"),
                _ => new IOException("unique raw SDK renewal failure")
            };
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
                        _renewalCancellationRegistration = token.Register(() => RenewalCancellationObserved.TrySetResult());
                        raw = _first.Task;
                    }
                    else if (ordinal == 2)
                        raw = _second.Task;
                    else
                        throw new NotSupportedException("Unexpected third visibility request.");
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
                    raw = _delete.Task;
                    if (Volatile.Read(ref _releaseAll) != 0)
                        ReleaseDelete();
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

        public void ReleaseFirst()
        {
            if (_site == 3)
                _first.TrySetResult(VisibilityResponse());
            else
                _first.TrySetException(FirstFailure);
        }
        public void ReleaseSecond() => _second.TrySetResult(VisibilityResponse());
        public void FaultSecond() => _second.TrySetException(SecondFailure);
        public void ReleaseDelete()
        {
            DeleteMessageBatchRequest? request = Volatile.Read(ref _deleteRequest);
            if (request is not null)
                _delete.TrySetResult(new DeleteMessageBatchResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Successful = request.Entries.Select(entry => new DeleteMessageBatchResultEntry { Id = entry.Id }).ToList(),
                    Failed = []
                });
        }
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
            await CaptureAsync(() => { _renewalCancellationRegistration.Dispose(); return Task.CompletedTask; }, failures);
            await CaptureAsync(() => { Lifetime.Dispose(); return Task.CompletedTask; }, failures);
            if (Volatile.Read(ref _sqsDisposals) == 0)
                await CaptureAsync(() => { Sqs.Dispose(); return Task.CompletedTask; }, failures);
            if (Volatile.Read(ref _snsDisposals) == 0)
                await CaptureAsync(() => { Sns.Dispose(); return Task.CompletedTask; }, failures);
        }
    }
}
