namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.AzureTable;

using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AzureTableFuturePersistenceTests
{
    private const string CalculationFutureEndpointPath = "/calculation-future";

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-REGISTRATION", "explicit-definition-persists-completed-result")]
    public async Task ExplicitFutureDefinition_CompletesAndPersistsTheResult()
    {
        await using FutureFixture fixture = await FutureFixture.StartAsync("future-explicit", useShortcutRegistration: false);
        var command = new CalculateValue(NewId.NextGuid(), 21, fail: false);

        Response<ValueCalculated> response = await fixture.Request(command);
        IReceivedMessage<CalculateValue> consumedCommand = await fixture.Harness.Consumed
            .SelectAsync<CalculateValue>(
                message => message.Context.Message.CorrelationId == command.CorrelationId
                    && IsConsumedByCalculationFuture(message.Context),
                TestContext.Current.CancellationToken)
            .First();
        IReceivedMessage<ValueCalculated> consumedResult = await fixture.Harness.Consumed
            .SelectAsync<ValueCalculated>(
                message => message.Context.Message.CorrelationId == command.CorrelationId
                    && IsConsumedByCalculationFuture(message.Context),
                TestContext.Current.CancellationToken)
            .First();
        FutureState persisted = await fixture.ReadFuture(command.CorrelationId);

        Assert.Equal(command.CorrelationId, response.Message.CorrelationId);
        Assert.Equal(42, response.Message.Value);
        Assert.Equal(1, fixture.Attempts.Count);
        Assert.Equal(command.CorrelationId, persisted.CorrelationId);
        Assert.Equal(Assert.IsType<DateTime>(consumedCommand.Context.SentTime), persisted.Created);
        Assert.Equal(Assert.IsType<DateTime>(consumedResult.Context.SentTime), persisted.Completed);
        Assert.Null(persisted.Faulted);
        Assert.Empty(persisted.Pending);
        Assert.Single(persisted.Results);
        Assert.Empty(persisted.Faults);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-DURABILITY", "completed-result-is-reused-without-reexecution")]
    public async Task CompletedFuture_ReusesThePersistedResultWithoutReexecutingTheConsumer()
    {
        await using FutureFixture fixture = await FutureFixture.StartAsync("future-durable-result", useShortcutRegistration: true);
        var command = new CalculateValue(NewId.NextGuid(), 7, fail: false);

        Response<ValueCalculated> first = await fixture.Request(command);
        Response<ValueCalculated> second = await fixture.Request(command);
        FutureState persisted = await fixture.ReadFuture(command.CorrelationId);

        Assert.Equal(command.CorrelationId, first.Message.CorrelationId);
        Assert.Equal(14, first.Message.Value);
        Assert.Equal(first.Message.CorrelationId, second.Message.CorrelationId);
        Assert.Equal(first.Message.Value, second.Message.Value);
        Assert.Equal(1, fixture.Attempts.Count);
        Assert.NotNull(persisted.Completed);
        Assert.Null(persisted.Faulted);
        Assert.Single(persisted.Results);
        Assert.Empty(persisted.Faults);
        Assert.True(persisted.HasSubscriptions());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-DURABILITY", "fault-is-reused-without-reexecution")]
    public async Task FaultedFuture_ReusesThePersistedFaultWithoutReexecutingTheConsumer()
    {
        await using FutureFixture fixture = await FutureFixture.StartAsync("future-durable-fault", useShortcutRegistration: true);
        var command = new CalculateValue(NewId.NextGuid(), 5, fail: true);

        RequestFaultException first = await Assert.ThrowsAsync<RequestFaultException>(() => fixture.Request(command));
        RequestFaultException second = await Assert.ThrowsAsync<RequestFaultException>(() => fixture.Request(command));
        FutureState persisted = await fixture.ReadFuture(command.CorrelationId);

        AssertFault(first, command);
        AssertFault(second, command);
        Assert.Equal(1, fixture.Attempts.Count);
        Assert.Null(persisted.Completed);
        Assert.NotNull(persisted.Faulted);
        Assert.Empty(persisted.Results);
        Assert.Single(persisted.Faults);
        Assert.True(persisted.HasSubscriptions());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-FAN-IN", "all-pending-results-complete-once")]
    public async Task FanInFuture_CompletesAfterEveryPersistedPendingRequest()
    {
        await using FutureFixture fixture = await FutureFixture.StartAsync("future-fan-in", useShortcutRegistration: true);
        Guid correlationId = NewId.NextGuid();
        var command = new AggregateValues(correlationId, [
            new CalculationItem(NewId.NextGuid(), 3, fail: false),
            new CalculationItem(NewId.NextGuid(), 5, fail: false),
            new CalculationItem(NewId.NextGuid(), 7, fail: false),
        ]);

        Response<ValuesAggregated> response = await fixture.Request<AggregateValues, ValuesAggregated>(command);
        Response<ValuesAggregated> repeated = await fixture.Request<AggregateValues, ValuesAggregated>(command);
        FutureState persisted = await fixture.ReadFuture(correlationId);

        Assert.Equal(correlationId, response.Message.CorrelationId);
        Assert.Equal(15, response.Message.Total);
        Assert.Equal(3, response.Message.ItemCount);
        Assert.Equal(response.Message.CorrelationId, repeated.Message.CorrelationId);
        Assert.Equal(response.Message.Total, repeated.Message.Total);
        Assert.Equal(response.Message.ItemCount, repeated.Message.ItemCount);
        Assert.Equal(3, fixture.PartAttempts.Count);
        Assert.Equal(command.Items.Select(item => item.ItemId).Order(), fixture.PartAttempts.ItemIds.Order());
        Assert.NotNull(persisted.Completed);
        Assert.Null(persisted.Faulted);
        Assert.Empty(persisted.Pending);
        Assert.Equal(
            command.Items.Select(item => item.ItemId).Append(correlationId).Order(),
            persisted.Results.Keys.Order());
        Assert.True(persisted.Results[correlationId].HasMessageType<ValuesAggregated>());
        Assert.All(command.Items, item =>
            Assert.True(persisted.Results[item.ItemId].HasMessageType<PartCalculated>()));
        Assert.Empty(persisted.Faults);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-FAN-IN", "one-failed-part-persists-terminal-fault")]
    public async Task FanInFuture_PersistsOneTerminalFaultWhenAnyPartFails()
    {
        await using FutureFixture fixture = await FutureFixture.StartAsync("future-fan-in-fault", useShortcutRegistration: true);
        Guid correlationId = NewId.NextGuid();
        Guid failingItemId = NewId.NextGuid();
        var command = new AggregateValues(correlationId, [
            new CalculationItem(NewId.NextGuid(), 3, fail: false),
            new CalculationItem(failingItemId, 5, fail: true),
            new CalculationItem(NewId.NextGuid(), 7, fail: false),
        ]);

        RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
            fixture.Request<AggregateValues, ValuesAggregated>(command));
        RequestFaultException repeated = await Assert.ThrowsAsync<RequestFaultException>(() =>
            fixture.Request<AggregateValues, ValuesAggregated>(command));
        FutureState persisted = await fixture.ReadFuture(correlationId);

        Fault<AggregateValues> fault = Assert.IsAssignableFrom<Fault<AggregateValues>>(exception.Fault);
        Fault<AggregateValues> repeatedFault = Assert.IsAssignableFrom<Fault<AggregateValues>>(repeated.Fault);
        Assert.Equal(correlationId, fault.Message.CorrelationId);
        Assert.Equal(fault.Message.CorrelationId, repeatedFault.Message.CorrelationId);
        Assert.Equal(fault.Exceptions.Select(item => item.ExceptionType), repeatedFault.Exceptions.Select(item => item.ExceptionType));
        Assert.Contains(fault.Exceptions, item =>
            item.ExceptionType == TypeCache<ExpectedPartFailure>.ShortName
            && item.Message.Contains(failingItemId.ToString("D"), StringComparison.Ordinal));
        Assert.Equal(3, fixture.PartAttempts.Count);
        Assert.Null(persisted.Completed);
        Assert.NotNull(persisted.Faulted);
        Assert.Empty(persisted.Pending);
        Assert.Equal(
            command.Items.Where(item => !item.Fail).Select(item => item.ItemId).Order(),
            persisted.Results.Keys.Order());
        Assert.All(persisted.Results.Values, result => Assert.True(result.HasMessageType<PartCalculated>()));
        Assert.Equal(new[] { failingItemId, correlationId }.Order(), persisted.Faults.Keys.Order());
        Assert.True(persisted.Faults[failingItemId].HasMessageType<Fault<CalculatePart>>());
        Assert.True(persisted.Faults[correlationId].HasMessageType<Fault<AggregateValues>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-COMPOSITION", "nested-futures-persist-variables-and-reuse-terminal-result")]
    public async Task ComposedFuture_PersistsNestedBranchesAndReusesTheTerminalResult()
    {
        await using FutureFixture fixture = await FutureFixture.StartAsync("future-composed", useShortcutRegistration: true);
        Guid correlationId = NewId.NextGuid();
        Guid calculationId = NewId.NextGuid();
        Guid aggregationId = NewId.NextGuid();
        var command = new ComposeValues(
            correlationId,
            calculationId,
            aggregationId,
            calculationValue: 11,
            [
                new CalculationItem(NewId.NextGuid(), 2, fail: false),
                new CalculationItem(NewId.NextGuid(), 4, fail: false),
            ]);

        Response<ValuesComposed> first = await fixture.Request<ComposeValues, ValuesComposed>(command);
        Response<ValuesComposed> repeated = await fixture.Request<ComposeValues, ValuesComposed>(command);
        FutureState parent = await fixture.ReadFuture(correlationId);
        FutureState calculation = await fixture.ReadFuture(calculationId);
        FutureState aggregation = await fixture.ReadFuture(aggregationId);

        Assert.Equal(correlationId, first.Message.CorrelationId);
        Assert.Equal(28, first.Message.Total);
        Assert.Equal(2, first.Message.BranchCount);
        Assert.Equal(first.Message.CorrelationId, repeated.Message.CorrelationId);
        Assert.Equal(first.Message.Total, repeated.Message.Total);
        Assert.Equal(first.Message.BranchCount, repeated.Message.BranchCount);
        Assert.Equal(1, fixture.Attempts.Count);
        Assert.Equal(2, fixture.PartAttempts.Count);

        Assert.NotNull(parent.Completed);
        Assert.Null(parent.Faulted);
        Assert.Empty(parent.Pending);
        Assert.True(parent.HasVariables());
        Assert.Equal(
            new[] { calculationId, aggregationId, correlationId }.Order(),
            parent.Results.Keys.Order());
        Assert.True(parent.Results[calculationId].HasMessageType<ValueCalculated>());
        Assert.True(parent.Results[aggregationId].HasMessageType<ValuesAggregated>());
        Assert.True(parent.Results[correlationId].HasMessageType<ValuesComposed>());

        Assert.NotNull(calculation.Completed);
        Assert.Null(calculation.Faulted);
        Assert.True(calculation.Results[calculationId].HasMessageType<ValueCalculated>());
        Assert.NotNull(aggregation.Completed);
        Assert.Null(aggregation.Faulted);
        Assert.Empty(aggregation.Pending);
        Assert.True(aggregation.Results[aggregationId].HasMessageType<ValuesAggregated>());
    }

    public sealed class CalculateValue : CorrelatedBy<Guid>
    {
        public CalculateValue()
        {
        }

        public CalculateValue(Guid correlationId, int value, bool fail)
        {
            CorrelationId = correlationId;
            Value = value;
            Fail = fail;
        }

        public Guid CorrelationId { get; init; }
        public int Value { get; init; }
        public bool Fail { get; init; }
    }

    public sealed class ValueCalculated
    {
        public ValueCalculated()
        {
        }

        public ValueCalculated(Guid correlationId, int value)
        {
            CorrelationId = correlationId;
            Value = value;
        }

        public Guid CorrelationId { get; init; }
        public int Value { get; init; }
    }

    public sealed class AggregateValues : CorrelatedBy<Guid>
    {
        public AggregateValues()
        {
        }

        public AggregateValues(Guid correlationId, CalculationItem[] items)
        {
            CorrelationId = correlationId;
            Items = items;
        }

        public Guid CorrelationId { get; init; }
        public CalculationItem[] Items { get; init; } = [];
    }

    public sealed class CalculationItem
    {
        public CalculationItem()
        {
        }

        public CalculationItem(Guid itemId, int value, bool fail)
        {
            ItemId = itemId;
            Value = value;
            Fail = fail;
        }

        public Guid ItemId { get; init; }
        public int Value { get; init; }
        public bool Fail { get; init; }
    }

    public sealed class CalculatePart
    {
        public Guid ItemId { get; init; }
        public int Value { get; init; }
        public bool Fail { get; init; }
    }

    public sealed class PartCalculated
    {
        public Guid ItemId { get; init; }
        public int Value { get; init; }
    }

    public sealed class ValuesAggregated
    {
        public Guid CorrelationId { get; init; }
        public int Total { get; init; }
        public int ItemCount { get; init; }
    }

    public sealed class ComposeValues : CorrelatedBy<Guid>
    {
        public ComposeValues()
        {
        }

        public ComposeValues(
            Guid correlationId,
            Guid calculationId,
            Guid aggregationId,
            int calculationValue,
            CalculationItem[] items)
        {
            CorrelationId = correlationId;
            CalculationId = calculationId;
            AggregationId = aggregationId;
            CalculationValue = calculationValue;
            Items = items;
        }

        public Guid CorrelationId { get; init; }
        public Guid CalculationId { get; init; }
        public Guid AggregationId { get; init; }
        public int CalculationValue { get; init; }
        public CalculationItem[] Items { get; init; } = [];
    }

    public sealed class ValuesComposed
    {
        public Guid CorrelationId { get; init; }
        public int Total { get; init; }
        public int BranchCount { get; init; }
    }

    public sealed class CalculationVariable
    {
        public int Value { get; init; }
    }

    public sealed class CalculationFuture : RequestConsumerFuture<CalculateValue, ValueCalculated>
    {
        public CalculationFuture(IFutureDefinition<CalculationFuture> definition) : base(definition)
        {
            ConfigureCommand(configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));
        }
    }

    public sealed class CalculationConsumer(FutureAttemptProbe attempts) : IConsumer<CalculateValue>
    {
        public Task Consume(ConsumeContext<CalculateValue> context)
        {
            attempts.Record(context.Message.CorrelationId);
            if (context.Message.Fail)
                throw new ExpectedFutureFailure(context.Message.CorrelationId);

            return context.RespondAsync(new ValueCalculated(
                context.Message.CorrelationId,
                checked(context.Message.Value * 2)));
        }
    }

    public sealed class CalculationConsumerDefinition :
        FutureRequestConsumerDefinition<CalculationConsumer, CalculateValue>;

    public sealed class CalculationFutureDefinition :
        RequestConsumerFutureDefinition<CalculationFuture, CalculationConsumer, CalculateValue, ValueCalculated>
    {
        public CalculationFutureDefinition(IConsumerDefinition<CalculationConsumer> consumerDefinition)
            : base(consumerDefinition)
        {
        }
    }

    public sealed class AggregateFuture : Future<AggregateValues, ValuesAggregated>
    {
        public AggregateFuture()
        {
            ConfigureCommand(configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));

            SendRequests<CalculationItem, CalculatePart>(command => command.Items, request =>
                {
                    request.UsingRequestInitializer(context => new
                    {
                        context.Message.ItemId,
                        context.Message.Value,
                        context.Message.Fail,
                    });
                    request.TrackPendingRequest(message => message.ItemId);
                })
                .OnResponseReceived<PartCalculated>(response =>
                    response.CompletePendingRequest(message => message.ItemId));

            WhenAllCompleted(result => result.SetCompletedUsingInitializer(context => new
            {
                CorrelationId = context.Saga.CorrelationId,
                Total = context.SelectResults<PartCalculated>().Sum(item => item.Value),
                ItemCount = context.Saga.Results.Count,
            }));

            WhenAllCompletedOrFaulted(_ => { });
        }
    }

    public sealed class ComposedFuture : Future<ComposeValues, ValuesComposed>
    {
        private const string CalculationVariableName = "calculation";

        public ComposedFuture()
        {
            ConfigureCommand(configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));

            SendRequest<CalculateValue>(request =>
                {
                    request.UsingRequestInitializer(context => new
                    {
                        CorrelationId = context.Message.CalculationId,
                        Value = context.Message.CalculationValue,
                        Fail = false,
                    });
                    request.TrackPendingRequest(message => message.CorrelationId);
                })
                .OnResponseReceived<ValueCalculated>(response =>
                {
                    response.CompletePendingRequest(message => message.CorrelationId);
                    response.WhenReceived(binder => binder.SetVariable(
                        CalculationVariableName,
                        context => new CalculationVariable { Value = context.Message.Value }));
                });

            SendRequest<AggregateValues>(request =>
                {
                    request.UsingRequestInitializer(context => new
                    {
                        CorrelationId = context.Message.AggregationId,
                        context.Message.Items,
                    });
                    request.TrackPendingRequest(message => message.CorrelationId);
                })
                .OnResponseReceived<ValuesAggregated>(response =>
                    response.CompletePendingRequest(message => message.CorrelationId));

            WhenAllCompleted(result => result.SetCompletedUsingInitializer(context =>
            {
                if (!context.TryGetVariable(CalculationVariableName, out CalculationVariable calculation))
                    throw new InvalidOperationException("The persisted calculation variable is missing.");

                ValuesAggregated aggregation = context.SelectResults<ValuesAggregated>().Single();
                return new
                {
                    CorrelationId = context.Saga.CorrelationId,
                    Total = checked(calculation.Value + aggregation.Total),
                    BranchCount = 2,
                };
            }));
        }
    }

    public sealed class PartConsumer(PartAttemptProbe attempts) : IConsumer<CalculatePart>
    {
        public Task Consume(ConsumeContext<CalculatePart> context)
        {
            attempts.Record(context.Message.ItemId);
            if (context.Message.Fail)
                throw new ExpectedPartFailure(context.Message.ItemId);

            return context.RespondAsync(new PartCalculated
            {
                ItemId = context.Message.ItemId,
                Value = context.Message.Value,
            });
        }
    }

    public sealed class ExpectedFutureFailure : Exception
    {
        public ExpectedFutureFailure(Guid correlationId)
            : base($"Future '{correlationId:D}' failed as requested.")
        {
        }
    }

    public sealed class ExpectedPartFailure : Exception
    {
        public ExpectedPartFailure(Guid itemId)
            : base($"Part '{itemId:D}' failed as requested.")
        {
        }
    }

    public sealed class FutureAttemptProbe
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Record(Guid correlationId)
        {
            if (correlationId == Guid.Empty)
                throw new ArgumentException("A future attempt must carry a non-empty correlation identifier.", nameof(correlationId));

            Interlocked.Increment(ref _count);
        }
    }

    public sealed class PartAttemptProbe
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<Guid> _itemIds = new();

        public int Count => _itemIds.Count;
        public IReadOnlyCollection<Guid> ItemIds => _itemIds.ToArray();

        public void Record(Guid itemId)
        {
            if (itemId == Guid.Empty)
                throw new ArgumentException("A future part must carry a non-empty identifier.", nameof(itemId));

            _itemIds.Enqueue(itemId);
        }
    }

    private static void AssertFault(RequestFaultException exception, CalculateValue command)
    {
        Fault<CalculateValue> fault = Assert.IsAssignableFrom<Fault<CalculateValue>>(exception.Fault);
        ExceptionInfo original = Assert.Single(fault.Exceptions, candidate =>
            candidate.ExceptionType == TypeCache<ExpectedFutureFailure>.ShortName);

        Assert.Equal(command.CorrelationId, fault.Message.CorrelationId);
        Assert.Equal(TypeCache<ExpectedFutureFailure>.ShortName, original.ExceptionType);
        Assert.Contains(command.CorrelationId.ToString("D"), original.Message, StringComparison.Ordinal);
    }

    private static bool IsConsumedByCalculationFuture(ConsumeContext context) =>
        string.Equals(
            context.ReceiveContext.InputAddress.AbsolutePath,
            CalculationFutureEndpointPath,
            StringComparison.Ordinal);

    private sealed class FutureFixture : IAsyncDisposable
    {
        private readonly AzureTableTestTable _table;
        private readonly ServiceProvider _provider;

        private FutureFixture(
            AzureTableTestTable table,
            ServiceProvider provider,
            ITestHarness harness,
            FutureAttemptProbe attempts,
            PartAttemptProbe partAttempts)
        {
            _table = table;
            _provider = provider;
            Harness = harness;
            Attempts = attempts;
            PartAttempts = partAttempts;
        }

        public FutureAttemptProbe Attempts { get; }
        public ITestHarness Harness { get; }
        public PartAttemptProbe PartAttempts { get; }

        public static async Task<FutureFixture> StartAsync(string purpose, bool useShortcutRegistration)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            AzureTableTestTable table = await AzureTableTestTable.CreateAsync(purpose, cancellationToken);
            try
            {
                var attempts = new FutureAttemptProbe();
                var partAttempts = new PartAttemptProbe();
                var services = new ServiceCollection();
                services.AddSingleton(attempts);
                services.AddSingleton(partAttempts);
                services.AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetKebabCaseEndpointNameFormatter();
                    if (useShortcutRegistration)
                    {
                        configuration.AddFutureRequestConsumer<
                            CalculationFuture,
                            CalculationConsumer,
                            CalculateValue,
                            ValueCalculated>();
                    }
                    else
                    {
                        configuration.AddConsumer<CalculationConsumer, CalculationConsumerDefinition>();
                        configuration.AddFuture<CalculationFuture, CalculationFutureDefinition>();
                    }

                    configuration.AddConsumer<PartConsumer>();
                    configuration.AddFuture<AggregateFuture>();
                    configuration.AddFuture<ComposedFuture>();

                    configuration.AddSagaRepository<FutureState>()
                        .AzureTableRepository(repository => repository.TableClientFactory(() => table.Table));
                    configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
                });
                ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
                try
                {
                    ITestHarness harness = await provider.StartTestHarness()
                        .WaitAsync(OperationTimeout(), cancellationToken);
                    return new FutureFixture(table, provider, harness, attempts, partAttempts);
                }
                catch
                {
                    await provider.DisposeAsync();
                    throw;
                }
            }
            catch
            {
                await table.DisposeAsync();
                throw;
            }
        }

        public Task<Response<ValueCalculated>> Request(CalculateValue command)
        {
            return Request<CalculateValue, ValueCalculated>(command);
        }

        public Task<Response<TResponse>> Request<TRequest, TResponse>(TRequest command)
            where TRequest : class
            where TResponse : class
        {
            IRequestClient<TRequest> client = Harness.GetRequestClient<TRequest>();
            return client.GetResponse<TResponse>(command, TestContext.Current.CancellationToken)
                .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }

        public async Task<FutureState> ReadFuture(Guid correlationId)
        {
            var repository = (ILoadSagaRepository<FutureState>)AzureTableSagaRepository<FutureState>
                .Create(() => _table.Table);
            return await repository.Load(correlationId)
                ?? throw new InvalidOperationException($"Future '{correlationId:D}' was not persisted.");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Harness.Stop(CancellationToken.None)
                    .WaitAsync(OperationTimeout(), CancellationToken.None);
            }
            finally
            {
                await _provider.DisposeAsync();
                await _table.DisposeAsync();
            }
        }

        private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureTable)
            .OperationTimeout!.Value;
    }
}
