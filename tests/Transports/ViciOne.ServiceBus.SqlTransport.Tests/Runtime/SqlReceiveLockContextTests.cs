using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlReceiveLockContextTests
{
    private static readonly Uri InputAddress = new("db://localhost/transport/input");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "unsafe-base-lookup-still-unlocks-with-original-failure")]
    public async Task FaultedAsync_UnsafeBaseLookupStillUnlocksWithTheOriginalFailureAsync(bool nullBase)
    {
        var client = Client(unlockResult: true);
        var proxy = (LockClientContextProxy)(object)client;
        var context = CreateContext(client);
        var failure = new UnsafeBaseException(nullBase);
        CancellationToken token = TestContext.Current.CancellationToken;

        await context.FaultedAsync(failure, token);

        Assert.Equal(1, proxy.UnlockCallCount);
        Assert.Equal(0, proxy.DeleteCallCount);
        object?[] arguments = Assert.IsType<object?[]>(proxy.UnlockArguments);
        Assert.Equal(token, Assert.IsType<CancellationToken>(arguments[^1]));
        SendHeaders headers = Assert.IsAssignableFrom<SendHeaders>(arguments[3]);
        Assert.Equal("fault", headers.Get<string>(MessageHeaders.Reason));
        Assert.Equal("original SQL failure", headers.Get<string>(MessageHeaders.FaultMessage));
        Assert.EndsWith(nameof(UnsafeBaseException),
            headers.Get<string>(MessageHeaders.FaultExceptionType), StringComparison.Ordinal);

        await context.FaultedAsync(failure, token);
        Assert.Equal(1, proxy.UnlockCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "failed-unlock-is-reported-as-lock-loss")]
    public async Task ScheduleRedeliveryAsync_ReportsARejectedUnlockAsLockLossAsync()
    {
        var client = Client(unlockResult: false);
        var context = CreateContext(client);

        TransportException exception = await Assert.ThrowsAsync<TransportException>(() =>
            context.ScheduleRedeliveryAsync(TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));

        Assert.Contains("lock", exception.Message, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "redelivery-callback-is-never-silently-ignored")]
    public async Task ScheduleRedeliveryAsync_RejectsAnUnsupportedSendContextCallbackAsync()
    {
        var client = Client(unlockResult: true);
        var context = CreateContext(client);

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            context.ScheduleRedeliveryAsync(TimeSpan.Zero, static (_, _) => { }, TestContext.Current.CancellationToken));

        Assert.Contains("callback", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ((LockClientContextProxy)(object)client).UnlockCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "concurrent-terminal-operations-settle-exactly-once")]
    public async Task ConcurrentTerminalOperations_SettleTheDeliveryExactlyOnceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var client = Client(unlockResult: true, holdDelete: true);
        var proxy = (LockClientContextProxy)(object)client;
        var context = CreateContext(client);

        Task complete = context.CompleteAsync(cancellationToken);
        await proxy.DeleteEntered.WaitAsync(cancellationToken);

        Task faulted = context.FaultedAsync(new InvalidOperationException("consumer failed"), cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        Assert.Equal(1, proxy.DeleteCallCount);
        Assert.Equal(0, proxy.UnlockCallCount);
        Assert.False(faulted.IsCompleted);

        proxy.ReleaseDelete();
        await Task.WhenAll(complete, faulted);

        Assert.Equal(1, proxy.DeleteCallCount);
        Assert.Equal(0, proxy.UnlockCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "terminal-settlement-cancels-in-flight-renewal")]
    public async Task CompleteAsync_CancelsAnInFlightProviderRenewalAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var client = Client(unlockResult: true, blockRenewal: true);
        var proxy = (LockClientContextProxy)(object)client;
        var context = CreateContext(client, new TestReceiveSettings(TimeSpan.Zero));

        await proxy.RenewEntered.WaitAsync(cancellationToken);
        await context.CompleteAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

        Assert.True(proxy.RenewalToken.CanBeCanceled);
        Assert.True(proxy.RenewalToken.IsCancellationRequested);
        Assert.Equal(1, proxy.DeleteCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "successful-redelivery-unlock-survives-debug-failure")]
    public async Task ScheduleRedeliveryAsync_SuccessfulUnlockSurvivesDebugFailureAsync(bool hostileLogger)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CancellationToken token = caller.Token;
        var expectedFailure = new IOException("SQL redelivery diagnostic failure");
        var logger = new RedeliveryLogger(hostileLogger ? expectedFailure : null);
        var previous = LogContext.Current;
        ClientContext client = DispatchProxy.Create<ClientContext, RedeliveryClientProxy>();
        var proxy = (RedeliveryClientProxy)(object)client;
        var message = new SqlTransportMessage
        {
            LockId = NewId.NextGuid(),
            MessageDeliveryId = 27,
            MessageId = NewId.NextGuid(),
            TransportHeaders = "[]",
        };
        TimeSpan delay = TimeSpan.FromSeconds(5);
        SqlReceiveLockContext? context = null;
        Task? operation = null;

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            context = new SqlReceiveLockContext(InputAddress, message, new TestReceiveSettings(), client, TimeProvider.System);
            operation = context.ScheduleRedeliveryAsync(delay, null, token);
            await proxy.UnlockEntered.WaitAsync(TimeSpan.FromSeconds(10), token);

            Assert.Equal(1, proxy.UnlockCalls);
            Assert.Equal(0, proxy.DeleteCalls);
            Assert.False(proxy.UnlockTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            object?[] arguments = Assert.IsType<object?[]>(proxy.UnlockArguments);
            Assert.Equal(message.LockId, Assert.IsType<Guid>(arguments[0]));
            Assert.Equal(message.MessageDeliveryId, Assert.IsType<long>(arguments[1]));
            Assert.Equal(delay, Assert.IsType<TimeSpan>(arguments[2]));
            SendHeaders headers = Assert.IsAssignableFrom<SendHeaders>(arguments[3]);
            Assert.Same(message.GetTransportHeaders(), headers);
            Assert.Equal(1, headers.Get<int>(MessageHeaders.RedeliveryCount));
            Assert.Equal(token, Assert.IsType<CancellationToken>(arguments[4]));
            Assert.Empty(logger.Entries);

            proxy.ReleaseUnlock();
            Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(10), token));

            Assert.True(await proxy.UnlockTask.WaitAsync(TimeSpan.FromSeconds(10), token));
            Assert.True(proxy.UnlockTask.IsCompletedSuccessfully);
            Assert.True(operation.IsCompleted);
            RedeliveryEntry entry = Assert.Single(logger.Entries);
            Assert.Equal("RESEND {DestinationAddress} {MessageId} (delay: {Delay})", entry.Template);
            Assert.Null(entry.Exception);
            Assert.Same(InputAddress, entry.Destination);
            Assert.Equal(message.MessageId, entry.MessageId);
            Assert.Equal(delay, entry.Delay);
            Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
            if (failure != null)
                Assert.Same(expectedFailure, failure);

            Assert.Null(failure);
            Assert.True(operation.IsCompletedSuccessfully);
            await context.CompleteAsync(token);
            Assert.Equal(1, proxy.UnlockCalls);
            Assert.Equal(0, proxy.DeleteCalls);
        }
        finally
        {
            proxy.ReleaseUnlock();
            try
            {
                try
                {
                    if (operation != null)
                        await ObserveRedeliveryTaskAsync(operation);
                }
                finally
                {
                    if (context != null)
                        await ObserveRedeliveryTaskAsync(context.CompleteAsync(CancellationToken.None));
                }
            }
            finally
            {
                try
                {
                    if (proxy.UnlockCalls != 0)
                        await ObserveRedeliveryTaskAsync(proxy.UnlockTask);
                }
                finally
                {
                    LogContext.Current = previous;
                }
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "admitted-settlement-survives-renewal-warning-failure")]
    public async Task CompleteAsync_AlreadyAdmittedSettlementSurvivesRenewalWarningFailureAsync(
        bool renewalThrows, bool hostileLogger)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CancellationToken token = caller.Token;
        var providerFailure = new IOException("SQL provider renewal failed");
        var diagnosticFailure = new IOException("SQL renewal Warning diagnostic failure");
        var logger = new RenewalWarningLogger(hostileLogger ? diagnosticFailure : null);
        var previous = LogContext.Current;
        ClientContext client = DispatchProxy.Create<ClientContext, HeldRenewalClientProxy>();
        var proxy = (HeldRenewalClientProxy)(object)client;
        proxy.RenewalFailure = renewalThrows ? providerFailure : null;
        var message = new SqlTransportMessage
        {
            LockId = NewId.NextGuid(),
            MessageDeliveryId = 27,
            MessageId = NewId.NextGuid(),
            TransportHeaders = "[]",
        };
        var clock = new FakeTimeProvider();
        SqlReceiveLockContext? context = null;
        Task? operation = null;

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            context = new SqlReceiveLockContext(InputAddress, message, new TestReceiveSettings(), client, clock);
            clock.Advance(TimeSpan.FromSeconds(42));
            await proxy.RenewalEntered.WaitAsync(TimeSpan.FromSeconds(10), token);

            Assert.Equal(1, proxy.RenewalCalls);
            object?[] renewalArguments = Assert.IsType<object?[]>(proxy.RenewalArguments);
            Assert.Equal(message.LockId, Assert.IsType<Guid>(renewalArguments[0]));
            Assert.Equal(message.MessageDeliveryId, Assert.IsType<long>(renewalArguments[1]));
            Assert.Equal(TimeSpan.FromSeconds(60), Assert.IsType<TimeSpan>(renewalArguments[2]));
            Assert.True(proxy.RenewalToken.CanBeCanceled);
            Assert.False(proxy.RenewalToken.IsCancellationRequested);
            Assert.False(proxy.RenewalTask.IsCompleted);
            Assert.Empty(logger.Entries);

            operation = context.CompleteAsync(token);

            Assert.True(proxy.RenewalToken.IsCancellationRequested);
            Assert.False(caller.IsCancellationRequested);
            Assert.False(operation.IsCompleted);
            Assert.False(proxy.RenewalTask.IsCompleted);
            Assert.Equal(0, proxy.DeleteCalls);

            proxy.ReleaseRenewal();
            Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(10), token));
            Exception? renewalFailure = await Record.ExceptionAsync(() => proxy.RenewalTask.WaitAsync(TimeSpan.FromSeconds(10), token));

            Assert.True(operation.IsCompleted);
            Assert.True(proxy.RenewalTask.IsCompleted);
            if (renewalThrows)
            {
                Assert.Same(providerFailure, renewalFailure);
                Assert.True(proxy.RenewalTask.IsFaulted);
            }
            else
            {
                Assert.Null(renewalFailure);
                Assert.False(await proxy.RenewalTask.WaitAsync(TimeSpan.FromSeconds(10), token));
                Assert.True(proxy.RenewalTask.IsCompletedSuccessfully);
            }
            string template = renewalThrows
                ? "Message lock renewal failed: {InputAddress} {MessageDeliveryId} {LockId}"
                : "Message Lock Lost: {InputAddress} - {MessageDeliveryId} ({LockId})";
            RenewalWarningEntry selected = Assert.Single(logger.Entries, entry => entry.Template == template);
            Assert.Same(renewalThrows ? providerFailure : null, selected.Exception);
            Assert.All(logger.Entries, entry =>
            {
                Assert.Same(InputAddress, entry.InputAddress);
                Assert.Equal(message.MessageDeliveryId, entry.DeliveryId);
                Assert.Equal(message.LockId, entry.LockId);
            });
            if (hostileLogger)
            {
                Assert.Equal(logger.Entries.Count, logger.ThrowCount);
                Assert.Same(diagnosticFailure, logger.LastThrownFailure);
            }
            else
            {
                Assert.Single(logger.Entries);
                Assert.Equal(0, logger.ThrowCount);
            }
            if (failure != null)
                Assert.Same(diagnosticFailure, failure);

            Assert.Equal(1, proxy.DeleteCalls);
            Assert.Null(failure);
            Assert.True(operation.IsCompletedSuccessfully);
            object?[] deleteArguments = Assert.IsType<object?[]>(proxy.DeleteArguments);
            Assert.Equal(message.LockId, Assert.IsType<Guid>(deleteArguments[0]));
            Assert.Equal(message.MessageDeliveryId, Assert.IsType<long>(deleteArguments[1]));
            Assert.Equal(token, Assert.IsType<CancellationToken>(deleteArguments[2]));
            Assert.True(await proxy.DeleteTask.WaitAsync(TimeSpan.FromSeconds(10), token));
            await context.CompleteAsync(token);
            Assert.Equal(1, proxy.DeleteCalls);
            Assert.Equal(1, proxy.RenewalCalls);
        }
        finally
        {
            proxy.ReleaseRenewal();
            try
            {
                try
                {
                    if (operation != null)
                        await ObserveRedeliveryTaskAsync(operation);
                }
                finally
                {
                    if (context != null)
                        await ObserveRedeliveryTaskAsync(context.CompleteAsync(CancellationToken.None));
                }
            }
            finally
            {
                try
                {
                    if (proxy.RenewalCalls != 0)
                        await ObserveRedeliveryTaskAsync(proxy.RenewalTask);
                }
                finally
                {
                    try
                    {
                        if (proxy.DeleteCalls != 0)
                            await ObserveRedeliveryTaskAsync(proxy.DeleteTask);
                    }
                    finally
                    {
                        LogContext.Current = previous;
                    }
                }
            }
        }
    }

    private class HeldRenewalClientProxy : DispatchProxy
    {
        readonly TaskCompletionSource<bool> _renewal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IOException? RenewalFailure { get; set; }
        public Task RenewalEntered => _entered.Task;
        public Task<bool> RenewalTask => _renewal.Task;
        public Task<bool> DeleteTask { get; } = Task.FromResult(true);
        public int RenewalCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public CancellationToken RenewalToken { get; private set; }
        public object?[]? RenewalArguments { get; private set; }
        public object?[]? DeleteArguments { get; private set; }

        public void ReleaseRenewal()
        {
            if (RenewalFailure != null)
                _renewal.TrySetException(RenewalFailure);
            else
                _renewal.TrySetResult(false);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            switch (targetMethod.Name)
            {
                case "get_CancellationToken":
                    return CancellationToken.None;
                case "RenewLockAsync":
                    RenewalCalls++;
                    RenewalArguments = args?.ToArray();
                    RenewalToken = (CancellationToken)args![3]!;
                    _entered.TrySetResult();
                    return _renewal.Task;
                case "DeleteMessageAsync":
                    DeleteCalls++;
                    DeleteArguments = args?.ToArray();
                    return DeleteTask;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private sealed record RenewalWarningEntry(Exception? Exception, string Template, Uri InputAddress, long DeliveryId, Guid LockId);

    private sealed class RenewalWarningLogger(Exception? failure) : ILogger
    {
        public List<RenewalWarningEntry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public Exception? LastThrownFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Warning)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            Entries.Add(new RenewalWarningEntry(exception, (string)values["{OriginalFormat}"]!,
                (Uri)values["InputAddress"]!, (long)values["MessageDeliveryId"]!, (Guid)values["LockId"]!));
            if (failure != null)
            {
                ThrowCount++;
                LastThrownFailure = failure;
                throw failure;
            }
        }
    }

    private static async Task ObserveRedeliveryTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private class RedeliveryClientProxy : DispatchProxy
    {
        readonly TaskCompletionSource<bool> _unlock = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task UnlockEntered => _entered.Task;
        public Task<bool> UnlockTask => _unlock.Task;
        public int UnlockCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public object?[]? UnlockArguments { get; private set; }
        public void ReleaseUnlock() => _unlock.TrySetResult(true);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            switch (targetMethod.Name)
            {
                case "get_CancellationToken":
                    return CancellationToken.None;
                case "UnlockAsync":
                    UnlockCalls++;
                    UnlockArguments = args?.ToArray();
                    _entered.TrySetResult();
                    return _unlock.Task;
                case "DeleteMessageAsync":
                    DeleteCalls++;
                    return Task.FromResult(true);
                case "RenewLockAsync":
                    return Task.FromResult(true);
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private sealed record RedeliveryEntry(Exception? Exception, string Template, Uri Destination, Guid MessageId, TimeSpan Delay);

    private sealed class RedeliveryLogger(Exception? failure) : ILogger
    {
        public List<RedeliveryEntry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Debug)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            string template = (string)values["{OriginalFormat}"]!;
            if (template != "RESEND {DestinationAddress} {MessageId} (delay: {Delay})")
                return;
            Entries.Add(new RedeliveryEntry(exception, template, (Uri)values["DestinationAddress"]!,
                (Guid)values["MessageId"]!, (TimeSpan)values["Delay"]!));
            if (failure != null)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "callback-failure-does-not-abandon-owned-renewal")]
    public async Task CompleteAsync_CallbackFailureStillJoinsTheOwnedProviderRenewalAsync(bool callbackThrows)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CancellationToken token = caller.Token;
        var callbackFailure = new IOException("SQL renewal cancellation callback failure");
        var previous = LogContext.Current;
        ClientContext client = DispatchProxy.Create<ClientContext, CallbackRenewalClientProxy>();
        var proxy = (CallbackRenewalClientProxy)(object)client;
        proxy.CallbackFailure = callbackThrows ? callbackFailure : null;
        var message = new SqlTransportMessage
        {
            LockId = NewId.NextGuid(),
            MessageDeliveryId = 27,
            MessageId = NewId.NextGuid(),
            TransportHeaders = "[]",
        };
        var clock = new FakeTimeProvider();
        SqlReceiveLockContext? context = null;
        Task? operation = null;

        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            context = new SqlReceiveLockContext(InputAddress, message, new TestReceiveSettings(), client, clock);
            clock.Advance(TimeSpan.FromSeconds(42));
            await proxy.RenewalEntered.WaitAsync(TimeSpan.FromSeconds(10), token);

            Assert.Equal(1, proxy.RenewalCalls);
            object?[] renewalArguments = Assert.IsType<object?[]>(proxy.RenewalArguments);
            Assert.Equal(message.LockId, Assert.IsType<Guid>(renewalArguments[0]));
            Assert.Equal(message.MessageDeliveryId, Assert.IsType<long>(renewalArguments[1]));
            Assert.Equal(TimeSpan.FromSeconds(60), Assert.IsType<TimeSpan>(renewalArguments[2]));
            Assert.True(proxy.RenewalToken.CanBeCanceled);
            Assert.False(proxy.RenewalToken.IsCancellationRequested);
            Assert.Equal(0, proxy.CallbackCalls);
            Assert.False(proxy.RenewalTask.IsCompleted);

            operation = context.CompleteAsync(token);

            Assert.Equal(1, proxy.CallbackCalls);
            Assert.True(proxy.CallbackSawCancellation);
            Assert.True(proxy.RenewalToken.IsCancellationRequested);
            Assert.False(caller.IsCancellationRequested);
            Assert.False(proxy.RenewalTask.IsCompleted);
            Assert.Equal(0, proxy.DeleteCalls);
            if (operation.IsFaulted)
            {
                AggregateException operationFault = Assert.IsType<AggregateException>(operation.Exception);
                Assert.Same(callbackFailure, Assert.Single(operationFault.Flatten().InnerExceptions));
            }

            Assert.False(operation.IsCompleted);

            proxy.ReleaseRenewal();
            Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(10), token));
            Assert.True(await proxy.RenewalTask.WaitAsync(TimeSpan.FromSeconds(10), token));
            Assert.True(proxy.RenewalTask.IsCompletedSuccessfully);
            Assert.True(operation.IsCompleted);
            Assert.Equal(1, proxy.CallbackCalls);
            Assert.Equal(1, proxy.RenewalCalls);
            if (callbackThrows)
            {
                AggregateException aggregate = Assert.IsType<AggregateException>(failure);
                Assert.Same(callbackFailure, Assert.Single(aggregate.Flatten().InnerExceptions));
                Assert.True(operation.IsFaulted);
                Assert.Equal(0, proxy.DeleteCalls);
            }
            else
            {
                Assert.Null(failure);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.Equal(1, proxy.DeleteCalls);
                object?[] deleteArguments = Assert.IsType<object?[]>(proxy.DeleteArguments);
                Assert.Equal(message.LockId, Assert.IsType<Guid>(deleteArguments[0]));
                Assert.Equal(message.MessageDeliveryId, Assert.IsType<long>(deleteArguments[1]));
                Assert.Equal(token, Assert.IsType<CancellationToken>(deleteArguments[2]));
                Assert.True(await proxy.DeleteTask.WaitAsync(TimeSpan.FromSeconds(10), token));
            }
        }
        finally
        {
            proxy.ReleaseRenewal();
            try
            {
                try
                {
                    if (operation != null)
                        await ObserveRedeliveryTaskAsync(operation);
                }
                finally
                {
                    if (context != null)
                        await ObserveRedeliveryTaskAsync(context.CompleteAsync(CancellationToken.None));
                }
            }
            finally
            {
                try
                {
                    if (proxy.RenewalCalls != 0)
                        await ObserveRedeliveryTaskAsync(proxy.RenewalTask);
                }
                finally
                {
                    try
                    {
                        if (proxy.DeleteCalls != 0)
                            await ObserveRedeliveryTaskAsync(proxy.DeleteTask);
                    }
                    finally
                    {
                        try
                        {
                            proxy.DisposeRegistration();
                        }
                        finally
                        {
                            LogContext.Current = previous;
                        }
                    }
                }
            }
        }
    }

    private class CallbackRenewalClientProxy : DispatchProxy
    {
        readonly TaskCompletionSource<bool> _renewal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration _registration;
        public IOException? CallbackFailure { get; set; }
        public Task RenewalEntered => _entered.Task;
        public Task<bool> RenewalTask => _renewal.Task;
        public Task<bool> DeleteTask { get; } = Task.FromResult(true);
        public int RenewalCalls { get; private set; }
        public int CallbackCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public bool CallbackSawCancellation { get; private set; }
        public CancellationToken RenewalToken { get; private set; }
        public object?[]? RenewalArguments { get; private set; }
        public object?[]? DeleteArguments { get; private set; }
        public void ReleaseRenewal() => _renewal.TrySetResult(true);
        public void DisposeRegistration() => _registration.Dispose();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            switch (targetMethod.Name)
            {
                case "get_CancellationToken":
                    return CancellationToken.None;
                case "RenewLockAsync":
                    RenewalCalls++;
                    RenewalArguments = args?.ToArray();
                    RenewalToken = (CancellationToken)args![3]!;
                    _registration = RenewalToken.Register(() =>
                    {
                        CallbackCalls++;
                        CallbackSawCancellation = RenewalToken.IsCancellationRequested;
                        if (CallbackFailure != null)
                            throw CallbackFailure;
                    });
                    _entered.TrySetResult();
                    return _renewal.Task;
                case "DeleteMessageAsync":
                    DeleteCalls++;
                    DeleteArguments = args?.ToArray();
                    return DeleteTask;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private static SqlReceiveLockContext CreateContext(ClientContext client, ReceiveSettings? settings = null)
    {
        var message = new SqlTransportMessage
        {
            LockId = NewId.NextGuid(),
            MessageDeliveryId = 27,
            MessageId = NewId.NextGuid(),
            TransportHeaders = "[]",
        };

        return new SqlReceiveLockContext(InputAddress, message, settings ?? new TestReceiveSettings(), client, TimeProvider.System);
    }

    private static ClientContext Client(bool unlockResult, bool holdDelete = false, bool blockRenewal = false)
    {
        ClientContext client = DispatchProxy.Create<ClientContext, LockClientContextProxy>();
        var proxy = (LockClientContextProxy)(object)client;
        proxy.UnlockResult = unlockResult;
        proxy.HoldDelete = holdDelete;
        proxy.BlockRenewal = blockRenewal;
        return client;
    }

    private class LockClientContextProxy : DispatchProxy
    {
        readonly TaskCompletionSource<bool> _deleteCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _deleteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _renewEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockRenewal { get; set; }
        public Task DeleteEntered => _deleteEntered.Task;
        public int DeleteCallCount { get; private set; }
        public bool HoldDelete { get; set; }
        public Task RenewEntered => _renewEntered.Task;
        public CancellationToken RenewalToken { get; private set; }
        public bool UnlockResult { get; set; }
        public int UnlockCallCount { get; private set; }
        public object?[]? UnlockArguments { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_CancellationToken" => CancellationToken.None,
                "UnlockAsync" => UnlockAsync(args),
                "DeleteMessageAsync" => DeleteMessageAsync(),
                "RenewLockAsync" => RenewLockAsync(args),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        public void ReleaseDelete() => _deleteCompletion.TrySetResult(true);

        private Task<bool> DeleteMessageAsync()
        {
            DeleteCallCount++;
            _deleteEntered.TrySetResult();
            return HoldDelete ? _deleteCompletion.Task : Task.FromResult(true);
        }

        private Task<bool> RenewLockAsync(object?[]? args)
        {
            if (!BlockRenewal)
                return Task.FromResult(true);

            RenewalToken = (CancellationToken)args![3]!;
            _renewEntered.TrySetResult();

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            RenewalToken.Register(() => completion.TrySetCanceled(RenewalToken));
            return completion.Task;
        }

        private Task<bool> UnlockAsync(object?[]? args)
        {
            UnlockCallCount++;
            UnlockArguments = args?.ToArray();
            return Task.FromResult(UnlockResult);
        }
    }

    private sealed class TestReceiveSettings(TimeSpan? lockDuration = null) : ReceiveSettings
    {
        public string QueueName => "input";
        public TimeSpan? AutoDeleteOnIdle => null;
        public int? MaxDeliveryCount => 10;
        public long? QueueId => 1;
        public int PrefetchCount => 1;
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
        public SqlReceiveMode ReceiveMode => SqlReceiveMode.Normal;
        public bool PurgeOnStartup => false;
        public TimeSpan LockDuration => lockDuration ?? TimeSpan.FromHours(1);
        public TimeSpan PollingInterval => TimeSpan.FromSeconds(1);
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(2);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => false;
    }

    private sealed class UnsafeBaseException(bool nullBase) : Exception("original SQL failure")
    {
        public override Exception GetBaseException() => nullBase
            ? null!
            : throw new InvalidOperationException("base lookup failed");
    }
}
