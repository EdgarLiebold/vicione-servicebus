using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transactions;

public sealed class BufferedBusTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "registered-scoped-publish-uses-buffered-capability")]
    public async Task Registration_RoutesScopedPublishEndpointThroughBufferedContractAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddBufferedBus();
                configuration.AddConsumer<TransactionalMessageConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IConsumerTestHarness<TransactionalMessageConsumer> consumer = harness.GetConsumerHarness<TransactionalMessageConsumer>();
        var message = new TransactionalMessage(NewId.NextGuid(), "registered");

        try
        {
            IServiceProvider scopedProvider = harness.Scope.ServiceProvider;
            IPublishEndpoint publishEndpoint = scopedProvider.GetRequiredService<IPublishEndpoint>();
            IBufferedBus bufferedBus = scopedProvider.GetRequiredService<IBufferedBus>();
            await publishEndpoint.PublishAsync(message, cancellationToken);

            Assert.Empty(harness.Published.Snapshot<TransactionalMessage>());

            await bufferedBus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            IConsumedMessage<TransactionalMessage> received = await consumer.Consumed
                .SelectAsync<TransactionalMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await bufferedBus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, received.Context.Message);
            Assert.Single(harness.Published.Snapshot<TransactionalMessage>());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "consumer-scope-retains-buffered-boundary")]
    public async Task ConsumerScope_RoutesScopedPublishEndpointThroughBufferedContractAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new BufferedConsumerCoordinator(timeout);
        var services = new ServiceCollection();
        services.AddSingleton(coordinator);
        await using ServiceProvider provider = services
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddBufferedBus();
                configuration.AddConsumer<BufferedConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var trigger = new BufferedConsumerTrigger(NewId.NextGuid());

        try
        {
            await harness.Bus.PublishAsync(trigger, cancellationToken);
            await coordinator.Buffered.Task.WaitAsync(timeout, cancellationToken);

            Assert.Empty(harness.Published.Snapshot<BufferedConsumerResult>());
            Assert.Empty(harness.Sent.Snapshot<BufferedConsumerSendResult>());

            coordinator.Release.TrySetResult();
            await coordinator.Flushed.Task.WaitAsync(timeout, cancellationToken);

            BufferedConsumerResult actual = Assert.Single(harness.Published
                .Snapshot<BufferedConsumerResult>())
                .Context.Message;
            BufferedConsumerSendResult sent = Assert.Single(harness.Sent
                .Snapshot<BufferedConsumerSendResult>())
                .Context.Message;
            Assert.Equal(new BufferedConsumerResult(trigger.CorrelationId), actual);
            Assert.Equal(new BufferedConsumerSendResult(trigger.CorrelationId), sent);
        }
        finally
        {
            coordinator.Release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "publish-flush-exactly-once")]
    public async Task BufferedPublish_FlushesExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "buffered-publish");
        HandlerTestHarness<TransactionalMessage> handler = harness.AddHandler<TransactionalMessage>();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new BufferedBusTestDriver(harness.Bus);
            var message = new TransactionalMessage(NewId.NextGuid(), "publish");

            await driver.Bus.PublishAsync(message, cancellationToken);

            Assert.Empty(harness.Published.Snapshot<TransactionalMessage>());

            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            IConsumedMessage<TransactionalMessage> received = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, received.Context.Message);
            Assert.Single(harness.Published.Snapshot<TransactionalMessage>());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "publish-send-endpoint-flushes-once")]
    public async Task PublishSendEndpoint_UsesTheSameSingleFlushBoundaryAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "buffered-publish-send-endpoint");
        HandlerTestHarness<TransactionalMessage> handler = harness.AddHandler<TransactionalMessage>();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new BufferedBusTestDriver(harness.Bus);
            ISendEndpoint endpoint = await driver.Bus.GetPublishSendEndpointAsync<TransactionalMessage>(TestContext.Current.CancellationToken);
            var message = new TransactionalMessage(NewId.NextGuid(), "publish-send-endpoint");

            await endpoint.SendAsync(message, cancellationToken);

            Assert.Empty(harness.Published.Snapshot<TransactionalMessage>());

            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            IConsumedMessage<TransactionalMessage> received = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, received.Context.Message);
            Assert.Single(harness.Published.Snapshot<TransactionalMessage>());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "every-publish-overload-shares-one-buffer")]
    public async Task EveryPublishOverload_UsesTheSameBufferedBoundaryAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "buffered-publish-overloads");
        HandlerTestHarness<TransactionalMessage> handler = harness.AddHandler<TransactionalMessage>();
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new BufferedBusTestDriver(harness.Bus);
            TransactionalMessage[] expected = Enumerable.Range(0, 10)
                .Select(index => new TransactionalMessage(NewId.NextGuid(), $"publish-{index}"))
                .ToArray();
            IPipe<PublishContext<TransactionalMessage>> typedPipe = Pipe.Empty<PublishContext<TransactionalMessage>>();
            IPipe<PublishContext> untypedPipe = Pipe.Empty<PublishContext>();

            await driver.Bus.PublishAsync(expected[0], cancellationToken);
            await driver.Bus.PublishAsync(expected[1], typedPipe, cancellationToken);
            await driver.Bus.PublishAsync(expected[2], untypedPipe, cancellationToken);
            await driver.Bus.Advanced().PublishAsync((object)expected[3], cancellationToken);
            await driver.Bus.Advanced().PublishAsync((object)expected[4], untypedPipe, cancellationToken);
            await driver.Bus.Advanced().PublishAsync((object)expected[5], typeof(TransactionalMessage), cancellationToken);
            await driver.Bus.Advanced().PublishAsync((object)expected[6], typeof(TransactionalMessage), untypedPipe, cancellationToken);
            await driver.Bus.PublishAsync<TransactionalMessage>(Values(expected[7]), cancellationToken);
            await driver.Bus.PublishAsync<TransactionalMessage>(Values(expected[8]), typedPipe, cancellationToken);
            await driver.Bus.PublishAsync<TransactionalMessage>(Values(expected[9]), untypedPipe, cancellationToken);

            Assert.Empty(harness.Published.Snapshot<TransactionalMessage>());

            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await handler.Consumed.SelectAsync(
                    observation => observation.Context.Message.CorrelationId == expected[^1].CorrelationId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(expected, harness.Published
                .Snapshot<TransactionalMessage>()
                .Select(item => item.Context.Message));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "send-flush-exactly-once")]
    public async Task BufferedSend_FlushesExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "buffered-send");
        HandlerTestHarness<TransactionalMessage> handler = harness.AddHandler<TransactionalMessage>();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new BufferedBusTestDriver(harness.Bus);
            ISendEndpoint endpoint = await driver.Bus.GetSendEndpointAsync(harness.InputQueueAddress, TestContext.Current.CancellationToken);
            var message = new TransactionalMessage(NewId.NextGuid(), "send");

            await endpoint.SendAsync(message, cancellationToken);

            Assert.Empty(harness.Sent.Snapshot<TransactionalMessage>());

            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            IConsumedMessage<TransactionalMessage> received = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, received.Context.Message);
            Assert.Single(harness.Sent.Snapshot<TransactionalMessage>());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "every-send-overload-shares-one-buffer")]
    public async Task EverySendOverload_UsesTheSameBufferedBoundaryAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "buffered-send-overloads");
        HandlerTestHarness<TransactionalMessage> handler = harness.AddHandler<TransactionalMessage>();
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new BufferedBusTestDriver(harness.Bus);
            ISendEndpoint endpoint = await driver.Bus.GetSendEndpointAsync(harness.InputQueueAddress, TestContext.Current.CancellationToken);
            TransactionalMessage[] expected = Enumerable.Range(0, 10)
                .Select(index => new TransactionalMessage(NewId.NextGuid(), $"send-{index}"))
                .ToArray();
            IPipe<SendContext<TransactionalMessage>> typedPipe = Pipe.Empty<SendContext<TransactionalMessage>>();
            IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();

            await endpoint.SendAsync(expected[0], cancellationToken);
            await endpoint.SendAsync(expected[1], typedPipe, cancellationToken);
            await endpoint.SendAsync(expected[2], untypedPipe, cancellationToken);
            await endpoint.Advanced().SendAsync((object)expected[3], cancellationToken);
            await endpoint.Advanced().SendAsync((object)expected[4], typeof(TransactionalMessage), cancellationToken);
            await endpoint.Advanced().SendAsync((object)expected[5], untypedPipe, cancellationToken);
            await endpoint.Advanced().SendAsync((object)expected[6], typeof(TransactionalMessage), untypedPipe, cancellationToken);
            await endpoint.SendAsync<TransactionalMessage>(Values(expected[7]), cancellationToken);
            await endpoint.SendAsync<TransactionalMessage>(Values(expected[8]), typedPipe, cancellationToken);
            await endpoint.SendAsync<TransactionalMessage>(Values(expected[9]), untypedPipe, cancellationToken);

            Assert.Empty(harness.Sent.Snapshot<TransactionalMessage>());

            await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await handler.Consumed.SelectAsync(
                    observation => observation.Context.Message.CorrelationId == expected[^1].CorrelationId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(expected, harness.Sent
                .Snapshot<TransactionalMessage>()
                .Select(item => item.Context.Message));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "fifo-single-drain-and-next-snapshot")]
    public async Task ConcurrentFlushes_AreSerializedAndActionsAddedDuringDrainUseTheNextSnapshotAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new BufferedBusTestDriver();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new ConcurrentQueue<string>();
        var active = 0;
        var maximumActive = 0;

        await driver.EnqueueAsync(async token =>
        {
            int current = Interlocked.Increment(ref active);
            InterlockedExtensions.Max(ref maximumActive, current);
            order.Enqueue("first");
            firstEntered.TrySetResult();
            try
            {
                await releaseFirst.Task.WaitAsync(timeout, token);
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }, cancellationToken);

        Task firstFlush = driver.Bus.FlushAsync(cancellationToken);
        Task secondFlush = Task.CompletedTask;
        await firstEntered.Task.WaitAsync(timeout, cancellationToken);

        try
        {
            await driver.EnqueueAsync(token =>
            {
                int current = Interlocked.Increment(ref active);
                InterlockedExtensions.Max(ref maximumActive, current);
                order.Enqueue("second");
                secondEntered.TrySetResult();
                Interlocked.Decrement(ref active);
                return Task.CompletedTask;
            }, cancellationToken);
            secondFlush = driver.Bus.FlushAsync(cancellationToken);

            Assert.Equal(["first"], order);
            Assert.False(secondEntered.Task.IsCompleted);
        }
        finally
        {
            releaseFirst.TrySetResult();
            await Task.WhenAll(firstFlush, secondFlush).WaitAsync(timeout, cancellationToken);
        }

        Assert.Equal(["first", "second"], order);
        Assert.True(secondEntered.Task.IsCompletedSuccessfully);
        Assert.Equal(1, maximumActive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "same-instance-reentrant-flush-fails-fast")]
    public async Task ReentrantFlush_FailsFastAndLeavesTheLockAndUnattemptedTailUsableAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new BufferedBusTestDriver();
        var releaseCapturedFlush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();
        Task capturedFlush = Task.CompletedTask;

        await driver.EnqueueAsync(async _ =>
        {
            order.Add("reentrant");
            capturedFlush = Task.Run(async () =>
            {
                await releaseCapturedFlush.Task.WaitAsync(timeout, cancellationToken);
                await driver.Bus.FlushAsync(cancellationToken);
            }, cancellationToken);
            await driver.Bus.FlushAsync(CancellationToken.None);
        }, cancellationToken);
        await driver.EnqueueAsync(_ =>
        {
            order.Add("tail");
            return Task.CompletedTask;
        }, cancellationToken);

        try
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken));

            Assert.Equal(
                "FlushAsync cannot be called recursively from an action being flushed by the same buffered bus.",
                actual.Message);
            Assert.Equal(["reentrant"], order);
        }
        finally
        {
            releaseCapturedFlush.TrySetResult();
        }

        await capturedFlush.WaitAsync(timeout, cancellationToken);

        Assert.Equal(["reentrant", "tail"], order);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "failure-preserves-unattempted-tail")]
    public async Task DispatchFailure_PreservesItsIdentityAndOnlyTheUnattemptedTailAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new BufferedBusTestDriver();
        var expected = new ExpectedDispatchException();
        var failureEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFailure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();

        await driver.EnqueueAsync(_ => RecordAsync("first"), cancellationToken);
        await driver.EnqueueAsync(async token =>
        {
            order.Add("failed");
            failureEntered.TrySetResult();
            await releaseFailure.Task.WaitAsync(timeout, token);
            throw expected;
        }, cancellationToken);
        await driver.EnqueueAsync(_ => RecordAsync("third"), cancellationToken);

        Task flush = driver.Bus.FlushAsync(cancellationToken);
        await failureEntered.Task.WaitAsync(timeout, cancellationToken);
        await driver.EnqueueAsync(_ => RecordAsync("added-during-failure"), cancellationToken);
        releaseFailure.TrySetResult();

        ExpectedDispatchException actual;
        try
        {
            actual = await Assert.ThrowsAsync<ExpectedDispatchException>(() => flush);
        }
        finally
        {
            releaseFailure.TrySetResult();
        }

        Assert.Same(expected, actual);
        Assert.Equal(["first", "failed"], order);

        await driver.Bus.FlushAsync(cancellationToken);
        await driver.Bus.FlushAsync(cancellationToken);

        Assert.Equal(["first", "failed", "third", "added-during-failure"], order);

        Task RecordAsync(string value)
        {
            order.Add(value);
            return Task.CompletedTask;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "enqueue-cancellation-does-not-create-work")]
    public async Task RequestedEnqueueCancellation_IsPreservedWithoutAddingTheActionAsync()
    {
        var driver = new BufferedBusTestDriver();
        using var source = new CancellationTokenSource();
        source.Cancel();
        var dispatchCount = 0;

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            driver.EnqueueAsync(_ =>
            {
                Interlocked.Increment(ref dispatchCount);
                return Task.CompletedTask;
            }, source.Token));

        await driver.Bus.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(source.Token, actual.CancellationToken);
        Assert.Equal(0, dispatchCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "flush-cancellation-retains-unattempted-work")]
    public async Task RequestedFlushCancellation_RetainsWorkAndPreservesTheFlushTokenAsync()
    {
        var driver = new BufferedBusTestDriver();
        using var source = new CancellationTokenSource();
        var order = new List<string>();
        await driver.EnqueueAsync(_ =>
        {
            order.Add("first");
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        await driver.EnqueueAsync(_ =>
        {
            order.Add("second");
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => driver.Bus.FlushAsync(source.Token));

        Assert.Equal(source.Token, actual.CancellationToken);
        Assert.Empty(order);

        await driver.Bus.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "flush-token-owns-dispatch")]
    public async Task Flush_UsesItsOwnTokenAfterTheEnqueueTokenLifetimeEndsAsync()
    {
        var driver = new BufferedBusTestDriver();
        using var enqueueSource = new CancellationTokenSource();
        using var flushSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CancellationToken observed = default;

        await driver.EnqueueAsync(token =>
        {
            observed = token;
            return Task.CompletedTask;
        }, enqueueSource.Token);
        enqueueSource.Cancel();

        await driver.Bus.FlushAsync(flushSource.Token);

        Assert.Equal(flushSource.Token, observed);
        Assert.NotEqual(enqueueSource.Token, observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS", "mid-dispatch-cancellation-preserves-tail")]
    public async Task CancellationDuringDispatch_DoesNotRetryTheAttemptedActionAndPreservesTheTailAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new BufferedBusTestDriver();
        using var flushSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();
        var firstAttempts = 0;

        await driver.EnqueueAsync(async token =>
        {
            Interlocked.Increment(ref firstAttempts);
            order.Add("first");
            firstEntered.TrySetResult();
            var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration = token.Register(() => canceled.TrySetCanceled(token));
            await canceled.Task;
        }, cancellationToken);
        await driver.EnqueueAsync(_ =>
        {
            order.Add("second");
            return Task.CompletedTask;
        }, cancellationToken);

        Task flush = driver.Bus.FlushAsync(flushSource.Token);
        await firstEntered.Task.WaitAsync(timeout, cancellationToken);
        flushSource.Cancel();
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flush);

        Assert.Equal(flushSource.Token, actual.CancellationToken);
        Assert.Equal(["first"], order);

        await driver.Bus.FlushAsync(cancellationToken);

        Assert.Equal(["first", "second"], order);
        Assert.Equal(1, firstAttempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS-BACKPRESSURE", "capacity-release-and-next-snapshot")]
    public async Task Capacity_BlocksTheNextWriterUntilFlushReleasesAReservationIntoTheNextSnapshotAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new BufferedBusTestDriver(capacity: 2);
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();

        await driver.EnqueueAsync(async token =>
        {
            order.Add("first");
            firstEntered.TrySetResult();
            await releaseFirst.Task.WaitAsync(timeout, token);
        }, cancellationToken);
        await driver.EnqueueAsync(_ =>
        {
            order.Add("second");
            return Task.CompletedTask;
        }, cancellationToken);

        Task thirdAdmission = driver.EnqueueAsync(_ =>
        {
            order.Add("third");
            return Task.CompletedTask;
        }, cancellationToken);
        Assert.False(thirdAdmission.IsCompleted);

        Task firstFlush = driver.Bus.FlushAsync(cancellationToken);
        await firstEntered.Task.WaitAsync(timeout, cancellationToken);
        try
        {
            await thirdAdmission.WaitAsync(timeout, cancellationToken);
            Assert.Equal(["first"], order);
        }
        finally
        {
            releaseFirst.TrySetResult();
            await firstFlush.WaitAsync(timeout, cancellationToken);
        }

        Assert.Equal(["first", "second"], order);

        await driver.Bus.FlushAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(["first", "second", "third"], order);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS-BACKPRESSURE", "blocked-admission-preserves-cancellation")]
    public async Task CapacityWait_PreservesTheRequestTokenAndDoesNotEnqueueCanceledWorkAsync()
    {
        var driver = new BufferedBusTestDriver(capacity: 1);
        var order = new List<string>();
        await driver.EnqueueAsync(_ =>
        {
            order.Add("accepted");
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        using var source = new CancellationTokenSource();

        Task blocked = driver.EnqueueAsync(_ =>
        {
            order.Add("canceled");
            return Task.CompletedTask;
        }, source.Token);
        Assert.False(blocked.IsCompleted);
        source.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blocked);
        await driver.Bus.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(source.Token, actual.CancellationToken);
        Assert.Equal(["accepted"], order);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-BUFFERED-BUS-BACKPRESSURE", "positive-capacity")]
    public void NonPositiveCapacity_IsRejected(int capacity)
    {
        ArgumentOutOfRangeException actual = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BufferedBusTestDriver(capacity));

        Assert.Equal("capacity", actual.ParamName);
        Assert.Equal(capacity, actual.ActualValue);
    }


    private static object Values(TransactionalMessage message) => new
    {
        message.CorrelationId,
        message.Value,
    };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, string purpose) =>
        new($"{purpose}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public sealed record TransactionalMessage : ICorrelatedBy<Guid>
    {
        public TransactionalMessage()
        {
        }

        public TransactionalMessage(Guid correlationId, string value)
        {
            CorrelationId = correlationId;
            Value = value;
        }

        public Guid CorrelationId { get; init; }

        public string Value { get; init; } = string.Empty;
    }

    public sealed record BufferedConsumerTrigger(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record BufferedConsumerResult(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record BufferedConsumerSendResult(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed class TransactionalMessageConsumer : IConsumer<TransactionalMessage>
    {
        public Task ConsumeAsync(ConsumeContext<TransactionalMessage> context) => Task.CompletedTask;
    }

    private sealed class BufferedConsumer(
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        IBufferedBus bufferedBus,
        BufferedConsumerCoordinator coordinator) : IConsumer<BufferedConsumerTrigger>
    {
        public async Task ConsumeAsync(ConsumeContext<BufferedConsumerTrigger> context)
        {
            try
            {
                await publishEndpoint.PublishAsync(new BufferedConsumerResult(context.Message.CorrelationId), context.CancellationToken);
                ISendEndpoint endpoint = await sendEndpointProvider.GetSendEndpointAsync(
                    new Uri($"loopback://localhost/buffered-consumer-send-{context.Message.CorrelationId:N}"));
                await endpoint.SendAsync(new BufferedConsumerSendResult(context.Message.CorrelationId), context.CancellationToken);
                coordinator.Buffered.TrySetResult();
                await coordinator.Release.Task.WaitAsync(coordinator.Timeout, context.CancellationToken);
                await bufferedBus.FlushAsync(context.CancellationToken);
                coordinator.Flushed.TrySetResult();
            }
            catch (Exception exception)
            {
                coordinator.Buffered.TrySetException(exception);
                coordinator.Flushed.TrySetException(exception);
                throw;
            }
        }
    }

    private sealed class BufferedConsumerCoordinator(TimeSpan timeout)
    {
        public TaskCompletionSource Buffered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Flushed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TimeSpan Timeout { get; } = timeout;
    }

    private sealed class ExpectedDispatchException : Exception;
}

internal static class InterlockedExtensions
{
    public static void Max(ref int target, int candidate)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref target);
            if (observed >= candidate)
                return;
        }
        while (Interlocked.CompareExchange(ref target, candidate, observed) != observed);
    }
}
