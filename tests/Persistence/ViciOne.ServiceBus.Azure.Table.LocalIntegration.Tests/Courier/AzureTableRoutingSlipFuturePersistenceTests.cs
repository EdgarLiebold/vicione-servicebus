using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Courier;

public sealed class AzureTableRoutingSlipFuturePersistenceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-ROUTING-SLIP", "completed-slip-persists-terminal-result")]
    public async Task CompletedRoutingSlip_PersistsTheActivityVariablesAndTerminalResultAsync()
    {
        await using RoutingSlipFutureFixture fixture = await RoutingSlipFutureFixture.StartAsync("routing-future-completed");
        var command = new TransformValue(NewId.NextGuid(), 14, fail: false);

        Response<ValueTransformed> response = await fixture.RequestAsync(command);
        FutureState persisted = await fixture.ReadFutureAsync(command.CorrelationId);

        Assert.Equal(command.CorrelationId, response.Message.CorrelationId);
        Assert.Equal(42, response.Message.Value);
        Assert.Equal(1, fixture.ExecutionAttempts);
        Assert.NotNull(persisted.Completed);
        Assert.Null(persisted.Faulted);
        Assert.Empty(persisted.Pending);
        Assert.Empty(persisted.Faults);
        FutureMessage result = Assert.Single(persisted.Results).Value;
        Assert.True(result.HasMessageType<ValueTransformed>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-FUTURE-ROUTING-SLIP", "faulted-slip-persists-original-activity-failure")]
    public async Task FaultedRoutingSlip_PersistsAndPublishesTheOriginalActivityFailureAsync()
    {
        await using RoutingSlipFutureFixture fixture = await RoutingSlipFutureFixture.StartAsync("routing-future-faulted");
        var command = new TransformValue(NewId.NextGuid(), 14, fail: true);

        RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() => fixture.RequestAsync(command));
        FutureState persisted = await fixture.ReadFutureAsync(command.CorrelationId);

        Fault<TransformValue> fault = Assert.IsAssignableFrom<Fault<TransformValue>>(exception.Fault);
        ExceptionInfo original = Assert.Single(fault.Exceptions, candidate =>
            candidate.ExceptionType == TypeCache<ExpectedRoutingSlipFailure>.ShortName);
        Assert.Equal(command.CorrelationId, fault.Message.CorrelationId);
        Assert.Contains(command.CorrelationId.ToString("D"), original.Message, StringComparison.Ordinal);
        Assert.Equal(1, fixture.ExecutionAttempts);
        Assert.Null(persisted.Completed);
        Assert.NotNull(persisted.Faulted);
        Assert.Empty(persisted.Pending);
        Assert.Empty(persisted.Results);
        FutureMessage storedFault = Assert.Single(persisted.Faults).Value;
        Assert.True(storedFault.HasMessageType<Fault<TransformValue>>());
    }

    public sealed class TransformValue : CorrelatedBy<Guid>
    {
        public TransformValue()
        {
        }

        public TransformValue(Guid correlationId, int value, bool fail)
        {
            CorrelationId = correlationId;
            Value = value;
            Fail = fail;
        }

        public Guid CorrelationId { get; init; }
        public int Value { get; init; }
        public bool Fail { get; init; }
    }

    public sealed class ValueTransformed
    {
        public Guid CorrelationId { get; init; }
        public int Value { get; init; }
    }

    public sealed class TransformArguments
    {
        public Guid CorrelationId { get; init; }
        public int Value { get; init; }
        public bool Fail { get; init; }
    }

    public sealed class TransformValueFuture : Future<TransformValue, ValueTransformed>
    {
        public const string ActivityName = "TransformValue";
        public const string ResultVariable = "TransformedValue";

        public TransformValueFuture(RoutingSlipDestination destination)
        {
            ConfigureCommand(configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));

            ExecuteRoutingSlip(routingSlip =>
            {
                routingSlip.BuildItinerary((context, builder) =>
                {
                    builder.AddActivity(ActivityName, destination.ExecuteAddress, new TransformArguments
                    {
                        CorrelationId = context.Message.CorrelationId,
                        Value = context.Message.Value,
                        Fail = context.Message.Fail,
                    });
                    return Task.CompletedTask;
                });
                routingSlip.OnRoutingSlipCompleted(result =>
                    result.SetResultInitializer(context => new
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        Value = context.GetVariable<int>(ResultVariable),
                    }));
            });
        }
    }

    public sealed class TransformValueActivity(RoutingSlipExecutionProbe probe) : IExecuteActivity<TransformArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TransformArguments> context)
        {
            probe.RecordExecution();
            if (context.Arguments.Fail)
                return Task.FromException<ExecutionResult>(new ExpectedRoutingSlipFailure(context.Arguments.CorrelationId));

            return Task.FromResult(context.CompletedWithVariables(new
            {
                TransformedValue = checked(context.Arguments.Value * 3),
            }));
        }
    }

    public sealed class RoutingSlipDestination(Uri executeAddress)
    {
        public Uri ExecuteAddress { get; } = executeAddress;
    }

    public sealed class RoutingSlipExecutionProbe
    {
        private int _executionAttempts;

        public int ExecutionAttempts => Volatile.Read(ref _executionAttempts);

        public void RecordExecution() => Interlocked.Increment(ref _executionAttempts);
    }

    public sealed class ExpectedRoutingSlipFailure : Exception
    {
        public ExpectedRoutingSlipFailure(Guid correlationId)
            : base($"Routing slip future '{correlationId:D}' failed as requested.")
        {
        }
    }

    private sealed class RoutingSlipFutureFixture : IAsyncDisposable
    {
        private readonly AzureTableTestTable _table;
        private readonly ServiceProvider _provider;
        private readonly RoutingSlipExecutionProbe _probe;

        private RoutingSlipFutureFixture(
            AzureTableTestTable table,
            ServiceProvider provider,
            ITestHarness harness,
            RoutingSlipExecutionProbe probe)
        {
            _table = table;
            _provider = provider;
            Harness = harness;
            _probe = probe;
        }

        public int ExecutionAttempts => _probe.ExecutionAttempts;
        public ITestHarness Harness { get; }

        public static async Task<RoutingSlipFutureFixture> StartAsync(string purpose)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            AzureTableTestTable table = await AzureTableTestTable.CreateAsync(purpose, cancellationToken);
            try
            {
                string executeQueueName = $"routing-future-activity-{NewId.NextGuid():N}";
                var destination = new RoutingSlipDestination(new Uri($"loopback://localhost/{executeQueueName}"));
                var probe = new RoutingSlipExecutionProbe();
                var services = new ServiceCollection();
                services.AddSingleton(destination);
                services.AddSingleton(probe);
                services.AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetKebabCaseEndpointNameFormatter();
                    configuration.AddFuture<TransformValueFuture>();
                    configuration.AddSagaRepository<FutureState>()
                        .UseAzureTable(repository => repository.UseTableClientFactory(() => table.Table));
                    configuration.UsingInMemory((context, bus) =>
                    {
                        bus.ReceiveEndpoint(executeQueueName, endpoint =>
                            endpoint.ExecuteActivityHost<TransformValueActivity, TransformArguments>(
                                () => new TransformValueActivity(probe)));
                        bus.ConfigureEndpoints(context);
                    });
                });
                ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
                try
                {
                    ITestHarness harness = await provider.StartTestHarnessAsync()
                        .WaitAsync(OperationTimeout(), cancellationToken);
                    return new RoutingSlipFutureFixture(table, provider, harness, probe);
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

        public Task<Response<ValueTransformed>> RequestAsync(TransformValue command)
        {
            IRequestClient<TransformValue> client = Harness.GetRequestClient<TransformValue>();
            return client.GetResponseAsync<ValueTransformed>(command, TestContext.Current.CancellationToken)
                .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }

        public async Task<FutureState> ReadFutureAsync(Guid correlationId)
        {
            var repository = (ILoadSagaRepository<FutureState>)AzureTableSagaRepository
                .Create<FutureState>(() => _table.Table);
            return await repository.LoadAsync(correlationId)
                ?? throw new InvalidOperationException($"Routing-slip future '{correlationId:D}' was not persisted.");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Harness.StopAsync(CancellationToken.None)
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
