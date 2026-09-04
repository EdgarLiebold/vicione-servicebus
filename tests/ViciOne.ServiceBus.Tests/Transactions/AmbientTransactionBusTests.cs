using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transactions;

public sealed class AmbientTransactionBusTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "no-transaction-send-failure-is-immediate")]
    public async Task NoAmbientTransaction_SendPropagatesTransportFailureImmediatelyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "ambient-immediate-failure");
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new AmbientTransactionBusTestDriver(harness.Bus);
            ISendEndpoint endpoint = await driver.Bus.GetSendEndpointAsync(harness.InputQueueAddress, TestContext.Current.CancellationToken);
            var expected = new ExpectedDispatchException();

            ExpectedDispatchException actual = await Assert.ThrowsAsync<ExpectedDispatchException>(() =>
                endpoint.SendAsync(
                    new TransactionalMessage(NewId.NextGuid(), "immediate-failure"),
                    Pipe.Execute<SendContext<TransactionalMessage>>(_ => throw expected),
                    cancellationToken));

            Assert.Same(expected, actual);
            Assert.Empty(harness.Sent.Select<TransactionalMessage>(SnapshotOnlyToken()));
            Assert.Equal(0, driver.PendingTransactionCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "commit-send-failure-aborts-with-cause")]
    public async Task CommittedTransaction_SendFailureAbortsWithOriginalCauseAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "ambient-commit-failure");
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new AmbientTransactionBusTestDriver(harness.Bus);
            ISendEndpoint endpoint = await driver.Bus.GetSendEndpointAsync(harness.InputQueueAddress, TestContext.Current.CancellationToken);
            var expected = new ExpectedDispatchException();

            TransactionAbortedException actual = await Assert.ThrowsAsync<TransactionAbortedException>(ExecuteAsync);

            Assert.Same(expected, actual.InnerException);
            Assert.Empty(harness.Sent.Select<TransactionalMessage>(SnapshotOnlyToken()));
            Assert.Equal(0, driver.PendingTransactionCount);

            async Task ExecuteAsync()
            {
                using var transaction = CreateTransactionScope(timeout);
                await endpoint.SendAsync(
                    new TransactionalMessage(NewId.NextGuid(), "commit-failure"),
                    Pipe.Execute<SendContext<TransactionalMessage>>(_ => throw expected),
                    cancellationToken);
                transaction.Complete();
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "registered-scoped-publish-uses-ambient-capability")]
    public async Task Registration_RoutesScopedPublishEndpointThroughAmbientContractAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddAmbientTransactionBus();
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
            using (var transaction = CreateTransactionScope(timeout))
            {
                IPublishEndpoint publishEndpoint = harness.Scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                await publishEndpoint.PublishAsync(message, cancellationToken);

                Assert.Empty(harness.Published.Select<TransactionalMessage>(SnapshotOnlyToken()));
                transaction.Complete();
            }

            IReceivedMessage<TransactionalMessage> received = await consumer.Consumed
                .SelectAsync<TransactionalMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(message, received.Context.Message);
            Assert.Single(harness.Published.Select<TransactionalMessage>(SnapshotOnlyToken()));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "consumer-scope-retains-ambient-boundary")]
    public async Task ConsumerScope_RoutesScopedPublishEndpointThroughAmbientContractAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new AmbientConsumerCoordinator(timeout);
        var services = new ServiceCollection();
        services.AddSingleton(coordinator);
        await using ServiceProvider provider = services
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddAmbientTransactionBus();
                configuration.AddConsumer<AmbientConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var trigger = new AmbientConsumerTrigger(NewId.NextGuid());

        try
        {
            await harness.Bus.PublishAsync(trigger, cancellationToken);
            await coordinator.Enlisted.Task.WaitAsync(timeout, cancellationToken);

            Assert.Empty(harness.Published.Select<AmbientConsumerResult>(SnapshotOnlyToken()));
            Assert.Empty(harness.Sent.Select<AmbientConsumerSendResult>(SnapshotOnlyToken()));

            coordinator.Release.TrySetResult();
            await coordinator.Committed.Task.WaitAsync(timeout, cancellationToken);

            AmbientConsumerResult actual = Assert.Single(harness.Published
                .Select<AmbientConsumerResult>(SnapshotOnlyToken()))
                .Context.Message;
            AmbientConsumerSendResult sent = Assert.Single(harness.Sent
                .Select<AmbientConsumerSendResult>(SnapshotOnlyToken()))
                .Context.Message;
            Assert.Equal(new AmbientConsumerResult(trigger.CorrelationId), actual);
            Assert.Equal(new AmbientConsumerSendResult(trigger.CorrelationId), sent);
        }
        finally
        {
            coordinator.Release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "commit-publish-fifo-exactly-once")]
    public async Task CommittedTransaction_PublishesExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "ambient-commit-publish");
        HandlerTestHarness<TransactionalMessage> handler = harness.Handler<TransactionalMessage>();
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new AmbientTransactionBusTestDriver(harness.Bus);
            IAmbientTransactionBus bus = driver.Bus;
            var first = new TransactionalMessage(NewId.NextGuid(), "first");
            var second = new TransactionalMessage(NewId.NextGuid(), "second");

            using (var transaction = CreateTransactionScope(timeout))
            {
                await bus.PublishAsync(first, cancellationToken);
                await bus.PublishAsync(second, cancellationToken);
                Assert.Empty(harness.Published.Select<TransactionalMessage>(SnapshotOnlyToken()));
                transaction.Complete();
            }

            await handler.Consumed.SelectAsync(
                    observation => observation.Context.Message.CorrelationId == second.CorrelationId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            TransactionalMessage[] published = harness.Published
                .Select<TransactionalMessage>(SnapshotOnlyToken())
                .Select(item => item.Context.Message)
                .ToArray();

            Assert.Equal([first, second], published);
            Assert.Equal(0, driver.PendingTransactionCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "commit-send-fifo-exactly-once")]
    public async Task CommittedTransaction_SendsExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "ambient-commit-send");
        HandlerTestHarness<TransactionalMessage> handler = harness.Handler<TransactionalMessage>();
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new AmbientTransactionBusTestDriver(harness.Bus);
            IAmbientTransactionBus bus = driver.Bus;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(harness.InputQueueAddress, TestContext.Current.CancellationToken);
            var first = new TransactionalMessage(NewId.NextGuid(), "first");
            var second = new TransactionalMessage(NewId.NextGuid(), "second");

            using (var transaction = CreateTransactionScope(timeout))
            {
                await endpoint.SendAsync(first, cancellationToken);
                await endpoint.SendAsync(second, cancellationToken);
                Assert.Empty(harness.Sent.Select<TransactionalMessage>(SnapshotOnlyToken()));
                transaction.Complete();
            }

            await handler.Consumed.SelectAsync(
                    observation => observation.Context.Message.CorrelationId == second.CorrelationId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            TransactionalMessage[] sent = harness.Sent
                .Select<TransactionalMessage>(SnapshotOnlyToken())
                .Select(item => item.Context.Message)
                .ToArray();

            Assert.Equal([first, second], sent);
            Assert.Equal(0, driver.PendingTransactionCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "rollback-discards-publish")]
    public async Task RolledBackTransaction_DiscardsPublishAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "ambient-rollback-publish");
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new AmbientTransactionBusTestDriver(harness.Bus);
            IAmbientTransactionBus bus = driver.Bus;

            using (CreateTransactionScope(timeout))
                await bus.PublishAsync(new TransactionalMessage(NewId.NextGuid(), "discard-publish"), cancellationToken);

            Assert.Empty(harness.Published.Select<TransactionalMessage>(SnapshotOnlyToken()));
            Assert.Equal(0, driver.PendingTransactionCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "rollback-discards-send")]
    public async Task RolledBackTransaction_DiscardsSendAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, "ambient-rollback-send");
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var driver = new AmbientTransactionBusTestDriver(harness.Bus);
            IAmbientTransactionBus bus = driver.Bus;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(harness.InputQueueAddress, TestContext.Current.CancellationToken);

            using (CreateTransactionScope(timeout))
                await endpoint.SendAsync(new TransactionalMessage(NewId.NextGuid(), "discard-send"), cancellationToken);

            Assert.Empty(harness.Sent.Select<TransactionalMessage>(SnapshotOnlyToken()));
            Assert.Equal(0, driver.PendingTransactionCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "in-doubt-discards-and-closes")]
    public async Task InDoubt_DiscardsPendingActionsAndClosesTheEnlistmentAsync()
    {
        var driver = new AmbientTransactionNotificationTestDriver();
        var dispatchCount = 0;
        await driver.EnqueueAsync(_ =>
        {
            Interlocked.Increment(ref dispatchCount);
            return Task.CompletedTask;
        });

        driver.CompleteInDoubtThroughARealEnlistment();

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driver.EnqueueAsync(_ =>
            {
                Interlocked.Increment(ref dispatchCount);
                return Task.CompletedTask;
            }));

        Assert.Contains("no longer accepting", actual.Message, StringComparison.Ordinal);
        Assert.Equal(0, dispatchCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "independent-transaction-state")]
    public async Task IndependentTransactions_CommitAndRollbackOnlyTheirOwnFifoActionsAsync()
    {
        var driver = new AmbientTransactionBusTestDriver();
        using var committed = new CommittableTransaction();
        using var rolledBack = new CommittableTransaction();
        var observed = new List<string>();

        using (var scope = new TransactionScope(committed, TransactionScopeAsyncFlowOption.Enabled))
        {
            await driver.EnqueueAsync(_ => RecordAsync("commit-1"), TestContext.Current.CancellationToken);
            await driver.EnqueueAsync(_ => RecordAsync("commit-2"), TestContext.Current.CancellationToken);
            scope.Complete();
        }
        using (var scope = new TransactionScope(rolledBack, TransactionScopeAsyncFlowOption.Enabled))
        {
            await driver.EnqueueAsync(_ => RecordAsync("rollback"), TestContext.Current.CancellationToken);
            scope.Complete();
        }

        Assert.Equal(2, driver.PendingTransactionCount);

        rolledBack.Rollback();
        Assert.Equal(1, driver.PendingTransactionCount);
        committed.Commit();

        Assert.Equal(["commit-1", "commit-2"], observed);
        Assert.Equal(0, driver.PendingTransactionCount);

        Task RecordAsync(string value)
        {
            observed.Add(value);
            return Task.CompletedTask;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "requested-enqueue-cancellation-is-not-enlisted")]
    public async Task RequestedCallerCancellation_IsPreservedWithoutEnlistingTheActionAsync()
    {
        var driver = new AmbientTransactionBusTestDriver();
        using var source = new CancellationTokenSource();
        using var transaction = new CommittableTransaction();
        var dispatchCount = 0;
        source.Cancel();

        OperationCanceledException actual;
        using (var scope = new TransactionScope(transaction, TransactionScopeAsyncFlowOption.Enabled))
        {
            actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                driver.EnqueueAsync(_ =>
                {
                    Interlocked.Increment(ref dispatchCount);
                    return Task.CompletedTask;
                }, source.Token));
            scope.Complete();
        }
        transaction.Commit();

        Assert.Equal(source.Token, actual.CancellationToken);
        Assert.Equal(0, dispatchCount);
        Assert.Equal(0, driver.PendingTransactionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "transaction-lifetime-owns-prepared-dispatch")]
    public async Task Commit_UsesTheTransactionLifetimeAfterTheEnqueueTokenEndsAsync()
    {
        var driver = new AmbientTransactionBusTestDriver();
        using var enqueueSource = new CancellationTokenSource();
        using var transaction = new CommittableTransaction();
        CancellationToken observed = new(canceled: true);
        var dispatchCount = 0;

        using (var scope = new TransactionScope(transaction, TransactionScopeAsyncFlowOption.Enabled))
        {
            await driver.EnqueueAsync(token =>
            {
                observed = token;
                Interlocked.Increment(ref dispatchCount);
                return Task.CompletedTask;
            }, enqueueSource.Token);
            scope.Complete();
        }

        enqueueSource.Cancel();
        transaction.Commit();

        Assert.Equal(CancellationToken.None, observed);
        Assert.Equal(1, dispatchCount);
        Assert.Equal(0, driver.PendingTransactionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "requested-immediate-cancellation-does-not-dispatch")]
    public async Task RequestedCallerCancellationWithoutATransaction_DoesNotDispatchAsync()
    {
        var driver = new AmbientTransactionBusTestDriver();
        using var source = new CancellationTokenSource();
        var dispatchCount = 0;
        source.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            driver.EnqueueAsync(_ =>
            {
                Interlocked.Increment(ref dispatchCount);
                return Task.CompletedTask;
            }, source.Token));

        Assert.Equal(source.Token, actual.CancellationToken);
        Assert.Equal(0, dispatchCount);
        Assert.Equal(0, driver.PendingTransactionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AMBIENT-TRANSACTION-BUS", "concurrent-enqueue-enlists-once")]
    public async Task ConcurrentEnqueuesInOneTransaction_ExecuteEveryActionExactlyOnceAsync()
    {
        const int actionCount = 8;
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new AmbientTransactionBusTestDriver();
        using var transaction = new CommittableTransaction();
        using var barrier = new Barrier(actionCount + 1);
        var observed = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();
        Task[] enqueues = Enumerable.Range(0, actionCount)
            .Select(index => Task.Factory.StartNew(async () =>
            {
                using var scope = new TransactionScope(transaction, TransactionScopeAsyncFlowOption.Enabled);
                if (!barrier.SignalAndWait(timeout, cancellationToken))
                    throw new TimeoutException("The concurrent ambient-enqueue barrier did not complete.");
                await driver.EnqueueAsync(_ =>
                {
                    observed.AddOrUpdate(index, 1, static (_, count) => count + 1);
                    return Task.CompletedTask;
                }, cancellationToken);
                scope.Complete();
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap())
            .ToArray();

        Assert.True(barrier.SignalAndWait(timeout, cancellationToken));
        await Task.WhenAll(enqueues).WaitAsync(timeout, cancellationToken);
        transaction.Commit();

        Assert.Equal(Enumerable.Range(0, actionCount), observed.Keys.Order());
        Assert.All(observed.Values, count => Assert.Equal(1, count));
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, string purpose) =>
        new($"{purpose}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TransactionScope CreateTransactionScope(TimeSpan timeout) =>
        new(
            TransactionScopeOption.RequiresNew,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = timeout,
            },
            TransactionScopeAsyncFlowOption.Enabled);

    public sealed record TransactionalMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record AmbientConsumerTrigger(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record AmbientConsumerResult(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record AmbientConsumerSendResult(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class TransactionalMessageConsumer : IConsumer<TransactionalMessage>
    {
        public Task ConsumeAsync(ConsumeContext<TransactionalMessage> context) => Task.CompletedTask;
    }

    private sealed class AmbientConsumer(
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        AmbientConsumerCoordinator coordinator) : IConsumer<AmbientConsumerTrigger>
    {
        public async Task ConsumeAsync(ConsumeContext<AmbientConsumerTrigger> context)
        {
            try
            {
                using (var transaction = CreateTransactionScope(coordinator.Timeout))
                {
                    await publishEndpoint.PublishAsync(new AmbientConsumerResult(context.Message.CorrelationId), context.CancellationToken);
                    ISendEndpoint endpoint = await sendEndpointProvider.GetSendEndpointAsync(
                        new Uri($"loopback://localhost/ambient-consumer-send-{context.Message.CorrelationId:N}"));
                    await endpoint.SendAsync(new AmbientConsumerSendResult(context.Message.CorrelationId), context.CancellationToken);
                    coordinator.Enlisted.TrySetResult();
                    await coordinator.Release.Task.WaitAsync(coordinator.Timeout, context.CancellationToken);
                    transaction.Complete();
                }

                coordinator.Committed.TrySetResult();
            }
            catch (Exception exception)
            {
                coordinator.Enlisted.TrySetException(exception);
                coordinator.Committed.TrySetException(exception);
                throw;
            }
        }
    }

    private sealed class AmbientConsumerCoordinator(TimeSpan timeout)
    {
        public TaskCompletionSource Committed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Enlisted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TimeSpan Timeout { get; } = timeout;
    }

    private sealed class ExpectedDispatchException : Exception;
}
