using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchDeliveryIntegrationTests
{
    private static readonly TimeSpan SizeTailTimeLimit = TimeSpan.FromSeconds(1);

    [Theory]
    [InlineData(SuccessMode.DefaultTimeLimit)]
    [InlineData(SuccessMode.InlineFiveAndTail)]
    [InlineData(SuccessMode.DefinitionFiveAndTail)]
    [InlineData(SuccessMode.HundredBySize)]
    [InlineData(SuccessMode.EndpointOutbox)]
    [InlineData(SuccessMode.RetryingEndpointOutbox)]
    [InlineData(SuccessMode.MessageOutbox)]
    [RequirementCoverage("REQ-VSB-BATCH-DELIVERY", "limits-definition-retry-and-outbox-matrix")]
    public async Task ConfiguredBatch_ProducesTheExactTerminalBatchesAsync(SuccessMode mode)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        int itemCount = mode switch
        {
            SuccessMode.InlineFiveAndTail or SuccessMode.DefinitionFiveAndTail => 6,
            SuccessMode.HundredBySize => 100,
            _ => 2,
        };
        int expectedResults = mode is SuccessMode.InlineFiveAndTail or SuccessMode.DefinitionFiveAndTail ? 2 : 1;
        await using ServiceProvider provider = BuildProvider(timeout, configuration => ConfigureSuccess(configuration, mode));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            BatchItem[] items = Enumerable.Range(0, itemCount)
                .Select(index => new BatchItem(NewId.NextGuid(), index))
                .ToArray();

            if (expectedResults == 1)
                await harness.Bus.PublishBatchAsync(items, cancellationToken);
            else
            {
                await harness.Bus.PublishBatchAsync(items[..5], cancellationToken);
                IPublishedMessage<BatchResult> sizeBatch = await harness.Published
                    .SelectAsync<BatchResult>(cancellationToken)
                    .FirstObservedAsync(cancellationToken: cancellationToken);
                Assert.Equal((5, BatchCompletionMode.Size), (sizeBatch.Context.Message.Count, sizeBatch.Context.Message.Mode));

                await harness.Bus.PublishAsync(items[5], cancellationToken);
            }

            Assert.Equal(expectedResults, await harness.Published
                .SelectAsync<BatchResult>(cancellationToken)
                .Take(expectedResults)
                .CountObservedAsync(TestContext.Current.CancellationToken));

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            BatchResult[] results = Snapshot(token => harness.Published.Select<BatchResult>(token))
                .Select(observation => observation.Context.Message)
                .ToArray();
            Assert.Equal(expectedResults, results.Length);
            Assert.Equal(items.Select(item => item.CorrelationId).Order(), results.SelectMany(result => result.ItemIds).Order());
            Assert.Equal(itemCount, results.Sum(result => result.Count));
            Assert.All(results, result => Assert.Equal(result.Count, result.ItemIds.Length));

            if (mode is SuccessMode.InlineFiveAndTail or SuccessMode.DefinitionFiveAndTail)
            {
                Assert.Contains(results, result => result is { Count: 5, Mode: BatchCompletionMode.Size });
                Assert.Contains(results, result => result is { Count: 1, Mode: BatchCompletionMode.Time });
            }
            else if (mode == SuccessMode.HundredBySize)
                Assert.Equal((100, BatchCompletionMode.Size), (results[0].Count, results[0].Mode));
            else
                Assert.Equal((2, BatchCompletionMode.Time), (results[0].Count, results[0].Mode));

            bool requiresOutbox = mode is SuccessMode.EndpointOutbox
                or SuccessMode.RetryingEndpointOutbox
                or SuccessMode.MessageOutbox
                or SuccessMode.DefinitionFiveAndTail;
            Assert.All(results, result => Assert.Equal(requiresOutbox, result.HasOutbox));
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(FailureMode.Plain)]
    [InlineData(FailureMode.EndpointRetry)]
    [InlineData(FailureMode.ConsumerRetry)]
    [InlineData(FailureMode.DelayedRedelivery)]
    [InlineData(FailureMode.ScheduledRedelivery)]
    [InlineData(FailureMode.OutboxRetry)]
    [RequirementCoverage("REQ-VSB-BATCH-FAULT-FANOUT", "one-terminal-fault-per-inner-message")]
    public async Task FaultingBatch_PublishesOneTerminalFaultPerInnerMessageAsync(FailureMode mode)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildProvider(timeout, configuration => ConfigureFailure(configuration, mode));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            BatchItem[] items =
            [
                new BatchItem(NewId.NextGuid(), 0),
                new BatchItem(NewId.NextGuid(), 1),
            ];
            await harness.Bus.PublishBatchAsync(items, cancellationToken);
            Assert.Equal(2, await harness.Published
                .SelectAsync<Fault<BatchItem>>(cancellationToken)
                .Take(2)
                .CountObservedAsync(TestContext.Current.CancellationToken));

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Fault<BatchItem>[] faults = Snapshot(token => harness.Published.Select<Fault<BatchItem>>(token))
                .Select(observation => observation.Context.Message)
                .ToArray();
            Assert.Equal(2, faults.Length);
            Assert.Equal(items.Select(item => item.CorrelationId).Order(), faults.Select(fault => fault.Message.CorrelationId).Order());
            Assert.All(faults, fault =>
            {
                ExceptionInfo exception = Assert.Single(fault.Exceptions);
                Assert.Equal(TypeCache<BatchFailureException>.ShortName, exception.ExceptionType);
            });
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DUPLICATE-SUPPRESSION", "same-message-id-is-consumed-once")]
    public async Task DuplicateMessageId_ProducesOneSingleItemBatchAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildProvider(timeout, configuration =>
            configuration.AddConsumer<BatchResultConsumer>(consumer => consumer.Options<BatchOptions>(options =>
                options.SetMessageLimit(2).SetTimeLimit(TimeSpan.FromMilliseconds(50)))));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            Guid messageId = NewId.NextGuid();
            var item = new BatchItem(NewId.NextGuid(), 0);
            await harness.Bus.PublishAsync(item, context => context.MessageId = messageId, cancellationToken);
            await harness.Bus.PublishAsync(item, context => context.MessageId = messageId, cancellationToken);
            IPublishedMessage<BatchResult> published = await harness.Published
                .SelectAsync<BatchResult>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, published.Context.Message.Count);
            Assert.Equal(BatchCompletionMode.Time, published.Context.Message.Mode);
            Assert.Equal(item.CorrelationId, Assert.Single(published.Context.Message.ItemIds));
            Assert.Single(Snapshot(token => harness.Consumed.Select<BatchItem>(token)));
            Assert.Single(Snapshot(token => harness.Published.Select<BatchResult>(token)));
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-OUTBOX", "failed-inner-publications-are-discarded")]
    public async Task FailedBatch_DiscardsEveryInnerContextPublicationFromTheOutboxAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildProvider(timeout, configuration =>
        {
            configuration.AddConsumer<FailingOutboxBatchConsumer>(consumer => consumer.Options<BatchOptions>(options =>
                options.SetMessageLimit(2)));
            configuration.AddConfigureEndpointsCallback((context, _, endpoint) => endpoint.UseVolatileOutbox(context));
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            IConsumerTestHarness<FailingOutboxBatchConsumer> consumer =
                harness.GetConsumerHarness<FailingOutboxBatchConsumer>();
            await harness.Bus.PublishBatchAsync(
                [new BatchItem(NewId.NextGuid(), 0), new BatchItem(NewId.NextGuid(), 1)],
                cancellationToken);
            IReceivedMessage<Batch<BatchItem>> failed = await consumer.Consumed
                .SelectAsync<Batch<BatchItem>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.IsType<BatchFailureException>(failed.Exception);
            Assert.Empty(Snapshot(token => harness.Published.Select<BatchResult>(token)));
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-BATCH-ERROR-TRANSPORT", "size-and-time-closed-failures-move-each-message")]
    public async Task FaultingSingleItemBatch_MovesTheOriginalMessageToTheErrorQueueAsync(int messageLimit)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"batch-error-{messageLimit}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var moved = NewSignal<ConsumeContext<ErrorBatchItem>>();
        string errorQueue = $"{harness.InputQueueName}_error";
        harness.InMemoryBusConfiguring += bus => bus.ReceiveEndpoint(errorQueue, endpoint =>
            endpoint.Handler<ErrorBatchItem>(context => CompleteAsync(moved, context)));
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Batch<ErrorBatchItem>(batch =>
        {
            batch.MessageLimit = messageLimit;
            batch.TimeLimit = TimeSpan.FromMilliseconds(50);
            batch.Consumer(() => new ErrorBatchConsumer());
        });

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var message = new ErrorBatchItem(NewId.NextGuid());
            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);
            ConsumeContext<ErrorBatchItem> error = await moved.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message.CorrelationId, error.Message.CorrelationId);
            Assert.Equal(new Uri(harness.BaseAddress, errorQueue), error.Advanced().ReceiveContext.InputAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-GROUPING", "guid-value-key-produces-three-exact-groups")]
    public Task GuidGrouping_ProducesBatchesOfOneTwoAndThreeAsync() =>
        RunGroupingAsync(GroupKeyMode.Guid);

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-GROUPING", "nullable-string-key-produces-three-exact-groups")]
    public Task StringGrouping_ProducesBatchesOfOneTwoAndThreeIncludingNullAsync() =>
        RunGroupingAsync(GroupKeyMode.String);

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BATCH", "four-messages-one-batch")]
    public async Task Mediator_DefaultBatchConsumerReceivesFourMessagesTogetherAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var delivered = NewSignal<Batch<MediatorBatchItem>>();
        var consumer = new MediatorBatchConsumer(delivered);
        IMediator mediator = Bus.Factory.CreateMediator(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Consumer(() => consumer);
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        MediatorBatchItem[] items = Enumerable.Range(0, 4)
            .Select(index => new MediatorBatchItem(NewId.NextGuid(), index))
            .ToArray();

        await Task.WhenAll(items.Select(item => mediator.SendAsync(item, cancellationToken)));
        Batch<MediatorBatchItem> batch = await delivered.Task.WaitAsync(timeout, cancellationToken);

        Assert.Equal(4, batch.Length);
        Assert.Equal(items.Select(item => item.CorrelationId).Order(), batch.Select(context => context.Message.CorrelationId).Order());
        Assert.Equal(BatchCompletionMode.Time, batch.Mode);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONCURRENCY", "one-thousand-exactly-once-with-overlap")]
    public async Task ConcurrentBatches_DeliverOneThousandMessageIdentitiesExactlyOnceAsync()
    {
        const int batchSize = 100;
        const int itemCount = 1000;
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var probe = new ExactlyOnceProbe(itemCount);
        using var harness = new InMemoryTestHarness($"batch-overlap-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.ConcurrentMessageLimit = batchSize * 4;
            endpoint.Batch<ExactlyOnceItem>(batch =>
            {
                batch.MessageLimit = batchSize;
                batch.TimeLimit = TimeSpan.FromMilliseconds(50);
                batch.ConcurrencyLimit = 4;
                batch.Consumer(() => new ExactlyOnceBatchConsumer(probe));
            });
        };
        Guid[] sent = Enumerable.Range(0, itemCount).Select(_ => NewId.NextGuid()).ToArray();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;
        try
        {
            Task[] sends = sent.Select(messageId => harness.InputQueueSendEndpoint.SendAsync(
                    new ExactlyOnceItem(messageId),
                    context => context.MessageId = messageId,
                    cancellationToken))
                .ToArray();
            await Task.WhenAll(sends).WaitAsync(timeout, cancellationToken);
            await probe.Completed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.True(probe.OverlapObserved);
            Assert.True(probe.MaximumOverlap >= 2);
            Assert.Empty(probe.Duplicates);
            Assert.Equal(sent.Order(), probe.Received.Order());
        }
        finally
        {
            probe.Release.TrySetResult();
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void ConfigureSuccess(IBusRegistrationConfigurator configuration, SuccessMode mode)
    {
        switch (mode)
        {
            case SuccessMode.DefaultTimeLimit:
                configuration.AddConsumer<BatchResultConsumer>();
                break;
            case SuccessMode.InlineFiveAndTail:
                configuration.AddConsumer<BatchResultConsumer>(consumer => consumer.Options<BatchOptions>(options =>
                    options.SetMessageLimit(5).SetTimeLimit(SizeTailTimeLimit).SetTimeLimitStart(BatchTimeLimitStart.FromLast)));
                break;
            case SuccessMode.DefinitionFiveAndTail:
                configuration.AddConsumer<BatchResultConsumer, BatchResultConsumerDefinition>();
                break;
            case SuccessMode.HundredBySize:
                configuration.AddConsumer<BatchResultConsumer>(consumer => consumer.Options<BatchOptions>(options =>
                    options.SetMessageLimit(100).SetTimeLimit(TimeSpan.FromSeconds(5))));
                break;
            case SuccessMode.EndpointOutbox:
                configuration.AddConsumer<OutboxBatchConsumer>();
                configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    endpoint.UseMessageRetry(retry => retry.Immediate(2));
                    endpoint.UseVolatileOutbox(context);
                });
                break;
            case SuccessMode.RetryingEndpointOutbox:
                configuration.AddConsumer<RetryingOutboxBatchConsumer>();
                configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    endpoint.UseMessageRetry(retry => retry.Immediate(2));
                    endpoint.UseVolatileOutbox(context);
                });
                break;
            case SuccessMode.MessageOutbox:
                configuration.AddConsumer<OutboxBatchConsumer>(consumer =>
                    consumer.Message<Batch<BatchItem>>(message => message.UseVolatileOutbox()));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown success mode.");
        }
    }

    private static void ConfigureFailure(IBusRegistrationConfigurator configuration, FailureMode mode)
    {
        if (mode == FailureMode.ConsumerRetry)
        {
            configuration.AddConsumer<FailingBatchConsumer>(consumer =>
            {
                consumer.UseMessageRetry(retry => retry.Immediate(1));
                consumer.Options<BatchOptions>(options => options.SetMessageLimit(2));
            });
        }
        else
            configuration.AddConsumer<FailingBatchConsumer>(consumer => consumer.Options<BatchOptions>(options => options.SetMessageLimit(2)));

        switch (mode)
        {
            case FailureMode.Plain:
            case FailureMode.ConsumerRetry:
                break;
            case FailureMode.EndpointRetry:
                configuration.AddConfigureEndpointsCallback((_, endpoint) => endpoint.UseMessageRetry(retry => retry.Immediate(1)));
                break;
            case FailureMode.DelayedRedelivery:
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                {
                    endpoint.UseDelayedRedelivery(redelivery => redelivery.Intervals(TimeSpan.FromMilliseconds(10)));
                    endpoint.UseMessageRetry(retry => retry.Immediate(1));
                });
                break;
            case FailureMode.ScheduledRedelivery:
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                {
                    endpoint.UseScheduledRedelivery(redelivery => redelivery.Intervals(TimeSpan.FromMilliseconds(10)));
                    endpoint.UseMessageRetry(retry => retry.Immediate(1));
                });
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
                break;
            case FailureMode.OutboxRetry:
                configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    endpoint.UseMessageRetry(retry => retry.Immediate(2));
                    endpoint.UseVolatileOutbox(context);
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown failure mode.");
        }
    }

    private static async Task RunGroupingAsync(GroupKeyMode mode)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildProvider(timeout, configuration =>
        {
            if (mode == GroupKeyMode.Guid)
            {
                configuration.AddConsumer<GuidGroupConsumer>(consumer => consumer.Options<BatchOptions>(options => options
                    .SetMessageLimit(10)
                    .SetTimeLimit(TimeSpan.FromSeconds(1))
                    .GroupBy<GroupedItem, Guid>(context => context.Message.GuidGroup)));
            }
            else
            {
                configuration.AddConsumer<StringGroupConsumer>(consumer => consumer.Options<BatchOptions>(options => options
                    .SetMessageLimit(10)
                    .SetTimeLimit(TimeSpan.FromSeconds(1))
                    .GroupBy<GroupedItem, string>(context => context.Message.StringGroup!)));
            }
        });
        ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            Guid first = NewId.NextGuid();
            Guid second = NewId.NextGuid();
            Guid third = NewId.NextGuid();
            GroupedItem[] items = mode == GroupKeyMode.Guid
                ?
                [
                    new(first, first, "unused"),
                    new(second, second, "unused"), new(second, second, "unused"),
                    new(third, third, "unused"), new(third, third, "unused"), new(third, third, "unused"),
                ]
                :
                [
                    new(NewId.NextGuid(), Guid.Empty, null),
                    new(NewId.NextGuid(), Guid.Empty, "two"), new(NewId.NextGuid(), Guid.Empty, "two"),
                    new(NewId.NextGuid(), Guid.Empty, "three"), new(NewId.NextGuid(), Guid.Empty, "three"),
                    new(NewId.NextGuid(), Guid.Empty, "three"),
                ];
            await harness.Bus.PublishBatchAsync(items, cancellationToken);
            Assert.Equal(3, await harness.Published
                .SelectAsync<GroupBatchResult>(cancellationToken)
                .Take(3)
                .CountObservedAsync(TestContext.Current.CancellationToken));

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;
            GroupBatchResult[] results = Snapshot(token => harness.Published.Select<GroupBatchResult>(token))
                .Select(observation => observation.Context.Message)
                .ToArray();

            Assert.Equal(new[] { 1, 2, 3 }, results.Select(result => result.Count).Order());
            Assert.Equal(items.Select(item => item.CorrelationId).Order(), results.SelectMany(result => result.ItemIds).Order());
            Assert.All(results, result => Assert.Equal(BatchCompletionMode.Time, result.Mode));
            if (mode == GroupKeyMode.Guid)
                Assert.Equal(new[] { first, second, third }.Order(), results.Select(result => result.GuidKey).Order());
            else
                Assert.Equal(new string?[] { null, "three", "two" }.Order(), results.Select(result => result.StringKey).Order());
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildProvider(TimeSpan timeout, Action<IBusRegistrationConfigurator> configure) =>
        new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configure(configuration);
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

    private static T[] Snapshot<T>(Func<CancellationToken, IEnumerable<T>> source)
    {
        using var completed = new CancellationTokenSource();
        completed.Cancel();
        return source(completed.Token).ToArray();
    }

    private static Task CompleteAsync<T>(TaskCompletionSource<ConsumeContext<T>> signal, ConsumeContext<T> context)
        where T : class
    {
        signal.TrySetResult(context);
        return Task.CompletedTask;
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum SuccessMode
    {
        DefaultTimeLimit,
        InlineFiveAndTail,
        DefinitionFiveAndTail,
        HundredBySize,
        EndpointOutbox,
        RetryingEndpointOutbox,
        MessageOutbox,
    }

    public enum FailureMode
    {
        Plain,
        EndpointRetry,
        ConsumerRetry,
        DelayedRedelivery,
        ScheduledRedelivery,
        OutboxRetry,
    }

    private enum GroupKeyMode
    {
        Guid,
        String,
    }

    private sealed record BatchItem(Guid CorrelationId, int Index) : CorrelatedBy<Guid>;

    private sealed record BatchResult(Guid[] ItemIds, int Count, BatchCompletionMode Mode, bool HasOutbox);

    private sealed class BatchResultConsumer : IConsumer<Batch<BatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<BatchItem>> context) => PublishResultAsync(context);
    }

    private sealed class BatchResultConsumerDefinition : ConsumerDefinition<BatchResultConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<BatchResultConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseVolatileOutbox(context);
            consumerConfigurator.Options<BatchOptions>(options =>
                options.SetMessageLimit(5).SetTimeLimit(SizeTailTimeLimit).SetTimeLimitStart(BatchTimeLimitStart.FromLast));
        }
    }

    private sealed class OutboxBatchConsumer : IConsumer<Batch<BatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<BatchItem>> context)
        {
            Assert.True(context.TryGetPayload<InMemoryOutboxConsumeContext>(out _));
            return PublishResultAsync(context);
        }
    }

    private sealed class RetryingOutboxBatchConsumer : IConsumer<Batch<BatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<BatchItem>> context)
        {
            Assert.True(context.TryGetPayload<InMemoryOutboxConsumeContext>(out _));
            if (context.Advanced().GetRetryCount() == 0)
                throw new BatchFailureException("The first outbox attempt must be retried.");

            return PublishResultAsync(context);
        }
    }

    private sealed class FailingBatchConsumer : IConsumer<Batch<BatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<BatchItem>> context) =>
            throw new BatchFailureException("The batch consumer failed.");
    }

    private sealed class FailingOutboxBatchConsumer : IConsumer<Batch<BatchItem>>
    {
        public async Task ConsumeAsync(ConsumeContext<Batch<BatchItem>> context)
        {
            foreach (ConsumeContext<BatchItem> item in context.Message)
                await item.Advanced().PublishAsync(CreateResult(context), item.CancellationToken);

            throw new BatchFailureException("The batch and its buffered publications must fail together.");
        }
    }

    private static Task PublishResultAsync(ConsumeContext<Batch<BatchItem>> context) =>
        context.Advanced().PublishAsync(CreateResult(context), context.CancellationToken);

    private static BatchResult CreateResult(ConsumeContext<Batch<BatchItem>> context) =>
        new(
            context.Message.Select(item => item.Message.CorrelationId).ToArray(),
            context.Message.Length,
            context.Message.Mode,
            context.TryGetPayload<InMemoryOutboxConsumeContext>(out _));

    private sealed record ErrorBatchItem(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class ErrorBatchConsumer : IConsumer<Batch<ErrorBatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<ErrorBatchItem>> context) =>
            throw new BatchFailureException("Move this batch to the error transport.");
    }

    private sealed record GroupedItem(Guid CorrelationId, Guid GuidGroup, string? StringGroup) : CorrelatedBy<Guid>;

    private sealed record GroupBatchResult(Guid[] ItemIds, int Count, BatchCompletionMode Mode, Guid GuidKey, string? StringKey);

    private sealed class GuidGroupConsumer : IConsumer<Batch<GroupedItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<GroupedItem>> context) => context.Advanced().PublishAsync(new GroupBatchResult(
            context.Message.Select(item => item.Message.CorrelationId).ToArray(),
            context.Message.Length,
            context.Message.Mode,
            context.Message[0].Message.GuidGroup,
            null), context.CancellationToken);
    }

    private sealed class StringGroupConsumer : IConsumer<Batch<GroupedItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<GroupedItem>> context) => context.Advanced().PublishAsync(new GroupBatchResult(
            context.Message.Select(item => item.Message.CorrelationId).ToArray(),
            context.Message.Length,
            context.Message.Mode,
            Guid.Empty,
            context.Message[0].Message.StringGroup), context.CancellationToken);
    }

    private sealed record MediatorBatchItem(Guid CorrelationId, int Index) : CorrelatedBy<Guid>;

    private sealed class MediatorBatchConsumer(TaskCompletionSource<Batch<MediatorBatchItem>> delivered) :
        IConsumer<Batch<MediatorBatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<MediatorBatchItem>> context)
        {
            delivered.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed record ExactlyOnceItem(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class ExactlyOnceBatchConsumer(ExactlyOnceProbe probe) : IConsumer<Batch<ExactlyOnceItem>>
    {
        public async Task ConsumeAsync(ConsumeContext<Batch<ExactlyOnceItem>> context)
        {
            await probe.EnterAsync(context.CancellationToken);
            probe.Record(context.Message);
        }
    }

    private sealed class ExactlyOnceProbe(int expected)
    {
        private readonly HashSet<Guid> _duplicates = [];
        private readonly HashSet<Guid> _received = [];
        private readonly object _sync = new();
        private int _inside;
        private int _maximumOverlap;

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Guid[] Duplicates
        {
            get
            {
                lock (_sync)
                    return _duplicates.ToArray();
            }
        }

        public int MaximumOverlap => Volatile.Read(ref _maximumOverlap);

        public bool OverlapObserved => Release.Task.IsCompletedSuccessfully;

        public Guid[] Received
        {
            get
            {
                lock (_sync)
                    return _received.ToArray();
            }
        }

        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            int inside = Interlocked.Increment(ref _inside);
            UpdateMaximum(ref _maximumOverlap, inside);
            if (inside >= 2)
                Release.TrySetResult();

            try
            {
                await Release.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _inside);
            }
        }

        public void Record(Batch<ExactlyOnceItem> batch)
        {
            lock (_sync)
            {
                foreach (ConsumeContext<ExactlyOnceItem> item in batch)
                {
                    Guid id = item.MessageId ?? throw new InvalidOperationException("Every input owns a MessageId.");
                    if (!_received.Add(id))
                        _duplicates.Add(id);
                }

                if (_received.Count == expected)
                    Completed.TrySetResult();
            }
        }

        private static void UpdateMaximum(ref int maximum, int candidate)
        {
            int observed;
            while (candidate > (observed = Volatile.Read(ref maximum)))
            {
                if (Interlocked.CompareExchange(ref maximum, candidate, observed) == observed)
                    return;
            }
        }
    }

    private sealed class BatchFailureException(string message) : Exception(message);
}
