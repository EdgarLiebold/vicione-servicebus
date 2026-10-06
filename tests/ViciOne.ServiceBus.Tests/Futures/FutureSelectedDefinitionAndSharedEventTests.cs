using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;
using CommandA = ViciOne.ServiceBus.Tests.Futures.FutureRegistrationBoundaryTests.BuiltinCommandA;
using CommandB = ViciOne.ServiceBus.Tests.Futures.FutureRegistrationBoundaryTests.BuiltinCommandB;
using ConsumerA = ViciOne.ServiceBus.Tests.Futures.FutureRegistrationBoundaryTests.BuiltinConsumerA;
using ConsumerB = ViciOne.ServiceBus.Tests.Futures.FutureRegistrationBoundaryTests.BuiltinConsumerB;
using ResultA = ViciOne.ServiceBus.Tests.Futures.FutureRegistrationBoundaryTests.BuiltinResultA;
using ResultB = ViciOne.ServiceBus.Tests.Futures.FutureRegistrationBoundaryTests.BuiltinResultB;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureSelectedDefinitionAndSharedEventTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "typed-companion-activation-resolves-owner-specific-consumer-definition")]
    public async Task TypedCompanions_ActivateTheirDefinitionsThroughNormalPublicCompositionAsync()
    {
        var services = new ServiceCollection();
        string unique = NewId.NextGuid().ToString("N");
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/default-future-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        services.AddViciOneServiceBus<ITypedFutureBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetInMemorySagaRepositoryProvider();
            registration.AddFutureRequestConsumer<RequestConsumerFuture<CommandA, ResultA>, ConsumerA, CommandA, ResultA>();
            registration.AddFutureRequestConsumer<RequestConsumerFuture<CommandB, ResultB>, ConsumerB, CommandB, ResultB>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/typed-future-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBus primary = provider.GetRequiredService<IBus>();
        ITypedFutureBus typed = provider.GetRequiredService<ITypedFutureBus>();
        Assert.NotNull(primary.Address);
        Assert.NotNull(typed.Address);
        Assert.NotEqual(primary.Address, typed.Address);
        var first = provider.GetRequiredService<RequestConsumerFuture<CommandA, ResultA>>();
        var second = provider.GetRequiredService<RequestConsumerFuture<CommandB, ResultB>>();
        Assert.NotNull(first.CommandReceived);
        Assert.NotNull(second.CommandReceived);
        Assert.NotSame(first, second);
        Assert.Same(first, provider.GetRequiredService<RequestConsumerFuture<CommandA, ResultA>>());
        Assert.Same(second, provider.GetRequiredService<RequestConsumerFuture<CommandB, ResultB>>());
        var firstDefinition = provider.GetRequiredService<Bind<ITypedFutureBus,
            IFutureDefinition<RequestConsumerFuture<CommandA, ResultA>>>>().Value;
        var secondDefinition = provider.GetRequiredService<Bind<ITypedFutureBus,
            IFutureDefinition<RequestConsumerFuture<CommandB, ResultB>>>>().Value;
        var firstConsumer = provider.GetRequiredService<Bind<ITypedFutureBus, IConsumerDefinition<ConsumerA>>>().Value;
        var secondConsumer = provider.GetRequiredService<Bind<ITypedFutureBus, IConsumerDefinition<ConsumerB>>>().Value;
        Assert.Equal(typeof(RequestConsumerFuture<CommandA, ResultA>), firstDefinition.FutureType);
        Assert.Equal(typeof(RequestConsumerFuture<CommandB, ResultB>), secondDefinition.FutureType);
        Assert.Equal(Assert.IsAssignableFrom<IFutureRequestDefinition<CommandA>>(firstConsumer).RequestAddress,
            Assert.IsAssignableFrom<IFutureRequestDefinition<CommandA>>(firstDefinition).RequestAddress);
        Assert.Equal(Assert.IsAssignableFrom<IFutureRequestDefinition<CommandB>>(secondConsumer).RequestAddress,
            Assert.IsAssignableFrom<IFutureRequestDefinition<CommandB>>(secondDefinition).RequestAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "shared-response-contract-completes-only-matching-pending-selectors")]
    public async Task SharedResponseContract_CompletesOnlyTheMatchingPendingOperationAsync()
    {
        var machine = new SharedResponseFuture();
        Guid first = Guid.NewGuid(), second = Guid.NewGuid(), unknown = Guid.NewGuid();
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var commands = new OutgoingMessageRecorder();
        await FutureBehaviorContextFactory.UseAsync(machine, machine.CommandReceived, state,
            new SharedCommand(state.CorrelationId, first, second),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context), commands,
            TestContext.Current.CancellationToken, new Uri("loopback://localhost/shared-response"), Guid.NewGuid());
        Assert.Equal(2, commands.Messages.Count);
        Assert.Equal(2, state.Pending.Count);
        Assert.Equal(machine.FirstResponse, machine.SecondResponse);
        var outgoing = new OutgoingMessageRecorder();
        Task RaiseAsync(Guid firstId, Guid secondId, string value) => FutureBehaviorContextFactory.UseAsync(
            machine, machine.SecondResponse, state, new SharedResponse(firstId, secondId, value),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context), outgoing,
            TestContext.Current.CancellationToken, requestId: state.CorrelationId);

        await RaiseAsync(unknown, unknown, "unknown");
        Assert.Empty(state.Results);
        Assert.Equal(2, state.Pending.Count);
        await RaiseAsync(first, unknown, "first");
        Assert.Equal(first, Assert.Single(state.Results).Key);
        FutureMessage storedFirst = state.Results[first];
        Assert.Equal(second, Assert.Single(state.Pending));
        Assert.Equal(0, machine.FactoryCalls);
        Assert.Empty(outgoing.Messages);
        await RaiseAsync(first, unknown, "first replay");
        Assert.Same(storedFirst, state.Results[first]);
        Assert.Single(state.Results);
        Assert.Equal(second, Assert.Single(state.Pending));
        await RaiseAsync(unknown, second, "second");
        Assert.Empty(state.Pending);
        Assert.Equal(new[] { first, second, state.CorrelationId }.Order(), state.Results.Keys.Order());
        Assert.Null(state.Faulted);
        Assert.NotNull(state.Completed);
        Assert.Equal(1, machine.FactoryCalls);
        Assert.Single(outgoing.Messages);
        FutureMessage terminal = state.Results[state.CorrelationId];
        await RaiseAsync(first, second, "terminal replay");
        Assert.Same(storedFirst, state.Results[first]);
        Assert.Same(terminal, state.Results[state.CorrelationId]);
        Assert.Equal(1, machine.FactoryCalls);
        Assert.Single(outgoing.Messages);
        await FutureBehaviorContextFactory.UseAsync(machine, machine.SecondResponse, state,
            new SharedResponse(unknown, unknown, "inspection"), context =>
            {
                Assert.True(context.TryGetResult<SharedResponse>(first, out var firstResult));
                Assert.Equal("first", firstResult.Value);
                Assert.True(context.TryGetResult<SharedResponse>(second, out var secondResult));
                Assert.Equal("second", secondResult.Value);
                Assert.True(context.TryGetResult<Outcome>(state.CorrelationId, out var result));
                Assert.Equal("first,second", result.Value);
                return Task.CompletedTask;
            }, cancellationToken: TestContext.Current.CancellationToken);
    }

    private sealed class SharedResponseFuture : Future<SharedCommand, Outcome>
    {
        public IEvent<SharedResponse> FirstResponse { get; }
        public IEvent<SharedResponse> SecondResponse { get; }
        public int FactoryCalls { get; private set; }
        public SharedResponseFuture()
        {
            ConfigureCommand(configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            FirstResponse = SendRequest<FirstRequest>(configuration =>
            {
                configuration.SetRequestFactory(context => new FirstRequest(context.Message.First));
                configuration.TrackPendingRequest(request => request.Id);
            }).OnResponseReceived<SharedResponse>(configuration =>
                configuration.CompletePendingRequest(response => response.FirstId)).Completed;
            SecondResponse = SendRequest<SecondRequest>(configuration =>
            {
                configuration.SetRequestFactory(context => new SecondRequest(context.Message.Second));
                configuration.TrackPendingRequest(request => request.Id);
            }).OnResponseReceived<SharedResponse>(configuration =>
                configuration.CompletePendingRequest(response => response.SecondId)).Completed;
            WhenAllCompleted(configuration => configuration.SetResultFactory(context =>
            {
                FactoryCalls++;
                return new Outcome(string.Join(",", context.SelectResults<SharedResponse>()
                    .Select(response => response.Value).Order(StringComparer.Ordinal)));
            }));
        }
    }

    public interface ITypedFutureBus : IBus;
    public sealed record SharedCommand(Guid CorrelationId, Guid First, Guid Second) : ICorrelatedBy<Guid>;
    public sealed record FirstRequest(Guid Id);
    public sealed record SecondRequest(Guid Id);
    public sealed record SharedResponse(Guid FirstId, Guid SecondId, string Value);
    public sealed record Outcome(string Value);
}
