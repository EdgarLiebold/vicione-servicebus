using System.Reflection;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class MiddlewareConfigurationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-CONFIGURATION", "delegate-required-inputs")]
    public void DelegateConfiguration_RejectsMissingPipelinesAndCallbacks()
    {
        IPipeConfigurator<TestContext>? missingConfigurator = null;
        var configurator = new PipeConfiguratorStub();

        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() => missingConfigurator!.UseExecute(_ => { })).ParamName);
        Assert.Equal(
            "callback",
            Assert.Throws<ArgumentNullException>(() => configurator.UseExecute((Action<TestContext>)null!)).ParamName);
        Assert.Equal(
            "callback",
            Assert.Throws<ArgumentNullException>(() => configurator.UseExecuteAwaited(null!)).ParamName);
        Assert.Empty(configurator.Specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-CONFIGURATION", "filter-required-inputs")]
    public void FilterConfiguration_RejectsEveryMissingRequiredInput()
    {
        IPipeConfigurator<TestContext>? missingConfigurator = null;
        var configurator = new PipeConfiguratorStub();
        var filter = new FilterStub();
        MergeFilterContextProvider<TestContext, TestContext> merge = static (input, _) => input;
        FilterContextProvider<TestContext, TestContext> select = static context => context;

        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() => missingConfigurator!.UseFilter(filter)).ParamName);
        Assert.Equal(
            "filter",
            Assert.Throws<ArgumentNullException>(() => configurator.UseFilter((IFilter<TestContext>)null!)).ParamName);
        Assert.Equal(
            "contextProvider",
            Assert.Throws<ArgumentNullException>(() => configurator.UseFilter(filter, null!, select)).ParamName);
        Assert.Equal(
            "inputContextProvider",
            Assert.Throws<ArgumentNullException>(() => configurator.UseFilter(filter, merge, null!)).ParamName);
        Assert.Empty(configurator.Specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-CONFIGURATION", "filter-collection-atomic-admission")]
    public void FilterCollections_AreValidatedCompletelyBeforeThePipelineChanges()
    {
        var enumerableConfigurator = new PipeConfiguratorStub();
        var parameterConfigurator = new PipeConfiguratorStub();
        var filter = new FilterStub();

        Assert.Equal(
            "filters",
            Assert.Throws<ArgumentNullException>(() =>
                enumerableConfigurator.UseFilters((IEnumerable<IFilter<TestContext>>)[filter, null!])).ParamName);
        Assert.Empty(enumerableConfigurator.Specifications);

        Assert.Equal(
            "filters",
            Assert.Throws<ArgumentNullException>(() => parameterConfigurator.UseFilters(filter, null!)).ParamName);
        Assert.Empty(parameterConfigurator.Specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "required-endpoints")]
    public void EndpointDependencyLink_RejectsMissingEndpointsBeforeConnectingObservers()
    {
        IReceiveEndpointConfigurator? missing = null;
        IReceiveEndpointConfigurator endpoint = CreateEndpoint(out EndpointConfiguratorProxy proxy);

        Assert.Equal(
            "connector",
            Assert.Throws<ArgumentNullException>(() => missing!.AddDependency(endpoint)).ParamName);
        Assert.Equal(
            "dependency",
            Assert.Throws<ArgumentNullException>(() => endpoint.AddDependency((IReceiveEndpointConfigurator)null!)).ParamName);
        Assert.Null(proxy.Observer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "readiness-failure-propagation")]
    public async Task DependencyReadiness_IgnoresRecoverableFaultsAndPropagatesTerminalFaultsAsync()
    {
        IReceiveEndpointConfigurator connector = CreateEndpoint(out EndpointConfiguratorProxy connectorProxy);
        IReceiveEndpointConfigurator dependency = CreateEndpoint(out EndpointConfiguratorProxy dependencyProxy);
        connector.AddDependency(dependency);
        var recoverable = new FaultedEvent(new InvalidOperationException("recoverable"), isTerminal: false);

        await dependencyProxy.Observer!.FaultedAsync(recoverable);

        Assert.False(connectorProxy.Dependency!.Ready.IsCompleted);

        var terminalException = new InvalidOperationException("terminal");
        await dependencyProxy.Observer.FaultedAsync(new FaultedEvent(terminalException, isTerminal: true));

        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connectorProxy.Dependency.Ready);
        Assert.Same(terminalException, observed);
        Assert.True(dependencyProxy.Handle.IsDisconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "completion-failure-propagation")]
    public async Task DependentCompletion_PropagatesTerminalFaultsAsync()
    {
        IReceiveEndpointConfigurator connector = CreateEndpoint(out EndpointConfiguratorProxy connectorProxy);
        IReceiveEndpointConfigurator dependency = CreateEndpoint(out EndpointConfiguratorProxy dependencyProxy);
        connector.AddDependency(dependency);
        var terminalException = new InvalidOperationException("terminal");

        await connectorProxy.Observer!.FaultedAsync(new FaultedEvent(terminalException, isTerminal: true));

        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dependencyProxy.Dependent!.Completed);
        Assert.Same(terminalException, observed);
        Assert.True(connectorProxy.Handle.IsDisconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "readiness-success-and-early-completion")]
    public async Task DependencyReadiness_CompletesOnReadyAndFailsWhenTheEndpointCompletesFirstAsync()
    {
        IReceiveEndpointConfigurator readyConnector = CreateEndpoint(out EndpointConfiguratorProxy readyConnectorProxy);
        IReceiveEndpointConfigurator readyDependency = CreateEndpoint(out EndpointConfiguratorProxy readyDependencyProxy);
        readyConnector.AddDependency(readyDependency);
        var readyEvent = new ReadyEvent();

        await readyDependencyProxy.Observer!.ReadyAsync(readyEvent);
        await readyConnectorProxy.Dependency!.Ready;

        Assert.True(readyDependencyProxy.Handle.IsDisconnected);

        IReceiveEndpointConfigurator stoppedConnector = CreateEndpoint(out EndpointConfiguratorProxy stoppedConnectorProxy);
        IReceiveEndpointConfigurator stoppedDependency = CreateEndpoint(out EndpointConfiguratorProxy stoppedDependencyProxy);
        stoppedConnector.AddDependency(stoppedDependency);

        await stoppedDependencyProxy.Observer!.CompletedAsync(new CompletedEvent());

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stoppedConnectorProxy.Dependency!.Ready);
        Assert.True(stoppedDependencyProxy.Handle.IsDisconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "completion-success-and-recoverable-fault")]
    public async Task DependentCompletion_IgnoresRecoverableFaultsAndCompletesWithTheEndpointAsync()
    {
        IReceiveEndpointConfigurator connector = CreateEndpoint(out EndpointConfiguratorProxy connectorProxy);
        IReceiveEndpointConfigurator dependency = CreateEndpoint(out EndpointConfiguratorProxy dependencyProxy);
        connector.AddDependency(dependency);

        await connectorProxy.Observer!.FaultedAsync(new FaultedEvent(new InvalidOperationException("recoverable"), isTerminal: false));
        Assert.False(dependencyProxy.Dependent!.Completed.IsCompleted);

        await connectorProxy.Observer.CompletedAsync(new CompletedEvent());
        await dependencyProxy.Dependent.Completed;

        Assert.True(connectorProxy.Handle.IsDisconnected);
    }

    private static IReceiveEndpointConfigurator CreateEndpoint(out EndpointConfiguratorProxy proxy)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointConfiguratorProxy>();
        proxy = (EndpointConfiguratorProxy)(object)endpoint;
        return endpoint;
    }

    private sealed class TestContext : BasePipeContext;

    private sealed class PipeConfiguratorStub : IPipeConfigurator<TestContext>
    {
        public List<IPipeSpecification<TestContext>> Specifications { get; } = [];

        public void AddPipeSpecification(IPipeSpecification<TestContext> specification) => Specifications.Add(specification);
    }

    private sealed class FilterStub : IFilter<TestContext>
    {
        public Task SendAsync(TestContext context, IPipe<TestContext> next) => next.SendAsync(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FaultedEvent(Exception exception, bool isTerminal) : ReceiveEndpointFaulted
    {
        public Exception Exception { get; } = exception;

        public bool IsTerminal { get; } = isTerminal;

        public Uri InputAddress { get; } = new("loopback://dependency");

        public IReceiveEndpoint ReceiveEndpoint { get; } =
            DispatchProxy.Create<IReceiveEndpoint, RejectInvocationProxy>();
    }

    private sealed class ReadyEvent : ReceiveEndpointReady
    {
        public bool IsStarted => true;

        public Uri InputAddress { get; } = new("loopback://dependency");

        public IReceiveEndpoint ReceiveEndpoint { get; } =
            DispatchProxy.Create<IReceiveEndpoint, RejectInvocationProxy>();
    }

    private sealed class CompletedEvent : ReceiveEndpointCompleted
    {
        public long DeliveryCount => 0;

        public int MaxConcurrentDeliveryCount => 0;

        public Uri InputAddress { get; } = new("loopback://dependency");

        public IReceiveEndpoint ReceiveEndpoint { get; } =
            DispatchProxy.Create<IReceiveEndpoint, RejectInvocationProxy>();
    }

    private class EndpointConfiguratorProxy : DispatchProxy
    {
        public IReceiveEndpointDependency? Dependency { get; private set; }

        public IReceiveEndpointDependent? Dependent { get; private set; }

        public IReceiveEndpointObserver? Observer { get; private set; }

        public TrackingConnectHandle Handle { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case nameof(IReceiveEndpointDependencyConnector.AddDependency):
                    Dependency = (IReceiveEndpointDependency)args![0]!;
                    return null;
                case nameof(IReceiveEndpointDependentConnector.AddDependent):
                    Dependent = (IReceiveEndpointDependent)args![0]!;
                    return null;
                case nameof(IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver):
                    Observer = (IReceiveEndpointObserver)args![0]!;
                    return Handle;
                default:
                    throw new NotSupportedException($"Unexpected endpoint configurator invocation: {targetMethod?.Name}");
            }
        }
    }

    private sealed class TrackingConnectHandle : ConnectHandle
    {
        public bool IsDisconnected { get; private set; }

        public void Disconnect() => IsDisconnected = true;

        public void Dispose() => Disconnect();
    }

    private class RejectInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected proxy invocation: {targetMethod?.Name}");
    }
}
