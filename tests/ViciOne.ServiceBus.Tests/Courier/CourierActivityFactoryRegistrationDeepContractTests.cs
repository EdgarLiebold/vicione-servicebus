using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityFactoryRegistrationDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-factory-api-covariance-async-token-shape")]
    public void ActivityFactoryContracts_ExposeCovariantActivitiesAndCancelableAsyncOperations()
    {
        Type executeContract = typeof(IExecuteActivityFactory<,>);
        Type compensateContract = typeof(ICompensateActivityFactory<,>);
        Type pairedContract = typeof(IActivityFactory<,,>);

        Assert.Equal(
            GenericParameterAttributes.Covariant,
            executeContract.GetGenericArguments()[0].GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        Assert.Equal(
            GenericParameterAttributes.Covariant,
            compensateContract.GetGenericArguments()[0].GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        Assert.Equal(
            GenericParameterAttributes.Covariant,
            pairedContract.GetGenericArguments()[0].GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        Assert.Contains(pairedContract.GetInterfaces(), type => type.IsGenericType
            && type.GetGenericTypeDefinition() == executeContract);
        Assert.Contains(pairedContract.GetInterfaces(), type => type.IsGenericType
            && type.GetGenericTypeDefinition() == compensateContract);

        AssertAsyncFactoryMethod(executeContract, nameof(IExecuteActivityFactory<TestActivity, TestArguments>.ExecuteAsync));
        AssertAsyncFactoryMethod(compensateContract, nameof(ICompensateActivityFactory<TestActivity, TestLog>.CompensateAsync));

        Assert.True(typeof(FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>).IsSealed);
        Assert.True(typeof(FactoryMethodCompensateActivityFactory<TestActivity, TestLog>).IsSealed);
        Assert.True(typeof(FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>).IsSealed);
        Assert.True(typeof(DefaultConstructorExecuteActivityFactory<TestActivity, TestArguments>).IsAbstract);
        Assert.True(typeof(DefaultConstructorExecuteActivityFactory<TestActivity, TestArguments>).IsSealed);
        Assert.True(typeof(DefaultConstructorCompensateActivityFactory<TestActivity, TestLog>).IsAbstract);
        Assert.True(typeof(DefaultConstructorCompensateActivityFactory<TestActivity, TestLog>).IsSealed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-paired-factory-exact-routing-and-owned-lifetimes")]
    public async Task PairedFactory_RoutesExactInputsAndOwnsEachCreatedActivityAsync()
    {
        var arguments = new TestArguments("execute");
        var log = new TestLog("compensate");
        var executeActivity = new TestActivity();
        var compensateActivity = new TestActivity();
        var observations = new List<string>();
        var factory = new FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>(
            value =>
            {
                Assert.Same(arguments, value);
                observations.Add("execute-created");
                return executeActivity;
            },
            value =>
            {
                Assert.Same(log, value);
                observations.Add("compensate-created");
                return compensateActivity;
            });

        await factory.ExecuteAsync(
            CreateExecuteContext(arguments),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(context =>
            {
                Assert.Same(executeActivity, context.Activity);
                Assert.Same(arguments, context.Arguments);
                Assert.Equal(0, executeActivity.DisposeCount);
                observations.Add("execute-pipeline");
                return Task.CompletedTask;
            }),
            TestContext.Current.CancellationToken);
        await factory.CompensateAsync(
            CreateCompensateContext(log),
            new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(context =>
            {
                Assert.Same(compensateActivity, context.Activity);
                Assert.Same(log, context.Log);
                Assert.Equal(0, compensateActivity.DisposeCount);
                observations.Add("compensate-pipeline");
                return Task.CompletedTask;
            }),
            TestContext.Current.CancellationToken);

        Assert.NotSame(executeActivity, compensateActivity);
        Assert.Equal(1, executeActivity.DisposeCount);
        Assert.Equal(1, compensateActivity.DisposeCount);
        Assert.Equal(
            ["execute-created", "execute-pipeline", "compensate-created", "compensate-pipeline"],
            observations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-creation-failure-identity-before-pipeline")]
    public async Task CreationDelegateFailures_PreserveIdentityAndSkipThePipelineAsync()
    {
        var executeFailure = new ExpectedFactoryException("execute creation");
        var compensateFailure = new ExpectedFactoryException("compensate creation");
        var pipelineCalls = 0;
        var factory = new FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>(
            _ => throw executeFailure,
            _ => throw compensateFailure);
        var executePipe = new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ =>
        {
            pipelineCalls++;
            return Task.CompletedTask;
        });
        var compensatePipe = new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(_ =>
        {
            pipelineCalls++;
            return Task.CompletedTask;
        });

        ExpectedFactoryException actualExecute = await Assert.ThrowsAsync<ExpectedFactoryException>(() => factory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("value")),
            executePipe,
            TestContext.Current.CancellationToken));
        ExpectedFactoryException actualCompensate = await Assert.ThrowsAsync<ExpectedFactoryException>(() => factory.CompensateAsync(
            CreateCompensateContext(new TestLog("value")),
            compensatePipe,
            TestContext.Current.CancellationToken));

        Assert.Same(executeFailure, actualExecute);
        Assert.Same(compensateFailure, actualCompensate);
        Assert.Equal(0, pipelineCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-null-operation-boundaries")]
    public async Task PairedFactory_RejectsMissingContextsAndPipelinesAsync()
    {
        var factory = new FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>(
            _ => new TestActivity(),
            _ => new TestActivity());
        ExecuteContext<TestArguments> executeContext = CreateExecuteContext(new TestArguments("execute"));
        CompensateContext<TestLog> compensateContext = CreateCompensateContext(new TestLog("compensate"));
        var executePipe = new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ => Task.CompletedTask);
        var compensatePipe = new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(_ => Task.CompletedTask);

        await AssertParameterAsync("context", () => factory.ExecuteAsync(null!, executePipe));
        await AssertParameterAsync("next", () => factory.ExecuteAsync(executeContext, null!));
        await AssertParameterAsync("context", () => factory.CompensateAsync(null!, compensatePipe));
        await AssertParameterAsync("next", () => factory.CompensateAsync(compensateContext, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-pipeline-release-dual-failure-order")]
    public async Task ActivityFactories_PreservePipelineAndReleaseFailuresInOperationOrderAsync()
    {
        var executeOperationFailure = new ExpectedFactoryException("execute pipeline");
        var executeReleaseFailure = new ExpectedReleaseException("execute release");
        var executeActivity = new AsyncReleaseFaultingActivity(executeReleaseFailure);
        var executeFactory = new FactoryMethodExecuteActivityFactory<AsyncReleaseFaultingActivity, TestArguments>(_ => executeActivity);

        AggregateException executeFailure = await Assert.ThrowsAsync<AggregateException>(() => executeFactory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("execute")),
            new DelegatePipe<ExecuteActivityContext<AsyncReleaseFaultingActivity, TestArguments>>(
                _ => Task.FromException(executeOperationFailure)),
            TestContext.Current.CancellationToken));

        Assert.StartsWith("Activity pipeline and release encountered multiple failures.", executeFailure.Message, StringComparison.Ordinal);
        Assert.Collection(
            executeFailure.InnerExceptions,
            failure => Assert.Same(executeOperationFailure, failure),
            failure => Assert.Same(executeReleaseFailure, failure));
        Assert.Equal(1, executeActivity.DisposeAsyncCount);

        var compensateOperationFailure = new ExpectedFactoryException("compensate pipeline");
        var compensateReleaseFailure = new ExpectedReleaseException("compensate release");
        var compensateActivity = new SyncReleaseFaultingActivity(compensateReleaseFailure);
        var compensateFactory = new FactoryMethodCompensateActivityFactory<SyncReleaseFaultingActivity, TestLog>(_ => compensateActivity);

        AggregateException compensateFailure = await Assert.ThrowsAsync<AggregateException>(() => compensateFactory.CompensateAsync(
            CreateCompensateContext(new TestLog("compensate")),
            new DelegatePipe<CompensateActivityContext<SyncReleaseFaultingActivity, TestLog>>(
                _ => Task.FromException(compensateOperationFailure)),
            TestContext.Current.CancellationToken));

        Assert.Collection(
            compensateFailure.InnerExceptions,
            failure => Assert.Same(compensateOperationFailure, failure),
            failure => Assert.Same(compensateReleaseFailure, failure));
        Assert.Equal(1, compensateActivity.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-release-only-failure-identity")]
    public async Task ActivityFactories_PreserveExactReleaseFailureAfterSuccessfulPipelinesAsync()
    {
        var executeReleaseFailure = new ExpectedReleaseException("execute release");
        var executeActivity = new AsyncReleaseFaultingActivity(executeReleaseFailure);
        var executeFactory = new FactoryMethodExecuteActivityFactory<AsyncReleaseFaultingActivity, TestArguments>(_ => executeActivity);

        ExpectedReleaseException actualExecute = await Assert.ThrowsAsync<ExpectedReleaseException>(() => executeFactory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("execute")),
            new DelegatePipe<ExecuteActivityContext<AsyncReleaseFaultingActivity, TestArguments>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken));

        var compensateReleaseFailure = new ExpectedReleaseException("compensate release");
        var compensateActivity = new SyncReleaseFaultingActivity(compensateReleaseFailure);
        var compensateFactory = new FactoryMethodCompensateActivityFactory<SyncReleaseFaultingActivity, TestLog>(_ => compensateActivity);

        ExpectedReleaseException actualCompensate = await Assert.ThrowsAsync<ExpectedReleaseException>(() => compensateFactory.CompensateAsync(
            CreateCompensateContext(new TestLog("compensate")),
            new DelegatePipe<CompensateActivityContext<SyncReleaseFaultingActivity, TestLog>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken));

        Assert.Same(executeReleaseFailure, actualExecute);
        Assert.Equal(1, executeActivity.DisposeAsyncCount);
        Assert.Same(compensateReleaseFailure, actualCompensate);
        Assert.Equal(1, compensateActivity.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-downstream-cancellation-releases-created-instance")]
    public async Task DownstreamCancellation_PreservesTheTokenAndReleasesTheCreatedActivityAsync()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var activity = new TestActivity();
        var factory = new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(_ => activity);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("execute")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(
                _ => Task.FromCanceled(cancellation.Token)),
            TestContext.Current.CancellationToken));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(1, activity.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-factory-probe-scope-and-null-boundary")]
    public void ActivityFactories_ProbeTheStableFactoryMethodScopeAndRejectNull()
    {
        IProbeSite[] factories =
        [
            new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(_ => new TestActivity()),
            new FactoryMethodCompensateActivityFactory<TestActivity, TestLog>(_ => new TestActivity()),
            new FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>(
                _ => new TestActivity(),
                _ => new TestActivity()),
        ];
        var probe = new RecordingProbeContext();

        foreach (IProbeSite factory in factories)
        {
            AssertParameter("context", () => factory.Probe(null!));
            factory.Probe(probe);
        }

        Assert.Equal(["factoryMethod", "factoryMethod", "factoryMethod"], probe.ScopeKeys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-consumer-kind-null-boundary-matrix")]
    public void ConsumerKinds_RejectEveryMissingRegistrationCollaborator()
    {
        var activity = new ActivityConsumerKind();
        var executeOnly = new ExecuteActivityConsumerKind();
        IReceiveEndpointConfigurator endpoint = Proxy<IReceiveEndpointConfigurator>();
        IRegistrationContext registration = Proxy<IRegistrationContext>();
        Uri companionAddress = new("loopback://localhost/compensate");

        AssertParameter("context", () => activity.GetRegistrations(null!).ToArray());
        AssertParameter("context", () => executeOnly.GetRegistrations(null!).ToArray());
        AssertParameter("context", () => activity.ConfigureTestHarness(null!));
        AssertParameter("context", () => executeOnly.ConfigureTestHarness(null!));

        AssertParameter("registrationType", () => activity.TryConfigurePair(null!, endpoint, endpoint, registration));
        AssertParameter("primaryEndpointConfigurator", () => activity.TryConfigurePair(typeof(TestActivity), null!, endpoint, registration));
        AssertParameter("companionEndpointConfigurator", () => activity.TryConfigurePair(typeof(TestActivity), endpoint, null!, registration));
        AssertParameter("registrationContext", () => activity.TryConfigurePair(typeof(TestActivity), endpoint, endpoint, null!));

        AssertParameter("registrationType", () => activity.TryConfigurePrimary(null!, endpoint, companionAddress, registration));
        AssertParameter("primaryEndpointConfigurator", () => activity.TryConfigurePrimary(typeof(TestActivity), null!, companionAddress, registration));
        AssertParameter("companionAddress", () => activity.TryConfigurePrimary(typeof(TestActivity), endpoint, null!, registration));
        AssertParameter("registrationContext", () => activity.TryConfigurePrimary(typeof(TestActivity), endpoint, companionAddress, null!));

        AssertParameter("registrationType", () => activity.TryConfigureCompanion(null!, endpoint, registration));
        AssertParameter("companionEndpointConfigurator", () => activity.TryConfigureCompanion(typeof(TestActivity), null!, registration));
        AssertParameter("registrationContext", () => activity.TryConfigureCompanion(typeof(TestActivity), endpoint, null!));

        AssertParameter("registrationType", () => executeOnly.TryConfigure(null!, endpoint, registration));
        AssertParameter("endpointConfigurator", () => executeOnly.TryConfigure(typeof(ExecuteOnlyActivity), null!, registration));
        AssertParameter("registrationContext", () => executeOnly.TryConfigure(typeof(ExecuteOnlyActivity), endpoint, null!));
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(IOpenBus<>))]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-invalid-bus-type-preflight")]
    public void CourierServiceRegistration_RejectsAnInvalidBusTypeBeforeServiceEffects(Type busType)
    {
        var services = new ServiceCollection();
        int baseline = services.Count;

        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
            CourierServiceRegistration.Register(services, busType));

        Assert.Equal("busType", failure.ParamName);
        Assert.Contains(nameof(IBus), failure.Message, StringComparison.Ordinal);
        Assert.Equal(baseline, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-scoped-executor-resolution-and-wiring")]
    public void CourierServiceRegistration_ResolvesScopedExecutorsFromEachExactBusContext()
    {
        var defaultContext = new RecordingScopedBusContext();
        var secondaryContext = new RecordingScopedBusContext();
        var clock = new RecordingTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IScopedBusContextProvider<IBus>>(new StaticScopedBusContextProvider<IBus>(defaultContext));
        services.AddSingleton<IScopedBusContextProvider<ISecondaryBus>>(
            new StaticScopedBusContextProvider<ISecondaryBus>(secondaryContext));
        services.AddSingleton<TimeProvider>(clock);
        CourierServiceRegistration.Register(services, typeof(IBus));
        CourierServiceRegistration.Register(services, typeof(ISecondaryBus));
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        IRoutingSlipExecutor firstDefault;
        IRoutingSlipExecutor firstNamed;
        using (IServiceScope scope = provider.CreateScope())
        {
            firstDefault = scope.ServiceProvider.GetRequiredService<IRoutingSlipExecutor>();
            firstNamed = scope.ServiceProvider
                .GetRequiredService<Bind<ISecondaryBus, IRoutingSlipExecutor>>()
                .Value;

            Assert.Same(firstDefault, scope.ServiceProvider.GetRequiredService<IRoutingSlipExecutor>());
            Assert.Same(firstNamed, scope.ServiceProvider
                .GetRequiredService<Bind<ISecondaryBus, IRoutingSlipExecutor>>()
                .Value);
            Assert.NotSame(firstDefault, firstNamed);
            AssertExecutorWiring(firstDefault, defaultContext, clock);
            AssertExecutorWiring(firstNamed, secondaryContext, clock);
        }

        using IServiceScope secondScope = provider.CreateScope();
        IRoutingSlipExecutor secondDefault = secondScope.ServiceProvider.GetRequiredService<IRoutingSlipExecutor>();
        IRoutingSlipExecutor secondNamed = secondScope.ServiceProvider
            .GetRequiredService<Bind<ISecondaryBus, IRoutingSlipExecutor>>()
            .Value;

        Assert.NotSame(firstDefault, secondDefault);
        Assert.NotSame(firstNamed, secondNamed);
        AssertExecutorWiring(secondDefault, defaultContext, clock);
        AssertExecutorWiring(secondNamed, secondaryContext, clock);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-existing-executor-registrations-win")]
    public void CourierServiceRegistration_PreservesExistingDefaultAndNamedExecutors()
    {
        var defaultExecutor = new StubRoutingSlipExecutor();
        var namedExecutor = new StubRoutingSlipExecutor();
        Bind<ISecondaryBus, IRoutingSlipExecutor> namedBinding =
            Bind<ISecondaryBus>.Create<IRoutingSlipExecutor>(namedExecutor);
        var services = new ServiceCollection();
        services.AddSingleton<IRoutingSlipExecutor>(defaultExecutor);
        services.AddSingleton(namedBinding);

        CourierServiceRegistration.Register(services, typeof(IBus));
        CourierServiceRegistration.Register(services, typeof(ISecondaryBus));
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Same(defaultExecutor, provider.GetRequiredService<IRoutingSlipExecutor>());
        Assert.Same(namedBinding, provider.GetRequiredService<Bind<ISecondaryBus, IRoutingSlipExecutor>>());
        Assert.Same(namedExecutor, provider.GetRequiredService<Bind<ISecondaryBus, IRoutingSlipExecutor>>().Value);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRoutingSlipExecutor));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(Bind<ISecondaryBus, IRoutingSlipExecutor>));
    }

    private static void AssertAsyncFactoryMethod(Type contract, string methodName)
    {
        MethodInfo method = Assert.Single(contract.GetMethods(), candidate => candidate.Name == methodName);
        ParameterInfo[] parameters = method.GetParameters();

        Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.True(parameters[2].IsOptional);
        Assert.True(parameters[2].HasDefaultValue);
    }

    private static void AssertExecutorWiring(
        IRoutingSlipExecutor executor,
        RecordingScopedBusContext context,
        TimeProvider clock)
    {
        RoutingSlipExecutor concrete = Assert.IsType<RoutingSlipExecutor>(executor);

        Assert.Same(context.SendEndpointProvider, GetField<ISendEndpointProvider>(concrete, "_sendEndpointProvider"));
        Assert.Same(context.PublishEndpoint, GetField<IPublishEndpoint>(concrete, "_publishEndpoint"));
        Assert.Same(clock, GetField<TimeProvider>(concrete, "_timeProvider"));
    }

    private static T GetField<T>(object instance, string name)
        where T : class
    {
        FieldInfo? field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<T>(field.GetValue(instance));
    }

    private static void AssertParameter(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static async Task AssertParameterAsync(string parameterName, Func<Task> action) =>
        Assert.Equal(parameterName, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    private static T Proxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static ExecuteContext<TestArguments> CreateExecuteContext(TestArguments arguments)
    {
        TestExecuteContext context = DispatchProxy.Create<TestExecuteContext, ExecuteContextProxy>();
        ((ExecuteContextProxy)(object)context).Arguments = arguments;
        return context;
    }

    private static CompensateContext<TestLog> CreateCompensateContext(TestLog log)
    {
        TestCompensateContext context = DispatchProxy.Create<TestCompensateContext, CompensateContextProxy>();
        ((CompensateContextProxy)(object)context).Log = log;
        return context;
    }

    private interface TestExecuteContext : ExecuteContext<TestArguments>;

    private interface TestCompensateContext : CompensateContext<TestLog>;

    private interface TestExecuteActivityContext<out TActivity> : ExecuteActivityContext<TActivity, TestArguments>
        where TActivity : class;

    private interface TestCompensateActivityContext<out TActivity> : CompensateActivityContext<TActivity, TestLog>
        where TActivity : class;

    private class ExecuteContextProxy : DispatchProxy
    {
        private static readonly MethodInfo CreateContextMethod = typeof(ExecuteContextProxy)
            .GetMethod(nameof(CreateActivityContext), BindingFlags.NonPublic | BindingFlags.Static)!;

        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Arguments" => Arguments,
            "CreateActivityContext" => CreateContextMethod
                .MakeGenericMethod(targetMethod.GetGenericArguments()[0])
                .Invoke(null, [args![0]!, Arguments]),
            _ => throw new NotSupportedException($"Unexpected execute-context member: {targetMethod?.Name}"),
        };

        private static object CreateActivityContext<TActivity>(object activity, TestArguments arguments)
            where TActivity : class
        {
            TestExecuteActivityContext<TActivity> context =
                DispatchProxy.Create<TestExecuteActivityContext<TActivity>, ExecuteActivityContextProxy<TActivity>>();
            var proxy = (ExecuteActivityContextProxy<TActivity>)(object)context;
            proxy.Activity = Assert.IsType<TActivity>(activity);
            proxy.Arguments = arguments;
            return context;
        }
    }

    private class CompensateContextProxy : DispatchProxy
    {
        private static readonly MethodInfo CreateContextMethod = typeof(CompensateContextProxy)
            .GetMethod(nameof(CreateActivityContext), BindingFlags.NonPublic | BindingFlags.Static)!;

        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Log" => Log,
            "CreateActivityContext" => CreateContextMethod
                .MakeGenericMethod(targetMethod.GetGenericArguments()[0])
                .Invoke(null, [args![0]!, Log]),
            _ => throw new NotSupportedException($"Unexpected compensate-context member: {targetMethod?.Name}"),
        };

        private static object CreateActivityContext<TActivity>(object activity, TestLog log)
            where TActivity : class
        {
            TestCompensateActivityContext<TActivity> context =
                DispatchProxy.Create<TestCompensateActivityContext<TActivity>, CompensateActivityContextProxy<TActivity>>();
            var proxy = (CompensateActivityContextProxy<TActivity>)(object)context;
            proxy.Activity = Assert.IsType<TActivity>(activity);
            proxy.Log = log;
            return context;
        }
    }

    private class ExecuteActivityContextProxy<TActivity> : DispatchProxy
        where TActivity : class
    {
        public TActivity Activity { get; set; } = null!;
        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Activity" => Activity,
            "get_Arguments" => Arguments,
            _ => throw new NotSupportedException($"Unexpected execute-activity-context member: {targetMethod?.Name}"),
        };
    }

    private class CompensateActivityContextProxy<TActivity> : DispatchProxy
        where TActivity : class
    {
        public TActivity Activity { get; set; } = null!;
        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Activity" => Activity,
            "get_Log" => Log,
            _ => throw new NotSupportedException($"Unexpected compensate-activity-context member: {targetMethod?.Name}"),
        };
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected member: {targetMethod?.Name}");
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;

        public List<string> ScopeKeys { get; } = [];

        public void Add(string key, string? value) => throw new NotSupportedException();

        public void Add(string key, object? value) => throw new NotSupportedException();

        public void Set(object values) => throw new NotSupportedException();

        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();

        public ProbeContext CreateScope(string key)
        {
            ScopeKeys.Add(key);
            return this;
        }
    }

    private sealed class RecordingScopedBusContext : ScopedBusContext
    {
        public ISendEndpointProvider SendEndpointProvider { get; } = Proxy<ISendEndpointProvider>();

        public IPublishEndpoint PublishEndpoint { get; } = Proxy<IPublishEndpoint>();

        public IScopedClientFactory ClientFactory => throw new NotSupportedException();
    }

    private sealed class StaticScopedBusContextProvider<TBus>(ScopedBusContext context) : IScopedBusContextProvider<TBus>
        where TBus : class, IBus
    {
        public ScopedBusContext Context { get; } = context;
    }

    private sealed class RecordingTimeProvider : TimeProvider;

    private sealed class StubRoutingSlipExecutor : IRoutingSlipExecutor
    {
        public Task ExecuteAsync(
            ViciOne.ServiceBus.Courier.Contracts.IRoutingSlip routingSlip,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record TestArguments(string Value);

    private sealed record TestLog(string Value);

    private sealed class TestActivity : IActivity<TestArguments, TestLog>, IDisposable
    {
        public int DisposeCount { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<TestLog> context) => throw new NotSupportedException();

        public void Dispose() => DisposeCount++;
    }

    private sealed class ExecuteOnlyActivity : IExecuteActivity<TestArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();
    }

    private sealed class AsyncReleaseFaultingActivity(ExpectedReleaseException releaseFailure) :
        IExecuteActivity<TestArguments>,
        IAsyncDisposable
    {
        public int DisposeAsyncCount { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return ValueTask.FromException(releaseFailure);
        }
    }

    private sealed class SyncReleaseFaultingActivity(ExpectedReleaseException releaseFailure) :
        ICompensateActivity<TestLog>,
        IDisposable
    {
        public int DisposeCount { get; private set; }

        public Task<CompensationResult> CompensateAsync(CompensateContext<TestLog> context) => throw new NotSupportedException();

        public void Dispose()
        {
            DisposeCount++;
            throw releaseFailure;
        }
    }

    private interface ISecondaryBus : IBus;

    private interface IOpenBus<T> : IBus;

    private sealed class ExpectedFactoryException(string message) : Exception(message);

    private sealed class ExpectedReleaseException(string message) : Exception(message);
}
