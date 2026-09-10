using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class EndpointConventionIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "missing-route-has-explicit-null-result")]
    public void EmptyRouteTable_ReturnsFalseAndNullAndRejectsANullContractType()
    {
        IMessageRouteTable routes = new MessageRouteTable();

        bool foundGeneric = routes.TryGetDestinationAddress<UnmappedRouteMessage>(out Uri? genericAddress);
        bool foundRuntime = routes.TryGetDestinationAddress(typeof(UnmappedRouteMessage), out Uri? runtimeAddress);

        Assert.False(foundGeneric);
        Assert.Null(genericAddress);
        Assert.False(foundRuntime);
        Assert.Null(runtimeAddress);
        Assert.Throws<ArgumentNullException>(() => routes.TryGetDestinationAddress(null!, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "missing-route-is-a-configuration-error")]
    public async Task ConventionSend_ReportsAMissingRouteAsAConfigurationErrorAsync()
    {
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            bus.SendAsync(new UnmappedRouteMessage(), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(UnmappedRouteMessage), exception.Message, StringComparison.Ordinal);
        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "full-short-interface-base-and-concrete-override")]
    public async Task ConventionMatrix_RoutesEachRuntimeContractToItsExactMappedEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string secondQueue = $"convention-override-{NewId.NextGuid():N}";
        using var harness = new InMemoryTestHarness($"endpoint-convention-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var full = NewSignal<ConsumeContext<FullAddressMessage>>();
        var shortAddress = NewSignal<ConsumeContext<ShortAddressMessage>>();
        var interfaceMessage = NewSignal<ConsumeContext<InterfaceConventionMessage>>();
        var baseMessage = NewSignal<ConsumeContext<DerivedConventionMessage>>();
        var overridden = NewSignal<ConsumeContext<ConcreteOverrideMessage>>();
        var overrideAtInput = 0;

        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Handler<FullAddressMessage>(context => CompleteAsync(full, context));
            endpoint.Handler<ShortAddressMessage>(context => CompleteAsync(shortAddress, context));
            endpoint.Handler<InterfaceConventionMessage>(context => CompleteAsync(interfaceMessage, context));
            endpoint.Handler<DerivedConventionMessage>(context => CompleteAsync(baseMessage, context));
            endpoint.Handler<ConcreteOverrideMessage>(_ =>
            {
                Interlocked.Increment(ref overrideAtInput);
                return Task.CompletedTask;
            });
        };
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.Route<FullAddressMessage>(harness.InputQueueAddress);
            bus.Route<ShortAddressMessage>(new Uri($"queue:{harness.InputQueueName}"));
            bus.Route<ConventionContract>(harness.InputQueueAddress);
            bus.Route<ConventionBase>(harness.InputQueueAddress);
            bus.Route<OverrideContract>(harness.InputQueueAddress);
            bus.Route<ConcreteOverrideMessage>(new Uri(harness.BaseAddress, secondQueue));
            bus.ReceiveEndpoint(secondQueue, endpoint =>
                endpoint.Handler<ConcreteOverrideMessage>(context => CompleteAsync(overridden, context)));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var exactFull = new FullAddressMessage(NewId.NextGuid());
            var exactShort = new ShortAddressMessage(NewId.NextGuid());
            var exactInterface = new InterfaceConventionMessage(NewId.NextGuid());
            var exactBase = new DerivedConventionMessage(NewId.NextGuid());
            var exactOverride = new ConcreteOverrideMessage(NewId.NextGuid());

            await harness.Bus.SendAsync(exactFull, cancellationToken);
            await harness.Bus.SendAsync(exactShort, cancellationToken);
            await harness.Bus.SendAsync(exactInterface, cancellationToken);
            await harness.Bus.SendAsync(exactBase, cancellationToken);
            await harness.Bus.SendAsync(exactOverride, cancellationToken);

            ConsumeContext<FullAddressMessage> fullContext = await full.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<ShortAddressMessage> shortContext = await shortAddress.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<InterfaceConventionMessage> interfaceContext = await interfaceMessage.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<DerivedConventionMessage> baseContext = await baseMessage.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<ConcreteOverrideMessage> overrideContext = await overridden.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(exactFull.CorrelationId, fullContext.Message.CorrelationId);
            Assert.Equal(exactShort.CorrelationId, shortContext.Message.CorrelationId);
            Assert.Equal(exactInterface.CorrelationId, interfaceContext.Message.CorrelationId);
            Assert.Equal(exactBase.CorrelationId, baseContext.Message.CorrelationId);
            Assert.Equal(exactOverride.CorrelationId, overrideContext.Message.CorrelationId);
            Assert.Equal(harness.InputQueueAddress, fullContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(harness.InputQueueAddress, shortContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(harness.InputQueueAddress, interfaceContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(harness.InputQueueAddress, baseContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(new Uri(harness.BaseAddress, secondQueue), overrideContext.Advanced().ReceiveContext.InputAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(0, Volatile.Read(ref overrideAtInput));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "same-contract-routes-are-isolated-per-bus")]
    public async Task TwoBuses_RouteTheSameContractWithoutCrossTalkAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var first = new InMemoryTestHarness($"route-first-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        using var second = new InMemoryTestHarness($"route-second-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var firstReceived = NewSignal<ConsumeContext<BusOwnedRouteMessage>>();
        var secondReceived = NewSignal<ConsumeContext<BusOwnedRouteMessage>>();

        first.InMemoryBusConfiguring += bus => bus.Route<BusOwnedRouteMessage>(first.InputQueueAddress);
        second.InMemoryBusConfiguring += bus => bus.Route<BusOwnedRouteMessage>(second.InputQueueAddress);
        first.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<BusOwnedRouteMessage>(context => CompleteAsync(firstReceived, context));
        second.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<BusOwnedRouteMessage>(context => CompleteAsync(secondReceived, context));

        await first.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        await second.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var firstMessage = new BusOwnedRouteMessage("first");
            var secondMessage = new BusOwnedRouteMessage("second");

            await first.Bus.SendAsync(firstMessage, cancellationToken);
            await second.Bus.SendAsync(secondMessage, cancellationToken);

            ConsumeContext<BusOwnedRouteMessage> firstContext =
                await firstReceived.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<BusOwnedRouteMessage> secondContext =
                await secondReceived.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(firstMessage, firstContext.Message);
            Assert.Equal(secondMessage, secondContext.Message);
            Assert.Equal(first.InputQueueAddress, firstContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(second.InputQueueAddress, secondContext.Advanced().ReceiveContext.InputAddress);
            Assert.NotEqual(firstContext.Advanced().ReceiveContext.InputAddress, secondContext.Advanced().ReceiveContext.InputAddress);
        }
        finally
        {
            await second.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            await first.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "ambiguous-inherited-routes-fail-deterministically")]
    public async Task TwoInheritedRoutes_RejectAnAmbiguousConcreteMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-ambiguous-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.Route<FirstRouteContract>(new Uri(harness.BaseAddress, "first"));
            bus.Route<SecondRouteContract>(new Uri(harness.BaseAddress, "second"));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
                harness.Bus.SendAsync(new AmbiguousRouteMessage(), cancellationToken));

            Assert.Contains(nameof(AmbiguousRouteMessage), exception.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(FirstRouteContract), exception.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(SecondRouteContract), exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "fixed-route-duplicate-is-idempotent-and-conflict-fails")]
    public async Task FixedRouteDuplicate_IsIdempotentButAConflictFailsDuringConfigurationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-duplicate-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        Uri destination = harness.InputQueueAddress;
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.Route<DuplicateRouteMessage>(destination);
            bus.Route<DuplicateRouteMessage>(destination);

            ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
                bus.Route<DuplicateRouteMessage>(new Uri(harness.BaseAddress, "other")));
            Assert.Contains(nameof(DuplicateRouteMessage), exception.Message, StringComparison.Ordinal);
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<DuplicateRouteMessage>(_ => Task.CompletedTask);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.Bus.SendAsync(new DuplicateRouteMessage(), cancellationToken);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "dynamic-route-is-lazy-and-duplicates-fail")]
    public async Task DynamicRoute_IsLazyAndCannotBeRegisteredTwiceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-dynamic-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var providerCalls = 0;
        var received = NewSignal<ConsumeContext<DynamicRouteMessage>>();
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.Route<DynamicRouteMessage>(() =>
            {
                Interlocked.Increment(ref providerCalls);
                return harness.InputQueueAddress;
            });

            Assert.Throws<ConfigurationException>(() =>
                bus.Route<DynamicRouteMessage>(() => harness.InputQueueAddress));
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<DynamicRouteMessage>(context => CompleteAsync(received, context));

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.Equal(0, Volatile.Read(ref providerCalls));

            await harness.Bus.SendAsync(new DynamicRouteMessage(), cancellationToken);
            await received.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(1, Volatile.Read(ref providerCalls));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "routes-freeze-when-the-bus-is-built")]
    public async Task BuiltBus_RejectsLateRouteMutationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-freeze-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        IInMemoryBusFactoryConfigurator? captured = null;
        harness.InMemoryBusConfiguring += bus =>
        {
            captured = bus;
            bus.Route<FrozenRouteMessage>(harness.InputQueueAddress);
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.NotNull(captured);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                captured.Route<LateRouteMessage>(harness.InputQueueAddress));
            Assert.Contains("immutable", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
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

    private sealed record FullAddressMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record ShortAddressMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private interface ConventionContract : CorrelatedBy<Guid>;

    private sealed record InterfaceConventionMessage(Guid CorrelationId) : ConventionContract;

    private abstract record ConventionBase(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record DerivedConventionMessage(Guid CorrelationId) : ConventionBase(CorrelationId);

    private interface OverrideContract : CorrelatedBy<Guid>;

    private sealed record ConcreteOverrideMessage(Guid CorrelationId) : OverrideContract;

    private sealed record BusOwnedRouteMessage(string Owner);

    private interface FirstRouteContract;

    private interface SecondRouteContract;

    private sealed record AmbiguousRouteMessage : FirstRouteContract, SecondRouteContract;

    private sealed record DuplicateRouteMessage;

    private sealed record DynamicRouteMessage;

    private sealed record FrozenRouteMessage;

    private sealed record LateRouteMessage;

    private sealed record UnmappedRouteMessage;
}
