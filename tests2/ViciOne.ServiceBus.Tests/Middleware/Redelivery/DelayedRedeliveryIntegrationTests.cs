using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Redelivery;

public sealed class DelayedRedeliveryIntegrationTests
{
    private static readonly TimeSpan[] Intervals =
    [
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(3),
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-DELAYED-REDELIVERY", "exact-provider-owned-interval-sequence")]
    public async Task ConfiguredIntervals_AreScheduledAndDeliveredInTheExactProviderOwnedSequence()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var deliveries = new DeliveryObservation<IntervalMessage>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<IntervalMessage>(context =>
                {
                    int attempt = deliveries.Record(context);
                    return attempt < 4
                        ? Task.FromException(new ExpectedIntervalFailure())
                        : Task.CompletedTask;
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.ReplaceMessageId = false;
                        redelivery.Intervals(Intervals);
                    }));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        var scheduled = new ScheduledSendObserver(typeof(IntervalMessage));
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduled);

        try
        {
            Guid messageId = NewId.NextGuid();
            await harness.Bus.Publish(
                    new IntervalMessage("exact-sequence"),
                    context => context.MessageId = messageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            DeliverySnapshot first = await deliveries.Next(timeout, cancellationToken);
            var snapshots = new List<DeliverySnapshot> { first };
            var schedules = new List<ScheduleSnapshot>();
            IInMemoryDelayProvider delayProvider = provider.GetRequiredService<IInMemoryDelayProvider>();

            foreach (TimeSpan interval in Intervals)
            {
                ScheduleSnapshot schedule = await scheduled.Next(timeout, cancellationToken);
                Assert.Equal(interval, schedule.Delay);
                schedules.Add(schedule);

                delayProvider.Advance(interval);
                snapshots.Add(await deliveries.Next(timeout, cancellationToken));
            }

            Assert.Equal([0, 1, 2, 3], snapshots.Select(snapshot => snapshot.RedeliveryCount));
            Assert.All(snapshots, snapshot => Assert.Equal(messageId, snapshot.MessageId));
            Assert.Equal(Intervals, schedules.Select(schedule => schedule.Delay));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(4, deliveries.Count);
        Assert.Equal(3, scheduled.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DELAYED-REDELIVERY", "replacement-id-preserves-original-id")]
    public async Task ReplaceMessageId_GeneratesANewIdentityAndPreservesTheOriginalIdentity()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var deliveries = new DeliveryObservation<IdentityMessage>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<IdentityMessage>(context =>
                {
                    int attempt = deliveries.Record(context);
                    return attempt == 1
                        ? Task.FromException(new ExpectedIdentityFailure())
                        : Task.CompletedTask;
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.ReplaceMessageId = true;
                        redelivery.Intervals(TimeSpan.FromHours(1));
                    }));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        var scheduled = new ScheduledSendObserver(typeof(IdentityMessage));
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduled);

        try
        {
            Guid originalMessageId = NewId.NextGuid();
            await harness.Bus.Publish(
                    new IdentityMessage("replace"),
                    context => context.MessageId = originalMessageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            DeliverySnapshot first = await deliveries.Next(timeout, cancellationToken);
            ScheduleSnapshot schedule = await scheduled.Next(timeout, cancellationToken);
            provider.GetRequiredService<IInMemoryDelayProvider>().Advance(TimeSpan.FromHours(1));
            DeliverySnapshot second = await deliveries.Next(timeout, cancellationToken);

            Assert.Equal(originalMessageId, first.MessageId);
            Assert.Null(first.OriginalMessageId);
            Assert.NotNull(schedule.MessageId);
            Assert.NotEqual(originalMessageId, schedule.MessageId);
            Assert.Equal(originalMessageId, schedule.OriginalMessageId);
            Assert.Equal(schedule.MessageId, second.MessageId);
            Assert.Equal(originalMessageId, second.OriginalMessageId);
            Assert.Equal(1, second.RedeliveryCount);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DELAYED-REDELIVERY", "exception-specific-filters-coexist")]
    public async Task ExceptionSpecificFilters_RedeliverOnlyThroughTheMatchingPolicyAndPublishOneTerminalFault()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var secondAttempts = new DeliveryObservation<SecondFilterMessage>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<FirstFilterMessage>((ConsumeContext<FirstFilterMessage> _) =>
                    Task.FromException(new FirstFilterFailure()));
                configuration.AddHandler<SecondFilterMessage>(context =>
                {
                    secondAttempts.Record(context);
                    return Task.FromException(new SecondFilterFailure());
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                {
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.Handle<FirstFilterFailure>();
                        redelivery.Interval(4, TimeSpan.FromHours(1));
                    });
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.Handle<SecondFilterFailure>();
                        redelivery.Interval(2, TimeSpan.FromHours(2));
                    });
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        var scheduled = new ScheduledSendObserver(typeof(SecondFilterMessage));
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduled);

        try
        {
            Task<IPublishedMessage<Fault<SecondFilterMessage>>> terminalFault = harness.Published
                .SelectAsync<Fault<SecondFilterMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new SecondFilterMessage("second"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await secondAttempts.Next(timeout, cancellationToken);

            for (var index = 0; index < 2; index++)
            {
                ScheduleSnapshot schedule = await scheduled.Next(timeout, cancellationToken);
                Assert.Equal(TimeSpan.FromHours(2), schedule.Delay);
                provider.GetRequiredService<IInMemoryDelayProvider>().Advance(schedule.Delay);
                await secondAttempts.Next(timeout, cancellationToken);
            }

            IPublishedMessage<Fault<SecondFilterMessage>> fault = await terminalFault
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal([0, 1, 2], secondAttempts.Snapshots.Select(snapshot => snapshot.RedeliveryCount));
            Assert.Equal(2, fault.Context.Headers.Get<int>(MessageHeaders.FaultRedeliveryCount));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(3, secondAttempts.Count);
        Assert.Equal(2, scheduled.Count);
        Assert.Single(harness.Published.Select<Fault<SecondFilterMessage>>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<FirstFilterMessage>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DELAYED-REDELIVERY", "outbound-message-excludes-inbound-redelivery-header")]
    public async Task PublishFromARedeliveredConsumer_DoesNotForwardTheInboundRedeliveryHeader()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inboundAttempts = new DeliveryObservation<InboundMessage>();
        var outbound = new TaskCompletionSource<ConsumeContext<OutboundMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<InboundMessage>(async context =>
                {
                    int attempt = inboundAttempts.Record(context);
                    if (attempt == 1)
                        throw new ExpectedInboundFailure();

                    await context.Publish(new OutboundMessage(context.Message.Value), context.CancellationToken);
                });
                configuration.AddHandler<OutboundMessage>(context =>
                {
                    outbound.TrySetResult(context);
                    return Task.CompletedTask;
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                        redelivery.Intervals(TimeSpan.FromHours(1))));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        var scheduled = new ScheduledSendObserver(typeof(InboundMessage));
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduled);

        try
        {
            await harness.Bus.Publish(new InboundMessage("header-boundary"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await inboundAttempts.Next(timeout, cancellationToken);
            ScheduleSnapshot schedule = await scheduled.Next(timeout, cancellationToken);
            provider.GetRequiredService<IInMemoryDelayProvider>().Advance(schedule.Delay);

            ConsumeContext<OutboundMessage> observed = await outbound.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal("header-boundary", observed.Message.Value);
            Assert.Equal(0, observed.GetRedeliveryCount());
            Assert.False(observed.Headers.TryGetHeader(MessageHeaders.RedeliveryCount, out object? _));
            Assert.Equal([0, 1], inboundAttempts.Snapshots.Select(snapshot => snapshot.RedeliveryCount));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Select<OutboundMessage>(SnapshotOnlyToken()));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed record IntervalMessage(string Value);
    public sealed record IdentityMessage(string Value);
    public sealed record FirstFilterMessage(string Value);
    public sealed record SecondFilterMessage(string Value);
    public sealed record InboundMessage(string Value);
    public sealed record OutboundMessage(string Value);

    private sealed class DeliveryObservation<TMessage>
        where TMessage : class
    {
        private readonly Channel<DeliverySnapshot> _deliveries = Channel.CreateUnbounded<DeliverySnapshot>();
        private readonly ConcurrentQueue<DeliverySnapshot> _snapshots = new();
        private int _count;

        public int Count => Volatile.Read(ref _count);
        public DeliverySnapshot[] Snapshots => _snapshots.ToArray();

        public int Record(ConsumeContext<TMessage> context)
        {
            int count = Interlocked.Increment(ref _count);
            Guid? originalMessageId = context.TryGetHeader(
                MessageHeaders.OriginalMessageId,
                out Guid? header)
                ? header
                : null;
            var snapshot = new DeliverySnapshot(
                context.MessageId,
                originalMessageId,
                context.GetRedeliveryCount());
            _snapshots.Enqueue(snapshot);
            Assert.True(_deliveries.Writer.TryWrite(snapshot));
            return count;
        }

        public Task<DeliverySnapshot> Next(TimeSpan timeout, CancellationToken cancellationToken) =>
            _deliveries.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    private sealed class ScheduledSendObserver(params Type[] messageTypes) : ISendObserver
    {
        private readonly HashSet<Type> _messageTypes = [.. messageTypes];
        private readonly Channel<ScheduleSnapshot> _scheduled = Channel.CreateUnbounded<ScheduleSnapshot>();
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task<ScheduleSnapshot> Next(TimeSpan timeout, CancellationToken cancellationToken) =>
            _scheduled.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);

        public Task PreSend<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            if (_messageTypes.Contains(typeof(T)) && context.Delay is { } delay)
            {
                Guid? originalMessageId = OriginalMessageId(context.Headers);
                Interlocked.Increment(ref _count);
                Assert.True(_scheduled.Writer.TryWrite(
                    new ScheduleSnapshot(typeof(T), delay, context.MessageId, originalMessageId)));
            }

            return Task.CompletedTask;
        }

        private static Guid? OriginalMessageId(Headers headers)
        {
            if (!headers.TryGetHeader(MessageHeaders.OriginalMessageId, out object? value))
                return null;

            return value switch
            {
                Guid messageId => messageId,
                string text when Guid.TryParse(text, out Guid messageId) => messageId,
                _ => null,
            };
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (_messageTypes.Contains(typeof(T)) && context.Delay.HasValue)
                _scheduled.Writer.TryComplete(exception);

            return Task.CompletedTask;
        }
    }

    private sealed record DeliverySnapshot(Guid? MessageId, Guid? OriginalMessageId, int RedeliveryCount);
    private sealed record ScheduleSnapshot(Type MessageType, TimeSpan Delay, Guid? MessageId, Guid? OriginalMessageId);

    private sealed class ExpectedIntervalFailure : Exception;
    private sealed class ExpectedIdentityFailure : Exception;
    private sealed class FirstFilterFailure : Exception;
    private sealed class SecondFilterFailure : Exception;
    private sealed class ExpectedInboundFailure : Exception;
}
