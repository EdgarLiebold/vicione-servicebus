using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ConsumerDuplicateAndTypedObserverAdmissionTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    const string Key = "same-transport-key";
    const string WireId = "original-wire-message-id";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "duplicate-own-diagnostic-preserves-retained-lock-fallback")]
    public async Task Duplicate_OptionalDiagnosticPreservesPublicDispatchAndRetainedFallbackAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new DuplicateLogger(hostile);
        var handles = new List<ConnectHandle>();
        var dispatch = new DispatchStage();
        var endpoint = new EndpointContext("duplicate", dispatch.Create(), null, handles);
        var agent = new PublicConsumer(endpoint);
        var firstLock = new FirstLock();
        var fallback = new FallbackLock();
        using var firstContext = new DeliveryContext(endpoint);
        using var duplicateContext = new DeliveryContext(endpoint);
        var publicTasks = new List<Task>();
        Task? original = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            original = agent.DeliverAsync(Key, firstContext, firstLock);
            publicTasks.Add(original);
            await dispatch.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Task rawDispatch = Assert.IsAssignableFrom<Task>(dispatch.Raw);
            Assert.False(rawDispatch.IsCompleted);
            Assert.False(original.IsCompleted);
            Assert.Same(firstContext, dispatch.Context);
            Assert.Equal(1, dispatch.Calls);
            Assert.Equal(0, firstLock.Calls);
            Task? duplicate = null;
            Exception? invocation = Record.Exception(() => { duplicate = agent.DeliverAsync(Key, duplicateContext, fallback); });
            if (duplicate is not null)
                publicTasks.Add(duplicate);
            logger.AssertEmission(endpoint.InputAddress);
            if (invocation is not null)
                Assert.Same(logger.Failure, invocation);
            // FIRST finite causal oracle: the optional owning diagnostic must not reject duplicate admission.
            Assert.Null(invocation);
            Assert.NotNull(duplicate);
            await duplicate.WaitAsync(Bound, CancellationToken.None);
            Assert.True(duplicate.IsCompletedSuccessfully);
            Assert.False(original.IsCompleted);
            Assert.Equal(1, dispatch.Calls);
            dispatch.Release.TrySetResult();
            await fallback.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, firstLock.Calls);
            Task rawFirst = Assert.IsAssignableFrom<Task>(firstLock.Raw);
            Exception? firstFailure = await Record.ExceptionAsync(() => rawFirst.WaitAsync(Bound, CancellationToken.None));
            Assert.True(rawFirst.IsFaulted);
            Assert.Same(firstLock.Failure, firstFailure);
            Assert.Equal(1, fallback.Calls);
            Assert.False(fallback.Raw.IsCompleted);
            Assert.False(rawDispatch.IsCompleted);
            Assert.False(original.IsCompleted);
            fallback.Release.TrySetResult();
            await fallback.Raw.WaitAsync(Bound, CancellationToken.None);
            await rawDispatch.WaitAsync(Bound, CancellationToken.None);
            await original.WaitAsync(Bound, CancellationToken.None);
            Assert.True(original.IsCompletedSuccessfully);
            Assert.Equal(1, agent.DeliveryCount);
            Assert.Equal(1, dispatch.Calls);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                dispatch.Release.TrySetResult();
                fallback.Release.TrySetResult();
                foreach (Task task in publicTasks)
                    await CaptureAsync(() => ObserveKnownAsync(task, firstLock.Failure), cleanup);
                if (dispatch.Raw is { } rawDispatch)
                    await CaptureAsync(() => ObserveKnownAsync(rawDispatch, firstLock.Failure), cleanup);
                if (firstLock.Raw is { } rawFirst)
                    await CaptureAsync(() => ObserveKnownAsync(rawFirst, firstLock.Failure), cleanup);
                if (fallback.Calls != 0)
                    await CaptureAsync(() => fallback.Raw.WaitAsync(Bound, CancellationToken.None), cleanup);
                foreach (Task task in dispatch.ActivityTasks.ToArray())
                    await CaptureAsync(() => task.WaitAsync(Bound, CancellationToken.None), cleanup);
                Task? stop = null;
                await CaptureAsync(() =>
                {
                    stop = agent.StopAsync(new ControlStopContext(), CancellationToken.None);
                    return Task.CompletedTask;
                }, cleanup);
                if (stop is { } actualStop)
                    await CaptureAsync(() => actualStop.WaitAsync(Bound, CancellationToken.None), cleanup);
                await CaptureAsync(() => agent.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
                foreach (ConnectHandle handle in handles)
                    await CaptureAsync(() => { handle.Disconnect(); return Task.CompletedTask; }, cleanup);
            }
            catch (Exception exception) { cleanup.Add(exception); }
            finally { LogContext.Current = previous; }
        }
        ThrowFailures(primary, cleanup);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-CONNECT-HANDLE-LIFETIME", "typed-endpoint-observer-failed-admission-retires-returned-handles")]
    public async Task TypedObservers_FailedAdmissionRetiresEveryActuallyReturnedRegistrationAsync(int mode)
    {
        var previous = LogContext.Current;
        var lifecycle = new List<ConnectHandle>();
        var admission = new ObserverAdmission(mode);
        var collection = new ReceiveEndpointCollection();
        var observer = new TypedObserver();
        var endpoints = new List<ReceiveEndpoint>();
        ConnectHandle? composite = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext();
            for (int i = 0; i < 3; i++)
            {
                string name = "typed-observer-" + i;
                IConsumePipe consume = CallbackProxy.Create<IConsumePipe>((method, args) =>
                {
                    if (method.Name != nameof(IConsumeMessageObserverConnector.ConnectConsumeMessageObserver))
                        throw new NotSupportedException("Unexpected consume operation: " + method);
                    return admission.Connect(name, method.GetGenericArguments()[0], (IConsumeMessageObserver<ObservedMessage>)args![0]!);
                });
                var pipe = new ReceivePipe(new UnusedReceivePipe(), consume);
                var context = new EndpointContext(name, null, pipe, lifecycle);
                var nativeObservers = new Connectable<IReceiveTransportObserver>();
                IReceiveTransport transport = CallbackProxy.Create<IReceiveTransport>((method, args) =>
                {
                    if (method.Name != nameof(IReceiveTransportObserverConnector.ConnectReceiveTransportObserver))
                        throw new NotSupportedException("Unexpected unstarted transport operation: " + method);
                    ConnectHandle handle = nativeObservers.Connect((IReceiveTransportObserver)args![0]!);
                    lifecycle.Add(handle);
                    return handle;
                });
                var endpoint = new ReceiveEndpoint(transport, context);
                endpoints.Add(endpoint);
                collection.Add(name, endpoint);
            }
            Exception? failure = Record.Exception(() => { composite = collection.ConnectConsumeMessageObserver(observer); });
            Assert.Equal(3, admission.Calls.Count);
            Assert.Equal(3, admission.Calls.Select(call => call.Endpoint).Distinct(StringComparer.Ordinal).Count());
            Assert.All(admission.Calls, call =>
            {
                Assert.Same(observer, call.Observer);
                Assert.Equal(typeof(ObservedMessage), call.MessageType);
            });
            if (mode == 0)
            {
                Assert.Null(failure);
                Assert.NotNull(composite);
                Assert.Equal(3, admission.Returned.Count);
                Assert.Equal(3, admission.Registrations.Count);
                composite.Disconnect();
                Assert.Equal(0, admission.Registrations.Count);
            }
            else
            {
                Assert.Null(composite);
                Assert.Equal(2, admission.Returned.Count);
                Assert.NotNull(failure);
                if (failure is AggregateException aggregate)
                    Assert.Same(admission.Primary, aggregate.InnerExceptions[0]);
                else
                    Assert.Same(admission.Primary, failure);
                // FIRST failed-admission oracle precedes fixture fallback and the optional cleanup-shape checks.
                Assert.Equal(0, admission.Registrations.Count);
                if (mode == 1)
                    Assert.Same(admission.Primary, failure);
                else
                {
                    AggregateException both = Assert.IsType<AggregateException>(failure);
                    Assert.Equal(2, both.InnerExceptions.Count);
                    Assert.Same(admission.Primary, both.InnerExceptions[0]);
                    Assert.Same(admission.Cleanup, both.InnerExceptions[1]);
                }
            }
            Assert.All(admission.Returned, handle => Assert.Equal(1, handle.DisconnectCalls));
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                admission.ThrowOnCleanup = false;
                if (composite is not null)
                    await CaptureAsync(() => { composite.Disconnect(); return Task.CompletedTask; }, cleanup);
                foreach (RegistrationHandle handle in admission.Returned)
                    await CaptureAsync(() => { handle.Disconnect(); return Task.CompletedTask; }, cleanup);
                foreach (ConnectHandle handle in lifecycle)
                    await CaptureAsync(() => { handle.Disconnect(); return Task.CompletedTask; }, cleanup);
                var endpointStops = new List<Task>();
                foreach (ReceiveEndpoint endpoint in endpoints)
                    await CaptureAsync(() =>
                    {
                        endpointStops.Add(endpoint.StopAsync(CancellationToken.None));
                        return Task.CompletedTask;
                    }, cleanup);
                foreach (Task actualStop in endpointStops)
                    await CaptureAsync(() => actualStop.WaitAsync(Bound, CancellationToken.None), cleanup);
            }
            catch (Exception exception) { cleanup.Add(exception); }
            finally { LogContext.Current = previous; }
        }
        ThrowFailures(primary, cleanup);
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
    static void ThrowFailures(Exception? primary, List<Exception> failures)
    {
        if (failures.Count != 0)
        {
            if (primary is not null)
                failures.Insert(0, primary);
            throw new AggregateException("Public control and independent fixture retirement failed.", failures);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    sealed class PublicConsumer(ReceiveEndpointContext context) : ConsumerAgent<string>(context)
    {
        public Task DeliverAsync(string key, BaseReceiveContext context, ReceiveLockContext receiveLock) => DispatchAsync(key, context, receiveLock);
    }
    sealed class ControlStopContext() : BasePipeContext(CancellationToken.None), StopContext
    {
        public string Reason => "Public duplicate control retirement";
    }
    sealed class DeliveryContext(ReceiveEndpointContext context) : BaseReceiveContext(false, context)
    {
        readonly DictionaryHeaderProvider _headers = new(new Dictionary<string, object> { [MessageHeaders.TransportMessageId] = WireId });
        readonly BinaryMessageBody _body = new(ReadOnlyMemory<byte>.Empty);
        protected override IHeaderProvider HeaderProvider => _headers;
        public override MessageBody Body => _body;
    }
    sealed class FirstLock : ReceiveLockContext
    {
        public readonly IOException Failure = new("unique first actual lock settlement failure");
        public int Calls { get; private set; }
        public Task? Raw { get; private set; }
        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Raw = Task.FromException(Failure);
        }
        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    sealed class FallbackLock : ReceiveLockContext
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls { get; private set; }
        public Task Raw => Release.Task;
        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            Entered.TrySetResult();
            return Raw;
        }
        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    sealed class DispatchStage
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentQueue<Task> ActivityTasks = new();
        ZeroActivityHandler? _zeroActivity;
        ReceiveLockContext? _lock;
        int _active;
        public int Calls { get; private set; }
        public ReceiveContext? Context { get; private set; }
        public Task? Raw { get; private set; }
        public IReceivePipeDispatcher Create() => CallbackProxy.Create<IReceivePipeDispatcher>((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(IReceivePipeDispatcher.DispatchAsync):
                    if (++Calls != 1)
                        throw new NotSupportedException("A duplicate reached the actual dispatcher.");
                    Context = (ReceiveContext)args![0]!;
                    _lock = (ReceiveLockContext)args[1]!;
                    Volatile.Write(ref _active, 1);
                    Raw = ExecuteAsync();
                    Entered.TrySetResult();
                    return Raw;
                case "add_ZeroActivity": _zeroActivity += (ZeroActivityHandler)args![0]!; return null;
                case "remove_ZeroActivity": _zeroActivity -= (ZeroActivityHandler)args![0]!; return null;
                case "get_ActiveDispatchCount": return Volatile.Read(ref _active);
                case "get_DispatchCount": return (long)Calls;
                case "get_MaxConcurrentDispatchCount": return Calls == 0 ? 0 : 1;
                default: throw new NotSupportedException("Unexpected public dispatcher operation: " + method);
            }
        });
        async Task ExecuteAsync()
        {
            try
            {
                await Release.Task.ConfigureAwait(false);
                await (_lock ?? throw new InvalidOperationException("No actual supplied lock.")).CompleteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _active, 0);
                if (_zeroActivity is not null)
                    foreach (ZeroActivityHandler callback in _zeroActivity.GetInvocationList().Cast<ZeroActivityHandler>())
                    {
                        Task actual = callback();
                        ActivityTasks.Enqueue(actual);
                        await actual.ConfigureAwait(false);
                    }
            }
        }
    }
    sealed record LogEmission(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class DuplicateLogger(bool hostile) : ILogger
    {
        const string Template = "R-DUPE {InputAddress} {MessageId} {TransportMessageId}";
        readonly ConcurrentQueue<LogEmission> _emissions = new();
        public readonly IOException Failure = new("unique owning duplicate diagnostic failure");
        public volatile bool Armed = true;
        int _throws;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Warning;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Warning || state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var template) || !Equals(template, Template))
                return;
            _emissions.Enqueue(new LogEmission(values, exception));
            if (Armed && hostile)
            {
                Interlocked.Increment(ref _throws);
                throw Failure;
            }
        }
        public void AssertEmission(Uri address)
        {
            LogEmission emission = Assert.Single(_emissions);
            Assert.Equal(address, Assert.IsType<Uri>(emission.Fields["InputAddress"]));
            Assert.Equal(WireId, emission.Fields["MessageId"]);
            Assert.Equal(Key, emission.Fields["TransportMessageId"]);
            Assert.Null(emission.Cause);
            Assert.Equal(hostile ? 1 : 0, Volatile.Read(ref _throws));
        }
    }
    sealed class ObservedMessage;
    sealed class TypedObserver : IConsumeMessageObserver<ObservedMessage>
    {
        public Task PreConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;
        public Task PostConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;
        public Task ConsumeFaultAsync(ConsumeContext<ObservedMessage> context, Exception exception) => Task.CompletedTask;
    }
    sealed record AdmissionCall(string Endpoint, Type MessageType, IConsumeMessageObserver<ObservedMessage> Observer);
    sealed class ObserverAdmission(int mode)
    {
        public readonly Connectable<IConsumeMessageObserver<ObservedMessage>> Registrations = new();
        public readonly List<AdmissionCall> Calls = [];
        public readonly List<RegistrationHandle> Returned = [];
        public readonly IOException Primary = new("unique third actual typed connector admission failure");
        public readonly InvalidOperationException Cleanup = new("unique first registration cleanup failure");
        public bool ThrowOnCleanup = mode == 2;
        public ConnectHandle Connect(string endpoint, Type messageType, IConsumeMessageObserver<ObservedMessage> observer)
        {
            Calls.Add(new AdmissionCall(endpoint, messageType, observer));
            if (mode != 0 && Calls.Count == 3)
                throw Primary;
            var handle = new RegistrationHandle(Registrations.Connect(observer), this, Returned.Count == 0);
            Returned.Add(handle);
            return handle;
        }
    }
    sealed class RegistrationHandle(ConnectHandle actual, ObserverAdmission owner, bool first) : ConnectHandle
    {
        public int DisconnectCalls { get; private set; }
        public void Dispose() => Disconnect();
        public void Disconnect()
        {
            DisconnectCalls++;
            actual.Disconnect();
            if (first && owner.ThrowOnCleanup)
                throw owner.Cleanup;
        }
    }
    sealed class UnusedReceivePipe : IPipe<ReceiveContext>
    {
        public Task SendAsync(ReceiveContext context) => throw new NotSupportedException("Registration did not authorize receive work.");
        public void Probe(ProbeContext context) => throw new NotSupportedException();
    }
    sealed class EndpointContext(string name, IReceivePipeDispatcher? dispatcher, IReceivePipe? pipe, List<ConnectHandle> handles)
        : BasePipeContext(CancellationToken.None, TimeProvider.System), ReceiveEndpointContext
    {
        readonly ReceiveEndpointObservable _observers = new();
        public Uri InputAddress { get; } = new("loopback://public-core-controls/" + name);
        public TimeSpan? ConsumerStopTimeout => null;
        public TimeSpan? StopTimeout => Bound;
        public bool IsBusEndpoint => false;
        public IReceiveEndpointObserver EndpointObservers => _observers;
        public ILogContext LogContext { get; } = new BusLogContext(NullLoggerFactory.Instance);
        public IReceivePipe ReceivePipe => pipe ?? throw new NotSupportedException();
        public IReceivePipeDispatcher CreateReceivePipeDispatcher() => dispatcher ?? throw new NotSupportedException();
        public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
        {
            ConnectHandle handle = _observers.Connect(observer);
            handles.Add(handle);
            return handle;
        }
        public IReceiveObserver ReceiveObservers => throw new NotSupportedException();
        public IReceiveTransportObserver TransportObservers => throw new NotSupportedException();
        public IPublishTopology Publish => throw new NotSupportedException();
        public IPublishEndpointProvider PublishEndpointProvider => throw new NotSupportedException();
        public ISendEndpointProvider SendEndpointProvider => throw new NotSupportedException();
        public IMessageRouteTable MessageRoutes => throw new NotSupportedException();
        public Task DependenciesReady => throw new NotSupportedException();
        public Task DependentsCompleted => throw new NotSupportedException();
        public bool PublishFaults => false;
        public int PrefetchCount => 1;
        public int? ConcurrentMessageLimit => 1;
        public ISerialization Serialization => throw new NotSupportedException();
        public Exception ConvertException(Exception exception, string message) => throw new NotSupportedException();
        public ValueTask ResetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void AddConsumeAgent(IAgent agent) => throw new NotSupportedException();
        public void AddSendAgent(IAgent agent) => throw new NotSupportedException();
        public void Probe(ProbeContext context) => throw new NotSupportedException();
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => throw new NotSupportedException();
        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer) => throw new NotSupportedException();
    }
    class CallbackProxy : DispatchProxy
    {
        Func<MethodInfo, object?[]?, object?>? _callback;
        public static T Create<T>(Func<MethodInfo, object?[]?, object?> callback) where T : class
        {
            T proxy = DispatchProxy.Create<T, CallbackProxy>();
            ((CallbackProxy)(object)proxy)._callback = callback;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => (_callback ?? throw new InvalidOperationException("Public proxy is uninitialized."))(
                targetMethod ?? throw new InvalidOperationException("Public method missing."), args);
    }
}
