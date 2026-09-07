using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.AzureTable.Saga;

public sealed class AzureTableSagaConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "etag-conflict-reloads-and-retries-without-lost-update")]
    public async Task ConcurrentUpdates_ReloadAfterAnEtagConflictWithoutLosingEitherUpdateAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureTable)
            .OperationTimeout!.Value;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("SagaConcurrency", cancellationToken);
        var handler = new ConcurrentHandlerProbe(timeout);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(handler)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<ConcurrentStateMachine, ConcurrentState, ConcurrentStateDefinition>()
                    .AzureTableRepository(repository => repository.TableClientFactory(() => fixture.Table));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            Guid firstCommandId = Guid.NewGuid();
            Guid secondCommandId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<ConcurrentState>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new BeginConcurrentSaga(sagaId), cancellationToken);
            await harness.Published
                .SelectAsync<ConcurrentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await Task.WhenAll(
                endpoint.SendAsync(new IncrementConcurrentSaga(sagaId, firstCommandId), cancellationToken),
                endpoint.SendAsync(new IncrementConcurrentSaga(sagaId, secondCommandId), cancellationToken));
            await handler.BothInitialAttemptsEntered.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<ConcurrentSagaIncremented> first = await harness.Published
                .SelectAsync<ConcurrentSagaIncremented>(
                    observed => observed.Context.Message.CommandId == firstCommandId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IPublishedMessage<ConcurrentSagaIncremented> second = await harness.Published
                .SelectAsync<ConcurrentSagaIncremented>(
                    observed => observed.Context.Message.CommandId == secondCommandId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            var repository = (ILoadSagaRepository<ConcurrentState>)AzureTableSagaRepository<ConcurrentState>
                .Create(() => fixture.Table);
            ConcurrentState persisted = Assert.IsType<ConcurrentState>(
                await repository.LoadAsync(sagaId, TestContext.Current.CancellationToken));
            using var completed = new CancellationTokenSource();
            completed.Cancel();
            ConcurrentSagaIncremented[] published = harness.Published
                .Select<ConcurrentSagaIncremented>(completed.Token)
                .Where(observed => observed.Context.Message.CorrelationId == sagaId)
                .Select(observed => observed.Context.Message)
                .ToArray();

            Assert.Equal([2, 3], new[] { first.Context.Message.Counter, second.Context.Message.Counter }.Order());
            Assert.Equal(3, persisted.Counter);
            Assert.Equal(3, handler.InvocationCount);
            Assert.Equal(2, published.Length);
            Assert.Equal(
                new[] { firstCommandId, secondCommandId }.Order(),
                published.Select(message => message.CommandId).Order());
        }
        finally
        {
            handler.Release();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record BeginConcurrentSaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record IncrementConcurrentSaga(Guid CorrelationId, Guid CommandId) : CorrelatedBy<Guid>;

    public sealed record ConcurrentSagaStarted(Guid CorrelationId);

    public sealed record ConcurrentSagaIncremented(Guid CorrelationId, Guid CommandId, int Counter);

    public sealed class ConcurrentState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public int Counter { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ConcurrentStateMachine : ViciOneServiceBusStateMachine<ConcurrentState>
    {
        public ConcurrentStateMachine(ConcurrentHandlerProbe handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            InstanceState(state => state.CurrentState);
            Event(() => Begin, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => Increment, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Initially(
                When(Begin)
                    .Then(context => context.Saga.Counter = 1)
                    .Publish(context => new ConcurrentSagaStarted(context.Saga.CorrelationId))
                    .TransitionTo(Active));
            During(Active,
                When(Increment)
                    .ThenAwaited(context => handler.EnterAsync(context.CancellationToken))
                    .Then(context => context.Saga.Counter++)
                    .Publish(context => new ConcurrentSagaIncremented(
                        context.Saga.CorrelationId,
                        context.Message.CommandId,
                        context.Saga.Counter)));
        }

        public State Active { get; private set; } = null!;

        public Event<BeginConcurrentSaga> Begin { get; private set; } = null!;

        public Event<IncrementConcurrentSaga> Increment { get; private set; } = null!;
    }

    private sealed class ConcurrentStateDefinition : SagaDefinition<ConcurrentState>
    {
        public ConcurrentStateDefinition()
        {
            ConcurrentMessageLimit = 2;
        }

        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ConcurrentState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.ConcurrentMessageLimit = 2;
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            sagaConfigurator.UseVolatileOutbox(context);
        }
    }

    public sealed class ConcurrentHandlerProbe(TimeSpan timeout)
    {
        private readonly TaskCompletionSource _bothInitialAttemptsEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _invocationCount;

        public Task BothInitialAttemptsEntered => _bothInitialAttemptsEntered.Task;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            int invocation = Interlocked.Increment(ref _invocationCount);
            if (invocation > 2)
                return;

            if (invocation == 2)
            {
                _bothInitialAttemptsEntered.TrySetResult();
                _release.TrySetResult();
            }

            await _release.Task.WaitAsync(timeout, cancellationToken);
        }

        public void Release() => _release.TrySetResult();
    }
}
