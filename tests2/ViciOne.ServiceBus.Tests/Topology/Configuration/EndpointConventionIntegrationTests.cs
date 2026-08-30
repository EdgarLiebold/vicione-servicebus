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

        EndpointConvention.Map<FullAddressMessage>(harness.InputQueueAddress);
        EndpointConvention.Map<ShortAddressMessage>(new Uri($"queue:{harness.InputQueueName}"));
        EndpointConvention.Map<ConventionContract>(harness.InputQueueAddress);
        EndpointConvention.Map<ConventionBase>(harness.InputQueueAddress);
        EndpointConvention.Map<OverrideContract>(harness.InputQueueAddress);
        EndpointConvention.Map<ConcreteOverrideMessage>(new Uri(harness.BaseAddress, secondQueue));

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
        harness.OnConfigureInMemoryBus += bus => bus.ReceiveEndpoint(secondQueue, endpoint =>
            endpoint.Handler<ConcreteOverrideMessage>(context => Complete(overridden, context)));

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
}
