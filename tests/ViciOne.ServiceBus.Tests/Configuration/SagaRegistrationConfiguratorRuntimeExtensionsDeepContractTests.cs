using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaRegistrationConfiguratorRuntimeExtensionsDeepContractTests
{
    private const string SagaTypeMessage =
        "The saga type must be a closed reference type that implements ISaga.";

    private const string StateMachineTypeMessage =
        "The saga state machine type must be a closed reference type that implements exactly one "
        + "ISagaStateMachine<TSaga> whose state implements ISagaStateMachineInstance.";

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "runtime-extensions-exact-two-method-public-surface")]
    public void PublicSurface_ExposesExactlyTheTwoNonGenericRuntimeRegistrationMethods()
    {
        Type extensions = typeof(SagaRegistrationConfiguratorRuntimeExtensions);

        Assert.True(extensions.IsPublic);
        Assert.True(extensions.IsAbstract);
        Assert.True(extensions.IsSealed);

        MethodInfo[] methods = extensions
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(SagaRegistrationConfiguratorRuntimeExtensions.AddSaga),
                nameof(SagaRegistrationConfiguratorRuntimeExtensions.AddSagaStateMachine),
            ],
            methods.Select(method => method.Name));

        Assert.All(methods, method =>
        {
            Assert.False(method.IsGenericMethod);
            Assert.Equal(typeof(ISagaRegistrationConfigurator), method.ReturnType);
            Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false));

            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(["configurator", "sagaType", "sagaDefinitionType"], parameters.Select(parameter => parameter.Name));
            Assert.Equal(
                [typeof(IRegistrationConfigurator), typeof(Type), typeof(Type)],
                parameters.Select(parameter => parameter.ParameterType));
            Assert.False(parameters[0].IsOptional);
            Assert.False(parameters[1].IsOptional);
            Assert.True(parameters[2].IsOptional);
            Assert.True(parameters[2].HasDefaultValue);
            Assert.Null(parameters[2].DefaultValue);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "runtime-extensions-receiver-first-null-guard-order")]
    public void RequiredBoundaries_ValidateTheReceiverBeforeTheSagaTypeWithoutEffects()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SagaRegistrationConfiguratorRuntimeExtensions.AddSaga(null!, null!, typeof(RuntimeSagaDefinition))).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SagaRegistrationConfiguratorRuntimeExtensions.AddSagaStateMachine(null!, null!, typeof(RuntimeStateDefinition))).ParamName);

        (ServiceCollection services, IRegistrationConfigurator configurator) = CreateConfiguration();
        int baseline = services.Count;

        Assert.Equal("sagaType", Assert.Throws<ArgumentNullException>(() =>
            SagaRegistrationConfiguratorRuntimeExtensions.AddSaga(configurator, null!, typeof(RuntimeSagaDefinition))).ParamName);
        Assert.Equal(baseline, services.Count);

        Assert.Equal("sagaType", Assert.Throws<ArgumentNullException>(() =>
            SagaRegistrationConfiguratorRuntimeExtensions.AddSagaStateMachine(configurator, null!, typeof(RuntimeStateDefinition))).ParamName);
        Assert.Equal(baseline, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "runtime-add-saga-invalid-type-matrix")]
    public void AddSaga_InvalidRuntimeTypeMatrixUsesStableDiagnosticsWithoutEffects()
    {
        (ServiceCollection services, IRegistrationConfigurator configurator) = CreateConfiguration();
        int baseline = services.Count;

        foreach (Type invalidType in new[] { typeof(ValueSaga), typeof(OpenSaga<>), typeof(string) })
        {
            AssertSagaTypeArgument(SagaTypeMessage, () =>
                SagaRegistrationConfiguratorRuntimeExtensions.AddSaga(
                    configurator,
                    invalidType,
                    typeof(RuntimeSagaDefinition)));
            Assert.Equal(baseline, services.Count);
        }

        AssertSagaTypeArgument(
            $"State machine sagas must be registered using AddSagaStateMachine: {TypeCache.GetShortName(typeof(RuntimeState))}",
            () => SagaRegistrationConfiguratorRuntimeExtensions.AddSaga(
                configurator,
                typeof(RuntimeState),
                typeof(RuntimeStateDefinition)));
        Assert.Equal(baseline, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "runtime-add-state-machine-invalid-type-matrix")]
    public void AddSagaStateMachine_InvalidRuntimeTypeMatrixUsesStableDiagnosticsWithoutEffects()
    {
        (ServiceCollection services, IRegistrationConfigurator configurator) = CreateConfiguration();
        int baseline = services.Count;

        foreach (Type invalidType in new[]
                 {
                     typeof(ValueSaga),
                     typeof(OpenStateMachine<>),
                     typeof(string),
                     typeof(RuntimeSaga),
                     typeof(AmbiguousRuntimeStateMachine),
                 })
        {
            AssertSagaTypeArgument(StateMachineTypeMessage, () =>
                SagaRegistrationConfiguratorRuntimeExtensions.AddSagaStateMachine(
                    configurator,
                    invalidType,
                    typeof(RuntimeStateDefinition)));
            Assert.Equal(baseline, services.Count);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "runtime-valid-registration-definition-forwarding-and-result-identity")]
    public void ValidRuntimeRegistration_ForwardsDefinitionsAndReturnsConfiguratorsBoundToTheExactRegistrations()
    {
        var services = new ServiceCollection();
        IRegistrationConfigurator? owner = null;
        ISagaRegistrationConfigurator? sagaResult = null;
        ISagaRegistrationConfigurator? stateMachineResult = null;

        services.AddViciOneServiceBus(configurator =>
        {
            owner = configurator;
            configurator.SetInMemorySagaRepositoryProvider();
            sagaResult = SagaRegistrationConfiguratorRuntimeExtensions.AddSaga(
                configurator,
                typeof(RuntimeSaga),
                typeof(RuntimeSagaDefinition));
            stateMachineResult = SagaRegistrationConfiguratorRuntimeExtensions.AddSagaStateMachine(
                configurator,
                typeof(RuntimeStateMachine),
                typeof(RuntimeStateDefinition));
        });

        IRegistrationConfigurator exactOwner = Assert.IsAssignableFrom<IRegistrationConfigurator>(owner);
        var exactSagaResult = Assert.IsType<SagaRegistrationConfigurator<RuntimeSaga>>(sagaResult);
        var exactStateMachineResult = Assert.IsType<SagaRegistrationConfigurator<RuntimeState>>(stateMachineResult);

        using ServiceProvider provider = services.BuildServiceProvider();
        RuntimeSagaDefinition sagaDefinition = provider.GetRequiredService<RuntimeSagaDefinition>();
        RuntimeStateDefinition stateDefinition = provider.GetRequiredService<RuntimeStateDefinition>();
        Assert.Same(sagaDefinition, provider.GetRequiredService<ISagaDefinition<RuntimeSaga>>());
        Assert.Same(stateDefinition, provider.GetRequiredService<ISagaDefinition<RuntimeState>>());

        ISagaRegistration[] registrations = provider.GetServices<ISagaRegistration>().ToArray();
        ISagaRegistration sagaRegistration = Assert.Single(registrations, registration => registration.Type == typeof(RuntimeSaga));
        ISagaRegistration stateMachineRegistration = Assert.Single(
            registrations,
            registration => registration.Type == typeof(RuntimeState));
        Assert.Null(sagaRegistration.StateMachineType);
        Assert.Equal(typeof(RuntimeStateMachine), stateMachineRegistration.StateMachineType);

        Assert.Same(exactOwner, ReadField(exactSagaResult, "_configurator"));
        Assert.Same(sagaRegistration, ReadField(exactSagaResult, "_registration"));
        Assert.Same(exactOwner, ReadField(exactStateMachineResult, "_configurator"));
        Assert.Same(stateMachineRegistration, ReadField(exactStateMachineResult, "_registration"));

        RuntimeStateMachine stateMachine = provider.GetRequiredService<RuntimeStateMachine>();
        Assert.Same(stateMachine, provider.GetRequiredService<ISagaStateMachine<RuntimeState>>());
    }

    private static (ServiceCollection Services, IRegistrationConfigurator Configurator) CreateConfiguration()
    {
        var services = new ServiceCollection();
        IRegistrationConfigurator? configurator = null;
        services.AddViciOneServiceBus(value => configurator = value);

        return (services, Assert.IsAssignableFrom<IRegistrationConfigurator>(configurator));
    }

    private static void AssertSagaTypeArgument(string expectedMessage, Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);

        Assert.Equal("sagaType", exception.ParamName);
        Assert.Equal(new ArgumentException(expectedMessage, "sagaType").Message, exception.Message);
    }

    private static object ReadField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{name}' was not found.");

        return field.GetValue(target) ?? throw new InvalidOperationException($"Field '{name}' was null.");
    }

    private struct ValueSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class OpenSaga<T> : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class OpenStateMachine<TState> : ViciOneServiceBusStateMachine<TState>
        where TState : class, ISagaStateMachineInstance
    {
    }

    public sealed class RuntimeSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class RuntimeSagaDefinition : SagaDefinition<RuntimeSaga>
    {
    }

    public sealed class RuntimeState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class RuntimeStateMachine : ViciOneServiceBusStateMachine<RuntimeState>
    {
    }

    private sealed class AlternateRuntimeState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class AmbiguousRuntimeStateMachine :
        ViciOneServiceBusStateMachine<RuntimeState>,
        ISagaStateMachine<AlternateRuntimeState>
    {
        IEnumerable<IEventCorrelation> ISagaStateMachine<AlternateRuntimeState>.Correlations =>
            throw new NotSupportedException();

        IStateAccessor<AlternateRuntimeState> IStateMachine<AlternateRuntimeState>.Accessor =>
            throw new NotSupportedException();

        Task<bool> ISagaStateMachine<AlternateRuntimeState>.IsCompletedAsync(
            IBehaviorContext<AlternateRuntimeState> context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        IState<AlternateRuntimeState> IStateMachine<AlternateRuntimeState>.GetState(string name) =>
            throw new NotSupportedException();

        Task IStateMachine<AlternateRuntimeState>.RaiseEventAsync(
            IBehaviorContext<AlternateRuntimeState> context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        Task IStateMachine<AlternateRuntimeState>.RaiseEventAsync<TMessage>(
            IBehaviorContext<AlternateRuntimeState, TMessage> context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        IDisposable IStateMachine<AlternateRuntimeState>.ConnectEventObserver(
            IEventObserver<AlternateRuntimeState> observer) =>
            throw new NotSupportedException();

        IDisposable IStateMachine<AlternateRuntimeState>.ConnectEventObserver(
            IEvent @event,
            IEventObserver<AlternateRuntimeState> observer) =>
            throw new NotSupportedException();

        IDisposable IStateMachine<AlternateRuntimeState>.ConnectStateObserver(
            IStateObserver<AlternateRuntimeState> observer) =>
            throw new NotSupportedException();
    }

    public sealed class RuntimeStateDefinition : SagaDefinition<RuntimeState>
    {
    }
}
