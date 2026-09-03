using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class EndpointConventionIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "full-short-interface-base-and-concrete-override")]
    public async Task ConventionMatrix_RoutesEachRuntimeContractToItsExactMappedEndpoint()
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

        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Handler<FullAddressMessage>(context => Complete(full, context));
            endpoint.Handler<ShortAddressMessage>(context => Complete(shortAddress, context));
            endpoint.Handler<InterfaceConventionMessage>(context => Complete(interfaceMessage, context));
            endpoint.Handler<DerivedConventionMessage>(context => Complete(baseMessage, context));
            endpoint.Handler<ConcreteOverrideMessage>(_ =>
            {
                Interlocked.Increment(ref overrideAtInput);
                return Task.CompletedTask;
            });
        };
        harness.OnConfigureInMemoryBus += bus =>
        {
            bus.Route<FullAddressMessage>(harness.InputQueueAddress);
            bus.Route<ShortAddressMessage>(new Uri($"queue:{harness.InputQueueName}"));
            bus.Route<ConventionContract>(harness.InputQueueAddress);
            bus.Route<ConventionBase>(harness.InputQueueAddress);
            bus.Route<OverrideContract>(harness.InputQueueAddress);
            bus.Route<ConcreteOverrideMessage>(new Uri(harness.BaseAddress, secondQueue));
            bus.ReceiveEndpoint(secondQueue, endpoint =>
                endpoint.Handler<ConcreteOverrideMessage>(context => Complete(overridden, context)));
        };

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var exactFull = new FullAddressMessage(NewId.NextGuid());
            var exactShort = new ShortAddressMessage(NewId.NextGuid());
            var exactInterface = new InterfaceConventionMessage(NewId.NextGuid());
            var exactBase = new DerivedConventionMessage(NewId.NextGuid());
            var exactOverride = new ConcreteOverrideMessage(NewId.NextGuid());

            await harness.Bus.Send(exactFull, cancellationToken);
            await harness.Bus.Send(exactShort, cancellationToken);
            await harness.Bus.Send(exactInterface, cancellationToken);
            await harness.Bus.Send(exactBase, cancellationToken);
            await harness.Bus.Send(exactOverride, cancellationToken);

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
            Assert.Equal(harness.InputQueueAddress, fullContext.ReceiveContext.InputAddress);
            Assert.Equal(harness.InputQueueAddress, shortContext.ReceiveContext.InputAddress);
            Assert.Equal(harness.InputQueueAddress, interfaceContext.ReceiveContext.InputAddress);
            Assert.Equal(harness.InputQueueAddress, baseContext.ReceiveContext.InputAddress);
            Assert.Equal(new Uri(harness.BaseAddress, secondQueue), overrideContext.ReceiveContext.InputAddress);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(0, Volatile.Read(ref overrideAtInput));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "same-contract-routes-are-isolated-per-bus")]
    public async Task TwoBuses_RouteTheSameContractWithoutCrossTalk()
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

        first.OnConfigureInMemoryBus += bus => bus.Route<BusOwnedRouteMessage>(first.InputQueueAddress);
        second.OnConfigureInMemoryBus += bus => bus.Route<BusOwnedRouteMessage>(second.InputQueueAddress);
        first.OnConfigureInMemoryReceiveEndpoint += endpoint =>
            endpoint.Handler<BusOwnedRouteMessage>(context => Complete(firstReceived, context));
        second.OnConfigureInMemoryReceiveEndpoint += endpoint =>
            endpoint.Handler<BusOwnedRouteMessage>(context => Complete(secondReceived, context));

        await first.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        await second.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var firstMessage = new BusOwnedRouteMessage("first");
            var secondMessage = new BusOwnedRouteMessage("second");

            await first.Bus.Send(firstMessage, cancellationToken);
            await second.Bus.Send(secondMessage, cancellationToken);

            ConsumeContext<BusOwnedRouteMessage> firstContext =
                await firstReceived.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<BusOwnedRouteMessage> secondContext =
                await secondReceived.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(firstMessage, firstContext.Message);
            Assert.Equal(secondMessage, secondContext.Message);
            Assert.Equal(first.InputQueueAddress, firstContext.ReceiveContext.InputAddress);
            Assert.Equal(second.InputQueueAddress, secondContext.ReceiveContext.InputAddress);
            Assert.NotEqual(firstContext.ReceiveContext.InputAddress, secondContext.ReceiveContext.InputAddress);
        }
        finally
        {
            await second.Stop().WaitAsync(timeout, CancellationToken.None);
            await first.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "ambiguous-inherited-routes-fail-deterministically")]
    public async Task TwoInheritedRoutes_RejectAnAmbiguousConcreteMessage()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-ambiguous-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryBus += bus =>
        {
            bus.Route<FirstRouteContract>(new Uri(harness.BaseAddress, "first"));
            bus.Route<SecondRouteContract>(new Uri(harness.BaseAddress, "second"));
        };

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
                harness.Bus.Send(new AmbiguousRouteMessage(), cancellationToken));

            Assert.Contains(nameof(AmbiguousRouteMessage), exception.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(FirstRouteContract), exception.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(SecondRouteContract), exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "fixed-route-duplicate-is-idempotent-and-conflict-fails")]
    public async Task FixedRouteDuplicate_IsIdempotentButAConflictFailsDuringConfiguration()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-duplicate-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        Uri destination = harness.InputQueueAddress;
        harness.OnConfigureInMemoryBus += bus =>
        {
            bus.Route<DuplicateRouteMessage>(destination);
            bus.Route<DuplicateRouteMessage>(destination);

            ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
                bus.Route<DuplicateRouteMessage>(new Uri(harness.BaseAddress, "other")));
            Assert.Contains(nameof(DuplicateRouteMessage), exception.Message, StringComparison.Ordinal);
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
            endpoint.Handler<DuplicateRouteMessage>(_ => Task.CompletedTask);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.Bus.Send(new DuplicateRouteMessage(), cancellationToken);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "dynamic-route-is-lazy-and-duplicates-fail")]
    public async Task DynamicRoute_IsLazyAndCannotBeRegisteredTwice()
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
        harness.OnConfigureInMemoryBus += bus =>
        {
            bus.Route<DynamicRouteMessage>((out Uri address) =>
            {
                Interlocked.Increment(ref providerCalls);
                address = harness.InputQueueAddress;
                return true;
            });

            Assert.Throws<ConfigurationException>(() =>
                bus.Route<DynamicRouteMessage>((out Uri address) =>
                {
                    address = harness.InputQueueAddress;
                    return true;
                }));
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
            endpoint.Handler<DynamicRouteMessage>(context => Complete(received, context));

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.Equal(0, Volatile.Read(ref providerCalls));

            await harness.Bus.Send(new DynamicRouteMessage(), cancellationToken);
            await received.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(1, Volatile.Read(ref providerCalls));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "routes-freeze-when-the-bus-is-built")]
    public async Task BuiltBus_RejectsLateRouteMutation()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"route-freeze-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        IInMemoryBusFactoryConfigurator? captured = null;
        harness.OnConfigureInMemoryBus += bus =>
        {
            captured = bus;
            bus.Route<FrozenRouteMessage>(harness.InputQueueAddress);
        };

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.NotNull(captured);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                captured.Route<LateRouteMessage>(harness.InputQueueAddress));
            Assert.Contains("immutable", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static Task Complete<T>(TaskCompletionSource<ConsumeContext<T>> signal, ConsumeContext<T> context)
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
}
