using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityRegistrationAndScopeFactoryDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-registration-identity-default-definition-and-exclusion")]
    public void Registrations_ExposeExactIdentityDefaultDefinitionsAndAttributeExclusion()
    {
        TestRegistrationContext context = Proxy<TestRegistrationContext>();
        var activitySelector = new RecordingContainerSelector();
        var activity = new ActivityRegistration<TestActivity, Arguments, Log>(activitySelector);
        IActivityRegistration activityContract = activity;
        var executeSelector = new RecordingContainerSelector();
        var execute = new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(executeSelector);
        IExecuteActivityRegistration executeContract = execute;

        IActivityDefinition activityDefinition = activityContract.GetDefinition(context);
        IExecuteActivityDefinition executeDefinition = executeContract.GetDefinition(context);

        Assert.Equal(typeof(TestActivity), activity.Type);
        Assert.True(activity.IncludeInConfigureEndpoints);
        Assert.IsType<DefaultActivityDefinition<TestActivity, Arguments, Log>>(activityDefinition);
        Assert.Equal(typeof(TestActivity), activityDefinition.ActivityType);
        Assert.Equal(typeof(Arguments), activityDefinition.ArgumentType);
        Assert.Equal(typeof(Log), activityDefinition.LogType);
        Assert.Same(activityDefinition, activityContract.GetDefinition(context));
        Assert.Same(context, activitySelector.DefinitionProvider);
        Assert.Equal(1, activitySelector.DefinitionCalls);
        Assert.Equal([typeof(IExecuteActivity<Arguments>), typeof(ICompensateActivity<Log>)],
            activitySelector.EndpointDefinitionRequests);

        Assert.Equal(typeof(ExecuteOnlyActivity), execute.Type);
        Assert.True(execute.IncludeInConfigureEndpoints);
        Assert.IsType<DefaultExecuteActivityDefinition<ExecuteOnlyActivity, Arguments>>(executeDefinition);
        Assert.Equal(typeof(ExecuteOnlyActivity), executeDefinition.ActivityType);
        Assert.Equal(typeof(Arguments), executeDefinition.ArgumentType);
        Assert.Same(executeDefinition, executeContract.GetDefinition(context));
        Assert.Same(context, executeSelector.DefinitionProvider);
        Assert.Equal(1, executeSelector.DefinitionCalls);
        Assert.Equal([typeof(IExecuteActivity<Arguments>)], executeSelector.EndpointDefinitionRequests);

        Assert.False(new ActivityRegistration<ExcludedActivity, Arguments, Log>(new RecordingContainerSelector())
            .IncludeInConfigureEndpoints);
        Assert.False(new ExecuteActivityRegistration<ExcludedExecuteActivity, Arguments>(new RecordingContainerSelector())
            .IncludeInConfigureEndpoints);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-definition-action-specification-order-and-context")]
    public void Registrations_ApplyDefinitionsActionsAndSpecificationsInExactOrder()
    {
        var activityEvents = new List<string>();
        TestRegistrationContext context = Proxy<TestRegistrationContext>();
        IReceiveEndpointConfigurator executeEndpoint = EndpointProxy("execute", "execute", activityEvents, out EndpointRecorder executeRecorder);
        IReceiveEndpointConfigurator compensateEndpoint = EndpointProxy("compensate", "compensate", activityEvents,
            out EndpointRecorder compensateRecorder);
        var activityDefinition = new OrderedActivityDefinition(activityEvents, executeEndpoint, compensateEndpoint, context);
        var activity = new ActivityRegistration<TestActivity, Arguments, Log>(new RecordingContainerSelector(activityDefinition));
        activity.AddConfigureAction<TestActivity, Arguments>((actualContext, configurator) =>
        {
            Assert.Same(context, actualContext);
            Assert.IsType<ExecuteActivityHostConfigurator<TestActivity, Arguments>>(configurator);
            activityEvents.Add("execute-action");
        });
        activity.AddConfigureAction<TestActivity, Log>((actualContext, configurator) =>
        {
            Assert.Same(context, actualContext);
            Assert.IsType<CompensateActivityHostConfigurator<TestActivity, Log>>(configurator);
            activityEvents.Add("compensate-action");
        });

        activity.Configure(executeEndpoint, compensateEndpoint, context);

        Assert.Equal(
            [
                "compensate-topology:false",
                "compensate-definition",
                "compensate-action",
                "compensate-specification",
                "execute-topology:false",
                "execute-definition",
                "execute-action",
                "execute-specification",
            ],
            activityEvents);
        Assert.False(executeRecorder.ConfigureConsumeTopology);
        Assert.False(compensateRecorder.ConfigureConsumeTopology);
        Assert.False(activity.IncludeInConfigureEndpoints);

        var executeEvents = new List<string>();
        IReceiveEndpointConfigurator executeOnlyEndpoint = EndpointProxy("execute-only", "execute-only", executeEvents,
            out EndpointRecorder executeOnlyRecorder);
        var executeOnlyDefinition = new OrderedExecuteDefinition(executeEvents, executeOnlyEndpoint, context);
        IExecuteActivityRegistration executeOnly = new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(
            new RecordingContainerSelector(executeOnlyDefinition));
        executeOnly.AddConfigureAction<ExecuteOnlyActivity, Arguments>((actualContext, configurator) =>
        {
            Assert.Same(context, actualContext);
            Assert.IsType<ExecuteActivityHostConfigurator<ExecuteOnlyActivity, Arguments>>(configurator);
            executeEvents.Add("execute-only-action");
        });

        executeOnly.Configure(executeOnlyEndpoint, context);

        Assert.Equal(
            ["execute-only-topology:false", "execute-only-definition", "execute-only-action", "execute-only-specification"],
            executeEvents);
        Assert.False(executeOnlyRecorder.ConfigureConsumeTopology);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "deep-explicit-endpoint-registration-identity-and-settings")]
    public void RegistrationConfigurators_ForwardExactRegistrationIdentityAndEndpointSettings()
    {
        RecordingRegistrationConfigurator bus = DispatchProxy.Create<RecordingRegistrationConfigurator, RegistrationConfiguratorRecorder>();
        var recorder = (RegistrationConfiguratorRecorder)(object)bus;
        var activityRegistration = new ActivityRegistration<TestActivity, Arguments, Log>(new RecordingContainerSelector());
        var activity = new ActivityRegistrationConfigurator<TestActivity, Arguments, Log>(bus, activityRegistration);
        var executeRegistration = new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(new RecordingContainerSelector());
        var execute = new ExecuteActivityRegistrationConfigurator<ExecuteOnlyActivity, Arguments>(bus, executeRegistration);

        Assert.Same(activity, activity.ExecuteEndpoint(endpoint =>
        {
            endpoint.Name = "activity-execute";
            endpoint.Temporary = true;
            endpoint.PrefetchCount = 17;
            endpoint.ConcurrentMessageLimit = 5;
            endpoint.InstanceId = "execute-instance";
        }));
        Assert.Same(activity, activity.CompensateEndpoint(endpoint =>
        {
            endpoint.Name = "activity-compensate";
            endpoint.Temporary = true;
            endpoint.PrefetchCount = 19;
            endpoint.ConcurrentMessageLimit = 7;
            endpoint.InstanceId = "compensate-instance";
        }));
        execute.Endpoint(endpoint =>
        {
            endpoint.Name = "execute-only";
            endpoint.Temporary = true;
            endpoint.PrefetchCount = 23;
            endpoint.ConcurrentMessageLimit = 11;
            endpoint.InstanceId = "execute-only-instance";
        });

        Assert.Collection(
            recorder.EndpointCalls,
            call =>
            {
                Assert.Equal(typeof(ExecuteActivityEndpointDefinition<TestActivity, Arguments>), call.DefinitionType);
                Assert.Equal(typeof(IExecuteActivity<Arguments>), call.MessageType);
                Assert.Same(activityRegistration, call.Registration);
                AssertEndpointSettings<IExecuteActivity<Arguments>>(call.Settings, "activity-execute", 17, 5,
                    "execute-instance");
            },
            call =>
            {
                Assert.Equal(typeof(CompensateActivityEndpointDefinition<TestActivity, Log>), call.DefinitionType);
                Assert.Equal(typeof(ICompensateActivity<Log>), call.MessageType);
                Assert.Same(activityRegistration, call.Registration);
                AssertEndpointSettings<ICompensateActivity<Log>>(call.Settings, "activity-compensate", 19, 7,
                    "compensate-instance");
            },
            call =>
            {
                Assert.Equal(typeof(ExecuteActivityEndpointDefinition<ExecuteOnlyActivity, Arguments>), call.DefinitionType);
                Assert.Equal(typeof(IExecuteActivity<Arguments>), call.MessageType);
                Assert.Same(executeRegistration, call.Registration);
                AssertEndpointSettings<IExecuteActivity<Arguments>>(call.Settings, "execute-only", 23, 11,
                    "execute-only-instance");
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-scoped-factory-input-boundaries-before-provider")]
    public async Task ScopedFactories_RejectMissingOperationInputsBeforeCallingProvidersAsync()
    {
        var executeProvider = new RecordingExecuteScopeProvider((_, _) => throw new InvalidOperationException("Unexpected provider call."));
        var compensateProvider = new RecordingCompensateScopeProvider((_, _) => throw new InvalidOperationException("Unexpected provider call."));
        var execute = new ScopeExecuteActivityFactory<TestActivity, Arguments>(executeProvider);
        var compensate = new ScopeCompensateActivityFactory<TestActivity, Log>(compensateProvider);
        ExecuteContext<Arguments> executeContext = Proxy<ExecuteContext<Arguments>>();
        CompensateContext<Log> compensateContext = Proxy<CompensateContext<Log>>();
        var executePipe = new DelegatePipe<ExecuteActivityContext<TestActivity, Arguments>>(_ => Task.CompletedTask);
        var compensatePipe = new DelegatePipe<CompensateActivityContext<TestActivity, Log>>(_ => Task.CompletedTask);

        await AssertParameterAsync("context", () =>
            execute.ExecuteAsync(null!, executePipe, TestContext.Current.CancellationToken));
        await AssertParameterAsync("next", () =>
            execute.ExecuteAsync(executeContext, null!, TestContext.Current.CancellationToken));
        await AssertParameterAsync("context", () =>
            compensate.CompensateAsync(null!, compensatePipe, TestContext.Current.CancellationToken));
        await AssertParameterAsync("next", () =>
            compensate.CompensateAsync(compensateContext, null!, TestContext.Current.CancellationToken));

        Assert.Equal(0, executeProvider.GetActivityScopeCalls);
        Assert.Equal(0, compensateProvider.GetActivityScopeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-scoped-factory-exact-context-token-forwarding-and-ownership")]
    public async Task ScopedFactories_ForwardExactContextAndTokenAndDisposeAfterSuccessfulPipelinesAsync()
    {
        var events = new List<string>();
        ExecuteContext<Arguments> executeContext = Proxy<ExecuteContext<Arguments>>();
        CompensateContext<Log> compensateContext = Proxy<CompensateContext<Log>>();
        ExecuteActivityContext<TestActivity, Arguments> executeActivityContext = Proxy<ExecuteActivityContext<TestActivity, Arguments>>();
        CompensateActivityContext<TestActivity, Log> compensateActivityContext = Proxy<CompensateActivityContext<TestActivity, Log>>();
        var executeScope = new RecordingExecuteScope(executeActivityContext, events, "execute-dispose");
        var compensateScope = new RecordingCompensateScope(compensateActivityContext, events, "compensate-dispose");
        var executeProvider = new RecordingExecuteScopeProvider((_, _) =>
        {
            events.Add("execute-provider");
            return new ValueTask<IExecuteActivityScopeContext<TestActivity, Arguments>>(executeScope);
        });
        var compensateProvider = new RecordingCompensateScopeProvider((_, _) =>
        {
            events.Add("compensate-provider");
            return new ValueTask<ICompensateActivityScopeContext<TestActivity, Log>>(compensateScope);
        });
        var execute = new ScopeExecuteActivityFactory<TestActivity, Arguments>(executeProvider);
        var compensate = new ScopeCompensateActivityFactory<TestActivity, Log>(compensateProvider);
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;

        await execute.ExecuteAsync(executeContext,
            new DelegatePipe<ExecuteActivityContext<TestActivity, Arguments>>(actual =>
            {
                Assert.Same(executeActivityContext, actual);
                events.Add("execute-pipeline");
                return Task.CompletedTask;
            }), token);
        await compensate.CompensateAsync(compensateContext,
            new DelegatePipe<CompensateActivityContext<TestActivity, Log>>(actual =>
            {
                Assert.Same(compensateActivityContext, actual);
                events.Add("compensate-pipeline");
                return Task.CompletedTask;
            }), token);

        Assert.Same(executeContext, executeProvider.Context);
        Assert.Equal(token, executeProvider.CancellationToken);
        Assert.Same(compensateContext, compensateProvider.Context);
        Assert.Equal(token, compensateProvider.CancellationToken);
        Assert.Equal(1, executeScope.DisposeCount);
        Assert.Equal(1, compensateScope.DisposeCount);
        Assert.Equal(
            [
                "execute-provider",
                "execute-pipeline",
                "execute-dispose",
                "compensate-provider",
                "compensate-pipeline",
                "compensate-dispose",
            ],
            events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-scoped-factory-downstream-cancellation-releases-both-scopes")]
    public async Task ScopedFactories_PreserveDownstreamCancellationAndReleaseBothScopesAsync()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        ExecuteActivityContext<TestActivity, Arguments> executeActivityContext = Proxy<ExecuteActivityContext<TestActivity, Arguments>>();
        CompensateActivityContext<TestActivity, Log> compensateActivityContext = Proxy<CompensateActivityContext<TestActivity, Log>>();
        var executeScope = new RecordingExecuteScope(executeActivityContext);
        var compensateScope = new RecordingCompensateScope(compensateActivityContext);
        var execute = new ScopeExecuteActivityFactory<TestActivity, Arguments>(new RecordingExecuteScopeProvider(
            (_, _) => new ValueTask<IExecuteActivityScopeContext<TestActivity, Arguments>>(executeScope)));
        var compensate = new ScopeCompensateActivityFactory<TestActivity, Log>(new RecordingCompensateScopeProvider(
            (_, _) => new ValueTask<ICompensateActivityScopeContext<TestActivity, Log>>(compensateScope)));

        OperationCanceledException executeFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execute.ExecuteAsync(
            Proxy<ExecuteContext<Arguments>>(),
            new DelegatePipe<ExecuteActivityContext<TestActivity, Arguments>>(_ => Task.FromCanceled(cancellation.Token)),
            TestContext.Current.CancellationToken));
        OperationCanceledException compensateFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => compensate.CompensateAsync(
            Proxy<CompensateContext<Log>>(),
            new DelegatePipe<CompensateActivityContext<TestActivity, Log>>(_ => Task.FromCanceled(cancellation.Token)),
            TestContext.Current.CancellationToken));

        Assert.Equal(cancellation.Token, executeFailure.CancellationToken);
        Assert.Equal(cancellation.Token, compensateFailure.CancellationToken);
        Assert.Equal(1, executeScope.DisposeCount);
        Assert.Equal(1, compensateScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-scoped-factory-acquisition-failure-identity-before-pipeline")]
    public async Task ScopedFactories_PreserveAcquisitionFailuresAndSkipPipelinesAsync()
    {
        var executeFailure = new ExpectedFactoryException("execute acquisition");
        var compensateFailure = new ExpectedFactoryException("compensate acquisition");
        var executeProvider = new RecordingExecuteScopeProvider((_, _) =>
            ValueTask.FromException<IExecuteActivityScopeContext<TestActivity, Arguments>>(executeFailure));
        var compensateProvider = new RecordingCompensateScopeProvider((_, _) =>
            ValueTask.FromException<ICompensateActivityScopeContext<TestActivity, Log>>(compensateFailure));
        var execute = new ScopeExecuteActivityFactory<TestActivity, Arguments>(executeProvider);
        var compensate = new ScopeCompensateActivityFactory<TestActivity, Log>(compensateProvider);
        int pipelineCalls = 0;

        ExpectedFactoryException actualExecute = await Assert.ThrowsAsync<ExpectedFactoryException>(() => execute.ExecuteAsync(
            Proxy<ExecuteContext<Arguments>>(),
            new DelegatePipe<ExecuteActivityContext<TestActivity, Arguments>>(_ =>
            {
                pipelineCalls++;
                return Task.CompletedTask;
            }), TestContext.Current.CancellationToken));
        ExpectedFactoryException actualCompensate = await Assert.ThrowsAsync<ExpectedFactoryException>(() => compensate.CompensateAsync(
            Proxy<CompensateContext<Log>>(),
            new DelegatePipe<CompensateActivityContext<TestActivity, Log>>(_ =>
            {
                pipelineCalls++;
                return Task.CompletedTask;
            }), TestContext.Current.CancellationToken));

        Assert.Same(executeFailure, actualExecute);
        Assert.Same(compensateFailure, actualCompensate);
        Assert.Equal(0, pipelineCalls);
        Assert.Equal(1, executeProvider.GetActivityScopeCalls);
        Assert.Equal(1, compensateProvider.GetActivityScopeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-scoped-factory-operation-release-dual-failure-order")]
    public async Task ScopedFactories_PreservePipelineAndScopeReleaseFailuresInOperationOrderAsync()
    {
        var executeOperationFailure = new ExpectedFactoryException("execute pipeline");
        var executeReleaseFailure = new ExpectedReleaseException("execute release");
        var executeScope = new RecordingExecuteScope(
            Proxy<ExecuteActivityContext<TestActivity, Arguments>>(), disposeFailure: executeReleaseFailure);
        var execute = new ScopeExecuteActivityFactory<TestActivity, Arguments>(new RecordingExecuteScopeProvider(
            (_, _) => new ValueTask<IExecuteActivityScopeContext<TestActivity, Arguments>>(executeScope)));

        AggregateException executeFailure = await Assert.ThrowsAsync<AggregateException>(() => execute.ExecuteAsync(
            Proxy<ExecuteContext<Arguments>>(),
            new DelegatePipe<ExecuteActivityContext<TestActivity, Arguments>>(_ => Task.FromException(executeOperationFailure)),
            TestContext.Current.CancellationToken));

        Assert.StartsWith("Activity pipeline and release encountered multiple failures.", executeFailure.Message,
            StringComparison.Ordinal);
        Assert.Collection(
            executeFailure.InnerExceptions,
            failure => Assert.Same(executeOperationFailure, failure),
            failure => Assert.Same(executeReleaseFailure, failure));
        Assert.Equal(1, executeScope.DisposeCount);

        var compensateOperationFailure = new ExpectedFactoryException("compensate pipeline");
        var compensateReleaseFailure = new ExpectedReleaseException("compensate release");
        var compensateScope = new RecordingCompensateScope(
            Proxy<CompensateActivityContext<TestActivity, Log>>(), disposeFailure: compensateReleaseFailure);
        var compensate = new ScopeCompensateActivityFactory<TestActivity, Log>(new RecordingCompensateScopeProvider(
            (_, _) => new ValueTask<ICompensateActivityScopeContext<TestActivity, Log>>(compensateScope)));

        AggregateException compensateFailure = await Assert.ThrowsAsync<AggregateException>(() => compensate.CompensateAsync(
            Proxy<CompensateContext<Log>>(),
            new DelegatePipe<CompensateActivityContext<TestActivity, Log>>(_ => Task.FromException(compensateOperationFailure)),
            TestContext.Current.CancellationToken));

        Assert.Collection(
            compensateFailure.InnerExceptions,
            failure => Assert.Same(compensateOperationFailure, failure),
            failure => Assert.Same(compensateReleaseFailure, failure));
        Assert.Equal(1, compensateScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-scoped-factory-probe-named-child-scope")]
    public void ScopedFactories_ProbeProvidersThroughExactNamedChildScopes()
    {
        var executeProvider = new RecordingExecuteScopeProvider((_, _) => throw new NotSupportedException());
        var compensateProvider = new RecordingCompensateScopeProvider((_, _) => throw new NotSupportedException());
        var execute = new ScopeExecuteActivityFactory<TestActivity, Arguments>(executeProvider);
        var compensate = new ScopeCompensateActivityFactory<TestActivity, Log>(compensateProvider);
        var executeRoot = new RecordingProbeContext();
        var compensateRoot = new RecordingProbeContext();

        execute.Probe(executeRoot);
        compensate.Probe(compensateRoot);

        RecordingProbeContext executeChild = Assert.Single(executeRoot.Children);
        RecordingProbeContext compensateChild = Assert.Single(compensateRoot.Children);
        Assert.Equal("scopeExecuteActivityFactory", executeChild.Key);
        Assert.Equal("scopeCompensateActivityFactory", compensateChild.Key);
        Assert.Same(executeChild, executeProvider.ProbeContext);
        Assert.Same(compensateChild, compensateProvider.ProbeContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "deep-scoped-factory-bidirectional-async-api-shape")]
    public void ScopedFactoryApi_UsesAsyncSuffixExactlyForTaskReturningOperations()
    {
        AssertAsyncApi(typeof(ScopeExecuteActivityFactory<TestActivity, Arguments>), "ExecuteAsync");
        AssertAsyncApi(typeof(ScopeCompensateActivityFactory<TestActivity, Log>), "CompensateAsync");
    }

    private static void AssertEndpointSettings<T>(object settings, string name, int prefetchCount, int concurrentMessageLimit,
        string instanceId)
        where T : class
    {
        var typed = Assert.IsAssignableFrom<IEndpointSettings<IEndpointDefinition<T>>>(settings);
        Assert.Equal(name, typed.Name);
        Assert.True(typed.IsTemporary);
        Assert.Equal(prefetchCount, typed.PrefetchCount);
        Assert.Equal(concurrentMessageLimit, typed.ConcurrentMessageLimit);
        Assert.False(typed.ConfigureConsumeTopology);
        Assert.Equal(instanceId, typed.InstanceId);
    }

    private static void AssertAsyncApi(Type type, string operationName)
    {
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        MethodInfo operation = Assert.Single(methods, method => method.Name == operationName);
        Assert.Equal(typeof(Task), operation.ReturnType);
        ParameterInfo cancellationToken = Assert.Single(operation.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
        Assert.Equal("cancellationToken", cancellationToken.Name);
        Assert.Same(cancellationToken, operation.GetParameters()[^1]);
        Assert.True(cancellationToken.IsOptional);
        Assert.True(cancellationToken.HasDefaultValue);
        Assert.All(methods.Where(method => method.ReturnType == typeof(Task)), method =>
            Assert.EndsWith("Async", method.Name, StringComparison.Ordinal));
        Assert.All(methods.Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal)), method =>
            Assert.Equal(typeof(Task), method.ReturnType));
    }

    private static IReceiveEndpointConfigurator EndpointProxy(string name, string eventPrefix, List<string> events,
        out EndpointRecorder recorder)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointRecorder>();
        recorder = (EndpointRecorder)(object)endpoint;
        recorder.InputAddress = new Uri($"loopback://localhost/{name}");
        recorder.EventPrefix = eventPrefix;
        recorder.Events = events;
        return endpoint;
    }

    private static T Proxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static async Task AssertParameterAsync(string expected, Func<Task> action)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(expected, exception.ParamName);
    }

    private sealed record Arguments(string Value);

    private sealed record Log(string Value);

    private interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;

    private sealed class TestActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    private sealed class ExecuteOnlyActivity : IExecuteActivity<Arguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class ExcludedActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class ExcludedExecuteActivity : IExecuteActivity<Arguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();
    }

    private sealed class OrderedActivityDefinition(
        List<string> events,
        IReceiveEndpointConfigurator executeEndpoint,
        IReceiveEndpointConfigurator compensateEndpoint,
        IRegistrationContext context) : ActivityDefinition<TestActivity, Arguments, Log>
    {
        protected override void ConfigureCompensateActivity(IReceiveEndpointConfigurator endpointConfigurator,
            ICompensateActivityConfigurator<TestActivity, Log> compensateActivityConfigurator,
            IRegistrationContext registrationContext)
        {
            Assert.Same(compensateEndpoint, endpointConfigurator);
            Assert.Same(context, registrationContext);
            Assert.False(((EndpointRecorder)(object)endpointConfigurator).ConfigureConsumeTopology);
            events.Add("compensate-definition");
        }

        protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
            IExecuteActivityConfigurator<TestActivity, Arguments> executeActivityConfigurator,
            IRegistrationContext registrationContext)
        {
            Assert.Same(executeEndpoint, endpointConfigurator);
            Assert.Same(context, registrationContext);
            Assert.False(((EndpointRecorder)(object)endpointConfigurator).ConfigureConsumeTopology);
            events.Add("execute-definition");
        }
    }

    private sealed class OrderedExecuteDefinition(
        List<string> events,
        IReceiveEndpointConfigurator endpoint,
        IRegistrationContext context) : ExecuteActivityDefinition<ExecuteOnlyActivity, Arguments>
    {
        protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
            IExecuteActivityConfigurator<ExecuteOnlyActivity, Arguments> executeActivityConfigurator,
            IRegistrationContext registrationContext)
        {
            Assert.Same(endpoint, endpointConfigurator);
            Assert.Same(context, registrationContext);
            Assert.False(((EndpointRecorder)(object)endpointConfigurator).ConfigureConsumeTopology);
            events.Add("execute-only-definition");
        }
    }

    private sealed class RecordingContainerSelector(IDefinition? definition = null) : IContainerSelector
    {
        public IServiceProvider? DefinitionProvider { get; private set; }

        public int DefinitionCalls { get; private set; }

        public List<Type> EndpointDefinitionRequests { get; } = [];

        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
            where T : class, IRegistration
        {
            value = null;
            return false;
        }

        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
            where T : class, IRegistration => [];

        public T? GetDefinition<T>(IServiceProvider provider)
            where T : class, IDefinition
        {
            DefinitionCalls++;
            DefinitionProvider = provider;
            return definition as T;
        }

        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
            where T : class
        {
            EndpointDefinitionRequests.Add(typeof(T));
            return null;
        }

        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) => throw new NotSupportedException();

        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) => throw new NotSupportedException();
    }

    private interface RecordingRegistrationConfigurator : IRegistrationConfigurator, IAdvancedRegistrationConfigurator;

    private sealed record EndpointCall(Type DefinitionType, Type MessageType, IRegistration Registration, object Settings);

    private class RegistrationConfiguratorRecorder : DispatchProxy
    {
        public List<EndpointCall> EndpointCalls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IAdvancedRegistrationConfigurator.AddEndpoint) && targetMethod.IsGenericMethod)
            {
                Type[] types = targetMethod.GetGenericArguments();
                EndpointCalls.Add(new EndpointCall(types[0], types[1], (IRegistration)args![0]!, args[1]!));
                return null;
            }

            throw new NotSupportedException($"Unexpected registration-configurator member: {targetMethod.Name}");
        }
    }

    private class EndpointRecorder : DispatchProxy
    {
        public bool ConfigureConsumeTopology { get; private set; } = true;

        public string EventPrefix { get; set; } = null!;

        public List<string> Events { get; set; } = null!;

        public Uri InputAddress { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_InputAddress":
                    return InputAddress;
                case "set_ConfigureConsumeTopology":
                    ConfigureConsumeTopology = (bool)args![0]!;
                    Events.Add($"{EventPrefix}-topology:{ConfigureConsumeTopology.ToString().ToLowerInvariant()}");
                    return null;
                case nameof(IReceiveEndpointConfigurator.AddEndpointSpecification):
                    Events.Add($"{EventPrefix}-specification");
                    return null;
                default:
                    throw new NotSupportedException($"Unexpected endpoint member: {targetMethod.Name}");
            }
        }
    }

    private sealed class RecordingExecuteScopeProvider(
        Func<ExecuteContext<Arguments>, CancellationToken,
            ValueTask<IExecuteActivityScopeContext<TestActivity, Arguments>>> acquire) :
        IExecuteActivityScopeProvider<TestActivity, Arguments>
    {
        public CancellationToken CancellationToken { get; private set; }

        public ExecuteContext<Arguments>? Context { get; private set; }

        public int GetActivityScopeCalls { get; private set; }

        public ProbeContext? ProbeContext { get; private set; }

        public ValueTask<IExecuteScopeContext<Arguments>> GetScopeAsync(ExecuteContext<Arguments> context,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IExecuteActivityScopeContext<TestActivity, Arguments>> GetActivityScopeAsync(
            ExecuteContext<Arguments> context,
            CancellationToken cancellationToken = default)
        {
            GetActivityScopeCalls++;
            Context = context;
            CancellationToken = cancellationToken;
            return acquire(context, cancellationToken);
        }

        public void Probe(ProbeContext context) => ProbeContext = context;
    }

    private sealed class RecordingCompensateScopeProvider(
        Func<CompensateContext<Log>, CancellationToken,
            ValueTask<ICompensateActivityScopeContext<TestActivity, Log>>> acquire) :
        ICompensateActivityScopeProvider<TestActivity, Log>
    {
        public CancellationToken CancellationToken { get; private set; }

        public CompensateContext<Log>? Context { get; private set; }

        public int GetActivityScopeCalls { get; private set; }

        public ProbeContext? ProbeContext { get; private set; }

        public ValueTask<ICompensateScopeContext<Log>> GetScopeAsync(CompensateContext<Log> context,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<ICompensateActivityScopeContext<TestActivity, Log>> GetActivityScopeAsync(
            CompensateContext<Log> context,
            CancellationToken cancellationToken = default)
        {
            GetActivityScopeCalls++;
            Context = context;
            CancellationToken = cancellationToken;
            return acquire(context, cancellationToken);
        }

        public void Probe(ProbeContext context) => ProbeContext = context;
    }

    private sealed class RecordingExecuteScope(
        ExecuteActivityContext<TestActivity, Arguments> context,
        List<string>? events = null,
        string? disposeEvent = null,
        Exception? disposeFailure = null) : IExecuteActivityScopeContext<TestActivity, Arguments>
    {
        public ExecuteActivityContext<TestActivity, Arguments> Context { get; } = context;

        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            if (disposeEvent is not null)
                events!.Add(disposeEvent);
            return disposeFailure is null ? default : ValueTask.FromException(disposeFailure);
        }

        public T GetService<T>()
            where T : class => throw new NotSupportedException();
    }

    private sealed class RecordingCompensateScope(
        CompensateActivityContext<TestActivity, Log> context,
        List<string>? events = null,
        string? disposeEvent = null,
        Exception? disposeFailure = null) : ICompensateActivityScopeContext<TestActivity, Log>
    {
        public CompensateActivityContext<TestActivity, Log> Context { get; } = context;

        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            if (disposeEvent is not null)
                events!.Add(disposeEvent);
            return disposeFailure is null ? default : ValueTask.FromException(disposeFailure);
        }

        public T GetService<T>()
            where T : class => throw new NotSupportedException();
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class RecordingProbeContext(string? key = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;

        public List<RecordingProbeContext> Children { get; } = [];

        public string? Key { get; } = key;

        public void Add(string key, string? value) => throw new NotSupportedException();

        public void Add(string key, object? value) => throw new NotSupportedException();

        public void Set(object values) => throw new NotSupportedException();

        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();

        public ProbeContext CreateScope(string scopeKey)
        {
            var child = new RecordingProbeContext(scopeKey);
            Children.Add(child);
            return child;
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected member: {targetMethod?.Name}");
    }

    private sealed class ExpectedFactoryException(string message) : Exception(message);

    private sealed class ExpectedReleaseException(string message) : Exception(message);
}
