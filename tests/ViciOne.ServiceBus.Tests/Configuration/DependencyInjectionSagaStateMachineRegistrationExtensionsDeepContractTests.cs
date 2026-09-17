using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class DependencyInjectionSagaStateMachineRegistrationExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "exact-public-extension-surface")]
    public void PublicSurface_ExposesExactlyTheSevenRegistrationOverloads()
    {
        Type extensions = typeof(DependencyInjectionSagaStateMachineRegistrationExtensions);

        Assert.True(extensions.IsPublic);
        Assert.True(extensions.IsAbstract);
        Assert.True(extensions.IsSealed);

        MethodInfo[] methods = extensions
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ThenBy(method => method.GetGenericArguments().Length)
            .ThenBy(method => method.GetParameters().Length)
            .ToArray();

        Assert.Equal(7, methods.Length);
        Assert.Equal(2, methods.Count(method => method.Name == nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga)));
        Assert.Equal(5, methods.Count(method => method.Name == nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine)));
        Assert.All(methods, method =>
        {
            Assert.Equal(typeof(ISagaRegistration), method.ReturnType);
            Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false));
        });

        AssertParameterNames(
            AssertMethod(methods, nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga), 3,
                typeof(IServiceCollection)),
            "collection");
        AssertParameterNames(
            AssertMethod(methods, nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga), 3,
                typeof(IServiceCollection), typeof(IContainerRegistrar)),
            "collection", "registrar");
        AssertParameterNames(
            AssertMethod(methods, nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine), 2,
                typeof(IServiceCollection)),
            "collection");
        AssertParameterNames(
            AssertMethod(methods, nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine), 2,
                typeof(IServiceCollection), typeof(IContainerRegistrar)),
            "collection", "registrar");
        AssertParameterNames(
            AssertMethod(methods, nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine), 2,
                typeof(IServiceCollection), typeof(Type)),
            "collection", "sagaDefinitionType");
        AssertParameterNames(
            AssertMethod(methods, nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine), 2,
                typeof(IServiceCollection), typeof(IContainerRegistrar), typeof(Type)),
            "collection", "registrar", "sagaDefinitionType");

        MethodInfo runtimeMethod = AssertMethod(
            methods,
            nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine),
            0,
            typeof(IServiceCollection),
            typeof(IContainerRegistrar),
            typeof(Type),
            typeof(Type));
        ParameterInfo[] runtimeParameters = runtimeMethod.GetParameters();
        Assert.Equal(["collection", "registrar", "sagaType", "sagaDefinitionType"], runtimeParameters.Select(parameter => parameter.Name));
        Assert.False(runtimeParameters[0].IsOptional);
        Assert.False(runtimeParameters[1].IsOptional);
        Assert.False(runtimeParameters[2].IsOptional);
        Assert.True(runtimeParameters[3].IsOptional);
        Assert.Null(runtimeParameters[3].DefaultValue);

        foreach (MethodInfo method in methods.Where(method => method.IsGenericMethodDefinition))
            AssertGenericContract(method);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "causal-required-argument-guard-order")]
    public void RequiredArguments_AreRejectedInCausalOrderBeforeAnyRegistrationEffect()
    {
        IServiceCollection missingCollection = null!;

        AssertArgumentNull("collection", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                missingCollection));
        AssertArgumentNull("collection", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga<ContractStateMachine, ContractState, ContractStateDefinition>(
                missingCollection));
        AssertArgumentNull("collection", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                missingCollection,
                (Type)null!));

        var services = new ServiceCollection();
        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(services);
        ServiceDescriptor[] baseline = services.ToArray();

        AssertArgumentNull("registrar", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                services,
                (IContainerRegistrar)null!));
        AssertUnchanged(baseline, services);

        AssertArgumentNull("registrar", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga<ContractStateMachine, ContractState, ContractStateDefinition>(
                services,
                null!));
        AssertUnchanged(baseline, services);

        AssertArgumentNull("sagaDefinitionType", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                services,
                (Type)null!));
        AssertUnchanged(baseline, services);

        AssertArgumentNull("collection", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                missingCollection,
                null!,
                null!,
                typeof(ContractStateDefinition)));
        AssertArgumentNull("registrar", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                services,
                null!,
                null!,
                typeof(ContractStateDefinition)));
        AssertUnchanged(baseline, services);
        AssertArgumentNull("sagaType", () =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                services,
                registrar,
                null!,
                typeof(ContractStateDefinition)));
        AssertUnchanged(baseline, services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "generic-admissibility-and-failure-atomicity")]
    public void GenericRegistration_RejectsAbstractImplementationsBeforeMutatingTheCollection()
    {
        var services = new ServiceCollection();
        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(services);
        ServiceDescriptor[] baseline = services.ToArray();

        ArgumentException machineException = Assert.Throws<ArgumentException>(() =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<AbstractStateMachine, ContractState>(
                services,
                registrar));
        Assert.Equal("T", machineException.ParamName);
        Assert.Equal(
            new ArgumentException(
                $"{TypeCache<AbstractStateMachine>.ShortName} is not a concrete, closed saga state machine implementation",
                "T").Message,
            machineException.Message);
        AssertUnchanged(baseline, services);

        ArgumentException definitionException = Assert.Throws<ArgumentException>(() =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga<ContractStateMachine, ContractState, AbstractStateDefinition>(
                services,
                registrar));
        Assert.Equal("TDefinition", definitionException.ParamName);
        Assert.Equal(
            new ArgumentException(
                $"{TypeCache<AbstractStateDefinition>.ShortName} is not a concrete, closed saga definition implementation",
                "TDefinition").Message,
            definitionException.Message);
        AssertUnchanged(baseline, services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "runtime-machine-admissibility-and-failure-atomicity")]
    public void RuntimeRegistration_RejectsEveryInvalidMachineShapeWithoutEffects()
    {
        var services = new ServiceCollection();
        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(services);
        ServiceDescriptor[] baseline = services.ToArray();

        foreach (Type invalidType in new[]
                 {
                     typeof(string),
                     typeof(ISagaStateMachine<ContractState>),
                     typeof(AbstractStateMachine),
                     typeof(OpenStateMachine<>),
                     typeof(AmbiguousStateMachine),
                 })
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                    services,
                    registrar,
                    invalidType,
                    typeof(ContractStateDefinition)));

            Assert.Equal("sagaType", exception.ParamName);
            Assert.Equal(
                new ArgumentException(
                    $"{TypeCache.GetShortName(invalidType)} is not a concrete, closed saga state machine with exactly one saga state contract",
                    "sagaType").Message,
                exception.Message);
            AssertUnchanged(baseline, services);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "runtime-definition-admissibility-and-failure-atomicity")]
    public void RuntimeRegistration_RejectsEveryInvalidDefinitionShapeWithoutEffects()
    {
        var services = new ServiceCollection();
        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(services);
        ServiceDescriptor[] baseline = services.ToArray();

        foreach (Type invalidType in new[]
                 {
                     typeof(string),
                     typeof(ISagaDefinition<ContractState>),
                     typeof(AbstractStateDefinition),
                     typeof(OpenStateDefinition<>),
                     typeof(OtherStateDefinition),
                     typeof(AmbiguousStateDefinition),
                 })
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                    services,
                    registrar,
                    typeof(ContractStateMachine),
                    invalidType));

            Assert.Equal("sagaDefinitionType", exception.ParamName);
            Assert.Equal(
                new ArgumentException(
                    $"{TypeCache.GetShortName(invalidType)} is not a concrete, closed saga definition of {TypeCache<ContractState>.ShortName}",
                    "sagaDefinitionType").Message,
                exception.Message);
            AssertUnchanged(baseline, services);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "registrar-failure-does-not-partially-mutate-services")]
    public void RegistrarFailure_DoesNotLeavePartialMachineOrDefinitionDescriptors()
    {
        IContainerRegistrar registrar = DispatchProxy.Create<IContainerRegistrar, ThrowingRegistrarProxy>();

        var machineServices = new ServiceCollection();
        machineServices.AddSingleton(new RegistrationSentinel());
        ServiceDescriptor[] machineBaseline = machineServices.ToArray();

        Assert.Throws<RegistrarFailureException>(() =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                machineServices,
                registrar));
        AssertUnchanged(machineBaseline, machineServices);

        var definitionServices = new ServiceCollection();
        definitionServices.AddSingleton(new RegistrationSentinel());
        ServiceDescriptor[] definitionBaseline = definitionServices.ToArray();

        Assert.Throws<RegistrarFailureException>(() =>
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                definitionServices,
                registrar,
                typeof(ContractStateDefinition)));
        AssertUnchanged(definitionBaseline, definitionServices);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "canonical-registration-and-singleton-service-identity")]
    public void RepeatedRegistration_PreservesTheCanonicalRegistrationAndSingletonMachineIdentity()
    {
        var services = new ServiceCollection();
        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(services);

        ISagaRegistration first =
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                services);
        ISagaRegistration second =
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                services,
                registrar,
                sagaDefinitionType: null);

        Assert.Same(first, second);
        Assert.Equal(typeof(ContractState), first.Type);
        Assert.Equal(typeof(ContractStateMachine), first.StateMachineType);

        AssertSingletonDescriptor<ContractStateMachine>(services);
        AssertSingletonDescriptor<ISagaStateMachine<ContractState>>(services);
        ServiceDescriptor registrationDescriptor = AssertSingletonDescriptor<ISagaRegistration>(services);
        Assert.Same(first, registrationDescriptor.ImplementationInstance);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IConsumerKind));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        ContractStateMachine machine = provider.GetRequiredService<ContractStateMachine>();
        Assert.Same(machine, provider.GetRequiredService<ISagaStateMachine<ContractState>>());
        Assert.Same(first, provider.GetRequiredService<ISagaRegistration>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "runtime-activator-path-and-definition-singleton-identity")]
    public void RuntimeRegistration_ActivatesOnlyAdmissibleRegistrarsAndPreservesDefinitionIdentity()
    {
        var typedServices = new ServiceCollection();
        ISagaRegistration typedRegistration =
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga<ContractStateMachine, ContractState, ContractStateDefinition>(
                typedServices);
        Assert.Equal(typeof(ContractState), typedRegistration.Type);
        AssertSingletonDescriptor<ContractStateDefinition>(typedServices);

        var genericRuntimeServices = new ServiceCollection();
        ISagaRegistration genericRuntimeRegistration =
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine<ContractStateMachine, ContractState>(
                genericRuntimeServices,
                typeof(ContractStateDefinition));
        Assert.Equal(typeof(ContractStateMachine), genericRuntimeRegistration.StateMachineType);
        AssertSingletonDescriptor<ContractStateDefinition>(genericRuntimeServices);

        var services = new ServiceCollection();
        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(services);

        ISagaRegistration registration =
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                services,
                registrar,
                typeof(ContractStateMachine),
                typeof(ContractStateDefinition));

        Assert.Equal(typeof(ContractState), registration.Type);
        Assert.Equal(typeof(ContractStateMachine), registration.StateMachineType);
        AssertSingletonDescriptor<ContractStateMachine>(services);
        AssertSingletonDescriptor<ISagaStateMachine<ContractState>>(services);
        AssertSingletonDescriptor<ContractStateDefinition>(services);
        AssertSingletonDescriptor<ISagaDefinition<ContractState>>(services);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        ContractStateMachine machine = provider.GetRequiredService<ContractStateMachine>();
        Assert.Same(machine, provider.GetRequiredService<ISagaStateMachine<ContractState>>());
        ContractStateDefinition definition = provider.GetRequiredService<ContractStateDefinition>();
        Assert.Same(definition, provider.GetRequiredService<ISagaDefinition<ContractState>>());
        Assert.Same(registration, provider.GetRequiredService<ISagaRegistration>());

        var conventionServices = new ServiceCollection();
        IContainerRegistrar conventionRegistrar = new DependencyInjectionContainerRegistrar(conventionServices);
        ISagaRegistration conventionRegistration =
            DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSagaStateMachine(
                conventionServices,
                conventionRegistrar,
                typeof(ContractStateMachine));
        Assert.Equal(typeof(ContractState), conventionRegistration.Type);
        Assert.DoesNotContain(
            conventionServices,
            descriptor => descriptor.ServiceType == typeof(ISagaDefinition<ContractState>));
    }

    private static MethodInfo AssertMethod(
        IEnumerable<MethodInfo> methods,
        string name,
        int genericArgumentCount,
        params Type[] parameterTypes)
    {
        return Assert.Single(
            methods,
            method => method.Name == name
                && method.GetGenericArguments().Length == genericArgumentCount
                && method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
    }

    private static void AssertArgumentNull(string parameterName, Action action)
    {
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);
    }

    private static void AssertParameterNames(MethodInfo method, params string[] names)
    {
        Assert.Equal(names, method.GetParameters().Select(parameter => parameter.Name));
        Assert.All(method.GetParameters(), parameter => Assert.False(parameter.IsOptional));
    }

    private static void AssertGenericContract(MethodInfo method)
    {
        Type[] arguments = method.GetGenericArguments();
        string[] expectedArguments = method.Name == nameof(DependencyInjectionSagaStateMachineRegistrationExtensions.RegisterSaga)
            ? ["T", "TSaga", "TDefinition"]
            : ["T", "TSaga"];
        Assert.Equal(expectedArguments, arguments.Select(argument => argument.Name));

        AssertReferenceTypeConstraint(arguments[0]);
        Assert.Equal([typeof(ISagaStateMachine<>).MakeGenericType(arguments[1])], arguments[0].GetGenericParameterConstraints());
        AssertReferenceTypeConstraint(arguments[1]);
        Assert.Equal([typeof(ISagaStateMachineInstance)], arguments[1].GetGenericParameterConstraints());

        if (arguments.Length == 3)
        {
            AssertReferenceTypeConstraint(arguments[2]);
            Assert.Equal([typeof(ISagaDefinition<>).MakeGenericType(arguments[1])], arguments[2].GetGenericParameterConstraints());
        }
    }

    private static void AssertReferenceTypeConstraint(Type argument)
    {
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            argument.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
    }

    private static void AssertUnchanged(IReadOnlyList<ServiceDescriptor> baseline, IServiceCollection services)
    {
        Assert.Equal(baseline.Count, services.Count);
        for (var index = 0; index < baseline.Count; index++)
            Assert.Same(baseline[index], services[index]);
    }

    private static ServiceDescriptor AssertSingletonDescriptor<TService>(IServiceCollection services)
    {
        ServiceDescriptor descriptor = Assert.Single(services, candidate => candidate.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        return descriptor;
    }

    public class ThrowingRegistrarProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new RegistrarFailureException();
        }
    }

    private sealed class RegistrarFailureException : Exception;

    private sealed class RegistrationSentinel;

    public sealed class ContractState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class OtherState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ContractStateMachine : ViciOneServiceBusStateMachine<ContractState>
    {
    }

    private abstract class AbstractStateMachine : ViciOneServiceBusStateMachine<ContractState>
    {
    }

    private sealed class OpenStateMachine<TState> : ViciOneServiceBusStateMachine<TState>
        where TState : class, ISagaStateMachineInstance
    {
    }

    private sealed class AmbiguousStateMachine :
        ViciOneServiceBusStateMachine<ContractState>,
        ISagaStateMachine<OtherState>
    {
        IEnumerable<IEventCorrelation> ISagaStateMachine<OtherState>.Correlations => throw new NotSupportedException();

        IStateAccessor<OtherState> IStateMachine<OtherState>.Accessor => throw new NotSupportedException();

        Task<bool> ISagaStateMachine<OtherState>.IsCompletedAsync(
            IBehaviorContext<OtherState> context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        IState<OtherState> IStateMachine<OtherState>.GetState(string name) => throw new NotSupportedException();

        Task IStateMachine<OtherState>.RaiseEventAsync(
            IBehaviorContext<OtherState> context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        Task IStateMachine<OtherState>.RaiseEventAsync<TMessage>(
            IBehaviorContext<OtherState, TMessage> context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        IDisposable IStateMachine<OtherState>.ConnectEventObserver(IEventObserver<OtherState> observer) =>
            throw new NotSupportedException();

        IDisposable IStateMachine<OtherState>.ConnectEventObserver(
            IEvent @event,
            IEventObserver<OtherState> observer) =>
            throw new NotSupportedException();

        IDisposable IStateMachine<OtherState>.ConnectStateObserver(IStateObserver<OtherState> observer) =>
            throw new NotSupportedException();
    }

    public sealed class ContractStateDefinition : SagaDefinition<ContractState>
    {
    }

    private abstract class AbstractStateDefinition : SagaDefinition<ContractState>
    {
    }

    private sealed class OpenStateDefinition<TState> : SagaDefinition<TState>
        where TState : class, ISagaStateMachineInstance
    {
    }

    private sealed class OtherStateDefinition : SagaDefinition<OtherState>
    {
    }

    private sealed class AmbiguousStateDefinition :
        SagaDefinition<ContractState>,
        ISagaDefinition<OtherState>
    {
        IEndpointDefinition<OtherState> ISagaDefinition<OtherState>.EndpointDefinition
        {
            set { }
        }

        void ISagaDefinition<OtherState>.Configure(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<OtherState> sagaConfigurator,
            IRegistrationContext context)
        {
            throw new NotSupportedException();
        }
    }
}
