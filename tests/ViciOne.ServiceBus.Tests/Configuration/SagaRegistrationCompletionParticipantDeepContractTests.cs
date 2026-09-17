using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaRegistrationCompletionParticipantDeepContractTests
{
    static readonly Type ParticipantType = typeof(SagaRegistrationConfiguratorExtensions).Assembly.GetType(
        "ViciOne.ServiceBus.Configuration.SagaRegistrationCompletionParticipant",
        throwOnError: true)!;

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "exact-internal-participant-surface-and-generic-constraint")]
    public void Surface_ExposesTheExactInternalParticipantContract()
    {
        Assert.True(ParticipantType.IsNotPublic && ParticipantType.IsSealed);
        Assert.Equal([typeof(IRegistrationCompletionParticipant)], ParticipantType.GetInterfaces());

        PropertyInfo order = GetProperty("Order");
        Assert.Equal(typeof(int), order.PropertyType);
        Assert.True(order.GetMethod?.IsPublic);
        Assert.Null(order.SetMethod);

        PropertyInfo provider = GetProperty("Provider");
        Assert.Equal(typeof(ISagaRepositoryRegistrationProvider), provider.PropertyType);
        Assert.True(provider.GetMethod?.IsPublic);
        Assert.True(provider.SetMethod?.IsPublic);

        MethodInfo ensure = GetMethod("Ensure");
        Assert.True(ensure.IsPublic && ensure.IsStatic);
        Assert.Equal(ParticipantType, ensure.ReturnType);
        Assert.Equal([typeof(IRegistrationConfigurator)], ensure.GetParameters().Select(x => x.ParameterType));

        MethodInfo require = GetMethod("RequireRepository");
        Assert.True(require.IsPublic && require.IsStatic);
        Assert.Equal(typeof(void), require.ReturnType);
        Type sagaType = Assert.Single(require.GetGenericArguments());
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            sagaType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISaga)], sagaType.GetGenericParameterConstraints());
        Assert.Equal([typeof(IRegistrationConfigurator)], require.GetParameters().Select(x => x.ParameterType));

        MethodInfo complete = GetMethod("Complete");
        Assert.True(complete.IsPublic && !complete.IsStatic);
        Assert.Equal(typeof(void), complete.ReturnType);
        Assert.Equal([typeof(IRegistrationConfigurator)], complete.GetParameters().Select(x => x.ParameterType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "canonical-participant-default-provider-order-and-provider-identity")]
    public void Ensure_ReturnsTheCanonicalParticipantWithStableOrderAndProviderIdentity()
    {
        (InspectableRegistrationConfigurator configurator, _) = CreateConfiguration();

        object first = Ensure(configurator);
        object second = Ensure(configurator);

        Assert.Same(first, second);
        Assert.Equal(100, GetProperty("Order").GetValue(first));
        Assert.Equal("MissingSagaRepositoryRegistrationProvider", GetProperty("Provider").GetValue(first)?.GetType().Name);

        var replacement = new RecordingRepositoryProvider();
        SetProvider(first, replacement);

        Assert.Same(replacement, GetProperty("Provider").GetValue(second));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "ensure-require-complete-and-provider-null-guard-ownership")]
    public void Boundaries_RejectMissingRequiredInputsWithStableOwnership()
    {
        (InspectableRegistrationConfigurator configurator, _) = CreateConfiguration();
        object participant = Ensure(configurator);

        AssertConfiguratorArgument(() => Ensure(null!));
        AssertConfiguratorArgument(() => RequireRepository<AlphaSaga>(null!));
        AssertConfiguratorArgument(() => ((IRegistrationCompletionParticipant)participant).Complete(null!));

        ArgumentNullException provider = Assert.Throws<ArgumentNullException>(() => SetProvider(participant, null!));
        Assert.Equal("value", provider.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "duplicate-requirements-and-registration-overlap-configure-once")]
    public void RequireRepository_DeduplicatesRepeatedAndRegisteredSagaRequirements()
    {
        var registration = new StubSagaRegistration(typeof(AlphaSaga));
        (InspectableRegistrationConfigurator configurator, _) = CreateConfiguration(registration);
        object participant = Ensure(configurator);
        var provider = new RecordingRepositoryProvider();
        SetProvider(participant, provider);

        RequireRepository<AlphaSaga>(configurator);
        RequireRepository<AlphaSaga>(configurator);
        ((IRegistrationCompletionParticipant)participant).Complete(configurator);

        RepositoryCall call = Assert.Single(provider.Calls);
        Assert.Equal(typeof(AlphaSaga), call.SagaType);
        AssertConfiguratorIdentity(call.Configurator, configurator, registration);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "unique-global-order-existing-repository-and-registration-identity")]
    public void Complete_BuildsOneOrderedPlanSkipsExistingRepositoriesAndPreservesRegistrationIdentity()
    {
        var firstDelta = new StubSagaRegistration(typeof(DeltaSaga));
        var bravo = new StubSagaRegistration(typeof(BravoSaga));
        var charlie = new StubSagaRegistration(typeof(CharlieSaga));
        var duplicateDelta = new StubSagaRegistration(typeof(DeltaSaga));
        (InspectableRegistrationConfigurator configurator, ServiceCollection services) =
            CreateConfiguration(firstDelta, bravo, charlie, duplicateDelta);
        ((IServiceCollection)services).Add(ServiceDescriptor.Singleton(
            typeof(ISagaRepositoryContextFactory<BravoSaga>),
            static _ => new object()));
        object participant = Ensure(configurator);
        var provider = new RecordingRepositoryProvider();
        SetProvider(participant, provider);
        RequireRepository<DeltaSaga>(configurator);
        RequireRepository<AlphaSaga>(configurator);

        ((IRegistrationCompletionParticipant)participant).Complete(configurator);

        Assert.Equal(
            [typeof(AlphaSaga), typeof(CharlieSaga), typeof(DeltaSaga)],
            provider.Calls.Select(x => x.SagaType));
        AssertConfiguratorIdentity(provider.Calls[0].Configurator, configurator, expectedRegistration: null);
        AssertConfiguratorIdentity(provider.Calls[1].Configurator, configurator, charlie);
        AssertConfiguratorIdentity(provider.Calls[2].Configurator, configurator, firstDelta);
        Assert.DoesNotContain(provider.Calls, x => x.SagaType == typeof(BravoSaga));
        Assert.DoesNotContain(provider.Calls, x => ReferenceEquals(ReadField(x.Configurator, "_registration"), duplicateDelta));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "provider-snapshot-isolated-from-mid-completion-replacement")]
    public void Complete_UsesOneProviderSnapshotForTheEntirePlan()
    {
        var alpha = new StubSagaRegistration(typeof(AlphaSaga));
        var charlie = new StubSagaRegistration(typeof(CharlieSaga));
        (InspectableRegistrationConfigurator configurator, _) = CreateConfiguration(charlie, alpha);
        object participant = Ensure(configurator);
        var replacement = new RecordingRepositoryProvider();
        var original = new RecordingRepositoryProvider
        {
            OnConfigure = _ => SetProvider(participant, replacement),
        };
        SetProvider(participant, original);

        ((IRegistrationCompletionParticipant)participant).Complete(configurator);

        Assert.Equal([typeof(AlphaSaga), typeof(CharlieSaga)], original.Calls.Select(x => x.SagaType));
        Assert.Empty(replacement.Calls);
        Assert.Same(replacement, GetProperty("Provider").GetValue(participant));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "runtime-type-and-reflection-activation-preflight-before-provider-effects")]
    public void Complete_PreflightsEveryRuntimeSagaTypeBeforeInvokingTheProvider()
    {
        var valid = new StubSagaRegistration(typeof(AlphaSaga));
        var invalid = new StubSagaRegistration(typeof(string));
        (InspectableRegistrationConfigurator configurator, ServiceCollection services) = CreateConfiguration(valid, invalid);
        object participant = Ensure(configurator);
        var provider = new RecordingRepositoryProvider();
        SetProvider(participant, provider);
        int serviceCount = services.Count;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ((IRegistrationCompletionParticipant)participant).Complete(configurator));

        Assert.Contains(typeof(string).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ISaga), exception.Message, StringComparison.Ordinal);
        Assert.Empty(provider.Calls);
        Assert.Equal(serviceCount, services.Count);

        (InspectableRegistrationConfigurator nullRegistrationConfigurator, _) = CreateConfiguration([null!]);
        object nullRegistrationParticipant = Ensure(nullRegistrationConfigurator);
        var nullRegistrationProvider = new RecordingRepositoryProvider();
        SetProvider(nullRegistrationParticipant, nullRegistrationProvider);

        InvalidOperationException nullRegistration = Assert.Throws<InvalidOperationException>(() =>
            ((IRegistrationCompletionParticipant)nullRegistrationParticipant).Complete(nullRegistrationConfigurator));

        Assert.Contains("null registration", nullRegistration.Message, StringComparison.Ordinal);
        Assert.Empty(nullRegistrationProvider.Calls);

        var missingType = new StubSagaRegistration(null!);
        (InspectableRegistrationConfigurator missingTypeConfigurator, _) = CreateConfiguration(missingType);
        object missingTypeParticipant = Ensure(missingTypeConfigurator);
        var missingTypeProvider = new RecordingRepositoryProvider();
        SetProvider(missingTypeParticipant, missingTypeProvider);

        InvalidOperationException missingTypeException = Assert.Throws<InvalidOperationException>(() =>
            ((IRegistrationCompletionParticipant)missingTypeParticipant).Complete(missingTypeConfigurator));

        Assert.Contains("did not specify a saga type", missingTypeException.Message, StringComparison.Ordinal);
        Assert.Empty(missingTypeProvider.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-COMPLETION", "provider-failure-propagates-unwrapped-and-stops-remaining-plan")]
    public void Complete_PropagatesProviderFailureUnwrappedAndStopsTheRemainingPlan()
    {
        var alpha = new StubSagaRegistration(typeof(AlphaSaga));
        var bravo = new StubSagaRegistration(typeof(BravoSaga));
        var charlie = new StubSagaRegistration(typeof(CharlieSaga));
        (InspectableRegistrationConfigurator configurator, _) = CreateConfiguration(charlie, bravo, alpha);
        object participant = Ensure(configurator);
        var expected = new InvalidOperationException("provider failure");
        var provider = new RecordingRepositoryProvider
        {
            OnConfigure = sagaType =>
            {
                if (sagaType == typeof(BravoSaga))
                    throw expected;
            },
        };
        SetProvider(participant, provider);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ((IRegistrationCompletionParticipant)participant).Complete(configurator));

        Assert.Same(expected, exception);
        Assert.Equal([typeof(AlphaSaga), typeof(BravoSaga)], provider.Calls.Select(x => x.SagaType));
    }

    static (InspectableRegistrationConfigurator Configurator, ServiceCollection Services) CreateConfiguration(
        params ISagaRegistration[] registrations)
    {
        var services = new ServiceCollection();
        var registrar = new StubContainerRegistrar(services, registrations);
        return (new InspectableRegistrationConfigurator(services, registrar), services);
    }

    static object Ensure(IRegistrationConfigurator configurator) =>
        Invoke(GetMethod("Ensure"), target: null, configurator)!;

    static void RequireRepository<TSaga>(IRegistrationConfigurator configurator)
        where TSaga : class, ISaga =>
        Invoke(GetMethod("RequireRepository").MakeGenericMethod(typeof(TSaga)), target: null, configurator);

    static void SetProvider(object participant, ISagaRepositoryRegistrationProvider provider) =>
        Invoke(GetProperty("Provider").SetMethod!, participant, provider);

    static MethodInfo GetMethod(string name) =>
        Assert.Single(ParticipantType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly),
            method => method.Name == name);

    static PropertyInfo GetProperty(string name) =>
        ParticipantType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        ?? throw new InvalidOperationException($"Property '{name}' was not found.");

    static object? Invoke(MethodInfo method, object? target, params object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    static void AssertConfiguratorArgument(Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("configurator", exception.ParamName);
    }

    static void AssertConfiguratorIdentity(object registrationConfigurator, IRegistrationConfigurator owner,
        ISagaRegistration? expectedRegistration)
    {
        Assert.Same(owner, ReadField(registrationConfigurator, "_configurator"));
        Assert.Same(expectedRegistration, ReadField(registrationConfigurator, "_registration"));
    }

    static object? ReadField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{name}' was not found.");
        return field.GetValue(target);
    }

    sealed class InspectableRegistrationConfigurator(IServiceCollection services, IContainerRegistrar registrar)
        : RegistrationConfigurator(services, registrar);

    sealed class StubContainerRegistrar(IServiceCollection services, IEnumerable<ISagaRegistration> registrations)
        : DependencyInjectionContainerRegistrar(services)
    {
        readonly ISagaRegistration[] _registrations = registrations.ToArray();

        public override IEnumerable<T> GetRegistrations<T>() =>
            typeof(T) == typeof(ISagaRegistration)
                ? _registrations.Cast<T>()
                : base.GetRegistrations<T>();
    }

    sealed class StubSagaRegistration(Type type) : ISagaRegistration
    {
        public Type Type { get; } = type;

        public bool IncludeInConfigureEndpoints { get; set; } = true;

        public Type? StateMachineType => null;

        public void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
            where T : class
        {
        }

        public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context) =>
            throw new NotSupportedException();

        public ISagaDefinition GetDefinition(IRegistrationContext context) =>
            throw new NotSupportedException();
    }

    sealed class RecordingRepositoryProvider : ISagaRepositoryRegistrationProvider
    {
        public List<RepositoryCall> Calls { get; } = [];

        public Action<Type>? OnConfigure { get; init; }

        public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            Calls.Add(new RepositoryCall(typeof(TSaga), configurator));
            OnConfigure?.Invoke(typeof(TSaga));
        }
    }

    sealed record RepositoryCall(Type SagaType, object Configurator);

    public sealed class AlphaSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class BravoSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class CharlieSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class DeltaSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
