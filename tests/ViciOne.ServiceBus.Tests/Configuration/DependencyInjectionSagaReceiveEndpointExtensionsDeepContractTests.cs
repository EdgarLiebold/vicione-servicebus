using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class DependencyInjectionSagaReceiveEndpointExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "di-receive-endpoint-required-guard-order-and-zero-effects")]
    public void PublicOverloads_RejectRequiredInputsInSignatureOrderBeforeAnyCollaboratorEffect()
    {
        var stateMachine = new RegistrationStateMachine();
        IRegistrationContext context = CreateContext(stateMachine, out RecordingServiceProvider services);
        IReceiveEndpointConfigurator endpoint = CreateEndpoint(out RecordingEndpointProxy endpointCalls);
        var callbackCalls = 0;
        Action<ISagaConfigurator<RegistrationSaga>> sagaCallback = _ => callbackCalls++;
        Action<ISagaConfigurator<RegistrationState>> stateMachineCallback = _ => callbackCalls++;

        AssertArgument("configurator", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.Saga<RegistrationSaga>(null!, null!, sagaCallback));
        AssertArgument("context", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.Saga<RegistrationSaga>(endpoint, null!, sagaCallback));

        AssertArgument("configurator", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga<RegistrationState>(
                null!, null!, null!, stateMachineCallback));
        AssertArgument("stateMachine", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga<RegistrationState>(
                endpoint, null!, null!, stateMachineCallback));
        AssertArgument("context", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga(
                endpoint, stateMachine, null!, stateMachineCallback));

        AssertArgument("configurator", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga<RegistrationState>(
                null!, context, stateMachineCallback));
        AssertArgument("context", () =>
            DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga<RegistrationState>(
                endpoint, null!, stateMachineCallback));

        Assert.Equal(0, callbackCalls);
        Assert.Empty(services.Requests);
        Assert.Empty(endpointCalls.Invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "di-receive-endpoint-saga-context-repository-and-callback-identity")]
    public void SagaOverload_PreservesTheExactContextRepositoryAndCallbackArgument()
    {
        var stateMachine = new RegistrationStateMachine();
        IRegistrationContext context = CreateContext(stateMachine, out RecordingServiceProvider services);
        IReceiveEndpointConfigurator endpoint = CreateEndpoint(out RecordingEndpointProxy endpointCalls);
        ISagaConfigurator<RegistrationSaga>? callbackArgument = null;
        var callbackCalls = 0;
        Action<ISagaConfigurator<RegistrationSaga>> callback = configurator =>
        {
            callbackCalls++;
            callbackArgument = configurator;
        };

        DependencyInjectionSagaReceiveEndpointExtensions.Saga(endpoint, context, callback);

        object specification = AssertSingleAddedSpecification(endpointCalls);
        Assert.Equal(1, callbackCalls);
        Assert.Same(specification, callbackArgument);
        AssertRepositoryContext<RegistrationSaga>(specification, "_sagaRepository", context);
        Assert.Empty(services.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "di-explicit-state-machine-context-repository-and-callback-identity")]
    public void ExplicitStateMachineOverload_PreservesTheExactMachineContextRepositoryAndCallbackArgument()
    {
        var stateMachine = new RegistrationStateMachine();
        IRegistrationContext context = CreateContext(stateMachine, out RecordingServiceProvider services);
        IReceiveEndpointConfigurator endpoint = CreateEndpoint(out RecordingEndpointProxy endpointCalls);
        ISagaConfigurator<RegistrationState>? callbackArgument = null;
        var callbackCalls = 0;
        Action<ISagaConfigurator<RegistrationState>> callback = configurator =>
        {
            callbackCalls++;
            callbackArgument = configurator;
        };

        DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga(
            endpoint, stateMachine, context, callback);

        object specification = AssertSingleAddedSpecification(endpointCalls);
        Assert.Equal(1, callbackCalls);
        Assert.Same(specification, callbackArgument);
        AssertStateMachine(specification, stateMachine);
        AssertRepositoryContext<RegistrationState>(specification, "_repository", context);
        Assert.Empty(services.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "di-container-state-machine-single-resolution-and-input-identity")]
    public void ContainerStateMachineOverload_ResolvesOnceAndPreservesTheResolvedMachineContextRepositoryAndCallbackArgument()
    {
        var stateMachine = new RegistrationStateMachine();
        IRegistrationContext context = CreateContext(stateMachine, out RecordingServiceProvider services);
        IReceiveEndpointConfigurator endpoint = CreateEndpoint(out RecordingEndpointProxy endpointCalls);
        ISagaConfigurator<RegistrationState>? callbackArgument = null;
        var callbackCalls = 0;
        Action<ISagaConfigurator<RegistrationState>> callback = configurator =>
        {
            callbackCalls++;
            callbackArgument = configurator;
        };

        DependencyInjectionSagaReceiveEndpointExtensions.StateMachineSaga(endpoint, context, callback);

        object specification = AssertSingleAddedSpecification(endpointCalls);
        Assert.Equal(1, callbackCalls);
        Assert.Same(specification, callbackArgument);
        AssertStateMachine(specification, stateMachine);
        AssertRepositoryContext<RegistrationState>(specification, "_repository", context);
        Assert.Equal([typeof(ISagaStateMachine<RegistrationState>)], services.Requests);
    }

    static IRegistrationContext CreateContext(
        ISagaStateMachine<RegistrationState> stateMachine,
        out RecordingServiceProvider services)
    {
        services = new RecordingServiceProvider(stateMachine);
        var selector = new DependencyInjectionContainerRegistrar(new ServiceCollection());
        return new RegistrationContext(services, selector, new UnexpectedScopedConsumeContext());
    }

    static IReceiveEndpointConfigurator CreateEndpoint(out RecordingEndpointProxy recording)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, RecordingEndpointProxy>();
        recording = (RecordingEndpointProxy)(object)endpoint;
        return endpoint;
    }

    static object AssertSingleAddedSpecification(RecordingEndpointProxy endpoint)
    {
        RecordedInvocation invocation = Assert.Single(endpoint.Invocations);
        Assert.Equal(nameof(IReceiveEndpointConfigurator.AddEndpointSpecification), invocation.Method.Name);
        return Assert.Single(invocation.Arguments)!;
    }

    static void AssertStateMachine(object specification, ISagaStateMachine<RegistrationState> expected)
    {
        object connector = ReadField(specification, "_connector");
        Assert.Same(expected, ReadField(connector, "_stateMachine"));
    }

    static void AssertRepositoryContext<TSaga>(
        object specification,
        string repositoryField,
        IRegistrationContext expectedContext)
        where TSaga : class, ISaga
    {
        var repository = Assert.IsType<DependencyInjectionSagaRepository<TSaga>>(
            ReadField(specification, repositoryField));
        object factory = ReadField(repository, "_repositoryContextFactory");

        Assert.Same(expectedContext, ReadField(factory, "_serviceProvider"));
        Assert.Same(expectedContext, ReadField(factory, "_setter"));
    }

    static object ReadField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The field '{name}' was not found on {target.GetType()}.");
        return field.GetValue(target)
            ?? throw new InvalidOperationException($"The field '{name}' on {target.GetType()} was null.");
    }

    static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    sealed class RegistrationSaga : ISaga, IInitiatedBy<RegistrationMessage>
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context) => Task.CompletedTask;
    }

    sealed record RegistrationMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    sealed class RegistrationState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public IState? CurrentState { get; set; }
    }

    sealed class RegistrationStateMachine : ViciOneServiceBusStateMachine<RegistrationState>
    {
    }

    sealed class RecordingServiceProvider(ISagaStateMachine<RegistrationState> stateMachine) : IServiceProvider
    {
        public List<Type> Requests { get; } = [];

        public object? GetService(Type serviceType)
        {
            Requests.Add(serviceType);
            return serviceType == typeof(ISagaStateMachine<RegistrationState>)
                ? stateMachine
                : null;
        }
    }

    sealed class UnexpectedScopedConsumeContext : ISetScopedConsumeContext
    {
        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            throw new InvalidOperationException("The receive-endpoint extensions must not push a consume context.");
        }
    }

    sealed record RecordedInvocation(MethodInfo Method, object?[] Arguments);

    class RecordingEndpointProxy : DispatchProxy
    {
        public List<RecordedInvocation> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod
                ?? throw new InvalidOperationException("The endpoint proxy received a call without method metadata.");
            Invocations.Add(new RecordedInvocation(method, args?.ToArray() ?? []));

            return method.ReturnType != typeof(void) && method.ReturnType.IsValueType
                ? Activator.CreateInstance(method.ReturnType)
                : null;
        }
    }
}
