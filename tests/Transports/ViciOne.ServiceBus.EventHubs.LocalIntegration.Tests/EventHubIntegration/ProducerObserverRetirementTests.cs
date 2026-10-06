using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class ProducerObserverRetirementTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-RETIREMENT", "observer-release-failure-does-not-skip-adopted-agent-stop")]
    public async Task ProducerDispose_ObserverFailureDoesNotSkipAdoptedAgentStopAsync(bool hostile)
    {
        var observers = new SendObservable();
        var handle = new ControlledHandle(observers.Connect(new EmptyObserver()), observers, hostile);
        var child = new ControlledAgent();
        EventHubSendTransportContext context = InterfaceProxy<EventHubSendTransportContext>.Create((method, _) =>
            method.Name == nameof(EventHubSendTransportContext.GetAgentHandles)
                ? new IAgent[] { child }
                : throw new NotSupportedException(method.ToString()));
        var producer = new EventHubProducer(context, handle);
        Task? disposal = null;
        Task? fallback = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            Assert.Equal(1, observers.Count);
            Assert.Equal(1L, producer.TotalCount);
            Assert.Equal(1, producer.PeakActiveCount);
            await producer.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.False(child.Completed.IsCompleted);
            disposal = producer.DisposeAsync().AsTask();
            await Task.WhenAny(disposal, child.StopEntered.Task).WaitAsync(Bound, CancellationToken.None);
            if (child.StopEntered.Task.IsCompletedSuccessfully)
            {
                Assert.False(child.RawStop.IsCompleted);
                Assert.False(disposal.IsCompleted);
                Assert.NotNull(child.Context);
                Assert.Equal(CancellationToken.None, child.Token);
                child.StopRelease.TrySetResult();
                await child.RawStop.WaitAsync(Bound, CancellationToken.None);
                Assert.False(child.Completed.IsCompleted);
                Assert.False(disposal.IsCompleted);
                child.CompletionRelease.TrySetResult();
            }
            Exception? observed = await Record.ExceptionAsync(() => disposal.WaitAsync(Bound, CancellationToken.None));
            Assert.Equal(0, observers.Count);
            Assert.Equal(1, handle.Calls);
            Assert.Equal(0, handle.CountAfterRelease);
            if (observed is not null) Assert.Same(handle.Failure, observed);
            // FIRST causal boundary: terminal Dispose cannot bypass the adopted child's Stop admission.
            Assert.Equal(1, child.StopCalls);
            if (hostile)
            {
                Assert.Same(handle.Failure, observed);
                Assert.True(disposal.IsFaulted);
            }
            else
            {
                Assert.Null(observed);
                Assert.True(disposal.IsCompletedSuccessfully);
            }
            Assert.True(child.RawStop.IsCompletedSuccessfully);
            Assert.True(child.Completed.IsCompletedSuccessfully);
            await producer.Completed.WaitAsync(Bound, CancellationToken.None);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            handle.Armed = false;
            await CaptureAsync(() => { handle.ReleaseInner(); return Task.CompletedTask; }, cleanup);
            // Start fallback while Completed remains held: otherwise the supervisor can remove the child before Stop.
            if (child.StopCalls == 0)
                await CaptureAsync(() => { fallback = producer.StopAsync(new FixtureStopContext(), CancellationToken.None); return Task.CompletedTask; }, cleanup);
            await CaptureAsync(() => { child.StopRelease.TrySetResult(); return Task.CompletedTask; }, cleanup);
            await CaptureAsync(() => { child.CompletionRelease.TrySetResult(); return Task.CompletedTask; }, cleanup);
            if (disposal is not null) await CaptureAsync(() => ObserveKnownAsync(disposal, handle.Failure), cleanup);
            if (fallback is not null) await CaptureAsync(() => fallback.WaitAsync(Bound, CancellationToken.None), cleanup);
            if (child.StopEntered.Task.IsCompletedSuccessfully)
                await CaptureAsync(() => child.RawStop.WaitAsync(Bound, CancellationToken.None), cleanup);
            await CaptureAsync(() => child.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
            await CaptureAsync(() => producer.Ready.WaitAsync(Bound, CancellationToken.None), cleanup);
            await CaptureAsync(() => producer.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Producer control and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveKnownAsync(Task task, Exception known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, known)) { }
    }

    sealed class FixtureStopContext : BasePipeContext, StopContext
    {
        public string Reason => "Independent fixture retirement";
    }

    sealed class ControlledAgent : IAgent
    {
        public readonly TaskCompletionSource StopEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StopRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource CompletionRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _stopCalls;
        public int StopCalls => Volatile.Read(ref _stopCalls);
        public StopContext? Context;
        public CancellationToken Token;
        public Task RawStop => StopRelease.Task;
        public Task Ready => Task.CompletedTask;
        public Task Completed => CompletionRelease.Task;
        public CancellationToken Stopping => CancellationToken.None;
        public CancellationToken Stopped => CancellationToken.None;
        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            Context = context;
            Token = cancellationToken;
            Interlocked.Increment(ref _stopCalls);
            StopEntered.TrySetResult();
            return RawStop;
        }
    }

    sealed class ControlledHandle(ConnectHandle inner, SendObservable observers, bool hostile) : ConnectHandle
    {
        public readonly IOException Failure = new("unique returned observer Disconnect failure");
        public bool Armed = true;
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public int CountAfterRelease = -1;
        public void Disconnect()
        {
            Interlocked.Increment(ref _calls);
            inner.Disconnect();
            CountAfterRelease = observers.Count;
            if (Armed && hostile) throw Failure;
        }
        public void ReleaseInner() => inner.Disconnect();
        public void Dispose() => Disconnect();
    }

    sealed class EmptyObserver : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task PostSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
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
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            _handler(method ?? throw new InvalidOperationException("Missing public interface method."), args);
    }
}
