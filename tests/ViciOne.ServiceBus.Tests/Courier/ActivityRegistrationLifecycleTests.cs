using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ActivityRegistrationLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "constructors-reject-missing-dependencies")]
    public void Constructors_RejectMissingDependencies()
    {
        var selector = new RecordingContainerSelector();
        var activityRegistration = new ActivityRegistration<TestActivity, Arguments, Log>(selector);
        var executeRegistration = new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(selector);
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        AssertParameter("selector", () => new ActivityRegistration<TestActivity, Arguments, Log>(null!));
        AssertParameter("selector", () => new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(null!));
        AssertParameter("configurator", () =>
            new ActivityRegistrationConfigurator<TestActivity, Arguments, Log>(null!, activityRegistration));
        AssertParameter("registration", () =>
            new ActivityRegistrationConfigurator<TestActivity, Arguments, Log>(configurator, null!));
        AssertParameter("configurator", () =>
            new ExecuteActivityRegistrationConfigurator<ExecuteOnlyActivity, Arguments>(null!, executeRegistration));
        AssertParameter("registration", () =>
            new ExecuteActivityRegistrationConfigurator<ExecuteOnlyActivity, Arguments>(configurator, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "definition-boundaries-remain-null-safe-after-caching")]
    public void Definitions_RejectMissingContextsBeforeAndAfterCaching()
    {
        IRegistrationContext context = Proxy<TestRegistrationContext>();
        IActivityRegistration activity = new ActivityRegistration<TestActivity, Arguments, Log>(new RecordingContainerSelector());
        IExecuteActivityRegistration execute =
            new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(new RecordingContainerSelector());

        AssertParameter("context", () => activity.GetDefinition(null!));
        AssertParameter("context", () => execute.GetDefinition(null!));

        Assert.NotNull(activity.GetDefinition(context));
        Assert.NotNull(execute.GetDefinition(context));

        AssertParameter("context", () => activity.GetDefinition(null!));
        AssertParameter("context", () => execute.GetDefinition(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "activity-definition-publication-is-atomic-and-cached")]
    public async Task ActivityDefinition_IsPublishedAtomicallyAndCachedAsync()
    {
        var definition = new TestActivityDefinition();
        IEndpointDefinition<IExecuteActivity<Arguments>> executeEndpoint =
            Proxy<IEndpointDefinition<IExecuteActivity<Arguments>>>();
        IEndpointDefinition<ICompensateActivity<Log>> compensateEndpoint =
            Proxy<IEndpointDefinition<ICompensateActivity<Log>>>();
        var selector = new RecordingContainerSelector(definition, executeEndpoint, compensateEndpoint);
        IActivityRegistration registration = new ActivityRegistration<TestActivity, Arguments, Log>(selector);
        IRegistrationContext context = Proxy<IRegistrationContext>();

        IActivityDefinition[] results = await ResolveConcurrentlyAsync(() => registration.GetDefinition(context), selector);

        Assert.All(results, result => Assert.Same(definition, result));
        Assert.Same(executeEndpoint, definition.ExecuteEndpointDefinition);
        Assert.Same(compensateEndpoint, definition.CompensateEndpointDefinition);
        Assert.Equal(1, selector.DefinitionCalls);
        Assert.Equal(1, selector.ExecuteEndpointCalls);
        Assert.Equal(1, selector.CompensateEndpointCalls);
        Assert.Same(definition, registration.GetDefinition(context));
        Assert.Equal(1, selector.DefinitionCalls);
        Assert.Equal(
            [typeof(IActivityDefinition<TestActivity, Arguments, Log>),
                typeof(ExecuteActivityEndpointDefinition<TestActivity, Arguments>),
                typeof(CompensateActivityEndpointDefinition<TestActivity, Log>)],
            selector.DefinitionRequests);
        Assert.All(selector.DefinitionProviders, provider => Assert.Same(context, provider));
        Assert.Equal(1, selector.ExecuteEndpointCalls);
        Assert.Equal(1, selector.CompensateEndpointCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "execute-definition-publication-is-atomic-and-cached")]
    public async Task ExecuteDefinition_IsPublishedAtomicallyAndCachedAsync()
    {
        var definition = new TestExecuteActivityDefinition();
        IEndpointDefinition<IExecuteActivity<Arguments>> endpoint =
            Proxy<IEndpointDefinition<IExecuteActivity<Arguments>>>();
        var selector = new RecordingContainerSelector(definition, endpoint);
        IExecuteActivityRegistration registration =
            new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(selector);
        IRegistrationContext context = Proxy<IRegistrationContext>();

        IExecuteActivityDefinition[] results =
            await ResolveConcurrentlyAsync(() => registration.GetDefinition(context), selector);

        Assert.All(results, result => Assert.Same(definition, result));
        Assert.Same(endpoint, definition.ExecuteEndpointDefinition);
        Assert.Equal(1, selector.DefinitionCalls);
        Assert.Equal(1, selector.ExecuteEndpointCalls);
        Assert.Equal(0, selector.CompensateEndpointCalls);
        Assert.Same(definition, registration.GetDefinition(context));
        Assert.Equal(1, selector.DefinitionCalls);
        Assert.Equal(
            [typeof(IExecuteActivityDefinition<ExecuteOnlyActivity, Arguments>),
                typeof(ExecuteActivityEndpointDefinition<ExecuteOnlyActivity, Arguments>)],
            selector.DefinitionRequests);
        Assert.All(selector.DefinitionProviders, provider => Assert.Same(context, provider));
        Assert.Equal(1, selector.ExecuteEndpointCalls);
        Assert.Equal(0, selector.CompensateEndpointCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "configuration-boundaries-and-exclusion-are-enforced")]
    public void Configurators_EnforceCallbacksEndpointDefaultsAndExclusion()
    {
        var services = new ServiceCollection();
        var bus = new ServiceCollectionBusConfigurator(services);
        var activityRegistration = new ActivityRegistration<TestActivity, Arguments, Log>(new RecordingContainerSelector());
        var activity = new ActivityRegistrationConfigurator<TestActivity, Arguments, Log>(bus, activityRegistration);
        var executeRegistration =
            new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(new RecordingContainerSelector());
        var execute = new ExecuteActivityRegistrationConfigurator<ExecuteOnlyActivity, Arguments>(bus, executeRegistration);

        AssertParameter("configureExecute", () => activity.ExecuteEndpoint(null!));
        AssertParameter("configureCompensate", () => activity.CompensateEndpoint(null!));
        AssertParameter("configure", () => execute.Endpoint(null!));

        bool executeConfigured = false;
        bool compensateConfigured = false;
        Assert.Same(activity, activity.ExecuteEndpoint(endpoint =>
        {
            executeConfigured = true;
            endpoint.Name = "atomic-activity-execute";
        }));
        Assert.Same(activity, activity.CompensateEndpoint(endpoint =>
        {
            compensateConfigured = true;
            endpoint.Name = "atomic-activity-compensate";
        }));
        execute.Endpoint(endpoint =>
        {
            endpoint.Name = "atomic-execute-only";
        });

        Assert.True(executeConfigured);
        Assert.True(compensateConfigured);
        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType ==
            typeof(IEndpointDefinition<IExecuteActivity<Arguments>>)));
        Assert.Single(services, descriptor => descriptor.ServiceType ==
            typeof(IEndpointDefinition<ICompensateActivity<Log>>));

        activity.ExcludeFromConfigureEndpoints();
        execute.ExcludeFromConfigureEndpoints();

        Assert.False(activityRegistration.IncludeInConfigureEndpoints);
        Assert.False(executeRegistration.IncludeInConfigureEndpoints);
        Assert.Throws<ConfigurationException>(() => activity.ExecuteEndpoint(_ => { }));
        Assert.Throws<ConfigurationException>(() => activity.CompensateEndpoint(_ => { }));
        Assert.Throws<ConfigurationException>(() => execute.Endpoint(_ => { }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "registration-methods-validate-endpoint-inputs")]
    public void Registrations_RejectMissingEndpointInputs()
    {
        IRegistrationContext context = Proxy<IRegistrationContext>();
        IReceiveEndpointConfigurator endpoint = Proxy<IReceiveEndpointConfigurator>();
        var activity = new ActivityRegistration<TestActivity, Arguments, Log>(new RecordingContainerSelector());
        var execute = new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(new RecordingContainerSelector());
        var compensateAddress = new Uri("loopback://localhost/compensate");

        AssertParameter("executeEndpointConfigurator", () => activity.Configure(null!, endpoint, context));
        AssertParameter("compensateEndpointConfigurator", () => activity.Configure(endpoint, null!, context));
        AssertParameter("context", () => activity.Configure(endpoint, endpoint, null!));
        AssertParameter("configurator", () => activity.ConfigureCompensate(null!, context));
        AssertParameter("context", () => activity.ConfigureCompensate(endpoint, null!));
        AssertParameter("configurator", () => activity.ConfigureExecute(null!, context, compensateAddress));
        AssertParameter("context", () => activity.ConfigureExecute(endpoint, null!, compensateAddress));
        AssertParameter("compensateAddress", () => activity.ConfigureExecute(endpoint, context, null!));
        AssertParameter("configurator", () => execute.Configure(null!, context));
        AssertParameter("context", () => execute.Configure(endpoint, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION-LIFECYCLE", "registrations-configure-exact-actions-definitions-and-endpoints")]
    public void Registrations_ConfigureExactActionsDefinitionsAndEndpoints()
    {
        IRegistrationContext context = Proxy<TestRegistrationContext>();
        IReceiveEndpointConfigurator activityExecuteEndpoint = EndpointProxy("activity-execute", out EndpointRecorder activityExecute);
        IReceiveEndpointConfigurator activityCompensateEndpoint = EndpointProxy("activity-compensate", out EndpointRecorder activityCompensate);
        var activityDefinition = new TestActivityDefinition();
        var activity = new ActivityRegistration<TestActivity, Arguments, Log>(
            new RecordingContainerSelector(activityDefinition));
        int activityExecuteActions = 0;
        int activityCompensateActions = 0;
        int mismatchedActions = 0;
        Action<IRegistrationContext, IExecuteActivityConfigurator<TestActivity, Arguments>> executeAction =
            (_, _) => activityExecuteActions++;
        Action<IRegistrationContext, ICompensateActivityConfigurator<TestActivity, Log>> compensateAction =
            (_, _) => activityCompensateActions++;
        Action<IRegistrationContext, IExecuteActivityConfigurator<OtherActivity, Arguments>> mismatchedExecuteAction =
            (_, _) => mismatchedActions++;
        Action<IRegistrationContext, ICompensateActivityConfigurator<OtherActivity, Log>> mismatchedCompensateAction =
            (_, _) => mismatchedActions++;

        activity.AddConfigureAction(executeAction);
        activity.AddConfigureAction(compensateAction);
        activity.AddConfigureAction(mismatchedExecuteAction);
        activity.AddConfigureAction(mismatchedCompensateAction);
        activity.AddConfigureAction<TestActivity, Arguments>(null);
        activity.AddConfigureAction<TestActivity, Log>(null);

        activity.Configure(activityExecuteEndpoint, activityCompensateEndpoint, context);

        Assert.Equal(1, activityDefinition.ExecuteConfigureCalls);
        Assert.Equal(1, activityDefinition.CompensateConfigureCalls);
        Assert.Equal(1, activityExecuteActions);
        Assert.Equal(1, activityCompensateActions);
        Assert.Equal(0, mismatchedActions);
        Assert.False(activityExecute.ConfigureConsumeTopology);
        Assert.False(activityCompensate.ConfigureConsumeTopology);
        Assert.Single(activityExecute.Specifications);
        Assert.Single(activityCompensate.Specifications);
        Assert.True(activity.IncludeInConfigureEndpoints);

        IReceiveEndpointConfigurator executeEndpoint = EndpointProxy("execute-only", out EndpointRecorder executeRecorder);
        var executeDefinition = new TestExecuteActivityDefinition();
        IExecuteActivityRegistration execute = new ExecuteActivityRegistration<ExecuteOnlyActivity, Arguments>(
            new RecordingContainerSelector(executeDefinition));
        int executeActions = 0;
        Action<IRegistrationContext, IExecuteActivityConfigurator<ExecuteOnlyActivity, Arguments>> matchingAction =
            (_, _) => executeActions++;

        execute.AddConfigureAction(matchingAction);
        execute.AddConfigureAction(mismatchedExecuteAction);
        execute.AddConfigureAction<ExecuteOnlyActivity, Arguments>(null);
        execute.Configure(executeEndpoint, context);

        Assert.Equal(1, executeDefinition.ExecuteConfigureCalls);
        Assert.Equal(1, executeActions);
        Assert.Equal(0, mismatchedActions);
        Assert.False(executeRecorder.ConfigureConsumeTopology);
        Assert.Single(executeRecorder.Specifications);
    }

    private static async Task<T[]> ResolveConcurrentlyAsync<T>(Func<T> resolve, RecordingContainerSelector selector)
    {
        const int workerCount = 16;
        selector.DefinitionEntered.Reset();
        selector.ReleaseDefinition.Reset();
        using var ready = new CountdownEvent(workerCount);
        using var start = new ManualResetEventSlim();
        Task<T>[] tasks = Enumerable.Range(0, workerCount).Select(_ => Task.Run(() =>
        {
            ready.Signal();
            start.Wait(TestContext.Current.CancellationToken);
            return resolve();
        }, TestContext.Current.CancellationToken)).ToArray();

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        start.Set();
        Assert.True(selector.DefinitionEntered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        SpinWait.SpinUntil(() => selector.DefinitionCalls > 1, TimeSpan.FromMilliseconds(250));
        selector.ReleaseDefinition.Set();

        return await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private static T Proxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static IReceiveEndpointConfigurator EndpointProxy(string name, out EndpointRecorder recorder)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointRecorder>();
        recorder = (EndpointRecorder)(object)endpoint;
        recorder.InputAddress = new Uri($"loopback://localhost/{name}");
        return endpoint;
    }

    private static void AssertParameter(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

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

    private sealed class OtherActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    private sealed class TestActivityDefinition : ActivityDefinition<TestActivity, Arguments, Log>
    {
        public int CompensateConfigureCalls { get; private set; }

        public int ExecuteConfigureCalls { get; private set; }

        protected override void ConfigureCompensateActivity(IReceiveEndpointConfigurator endpointConfigurator,
            ICompensateActivityConfigurator<TestActivity, Log> compensateActivityConfigurator, IRegistrationContext context)
        {
            CompensateConfigureCalls++;
        }

        protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
            IExecuteActivityConfigurator<TestActivity, Arguments> executeActivityConfigurator, IRegistrationContext context)
        {
            ExecuteConfigureCalls++;
        }
    }

    private sealed class TestExecuteActivityDefinition : ExecuteActivityDefinition<ExecuteOnlyActivity, Arguments>
    {
        public int ExecuteConfigureCalls { get; private set; }

        protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
            IExecuteActivityConfigurator<ExecuteOnlyActivity, Arguments> executeActivityConfigurator, IRegistrationContext context)
        {
            ExecuteConfigureCalls++;
        }
    }

    private sealed class RecordingContainerSelector(
        IDefinition? definition = null,
        object? executeEndpoint = null,
        object? compensateEndpoint = null) : IContainerSelector
    {
        int _compensateEndpointCalls;
        int _definitionCalls;
        int _executeEndpointCalls;

        public int CompensateEndpointCalls => Volatile.Read(ref _compensateEndpointCalls);

        public int DefinitionCalls => Volatile.Read(ref _definitionCalls);

        public System.Collections.Concurrent.ConcurrentQueue<Type> DefinitionRequests { get; } = new();

        public System.Collections.Concurrent.ConcurrentQueue<IServiceProvider> DefinitionProviders { get; } = new();

        public ManualResetEventSlim DefinitionEntered { get; } = new();

        public int ExecuteEndpointCalls => Volatile.Read(ref _executeEndpointCalls);

        public ManualResetEventSlim ReleaseDefinition { get; } = new(initialState: true);

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
            DefinitionRequests.Enqueue(typeof(T));
            DefinitionProviders.Enqueue(provider);
            if (typeof(T) == typeof(IActivityDefinition<TestActivity, Arguments, Log>)
                || typeof(T) == typeof(IExecuteActivityDefinition<ExecuteOnlyActivity, Arguments>))
            {
                Interlocked.Increment(ref _definitionCalls);
                DefinitionEntered.Set();
                ReleaseDefinition.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            }
            return definition as T;
        }

        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
            where T : class
        {
            if (typeof(T) == typeof(IExecuteActivity<Arguments>))
            {
                Interlocked.Increment(ref _executeEndpointCalls);
                return executeEndpoint as IEndpointDefinition<T>;
            }

            if (typeof(T) == typeof(ICompensateActivity<Log>))
            {
                Interlocked.Increment(ref _compensateEndpointCalls);
                return compensateEndpoint as IEndpointDefinition<T>;
            }

            return null;
        }

        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) =>
            throw new NotSupportedException();

        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) =>
            throw new NotSupportedException();
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class EndpointRecorder : DispatchProxy
    {
        public bool ConfigureConsumeTopology { get; private set; } = true;

        public Uri InputAddress { get; set; } = null!;

        public List<IReceiveEndpointSpecification> Specifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_InputAddress":
                    return InputAddress;
                case "set_ConfigureConsumeTopology":
                    ConfigureConsumeTopology = (bool)args![0]!;
                    return null;
                case nameof(IReceiveEndpointConfigurator.AddEndpointSpecification):
                    Specifications.Add((IReceiveEndpointSpecification)args![0]!);
                    return null;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }
}
