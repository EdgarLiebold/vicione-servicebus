using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class OutboxMessagePipeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-OUTBOX-RECOVERY", "missing-destination-cannot-complete-delivery")]
    public async Task MissingDestination_FaultsWithoutAcknowledgingOrCompletingTheOutboxAsync()
    {
        var state = new DeliveryState(destination: null);
        Exception? failure = await Record.ExceptionAsync(() => state.Pipe.SendAsync(state.Context));

        InvalidOperationException rejected = Assert.IsType<InvalidOperationException>(failure);
        Assert.Contains("DestinationAddress", rejected.Message, StringComparison.Ordinal);
        Assert.Equal(0, state.CompletedCount);
        Assert.Equal(0, state.AcknowledgedCount);
        Assert.Equal(0, state.RemovedCount);
        Assert.Equal(0, state.ResolutionCount);
        Assert.Single(state.Messages);
        Assert.Same(state.Message, state.Messages[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-OUTBOX-RECOVERY", "resolution-observes-caller-and-delivery-cancellation")]
    public async Task EndpointResolution_ObservesDeliveryCancellationBeforeSendAsync(bool deliveryDeadline)
    {
        using var caller = new CancellationTokenSource();
        var state = new DeliveryState(new Uri("loopback://localhost/outgoing"), caller.Token,
            deliveryDeadline ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromMinutes(1));
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        state.Resolve = async token =>
        {
            entered.TrySetResult(token);
            return await release.Task.WaitAsync(token);
        };
        Task operation = state.Pipe.SendAsync(state.Context);
        try
        {
            CancellationToken observed = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(observed.CanBeCanceled, "Endpoint resolution must receive the linked delivery token.");
            if (!deliveryDeadline)
                await caller.CancelAsync();

            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(observed, failure.CancellationToken);
            Assert.True(observed.IsCancellationRequested);
            Assert.Equal(!deliveryDeadline, caller.IsCancellationRequested);
            Assert.Equal(1, state.ResolutionCount);
            Assert.Equal(0, state.AcknowledgedCount);
            Assert.Equal(0, state.CompletedCount);
            Assert.Equal(0, state.RemovedCount);
            Assert.Same(state.Message, Assert.Single(state.Messages));
        }
        finally
        {
            release.TrySetCanceled(CancellationToken.None);
            Exception? cleanup = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            Assert.IsNotType<TimeoutException>(cleanup);
            await Record.ExceptionAsync(() => release.Task);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-OUTBOX-RECOVERY", "set-consumed-and-fault-notification-failures-remain-observable")]
    public async Task SetConsumedFailure_RetainsTheCommitCauseWhenFaultNotificationFailsAsync(bool notificationFails)
    {
        var commitFailure = new InvalidOperationException("commit failed");
        var notificationFailure = new ApplicationException("fault notification failed");
        var nextCalls = 0;
        var setConsumedCalls = 0;
        var notifyFaultedCalls = 0;
        Exception? reported = null;
        ReceiveContext receive = StrictProxy.Create<ReceiveContext>((method, _) => method.Name switch
        {
            "get_IsFaulted" => false,
            _ => throw new NotSupportedException(method.Name),
        });
        OutboxConsumeContext<Input> context = null!;
        context = StrictProxy.Create<OutboxConsumeContext<Input>>((method, args) => method.Name switch
        {
            "TryGetPayload" => NoPayload(args),
            "get_IsMessageConsumed" => false,
            "get_ConsumeCompleted" => Task.CompletedTask,
            "get_ReceiveContext" => receive,
            "SetConsumedAsync" => SetConsumedAsync(),
            "NotifyFaultedAsync" => NotifyFaultedAsync(args),
            _ => throw new NotSupportedException(method.Name),
        });
        var options = new OutboxConsumeOptions
        {
            ConsumerId = Guid.Parse("36d4ebd6-14cf-4e79-b268-0ae945eaaf03"),
            ConsumerType = nameof(Input),
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromSeconds(5),
        };
        var pipe = new OutboxMessagePipe<Input>(options, new Scope(context),
            ViciOne.ServiceBus.Advanced.Middleware.Pipe.Execute<ConsumeContext<Input>>(received =>
            {
                Assert.Same(context, received);
                nextCalls++;
            }));

        Exception actual = await Record.ExceptionAsync(() => pipe.SendAsync(context))
            ?? throw new Xunit.Sdk.XunitException("The rejected outbox commit completed successfully.");

        if (notificationFails)
        {
            Assert.Collection(Assert.IsType<AggregateException>(actual).InnerExceptions,
                first => Assert.Same(commitFailure, first),
                second => Assert.Same(notificationFailure, second));
        }
        else
            Assert.Same(commitFailure, actual);
        Assert.Same(commitFailure, reported);
        Assert.Equal(1, nextCalls);
        Assert.Equal(1, setConsumedCalls);
        Assert.Equal(1, notifyFaultedCalls);

        Task SetConsumedAsync()
        {
            setConsumedCalls++;
            return Task.FromException(commitFailure);
        }

        Task NotifyFaultedAsync(object?[]? args)
        {
            notifyFaultedCalls++;
            Assert.Same(context, args![0]);
            reported = Assert.IsType<InvalidOperationException>(args[3]);
            return notificationFails ? Task.FromException(notificationFailure) : Task.CompletedTask;
        }

        static bool NoPayload(object?[]? args)
        {
            args![0] = null;
            return false;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-OUTBOX-RECOVERY", "debug-diagnostics-do-not-prevent-provider-progress")]
    public async Task DebugDiagnostics_DoNotPreventOutboxProgressAfterSuccessfulWorkAsync(bool alreadyDelivered, bool loggerThrows)
    {
        using var caller = new CancellationTokenSource();
        Guid inboxId = Guid.Parse("8d03e386-3efe-4d94-9e14-6b0114cf9e04");
        Guid messageId = Guid.Parse("4c95a603-9f97-479b-92fc-238ccbf61274");
        var destination = new Uri("loopback://localhost/debug-progress");
        string template = alreadyDelivered
            ? "Outbox Completed: {MessageId} ({ReceiveCount})"
            : "Outbox Sent: {InboxMessageId} {SequenceNumber} {MessageId}";
        var diagnosticFailure = new ApplicationException("selected-outbox-debug-failure");
        var logger = new ProgressDebugLogger(template, loggerThrows ? diagnosticFailure : null);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? actualWork = null;
        Task? operation = null;
        int resolutionCalls = 0, sendCalls = 0, acknowledgments = 0, deliveredCalls = 0;
        int removeCalls = 0, consumedCalls = 0, nextCalls = 0;
        Uri? resolvedAddress = null;
        CancellationToken resolvedToken = default, sendToken = default;
        object? sentMessage = null, sendPipe = null, notifiedContext = null;
        string? notifiedConsumer = null;
        OutboxMessageContext? acknowledgedMessage = null;
        bool? continueProcessing = null;
        OutboxMessageContext message = StrictProxy.Create<OutboxMessageContext>((method, _) => method.Name switch
        {
            "get_SequenceNumber" => 7L,
            "get_MessageId" => messageId,
            "get_DestinationAddress" => destination,
            "get_Headers" => EmptyHeaders.Instance,
            _ => throw new NotSupportedException(method.Name),
        });
        IAdvancedSendEndpoint endpoint = StrictProxy.Create<IAdvancedSendEndpoint>((method, args) =>
        {
            if (method.Name != "SendAsync" || !method.IsGenericMethod || args?.Length != 3)
                throw new NotSupportedException(method.Name);
            sendCalls++;
            sentMessage = args[0];
            sendPipe = args[1];
            sendToken = (CancellationToken)args[2]!;
            actualWork = HoldWorkAsync(sendToken);
            return actualWork;
        });
        ConsumeContext captured = StrictProxy.Create<ConsumeContext>((method, args) =>
        {
            if (method.Name != "GetSendEndpointAsync")
                throw new NotSupportedException(method.Name);
            resolutionCalls++;
            resolvedAddress = (Uri)args![0]!;
            resolvedToken = (CancellationToken)args[1]!;
            return Task.FromResult<ISendEndpoint>(endpoint);
        });
        ReceiveContext receive = StrictProxy.Create<ReceiveContext>((method, _) => method.Name switch
        {
            "get_IsDelivered" => false,
            "get_IsFaulted" => false,
            _ => throw new NotSupportedException(method.Name),
        });
        OutboxConsumeContext<Input> context = StrictProxy.Create<OutboxConsumeContext<Input>>((method, args) =>
        {
            switch (method.Name)
            {
                case "TryGetPayload": args![0] = null; return false;
                case "get_IsMessageConsumed": return true;
                case "get_IsOutboxDelivered": return alreadyDelivered;
                case "get_LastSequenceNumber": return null;
                case "get_CancellationToken": return caller.Token;
                case "get_CapturedContext": return captured;
                case "get_ConsumeCompleted": return Task.CompletedTask;
                case "get_MessageId": return inboxId;
                case "get_ReceiveCount": return 3;
                case "get_ReceiveContext": return receive;
                case "LoadOutboxMessagesAsync": return Task.FromResult(new List<OutboxMessageContext> { message });
                case "SetDeliveredAsync": deliveredCalls++; return Task.CompletedTask;
                case "NotifyOutboxMessageDeliveredAsync":
                    acknowledgments++;
                    acknowledgedMessage = (OutboxMessageContext)args![0]!;
                    return Task.CompletedTask;
                case "RemoveOutboxMessagesAsync":
                    removeCalls++;
                    actualWork = HoldWorkAsync((CancellationToken)args![0]!);
                    return actualWork;
                case "NotifyConsumedAsync":
                    consumedCalls++;
                    notifiedContext = args![0];
                    notifiedConsumer = (string)args[2]!;
                    return Task.CompletedTask;
                case "set_ContinueProcessing": continueProcessing = (bool)args![0]!; return null;
                default: throw new NotSupportedException(method.Name);
            }
        });
        var options = new OutboxConsumeOptions
        {
            ConsumerId = Guid.Parse("89f37c9a-1713-4db5-842d-b2ac1577384a"),
            ConsumerType = nameof(Input),
            MessageDeliveryLimit = 2,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        var pipe = new OutboxMessagePipe<Input>(options, new Scope(context),
            ViciOne.ServiceBus.Advanced.Middleware.Pipe.Execute<ConsumeContext<Input>>(_ => nextCalls++));
        ILogContext? previous = LogContext.Current;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            operation = pipe.SendAsync(context);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Task work = Assert.IsAssignableFrom<Task>(actualWork);
            Assert.False(work.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Empty(logger.Entries);
            Assert.Equal(alreadyDelivered ? 1 : 0, removeCalls);
            Assert.Equal(alreadyDelivered ? 0 : 1, sendCalls);
            Assert.Equal(0, acknowledgments);
            Assert.Equal(0, consumedCalls);

            release.TrySetResult();
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(work.IsCompletedSuccessfully);
            Assert.True(operation.IsCompleted);
            ProgressDebugEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Equal(template, entry.Template);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            Assert.Equal(inboxId, entry.Values[alreadyDelivered ? "MessageId" : "InboxMessageId"]);
            if (!alreadyDelivered)
            {
                Assert.Equal(messageId, entry.Values["MessageId"]);
                Assert.Equal(7L, entry.Values["SequenceNumber"]);
            }
            else
                Assert.Equal(3, entry.Values["ReceiveCount"]);
            if (observed is not null && loggerThrows)
                Assert.Same(diagnosticFailure, observed);
            Assert.Null(observed);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(0, nextCalls);
            if (alreadyDelivered)
            {
                Assert.Equal(1, removeCalls);
                Assert.Equal(1, consumedCalls);
                Assert.Same(context, notifiedContext);
                Assert.Equal(nameof(Input), notifiedConsumer);
                Assert.False(continueProcessing);
                Assert.Equal(0, resolutionCalls);
                Assert.Equal(0, sendCalls);
                Assert.Equal(0, acknowledgments);
                Assert.Equal(0, deliveredCalls);
            }
            else
            {
                Assert.Equal(1, resolutionCalls);
                Assert.Equal(destination, resolvedAddress);
                Assert.Equal(resolvedToken, sendToken);
                Assert.True(sendToken.CanBeCanceled);
                Assert.Same(SerializedTransportMessage.Instance, sentMessage);
                Assert.IsType<OutboxMessageSendPipe>(sendPipe);
                Assert.Equal(1, acknowledgments);
                Assert.Same(message, acknowledgedMessage);
                Assert.Equal(1, deliveredCalls);
                Assert.Equal(0, removeCalls);
                Assert.Equal(0, consumedCalls);
                Assert.Null(continueProcessing);
            }
        }
        finally
        {
            release.TrySetResult();
            try
            {
                try
                {
                    if (actualWork is not null)
                        await ObserveTerminalAsync(actualWork);
                }
                finally
                {
                    if (operation is not null)
                        await ObserveTerminalAsync(operation);
                }
            }
            finally
            {
                LogContext.Current = previous;
            }
        }

        async Task HoldWorkAsync(CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
        }

        static async Task ObserveTerminalAsync(Task task)
        {
            try
            {
                await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException)
            {
                _ = task.Exception;
            }
        }
    }

    public sealed record Input(Guid Id);

    private sealed class DeliveryState
    {
        public DeliveryState(Uri? destination, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            Message = StrictProxy.Create<OutboxMessageContext>((method, _) => method.Name switch
            {
                "get_SequenceNumber" => 7L,
                "get_MessageId" => Guid.Parse("4927f624-e0f5-4ac4-a092-d9511a200bea"),
                "get_DestinationAddress" => destination,
                _ => throw new NotSupportedException(method.Name),
            });
            Messages = [Message];
            ConsumeContext captured = StrictProxy.Create<ConsumeContext>((method, args) =>
            {
                if (method.Name != "GetSendEndpointAsync")
                    throw new NotSupportedException(method.Name);
                Assert.Equal(destination, args![0]);
                ResolutionCount++;
                return Resolve((CancellationToken)args[1]!);
            });
            Context = StrictProxy.Create<OutboxConsumeContext<Input>>((method, args) =>
            {
                switch (method.Name)
                {
                    case "TryGetPayload":
                        args![0] = null;
                        return false;
                    case "get_IsMessageConsumed": return true;
                    case "get_IsOutboxDelivered": return false;
                    case "get_LastSequenceNumber": return null;
                    case "get_CancellationToken": return cancellationToken;
                    case "get_CapturedContext": return captured;
                    case "get_ConsumeCompleted": return Task.CompletedTask;
                    case "LoadOutboxMessagesAsync": return Task.FromResult(Messages);
                    case "SetDeliveredAsync": CompletedCount++; return Task.CompletedTask;
                    case "NotifyOutboxMessageDeliveredAsync": AcknowledgedCount++; return Task.CompletedTask;
                    case "RemoveOutboxMessagesAsync": RemovedCount++; return Task.CompletedTask;
                    default: throw new NotSupportedException(method.Name);
                }
            });
            var options = new OutboxConsumeOptions
            {
                ConsumerId = Guid.Parse("36d4ebd6-14cf-4e79-b268-0ae945eaaf03"),
                ConsumerType = nameof(Input),
                MessageDeliveryLimit = 2,
                MessageDeliveryTimeout = timeout ?? TimeSpan.FromSeconds(5),
            };
            Pipe = new OutboxMessagePipe<Input>(options, new Scope(Context),
                ViciOne.ServiceBus.Advanced.Middleware.Pipe.Execute<ConsumeContext<Input>>(_ =>
                    throw new InvalidOperationException("Committed consumption must not invoke the consumer again.")));
        }

        public OutboxMessagePipe<Input> Pipe { get; }
        public OutboxConsumeContext<Input> Context { get; }
        public OutboxMessageContext Message { get; }
        public List<OutboxMessageContext> Messages { get; }
        public Func<CancellationToken, Task<ISendEndpoint>> Resolve { get; set; } =
            _ => throw new InvalidOperationException("A missing destination must not resolve an endpoint.");
        public int ResolutionCount { get; private set; }
        public int CompletedCount { get; private set; }
        public int AcknowledgedCount { get; private set; }
        public int RemovedCount { get; private set; }
    }

    private sealed class Scope(ConsumeContext<Input> context) : IConsumeScopeContext<Input>, IDisposable
    {
        public ConsumeContext<Input> Context => context;
        public T GetService<T>() where T : class => throw new NotSupportedException();
        public T CreateInstance<T>(params object[] arguments) where T : class => throw new NotSupportedException();
        public IDisposable PushConsumeContext(ConsumeContext value) => this;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed record ProgressDebugEntry(LogLevel Level, string Template, IReadOnlyDictionary<string, object?> Values);

    private sealed class ProgressDebugLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public List<ProgressDebugEntry> Entries { get; } = [];
        public int ThrowCount { get; private set; }
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> values)
                throw new InvalidOperationException("Expected structured diagnostic state.");
            Dictionary<string, object?> fields = values.ToDictionary(pair => pair.Key, pair => pair.Value);
            string template = (string)fields["{OriginalFormat}"]!;
            Entries.Add(new ProgressDebugEntry(logLevel, template, fields));
            if (template == selectedTemplate && failure is not null)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

    public class StrictProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?> _invoke = null!;

        public static T Create<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
        {
            T instance = Create<T, StrictProxy>();
            ((StrictProxy)(object)instance)._invoke = invoke;
            return instance;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _invoke(targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)), args);
    }
}
