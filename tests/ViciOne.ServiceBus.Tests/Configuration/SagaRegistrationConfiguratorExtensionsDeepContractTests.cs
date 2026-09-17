using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaRegistrationConfiguratorExtensionsDeepContractTests
{
    private const string DefinitionMessage =
        "The saga definition type must be a closed, non-abstract class compatible with the registered saga type.";

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "generic-extensions-exact-six-method-public-surface")]
    public void PublicSurface_ExposesExactlyTheSixGenericRegistrationMethods()
    {
        Type extensions = typeof(SagaRegistrationConfiguratorExtensions);
        MethodInfo[] methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.True(extensions.IsPublic && extensions.IsAbstract && extensions.IsSealed);
        Assert.Equal(6, methods.Length);
        Assert.Equal(2, methods.Count(method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.AddSaga)));
        Assert.Equal(2, methods.Count(method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.AddSagaStateMachine)));
        Assert.Single(methods, method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.AddSagaRepository));
        Assert.Single(methods, method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.SetSagaRepositoryProvider));
        Assert.All(methods, method => Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false)));

        foreach (MethodInfo method in methods.Where(method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.AddSaga)))
            AssertSagaShape(method);
        foreach (MethodInfo method in methods.Where(method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.AddSagaStateMachine)))
            AssertStateMachineShape(method);

        MethodInfo repository = Assert.Single(methods, method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.AddSagaRepository));
        Type repositorySaga = Assert.Single(repository.GetGenericArguments());
        AssertReferenceConstraint(repositorySaga, typeof(ISaga));
        Assert.Equal([typeof(IRegistrationConfigurator)], repository.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(typeof(ISagaRegistrationConfigurator<>).MakeGenericType(repositorySaga), repository.ReturnType);

        MethodInfo provider = Assert.Single(methods, method => method.Name == nameof(SagaRegistrationConfiguratorExtensions.SetSagaRepositoryProvider));
        Assert.False(provider.IsGenericMethod);
        Assert.Equal(typeof(void), provider.ReturnType);
        Assert.Equal(
            [typeof(IRegistrationConfigurator), typeof(ISagaRepositoryRegistrationProvider)],
            provider.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "all-six-entry-points-receiver-first-null-boundary")]
    public void AllEntryPoints_RejectMissingReceiverBeforeSecondaryArguments()
    {
        AssertNullConfigurator(() => SagaRegistrationConfiguratorExtensions.AddSaga<ContractSaga>(null!, configure: null));
        AssertNullConfigurator(() => SagaRegistrationConfiguratorExtensions.AddSaga<ContractSaga>(null!, typeof(int), null));
        AssertNullConfigurator(() => SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<ContractStateMachine, ContractState>(null!, configure: null));
        AssertNullConfigurator(() => SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<ContractStateMachine, ContractState>(null!, typeof(int), null));
        AssertNullConfigurator(() => SagaRegistrationConfiguratorExtensions.AddSagaRepository<ContractSaga>(null!));
        AssertNullConfigurator(() => SagaRegistrationConfiguratorExtensions.SetSagaRepositoryProvider(null!, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "add-saga-definition-closed-concrete-compatible-matrix-no-di-effects")]
    public void AddSaga_InvalidDefinitionMatrixUsesStableDiagnosticsBeforeAnyMutation()
    {
        (ServiceCollection services, IRegistrationConfigurator configurator) = CreateCompletedConfiguration();
        int baseline = services.Count;

        foreach (Type invalid in InvalidSagaDefinitions())
        {
            AssertDefinitionArgument(() =>
                SagaRegistrationConfiguratorExtensions.AddSaga<ContractSaga>(configurator, invalid));
            Assert.Equal(baseline, services.Count);
        }

        AssertDefinitionArgument(() =>
            SagaRegistrationConfiguratorExtensions.AddSaga<ContractState>(configurator, typeof(int)));
        Assert.Equal(baseline, services.Count);

        ArgumentException stateMachineFamily = Assert.Throws<ArgumentException>(() =>
            SagaRegistrationConfiguratorExtensions.AddSaga<ContractState>(configurator, typeof(ContractStateDefinition)));
        Assert.Contains("AddSagaStateMachine", stateMachineFamily.Message, StringComparison.Ordinal);
        Assert.Equal(baseline, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "add-state-machine-definition-closed-concrete-compatible-matrix-no-di-effects")]
    public void AddSagaStateMachine_InvalidDefinitionMatrixUsesStableDiagnosticsBeforeAnyMutation()
    {
        (ServiceCollection services, IRegistrationConfigurator configurator) = CreateCompletedConfiguration();
        int baseline = services.Count;

        foreach (Type invalid in InvalidStateDefinitions())
        {
            AssertDefinitionArgument(() =>
                SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<ContractStateMachine, ContractState>(configurator, invalid));
            Assert.Equal(baseline, services.Count);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "add-saga-valid-definition-callback-registration-owner-result-and-null-definition-identity")]
    public void AddSaga_ValidInputsPreserveDefinitionCallbackRegistrationOwnerAndResultIdentity()
    {
        var services = new ServiceCollection();
        IRegistrationConfigurator? owner = null;
        ISagaRegistrationConfigurator<ContractSaga>? definedResult = null;
        ISagaRegistrationConfigurator<ConventionSaga>? conventionResult = null;
        Action<IRegistrationContext, ISagaConfigurator<ContractSaga>> definedCallback = static (_, _) => { };
        Action<IRegistrationContext, ISagaConfigurator<ConventionSaga>> conventionCallback = static (_, _) => { };

        services.AddViciOneServiceBus(configurator =>
        {
            owner = configurator;
            configurator.SetInMemorySagaRepositoryProvider();
            definedResult = SagaRegistrationConfiguratorExtensions.AddSaga<ContractSaga>(
                configurator, typeof(ContractSagaDefinition), definedCallback);
            conventionResult = SagaRegistrationConfiguratorExtensions.AddSaga(configurator, conventionCallback);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        ContractSagaDefinition definition = provider.GetRequiredService<ContractSagaDefinition>();
        Assert.Same(definition, provider.GetRequiredService<ISagaDefinition<ContractSaga>>());
        Assert.Null(provider.GetService<ISagaDefinition<ConventionSaga>>());

        ISagaRegistration definedRegistration = Assert.Single(
            provider.GetServices<ISagaRegistration>(), registration => registration.Type == typeof(ContractSaga));
        ISagaRegistration conventionRegistration = Assert.Single(
            provider.GetServices<ISagaRegistration>(), registration => registration.Type == typeof(ConventionSaga));
        AssertResult(definedResult, owner, definedRegistration);
        AssertResult(conventionResult, owner, conventionRegistration);
        AssertCallback(definedRegistration, definedCallback);
        AssertCallback(conventionRegistration, conventionCallback);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "add-state-machine-valid-definition-callback-registration-owner-result-and-null-definition-identity")]
    public void AddSagaStateMachine_ValidInputsPreserveDefinitionCallbackRegistrationOwnerAndResultIdentity()
    {
        var services = new ServiceCollection();
        IRegistrationConfigurator? owner = null;
        ISagaRegistrationConfigurator<ContractState>? definedResult = null;
        ISagaRegistrationConfigurator<ConventionState>? conventionResult = null;
        Action<IRegistrationContext, ISagaConfigurator<ContractState>> definedCallback = static (_, _) => { };
        Action<IRegistrationContext, ISagaConfigurator<ConventionState>> conventionCallback = static (_, _) => { };

        services.AddViciOneServiceBus(configurator =>
        {
            owner = configurator;
            configurator.SetInMemorySagaRepositoryProvider();
            definedResult = SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<ContractStateMachine, ContractState>(
                configurator, typeof(ContractStateDefinition), definedCallback);
            conventionResult = SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<ConventionStateMachine, ConventionState>(
                configurator, conventionCallback);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        ContractStateDefinition definition = provider.GetRequiredService<ContractStateDefinition>();
        Assert.Same(definition, provider.GetRequiredService<ISagaDefinition<ContractState>>());
        Assert.Null(provider.GetService<ISagaDefinition<ConventionState>>());

        ISagaRegistration definedRegistration = Assert.Single(
            provider.GetServices<ISagaRegistration>(), registration => registration.Type == typeof(ContractState));
        ISagaRegistration conventionRegistration = Assert.Single(
            provider.GetServices<ISagaRegistration>(), registration => registration.Type == typeof(ConventionState));
        Assert.Equal(typeof(ContractStateMachine), definedRegistration.StateMachineType);
        Assert.Equal(typeof(ConventionStateMachine), conventionRegistration.StateMachineType);
        AssertResult(definedResult, owner, definedRegistration);
        AssertResult(conventionResult, owner, conventionRegistration);
        AssertCallback(definedRegistration, definedCallback);
        AssertCallback(conventionRegistration, conventionCallback);
        Assert.Same(provider.GetRequiredService<ContractStateMachine>(),
            provider.GetRequiredService<ISagaStateMachine<ContractState>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "repository-only-result-owner-type-and-exact-provider-identity")]
    public void RepositoryOnlyRegistration_PreservesOwnerSagaTypeResultAndProviderIdentity()
    {
        var services = new ServiceCollection();
        var repositoryProvider = new RecordingRepositoryProvider();
        IRegistrationConfigurator? owner = null;
        ISagaRegistrationConfigurator<RepositorySaga>? result = null;

        services.AddViciOneServiceBus(configurator =>
        {
            owner = configurator;
            SagaRegistrationConfiguratorExtensions.SetSagaRepositoryProvider(configurator, repositoryProvider);
            object participant = FindSagaParticipant(configurator);
            Assert.Same(repositoryProvider, ReadProperty(participant, "Provider"));

            result = SagaRegistrationConfiguratorExtensions.AddSagaRepository<RepositorySaga>(configurator);
            var repositoryTypes = Assert.IsAssignableFrom<IEnumerable<Type>>(ReadField(participant, "_repositoryOnlySagaTypes"));
            Assert.Equal(typeof(RepositorySaga), Assert.Single(repositoryTypes));
        });

        AssertResult(result, owner, expectedRegistration: null);
        (Type sagaType, object configured) = Assert.Single(repositoryProvider.Calls);
        Assert.Equal(typeof(RepositorySaga), sagaType);
        Assert.Same(owner, ReadField(configured, "_configurator"));
        Assert.Null(ReadFieldValue(configured, "_registration"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-GENERIC", "provider-null-guard-before-participant-or-di-effects")]
    public void SetSagaRepositoryProvider_RejectsNullBeforeParticipantOrServiceMutation()
    {
        var services = new ServiceCollection();

        services.AddViciOneServiceBus(configurator =>
        {
            int baseline = services.Count;
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                SagaRegistrationConfiguratorExtensions.SetSagaRepositoryProvider(configurator, null!));

            Assert.Equal("provider", exception.ParamName);
            Assert.Equal(baseline, services.Count);
            Assert.DoesNotContain(CompletionParticipants(configurator),
                participant => participant.GetType().Name == "SagaRegistrationCompletionParticipant");
        });
    }

    private static void AssertSagaShape(MethodInfo method)
    {
        Type saga = Assert.Single(method.GetGenericArguments());
        AssertReferenceConstraint(saga, typeof(ISaga));
        ParameterInfo[] parameters = method.GetParameters();
        Assert.Contains(parameters.Length, new[] { 2, 3 });
        Assert.Equal(typeof(IRegistrationConfigurator), parameters[0].ParameterType);
        int callbackIndex = parameters.Length - 1;
        if (parameters.Length == 3)
            Assert.Equal(typeof(Type), parameters[1].ParameterType);
        Assert.Equal(typeof(Action<,>).MakeGenericType(
            typeof(IRegistrationContext), typeof(ISagaConfigurator<>).MakeGenericType(saga)), parameters[callbackIndex].ParameterType);
        Assert.True(parameters[callbackIndex].IsOptional);
        Assert.Null(parameters[callbackIndex].DefaultValue);
        Assert.Equal(typeof(ISagaRegistrationConfigurator<>).MakeGenericType(saga), method.ReturnType);
    }

    private static void AssertStateMachineShape(MethodInfo method)
    {
        Type[] arguments = method.GetGenericArguments();
        Assert.Equal(2, arguments.Length);
        Type stateMachine = arguments[0];
        Type saga = arguments[1];
        AssertReferenceConstraint(stateMachine, typeof(ISagaStateMachine<>).MakeGenericType(saga));
        AssertReferenceConstraint(saga, typeof(ISagaStateMachineInstance));
        ParameterInfo[] parameters = method.GetParameters();
        Assert.Contains(parameters.Length, new[] { 2, 3 });
        Assert.Equal(typeof(IRegistrationConfigurator), parameters[0].ParameterType);
        int callbackIndex = parameters.Length - 1;
        if (parameters.Length == 3)
            Assert.Equal(typeof(Type), parameters[1].ParameterType);
        Assert.Equal(typeof(Action<,>).MakeGenericType(
            typeof(IRegistrationContext), typeof(ISagaConfigurator<>).MakeGenericType(saga)), parameters[callbackIndex].ParameterType);
        Assert.True(parameters[callbackIndex].IsOptional);
        Assert.Null(parameters[callbackIndex].DefaultValue);
        Assert.Equal(typeof(ISagaRegistrationConfigurator<>).MakeGenericType(saga), method.ReturnType);
    }

    private static void AssertReferenceConstraint(Type parameter, Type contract)
    {
        Assert.True((parameter.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
        Assert.Equal([contract], parameter.GetGenericParameterConstraints());
    }

    private static void AssertNullConfigurator(Action action) =>
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(action).ParamName);

    private static void AssertDefinitionArgument(Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("sagaDefinitionType", exception.ParamName);
        Assert.Equal(new ArgumentException(DefinitionMessage, "sagaDefinitionType").Message, exception.Message);
    }

    private static Type[] InvalidSagaDefinitions() =>
    [
        typeof(int),
        typeof(ISagaDefinition<ContractSaga>),
        typeof(AbstractContractSagaDefinition),
        typeof(OpenSagaDefinition<>),
        typeof(UnrelatedDefinition),
        typeof(OtherSagaDefinition),
    ];

    private static Type[] InvalidStateDefinitions() =>
    [
        typeof(int),
        typeof(ISagaDefinition<ContractState>),
        typeof(AbstractContractStateDefinition),
        typeof(OpenSagaDefinition<>),
        typeof(UnrelatedDefinition),
        typeof(OtherSagaDefinition),
    ];

    private static (ServiceCollection Services, IRegistrationConfigurator Configurator) CreateCompletedConfiguration()
    {
        var services = new ServiceCollection();
        IRegistrationConfigurator? configurator = null;
        services.AddViciOneServiceBus(value => configurator = value);
        return (services, Assert.IsAssignableFrom<IRegistrationConfigurator>(configurator));
    }

    private static void AssertResult<TSaga>(ISagaRegistrationConfigurator<TSaga>? result,
        IRegistrationConfigurator? owner, ISagaRegistration? expectedRegistration)
        where TSaga : class, ISaga
    {
        var exact = Assert.IsType<SagaRegistrationConfigurator<TSaga>>(result);
        Assert.Same(owner, ReadField(exact, "_configurator"));
        Assert.Same(expectedRegistration, ReadFieldValue(exact, "_registration"));
    }

    private static void AssertCallback<TSaga>(ISagaRegistration registration,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>> expected)
        where TSaga : class
    {
        var callbacks = Assert.IsAssignableFrom<IEnumerable<Action<IRegistrationContext, ISagaConfigurator<TSaga>>>>(
            ReadField(registration, "_configureActions"));
        Assert.Same(expected, Assert.Single(callbacks));
    }

    private static object FindSagaParticipant(IRegistrationConfigurator configurator) =>
        Assert.Single(CompletionParticipants(configurator),
            participant => participant.GetType().Name == "SagaRegistrationCompletionParticipant");

    private static IEnumerable<object> CompletionParticipants(IRegistrationConfigurator configurator)
    {
        var participants = Assert.IsAssignableFrom<IDictionary>(ReadField(configurator, "_registrationCompletionParticipants"));
        return participants.Values.Cast<object>();
    }

    private static object ReadProperty(object target, string name) =>
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)
        ?? throw new InvalidOperationException($"Property '{name}' was not found or was null.");

    private static object ReadField(object target, string name) =>
        ReadFieldValue(target, name) ?? throw new InvalidOperationException($"Field '{name}' was null.");

    private static object? ReadFieldValue(object target, string name)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(target);
        }

        throw new InvalidOperationException($"Field '{name}' was not found.");
    }

    public sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ConventionSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class RepositorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class OtherSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ContractSagaDefinition : SagaDefinition<ContractSaga>
    {
    }

    public abstract class AbstractContractSagaDefinition : SagaDefinition<ContractSaga>
    {
    }

    public sealed class OtherSagaDefinition : SagaDefinition<OtherSaga>
    {
    }

    public sealed class OpenSagaDefinition<TSaga> : SagaDefinition<TSaga>
        where TSaga : class, ISaga
    {
    }

    public sealed class UnrelatedDefinition
    {
    }

    public sealed class ContractState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ConventionState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ContractStateMachine : ViciOneServiceBusStateMachine<ContractState>
    {
    }

    public sealed class ConventionStateMachine : ViciOneServiceBusStateMachine<ConventionState>
    {
    }

    public sealed class ContractStateDefinition : SagaDefinition<ContractState>
    {
    }

    public abstract class AbstractContractStateDefinition : SagaDefinition<ContractState>
    {
    }

    private sealed class RecordingRepositoryProvider : ISagaRepositoryRegistrationProvider
    {
        public List<(Type SagaType, object Configurator)> Calls { get; } = [];

        public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga =>
            Calls.Add((typeof(TSaga), configurator));
    }
}
