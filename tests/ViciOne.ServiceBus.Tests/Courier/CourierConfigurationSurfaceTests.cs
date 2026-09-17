using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierConfigurationSurfaceTests
{
    private static readonly Uri CompensateAddress = new("loopback://localhost/compensate");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "all-host-overloads-build-one-matching-specification")]
    public void HostOverloads_BuildOneMatchingSpecificationAndInvokeOneCallbackEach()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder recorder);
        var callbacks = 0;
        var executeFactory = new FactoryMethodExecuteActivityFactory<TestActivity, Arguments>(_ => new TestActivity());
        var compensateFactory = new FactoryMethodCompensateActivityFactory<TestActivity, Log>(_ => new TestActivity());

        endpoint.ExecuteActivityHost<TestActivity, Arguments>(_ => callbacks++);
        endpoint.ExecuteActivityHost<TestActivity, Arguments>(CompensateAddress, _ => callbacks++);
        endpoint.ExecuteActivityHost<TestActivity, Arguments>(() => new TestActivity(), _ => callbacks++);
        endpoint.ExecuteActivityHost<TestActivity, Arguments>(CompensateAddress, () => new TestActivity(), _ => callbacks++);
        endpoint.ExecuteActivityHost<TestActivity, Arguments>(_ => new TestActivity(), _ => callbacks++);
        endpoint.ExecuteActivityHost<TestActivity, Arguments>(CompensateAddress, _ => new TestActivity(), _ => callbacks++);
        endpoint.ExecuteActivityHost(executeFactory, _ => callbacks++);
        endpoint.ExecuteActivityHost(CompensateAddress, executeFactory, _ => callbacks++);
        endpoint.CompensateActivityHost<TestActivity, Log>(_ => callbacks++);
        endpoint.CompensateActivityHost<TestActivity, Log>(() => new TestActivity(), _ => callbacks++);
        endpoint.CompensateActivityHost<TestActivity, Log>(_ => new TestActivity(), _ => callbacks++);
        endpoint.CompensateActivityHost(compensateFactory, _ => callbacks++);

        Assert.Equal(12, callbacks);
        Assert.Equal(12, recorder.Specifications.Count);
        Assert.Equal(8, recorder.Specifications.Count(specification =>
            specification is ExecuteActivityHostConfigurator<TestActivity, Arguments>));
        Assert.Equal(4, recorder.Specifications.Count(specification =>
            specification is CompensateActivityHostConfigurator<TestActivity, Log>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "host-overloads-validate-receiver-before-other-inputs")]
    public void HostOverloads_ValidateTheReceiverBeforeOtherInputs()
    {
        AssertParameter("configurator", () => CourierHostConfiguratorExtensions.ExecuteActivityHost<TestActivity, Arguments>(
            null!, (Func<TestActivity>)null!));
        AssertParameter("configurator", () => CourierHostConfiguratorExtensions.ExecuteActivityHost<TestActivity, Arguments>(
            null!, null!, (Func<TestActivity>)null!));
        AssertParameter("configurator", () => CourierHostConfiguratorExtensions.ExecuteActivityHost<TestActivity, Arguments>(
            null!, (Func<Arguments, TestActivity>)null!));
        AssertParameter("configurator", () => CourierHostConfiguratorExtensions.ExecuteActivityHost<TestActivity, Arguments>(
            null!, null!, (Func<Arguments, TestActivity>)null!));
        AssertParameter("configurator", () => CourierHostConfiguratorExtensions.CompensateActivityHost<TestActivity, Log>(
            null!, (Func<TestActivity>)null!));
        AssertParameter("configurator", () => CourierHostConfiguratorExtensions.CompensateActivityHost<TestActivity, Log>(
            null!, (Func<Log, TestActivity>)null!));

        IReceiveEndpointConfigurator endpoint = Endpoint(out _);
        AssertParameter("compensateAddress", () => endpoint.ExecuteActivityHost<TestActivity, Arguments>(
            null!, () => new TestActivity()));
        AssertParameter("activityFactory", () => endpoint.ExecuteActivityHost<TestActivity, Arguments>(
            (Func<TestActivity>)null!));
        AssertParameter("factory", () => endpoint.ExecuteActivityHost<TestActivity, Arguments>(
            (IExecuteActivityFactory<TestActivity, Arguments>)null!));
        AssertParameter("factory", () => endpoint.ExecuteActivityHost<TestActivity, Arguments>(
            CompensateAddress, (IExecuteActivityFactory<TestActivity, Arguments>)null!));
        AssertParameter("activityFactory", () => endpoint.CompensateActivityHost<TestActivity, Log>(
            (Func<TestActivity>)null!));
        AssertParameter("factory", () => endpoint.CompensateActivityHost<TestActivity, Log>(
            (ICompensateActivityFactory<TestActivity, Log>)null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "dependency-injection-host-overloads-validate-and-forward")]
    public void DependencyInjectionHostOverloads_ValidateAndForwardOneScopedSpecification()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder recorder);
        IRegistrationContext context = DispatchProxy.Create<TestRegistrationContext, PassiveProxy>();
        var callbacks = 0;

        endpoint.ExecuteActivityHost<TestActivity, Arguments>(CompensateAddress, context, _ => callbacks++);
        endpoint.ExecuteActivityHost<TestActivity, Arguments>(context, _ => callbacks++);
        endpoint.CompensateActivityHost<TestActivity, Log>(context, _ => callbacks++);

        Assert.Equal(3, callbacks);
        Assert.Equal(3, recorder.Specifications.Count);
        Assert.Equal(2, recorder.Specifications.Count(specification =>
            specification is ExecuteActivityHostConfigurator<TestActivity, Arguments>));
        Assert.Single(recorder.Specifications, specification =>
            specification is CompensateActivityHostConfigurator<TestActivity, Log>);
        AssertParameter("configurator", () => DependencyInjectionCourierReceiveEndpointExtensions.ExecuteActivityHost<TestActivity, Arguments>(
            null!, CompensateAddress, context));
        AssertParameter("compensateAddress", () => endpoint.ExecuteActivityHost<TestActivity, Arguments>(null!, context));
        AssertParameter("context", () => DependencyInjectionCourierReceiveEndpointExtensions.ExecuteActivityHost<TestActivity, Arguments>(
            endpoint, CompensateAddress, null!));
        AssertParameter("context", () => endpoint.ExecuteActivityHost<TestActivity, Arguments>((IRegistrationContext)null!));
        AssertParameter("context", () => DependencyInjectionCourierReceiveEndpointExtensions.CompensateActivityHost<TestActivity, Log>(
            endpoint, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "activity-and-routing-slip-pipe-adapters-validate-and-forward")]
    public void PipeAdapters_ValidateAndForwardExactlyOneSpecification()
    {
        IPipeConfigurator<ExecuteActivityContext<TestActivity, Arguments>> execute = PipeConfigurator<ExecuteActivityContext<TestActivity, Arguments>>(
            out PipeRecorder<ExecuteActivityContext<TestActivity, Arguments>> executeRecorder);
        IPipeConfigurator<CompensateActivityContext<TestActivity, Log>> compensate = PipeConfigurator<CompensateActivityContext<TestActivity, Log>>(
            out PipeRecorder<CompensateActivityContext<TestActivity, Log>> compensateRecorder);
        var executeSpecification = new EmptySpecification<ExecuteActivityContext<Arguments>>();
        var compensateSpecification = new EmptySpecification<CompensateActivityContext<Log>>();

        ActivityPipeConfiguratorExtensions.AddPipeSpecification(execute, executeSpecification);
        ActivityPipeConfiguratorExtensions.AddPipeSpecification(compensate, compensateSpecification);

        Assert.Single(executeRecorder.Specifications);
        Assert.Single(compensateRecorder.Specifications);
        AssertParameter("configurator", () => ActivityPipeConfiguratorExtensions.AddPipeSpecification(
            (IPipeConfigurator<ExecuteActivityContext<TestActivity, Arguments>>)null!, executeSpecification));
        AssertParameter("specification", () => ActivityPipeConfiguratorExtensions.AddPipeSpecification(execute,
            (IPipeSpecification<ExecuteActivityContext<Arguments>>)null!));
        AssertParameter("configurator", () => ActivityPipeConfiguratorExtensions.AddPipeSpecification(
            (IPipeConfigurator<CompensateActivityContext<TestActivity, Log>>)null!, compensateSpecification));
        AssertParameter("specification", () => ActivityPipeConfiguratorExtensions.AddPipeSpecification(compensate,
            (IPipeSpecification<CompensateActivityContext<Log>>)null!));

        var routingSlip = new RoutingSlipConfigurator();
        var routingSpecification = new EmptySpecification<ConsumeContext<IRoutingSlip>>();
        AssertParameter("specification", () => routingSlip.AddPipeSpecification(null!));
        routingSlip.AddPipeSpecification(routingSpecification);

        Assert.Empty(routingSlip.Validate());
        Assert.NotNull(routingSlip.Build());
        Assert.Equal(1, routingSpecification.ApplyCalls);
    }

    private static IReceiveEndpointConfigurator Endpoint(out EndpointRecorder recorder)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointRecorder>();
        recorder = (EndpointRecorder)(object)endpoint;
        return endpoint;
    }

    private static IPipeConfigurator<TContext> PipeConfigurator<TContext>(out PipeRecorder<TContext> recorder)
        where TContext : class, PipeContext
    {
        IPipeConfigurator<TContext> configurator = DispatchProxy.Create<IPipeConfigurator<TContext>, PipeRecorder<TContext>>();
        recorder = (PipeRecorder<TContext>)(object)configurator;
        return configurator;
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private sealed record Arguments(string Value);

    private sealed record Log(string Value);

    private interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;

    private sealed class TestActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    private sealed class EmptySpecification<TContext> : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public int ApplyCalls { get; private set; }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ApplyCalls++;
        }

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private class EndpointRecorder : DispatchProxy
    {
        public List<IReceiveEndpointSpecification> Specifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IReceiveEndpointConfigurator.AddEndpointSpecification))
            {
                Specifications.Add((IReceiveEndpointSpecification)args![0]!);
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PipeRecorder<TContext> : DispatchProxy
        where TContext : class, PipeContext
    {
        public List<IPipeSpecification<TContext>> Specifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IPipeConfigurator<TContext>.AddPipeSpecification))
            {
                Specifications.Add((IPipeSpecification<TContext>)args![0]!);
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
