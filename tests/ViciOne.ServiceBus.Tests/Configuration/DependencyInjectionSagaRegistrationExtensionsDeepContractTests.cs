using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class DependencyInjectionSagaRegistrationExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "exact-seven-overload-public-surface")]
    public void PublicSurface_ExposesExactlyTheSevenRegistrationOverloads()
    {
        Type extensions = typeof(DependencyInjectionSagaRegistrationExtensions);
        MethodInfo[] methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.True(extensions.IsPublic && extensions.IsAbstract && extensions.IsSealed);
        Assert.Equal(7, methods.Length);
        Assert.All(methods, method =>
        {
            Assert.Equal(nameof(DependencyInjectionSagaRegistrationExtensions.RegisterSaga), method.Name);
            Assert.Equal(typeof(ISagaRegistration), method.ReturnType);
            Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false));
        });

        MethodInfo[] oneArgumentMethods = methods
            .Where(method => method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1)
            .ToArray();
        Assert.Equal(4, oneArgumentMethods.Length);
        foreach (MethodInfo method in oneArgumentMethods)
        {
            Type saga = Assert.Single(method.GetGenericArguments());
            Assert.Equal("T", saga.Name);
            AssertSagaConstraint(saga);
            Assert.Equal("collection", method.GetParameters()[0].Name);
        }

        Assert.Single(oneArgumentMethods, method => HasParameters(method, typeof(IServiceCollection)));
        Assert.Single(oneArgumentMethods, method => HasParameters(method, typeof(IServiceCollection), typeof(IContainerRegistrar)));
        MethodInfo requiredDefinition = Assert.Single(
            oneArgumentMethods,
            method => HasParameters(method, typeof(IServiceCollection), typeof(Type)));
        Assert.False(requiredDefinition.GetParameters()[1].IsOptional);
        MethodInfo optionalDefinition = Assert.Single(
            oneArgumentMethods,
            method => HasParameters(method, typeof(IServiceCollection), typeof(IContainerRegistrar), typeof(Type)));
        Assert.False(optionalDefinition.GetParameters()[2].IsOptional);

        MethodInfo[] twoArgumentMethods = methods
            .Where(method => method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2)
            .ToArray();
        Assert.Equal(2, twoArgumentMethods.Length);
        foreach (MethodInfo method in twoArgumentMethods)
        {
            Type[] arguments = method.GetGenericArguments();
            Assert.Equal(["T", "TDefinition"], arguments.Select(argument => argument.Name));
            AssertSagaConstraint(arguments[0]);
            AssertReferenceConstraint(arguments[1], typeof(ISagaDefinition<>).MakeGenericType(arguments[0]));
            Assert.Equal("collection", method.GetParameters()[0].Name);
        }

        Assert.Single(twoArgumentMethods, method => HasParameters(method, typeof(IServiceCollection)));
        Assert.Single(twoArgumentMethods, method => HasParameters(method, typeof(IServiceCollection), typeof(IContainerRegistrar)));

        MethodInfo runtime = Assert.Single(methods, method => !method.IsGenericMethod);
        Assert.Equal(
            ["collection", "registrar", "sagaType", "sagaDefinitionType"],
            runtime.GetParameters().Select(parameter => parameter.Name));
        Assert.True(HasParameters(runtime, typeof(IServiceCollection), typeof(IContainerRegistrar), typeof(Type), typeof(Type)));
        Assert.False(runtime.GetParameters()[2].IsOptional);
        Assert.True(runtime.GetParameters()[3].IsOptional);
        Assert.Null(runtime.GetParameters()[3].DefaultValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "receiver-registrar-runtime-type-null-guard-order")]
    public void RequiredInputs_UseReceiverRegistrarThenRuntimeTypeCausalOrderWithoutMutation()
    {
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga<ContractSaga>(null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga<ContractSaga>(null!, (IContainerRegistrar)null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga<ContractSaga, ContractSagaDefinition>(null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga<ContractSaga, ContractSagaDefinition>(
                null!, (IContainerRegistrar)null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga<ContractSaga>(null!, (Type)null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga<ContractSaga>(
                null!, (IContainerRegistrar)null!, sagaDefinitionType: null)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionSagaRegistrationExtensions.RegisterSaga(
                null!, null!, null!, sagaDefinitionType: null)).ParamName);

        var services = new ServiceCollection();
        services.AddSingleton(new MutationSentinel());
        ServiceDescriptor[] baseline = services.ToArray();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterSaga<ContractSaga>((IContainerRegistrar)null!)).ParamName);
        AssertServicesUnchanged(services, baseline);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterSaga<ContractSaga, ContractSagaDefinition>((IContainerRegistrar)null!)).ParamName);
        AssertServicesUnchanged(services, baseline);
        Assert.Equal("sagaDefinitionType", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterSaga<ContractSaga>((Type)null!)).ParamName);
        AssertServicesUnchanged(services, baseline);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterSaga<ContractSaga>((IContainerRegistrar)null!, sagaDefinitionType: null)).ParamName);
        AssertServicesUnchanged(services, baseline);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterSaga(null!, null!, sagaDefinitionType: null)).ParamName);
        AssertServicesUnchanged(services, baseline);
        Assert.Equal("sagaType", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterSaga(registrar, null!, sagaDefinitionType: null)).ParamName);
        AssertServicesUnchanged(services, baseline);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "typed-saga-concrete-and-family-validation-before-mutation")]
    public void TypedSagaTypes_MustBeConcreteAndRejectStateMachineFamilyBeforeMutation()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MutationSentinel());
        ServiceDescriptor[] baseline = services.ToArray();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        AssertConcreteSagaArgument(typeof(AbstractSaga), "T", () => services.RegisterSaga<AbstractSaga>(registrar));
        AssertServicesUnchanged(services, baseline);
        AssertConcreteSagaArgument(typeof(IContractSaga), "T", () => services.RegisterSaga<IContractSaga>(registrar));
        AssertServicesUnchanged(services, baseline);
        AssertConcreteSagaArgument(typeof(AbstractSaga), "T", () =>
            services.RegisterSaga<AbstractSaga, AbstractSagaDefinition>(registrar));
        AssertServicesUnchanged(services, baseline);

        AssertStateMachineArgument(typeof(StateMachineSaga), "T", () => services.RegisterSaga<StateMachineSaga>(registrar));
        AssertServicesUnchanged(services, baseline);
        AssertStateMachineArgument(typeof(StateMachineSaga), "T", () =>
            services.RegisterSaga<StateMachineSaga, AbstractStateMachineDefinition>(registrar));
        AssertServicesUnchanged(services, baseline);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "runtime-saga-invalid-shape-matrix-before-definition-and-mutation")]
    public void RuntimeSagaType_InvalidShapeMatrixUsesStableDiagnosticsBeforeMutation()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MutationSentinel());
        ServiceDescriptor[] baseline = services.ToArray();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        foreach (Type invalidType in new[]
                 {
                     typeof(string),
                     typeof(ISaga),
                     typeof(AbstractSaga),
                     typeof(OpenSaga<>),
                     typeof(ValueSaga),
                     typeof(int).MakeByRefType(),
                     typeof(int).MakePointerType(),
                 })
        {
            AssertConcreteSagaArgument(invalidType, "sagaType", () =>
                services.RegisterSaga(registrar, invalidType, typeof(OtherSagaDefinition)));
            AssertServicesUnchanged(services, baseline);
        }

        AssertStateMachineArgument(typeof(StateMachineSaga), "sagaType", () =>
            services.RegisterSaga(registrar, typeof(StateMachineSaga), typeof(OtherSagaDefinition)));
        AssertServicesUnchanged(services, baseline);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "definition-closed-concrete-exact-match-failure-atomicity")]
    public void DefinitionTypes_MustBeClosedConcreteAndExactlyMatchedBeforeMutation()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MutationSentinel());
        ServiceDescriptor[] baseline = services.ToArray();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        AssertDefinitionArgument(typeof(ISagaDefinition<ContractSaga>), typeof(ContractSaga), "TDefinition", () =>
            services.RegisterSaga<ContractSaga, ISagaDefinition<ContractSaga>>(registrar));
        AssertServicesUnchanged(services, baseline);
        AssertDefinitionArgument(typeof(AbstractContractSagaDefinition), typeof(ContractSaga), "TDefinition", () =>
            services.RegisterSaga<ContractSaga, AbstractContractSagaDefinition>(registrar));
        AssertServicesUnchanged(services, baseline);

        foreach (Type invalidDefinition in new[]
                 {
                     typeof(string),
                     typeof(ISagaDefinition<ContractSaga>),
                     typeof(AbstractContractSagaDefinition),
                     typeof(OpenSagaDefinition<>),
                     typeof(OtherSagaDefinition),
                 })
        {
            AssertDefinitionArgument(invalidDefinition, typeof(ContractSaga), "sagaDefinitionType", () =>
                services.RegisterSaga<ContractSaga>(registrar, invalidDefinition));
            AssertServicesUnchanged(services, baseline);

            AssertDefinitionArgument(invalidDefinition, typeof(ContractSaga), "sagaDefinitionType", () =>
                services.RegisterSaga(registrar, typeof(ContractSaga), invalidDefinition));
            AssertServicesUnchanged(services, baseline);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "registrar-failure-does-not-partially-mutate-services")]
    public void RegistrarFailure_DoesNotLeavePartialSagaOrDefinitionDescriptors()
    {
        IContainerRegistrar registrar = DispatchProxy.Create<IContainerRegistrar, ThrowingRegistrarProxy>();

        var conventionServices = new ServiceCollection();
        conventionServices.AddSingleton(new MutationSentinel());
        ServiceDescriptor[] conventionBaseline = conventionServices.ToArray();

        Assert.Throws<RegistrarFailureException>(() => conventionServices.RegisterSaga<ContractSaga>(registrar));
        AssertServicesUnchanged(conventionServices, conventionBaseline);

        var definitionServices = new ServiceCollection();
        definitionServices.AddSingleton(new MutationSentinel());
        ServiceDescriptor[] definitionBaseline = definitionServices.ToArray();

        Assert.Throws<RegistrarFailureException>(() =>
            definitionServices.RegisterSaga<ContractSaga>(registrar, typeof(ContractSagaDefinition)));
        AssertServicesUnchanged(definitionServices, definitionBaseline);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "canonical-instance-registration-singleton-lifetime-and-kind-deduplication")]
    public void ConventionRegistration_IsCanonicalInstanceBackedSingletonAndAddsOneSagaConsumerKind()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        ISagaRegistration first = services.RegisterSaga<ContractSaga>();
        ISagaRegistration second = services.RegisterSaga<ContractSaga>(registrar);
        ISagaRegistration runtime = services.RegisterSaga(registrar, typeof(ContractSaga));

        Assert.Same(first, second);
        Assert.Same(first, runtime);
        Assert.Equal(typeof(ContractSaga), first.Type);
        Assert.Null(first.StateMachineType);
        Assert.Same(first, Assert.Single(registrar.GetRegistrations<ISagaRegistration>()));

        ServiceDescriptor registrationDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(ISagaRegistration));
        Assert.Equal(ServiceLifetime.Singleton, registrationDescriptor.Lifetime);
        Assert.Same(first, registrationDescriptor.ImplementationInstance);

        ServiceDescriptor kindDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IConsumerKind));
        Assert.Equal(ServiceLifetime.Singleton, kindDescriptor.Lifetime);
        Assert.Equal("SagaConsumerKind", kindDescriptor.ImplementationType?.Name);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ContractSaga));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        Assert.Same(first, provider.GetRequiredService<ISagaRegistration>());
        Assert.Equal("SagaConsumerKind", Assert.Single(provider.GetServices<IConsumerKind>()).GetType().Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "definition-and-registration-singleton-alias-identity")]
    public void DefinitionRegistration_ReturnsCanonicalSagaRegistrationAndOneDefinitionSingleton()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        ISagaRegistration convention = services.RegisterSaga<ContractSaga>(registrar);
        ISagaRegistration defined = services.RegisterSaga<ContractSaga, ContractSagaDefinition>(registrar);

        Assert.Same(convention, defined);
        Assert.Equal(ServiceLifetime.Singleton, Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(ContractSagaDefinition)).Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(ISagaDefinition<ContractSaga>)).Lifetime);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ContractSagaDefinition implementation = provider.GetRequiredService<ContractSagaDefinition>();
        Assert.Same(implementation, provider.GetRequiredService<ISagaDefinition<ContractSaga>>());
        Assert.Same(implementation, registrar.GetDefinition<ISagaDefinition<ContractSaga>>(provider));
        Assert.Same(convention, provider.GetRequiredService<ISagaRegistration>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "runtime-activator-paths-and-null-definition-semantics")]
    public void RuntimeOverloads_PreserveConcreteTypesAndOptionalDefinitionSemantics()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        ISagaRegistration naturalDefinition = services.RegisterSaga<ContractSaga, ContractSagaDefinition>();
        ISagaRegistration genericDefinition = services.RegisterSaga<ContractSaga>(registrar, typeof(ContractSagaDefinition));
        ISagaRegistration naturalRuntimeDefinition = services.RegisterSaga<OtherSaga>(typeof(OtherSagaDefinition));
        ISagaRegistration runtimeDefinition = services.RegisterSaga(registrar, typeof(OtherSaga), typeof(OtherSagaDefinition));
        ISagaRegistration runtimeConvention = services.RegisterSaga(registrar, typeof(ConventionSaga));
        ISagaRegistration genericNullDefinition = services.RegisterSaga<ConventionSaga>(registrar, sagaDefinitionType: null);

        Assert.Same(naturalDefinition, genericDefinition);
        Assert.Same(naturalRuntimeDefinition, runtimeDefinition);
        Assert.Equal(typeof(ContractSaga), genericDefinition.Type);
        Assert.Equal(typeof(OtherSaga), runtimeDefinition.Type);
        Assert.Equal(typeof(ConventionSaga), runtimeConvention.Type);
        Assert.Same(runtimeConvention, genericNullDefinition);
        Assert.All(new[] { genericDefinition, runtimeDefinition, runtimeConvention }, registration =>
            Assert.Null(registration.StateMachineType));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        Assert.Same(provider.GetRequiredService<ContractSagaDefinition>(),
            provider.GetRequiredService<ISagaDefinition<ContractSaga>>());
        Assert.Same(provider.GetRequiredService<OtherSagaDefinition>(),
            provider.GetRequiredService<ISagaDefinition<OtherSaga>>());
        Assert.Null(provider.GetService<ISagaDefinition<ConventionSaga>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "owner-qualified-registration-and-definition-singleton-isolation")]
    public void OwnerQualifiedRegistrar_IsolatesRegistrationAndDefinitionWithSingletonIdentity()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar<TestBus>(services);

        ISagaRegistration registration = services.RegisterSaga<ContractSaga, ContractSagaDefinition>(registrar);

        Assert.Same(registration, Assert.Single(registrar.GetRegistrations<ISagaRegistration>()));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISagaRegistration));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ContractSagaDefinition));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISagaDefinition<ContractSaga>));
        ServiceDescriptor[] ownedDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(Bind<TestBus, ISagaRegistration>)
                || descriptor.ServiceType == typeof(Bind<TestBus, ContractSagaDefinition>)
                || descriptor.ServiceType == typeof(Bind<TestBus, ISagaDefinition<ContractSaga>>))
            .ToArray();
        Assert.Equal(3, ownedDescriptors.Length);
        Assert.All(
            ownedDescriptors,
            descriptor => Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        Assert.Same(registration, provider.GetRequiredService<Bind<TestBus, ISagaRegistration>>().Value);
        ContractSagaDefinition implementation = provider.GetRequiredService<Bind<TestBus, ContractSagaDefinition>>().Value;
        Assert.Same(implementation, provider.GetRequiredService<Bind<TestBus, ISagaDefinition<ContractSaga>>>().Value);
        Assert.Same(implementation, registrar.GetDefinition<ISagaDefinition<ContractSaga>>(provider));
        Assert.Null(provider.GetService<ISagaRegistration>());
        Assert.Null(provider.GetService<ContractSagaDefinition>());
        Assert.Null(provider.GetService<ISagaDefinition<ContractSaga>>());
    }

    private static bool HasParameters(MethodInfo method, params Type[] types) =>
        method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(types);

    private static void AssertSagaConstraint(Type parameter) => AssertReferenceConstraint(parameter, typeof(ISaga));

    private static void AssertReferenceConstraint(Type parameter, Type contract)
    {
        Assert.True((parameter.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
        Assert.Equal([contract], parameter.GetGenericParameterConstraints());
    }

    private static void AssertConcreteSagaArgument(Type sagaType, string parameterName, Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        string message = $"{TypeCache.GetShortName(sagaType)} is not a concrete, closed saga implementation";

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Equal(new ArgumentException(message, parameterName).Message, exception.Message);
    }

    private static void AssertStateMachineArgument(Type sagaType, string parameterName, Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        string message =
            $"State machine sagas must be registered using RegisterSagaStateMachine: {TypeCache.GetShortName(sagaType)}";

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Equal(new ArgumentException(message, parameterName).Message, exception.Message);
    }

    private static void AssertDefinitionArgument(Type definitionType, Type sagaType, string parameterName, Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        string message =
            $"{TypeCache.GetShortName(definitionType)} is not a concrete, closed saga definition of {TypeCache.GetShortName(sagaType)}";

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Equal(new ArgumentException(message, parameterName).Message, exception.Message);
    }

    private static void AssertServicesUnchanged(IServiceCollection services, ServiceDescriptor[] baseline) =>
        Assert.Equal(baseline, services.ToArray());

    private sealed class MutationSentinel;

    public class ThrowingRegistrarProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new RegistrarFailureException();
        }
    }

    private sealed class RegistrarFailureException : Exception;

    private interface IContractSaga : ISaga;

    private abstract class AbstractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class AbstractSagaDefinition : SagaDefinition<AbstractSaga>;

    private sealed class OpenSaga<T> : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private struct ValueSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class StateMachineSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private abstract class AbstractStateMachineDefinition : SagaDefinition<StateMachineSaga>;

    public sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ContractSagaDefinition : SagaDefinition<ContractSaga>;

    private abstract class AbstractContractSagaDefinition : SagaDefinition<ContractSaga>;

    private sealed class OpenSagaDefinition<TSaga> : SagaDefinition<TSaga>
        where TSaga : class, ISaga;

    public sealed class OtherSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class OtherSagaDefinition : SagaDefinition<OtherSaga>;

    public sealed class ConventionSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private interface TestBus : IBus;
}
