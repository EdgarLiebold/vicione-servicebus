using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class ForwardMessageTests
{
    private const string ExpectedHeaderName = "ViciOne-Test-Trace";
    private const string ExpectedHeaderValue = "forwarded-once";
    private static readonly Guid ExpectedMessageId =
        Guid.Parse("ce8d77d6-0fda-43dd-b2a2-a2a05775cbb1");
    private static readonly Guid ExpectedRequestId =
        Guid.Parse("982962ed-ce98-4c2d-8e4c-35e5483c71a3");
    private static readonly Guid ExpectedCorrelationId =
        Guid.Parse("2b54713e-55e8-480f-bcc4-24b932bb13e5");
    private static readonly Guid ExpectedConversationId =
        Guid.Parse("e1b134d8-807f-4b97-af35-67a88b8d1d82");
    private static readonly Guid ExpectedInitiatorId =
        Guid.Parse("50c9901f-296d-4296-a5d7-26619b5297fb");
    private static readonly Guid ExpectedOriginalMessageId =
        Guid.Parse("c09bad18-9a6f-4fbc-a4a5-116d273e2b44");
    private static readonly Uri ExpectedSourceAddress = new("loopback://forward-source");
    private static readonly Uri ExpectedResponseAddress = new("loopback://forward-response");
    private static readonly Uri ExpectedFaultAddress = new("loopback://forward-fault");

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "interface-to-concrete-json-envelope")]
    public async Task ConsumedInterfaceMessage_ForwardsItsCompleteBodyAndMetadataExactlyOnceAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"message-forwarding-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var interfaceDelivery = new TaskCompletionSource<ConsumeContext<ForwardCommand>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var concreteDelivery = new TaskCompletionSource<ConsumeContext<ForwardMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var projectedContext = new TaskCompletionSource<ForwardContextProjection>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var forwardDeliveryCount = 0;
        Uri forwardAddress = new(harness.BaseAddress, "forward");

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ForwardCommand>(async context =>
            {
                interfaceDelivery.TrySetResult(context);
                IPipe<SendContext<ForwardCommand>> projectionPipe = Pipe.Execute<SendContext<ForwardCommand>>(
                    sendContext => projectedContext.TrySetResult(ForwardContextProjection.Capture(sendContext)));

                await context.ForwardAsync(forwardAddress, projectionPipe).ConfigureAwait(false);
            });
        harness.OnConfigureInMemoryBus += configurator =>
            configurator.ReceiveEndpoint("forward", endpoint =>
                endpoint.Handler<ForwardMessage>(context =>
                {
                    Interlocked.Increment(ref forwardDeliveryCount);
                    concreteDelivery.TrySetResult(context);
                    return Task.CompletedTask;
                }));

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            var message = new ForwardMessage
            {
                CommandId = Guid.Parse("eb229bb5-300f-41f7-97ad-1907aeb43bc6"),
                ItemNumber = "item-27",
                AdditionalValue = "complete-body",
            };

            await harness.InputQueueSendEndpoint.SendAsync(
                    message,
                    context =>
                    {
                        context.MessageId = ExpectedMessageId;
                        context.RequestId = ExpectedRequestId;
                        context.CorrelationId = ExpectedCorrelationId;
                        context.ConversationId = ExpectedConversationId;
                        context.InitiatorId = ExpectedInitiatorId;
                        context.SourceAddress = ExpectedSourceAddress;
                        context.ResponseAddress = ExpectedResponseAddress;
                        context.FaultAddress = ExpectedFaultAddress;
                        context.Headers.Set(ExpectedHeaderName, ExpectedHeaderValue);
                        context.Headers.Set(MessageHeaders.OriginalMessageId, ExpectedOriginalMessageId.ToString());
                    },
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ForwardCommand> consumed = await interfaceDelivery.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            _ = new ForwardMessagePipe<ForwardCommand>(consumed).GetProbeResult(cancellationToken);
            ForwardContextProjection projected = await projectedContext.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ConsumeContext<ForwardMessage> forwarded = await concreteDelivery.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            Assert.Equal(message.CommandId, consumed.Message.CommandId);
            Assert.Equal(message.ItemNumber, consumed.Message.ItemNumber);
            AssertProjectedContext(projected, consumed.Advanced(), forwardAddress);
            AssertForwardedContext(forwarded, message);
            Assert.Null(consumed.ExpirationTime);
            Assert.Null(projected.TimeToLive);
            Assert.Null(forwarded.ExpirationTime);
            Assert.Equal(1, Volatile.Read(ref forwardDeliveryCount));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "future-expiration-preserved")]
    public async Task FutureExpiration_IsProjectedAsRemainingTimeAndPreservedByTheJsonEnvelopeAsync()
    {
        TimeSpan sourceTimeToLive = TimeSpan.FromMinutes(5);

        ForwardExpirationObservation observation = await ObserveForwardedExpirationAsync(
            sourceTimeToLive,
            forwardingOverride: null);

        DateTimeOffset sourceExpiration = Assert.IsType<DateTimeOffset>(observation.SourceExpiration);
        TimeSpan projectedTimeToLive = Assert.IsType<TimeSpan>(observation.Projection.TimeToLive);
        DateTimeOffset forwardedExpiration = Assert.IsType<DateTimeOffset>(observation.ForwardedExpiration);

        Assert.True(projectedTimeToLive > TimeSpan.Zero);
        Assert.True(projectedTimeToLive <= sourceTimeToLive);
        Assert.InRange(
            forwardedExpiration,
            sourceExpiration,
            sourceExpiration + observation.ForwardDuration);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "expired-forward-discarded-before-transport")]
    public async Task ExpiredMessage_IsObservedAndDiscardedBeforeTransportDispatchAsync()
    {
        ExpiredForwardObservation observation = await ObserveExpiredForwardAsync();

        DateTimeOffset sourceExpiration = Assert.IsType<DateTimeOffset>(observation.SourceExpiration);
        TimeSpan projectedTimeToLive = Assert.IsType<TimeSpan>(observation.Projection.TimeToLive);

        Assert.True(sourceExpiration < observation.Projection.CapturedAtUtc);
        Assert.True(projectedTimeToLive < TimeSpan.Zero);
        Assert.Equal(0, observation.DestinationDeliveryCount);
        Assert.Equal(0, observation.TargetObserver.PreSendCount);
        Assert.Equal(0, observation.TargetObserver.PostSendCount);
        Assert.Equal(0, observation.TargetObserver.SendFaultCount);

        ForwardingLogEntry logEntry = Assert.Single(
            observation.LogEntries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.StartsWith("FORWARD-EXPIRED ", StringComparison.Ordinal));
        Assert.Null(logEntry.Exception);
        Assert.Contains(logEntry.Properties, property =>
            property.Key == "DestinationAddress"
            && Equals(property.Value, observation.DestinationAddress));
        Assert.Contains(logEntry.Properties, property =>
            property.Key == "ExpirationTime"
            && Equals(property.Value, sourceExpiration));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "forwarding-pipe-expiration-override")]
    public async Task ForwardingPipe_CanReplaceTheInheritedExpirationBeforeSerializationAsync()
    {
        TimeSpan forwardingOverride = TimeSpan.FromMinutes(2);

        ForwardExpirationObservation observation = await ObserveForwardedExpirationAsync(
            TimeSpan.FromSeconds(-30),
            forwardingOverride);

        Assert.True(Assert.IsType<DateTimeOffset>(observation.SourceExpiration) < observation.Projection.CapturedAtUtc);
        Assert.Equal(forwardingOverride, Assert.IsType<TimeSpan>(observation.Projection.TimeToLive));
        Assert.InRange(
            Assert.IsType<DateTimeOffset>(observation.ForwardedExpiration),
            observation.ForwardStartedAtUtc + forwardingOverride,
            observation.ForwardCompletedAtUtc + forwardingOverride);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "expired-forward-requires-positive-override")]
    public async Task ClearingTheInheritedExpiration_DoesNotReviveAnExpiredMessageAsync()
    {
        ExpiredForwardObservation observation = await ObserveExpiredForwardAsync(clearTimeToLive: true);

        Assert.Null(observation.Projection.TimeToLive);
        Assert.Equal(0, observation.DestinationDeliveryCount);
        Assert.Equal(0, observation.TargetObserver.PreSendCount);
        Assert.Equal(0, observation.TargetObserver.PostSendCount);
        Assert.Equal(0, observation.TargetObserver.SendFaultCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "expired-replacement-discarded-before-transport")]
    public async Task ExpiredReplacementMessage_IsDiscardedBeforeTransportDispatchAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"message-forwarding-replacement-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var sourceCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var destinationDeliveryCount = 0;
        Uri forwardAddress = new(harness.BaseAddress, "replacement-forward");
        var observer = new DestinationSendObserver(forwardAddress);

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ForwardMessage>(async context =>
            {
                ExpireConsumeContext(context.Advanced());
                await context.Advanced().ForwardAsync(
                        forwardAddress,
                        new ForwardReplacement { Value = "must-not-be-delivered" })
                    .ConfigureAwait(false);
                sourceCompleted.TrySetResult();
            });
        harness.OnConfigureInMemoryBus += configurator =>
            configurator.ReceiveEndpoint("replacement-forward", endpoint =>
                endpoint.Handler<ForwardReplacement>(_ =>
                {
                    Interlocked.Increment(ref destinationDeliveryCount);
                    return Task.CompletedTask;
                }));

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(observer);
            await harness.InputQueueSendEndpoint.SendAsync(
                    new ForwardMessage { ItemNumber = "expired-replacement" },
                    context => context.TimeToLive = TimeSpan.FromMinutes(5),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);
            await sourceCompleted.Task.WaitAsync(operationTimeout, cancellationToken);

            Assert.Equal(0, Volatile.Read(ref destinationDeliveryCount));
            Assert.Equal(0, observer.PreSendCount);
            Assert.Equal(0, observer.PostSendCount);
            Assert.Equal(0, observer.SendFaultCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private static async Task<ExpiredForwardObservation> ObserveExpiredForwardAsync(bool clearTimeToLive = false)
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ILogContext? previousLogContext = LogContext.Current;
        var logger = new ForwardingRecordingLogger();
        LogContext.ConfigureCurrentLogContext(logger);

        using var harness = new InMemoryTestHarness($"message-forwarding-expired-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var sourceCompleted = new TaskCompletionSource<ConsumeContext<ForwardMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var projection = new TaskCompletionSource<ForwardExpirationProjection>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var destinationDeliveryCount = 0;
        Uri forwardAddress = new(harness.BaseAddress, "expired-forward");
        var observer = new DestinationSendObserver(forwardAddress);

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ForwardMessage>(async context =>
            {
                TimeProvider timeProvider = ExpireConsumeContext(context.Advanced());
                await context.ForwardAsync(
                        forwardAddress,
                        Pipe.Execute<SendContext<ForwardMessage>>(sendContext =>
                        {
                            if (clearTimeToLive)
                                sendContext.TimeToLive = null;

                            projection.TrySetResult(new ForwardExpirationProjection(
                                sendContext.TimeToLive,
                                timeProvider.GetUtcNow()));
                        }))
                    .ConfigureAwait(false);
                sourceCompleted.TrySetResult(context);
            });
        harness.OnConfigureInMemoryBus += configurator =>
            configurator.ReceiveEndpoint("expired-forward", endpoint =>
                endpoint.Handler<ForwardMessage>(_ =>
                {
                    Interlocked.Increment(ref destinationDeliveryCount);
                    return Task.CompletedTask;
                }));

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(observer);
            await harness.InputQueueSendEndpoint.SendAsync(
                    new ForwardMessage { ItemNumber = "expired" },
                    context => context.TimeToLive = TimeSpan.FromMinutes(5),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ForwardMessage> source = await sourceCompleted.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ForwardExpirationProjection projected = await projection.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            return new ExpiredForwardObservation(
                source.ExpirationTime,
                projected,
                forwardAddress,
                Volatile.Read(ref destinationDeliveryCount),
                observer,
                logger.Entries);
        }
        finally
        {
            await harness.StopAsync().WaitAsync(operationTimeout, CancellationToken.None);
            LogContext.Current = previousLogContext!;
        }
    }

    private static async Task<ForwardExpirationObservation> ObserveForwardedExpirationAsync(
        TimeSpan? sourceTimeToLive,
        TimeSpan? forwardingOverride)
    {
        bool simulateExpiredInheritedExpiration = sourceTimeToLive <= TimeSpan.Zero;
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"message-forwarding-expiration-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var sourceDelivery = new TaskCompletionSource<ConsumeContext<ForwardMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var projection = new TaskCompletionSource<ForwardExpirationProjection>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var forwardWindow = new TaskCompletionSource<ForwardTimeWindow>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var forwardedDelivery = new TaskCompletionSource<ConsumeContext<ForwardMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Uri forwardAddress = new(harness.BaseAddress, "expiration-forward");

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ForwardMessage>(async context =>
            {
                TimeProvider timeProvider = simulateExpiredInheritedExpiration
                    ? ExpireConsumeContext(context.Advanced())
                    : context.GetTimeProvider();
                sourceDelivery.TrySetResult(context);
                IPipe<SendContext<ForwardMessage>> forwardingPipe = Pipe.Execute<SendContext<ForwardMessage>>(
                    sendContext =>
                    {
                        if (forwardingOverride.HasValue)
                            sendContext.TimeToLive = forwardingOverride;

                        projection.TrySetResult(new ForwardExpirationProjection(
                            sendContext.TimeToLive,
                            timeProvider.GetUtcNow()));
                    });

                DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
                await context.ForwardAsync(forwardAddress, forwardingPipe).ConfigureAwait(false);
                forwardWindow.TrySetResult(new ForwardTimeWindow(startedAtUtc, timeProvider.GetUtcNow()));
            });
        harness.OnConfigureInMemoryBus += configurator =>
            configurator.ReceiveEndpoint("expiration-forward", endpoint =>
                endpoint.Handler<ForwardMessage>(context =>
                {
                    forwardedDelivery.TrySetResult(context);
                    return Task.CompletedTask;
                }));

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(
                    new ForwardMessage { ItemNumber = "expiration-boundary" },
                    context =>
                    {
                        if (sourceTimeToLive.HasValue)
                        {
                            context.TimeToLive = simulateExpiredInheritedExpiration
                                ? TimeSpan.FromMinutes(5)
                                : sourceTimeToLive;
                        }
                    },
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ForwardMessage> source = await sourceDelivery.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ForwardExpirationProjection projected = await projection.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ForwardTimeWindow window = await forwardWindow.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ConsumeContext<ForwardMessage> forwarded = await forwardedDelivery.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            return new ForwardExpirationObservation(
                source.ExpirationTime,
                projected,
                forwarded.ExpirationTime,
                window.StartedAtUtc,
                window.CompletedAtUtc);
        }
        finally
        {
            await harness.StopAsync().WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private static FakeTimeProvider ExpireConsumeContext(ConsumeContext context)
    {
        DateTimeOffset expiration = Assert.IsType<DateTimeOffset>(context.ExpirationTime).ToUniversalTime();
        var timeProvider = new FakeTimeProvider(expiration.AddMinutes(1));
        context.SetTimeProvider(timeProvider);
        return timeProvider;
    }

    private static void AssertForwardedContext(
        ConsumeContext<ForwardMessage> context,
        ForwardMessage expected)
    {
        Assert.Equal(expected.CommandId, context.Message.CommandId);
        Assert.Equal(expected.ItemNumber, context.Message.ItemNumber);
        Assert.Equal(expected.AdditionalValue, context.Message.AdditionalValue);
        Assert.Equal(ExpectedMessageId, context.MessageId);
        Assert.Equal(ExpectedRequestId, context.RequestId);
        Assert.Equal(ExpectedCorrelationId, context.CorrelationId);
        Assert.Equal(ExpectedConversationId, context.ConversationId);
        Assert.Equal(ExpectedInitiatorId, context.InitiatorId);
        Assert.Equal(ExpectedSourceAddress, context.SourceAddress);
        Assert.Equal(ExpectedResponseAddress, context.ResponseAddress);
        Assert.Equal(ExpectedFaultAddress, context.FaultAddress);
        Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType, context.Advanced().ReceiveContext.ContentType);
        Assert.True(context.Headers.TryGetHeader(ExpectedHeaderName, out object? headerValue));
        Assert.Equal(ExpectedHeaderValue, Assert.IsType<string>(headerValue));
        Assert.True(context.Headers.TryGetHeader(MessageHeaders.OriginalMessageId, out object? originalMessageId));
        Assert.Equal(ExpectedOriginalMessageId.ToString(), Assert.IsType<string>(originalMessageId));
    }

    private static void AssertProjectedContext(
        ForwardContextProjection projected,
        ConsumeContext consumed,
        Uri forwardAddress)
    {
        Assert.Equal(consumed.MessageId, projected.MessageId);
        Assert.Equal(consumed.RequestId, projected.RequestId);
        Assert.Equal(consumed.CorrelationId, projected.CorrelationId);
        Assert.Equal(consumed.ConversationId, projected.ConversationId);
        Assert.Equal(consumed.InitiatorId, projected.InitiatorId);
        Assert.Equal(consumed.SourceAddress, projected.SourceAddress);
        Assert.Equal(consumed.ResponseAddress, projected.ResponseAddress);
        Assert.Equal(consumed.FaultAddress, projected.FaultAddress);
        Assert.Equal(forwardAddress, projected.DestinationAddress);
        Assert.True(projected.HasExpectedHeader);
        Assert.Equal(ExpectedHeaderValue, Assert.IsType<string>(projected.HeaderValue));
        Assert.True(projected.HasExpectedInternalHeader);
        Assert.Equal(ExpectedOriginalMessageId.ToString(), Assert.IsType<string>(projected.InternalHeaderValue));
    }

    private sealed record ForwardContextProjection(
        Guid? MessageId,
        Guid? RequestId,
        Guid? CorrelationId,
        Guid? ConversationId,
        Guid? InitiatorId,
        Uri? SourceAddress,
        Uri? DestinationAddress,
        Uri? ResponseAddress,
        Uri? FaultAddress,
        bool HasExpectedHeader,
        object? HeaderValue,
        bool HasExpectedInternalHeader,
        object? InternalHeaderValue,
        TimeSpan? TimeToLive)
    {
        public static ForwardContextProjection Capture(SendContext context)
        {
            bool hasExpectedHeader = context.Headers.TryGetHeader(ExpectedHeaderName, out object? headerValue);
            bool hasExpectedInternalHeader = context.Headers.TryGetHeader(
                MessageHeaders.OriginalMessageId,
                out object? internalHeaderValue);

            return new ForwardContextProjection(
                context.MessageId,
                context.RequestId,
                context.CorrelationId,
                context.ConversationId,
                context.InitiatorId,
                context.SourceAddress,
                context.DestinationAddress,
                context.ResponseAddress,
                context.FaultAddress,
                hasExpectedHeader,
                headerValue,
                hasExpectedInternalHeader,
                internalHeaderValue,
                context.TimeToLive);
        }
    }

    private sealed record ForwardExpirationObservation(
        DateTimeOffset? SourceExpiration,
        ForwardExpirationProjection Projection,
        DateTimeOffset? ForwardedExpiration,
        DateTimeOffset ForwardStartedAtUtc,
        DateTimeOffset ForwardCompletedAtUtc)
    {
        public TimeSpan ForwardDuration => ForwardCompletedAtUtc - ForwardStartedAtUtc;
    }

    private sealed record ForwardExpirationProjection(
        TimeSpan? TimeToLive,
        DateTimeOffset CapturedAtUtc);

    private sealed record ExpiredForwardObservation(
        DateTimeOffset? SourceExpiration,
        ForwardExpirationProjection Projection,
        Uri DestinationAddress,
        int DestinationDeliveryCount,
        DestinationSendObserver TargetObserver,
        IReadOnlyList<ForwardingLogEntry> LogEntries);

    private sealed record ForwardTimeWindow(
        DateTimeOffset StartedAtUtc,
        DateTimeOffset CompletedAtUtc);

    public interface ForwardCommand
    {
        Guid CommandId { get; }

        string ItemNumber { get; }
    }

    public sealed class ForwardMessage : ForwardCommand
    {
        public Guid CommandId { get; init; }

        public string ItemNumber { get; init; } = string.Empty;

        public string AdditionalValue { get; init; } = string.Empty;
    }

    private sealed class ForwardReplacement
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class DestinationSendObserver(Uri? destinationAddress) : ISendObserver
    {
        private int _postSendCount;
        private int _preSendCount;
        private int _sendFaultCount;

        public int PostSendCount => Volatile.Read(ref _postSendCount);
        public int PreSendCount => Volatile.Read(ref _preSendCount);
        public int SendFaultCount => Volatile.Read(ref _sendFaultCount);

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (IsTarget(context))
                Interlocked.Increment(ref _preSendCount);

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (IsTarget(context))
                Interlocked.Increment(ref _postSendCount);

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (IsTarget(context))
                Interlocked.Increment(ref _sendFaultCount);

            return Task.CompletedTask;
        }

        private bool IsTarget(SendContext context) =>
            destinationAddress is null || context.DestinationAddress == destinationAddress;
    }

    private sealed class ForwardingRecordingLogger : ILogger
    {
        private readonly ConcurrentQueue<ForwardingLogEntry> _entries = new();

        public IReadOnlyList<ForwardingLogEntry> Entries => _entries.ToArray();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            IReadOnlyList<KeyValuePair<string, object?>> properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToArray()
                : [];
            _entries.Enqueue(new ForwardingLogEntry(logLevel, formatter(state, exception), exception, properties));
        }
    }

    private sealed record ForwardingLogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception,
        IReadOnlyList<KeyValuePair<string, object?>> Properties);
}
