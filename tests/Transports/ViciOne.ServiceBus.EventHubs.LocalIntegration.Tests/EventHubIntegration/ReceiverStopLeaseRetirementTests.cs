using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class ReceiverStopLeaseRetirementTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "sdk-stop-failure-preserves-cause-and-retires-acquired-processor-lease")]
    public async Task StopAsync_SdkStopOutcomePreservesCauseAndRetiresItsAcquiredLeaseAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var client = new ControlledProcessorClient();
        var failure = new IOException("unique actual SDK processor stop failure");
        EventHubProcessorContext? inner = null;
        ProcessorLockContext? receipt = null;
        ProcessorLockContext? previousReceipt = null;
        object? acquiredBuilder = null;
        EventHubDataReceiver? receiver = null;
        Task? stop = null;
        EventProcessorClient? newLease = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        int acquisitions = 0;
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            ILogContext log = LogContext.Current!;
            IHostConfiguration host = InterfaceProxy<IHostConfiguration>.Create((method, _) =>
                method.Name == "get_ReceiveLogContext" ? log : throw new NotSupportedException(method.ToString()));
            inner = new EventHubProcessorContext(host, client, null, null, CancellationToken.None);
            ProcessorContext forwarding = InterfaceProxy<ProcessorContext>.Create((method, args) =>
            {
                if (method.Name == "GetClient")
                {
                    acquiredBuilder = args![0];
                    previousReceipt = receipt;
                    EventProcessorClient leased = inner.GetClient((ProcessorClientBuilderContext)acquiredBuilder!);
                    receipt = acquiredBuilder as ProcessorLockContext;
                    Interlocked.Increment(ref acquisitions);
                    return leased;
                }
                if (method.Name == "ReleaseClient") { inner.ReleaseClient(); return null; }
                if (method.Name == "get_LogContext") return inner.LogContext;
                throw new NotSupportedException(method.ToString());
            });
            IReceivePipeDispatcher dispatcher = InterfaceProxy<IReceivePipeDispatcher>.Create((method, _) => method.Name switch
            {
                "add_ZeroActivity" or "remove_ZeroActivity" => null,
                "get_ActiveDispatchCount" or "get_MaxConcurrentDispatchCount" => 0,
                "get_DispatchCount" => 0L,
                _ => throw new NotSupportedException(method.ToString())
            });
            ReceiveEndpointContext endpoint = InterfaceProxy<ReceiveEndpointContext>.Create((method, args) =>
            {
                if (method.Name == "TryGetPayload" && method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(TimeProvider))
                { args![0] = null; return false; }
                return method.Name switch
                {
                    "CreateReceivePipeDispatcher" => dispatcher,
                    "get_InputAddress" => new Uri("sb://unit.servicebus.invalid/orders"),
                    "get_LogContext" => log,
                    "get_ConsumerStopTimeout" or "get_StopTimeout" => null,
                    _ => throw new NotSupportedException(method.ToString())
                };
            });
            receiver = new EventHubDataReceiver(new Settings(), endpoint, forwarding);
            await client.StartEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, client.StartCalls);
            Assert.True(client.StartToken.CanBeCanceled);
            Assert.False(client.StartRaw.IsCompleted);
            Assert.Equal(1, Volatile.Read(ref acquisitions));
            Assert.Null(previousReceipt);
            Assert.Same(receipt, Assert.IsType<ProcessorLockContext>(acquiredBuilder));
            Assert.NotNull(receipt);
            Assert.Same(client, receipt.Client);
            client.StartRelease.TrySetResult();
            await client.StartRaw.WaitAsync(Bound, CancellationToken.None);
            await receiver.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.False(receiver.Completed.IsCompleted);
            stop = receiver.StopAsync("Public SDK-stop lease-retirement control", CancellationToken.None);
            await Task.WhenAny(stop, client.StopEntered.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(client.StopEntered.Task.IsCompletedSuccessfully);
            Assert.Equal(1, client.StopCalls);
            Assert.Equal(CancellationToken.None, client.StopToken);
            Assert.False(client.StopRaw.IsCompleted);
            client.ReleaseStop(hostile, failure);
            Exception? rawFailure = await Record.ExceptionAsync(() => client.StopRaw.WaitAsync(Bound, CancellationToken.None));
            Exception? stopFailure = await Record.ExceptionAsync(() => stop.WaitAsync(Bound, CancellationToken.None));
            Exception? completedFailure = await Record.ExceptionAsync(() => receiver.Completed.WaitAsync(Bound, CancellationToken.None));
            if (hostile)
            {
                Assert.Same(failure, rawFailure);
                Assert.Same(failure, stopFailure);
                Assert.Same(failure, completedFailure);
                Assert.True(client.StopRaw.IsFaulted);
                Assert.True(stop.IsFaulted);
                Assert.True(receiver.Completed.IsFaulted);
            }
            else
            {
                Assert.Null(rawFailure);
                Assert.Null(stopFailure);
                Assert.Null(completedFailure);
                Assert.True(client.StopRaw.IsCompletedSuccessfully);
                Assert.True(stop.IsCompletedSuccessfully);
                Assert.True(receiver.Completed.IsCompletedSuccessfully);
            }
            Assert.True(client.StartRaw.IsCompletedSuccessfully);
            Assert.True(receiver.Ready.IsCompletedSuccessfully);
            Assert.Equal(1, client.StartCalls);
            Assert.Equal(1, client.StopCalls);
            Exception? leaseFailure = Record.Exception(() => { newLease = inner.GetClient(new EmptyBuilder()); });
            // FIRST causal boundary: all actual SDK/public stop outcomes were observed before probing the public lease.
            Assert.Null(leaseFailure);
            Assert.Same(client, newLease);
            inner.ReleaseClient();
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                await CaptureAsync(() => { client.StartRelease.TrySetResult(); return Task.CompletedTask; }, cleanup);
                if (receiver is not null && stop is null)
                    await CaptureAsync(() => { stop = receiver.StopAsync("Independent first receiver stop", CancellationToken.None); return Task.CompletedTask; }, cleanup);
                await CaptureAsync(() => { client.ReleaseStop(hostile && client.StopCalls != 0, failure); return Task.CompletedTask; }, cleanup);
                if (receiver is not null)
                {
                    await CaptureAsync(() => receiver.Ready.WaitAsync(Bound, CancellationToken.None), cleanup);
                    if (stop is not null) await CaptureAsync(() => ObserveKnownAsync(stop, failure), cleanup);
                    await CaptureAsync(() => ObserveKnownAsync(receiver.Completed, failure), cleanup);
                }
                // Only first public Stop is used; never mistake a repeated Stop's old Completed signal for a new drain.
                foreach (Task actual in client.ActualTasks.ToArray())
                    await CaptureAsync(() => ObserveKnownAsync(actual, failure), cleanup);
                if (inner is not null) await CaptureAsync(() => { inner.ReleaseClient(); return Task.CompletedTask; }, cleanup);
                if (receipt is not null) await CaptureAsync(() => receipt.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), cleanup);
                // The real public receipt releases its lease/pending registration. Unexposed Original CTS/semaphore/Agent CTS
                // remain fixture-local managed objects: no complete Dispose, WaitHandle, private recovery or GC claim.
            }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Receiver stop control and independent receipt retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> errors)
    {
        try { await action(); }
        catch (Exception exception) { errors.Add(exception); }
    }
    static async Task ObserveKnownAsync(Task task, Exception known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, known)) { }
    }
    sealed class ControlledProcessorClient : EventProcessorClient
    {
        public readonly TaskCompletionSource StartEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StopEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StartRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StopRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentQueue<Task> ActualTasks = new();
        int _startCalls, _stopCalls;
        public int StartCalls => Volatile.Read(ref _startCalls);
        public int StopCalls => Volatile.Read(ref _stopCalls);
        public CancellationToken StartToken, StopToken;
        public Task StartRaw => StartRelease.Task;
        public Task StopRaw => StopRelease.Task;
        public void ReleaseStop(bool hostile, Exception failure)
        {
            if (hostile) StopRelease.TrySetException(failure);
            else StopRelease.TrySetResult();
        }
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            StartToken = cancellationToken;
            Interlocked.Increment(ref _startCalls);
            ActualTasks.Enqueue(StartRaw);
            StartEntered.TrySetResult();
            return StartRaw;
        }
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            StopToken = cancellationToken;
            Interlocked.Increment(ref _stopCalls);
            ActualTasks.Enqueue(StopRaw);
            StopEntered.TrySetResult();
            return StopRaw;
        }
    }
    sealed class Settings : ReceiveSettings
    {
        public string ConsumerGroup => "group";
        public string ContainerName => "checkpoints";
        public string EventHubName => "orders";
        public ushort CheckpointMessageLimit => 1;
        public ushort CheckpointMessageCount => 1;
        public int PrefetchCount => 3;
        public TimeSpan CheckpointInterval => TimeSpan.FromMinutes(30);
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
    }
    sealed class EmptyBuilder : ProcessorClientBuilderContext
    {
        public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected real partition activation.");
        public Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected real partition activation.");
    }
    public class InterfaceProxy<T> : DispatchProxy where T : class
    {
        Func<MethodInfo, object?[]?, object?> _handler = null!;
        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T value = Create<T, InterfaceProxy<T>>();
            ((InterfaceProxy<T>)(object)value)._handler = handler;
            return value;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args) => _handler(method ?? throw new InvalidOperationException("Missing public SPI method."), args);
    }
}
